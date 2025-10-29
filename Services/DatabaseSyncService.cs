using System.Diagnostics;
using System.Text.Json;
using Coftea_Capstone.Models;
using Coftea_Capstone.C_;

namespace Coftea_Capstone.Services;

/// <summary>
/// Service to sync offline data to online MySQL database
/// Handles background sync when internet is available
/// </summary>
public class DatabaseSyncService
{
    private readonly LocalDatabaseService _localDb;
    private readonly Database _onlineDb;
    private readonly ConnectivityService _connectivity;
    private bool _isSyncing = false;
    
    public event EventHandler<SyncStatus>? SyncStatusChanged;
    
    public DatabaseSyncService(
        LocalDatabaseService localDb, 
        Database onlineDb,
        ConnectivityService connectivity)
    {
        _localDb = localDb;
        _onlineDb = onlineDb;
        _connectivity = connectivity;
        
        // Subscribe to connectivity changes
        _connectivity.ConnectivityChanged += OnConnectivityChanged;
    }
    
    private async void OnConnectivityChanged(object? sender, bool isConnected)
    {
        if (isConnected)
        {
            Debug.WriteLine("[Sync] 🌐 Internet restored - pulling latest data and syncing...");
            
            // First, pull latest data from online database
            await PullLatestDataAsync();
            
            // Then, sync any pending operations
            await SyncPendingOperationsAsync();
        }
    }
    

