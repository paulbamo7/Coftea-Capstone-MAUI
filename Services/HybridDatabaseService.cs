using System.Diagnostics;
using System.Text.Json;
using Coftea_Capstone.Models;
using Coftea_Capstone.C_;

namespace Coftea_Capstone.Services;

/// <summary>
/// Hybrid database service that automatically switches between offline (SQLite) and online (MySQL)
/// Uses local database when offline, syncs to MySQL when online
/// </summary>
public class HybridDatabaseService
{
    private readonly LocalDatabaseService _localDb;
    private readonly Database _onlineDb;
    private readonly ConnectivityService _connectivity;
    
    public bool IsOnline => _connectivity.IsConnected;
    
    public HybridDatabaseService(
        LocalDatabaseService localDb,
        Database onlineDb,
        ConnectivityService connectivity)
    {
        _localDb = localDb;
        _onlineDb = onlineDb;
        _connectivity = connectivity;
    }
    
    // ==================== TRANSACTIONS ====================
    
    /// <summary>
    /// Save a transaction - works both online and offline
    /// </summary>
    public async Task<int> SaveTransactionAsync(int? userId, decimal totalAmount, string paymentMethod, string orderDetailsJson)
    {
        var transaction = new LocalTransaction
        {
            userId = userId,
            totalAmount = totalAmount,
            paymentMethod = paymentMethod,
            transactionDate = DateTime.Now,
            orderDetails = orderDetailsJson
        };
        
        // Always save to local database first (for offline capability)
        await _localDb.SaveTransactionAsync(transaction);
        
        if (IsOnline)
        {
            try
            {
                // TODO: Implement online transaction saving with proper model mapping
                // For now, just queue it for manual review
                await _localDb.QueueOperationAsync("INSERT", "Transactions", JsonSerializer.Serialize(transaction));
                Debug.WriteLine($"[HybridDB] 📝 Transaction queued for sync: ₱{totalAmount}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HybridDB] ⚠️ Failed to queue transaction: {ex.Message}");
            }
        }
        else
        {
            // Queue for later sync
            await _localDb.QueueOperationAsync("INSERT", "Transactions", JsonSerializer.Serialize(transaction));
            Debug.WriteLine($"[HybridDB] 📱 OFFLINE: Transaction queued: ₱{totalAmount}");
        }
        
        return 1;
    }
    
    // ==================== INVENTORY ====================
    
