using Coftea_Capstone.Views.Pages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Coftea_Capstone;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;
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

        public async Task LoadTodaysMetricsAsync()
        {
            try
            {
                // Load top selling products for dashboard
                await LoadTopSellingProductsAsync();
                var database = new Models.Database(); // Will use auto-detected host
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);
                
                var transactions = await database.GetTransactionsByDateRangeAsync(today, tomorrow);
                
                TotalOrdersToday = transactions.Count;
                TotalSalesToday = transactions.Sum(t => t.Total);
                
                // Load recent orders
                await LoadRecentOrdersAsync(transactions);
                
                // Load most bought and trending items
                await LoadMostBoughtAndTrendingAsync(transactions);
                
                // Load inventory alerts
                await LoadInventoryAlertsAsync();
                
                // Set trend indicators
                OrdersTrend = TotalOrdersToday > 0 ? "↑ Today" : "No orders today";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load today's metrics: {ex.Message}");
                // Keep default values (0) if loading fails
            }
        }

        [RelayCommand]
        public void ShowSettingsPopup() => IsSettingsPopupVisible = true;

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
            IsSettingsPopupVisible = false;
            if (!(App.CurrentUser?.IsAdmin ?? false))
            {
                Application.Current?.MainPage?.DisplayAlert("Unauthorized", "Only admins can manage Inventory.", "OK");
                return;
            }
            _manageInventoryOptionsViewModel.IsInventoryManagementPopupVisible = true;
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
            IsSettingsPopupVisible = false;
            if (!(App.CurrentUser?.IsAdmin ?? false))
            {
                Application.Current?.MainPage?.DisplayAlert("Unauthorized", "Only admins can manage POS settings.", "OK");
                return;
            }
            _managePOSOptionsViewModel.IsPOSManagementPopupVisible = true;
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
            if (Application.Current?.MainPage is not null)
            {
                await Application.Current.MainPage.Navigation.PushAsync(new UserManagement());
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

                foreach (var transaction in recentTransactions)
                {
                    RecentOrders.Add(new RecentOrderModel
                    {
                        OrderNumber = transaction.TransactionId,
                        ProductName = transaction.DrinkName,
                        ProductImage = "drink.png", // Default image
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
                
                // Filter items that have low stock (below minimum quantity or very low amounts)
                var lowStockItems = inventoryItems
                    .Where(item => item.itemQuantity <= item.minimumQuantity || item.itemQuantity <= 10)
                    .OrderBy(item => item.itemQuantity)
                    .Take(5) // Show top 5 lowest stock items
                    .ToList();

                if (lowStockItems.Count == 0)
                {
                    InventoryAlerts.Add("✅ All items well stocked");
                }
                else
                {
                    foreach (var item in lowStockItems)
                    {
                        var stockLevel = item.itemQuantity <= item.minimumQuantity ? "CRITICAL" : "LOW";
                        var alertText = $"{stockLevel}: {item.itemName} ({item.itemQuantity:F1} {item.unitOfMeasurement})";
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