    public async Task<SyncResult> SyncPendingOperationsAsync()
    {
        if (_isSyncing)
        {
            Debug.WriteLine("[Sync] ⏳ Sync already in progress, skipping...");
            return new SyncResult { Success = false, Message = "Sync already in progress" };
        }
        
        if (!_connectivity.IsConnected)
        {
            Debug.WriteLine("[Sync] ⚠️ No internet connection, cannot sync");
            return new SyncResult { Success = false, Message = "No internet connection" };
        }
        
        _isSyncing = true;
        SyncStatusChanged?.Invoke(this, new SyncStatus { IsSyncing = true, Progress = 0 });
        
        try
        {
            // Check if we can actually reach the database
            if (!await _connectivity.CanReachDatabaseAsync())
            {
                Debug.WriteLine("[Sync] ⚠️ Cannot reach database server");
                return new SyncResult { Success = false, Message = "Cannot reach database server" };
            }
            
            var pendingOps = await _localDb.GetPendingOperationsAsync();
            
            if (pendingOps.Count == 0)
            {
                Debug.WriteLine("[Sync] ✅ No pending operations to sync");
                SyncStatusChanged?.Invoke(this, new SyncStatus { IsSyncing = false, Progress = 100 });
                return new SyncResult { Success = true, Message = "No operations to sync", OperationsSynced = 0 };
            }
            
            Debug.WriteLine($"[Sync] 📤 Syncing {pendingOps.Count} pending operations...");
            
            int synced = 0;
            int failed = 0;
            
            foreach (var op in pendingOps)
            {
                try
                {
                    Debug.WriteLine($"[Sync] 📤 Processing Operation ID: {op.Id} | Type: {op.OperationType} | Table: {op.TableName}");
                    
                    await ProcessOperationAsync(op);
                    
                    // CRITICAL: Mark as synced immediately after successful processing
                    await _localDb.MarkOperationSyncedAsync(op.Id);
                    Debug.WriteLine($"[Sync] ✅ Operation {op.Id} marked as synced - will NOT be processed again");
                    
                    synced++;
                    
                    // Update progress
                    var progress = (int)((double)synced / pendingOps.Count * 100);
                    SyncStatusChanged?.Invoke(this, new SyncStatus 
                    { 
                        IsSyncing = true, 
                        Progress = progress,
                        Message = $"Synced {synced}/{pendingOps.Count}"
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Sync] ❌ Failed to sync operation {op.Id}: {ex.Message}");
                    Debug.WriteLine($"[Sync] ⚠️ Operation {op.Id} will be retried on next sync");
                    failed++;
                }
            }
            
            // Cleanup old synced operations (removes operations older than 7 days)
            var clearedCount = await _localDb.ClearSyncedOperationsAsync();
            Debug.WriteLine($"[Sync] 🧹 Cleared {clearedCount} old synced operations from queue");
            
            // Verify no pending operations remain
            var remainingPending = await _localDb.GetPendingOperationsCountAsync();
            Debug.WriteLine($"[Sync] ✅ Sync complete: {synced} synced, {failed} failed, {remainingPending} still pending");
            
            if (remainingPending > 0)
            {
                Debug.WriteLine($"[Sync] ⚠️ {remainingPending} operations still pending (these were not synced due to errors)");
            }
            else
            {
                Debug.WriteLine($"[Sync] ✨ All operations synced successfully! Queue is now empty.");
            }
            
            SyncStatusChanged?.Invoke(this, new SyncStatus { IsSyncing = false, Progress = 100 });
            
            return new SyncResult 
            { 
                Success = true, 
                Message = $"Synced {synced} operations" + (failed > 0 ? $", {failed} failed" : ""),
                OperationsSynced = synced,
                OperationsFailed = failed
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Sync] ❌ Sync error: {ex.Message}");
            SyncStatusChanged?.Invoke(this, new SyncStatus { IsSyncing = false, Progress = 0 });
            return new SyncResult { Success = false, Message = $"Sync error: {ex.Message}" };
        }
        finally
        {
            _isSyncing = false;
        }
    }
    
    private async Task ProcessOperationAsync(PendingOperation operation)
    {
        Debug.WriteLine($"[Sync] Processing: {operation.OperationType} on {operation.TableName}");
        
        switch (operation.TableName.ToLower())
        {
            case "transactions":
                await SyncTransactionAsync(operation);
                break;
                
            case "inventoryitems":
                await SyncInventoryAsync(operation);
                break;
                
            case "inventoryactivitylog":
                await SyncActivityLogAsync(operation);
                break;
                
            case "products":
                await SyncProductAsync(operation);
                break;
                
            case "users":
                await SyncUserAsync(operation);
                break;
                
            default:
                Debug.WriteLine($"[Sync] ⚠️ Unknown table: {operation.TableName}");
                break;
        }
    }
    
    private async Task SyncTransactionAsync(PendingOperation operation)
    {
        var data = JsonSerializer.Deserialize<LocalTransaction>(operation.DataJson);
        if (data == null) return;
        
        if (operation.OperationType == "INSERT")
        {
            // TODO: Implement proper transaction sync with order details parsing
            // For now, transactions are saved locally and can be reviewed manually
            Debug.WriteLine($"[Sync] ⏸️ Transaction sync pending (manual review required): ₱{data.totalAmount}");
        }
    }
    
    private async Task SyncInventoryAsync(PendingOperation operation)
    {
        var data = JsonSerializer.Deserialize<LocalInventoryItem>(operation.DataJson);
        if (data == null) return;
        
        if (operation.OperationType == "UPDATE")
        {
            // Update inventory quantity in online database
            var item = await _onlineDb.GetInventoryItemByIdAsync(data.inventoryItemID);
            if (item != null)
            {
                var oldQty = item.itemQuantity;
                
                // CRITICAL: Set to absolute quantity (NOT deducting again)
                // The local database already has the correct final quantity
                item.itemQuantity = data.currentQuantity;
                await _onlineDb.SaveInventoryItemAsync(item);
                
                Debug.WriteLine($"[Sync] ✅ Inventory synced: {data.itemName}");
                Debug.WriteLine($"[Sync]    Old Qty: {oldQty} {data.unit} → New Qty: {data.currentQuantity} {data.unit}");
                Debug.WriteLine($"[Sync]    This operation will NOT be processed again (marked as synced)");
            }
        }
        else if (operation.OperationType == "INSERT")
        {
            // Insert new inventory item
            var newItem = new InventoryPageModel
            {
                itemName = data.itemName,
                itemCategory = data.category,
                itemQuantity = data.currentQuantity,
                minimumQuantity = data.minimumQuantity,
                maximumQuantity = data.maximumQuantity,
                unitOfMeasurement = data.unit
            };
            await _onlineDb.SaveInventoryItemAsync(newItem);
            Debug.WriteLine($"[Sync] ✅ Inventory item created: {data.itemName}");
        }
    }
    
    private async Task SyncActivityLogAsync(PendingOperation operation)
    {
        var data = JsonSerializer.Deserialize<LocalInventoryActivityLog>(operation.DataJson);
        if (data == null) return;
        
        if (operation.OperationType == "INSERT")
        {
            var logEntry = new InventoryActivityLog
            {
                ItemId = data.inventoryItemID ?? 0,
                Action = data.actionType,
                QuantityChanged = data.quantityChanged,
                Reason = data.reasonOrDetails,
                UserId = data.userId,
                Timestamp = data.actionDate
            };
            await _onlineDb.LogInventoryActivityAsync(logEntry);
            Debug.WriteLine($"[Sync] ✅ Activity log synced: {data.actionType}");
        }
    }
    
    private async Task SyncProductAsync(PendingOperation operation)
    {
        var data = JsonSerializer.Deserialize<LocalProduct>(operation.DataJson);
        if (data == null) return;
        
        if (operation.OperationType == "INSERT")
        {
            var product = new POSPageModel
            {
                ProductName = data.productName,
                SmallPrice = data.smallPrice ?? 0,
                MediumPrice = data.mediumPrice,
                LargePrice = data.largePrice,
                Category = data.category,
                Subcategory = data.subcategory,
                ImageSet = data.imageSet,
                ProductDescription = data.description,
                ColorCode = data.colorCode
            };
            await _onlineDb.SaveProductAsync(product);
            Debug.WriteLine($"[Sync] ✅ Product synced: {data.productName}");
        }
        else if (operation.OperationType == "UPDATE")
        {
            var product = new POSPageModel
            {
                ProductID = data.productID,
                ProductName = data.productName,
                SmallPrice = data.smallPrice ?? 0,
                MediumPrice = data.mediumPrice,
                LargePrice = data.largePrice,
                Category = data.category,
                Subcategory = data.subcategory,
                ImageSet = data.imageSet,
                ProductDescription = data.description,
                ColorCode = data.colorCode
            };
            await _onlineDb.UpdateProductAsync(product);
            Debug.WriteLine($"[Sync] ✅ Product updated: {data.productName}");
        }
    }
    
    private async Task SyncUserAsync(PendingOperation operation)
    {
        var data = JsonSerializer.Deserialize<LocalUser>(operation.DataJson);
        if (data == null) return;
        
        if (operation.OperationType == "INSERT")
        {
            // TODO: Implement user sync if needed
            // Users are typically managed from admin panel only
            Debug.WriteLine($"[Sync] ⏸️ User sync pending (admin review required): {data.username}");
        }
    }
    
    /// <summary>
    /// Pull latest data from online database to local cache
    /// This runs when going online to get latest changes from other devices
    /// </summary>
    public async Task<bool> PullLatestDataAsync()
    {
        if (!_connectivity.IsConnected)
        {
            Debug.WriteLine("[Sync] ⚠️ No internet connection detected");
            return false;
        }
        
        try
        {
            Debug.WriteLine("[Sync] 📥 Pulling latest data from online database...");
            
            // Pull users
            try
            {
                Debug.WriteLine("[Sync] 👥 Pulling users...");
                var users = await _onlineDb.GetAllUsersAsync();
                Debug.WriteLine($"[Sync] Found {users.Count} users to sync");
                
                foreach (var user in users)
                {
                    var localUser = new LocalUser
                    {
                        id = user.ID,
                        username = user.Email,
                        hashedPassword = user.Password,
                        firstName = user.FirstName,
                        lastName = user.LastName,
                        contactNumber = user.PhoneNumber,
                        role = user.IsAdmin ? "Admin" : "Employee"
                    };
                    await _localDb.SaveUserAsync(localUser);
                }
                Debug.WriteLine($"[Sync] ✅ Synced {users.Count} users");
            }
            catch (Exception userEx)
            {
                Debug.WriteLine($"[Sync] ⚠️ Error pulling users: {userEx.Message}");
                // Continue with other data even if users fail
            }
            
            // Pull products
            try
            {
                Debug.WriteLine("[Sync] 🍵 Pulling products...");
                var products = await _onlineDb.GetProductsAsyncCached();
                Debug.WriteLine($"[Sync] Found {products.Count} products to sync");
                
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
                Debug.WriteLine($"[Sync] ✅ Synced {products.Count} products");
            }
            catch (Exception prodEx)
            {
                Debug.WriteLine($"[Sync] ⚠️ Error pulling products: {prodEx.Message}");
                Debug.WriteLine($"[Sync] Stack trace: {prodEx.StackTrace}");
                // Continue with other data even if products fail
            }
            
            // Pull inventory
            try
            {
                Debug.WriteLine("[Sync] 📦 Pulling inventory...");
                var inventory = await _onlineDb.GetInventoryItemsAsyncCached();
                Debug.WriteLine($"[Sync] Found {inventory.Count} inventory items to sync");
                
                foreach (var item in inventory)
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
                Debug.WriteLine($"[Sync] ✅ Synced {inventory.Count} inventory items");
            }
            catch (Exception invEx)
            {
                Debug.WriteLine($"[Sync] ⚠️ Error pulling inventory: {invEx.Message}");
                Debug.WriteLine($"[Sync] Stack trace: {invEx.StackTrace}");
                // Continue even if inventory fails
            }
            
            Debug.WriteLine($"[Sync] ✅ Pull completed");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Sync] ❌ Error pulling data: {ex.Message}");
            Debug.WriteLine($"[Sync] Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Debug.WriteLine($"[Sync] Inner exception: {ex.InnerException.Message}");
            }
            return false;
        }
    }
}

public class SyncResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int OperationsSynced { get; set; }
    public int OperationsFailed { get; set; }
}

public class SyncStatus
{
    public bool IsSyncing { get; set; }
    public int Progress { get; set; } // 0-100
    public string Message { get; set; } = string.Empty;
}

