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

namespace Coftea_Capstone.ViewModel
{
    public partial class SettingsPopUpViewModel : ObservableObject
    {
        private readonly AddItemToPOSViewModel _addItemToPOSViewModel;
        private readonly ManagePOSOptionsViewModel _managePOSOptionsViewModel;
        private readonly ManageInventoryOptionsViewModel _manageInventoryOptionsViewModel;

        [ObservableProperty] private bool isSettingsPopupVisible = false;
        [ObservableProperty] private bool isAddItemToPOSVisible = false;
        [ObservableProperty] private bool isAddItemToInventoryVisible = false;
        
        // Sync properties
        [ObservableProperty] private bool isSyncing = false;
        [ObservableProperty] private string syncButtonText = "Sync Database";
        
        public bool IsNotSyncing => !IsSyncing;
        
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

        public ManagePOSOptionsViewModel ManagePOSOptionsVM => _managePOSOptionsViewModel;
        public ManageInventoryOptionsViewModel ManageInventoryOptionsVM => _manageInventoryOptionsViewModel;

        // Permission check for Manage Inventory visibility
        public bool CanManageInventory => (App.CurrentUser?.IsAdmin ?? false) || (App.CurrentUser?.CanAccessInventory ?? false);
        
        // Permission check for Manage POS visibility
        public bool CanManagePOS => (App.CurrentUser?.IsAdmin ?? false) || (App.CurrentUser?.CanAccessPOS ?? false);

        public SettingsPopUpViewModel(AddItemToPOSViewModel addItemToPOSViewModel, ManagePOSOptionsViewModel managePOSOptionsViewModel, ManageInventoryOptionsViewModel manageInventoryOptionsViewModel)
        {
            _addItemToPOSViewModel = addItemToPOSViewModel;
            _managePOSOptionsViewModel = managePOSOptionsViewModel;
            _manageInventoryOptionsViewModel = manageInventoryOptionsViewModel;
            
            // Initialize with empty recent orders - will be populated from real data
            RecentOrders = new ObservableCollection<RecentOrderModel>();
            
            // Subscribe to inventory changes to refresh alerts
            MessagingCenter.Subscribe<AddItemToInventoryViewModel>(this, "InventoryChanged", async (sender) =>
            {
                System.Diagnostics.Debug.WriteLine("🔄 InventoryChanged message received - refreshing inventory alerts");
                // Small delay to ensure database is updated
                await Task.Delay(300);
                await LoadInventoryAlertsAsync();
            });
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
            
            // Notify property changes to update UI
            OnPropertyChanged(nameof(RecentOrders));
            OnPropertyChanged(nameof(TotalOrdersToday));
            OnPropertyChanged(nameof(TotalSalesToday));
        }

