using Coftea_Capstone.Views.Pages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Coftea_Capstone;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;
using Coftea_Capstone.Services;
using System.Collections.ObjectModel;
using System.Linq;
using Coftea_Capstone.Models;
using Coftea_Capstone.Services;
using System.IO;
using System.Text.Json;

namespace Coftea_Capstone.ViewModel
{
    public partial class SettingsPopUpViewModel : ObservableObject
    {
        // ===================== Dependencies & Services =====================
        private readonly AddItemToPOSViewModel _addItemToPOSViewModel;
        private readonly ManagePOSOptionsViewModel _managePOSOptionsViewModel;
        private readonly ManageInventoryOptionsViewModel _manageInventoryOptionsViewModel;

        // ===================== State =====================
        [ObservableProperty] private bool isSettingsPopupVisible = false;
        [ObservableProperty] private bool isAddItemToPOSVisible = false;
        [ObservableProperty] private bool isAddItemToInventoryVisible = false;
        
        [ObservableProperty] 
        private ObservableCollection<RecentOrderModel> recentOrders = new();

        [ObservableProperty]
        private int totalOrdersToday = 0;

        [ObservableProperty]
        private decimal totalSalesToday = 0m;

        // Top selling products for dashboard
        [ObservableProperty]
        private ObservableCollection<TrendItem> topSellingProductsToday = new();

        // Dashboard specific properties
        [ObservableProperty]
        private string mostBoughtToday = "No data";

        [ObservableProperty]
        private string trendingToday = "No data";

        [ObservableProperty]
        private string mostBoughtTrend = "";

        [ObservableProperty]
        private string trendingPercent = "";

        [ObservableProperty]
        private string ordersTrend = "";

        // Inventory alerts
        private ObservableCollection<string> _inventoryAlerts = new();
        public ObservableCollection<string> InventoryAlerts
        {
            get => _inventoryAlerts;
            set => SetProperty(ref _inventoryAlerts, value);
        }

        // Database IP setting - COMMENTED OUT
        // [ObservableProperty] private string databaseIP = string.Empty;

        public ManagePOSOptionsViewModel ManagePOSOptionsVM => _managePOSOptionsViewModel;
        public ManageInventoryOptionsViewModel ManageInventoryOptionsVM => _manageInventoryOptionsViewModel;

        // ===================== Initialization =====================
        public SettingsPopUpViewModel(AddItemToPOSViewModel addItemToPOSViewModel, ManagePOSOptionsViewModel managePOSOptionsViewModel, ManageInventoryOptionsViewModel manageInventoryOptionsViewModel)
        {
            _addItemToPOSViewModel = addItemToPOSViewModel;
            _managePOSOptionsViewModel = managePOSOptionsViewModel;
            _manageInventoryOptionsViewModel = manageInventoryOptionsViewModel;
            
            // Initialize with empty recent orders - will be populated from real data
            RecentOrders = new ObservableCollection<RecentOrderModel>();
        }

        public void AddRecentOrder(int orderNumber, string productName, string productImage, decimal totalAmount)
        {
            // Add new order to the top
            RecentOrders.Insert(0, new RecentOrderModel
            {
                OrderNumber = orderNumber,
                ProductName = productName,
                ProductImage = productImage,
                TotalAmount = totalAmount,
                OrderTime = DateTime.Now,
                Status = "Completed"
            });
            
            // Update today's metrics
            TotalOrdersToday++;
            TotalSalesToday += totalAmount;
            
            // Keep only the last 10 orders
            while (RecentOrders.Count > 10)
            {
                RecentOrders.RemoveAt(RecentOrders.Count - 1);
            }
        }

