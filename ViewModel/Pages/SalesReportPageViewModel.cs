using Coftea_Capstone.C_;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Coftea_Capstone.ViewModel.Controls;
using Coftea_Capstone.Models;
using Microsoft.Maui.Networking;
using System.Windows.Input;

namespace Coftea_Capstone.ViewModel
{
    public partial class SalesReportPageViewModel : ObservableObject
    {
        public SettingsPopUpViewModel SettingsPopup { get; set; }
        public RetryConnectionPopupViewModel RetryConnectionPopup { get; set; }

        private readonly Database _database;
        private readonly Services.ISalesReportService _salesReportService;

        [ObservableProperty]
        private ObservableCollection<SalesReportPageModel> salesReports = new();

        // KPI properties
        [ObservableProperty]
        private int activeDays;

        [ObservableProperty]
        private string mostBoughtToday;

        [ObservableProperty]
        private string trendingToday;

        [ObservableProperty]
        private double trendingPercentage;

        [ObservableProperty]
        private decimal totalSalesToday;

        [ObservableProperty]
        private int totalOrdersToday;

        [ObservableProperty]
        private int totalOrdersThisWeek;

        // Top items lists
        [ObservableProperty]
        private ObservableCollection<TrendItem> topCoffeeToday = new();

        [ObservableProperty]
        private ObservableCollection<TrendItem> topMilkteaToday = new();

        [ObservableProperty]
        private ObservableCollection<TrendItem> topCoffeeWeekly = new();

        [ObservableProperty]
        private ObservableCollection<TrendItem> topMilkteaWeekly = new();

        // Combined top items for new layout
        [ObservableProperty]
        private ObservableCollection<TrendItem> topItemsToday = new();

        [ObservableProperty]
        private ObservableCollection<TrendItem> topItemsWeekly = new();

        [ObservableProperty]
        private ObservableCollection<TrendItem> topItemsMonthly = new();

        // Filtered order counts for selected category
        [ObservableProperty]
        private int filteredTodayOrders;

        [ObservableProperty]
        private int filteredWeeklyOrders;

        [ObservableProperty]
        private int monthlyOrders;

        // Pie chart item names
        [ObservableProperty]
        private string item1Name = "Item 1";

        [ObservableProperty]
        private string item2Name = "Item 2";

        [ObservableProperty]
        private string item3Name = "Item 3";

        // Pie chart item counts
        [ObservableProperty]
        private int item1Count = 0;

        [ObservableProperty]
        private int item2Count = 0;

        [ObservableProperty]
        private int item3Count = 0;

        // Pie chart trend indicators
        [ObservableProperty]
        private bool item1HasTrend = false;

        [ObservableProperty]
        private bool item2HasTrend = false;

        [ObservableProperty]
        private bool item3HasTrend = false;

        // Derived totals for the "X Orders" labels
        public int TopCoffeeTodayOrders => TopCoffeeToday?.Sum(i => i.Count) ?? 0;
        public int TopMilkteaTodayOrders => TopMilkteaToday?.Sum(i => i.Count) ?? 0;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string statusMessage;

        [ObservableProperty]
        private bool hasError;

        // Category selection
        public IReadOnlyList<string> AvailableCategories { get; } = new List<string>
        {
            "Overview",
            "Americano",
            "Cafe Latte",
            "Fruit and Soda Mix",
            "Frappe",
            "Milktea"
        };

        [ObservableProperty]
        private string selectedCategory = "Overview";

        [ObservableProperty]
        private bool showCoffee = true;

        [ObservableProperty]
        private bool showMilktea = true;

        // Recent Orders properties
        [ObservableProperty]
        private ObservableCollection<TransactionHistoryModel> recentOrders = new();

        [ObservableProperty]
        private int recentOrdersCount;

        public SalesReportPageViewModel(SettingsPopUpViewModel settingsPopup, Services.ISalesReportService salesReportService = null)
        {
            _database = new Database(); // Will use auto-detected host
            SettingsPopup = settingsPopup;
            _salesReportService = salesReportService ?? new Services.DatabaseSalesReportService();
        }

