using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using Coftea_Capstone.Models;
using Coftea_Capstone.C_;

namespace Coftea_Capstone.Services
{
    public class OfflineQueueService
    {
        private const string QueueDirectory = "offline_queue";
        private const string TransactionsFile = "pending_transactions.json";
        private const string ProductsFile = "pending_products.json";
        private const string InventoryFile = "pending_inventory.json";
        private const string UsersFile = "pending_users.json";
        private const string InventoryDeductionsFile = "pending_deductions.json";

        private static string GetQueuePath(string fileName)
        {
            var basePath = FileSystem.AppDataDirectory;
            var queuePath = Path.Combine(basePath, QueueDirectory);
            Directory.CreateDirectory(queuePath);
            return Path.Combine(queuePath, fileName);
        }

        // ==================== TRANSACTIONS ====================
        public async Task QueueTransactionAsync(TransactionHistoryModel transaction)
        {
            try
            {
                var path = GetQueuePath(TransactionsFile);
                var transactions = await LoadPendingTransactionsAsync();
                transactions.Add(transaction);
                var json = JsonSerializer.Serialize(transactions, new JsonSerializerOptions { WriteIndented = false });
                await File.WriteAllTextAsync(path, json);
                System.Diagnostics.Debug.WriteLine($"📦 Queued transaction offline: {transaction.TransactionId}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error queueing transaction: {ex.Message}");
            }
        }

        public async Task<List<TransactionHistoryModel>> LoadPendingTransactionsAsync()
        {
            try
            {
                var path = GetQueuePath(TransactionsFile);
                if (!File.Exists(path)) return new List<TransactionHistoryModel>();
                
                var json = await File.ReadAllTextAsync(path);
                var transactions = JsonSerializer.Deserialize<List<TransactionHistoryModel>>(json) ?? new List<TransactionHistoryModel>();
                return transactions;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading pending transactions: {ex.Message}");
                return new List<TransactionHistoryModel>();
            }
        }

        public async Task ClearTransactionsAsync()
        {
            try
            {
                var path = GetQueuePath(TransactionsFile);
                if (File.Exists(path)) File.Delete(path);
                System.Diagnostics.Debug.WriteLine("✅ Cleared pending transactions");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error clearing transactions: {ex.Message}");
            }
        }

        // ==================== PRODUCTS ====================
        public async Task QueueProductAsync(POSPageModel product)
        {
            try
            {
                var path = GetQueuePath(ProductsFile);
                var products = await LoadPendingProductsAsync();
                
                // Add unique identifier if not present
                if (products.Any(p => p.ProductID == product.ProductID))
                {
                    // Update existing product
                    var index = products.FindIndex(p => p.ProductID == product.ProductID);
                    products[index] = product;
                }
                else
                {
                    products.Add(product);
                }
                
                var json = JsonSerializer.Serialize(products, new JsonSerializerOptions { WriteIndented = false });
                await File.WriteAllTextAsync(path, json);
                System.Diagnostics.Debug.WriteLine($"📦 Queued product offline: {product.ProductName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error queueing product: {ex.Message}");
            }
        }

        public async Task<List<POSPageModel>> LoadPendingProductsAsync()
        {
            try
            {
                var path = GetQueuePath(ProductsFile);
                if (!File.Exists(path)) return new List<POSPageModel>();
                
                var json = await File.ReadAllTextAsync(path);
                var products = JsonSerializer.Deserialize<List<POSPageModel>>(json) ?? new List<POSPageModel>();
                return products;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading pending products: {ex.Message}");
                return new List<POSPageModel>();
            }
        }

        public async Task ClearProductsAsync()
        {
            try
            {
                var path = GetQueuePath(ProductsFile);
                if (File.Exists(path)) File.Delete(path);
                System.Diagnostics.Debug.WriteLine("✅ Cleared pending products");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error clearing products: {ex.Message}");
            }
        }