        // ===================== Data Loading =====================
        public async Task LoadTodaysMetricsAsync()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20)); // Increased timeout
                System.Diagnostics.Debug.WriteLine("🔄 Starting LoadTodaysMetricsAsync...");
                
                // Load top selling products for dashboard
                await LoadTopSellingProductsAsync();
                System.Diagnostics.Debug.WriteLine("✅ LoadTopSellingProductsAsync completed");
                
                var database = new Models.Database(); // Will use auto-detected host
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);
                
                var transactions = await database.GetTransactionsByDateRangeAsync(today, tomorrow);
                System.Diagnostics.Debug.WriteLine($"✅ Loaded {transactions.Count} transactions for today");
                
                TotalOrdersToday = transactions.Count;
                TotalSalesToday = transactions.Sum(t => t.Total);
                
                // Load recent orders
                await LoadRecentOrdersAsync(transactions);
                System.Diagnostics.Debug.WriteLine("✅ LoadRecentOrdersAsync completed");
                
                // Load most bought and trending items
                await LoadMostBoughtAndTrendingAsync(transactions);
                System.Diagnostics.Debug.WriteLine("✅ LoadMostBoughtAndTrendingAsync completed");
                
                // Load inventory alerts
                await LoadInventoryAlertsAsync();
                System.Diagnostics.Debug.WriteLine("✅ LoadInventoryAlertsAsync completed");
                
                // Set trend indicators
                OrdersTrend = TotalOrdersToday > 0 ? "↑ Today" : "No orders today";
                
                System.Diagnostics.Debug.WriteLine("✅ LoadTodaysMetricsAsync completed successfully");
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("⏰ LoadTodaysMetricsAsync timeout - this is normal on slow connections");
                // Keep default values (0) if loading times out
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ LoadTodaysMetricsAsync error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                // Keep default values (0) if loading fails
            }
        }

        // ===================== Commands =====================
        [RelayCommand]
        public void ShowSettingsPopup() 
        {
            // LoadCurrentDatabaseIP(); // COMMENTED OUT
            IsSettingsPopupVisible = true;
        }

        [RelayCommand]
        private void CloseSettingsPopup() => IsSettingsPopupVisible = false;

        [RelayCommand]
        private void OpenAddItemToPOS()
        {
            IsSettingsPopupVisible = false;
            _addItemToPOSViewModel.IsAddItemToPOSVisible = true;
        }

        [RelayCommand]
        private void OpenManageInventoryOptions()
        {
            System.Diagnostics.Debug.WriteLine("OpenManageInventoryOptions called");
            IsSettingsPopupVisible = false;
            if (!(App.CurrentUser?.IsAdmin ?? false))
            {
                Application.Current?.MainPage?.DisplayAlert("Unauthorized", "Only admins can manage Inventory.", "OK");
                return;
            }
            System.Diagnostics.Debug.WriteLine($"Setting ManageInventoryPopup visibility to true. Current value: {_manageInventoryOptionsViewModel.IsInventoryManagementPopupVisible}");
            _manageInventoryOptionsViewModel.IsInventoryManagementPopupVisible = true;
            System.Diagnostics.Debug.WriteLine($"ManageInventoryPopup visibility set to: {_manageInventoryOptionsViewModel.IsInventoryManagementPopupVisible}");
        }

        [RelayCommand]
        private void OpenAddItemToInventory()
        {
            IsSettingsPopupVisible = false;
            IsAddItemToInventoryVisible = true;
        }

        [RelayCommand]
        private void OpenManagePOSOptions()
        {
            System.Diagnostics.Debug.WriteLine("OpenManagePOSOptions called");
            IsSettingsPopupVisible = false;
            if (!(App.CurrentUser?.IsAdmin ?? false))
            {
                Application.Current?.MainPage?.DisplayAlert("Unauthorized", "Only admins can manage POS settings.", "OK");
                return;
            }
            System.Diagnostics.Debug.WriteLine($"Setting ManagePOSPopup visibility to true. Current value: {_managePOSOptionsViewModel.IsPOSManagementPopupVisible}");
            _managePOSOptionsViewModel.IsPOSManagementPopupVisible = true;
            System.Diagnostics.Debug.WriteLine($"ManagePOSPopup visibility set to: {_managePOSOptionsViewModel.IsPOSManagementPopupVisible}");
        }

        [RelayCommand]
        private async void OpenUserManagement()
        {
            IsSettingsPopupVisible = false;
            if (!(App.CurrentUser?.IsAdmin ?? false))
            {
                await Application.Current.MainPage.DisplayAlert("Unauthorized", "Only admins can access User Management.", "OK");
                return;
            }
            if (Application.Current?.MainPage is NavigationPage nav)
            {
                await nav.PushWithAnimationAsync(new UserManagement(), false);
            }
        }

        [RelayCommand]
        private void CloseAddItemToInventory()
        {
            IsSettingsPopupVisible = false;
            IsAddItemToInventoryVisible = false;
        }

        [RelayCommand]
        private void Logout()
        {
            if (Application.Current is App app)
            {
                app.ResetAppAfterLogout(); // now it works
            }
        }

        // COMMENTED OUT - Database IP functionality
        /*
        [RelayCommand]
        private void SaveDatabaseIP()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(DatabaseIP))
                {
                    Application.Current.MainPage.DisplayAlert("Error", "Please enter a valid IP address", "OK");
                    return;
                }

                // Validate IP format (basic validation)
                if (!IsValidIPAddress(DatabaseIP))
                {
                    Application.Current.MainPage.DisplayAlert("Error", "Please enter a valid IP address format (e.g., 192.168.1.100)", "OK");
                    return;
                }

                // Set the database host
                NetworkConfigurationService.SetManualDatabaseHost(DatabaseIP.Trim());
                
                System.Diagnostics.Debug.WriteLine($"🔧 Database IP set to: {DatabaseIP}");
                Application.Current.MainPage.DisplayAlert("Success", $"Database IP set to {DatabaseIP}\n\nYou may need to restart the app for changes to take effect.", "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error setting database IP: {ex.Message}");
                Application.Current.MainPage.DisplayAlert("Error", $"Failed to set database IP: {ex.Message}", "OK");
            }
        }
        */

        // COMMENTED OUT - Database IP functionality
        /*
        private void LoadCurrentDatabaseIP()
        {
            try
            {
                var currentSettings = NetworkConfigurationService.GetCurrentSettings();
                if (currentSettings.ContainsKey("Database Host"))
                {
                    DatabaseIP = currentSettings["Database Host"];
                }
                else
                {
                    DatabaseIP = string.Empty;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading current database IP: {ex.Message}");
                DatabaseIP = string.Empty;
            }
        }
        */

        // COMMENTED OUT - Database IP functionality
        /*
        private bool IsValidIPAddress(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip)) return false;
            
            var parts = ip.Split('.');
            if (parts.Length != 4) return false;
            
            foreach (var part in parts)
            {
                if (!int.TryParse(part, out int num) || num < 0 || num > 255)
                    return false;
            }
            
            return true;
        }
        */

        // ===================== Backup =====================
        [RelayCommand]
        private async Task CreateBackup()
        {
            try
            {
                var database = new Models.Database();

                // Fetch data
                var products = await database.GetProductsAsync();
                var inventoryItems = await database.GetInventoryItemsAsync();
                var transactions = await database.GetTransactionsByDateRangeAsync(DateTime.MinValue, DateTime.MaxValue);

                var backupData = new
                {
                    Timestamp = DateTime.Now,
                    Products = products,
                    InventoryItems = inventoryItems,
                    Transactions = transactions
                };

                var json = JsonSerializer.Serialize(backupData, new JsonSerializerOptions { WriteIndented = true });

                var fileName = $"database_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                var filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);
                await File.WriteAllTextAsync(filePath, json);

                await Application.Current.MainPage.DisplayAlert("Backup Created",
                    $"Saved to:\n{filePath}\n\nProducts: {products.Count}\nInventory Items: {inventoryItems.Count}\nTransactions: {transactions.Count}",
                    "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Backup Failed", ex.Message, "OK");
            }
        }

        // COMMENTED OUT - Database backup functionality
        /*
        [RelayCommand]
        private async Task RestoreBackup()
        {
            try
            {
                // Get all backup files
                var backupFiles = Directory.GetFiles(FileSystem.AppDataDirectory, "database_backup_*.json")
                    .OrderByDescending(f => File.GetCreationTime(f))
                    .ToList();
                
                if (backupFiles.Count == 0)
                {
                    await Application.Current.MainPage.DisplayAlert("No Backups", "No backup files found. Please create a backup first.", "OK");
                    return;
                }
                
                // Show backup selection
                var fileNames = backupFiles.Select(f => Path.GetFileName(f)).ToArray();
                var selectedFile = await Application.Current.MainPage.DisplayActionSheet("Select Backup to Restore", "Cancel", null, fileNames);
                
                if (selectedFile == "Cancel" || string.IsNullOrEmpty(selectedFile))
                    return;
                
                var selectedFilePath = backupFiles.First(f => Path.GetFileName(f) == selectedFile);
                
                // Confirm restore
                var confirm = await Application.Current.MainPage.DisplayAlert("Confirm Restore", 
                    $"Are you sure you want to restore from {selectedFile}?\n\nThis will overwrite all current data!", "Yes", "No");
                
                if (!confirm) return;
                
                // Read backup file
                var json = await File.ReadAllTextAsync(selectedFilePath);
                var backupData = System.Text.Json.JsonSerializer.Deserialize<dynamic>(json);
                
                // Show loading message
                await Application.Current.MainPage.DisplayAlert("Restoring Backup", "Please wait while we restore your database...", "OK");
                
                // Note: Full restore would require implementing restore methods in Database.cs
                // For now, just show success message
                System.Diagnostics.Debug.WriteLine($"🔄 Restore initiated from: {selectedFilePath}");
                await Application.Current.MainPage.DisplayAlert("Restore Initiated", 
                    $"Backup restore initiated from {selectedFile}.\n\nNote: Full restore functionality needs to be implemented in the Database class.", "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error restoring backup: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Restore Failed", $"Failed to restore backup: {ex.Message}", "OK");
            }
        }
        */

        // ===================== Helpers =====================
        private async Task LoadTopSellingProductsAsync()
        {
            try
            {
                var database = new Models.Database(); // Will use auto-detected host
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);
                
                var transactions = await database.GetTransactionsByDateRangeAsync(today, tomorrow);
                
                // Group by product name and count sales
                var productSales = transactions
                    .GroupBy(t => t.DrinkName)
                    .Select(group => new TrendItem
                    {
                        Name = group.Key,
                        Count = group.Sum(t => t.Quantity)
                    })
                    .OrderByDescending(item => item.Count)
                    .Take(5)
                    .ToList();

                // Set MaxCount for proper scaling (like in sales report)
                if (productSales.Count > 0)
                {
                    int maxCount = productSales.Max(x => x.Count);
                    if (maxCount <= 0) maxCount = 1; // Prevent division by zero
                    
                    foreach (var item in productSales)
                    {
                        item.MaxCount = maxCount;
                    }
                }

                TopSellingProductsToday = new ObservableCollection<TrendItem>(productSales);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load top selling products: {ex.Message}");
                // Initialize with empty collection on error
                TopSellingProductsToday = new ObservableCollection<TrendItem>();
            }
        }

        private async Task LoadRecentOrdersAsync(List<TransactionHistoryModel> transactions)
        {
            try
            {
                RecentOrders.Clear();
                
                // Get the most recent 5 transactions
                var recentTransactions = transactions
                    .OrderByDescending(t => t.TransactionDate)
                    .Take(5)
                    .ToList();

                var database = new Models.Database();
                foreach (var transaction in recentTransactions)
                {
                    // Try to get the actual product image from the products table
                    string productImage = "drink.png"; // default fallback
                    try
                    {
                        var product = await database.GetProductByNameAsync(transaction.DrinkName);
                        if (product != null && !string.IsNullOrWhiteSpace(product.ImageSet))
                        {
                            productImage = product.ImageSet;
                        }
                    }
                    catch { /* ignore and use default */ }

                    RecentOrders.Add(new RecentOrderModel
                    {
                        OrderNumber = transaction.TransactionId,
                        ProductName = transaction.DrinkName,
                        ProductImage = productImage,
                        TotalAmount = transaction.Total,
                        OrderTime = transaction.TransactionDate,
                        Status = "Completed"
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load recent orders: {ex.Message}");
            }
        }

        private async Task LoadMostBoughtAndTrendingAsync(List<TransactionHistoryModel> transactions)
        {
            try
            {
                if (transactions.Count == 0)
                {
                    MostBoughtToday = "No data";
                    TrendingToday = "No data";
                    MostBoughtTrend = "";
                    TrendingPercent = "";
                    return;
                }

                // Group by product and calculate total quantity sold
                var productSales = transactions
                    .GroupBy(t => t.DrinkName)
                    .Select(group => new
                    {
                        Name = group.Key,
                        TotalQuantity = group.Sum(t => t.Quantity),
                        TransactionCount = group.Count()
                    })
                    .OrderByDescending(x => x.TotalQuantity)
                    .ToList();

                if (productSales.Count > 0)
                {
                    // Most bought today (highest quantity)
                    var mostBought = productSales.First();
                    MostBoughtToday = mostBought.Name;
                    MostBoughtTrend = $"{mostBought.TotalQuantity} sold";

                    // Trending today (most transactions, not necessarily highest quantity)
                    var trending = productSales.OrderByDescending(x => x.TransactionCount).First();
                    TrendingToday = trending.Name;
                    TrendingPercent = $"{trending.TransactionCount} orders";
                }
                else
                {
                    MostBoughtToday = "No data";
                    TrendingToday = "No data";
                    MostBoughtTrend = "";
                    TrendingPercent = "";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load most bought and trending: {ex.Message}");
                MostBoughtToday = "Error";
                TrendingToday = "Error";
                MostBoughtTrend = "";
                TrendingPercent = "";
            }
        }

        private async Task LoadInventoryAlertsAsync()
        {
            try
            {
                InventoryAlerts.Clear();
                
                var database = new Models.Database(); // Will use auto-detected host
                var inventoryItems = await database.GetInventoryItemsAsync();
                
                System.Diagnostics.Debug.WriteLine($"🔍 LoadInventoryAlertsAsync: Found {inventoryItems.Count} inventory items");
                
                // Check for duplicate item names
                var duplicateNames = inventoryItems
                    .GroupBy(item => item.itemName)
                    .Where(group => group.Count() > 1)
                    .Select(group => group.Key)
                    .ToList();
                
                if (duplicateNames.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Found duplicate item names: {string.Join(", ", duplicateNames)}");
                }
                
                // Compute percent of minimum threshold remaining
                // percentOfMin = (current / minimum) * 100; if minimum is 0, skip (treated as healthy)
                var lowStockItems = inventoryItems
                    .Where(item => item.minimumQuantity > 0)
                    .Select(item => new {
                        Item = item,
                        PercentOfMin = (item.itemQuantity / item.minimumQuantity) * 100.0
                    })
                    .Where(x => x.PercentOfMin <= 100.0) // at or below minimum threshold
                    .GroupBy(x => x.Item.itemName)
                    .Select(group => group.OrderBy(x => x.PercentOfMin).First())
                    .OrderBy(x => x.PercentOfMin)
                    .Take(5)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"🔍 LoadInventoryAlertsAsync: Found {lowStockItems.Count} low stock items");

                if (lowStockItems.Count == 0)
                {
                    InventoryAlerts.Add("✅ All items well stocked");
                }
                else
                {
                    foreach (var entry in lowStockItems)
                    {
                        var item = entry.Item;
                        var percent = entry.PercentOfMin;
                        // Severity by percent of minimum
                        // <= 30% => CRITICAL, 31-70% => MEDIUM, 71-100% => LOW
                        string stockLevel = percent <= 30.0 ? "CRITICAL" : (percent <= 70.0 ? "MEDIUM" : "LOW");

                        var alertText = $"{stockLevel}: {item.itemName} ({item.itemQuantity:F1} {item.unitOfMeasurement}) - {percent:F0}% of min";
                        System.Diagnostics.Debug.WriteLine($"🔍 Adding alert: {alertText}");
                        InventoryAlerts.Add(alertText);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load inventory alerts: {ex.Message}");
                InventoryAlerts.Add("❌ Error loading inventory data");
            }
        }
    }
}
