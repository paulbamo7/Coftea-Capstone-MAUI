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
        // ===================== Dependencies & Services =====================
        public SettingsPopUpViewModel SettingsPopup { get; set; }
        public RetryConnectionPopupViewModel RetryConnectionPopup { get; set; }

        private readonly Database _database;
        private readonly Services.ISalesReportService _salesReportService;

        // ===================== State & Models =====================
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

        // Payment method totals
        [ObservableProperty]
        private decimal cashTotal;

        [ObservableProperty]
        private decimal gCashTotal;

        [ObservableProperty]
        private decimal bankTotal;

        // Cumulative cash tracking over time
        [ObservableProperty]
        private decimal cumulativeCashTotal;

        [ObservableProperty]
        private decimal cumulativeGCashTotal;

        [ObservableProperty]
        private decimal cumulativeBankTotal;

        [ObservableProperty]
        private decimal cumulativeTotalSales;

        // Time period filter properties
        [ObservableProperty]
        private string selectedTimePeriod = "All";

        [ObservableProperty]
        private decimal weeklyCashTotal;

        [ObservableProperty]
        private decimal weeklyGCashTotal;

        [ObservableProperty]
        private decimal weeklyBankTotal;

        [ObservableProperty]
        private decimal weeklyTotalSales;

        [ObservableProperty]
        private decimal monthlyCashTotal;

        [ObservableProperty]
        private decimal monthlyGCashTotal;

        [ObservableProperty]
        private decimal monthlyBankTotal;

        [ObservableProperty]
        private decimal monthlyTotalSales;

        [ObservableProperty]
        private decimal yesterdayCashTotal;

        [ObservableProperty]
        private decimal yesterdayGCashTotal;

        [ObservableProperty]
        private decimal yesterdayBankTotal;

        [ObservableProperty]
        private decimal yesterdayTotalSales;

        // Computed properties for display
        public decimal DisplayCashTotal => SelectedTimePeriod switch
        {
            "Yesterday" => YesterdayCashTotal,
            "Weekly" => WeeklyCashTotal,
            "Monthly" => MonthlyCashTotal,
            "Today" => CashTotal,
            "All" => CumulativeCashTotal,
            _ => CumulativeCashTotal
        };

        public decimal DisplayGCashTotal => SelectedTimePeriod switch
        {
            "Yesterday" => YesterdayGCashTotal,
            "Weekly" => WeeklyGCashTotal,
            "Monthly" => MonthlyGCashTotal,
            "Today" => GCashTotal,
            "All" => CumulativeGCashTotal,
            _ => CumulativeGCashTotal
        };

        public decimal DisplayBankTotal => SelectedTimePeriod switch
        {
            "Yesterday" => YesterdayBankTotal,
            "Weekly" => WeeklyBankTotal,
            "Monthly" => MonthlyBankTotal,
            "Today" => BankTotal,
            "All" => CumulativeBankTotal,
            _ => CumulativeBankTotal
        };

        public decimal DisplayTotalSales => SelectedTimePeriod switch
        {
            "Yesterday" => YesterdayTotalSales,
            "Weekly" => WeeklyTotalSales,
            "Monthly" => MonthlyTotalSales,
            "Today" => TotalSalesToday,
            "All" => CumulativeTotalSales,
            _ => CumulativeTotalSales
        };

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
            "Frappe",
            "Fruit/Soda",
            "Milktea",
            "Coffee"
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
            
            // Load cumulative totals on initialization
            _ = LoadCumulativeTotalsAsync();
        }

        private RetryConnectionPopupViewModel GetRetryConnectionPopup()
        {
            return ((App)Application.Current).RetryConnectionPopup;
        }

        private async Task LoadRecentOrdersAsync() // Load recent orders from today
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            
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
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("⏰ LoadRecentOrdersAsync timeout");
                RecentOrders = new ObservableCollection<TransactionHistoryModel>();
                RecentOrdersCount = 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load recent orders: {ex.Message}");
                RecentOrders = new ObservableCollection<TransactionHistoryModel>();
                RecentOrdersCount = 0;
            }
        }

        private async Task CalculatePaymentMethodTotalsAsync() // Calculate today's totals by payment method
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            
            try
            {
                // Get today's transactions
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);
                var todayTransactions = await _database.GetTransactionsByDateRangeAsync(today, tomorrow);

                // Calculate totals by payment method
                CashTotal = todayTransactions
                    .Where(t => t.PaymentMethod?.Equals("Cash", StringComparison.OrdinalIgnoreCase) == true)
                    .Sum(t => t.Total);

                GCashTotal = todayTransactions
                    .Where(t => t.PaymentMethod?.Equals("GCash", StringComparison.OrdinalIgnoreCase) == true)
                    .Sum(t => t.Total);

                BankTotal = todayTransactions
                    .Where(t => t.PaymentMethod?.Equals("Bank", StringComparison.OrdinalIgnoreCase) == true)
                    .Sum(t => t.Total);

                System.Diagnostics.Debug.WriteLine($"Payment totals - Cash: {CashTotal}, GCash: {GCashTotal}, Bank: {BankTotal}");
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("⏰ CalculatePaymentMethodTotalsAsync timeout");
                CashTotal = 0;
                GCashTotal = 0;
                BankTotal = 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to calculate payment method totals: {ex.Message}");
                CashTotal = 0;
                GCashTotal = 0;
                BankTotal = 0;
            }
        }

        public async Task InitializeAsync() // Initial data load
        {
            await LoadDataAsync();
        }

        public async Task LoadDataAsync() // Load sales report data
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            
            try
            {
                IsLoading = true;
                StatusMessage = "Loading sales reports...";
                HasError = false;

                // Check DB connectivity (XAMPP/MySQL); warn if server is down
                var dbReachable = await _database.CanConnectAsync(cts.Token);
                if (!dbReachable)
                {
                    HasError = true;
                    StatusMessage = "Database not reachable. Please start XAMPP/MySQL.";
                    GetRetryConnectionPopup().ShowRetryPopup(LoadDataAsync, "Database not reachable. Please ensure XAMPP/MySQL is running and try again.");
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

                // Calculate payment method totals for today
                await CalculatePaymentMethodTotalsAsync();
                
                // Update cumulative totals
                await UpdateCumulativeTotalsAsync();
                
                // Calculate time period totals
                await CalculateTimePeriodTotalsAsync();

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
            catch (OperationCanceledException)
            {
                HasError = true;
                StatusMessage = "Loading timeout. Please try again.";
                System.Diagnostics.Debug.WriteLine("⏰ Sales Report - LoadDataAsync timeout");
                
                // Set fallback data
                SetFallbackData();
            }
            catch (Exception ex)
            {
                HasError = true;
                StatusMessage = $"Failed to load sales reports: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"❌ Sales Report error: {ex.Message}");
                
                // Set fallback data
                SetFallbackData();
                
                GetRetryConnectionPopup().ShowRetryPopup(LoadDataAsync, $"Failed to load sales reports: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        partial void OnSelectedCategoryChanged(string value) // Apply category filter
        {
            ApplyCategoryFilter();
        }

        partial void OnSelectedTimePeriodChanged(string value) // Update display totals
        {
            OnPropertyChanged(nameof(DisplayCashTotal));
            OnPropertyChanged(nameof(DisplayGCashTotal));
            OnPropertyChanged(nameof(DisplayBankTotal));
            OnPropertyChanged(nameof(DisplayTotalSales));
        }

        [RelayCommand]
        private void SelectCategory(string category) // Select category and apply filter
        {
            SelectedCategory = category;
        }

        [RelayCommand]
        private async Task SelectTimePeriod(string timePeriod) // Select time period and recalculate totals
        {
            SelectedTimePeriod = timePeriod;
            await CalculateTimePeriodTotalsAsync();
            OnPropertyChanged(nameof(DisplayCashTotal));
            OnPropertyChanged(nameof(DisplayGCashTotal));
            OnPropertyChanged(nameof(DisplayBankTotal));
            OnPropertyChanged(nameof(DisplayTotalSales));
        }

        private void UpdateCombinedCollections() // Combine coffee and milktea into single collections
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

            // Force pie charts to refresh bindings by setting new instances
            TopItemsToday = new ObservableCollection<TrendItem>(TopItemsToday?.ToList() ?? new List<TrendItem>());
            TopItemsWeekly = new ObservableCollection<TrendItem>(TopItemsWeekly?.ToList() ?? new List<TrendItem>());
            TopItemsMonthly = new ObservableCollection<TrendItem>(TopItemsMonthly?.ToList() ?? new List<TrendItem>());
        }

        private void UpdatePieChartNames(List<TrendItem> items) // Update pie chart item names and counts
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

        private List<TrendItem> GetFilteredItems(ObservableCollection<TrendItem> coffeeItems, ObservableCollection<TrendItem> milkTeaItems, string category) // Filter items based on category
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

        private void ApplyCategoryFilter() // Apply category filter to combined collections
        {
            UpdateCombinedCollections();
        }
        private async Task LoadCumulativeTotalsAsync() // Load cumulative totals from preferences
        {
            try
            {
                // Load cumulative totals from preferences
                var prefs = Preferences.Default;
                CumulativeCashTotal = prefs.Get("CumulativeCashTotal", 0m);
                CumulativeGCashTotal = prefs.Get("CumulativeGCashTotal", 0m);
                CumulativeBankTotal = prefs.Get("CumulativeBankTotal", 0m);
                CumulativeTotalSales = prefs.Get("CumulativeTotalSales", 0m);
                
                System.Diagnostics.Debug.WriteLine($"Loaded cumulative totals - Cash: {CumulativeCashTotal}, GCash: {CumulativeGCashTotal}, Bank: {CumulativeBankTotal}, Total: {CumulativeTotalSales}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading cumulative totals: {ex.Message}");
                // Initialize to zero if loading fails
                CumulativeCashTotal = 0;
                CumulativeGCashTotal = 0;
                CumulativeBankTotal = 0;
                CumulativeTotalSales = 0;
            }
        }

        private async Task UpdateCumulativeTotalsAsync() // Update cumulative totals if it's a new day
        {
            try
            {
                // Check if we've already updated cumulative totals for today
                var prefs = Preferences.Default;
                var lastUpdateDate = prefs.Get("LastCumulativeUpdateDate", DateTime.MinValue);
                var today = DateTime.Today;

                // Only update cumulative totals if it's a new day
                if (lastUpdateDate.Date < today)
                {
                    // Add today's totals to cumulative totals
                    CumulativeCashTotal += CashTotal;
                    CumulativeGCashTotal += GCashTotal;
                    CumulativeBankTotal += BankTotal;
                    CumulativeTotalSales += TotalSalesToday;

                    // Save to preferences
                    prefs.Set("CumulativeCashTotal", CumulativeCashTotal);
                    prefs.Set("CumulativeGCashTotal", CumulativeGCashTotal);
                    prefs.Set("CumulativeBankTotal", CumulativeBankTotal);
                    prefs.Set("CumulativeTotalSales", CumulativeTotalSales);
                    prefs.Set("LastCumulativeUpdateDate", today);

                    System.Diagnostics.Debug.WriteLine($"Updated cumulative totals for new day - Cash: {CumulativeCashTotal}, GCash: {CumulativeGCashTotal}, Bank: {CumulativeBankTotal}, Total: {CumulativeTotalSales}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Cumulative totals already updated for today, skipping update");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating cumulative totals: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task ResetCumulativeTotals() // Reset cumulative totals to zero
        {
            try
            {
                // Reset cumulative totals to zero
                CumulativeCashTotal = 0;
                CumulativeGCashTotal = 0;
                CumulativeBankTotal = 0;
                CumulativeTotalSales = 0;

                // Save to preferences
                var prefs = Preferences.Default;
                prefs.Set("CumulativeCashTotal", 0m);
                prefs.Set("CumulativeGCashTotal", 0m);
                prefs.Set("CumulativeBankTotal", 0m);
                prefs.Set("CumulativeTotalSales", 0m);

                System.Diagnostics.Debug.WriteLine("Cumulative totals reset to zero");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error resetting cumulative totals: {ex.Message}");
            }
        }

        private async Task CalculateTimePeriodTotalsAsync() // Calculate totals for selected time period
        {
            try
            {
                var today = DateTime.Today;
                
                if (SelectedTimePeriod == "Yesterday")
                {
                    var yesterday = today.AddDays(-1);
                    var yesterdayEnd = yesterday.AddDays(1);
                    
                    var yesterdayTransactions = await _database.GetTransactionsByDateRangeAsync(yesterday, yesterdayEnd);
                    
                    YesterdayCashTotal = yesterdayTransactions
                        .Where(t => t.PaymentMethod?.Equals("Cash", StringComparison.OrdinalIgnoreCase) == true)
                        .Sum(t => t.Total);
                    
                    YesterdayGCashTotal = yesterdayTransactions
                        .Where(t => t.PaymentMethod?.Equals("GCash", StringComparison.OrdinalIgnoreCase) == true)
                        .Sum(t => t.Total);
                    
                    YesterdayBankTotal = yesterdayTransactions
                        .Where(t => t.PaymentMethod?.Equals("Bank", StringComparison.OrdinalIgnoreCase) == true)
                        .Sum(t => t.Total);
                    
                    YesterdayTotalSales = yesterdayTransactions.Sum(t => t.Total);
                }
                else if (SelectedTimePeriod == "Weekly")
                {
                    var weekStart = today.AddDays(-(int)today.DayOfWeek);
                    var weekEnd = weekStart.AddDays(7);
                    
                    var weeklyTransactions = await _database.GetTransactionsByDateRangeAsync(weekStart, weekEnd);
                    
                    WeeklyCashTotal = weeklyTransactions
                        .Where(t => t.PaymentMethod?.Equals("Cash", StringComparison.OrdinalIgnoreCase) == true)
                        .Sum(t => t.Total);
                    
                    WeeklyGCashTotal = weeklyTransactions
                        .Where(t => t.PaymentMethod?.Equals("GCash", StringComparison.OrdinalIgnoreCase) == true)
                        .Sum(t => t.Total);
                    
                    WeeklyBankTotal = weeklyTransactions
                        .Where(t => t.PaymentMethod?.Equals("Bank", StringComparison.OrdinalIgnoreCase) == true)
                        .Sum(t => t.Total);
                    
                    WeeklyTotalSales = weeklyTransactions.Sum(t => t.Total);
                }
                else if (SelectedTimePeriod == "Monthly")
                {
                    var monthStart = new DateTime(today.Year, today.Month, 1);
                    var monthEnd = monthStart.AddMonths(1);
                    
                    var monthlyTransactions = await _database.GetTransactionsByDateRangeAsync(monthStart, monthEnd);
                    
                    MonthlyCashTotal = monthlyTransactions
                        .Where(t => t.PaymentMethod?.Equals("Cash", StringComparison.OrdinalIgnoreCase) == true)
                        .Sum(t => t.Total);
                    
                    MonthlyGCashTotal = monthlyTransactions
                        .Where(t => t.PaymentMethod?.Equals("GCash", StringComparison.OrdinalIgnoreCase) == true)
                        .Sum(t => t.Total);
                    
                    MonthlyBankTotal = monthlyTransactions
                        .Where(t => t.PaymentMethod?.Equals("Bank", StringComparison.OrdinalIgnoreCase) == true)
                        .Sum(t => t.Total);
                    
                    MonthlyTotalSales = monthlyTransactions.Sum(t => t.Total);
                }
                
                System.Diagnostics.Debug.WriteLine($"Calculated {SelectedTimePeriod} totals - Cash: {DisplayCashTotal}, GCash: {DisplayGCashTotal}, Bank: {DisplayBankTotal}, Total: {DisplayTotalSales}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error calculating {SelectedTimePeriod} totals: {ex.Message}");
            }
        }

        private void SetFallbackData() // Set fallback data on error
        {
            // Set fallback data when loading fails
            MostBoughtToday = "No data available";
            TrendingToday = "No data available";
            TrendingPercentage = 0;
            TotalSalesToday = 0;
            TotalOrdersToday = 0;
            TotalOrdersThisWeek = 0;
            ActiveDays = 0;
            
            // Set empty collections
            TopCoffeeToday = new ObservableCollection<TrendItem>();
            TopMilkteaToday = new ObservableCollection<TrendItem>();
            TopCoffeeWeekly = new ObservableCollection<TrendItem>();
            TopMilkteaWeekly = new ObservableCollection<TrendItem>();
            TopItemsToday = new ObservableCollection<TrendItem>();
            TopItemsWeekly = new ObservableCollection<TrendItem>();
            TopItemsMonthly = new ObservableCollection<TrendItem>();
            
            // Set payment totals to zero
            CashTotal = 0;
            GCashTotal = 0;
            BankTotal = 0;
            
            // Update combined collections
            UpdateCombinedCollections();
            
            // Notify property changes
            OnPropertyChanged(nameof(TopCoffeeTodayOrders));
            OnPropertyChanged(nameof(TopMilkteaTodayOrders));
        }
    }
}