        // ==================== INVENTORY ====================
        public async Task QueueInventoryItemAsync(InventoryPageModel item)
        {
            try
            {
                var path = GetQueuePath(InventoryFile);
                var items = await LoadPendingInventoryAsync();
                
                // Add or update
                var index = items.FindIndex(i => i.itemID == item.itemID);
                if (index >= 0)
                {
                    items[index] = item;
                }
                else
                {
                    items.Add(item);
                }
                
                var json = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = false });
                await File.WriteAllTextAsync(path, json);
                System.Diagnostics.Debug.WriteLine($"📦 Queued inventory item offline: {item.itemName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error queueing inventory: {ex.Message}");
            }
        }

        public async Task<List<InventoryPageModel>> LoadPendingInventoryAsync()
        {
            try
            {
                var path = GetQueuePath(InventoryFile);
                if (!File.Exists(path)) return new List<InventoryPageModel>();
                
                var json = await File.ReadAllTextAsync(path);
                var items = JsonSerializer.Deserialize<List<InventoryPageModel>>(json) ?? new List<InventoryPageModel>();
                return items;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading pending inventory: {ex.Message}");
                return new List<InventoryPageModel>();
            }
        }

        public async Task ClearInventoryAsync()
        {
            try
            {
                var path = GetQueuePath(InventoryFile);
                if (File.Exists(path)) File.Delete(path);
                System.Diagnostics.Debug.WriteLine("✅ Cleared pending inventory");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error clearing inventory: {ex.Message}");
            }
        }

        // ==================== INVENTORY DEDUCTIONS ====================
        public async Task QueueInventoryDeductionAsync(string productName, List<(string name, double amount, string unit)> deductions)
        {
            try
            {
                var path = GetQueuePath(InventoryDeductionsFile);
                var pending = await LoadPendingDeductionsAsync();
                
                pending.Add(new PendingDeduction
                {
                    ProductName = productName,
                    Deductions = deductions.Select(d => new DeductionItem
                    {
                        ItemName = d.name,
                        Amount = d.amount,
                        Unit = d.unit
                    }).ToList(),
                    Timestamp = DateTime.Now
                });
                
                var json = JsonSerializer.Serialize(pending, new JsonSerializerOptions { WriteIndented = false });
                await File.WriteAllTextAsync(path, json);
                System.Diagnostics.Debug.WriteLine($"📦 Queued inventory deduction offline for: {productName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error queueing deduction: {ex.Message}");
            }
        }

        public async Task<List<PendingDeduction>> LoadPendingDeductionsAsync()
        {
            try
            {
                var path = GetQueuePath(InventoryDeductionsFile);
                if (!File.Exists(path)) return new List<PendingDeduction>();
                
                var json = await File.ReadAllTextAsync(path);
                var deductions = JsonSerializer.Deserialize<List<PendingDeduction>>(json) ?? new List<PendingDeduction>();
                return deductions;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading pending deductions: {ex.Message}");
                return new List<PendingDeduction>();
            }
        }

        public async Task ClearDeductionsAsync()
        {
            try
            {
                var path = GetQueuePath(InventoryDeductionsFile);
                if (File.Exists(path)) File.Delete(path);
                System.Diagnostics.Debug.WriteLine("✅ Cleared pending deductions");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error clearing deductions: {ex.Message}");
            }
        }

        // ==================== USERS ====================
        public async Task QueueUserAsync(UserInfoModel user)
        {
            try
            {
                var path = GetQueuePath(UsersFile);
                var users = await LoadPendingUsersAsync();
                
                var index = users.FindIndex(u => u.Email == user.Email);
                if (index >= 0)
                {
                    users[index] = user;
                }
                else
                {
                    users.Add(user);
                }
                
                var json = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = false });
                await File.WriteAllTextAsync(path, json);
                System.Diagnostics.Debug.WriteLine($"📦 Queued user offline: {user.Email}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error queueing user: {ex.Message}");
            }
        }

        public async Task<List<UserInfoModel>> LoadPendingUsersAsync()
        {
            try
            {
                var path = GetQueuePath(UsersFile);
                if (!File.Exists(path)) return new List<UserInfoModel>();
                
                var json = await File.ReadAllTextAsync(path);
                var users = JsonSerializer.Deserialize<List<UserInfoModel>>(json) ?? new List<UserInfoModel>();
                return users;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading pending users: {ex.Message}");
                return new List<UserInfoModel>();
            }
        }

        public async Task ClearUsersAsync()
        {
            try
            {
                var path = GetQueuePath(UsersFile);
                if (File.Exists(path)) File.Delete(path);
                System.Diagnostics.Debug.WriteLine("✅ Cleared pending users");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error clearing users: {ex.Message}");
            }
        }

        // ==================== SYNC ====================
        public async Task<int> SyncPendingOperationsAsync()
        {
            if (!NetworkService.HasInternetConnection())
            {
                System.Diagnostics.Debug.WriteLine("⚠️ Cannot sync: No internet connection");
                return 0;
            }

            var database = new Database();
            int syncedCount = 0;

            try
            {
                // Sync transactions
                var transactions = await LoadPendingTransactionsAsync();
                foreach (var transaction in transactions)
                {
                    try
                    {
                        await database.SaveTransactionAsync(transaction);
                        syncedCount++;
                        System.Diagnostics.Debug.WriteLine($"✅ Synced transaction: {transaction.TransactionId}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Failed to sync transaction {transaction.TransactionId}: {ex.Message}");
                    }
                }
                if (transactions.Count > 0) await ClearTransactionsAsync();

                // Sync inventory deductions
                var deductions = await LoadPendingDeductionsAsync();
                foreach (var deduction in deductions)
                {
                    try
                    {
                        // Convert to the format expected by DeductInventoryAsync: (string name, double amount)
                        var deductionList = deduction.Deductions.Select(d => (name: d.ItemName, amount: d.Amount)).ToList();
                        await database.DeductInventoryAsync(deductionList, deduction.ProductName);
                        syncedCount++;
                        System.Diagnostics.Debug.WriteLine($"✅ Synced deduction for: {deduction.ProductName}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Failed to sync deduction: {ex.Message}");
                    }
                }
                if (deductions.Count > 0) await ClearDeductionsAsync();

                // Sync products (partial - just log for now, full product sync needs more context)
                var products = await LoadPendingProductsAsync();
                System.Diagnostics.Debug.WriteLine($"📦 {products.Count} products pending sync (requires manual review)");

                // Sync inventory items (partial - just log for now, full inventory sync needs more context)
                var inventory = await LoadPendingInventoryAsync();
                System.Diagnostics.Debug.WriteLine($"📦 {inventory.Count} inventory items pending sync (requires manual review)");

                // Sync users (partial - just log for now, full user sync needs more context)
                var users = await LoadPendingUsersAsync();
                System.Diagnostics.Debug.WriteLine($"📦 {users.Count} users pending sync (requires manual review)");

                System.Diagnostics.Debug.WriteLine($"✅ Sync complete: {syncedCount} operations synced");
                return syncedCount;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error during sync: {ex.Message}");
                return syncedCount;
            }
        }

        public async Task<int> GetPendingOperationsCountAsync()
        {
            var transactions = await LoadPendingTransactionsAsync();
            var deductions = await LoadPendingDeductionsAsync();
            return transactions.Count + deductions.Count;
        }
    }

    // Helper classes for JSON serialization
    public class PendingDeduction
    {
        public string ProductName { get; set; }
        public List<DeductionItem> Deductions { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class DeductionItem
    {
        public string ItemName { get; set; }
        public double Amount { get; set; }
        public string Unit { get; set; }
    }
}