    /// <summary>
    /// Update inventory quantity - works both online and offline
    /// </summary>
    public async Task UpdateInventoryQuantityAsync(int inventoryId, double newQuantity, int? userId, string actionType, string? reason = null)
    {
        // Update local first
        await _localDb.UpdateInventoryQuantityAsync(inventoryId, newQuantity);
        
        var item = await _localDb.GetInventoryByIdAsync(inventoryId);
        if (item == null) return;
        
        if (IsOnline)
        {
            try
            {
                // Update online
                var onlineItem = await _onlineDb.GetInventoryItemByIdAsync(inventoryId);
                if (onlineItem != null)
                {
                    var oldQuantity = onlineItem.itemQuantity;
                    onlineItem.itemQuantity = newQuantity;
                    await _onlineDb.SaveInventoryItemAsync(onlineItem);
                    
                    // Log activity
                    var logEntry = new InventoryActivityLog
                    {
                        ItemId = inventoryId,
                        ItemName = item.itemName,
                        ItemCategory = item.category,
                        Action = actionType,
                        QuantityChanged = newQuantity - oldQuantity,
                        PreviousQuantity = oldQuantity,
                        NewQuantity = newQuantity,
                        UnitOfMeasurement = item.unit,
                        Reason = reason,
                        UserId = userId,
                        Timestamp = DateTime.Now
                    };
                    await _onlineDb.LogInventoryActivityAsync(logEntry);
                    
                    Debug.WriteLine($"[HybridDB] ✅ ONLINE: Inventory updated: {item.itemName} -> {newQuantity} {item.unit}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HybridDB] ⚠️ Failed to update online inventory: {ex.Message}");
                // Queue for sync
                await _localDb.QueueOperationAsync("UPDATE", "InventoryItems", JsonSerializer.Serialize(item));
                
                // Queue activity log
                var log = new LocalInventoryActivityLog
                {
                    userId = userId,
                    inventoryItemID = inventoryId,
                    actionType = actionType,
                    quantityChanged = newQuantity,
                    reasonOrDetails = reason,
                    actionDate = DateTime.Now
                };
                await _localDb.QueueOperationAsync("INSERT", "InventoryActivityLog", JsonSerializer.Serialize(log));
            }
        }
        else
        {
            // Queue for sync
            await _localDb.QueueOperationAsync("UPDATE", "InventoryItems", JsonSerializer.Serialize(item));
            
            // Queue activity log
            var log = new LocalInventoryActivityLog
            {
                userId = userId,
                inventoryItemID = inventoryId,
                actionType = actionType,
                quantityChanged = newQuantity,
                reasonOrDetails = reason,
                actionDate = DateTime.Now
            };
            await _localDb.QueueOperationAsync("INSERT", "InventoryActivityLog", JsonSerializer.Serialize(log));
            
            Debug.WriteLine($"[HybridDB] 📱 OFFLINE: Inventory update queued: {item.itemName}");
        }
    }
    
    /// <summary>
    /// Deduct inventory (for sales) - works offline and online
    /// </summary>
    public async Task DeductInventoryAsync(int inventoryId, double amount, int? userId)
    {
        var item = await _localDb.GetInventoryByIdAsync(inventoryId);
        if (item == null)
        {
            // Try to get from online if not in local cache
            if (IsOnline)
            {
                var onlineItem = await _onlineDb.GetInventoryItemByIdAsync(inventoryId);
                if (onlineItem != null)
                {
                    // Cache it locally
                    item = new LocalInventoryItem
                    {
                        inventoryItemID = onlineItem.itemID,
                        itemName = onlineItem.itemName,
                        category = onlineItem.itemCategory,
                        currentQuantity = onlineItem.itemQuantity,
                        minimumQuantity = onlineItem.minimumQuantity,
                        maximumQuantity = onlineItem.maximumQuantity,
                        unit = onlineItem.unitOfMeasurement
                    };
                    await _localDb.SaveInventoryAsync(item);
                }
            }
            
            if (item == null)
            {
                Debug.WriteLine($"[HybridDB] ❌ Inventory item {inventoryId} not found");
                return;
            }
        }
        
        var newQuantity = item.currentQuantity - amount;
        if (newQuantity < 0) newQuantity = 0;
        
        await UpdateInventoryQuantityAsync(inventoryId, newQuantity, userId, "Sale Deduction", $"Deducted {amount} {item.unit}");
    }
    
    // ==================== PRODUCTS ====================
    
    /// <summary>
    /// Get all products - uses local cache when offline, online when available
    /// </summary>
    public async Task<List<POSPageModel>> GetAllProductsAsync()
    {
        if (IsOnline)
        {
            try
            {
                var products = await _onlineDb.GetProductsAsyncCached();
                
                // Cache them locally
                foreach (var product in products)
                {
                    var localProduct = new LocalProduct
                    {
                        productID = product.ProductID,
                        productName = product.ProductName,
                        smallPrice = product.SmallPrice,
                        mediumPrice = product.MediumPrice,
                        largePrice = product.LargePrice,
                        category = product.Category,
                        subcategory = product.Subcategory,
                        imageSet = product.ImageSet,
                        description = product.ProductDescription,
                        colorCode = product.ColorCode
                    };
                    await _localDb.SaveProductAsync(localProduct);
                }
                
                return products;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HybridDB] ⚠️ Failed to get online products: {ex.Message}");
            }
        }
        
        // Fallback to local
        Debug.WriteLine("[HybridDB] 📱 Using local products cache");
        var localProducts = await _localDb.GetAllProductsAsync();
        return localProducts.Select(p => new POSPageModel
        {
            ProductID = p.productID,
            ProductName = p.productName,
            SmallPrice = p.smallPrice ?? 0,
            MediumPrice = p.mediumPrice,
            LargePrice = p.largePrice,
            Category = p.category,
            Subcategory = p.subcategory,
            ImageSet = p.imageSet,
            ProductDescription = p.description,
            ColorCode = p.colorCode
        }).ToList();
    }
    
    /// <summary>
    /// Get all inventory items - uses local cache when offline
    /// </summary>
    public async Task<List<InventoryPageModel>> GetAllInventoryItemsAsync()
    {
        if (IsOnline)
        {
            try
            {
                var items = await _onlineDb.GetInventoryItemsAsyncCached();
                
                // Cache them locally
                foreach (var item in items)
                {
                    var localItem = new LocalInventoryItem
                    {
                        inventoryItemID = item.itemID,
                        itemName = item.itemName,
                        category = item.itemCategory,
                        currentQuantity = item.itemQuantity,
                        minimumQuantity = item.minimumQuantity,
                        maximumQuantity = item.maximumQuantity,
                        unit = item.unitOfMeasurement
                    };
                    await _localDb.SaveInventoryAsync(localItem);
                }
                
                return items;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HybridDB] ⚠️ Failed to get online inventory: {ex.Message}");
            }
        }
        
        // Fallback to local
        Debug.WriteLine("[HybridDB] 📱 Using local inventory cache");
        var localItems = await _localDb.GetAllInventoryAsync();
        return localItems.Select(i => new InventoryPageModel
        {
            itemID = i.inventoryItemID,
            itemName = i.itemName,
            itemCategory = i.category,
            itemQuantity = i.currentQuantity,
            minimumQuantity = i.minimumQuantity,
            maximumQuantity = i.maximumQuantity,
            unitOfMeasurement = i.unit
        }).ToList();
    }
    
    /// <summary>
    /// Get pending sync operations count
    /// </summary>
    public async Task<int> GetPendingOperationsCountAsync()
    {
        return await _localDb.GetPendingOperationsCountAsync();
    }
}