        private RetryConnectionPopupViewModel GetRetryConnectionPopup()
        {
            return ((App)Application.Current).RetryConnectionPopup;
        }

        private async Task LoadRecentOrdersAsync()
        {
            try
            {
                // Get recent transactions from today
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);
                var recentTransactions = await _database.GetTransactionsByDateRangeAsync(today, tomorrow);
                
                // Sort by transaction date descending and take the most recent 10
                var sortedTransactions = recentTransactions
                    .OrderByDescending(t => t.TransactionDate)
                    .Take(10)
                    .ToList();

                RecentOrders = new ObservableCollection<TransactionHistoryModel>(sortedTransactions);
                RecentOrdersCount = sortedTransactions.Count;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load recent orders: {ex.Message}");
                RecentOrders = new ObservableCollection<TransactionHistoryModel>();
                RecentOrdersCount = 0;
            }
        }

        public async Task InitializeAsync()
        {
            await LoadDataAsync();
        }

        public async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading sales reports...";
                HasError = false;

                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    HasError = true;
                    StatusMessage = "No internet connection. Please check your network.";
                    GetRetryConnectionPopup().ShowRetryPopup(LoadDataAsync, "No internet connection detected. Please check your network settings and try again.");
                    return;
                }

                // Pull summary from service (real implementation can fetch from DB)
                var startDate = DateTime.Today.AddDays(-30); // Last 30 days
                var endDate = DateTime.Today.AddDays(1); // Include today
                var summary = await _salesReportService.GetSummaryAsync(startDate, endDate);

                SalesReports = new ObservableCollection<SalesReportPageModel>(summary.Reports ?? new List<SalesReportPageModel>());

                ActiveDays = summary.ActiveDays;
                MostBoughtToday = summary.MostBoughtToday ?? "No data";
                TrendingToday = summary.TrendingToday ?? "No data";
                TrendingPercentage = summary.TrendingPercentage;
                TotalSalesToday = summary.TotalSalesToday;
                TotalOrdersToday = summary.TotalOrdersToday;
                TotalOrdersThisWeek = summary.TotalOrdersThisWeek;

                // Load recent orders
                await LoadRecentOrdersAsync();

                TopCoffeeToday = new ObservableCollection<TrendItem>(summary.TopCoffeeToday ?? new List<TrendItem>());
                TopMilkteaToday = new ObservableCollection<TrendItem>(summary.TopMilkteaToday ?? new List<TrendItem>());
                TopCoffeeWeekly = new ObservableCollection<TrendItem>(summary.TopCoffeeWeekly ?? new List<TrendItem>());
                TopMilkteaWeekly = new ObservableCollection<TrendItem>(summary.TopMilkteaWeekly ?? new List<TrendItem>());

                // Populate combined collections for new layout
                UpdateCombinedCollections();

                StatusMessage = SalesReports.Any() ? "Sales reports loaded successfully." : "No sales reports found.";

                // Notify derived properties
                OnPropertyChanged(nameof(TopCoffeeTodayOrders));
                OnPropertyChanged(nameof(TopMilkteaTodayOrders));

                ApplyCategoryFilter();
            }
            catch (Exception ex)
            {
                HasError = true;
                StatusMessage = $"Failed to load sales reports: {ex.Message}";
                GetRetryConnectionPopup().ShowRetryPopup(LoadDataAsync, $"Failed to load sales reports: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        partial void OnSelectedCategoryChanged(string value)
        {
            ApplyCategoryFilter();
        }

        [RelayCommand]
        private void SelectCategory(string category)
        {
            SelectedCategory = category;
        }

        private void UpdateCombinedCollections()
        {
            // Filter today's items based on selected category
            var todayItems = GetFilteredItems(TopCoffeeToday, TopMilkteaToday, SelectedCategory);
            var topTodayItems = todayItems.OrderByDescending(x => x.Count).Take(5).ToList();
            SetMaxCountForItems(topTodayItems);
            TopItemsToday = new ObservableCollection<TrendItem>(topTodayItems);

            // Filter weekly items based on selected category
            var weeklyItems = GetFilteredItems(TopCoffeeWeekly, TopMilkteaWeekly, SelectedCategory);
            var topWeeklyItems = weeklyItems.OrderByDescending(x => x.Count).Take(5).ToList();
            SetMaxCountForItems(topWeeklyItems);
            TopItemsWeekly = new ObservableCollection<TrendItem>(topWeeklyItems);

            // Create monthly data (combine weekly data for demo)
            var monthlyItems = GetFilteredItems(TopCoffeeWeekly, TopMilkteaWeekly, SelectedCategory);
            var topMonthlyItems = monthlyItems.OrderByDescending(x => x.Count).Take(5).ToList();
            SetMaxCountForItems(topMonthlyItems);
            TopItemsMonthly = new ObservableCollection<TrendItem>(topMonthlyItems);

            // Update filtered order counts
            FilteredTodayOrders = todayItems.Sum(x => x.Count);
            FilteredWeeklyOrders = weeklyItems.Sum(x => x.Count);
            MonthlyOrders = monthlyItems.Sum(x => x.Count);

            // Update pie chart item names
            UpdatePieChartNames(topTodayItems);
        }

        private void UpdatePieChartNames(List<TrendItem> items)
        {
            Item1Name = items.Count > 0 ? items[0].Name : "No Data";
            Item2Name = items.Count > 1 ? items[1].Name : "";
            Item3Name = items.Count > 2 ? items[2].Name : "";

            Item1Count = items.Count > 0 ? items[0].Count : 0;
            Item2Count = items.Count > 1 ? items[1].Count : 0;
            Item3Count = items.Count > 2 ? items[2].Count : 0;

            // Simple trend logic - show trend if count > 0
            Item1HasTrend = items.Count > 0 && items[0].Count > 0;
            Item2HasTrend = items.Count > 1 && items[1].Count > 0;
            Item3HasTrend = items.Count > 2 && items[2].Count > 0;
        }

        private void SetMaxCountForItems(List<TrendItem> items)
        {
            if (items == null || items.Count == 0) return;
            
            // Find the maximum count in the collection
            int maxCount = items.Max(x => x.Count);
            if (maxCount <= 0) maxCount = 1; // Prevent division by zero
            
            // Set the MaxCount for each item
            foreach (var item in items)
            {
                item.MaxCount = maxCount;
            }
        }

        private List<TrendItem> GetFilteredItems(ObservableCollection<TrendItem> coffeeItems, ObservableCollection<TrendItem> milkTeaItems, string category)
        {
            var filteredItems = new List<TrendItem>();

            switch (category?.ToLower())
            {
                case "frappe":
                    // Filter for frappe items from both collections
                    filteredItems.AddRange(coffeeItems.Where(item => 
                        item.Name.Contains("Frappe", StringComparison.OrdinalIgnoreCase) ||
                        item.Name.Contains("Frappé", StringComparison.OrdinalIgnoreCase)));
                    filteredItems.AddRange(milkTeaItems.Where(item => 
                        item.Name.Contains("Frappe", StringComparison.OrdinalIgnoreCase) ||
                        item.Name.Contains("Frappé", StringComparison.OrdinalIgnoreCase)));
                    break;

                case "fruit/soda":
                    // Filter for fruit/soda items from both collections
                    filteredItems.AddRange(coffeeItems.Where(item => 
                        item.Name.Contains("Soda", StringComparison.OrdinalIgnoreCase) ||
                        item.Name.Contains("Orange", StringComparison.OrdinalIgnoreCase) ||
                        item.Name.Contains("Lemon", StringComparison.OrdinalIgnoreCase) ||
                        item.Name.Contains("Grape", StringComparison.OrdinalIgnoreCase) ||
                        item.Name.Contains("Fruit", StringComparison.OrdinalIgnoreCase)));
                    filteredItems.AddRange(milkTeaItems.Where(item => 
                        item.Name.Contains("Soda", StringComparison.OrdinalIgnoreCase) ||
                        item.Name.Contains("Orange", StringComparison.OrdinalIgnoreCase) ||
                        item.Name.Contains("Lemon", StringComparison.OrdinalIgnoreCase) ||
                        item.Name.Contains("Grape", StringComparison.OrdinalIgnoreCase) ||
                        item.Name.Contains("Fruit", StringComparison.OrdinalIgnoreCase)));
                    break;

                case "milktea":
                    // Filter for milktea items from both collections
                    filteredItems.AddRange(coffeeItems.Where(item => 
                        item.Name.Contains("Matcha", StringComparison.OrdinalIgnoreCase) ||
                        item.Name.Contains("Brown Sugar", StringComparison.OrdinalIgnoreCase) ||
                        item.Name.Contains("Hokkaido", StringComparison.OrdinalIgnoreCase) ||
                        item.Name.Contains("Taro", StringComparison.OrdinalIgnoreCase) ||
                        item.Name.Contains("Wintermelon", StringComparison.OrdinalIgnoreCase) ||
                        item.Name.Contains("Milktea", StringComparison.OrdinalIgnoreCase)));
                    filteredItems.AddRange(milkTeaItems.Where(item => 
                        item.Name.Contains("Matcha", StringComparison.OrdinalIgnoreCase) ||
                        item.Name.Contains("Brown Sugar", StringComparison.OrdinalIgnoreCase) ||
                        item.Name.Contains("Hokkaido", StringComparison.OrdinalIgnoreCase) ||
                        item.Name.Contains("Taro", StringComparison.OrdinalIgnoreCase) ||
                        item.Name.Contains("Wintermelon", StringComparison.OrdinalIgnoreCase) ||
                        item.Name.Contains("Milktea", StringComparison.OrdinalIgnoreCase)));
                    break;

                case "coffee":
                    // Filter for coffee items from both collections
                    filteredItems.AddRange(coffeeItems.Where(item => 
                        item.Name.Contains("Coffee", StringComparison.OrdinalIgnoreCase) ||
                        item.Name.Contains("Americano", StringComparison.OrdinalIgnoreCase) ||
                        item.Name.Contains("Latte", StringComparison.OrdinalIgnoreCase) ||
                        item.Name.Contains("Cappuccino", StringComparison.OrdinalIgnoreCase) ||
                        item.Name.Contains("Espresso", StringComparison.OrdinalIgnoreCase) ||
                        (!item.Name.Contains("Frappe", StringComparison.OrdinalIgnoreCase) &&
                         !item.Name.Contains("Soda", StringComparison.OrdinalIgnoreCase) &&
                         !item.Name.Contains("Matcha", StringComparison.OrdinalIgnoreCase) &&
                         !item.Name.Contains("Milktea", StringComparison.OrdinalIgnoreCase))));
                    filteredItems.AddRange(milkTeaItems.Where(item => 
                        item.Name.Contains("Coffee", StringComparison.OrdinalIgnoreCase) ||
                        item.Name.Contains("Americano", StringComparison.OrdinalIgnoreCase) ||
                        item.Name.Contains("Latte", StringComparison.OrdinalIgnoreCase) ||
                        item.Name.Contains("Cappuccino", StringComparison.OrdinalIgnoreCase) ||
                        item.Name.Contains("Espresso", StringComparison.OrdinalIgnoreCase)));
                    break;

                default:
                    // Show all items for overview or unknown categories
                    filteredItems.AddRange(coffeeItems);
                    filteredItems.AddRange(milkTeaItems);
                    break;
            }

            return filteredItems;
        }

        private void ApplyCategoryFilter()
        {
            // Update combined collections when category changes
            UpdateCombinedCollections();
        }
    }
}
