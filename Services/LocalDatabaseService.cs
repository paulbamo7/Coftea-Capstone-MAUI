using SQLite;
using System.Diagnostics;
using Coftea_Capstone.Models;

namespace Coftea_Capstone.Services;

/// <summary>
/// Local SQLite database for offline operations
/// All data is stored locally and synced to MySQL when online
/// </summary>
public class LocalDatabaseService
{
    private SQLiteAsyncConnection? _database;
    private readonly string _dbPath;
    
    public LocalDatabaseService()
    {
        _dbPath = Path.Combine(FileSystem.AppDataDirectory, "coftea_local.db");
        Debug.WriteLine($"[LocalDB] Database path: {_dbPath}");
    }
    
    private async Task InitAsync()
    {
        if (_database != null)
            return;
        
        _database = new SQLiteAsyncConnection(_dbPath);
        
        // Create all tables for offline storage
        await _database.CreateTableAsync<LocalUser>();
        await _database.CreateTableAsync<LocalProduct>();
        await _database.CreateTableAsync<LocalInventoryItem>();
        await _database.CreateTableAsync<LocalTransaction>();
        await _database.CreateTableAsync<LocalProductIngredient>();
        await _database.CreateTableAsync<LocalProductAddon>();
        await _database.CreateTableAsync<LocalInventoryActivityLog>();
        await _database.CreateTableAsync<PendingOperation>();
        
        Debug.WriteLine("[LocalDB] ✅ Database initialized with all tables");
    }
    
    // ==================== PENDING OPERATIONS ====================
    
    /// <summary>
    /// Add an operation to the queue for later sync
    /// </summary>
    public async Task QueueOperationAsync(string operationType, string tableName, string dataJson)
    {
        await InitAsync();
        
        // Check if a similar pending operation already exists (deduplication)
        var existingPending = await _database!.Table<PendingOperation>()
            .Where(o => !o.IsSynced && 
                       o.OperationType == operationType && 
                       o.TableName == tableName)
            .CountAsync();
        
        if (existingPending > 0)
        {
            Debug.WriteLine($"[LocalDB] ⏭️ Similar pending operation exists, updating instead of duplicating");
            // Update the existing operation's data to the latest
            var existing = await _database.Table<PendingOperation>()
                .Where(o => !o.IsSynced && 
                           o.OperationType == operationType && 
                           o.TableName == tableName)
                .FirstOrDefaultAsync();
            
            if (existing != null)
            {
                existing.DataJson = dataJson;
                existing.CreatedAt = DateTime.Now; // Update timestamp to latest
                await _database.UpdateAsync(existing);
                Debug.WriteLine($"[LocalDB] ✏️ Updated existing operation: {operationType} on {tableName}");
                return;
            }
        }
        
        var operation = new PendingOperation
        {
            OperationType = operationType,
            TableName = tableName,
            DataJson = dataJson,
            CreatedAt = DateTime.Now,
            IsSynced = false
        };
        
        await _database!.InsertAsync(operation);
        Debug.WriteLine($"[LocalDB] ⏳ Queued NEW operation: {operationType} on {tableName}");
    }
    
    /// <summary>
    /// Get all pending operations that need to be synced
    /// </summary>
    public async Task<List<PendingOperation>> GetPendingOperationsAsync()
    {
        await InitAsync();
        return await _database!.Table<PendingOperation>()
            .Where(o => !o.IsSynced)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync();
    }
    
    /// <summary>
    /// Mark an operation as synced
    /// </summary>
    public async Task MarkOperationSyncedAsync(int operationId)
    {
        await InitAsync();
        var operation = await _database!.FindAsync<PendingOperation>(operationId);
        if (operation != null)
        {
            operation.IsSynced = true;
            operation.SyncedAt = DateTime.Now;
            await _database.UpdateAsync(operation);
            Debug.WriteLine($"[LocalDB] ✅ Operation {operationId} marked as synced");
        }
    }
    
    /// <summary>
    /// Clear old synced operations (cleanup)
    /// </summary>
    public async Task<int> ClearSyncedOperationsAsync()
    {
        await InitAsync();
        var deleted = await _database!.ExecuteAsync(
            "DELETE FROM PendingOperation WHERE IsSynced = 1 AND SyncedAt < ?",
            DateTime.Now.AddDays(-7) // Keep last 7 days
        );
        Debug.WriteLine($"[LocalDB] 🗑️ Cleared {deleted} old synced operations");
        return deleted;
    }
    
    // ==================== USERS ====================
    
    public async Task<List<LocalUser>> GetAllUsersAsync()
    {
        await InitAsync();
        return await _database!.Table<LocalUser>().ToListAsync();
    }
    
    public async Task<LocalUser?> GetUserByIdAsync(int userId)
    {
        await InitAsync();
        return await _database!.Table<LocalUser>()
            .Where(u => u.id == userId)
            .FirstOrDefaultAsync();
    }
    
    public async Task<LocalUser?> GetUserByUsernameAsync(string username)
    {
        await InitAsync();
        return await _database!.Table<LocalUser>()
            .Where(u => u.username == username)
            .FirstOrDefaultAsync();
    }
    
    public async Task SaveUserAsync(LocalUser user)
    {
        await InitAsync();
        if (user.id == 0)
            await _database!.InsertAsync(user);
        else
            await _database!.InsertOrReplaceAsync(user);
    }
    
    // ==================== PRODUCTS ====================
    
    public async Task<List<LocalProduct>> GetAllProductsAsync()
    {
        await InitAsync();
        return await _database!.Table<LocalProduct>().ToListAsync();
    }
    