        public async Task LoadTodaysMetricsAsync() // Load today's metrics for dashboard
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20)); // Increased timeout
                System.Diagnostics.Debug.WriteLine("🔄 Starting LoadTodaysMetricsAsync...");
                
                // Reset all metrics to default values first to prevent showing stale data
                TotalOrdersToday = 0;
                TotalSalesToday = 0m;
                MostBoughtToday = "No data";
                TrendingToday = "No data";
                MostBoughtTrend = "";
                TrendingPercent = "";
                OrdersTrend = "";
                RecentOrders.Clear();
                TopSellingProductsToday.Clear();
                InventoryAlerts.Clear();
                
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


        [RelayCommand]
        public void ShowSettingsPopup() // Show settings popup
        {
            IsSettingsPopupVisible = true;
        }

        [RelayCommand]
        private void CloseSettingsPopup() => IsSettingsPopupVisible = false; // Close settings popup

        // Sync removed in online-only mode

        [RelayCommand]
        private void OpenAddItemToPOS() // Open the Add Item to POS panel
        {
            IsSettingsPopupVisible = false;
            _addItemToPOSViewModel.IsAddItemToPOSVisible = true;
        }

        [RelayCommand]
        private void OpenManageInventoryOptions()
        {
            System.Diagnostics.Debug.WriteLine("OpenManageInventoryOptions called");
            IsSettingsPopupVisible = false;
            
            // Check if user is admin OR has been granted inventory access
            var currentUser = App.CurrentUser;
            bool hasAccess = (currentUser?.IsAdmin ?? false) || (currentUser?.CanAccessInventory ?? false);
            
            if (!hasAccess)
            {
                Application.Current?.MainPage?.DisplayAlert("Access Denied", 
                    "You don't have permission to manage inventory. Please contact an administrator.", "OK");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"Setting ManageInventoryPopup visibility to true. Current value: {_manageInventoryOptionsViewModel.IsInventoryManagementPopupVisible}");
            _manageInventoryOptionsViewModel.IsInventoryManagementPopupVisible = true;
            System.Diagnostics.Debug.WriteLine($"ManageInventoryPopup visibility set to: {_manageInventoryOptionsViewModel.IsInventoryManagementPopupVisible}");
        }

        [RelayCommand]
        private void OpenAddItemToInventory() // Open the Add Item to Inventory panel
        {
            IsSettingsPopupVisible = false;
            IsAddItemToInventoryVisible = true;
        }

        [RelayCommand] // Open the Manage POS Options panel
        private void OpenManagePOSOptions()
        {
            System.Diagnostics.Debug.WriteLine("OpenManagePOSOptions called");
            IsSettingsPopupVisible = false;
            
            // Check if user is admin OR has been granted POS access
            var currentUser = App.CurrentUser;
            bool hasAccess = (currentUser?.IsAdmin ?? false) || (currentUser?.CanAccessPOS ?? false);
            
            if (!hasAccess)
            {
                Application.Current?.MainPage?.DisplayAlert("Access Denied", 
                    "You don't have permission to manage POS. Please contact an administrator.", "OK");
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
            await SimpleNavigationService.NavigateToAsync("//usermanagement");
        }

        [RelayCommand]
        private void CloseAddItemToInventory()
        {
            IsSettingsPopupVisible = false;
            IsAddItemToInventoryVisible = false;
        }

        [RelayCommand]
        private async Task Logout()
        {
            if (Application.Current is App app)
            {
                await app.ResetAppAfterLogout(); // now it works
            }
        }

        private async Task LoadTopSellingProductsAsync() // Load top selling products for dashboard
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
                    
                    // Define color palette matching SalesReport
                    var colorPalette = new List<string>
                    {
                        "#99E599", // Green
                        "#ac94f4", // Purple/Violet
                        "#1976D2", // Blue
                        "#F0E0C1", // Brown/Beige
                        "#f5dde0"  // Light pink
                    };
                    
                    for (int i = 0; i < productSales.Count; i++)
                    {
                        productSales[i].MaxCount = maxCount;
                        productSales[i].ColorCode = colorPalette[i % colorPalette.Count];
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

        private async Task LoadRecentOrdersAsync(List<TransactionHistoryModel> transactions) // Load recent orders for dashboard
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

        private async Task LoadMostBoughtAndTrendingAsync(List<TransactionHistoryModel> transactions) // Load most bought and trending items for dashboard
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

        private async Task LoadInventoryAlertsAsync() // Load inventory alerts for dashboard
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
                
                // Compute percent of buffer remaining before hitting minimum
                // Formula: (current - minimum) / (maximum - minimum) × 100
                // This shows how much "safety buffer" remains before reaching minimum threshold
                // Also include items that are simply below minimum, even without a maximum set
                var lowStockItems = inventoryItems
                    .Where(item => item.minimumQuantity > 0)
                    .Select(item => {
                        // If item has maximum and maximum > minimum, calculate buffer percent
                        if (item.maximumQuantity > item.minimumQuantity)
                        {
                            var buffer = item.itemQuantity - item.minimumQuantity;
                            var maxBuffer = item.maximumQuantity - item.minimumQuantity;
                            var percentRemaining = (buffer / maxBuffer) * 100.0;
                            return new {
                                Item = item,
                                PercentRemaining = percentRemaining
                            };
                        }
                        else
                        {
                            // If no maximum or maximum <= minimum, just check if below minimum
                            // Set percent to negative if below minimum, 0 if at minimum, 100+ if above
                            var buffer = item.itemQuantity - item.minimumQuantity;
                            var percentRemaining = buffer < 0 ? -1.0 : (buffer == 0 ? 0.0 : 101.0);
                            return new {
                                Item = item,
                                PercentRemaining = percentRemaining
                            };
                        }
                    })
                    .Where(x => x.PercentRemaining <= 100.0) // Show items at or below max, or below minimum
                    .GroupBy(x => x.Item.itemName)
                    .Select(group => group.OrderBy(x => x.PercentRemaining).First())
                    .OrderBy(x => x.Item.CreatedAt) // FIFO: Order by creation date (oldest first)
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
                        var percent = entry.PercentRemaining;
                        
                        // Debug: Show exact calculation
                        var buffer = item.itemQuantity - item.minimumQuantity;
                        var maxBuffer = item.maximumQuantity - item.minimumQuantity;
                        System.Diagnostics.Debug.WriteLine($"🔍 {item.itemName}: Current={item.itemQuantity}, Min={item.minimumQuantity}, Max={item.maximumQuantity}");
                        System.Diagnostics.Debug.WriteLine($"🔍   Buffer: ({item.itemQuantity} - {item.minimumQuantity}) ÷ ({item.maximumQuantity} - {item.minimumQuantity}) × 100 = {percent:F1}%");
                        
                        // Severity by percent of buffer remaining
                        // <= 0% => AT/BELOW MINIMUM (CRITICAL)
                        // 1-30% => CRITICAL (very close to minimum)
                        // 31-50% => MEDIUM
                        // 51-100% => LOW (still have good buffer)
                        string stockLevel;
                        if (percent <= 0)
                            stockLevel = "AT MINIMUM";
                        else if (percent <= 30.0)
                            stockLevel = "CRITICAL";
                        else if (percent <= 50.0)
                            stockLevel = "MEDIUM";
                        else
                            stockLevel = "LOW";

                        // Normalize unit display (e.g., "Liters (L)" -> "L", "Kilograms (kg)" -> "kg")
                        var normalizedUnit = item.unitOfMeasurement;
                        if (!string.IsNullOrWhiteSpace(normalizedUnit))
                        {
                            // Extract unit abbreviation from formats like "Liters (L)" or "Kilograms (kg)"
                            var parenStart = normalizedUnit.IndexOf('(');
                            if (parenStart >= 0 && normalizedUnit.EndsWith(")"))
                            {
                                normalizedUnit = normalizedUnit.Substring(parenStart + 1, normalizedUnit.Length - parenStart - 2);
                            }
                        }

                        // Format: "CRITICAL: Tapiocca (6 kg)"
                        var alertText = $"{stockLevel}: {item.itemName} ({item.itemQuantity:F1} {normalizedUnit})";
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
