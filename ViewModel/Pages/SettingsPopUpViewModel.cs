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
                var database = new Models.Database(host: "0.0.0.0", database: "coftea_db", user: "root", password: "");
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);
                
                var transactions = await database.GetTransactionsByDateRangeAsync(today, tomorrow);
                
                TotalOrdersToday = transactions.Count;
                TotalSalesToday = transactions.Sum(t => t.Total);
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
                var database = new Models.Database(host: "0.0.0.0", database: "coftea_db", user: "root", password: "");
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
    }
}