    public async Task<LocalProduct?> GetProductByIdAsync(int productId)
    {
        await InitAsync();
        return await _database!.Table<LocalProduct>()
            .Where(p => p.productID == productId)
            .FirstOrDefaultAsync();
    }
    
    public async Task SaveProductAsync(LocalProduct product)
    {
        await InitAsync();
        await _database!.InsertOrReplaceAsync(product);
    }
    
    // ==================== INVENTORY ====================
    
    public async Task<List<LocalInventoryItem>> GetAllInventoryAsync()
    {
        await InitAsync();
        return await _database!.Table<LocalInventoryItem>().ToListAsync();
    }
    
    public async Task<LocalInventoryItem?> GetInventoryByIdAsync(int inventoryId)
    {
        await InitAsync();
        return await _database!.Table<LocalInventoryItem>()
            .Where(i => i.inventoryItemID == inventoryId)
            .FirstOrDefaultAsync();
    }
    
    public async Task SaveInventoryAsync(LocalInventoryItem item)
    {
        await InitAsync();
        await _database!.InsertOrReplaceAsync(item);
    }
    
    public async Task UpdateInventoryQuantityAsync(int inventoryId, double newQuantity)
    {
        await InitAsync();
        var item = await GetInventoryByIdAsync(inventoryId);
        if (item != null)
        {
            item.currentQuantity = newQuantity;
            item.lastUpdated = DateTime.Now;
            await _database!.UpdateAsync(item);
        }
    }
    
    // ==================== TRANSACTIONS ====================
    
    public async Task SaveTransactionAsync(LocalTransaction transaction)
    {
        await InitAsync();
        await _database!.InsertAsync(transaction);
        Debug.WriteLine($"[LocalDB] 💰 Transaction saved locally: ₱{transaction.totalAmount}");
    }
    
    public async Task<List<LocalTransaction>> GetAllTransactionsAsync()
    {
        await InitAsync();
        return await _database!.Table<LocalTransaction>()
            .OrderByDescending(t => t.transactionDate)
            .ToListAsync();
    }
    
    // ==================== SYNC STATUS ====================
    
    public async Task<int> GetPendingOperationsCountAsync()
    {
        await InitAsync();
        return await _database!.Table<PendingOperation>()
            .Where(o => !o.IsSynced)
            .CountAsync();
    }
}

// ==================== LOCAL DATABASE MODELS ====================

[Table("Users")]
public class LocalUser
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }
    public string username { get; set; } = string.Empty;
    public string hashedPassword { get; set; } = string.Empty;
    public string firstName { get; set; } = string.Empty;
    public string lastName { get; set; } = string.Empty;
    public string contactNumber { get; set; } = string.Empty;
    public string role { get; set; } = string.Empty;
    public DateTime? createdAt { get; set; }
}

[Table("Products")]
public class LocalProduct
{
    [PrimaryKey, AutoIncrement]
    public int productID { get; set; }
    public string productName { get; set; } = string.Empty;
    public decimal? smallPrice { get; set; }
    public decimal mediumPrice { get; set; }
    public decimal largePrice { get; set; }
    public string category { get; set; } = string.Empty;
    public string? subcategory { get; set; }
    public string imageSet { get; set; } = string.Empty;
    public string? description { get; set; }
    public string? colorCode { get; set; }
}

[Table("InventoryItems")]
public class LocalInventoryItem
{
    [PrimaryKey, AutoIncrement]
    public int inventoryItemID { get; set; }
    public string itemName { get; set; } = string.Empty;
    public string category { get; set; } = string.Empty;
    public double currentQuantity { get; set; }
    public double minimumQuantity { get; set; }
    public double maximumQuantity { get; set; }
    public string unit { get; set; } = string.Empty;
    public double reorderPoint { get; set; }
    public DateTime? lastUpdated { get; set; }
    public string? supplierName { get; set; }
    public string? supplierContact { get; set; }
}

[Table("Transactions")]
public class LocalTransaction
{
    [PrimaryKey, AutoIncrement]
    public int transactionID { get; set; }
    public int? userId { get; set; }
    public decimal totalAmount { get; set; }
    public string paymentMethod { get; set; } = string.Empty;
    public DateTime transactionDate { get; set; }
    public string? orderDetails { get; set; } // JSON string
}

[Table("ProductIngredients")]
public class LocalProductIngredient
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }
    public int productId { get; set; }
    public int inventoryItemId { get; set; }
    public double? smallAmount { get; set; }
    public string? smallUnit { get; set; }
    public double? mediumAmount { get; set; }
    public string? mediumUnit { get; set; }
    public double? largeAmount { get; set; }
    public string? largeUnit { get; set; }
}

[Table("ProductAddons")]
public class LocalProductAddon
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }
    public int productId { get; set; }
    public int inventoryItemId { get; set; }
    public double deductionAmount { get; set; }
    public string deductionUnit { get; set; } = string.Empty;
}

[Table("InventoryActivityLog")]
public class LocalInventoryActivityLog
{
    [PrimaryKey, AutoIncrement]
    public int logID { get; set; }
    public int? userId { get; set; }
    public int? inventoryItemID { get; set; }
    public string actionType { get; set; } = string.Empty;
    public double quantityChanged { get; set; }
    public string? reasonOrDetails { get; set; }
    public DateTime actionDate { get; set; }
}

[Table("PendingOperations")]
public class PendingOperation
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string OperationType { get; set; } = string.Empty; // INSERT, UPDATE, DELETE
    public string TableName { get; set; } = string.Empty;
    public string DataJson { get; set; } = string.Empty; // JSON representation of data
    public DateTime CreatedAt { get; set; }
    public bool IsSynced { get; set; }
    public DateTime? SyncedAt { get; set; }
}

