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
            "1 Week" => WeeklyCashTotal,
            "1 Month" => MonthlyCashTotal,
            "Today" => CashTotal,
            "All Time" => CumulativeCashTotal,
            _ => CumulativeCashTotal
        };

        public decimal DisplayGCashTotal => SelectedTimePeriod switch
        {
            "Yesterday" => YesterdayGCashTotal,
            "1 Week" => WeeklyGCashTotal,
            "1 Month" => MonthlyGCashTotal,
            "Today" => GCashTotal,
            "All Time" => CumulativeGCashTotal,
            _ => CumulativeGCashTotal
        };

        public decimal DisplayBankTotal => SelectedTimePeriod switch
        {
            "Yesterday" => YesterdayBankTotal,
            "1 Week" => WeeklyBankTotal,
            "1 Month" => MonthlyBankTotal,
            "Today" => BankTotal,
            "All Time" => CumulativeBankTotal,
            _ => CumulativeBankTotal
        };

        public decimal DisplayTotalSales => SelectedTimePeriod switch
        {
            "Yesterday" => YesterdayTotalSales,
            "1 Week" => WeeklyTotalSales,
            "1 Month" => MonthlyTotalSales,
            "Today" => TotalSalesToday,
            "All Time" => CumulativeTotalSales,
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

        // Header date ranges
        [ObservableProperty]
        private string weeklyRangeText = string.Empty; // e.g., 10/30 - 11/05

        [ObservableProperty]
        private string monthlyRangeText = string.Empty; // e.g., 11/01 - 11/30

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

        // Additional category series (used by filters; can be empty and fall back to name-matching)
        [ObservableProperty]
        private ObservableCollection<TrendItem> topFrappeToday = new();
        [ObservableProperty]
        private ObservableCollection<TrendItem> topFruitSodaToday = new();
        [ObservableProperty]
        private ObservableCollection<TrendItem> topFrappeWeekly = new();
        [ObservableProperty]
        private ObservableCollection<TrendItem> topFruitSodaWeekly = new();

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

        [RelayCommand]
        private async Task ExportWeeklyReportToPdf()
        {
            try
            {
                if (!Services.NetworkService.HasInternetConnection())
                {
                    await Application.Current.MainPage.DisplayAlert("No Internet", "Please connect to the internet to export reports.", "OK");
                    return;
                }

                var end = DateTime.Today.AddDays(1);
                var start = end.AddDays(-7);
                var transactions = await _database.GetTransactionsByDateRangeAsync(start, end);
                var top = TopItemsWeekly?.ToList() ?? new List<TrendItem>();

                var pdfService = new Services.PDFReportService();
                var path = await pdfService.GenerateWeeklyReportAsync(start, end, transactions, top);
                await Application.Current.MainPage.DisplayAlert("Exported", $"Weekly report saved to:\n{path}", "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to export weekly report: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task ExportMonthlyReportToPdf()
        {
            try
            {
                if (!Services.NetworkService.HasInternetConnection())
                {
                    await Application.Current.MainPage.DisplayAlert("No Internet", "Please connect to the internet to export reports.", "OK");
                    return;
                }

                // Determine month range (current month)
                var now = DateTime.Today;
                var start = new DateTime(now.Year, now.Month, 1);
                var end = start.AddMonths(1);

                var transactions = await _database.GetTransactionsByDateRangeAsync(start, end);
                var top = TopItemsMonthly?.ToList() ?? new List<TrendItem>();

                var pdfService = new Services.PDFReportService();
                var path = await pdfService.GenerateMonthlyReportAsync(start, end, transactions, top);
                await Application.Current.MainPage.DisplayAlert("Exported", $"Monthly report saved to:\n{path}", "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to export monthly report: {ex.Message}", "OK");
            }
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

        private void UpdateHeaderRanges()
        {
            try
            {
                var today = DateTime.Today;
                // Weekly: last 7 days inclusive (Monolithic approach: today-6 to today)
                var weeklyStart = today.AddDays(-6);
                var weeklyEnd = today;
                WeeklyRangeText = $"{weeklyStart:MM/dd} - {weeklyEnd:MM/dd}";

                // Monthly: first day to last day of current month
                var monthStart = new DateTime(today.Year, today.Month, 1);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                MonthlyRangeText = $"{monthStart:MM/dd} - {monthEnd:MM/dd}";
            }
            catch { }
        }

        public async Task RefreshRecentOrdersAsync() // Public method to refresh recent orders
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 Refreshing recent orders...");
                await LoadRecentOrdersAsync();
                OnPropertyChanged(nameof(RecentOrders));
                OnPropertyChanged(nameof(RecentOrdersCount));
                System.Diagnostics.Debug.WriteLine("✅ Recent orders refreshed successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error refreshing recent orders: {ex.Message}");
            }
        }

        public async Task RefreshTodayAsync() // Public method to refresh today's section and totals
        {
            try
            {
                // Reload recent orders (today)
                await LoadRecentOrdersAsync();

                // Recompute payment method totals for today
                await CalculatePaymentMethodTotalsAsync();

                // Force Today totals recalculation and update bound display properties
                SelectedTimePeriod = "Today";
                await CalculateTimePeriodTotalsAsync();
                OnPropertyChanged(nameof(DisplayCashTotal));
                OnPropertyChanged(nameof(DisplayGCashTotal));
                OnPropertyChanged(nameof(DisplayBankTotal));
                OnPropertyChanged(nameof(DisplayTotalSales));

                // Refresh top products for pie/bar charts (today)
                try
                {
                    var today = DateTime.Today;
                    var tomorrow = today.AddDays(1);
                    var topDict = await _database.GetTopProductsByDateRangeAsync(today, tomorrow, 50);
                    
                    // Categorize products properly
                    var allProducts = await _database.GetProductsAsyncCached();
                    var productLookup = allProducts.ToDictionary(p => p.ProductName, p => new { p.Category, ColorCode = p.ColorCode ?? "" });
                    
                    // Separate by category
                    var coffeeProducts = new List<TrendItem>();
                    var milkTeaProducts = new List<TrendItem>();
                    var frappeProducts = new List<TrendItem>();
                    var fruitSodaProducts = new List<TrendItem>();
                    
                    foreach (var product in topDict)
                    {
                        var productInfo = productLookup.GetValueOrDefault(product.Key, new { Category = "Coffee", ColorCode = "" });
                        
                        var trendItem = new TrendItem 
                        { 
                            Name = product.Key, 
                            Count = product.Value,
                            ColorCode = productInfo.ColorCode
                        };
                        
                        // Categorize based on product category from database
                        switch (productInfo.Category?.ToLower())
                        {
                            case "frappe":
                                frappeProducts.Add(trendItem);
                                break;
                            case "fruitsoda":
                            case "fruit soda":
                            case "fruit_soda":
                                fruitSodaProducts.Add(trendItem);
                                break;
                            case "milktea":
                            case "milk tea":
                            case "milk_tea":
                                milkTeaProducts.Add(trendItem);
                                break;
                            case "coffee":
                            default:
                                coffeeProducts.Add(trendItem);
                                break;
                        }
                    }
                    
                    // Update category collections
                    TopCoffeeToday = new ObservableCollection<TrendItem>(coffeeProducts.OrderByDescending(i => i.Count).Take(5));
                    TopMilkteaToday = new ObservableCollection<TrendItem>(milkTeaProducts.OrderByDescending(i => i.Count).Take(5));
                    TopFrappeToday = new ObservableCollection<TrendItem>(frappeProducts.OrderByDescending(i => i.Count).Take(5));
                    TopFruitSodaToday = new ObservableCollection<TrendItem>(fruitSodaProducts.OrderByDescending(i => i.Count).Take(5));
                    
                    // Apply current category filter
                    ApplyCategoryFilter();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ RefreshTodayAsync: top products refresh failed: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error refreshing today's sales: {ex.Message}");
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

                // Populate category-specific collections
                TopFrappeToday = new ObservableCollection<TrendItem>(summary.TopFrappeToday ?? new List<TrendItem>());
                TopFruitSodaToday = new ObservableCollection<TrendItem>(summary.TopFruitSodaToday ?? new List<TrendItem>());
                TopFrappeWeekly = new ObservableCollection<TrendItem>(summary.TopFrappeWeekly ?? new List<TrendItem>());
                TopFruitSodaWeekly = new ObservableCollection<TrendItem>(summary.TopFruitSodaWeekly ?? new List<TrendItem>());

                // Populate combined collections for new layout
                UpdateCombinedCollections();

                StatusMessage = SalesReports.Any() ? "Sales reports loaded successfully." : "No sales reports found.";

                // Notify derived properties
                OnPropertyChanged(nameof(TopCoffeeTodayOrders));
                OnPropertyChanged(nameof(TopMilkteaTodayOrders));

                ApplyCategoryFilter();
                UpdateHeaderRanges();
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
            System.Diagnostics.Debug.WriteLine($"Category changed to: {value}");
            ApplyCategoryFilter();
            
            // Force UI updates for all filtered collections
            OnPropertyChanged(nameof(TopItemsToday));
            OnPropertyChanged(nameof(TopItemsWeekly));
            OnPropertyChanged(nameof(TopItemsMonthly));
            OnPropertyChanged(nameof(FilteredTodayOrders));
            OnPropertyChanged(nameof(FilteredWeeklyOrders));
            OnPropertyChanged(nameof(MonthlyOrders));
        }

        partial void OnSelectedTimePeriodChanged(string value) // Update display totals
        {
            System.Diagnostics.Debug.WriteLine($"Time period changed to: {value}");
            OnPropertyChanged(nameof(DisplayCashTotal));
            OnPropertyChanged(nameof(DisplayGCashTotal));
            OnPropertyChanged(nameof(DisplayBankTotal));
            OnPropertyChanged(nameof(DisplayTotalSales));
            
            // Also update category-specific data when time period changes
            ApplyCategoryFilter();
        }

        [RelayCommand]
        private void SelectCategory(string category) // Select category and apply filter
        {
            SelectedCategory = category;
        }

        [RelayCommand]
        private async Task SelectTimePeriod(string timePeriod) // Select time period and recalculate totals
        {
            try
            {
                if (string.Equals(SelectedTimePeriod, timePeriod, StringComparison.OrdinalIgnoreCase))
                    return; // Skip if already on the selected time period

                // Set loading state
                IsLoading = true;
                
                // Update the selected time period
                SelectedTimePeriod = timePeriod;
                
                // Clear previous data to prevent showing stale data
                CashTotal = GCashTotal = BankTotal = 0;
                
                // Calculate the new totals
                await CalculateTimePeriodTotalsAsync();
                
                // Notify UI of all property changes in a single batch
                OnPropertyChanged(nameof(DisplayCashTotal));
                OnPropertyChanged(nameof(DisplayGCashTotal));
                OnPropertyChanged(nameof(DisplayBankTotal));
                OnPropertyChanged(nameof(DisplayTotalSales));
                
                // Force UI update
                await Task.Delay(50); // Small delay to allow UI to catch up
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in SelectTimePeriod: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void UpdateCombinedCollections() // Combine categories into single collections
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Updating combined collections for category: {SelectedCategory}");

                // Filter today's items based on selected category
                var todayItems = GetFilteredItems(TopCoffeeToday, TopMilkteaToday, TopFrappeToday, TopFruitSodaToday, SelectedCategory);
                var topTodayItems = todayItems.OrderByDescending(x => x.Count).Take(5).ToList();
                
                System.Diagnostics.Debug.WriteLine($"Found {todayItems.Count} today items, top {topTodayItems.Count} after filtering");
                
                // Set max count for proper scaling of bars
                SetMaxCountForItems(topTodayItems);
                
                // Filter weekly items based on selected category
                var weeklyItems = GetFilteredItems(TopCoffeeWeekly, TopMilkteaWeekly, TopFrappeWeekly, TopFruitSodaWeekly, SelectedCategory);
                var topWeeklyItems = weeklyItems.OrderByDescending(x => x.Count).Take(5).ToList();
                SetMaxCountForItems(topWeeklyItems);
                AssignUniqueColors(topWeeklyItems); // Assign unique colors to weekly items
                
                // Create monthly data (combine weekly data for demo)
                var monthlyItems = GetFilteredItems(TopCoffeeWeekly, TopMilkteaWeekly, TopFrappeWeekly, TopFruitSodaWeekly, SelectedCategory);
                var topMonthlyItems = monthlyItems.OrderByDescending(x => x.Count).Take(5).ToList();
                SetMaxCountForItems(topMonthlyItems);
                AssignUniqueColors(topMonthlyItems); // Assign unique colors to monthly items
                
                // Update filtered order counts
                FilteredTodayOrders = todayItems.Sum(x => x.Count);
                FilteredWeeklyOrders = weeklyItems.Sum(x => x.Count);
                MonthlyOrders = monthlyItems.Sum(x => x.Count);
                
                System.Diagnostics.Debug.WriteLine($"Order counts - Today: {FilteredTodayOrders}, Weekly: {FilteredWeeklyOrders}, Monthly: {MonthlyOrders}");

                // Update the collections with new data on the main thread
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        // Update the collections with new data
                        TopItemsToday = new ObservableCollection<TrendItem>(topTodayItems);
                        TopItemsWeekly = new ObservableCollection<TrendItem>(topWeeklyItems);
                        TopItemsMonthly = new ObservableCollection<TrendItem>(topMonthlyItems);
                        
                        // Update pie chart with the filtered items
                        UpdatePieChartNames(topTodayItems);
                        
                        // Force UI to update
                        OnPropertyChanged(nameof(TopItemsToday));
                        OnPropertyChanged(nameof(TopItemsWeekly));
                        OnPropertyChanged(nameof(TopItemsMonthly));
                        OnPropertyChanged(nameof(Item1Name));
                        OnPropertyChanged(nameof(Item2Name));
                        OnPropertyChanged(nameof(Item3Name));
                        OnPropertyChanged(nameof(Item1Count));
                        OnPropertyChanged(nameof(Item2Count));
                        OnPropertyChanged(nameof(Item3Count));
                        OnPropertyChanged(nameof(Item1HasTrend));
                        OnPropertyChanged(nameof(Item2HasTrend));
                        OnPropertyChanged(nameof(Item3HasTrend));
                        
                        System.Diagnostics.Debug.WriteLine($"UI collections updated with {topTodayItems.Count} items for {SelectedCategory}");
                        
                        // Debug output for pie chart data
                        if (topTodayItems.Any())
                        {
                            System.Diagnostics.Debug.WriteLine("Pie Chart Data:");
                            for (int i = 0; i < Math.Min(3, topTodayItems.Count); i++)
                            {
                                System.Diagnostics.Debug.WriteLine($"  Item {i + 1}: {topTodayItems[i].Name} - {topTodayItems[i].Count}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error updating UI collections: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in UpdateCombinedCollections: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
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

        private void AssignUniqueColors(List<TrendItem> items)
        {
            if (items == null || items.Count == 0) return;
            
            // Define a palette of distinct colors
            var colorPalette = new List<string>
            {
                "#66BB6A", // Green
                "#42A5F5", // Blue
                "#EF5350", // Red
                "#FFA726", // Orange
                "#AB47BC", // Purple
                "#26A69A", // Teal
                "#FFB74D", // Amber
                "#78909C", // Blue Grey
                "#EC407A", // Pink
                "#5C6BC0", // Indigo
                "#26C6DA", // Cyan
                "#AED581", // Light Green
                "#FFD54F", // Yellow
                "#9575CD", // Deep Purple
                "#FF7043"  // Deep Orange
            };
            
            // Assign unique colors to each item
            for (int i = 0; i < items.Count; i++)
            {
                items[i].ColorCode = colorPalette[i % colorPalette.Count];
            }
        }

        private List<TrendItem> GetFilteredItems(ObservableCollection<TrendItem> coffeeItems, ObservableCollection<TrendItem> milkTeaItems, 
            ObservableCollection<TrendItem> frappeItems, ObservableCollection<TrendItem> fruitSodaItems, string category)
        {
            var filteredItems = new List<TrendItem>();
            var categoryLower = category?.ToLower() ?? string.Empty;

            // Handle Overview/All category - get top item from each category
            if (string.IsNullOrEmpty(category) || 
                categoryLower.Equals("all", StringComparison.OrdinalIgnoreCase) ||
                categoryLower.Equals("overview", StringComparison.OrdinalIgnoreCase))
            {
                // Get all items from all categories
                var allItems = new List<TrendItem>();
                if (coffeeItems != null) allItems.AddRange(coffeeItems);
                if (milkTeaItems != null) allItems.AddRange(milkTeaItems);
                if (frappeItems != null) allItems.AddRange(frappeItems);
                if (fruitSodaItems != null) allItems.AddRange(fruitSodaItems);
                
                // Group by name and sum counts for duplicate items
                var groupedItems = allItems
                    .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(g => new TrendItem 
                    { 
                        Name = g.Key, 
                        Count = g.Sum(x => x.Count),
                        ColorCode = g.First().ColorCode,
                        MaxCount = g.Max(x => x.MaxCount)
                    })
                    .OrderByDescending(x => x.Count)
                    .Take(5) // Get top 5 overall
                    .ToList();
                    
                return groupedItems;
            }

            // For specific categories, use the appropriate collection
            switch (categoryLower)
            {
                case "coffee":
                    if (coffeeItems != null) 
                    {
                        filteredItems.AddRange(coffeeItems);
                    }
                    break;
                    
                case "milktea":
                    if (milkTeaItems != null) 
                    {
                        filteredItems.AddRange(milkTeaItems);
                    }
                    break;
                    
                case "frappe":
                    // First check dedicated frappe items
                    if (frappeItems != null && frappeItems.Any())
                    {
                        filteredItems.AddRange(frappeItems);
                    }
                    
                    // Always check coffee and milk tea items for frappe products
                    var frappeKeywords = new[] { "frappe", "frap" };
                    
                    if (coffeeItems != null)
                    {
                        var frappeFromCoffee = coffeeItems
                            .Where(item => frappeKeywords.Any(keyword => 
                                item.Name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0))
                            .ToList();
                        filteredItems.AddRange(frappeFromCoffee);
                    }
                    
                    if (milkTeaItems != null)
                    {
                        var frappeFromMilkTea = milkTeaItems
                            .Where(item => frappeKeywords.Any(keyword => 
                                item.Name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0))
                            .ToList();
                        filteredItems.AddRange(frappeFromMilkTea);
                    }
                    
                    // If still no items, try to find any items that might be frappes
                    if (!filteredItems.Any())
                    {
                        var allItems = new List<TrendItem>();
                        if (coffeeItems != null) allItems.AddRange(coffeeItems);
                        if (milkTeaItems != null) allItems.AddRange(milkTeaItems);
                        
                        var possibleFrappes = allItems
                            .Where(item => item.Name.IndexOf("fr", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                         item.Name.IndexOf("fp", StringComparison.OrdinalIgnoreCase) >= 0)
                            .ToList();
                            
                        filteredItems.AddRange(possibleFrappes);
                    }
                    break;
                    
                case "fruit/soda":
                case "fruitsoda":
                    System.Diagnostics.Debug.WriteLine($"🍹 Filtering for Fruit/Soda category");
                    // First check dedicated fruit soda items
                    if (fruitSodaItems != null && fruitSodaItems.Any())
                    {
                        System.Diagnostics.Debug.WriteLine($"🍹 Found {fruitSodaItems.Count} dedicated fruit/soda items");
                        filteredItems.AddRange(fruitSodaItems);
                    }
                    
                    // Always check coffee and milk tea items for fruit/soda products
                    var fruitSodaKeywords = new[] { 
                        "soda", "orange", "lemon", "grape", "fruit", "berry", "mango", 
                        "strawberry", "blueberry", "raspberry", "passion", "pineapple",
                        "kiwi", "watermelon", "peach", "pear", "apple", "cranberry",
                        "cherry", "banana", "coconut", "lime", "pomegranate", "guava",
                        "lychee", "dragon", "bubble", "tapioca", "jelly", "sago"
                    };
                    
                    if (coffeeItems != null)
                    {
                        var fruitSodaFromCoffee = coffeeItems
                            .Where(item => fruitSodaKeywords.Any(keyword => 
                                item.Name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0))
                            .ToList();
                        System.Diagnostics.Debug.WriteLine($"🍹 Found {fruitSodaFromCoffee.Count} fruit/soda items from coffee collection");
                        filteredItems.AddRange(fruitSodaFromCoffee);
                    }
                    
                    if (milkTeaItems != null)
                    {
                        var fruitSodaFromMilkTea = milkTeaItems
                            .Where(item => fruitSodaKeywords.Any(keyword => 
                                item.Name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0))
                            .ToList();
                        System.Diagnostics.Debug.WriteLine($"🍹 Found {fruitSodaFromMilkTea.Count} fruit/soda items from milk tea collection");
                        filteredItems.AddRange(fruitSodaFromMilkTea);
                    }
                    
                    // If still no items, try to find any items that might be fruit/soda
                    if (!filteredItems.Any())
                    {
                        System.Diagnostics.Debug.WriteLine($"🍹 No fruit/soda items found, trying fallback search");
                        var allItems = new List<TrendItem>();
                        if (coffeeItems != null) allItems.AddRange(coffeeItems);
                        if (milkTeaItems != null) allItems.AddRange(milkTeaItems);
                        
                        // More specific fallback - look for items that contain fruit/soda keywords
                        var possibleFruitSodas = allItems
                            .Where(item => fruitSodaKeywords.Any(keyword => 
                                item.Name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0))
                            .ToList();
                        
                        System.Diagnostics.Debug.WriteLine($"🍹 Fallback found {possibleFruitSodas.Count} possible fruit/soda items");
                        filteredItems.AddRange(possibleFruitSodas.Take(5));
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"🍹 Total fruit/soda items after filtering: {filteredItems.Count}");
                    break;
            }

            // Group by name and sum counts for duplicate items
            var groupedFilteredItems = filteredItems
                .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .Select(g => new TrendItem 
                { 
                    Name = g.Key, 
                    Count = g.Sum(x => x.Count),
                    ColorCode = g.First().ColorCode,
                    MaxCount = g.Max(x => x.MaxCount)
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            return groupedFilteredItems;
        }
        
        private TrendItem FindTopItemByName(IEnumerable<TrendItem> items, params string[] searchTerms)
        {
            if (items == null || !items.Any()) return null;
            
            return items
                .Where(item => searchTerms.Any(term => 
                    item.Name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0))
                .OrderByDescending(item => item.Count)
                .FirstOrDefault();
        }

        // Helper method to find items by name patterns
        private IEnumerable<TrendItem> FindItemsByName(IEnumerable<TrendItem> items, params string[] searchTerms)
        {
            if (items == null) return Enumerable.Empty<TrendItem>();
            
            return items.Where(item => 
                searchTerms.Any(term => 
                    item.Name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0));
        }
        private void ApplyCategoryFilter() // Apply category filter to combined collections
        {
            System.Diagnostics.Debug.WriteLine($"Applying category filter for: {SelectedCategory}");
            UpdateCombinedCollections();
            
            // Force UI updates for all related properties
            OnPropertyChanged(nameof(TopItemsToday));
            OnPropertyChanged(nameof(TopItemsWeekly));
            OnPropertyChanged(nameof(TopItemsMonthly));
            OnPropertyChanged(nameof(FilteredTodayOrders));
            OnPropertyChanged(nameof(FilteredWeeklyOrders));
            OnPropertyChanged(nameof(MonthlyOrders));
            OnPropertyChanged(nameof(Item1Name));
            OnPropertyChanged(nameof(Item2Name));
            OnPropertyChanged(nameof(Item3Name));
            OnPropertyChanged(nameof(Item1Count));
            OnPropertyChanged(nameof(Item2Count));
            OnPropertyChanged(nameof(Item3Count));
            OnPropertyChanged(nameof(Item1HasTrend));
            OnPropertyChanged(nameof(Item2HasTrend));
            OnPropertyChanged(nameof(Item3HasTrend));
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
                var now = DateTime.Now;
                var today = now.Date;
                
                if (SelectedTimePeriod == "Today")
                {
                    var todayStart = today;
                    var todayEnd = today.AddDays(1);
                    
                    var todayTransactions = await _database.GetTransactionsByDateRangeAsync(todayStart, todayEnd);
                    
                    // Update today's totals
                    CashTotal = todayTransactions
                        .Where(t => t.PaymentMethod?.Equals("Cash", StringComparison.OrdinalIgnoreCase) == true)
                        .Sum(t => t.Total);
                    
                    GCashTotal = todayTransactions
                        .Where(t => t.PaymentMethod?.Equals("GCash", StringComparison.OrdinalIgnoreCase) == true)
                        .Sum(t => t.Total);
                    
                    BankTotal = todayTransactions
                        .Where(t => t.PaymentMethod?.Equals("Bank", StringComparison.OrdinalIgnoreCase) == true)
                        .Sum(t => t.Total);
                    
                    TotalSalesToday = todayTransactions.Sum(t => t.Total);
                    
                    System.Diagnostics.Debug.WriteLine($"Today's totals - Cash: {CashTotal}, GCash: {GCashTotal}, Bank: {BankTotal}, Total: {TotalSalesToday}");
                }
                else if (SelectedTimePeriod == "Yesterday")
                {
                    var yesterday = today.AddDays(-1);
                    var yesterdayStart = yesterday;
                    var yesterdayEnd = today;
                    
                    // Get transactions from yesterday to today (to show cumulative data up to today)
                    var allTransactions = await _database.GetTransactionsByDateRangeAsync(yesterdayStart, today.AddDays(1));
                    
                    // Filter for just yesterday
                    var yesterdayOnly = allTransactions
                        .Where(t => t.TransactionDate >= yesterdayStart && t.TransactionDate < yesterdayEnd)
                        .ToList();
                    
                    // Calculate yesterday's totals
                    var cashTotal = yesterdayOnly
                        .Where(t => t.PaymentMethod?.Equals("Cash", StringComparison.OrdinalIgnoreCase) == true)
                        .Sum(t => t.Total);
                    
                    var gcashTotal = yesterdayOnly
                        .Where(t => t.PaymentMethod?.Equals("GCash", StringComparison.OrdinalIgnoreCase) == true)
                        .Sum(t => t.Total);
                    
                    var bankTotal = yesterdayOnly
                        .Where(t => t.PaymentMethod?.Equals("Bank", StringComparison.OrdinalIgnoreCase) == true)
                        .Sum(t => t.Total);
                    
                    var totalSales = yesterdayOnly.Sum(t => t.Total);
                    
                    // Update properties on the main thread
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        YesterdayCashTotal = allTransactions
                            .Where(t => t.PaymentMethod?.Equals("Cash", StringComparison.OrdinalIgnoreCase) == true)
                            .Sum(t => t.Total);
                            
                        YesterdayGCashTotal = allTransactions
                            .Where(t => t.PaymentMethod?.Equals("GCash", StringComparison.OrdinalIgnoreCase) == true)
                            .Sum(t => t.Total);
                            
                        YesterdayBankTotal = allTransactions
                            .Where(t => t.PaymentMethod?.Equals("Bank", StringComparison.OrdinalIgnoreCase) == true)
                            .Sum(t => t.Total);
                            
                        YesterdayTotalSales = allTransactions.Sum(t => t.Total);
                        
                        // Force UI update
                        OnPropertyChanged(nameof(DisplayCashTotal));
                        OnPropertyChanged(nameof(DisplayGCashTotal));
                        OnPropertyChanged(nameof(DisplayBankTotal));
                        OnPropertyChanged(nameof(DisplayTotalSales));
                    });
                    
                    System.Diagnostics.Debug.WriteLine($"Yesterday's totals - Cash: {cashTotal}, GCash: {gcashTotal}, Bank: {bankTotal}, Total: {totalSales}");
                    System.Diagnostics.Debug.WriteLine($"Cumulative totals - Cash: {YesterdayCashTotal}, GCash: {YesterdayGCashTotal}, Bank: {YesterdayBankTotal}, Total: {YesterdayTotalSales}");
                }
                else if (SelectedTimePeriod == "1 Week")
                {
                    var weekStart = today.AddDays(-7);
                    
                    // Get transactions for the past 7 days up to now
                    var weeklyTransactions = await _database.GetTransactionsByDateRangeAsync(weekStart, today.AddDays(1));
                    
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
                    
                    System.Diagnostics.Debug.WriteLine($"Weekly totals (last 7 days) - Cash: {WeeklyCashTotal}, GCash: {WeeklyGCashTotal}, Bank: {WeeklyBankTotal}, Total: {WeeklyTotalSales}");
                }
                else if (SelectedTimePeriod == "1 Month")
                {
                    var monthStart = today.AddMonths(-1);
                    
                    // Get transactions for the past 30 days up to now
                    var monthlyTransactions = await _database.GetTransactionsByDateRangeAsync(monthStart, today.AddDays(1));
                    
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
                    
                    System.Diagnostics.Debug.WriteLine($"Monthly totals (last 30 days) - Cash: {MonthlyCashTotal}, GCash: {MonthlyGCashTotal}, Bank: {MonthlyBankTotal}, Total: {MonthlyTotalSales}");
                }
                else if (SelectedTimePeriod == "All Time")
                {
                    // For "All Time" period, get all transactions from the beginning of time
                    var allStart = DateTime.MinValue;
                    var allEnd = DateTime.MaxValue;
                    
                    var allTransactions = await _database.GetTransactionsByDateRangeAsync(allStart, allEnd);
                    
                    // Update cumulative totals (used by "All Time" time period)
                    CumulativeCashTotal = allTransactions
                        .Where(t => t.PaymentMethod?.Equals("Cash", StringComparison.OrdinalIgnoreCase) == true)
                        .Sum(t => t.Total);
                    
                    CumulativeGCashTotal = allTransactions
                        .Where(t => t.PaymentMethod?.Equals("GCash", StringComparison.OrdinalIgnoreCase) == true)
                        .Sum(t => t.Total);
                    
                    CumulativeBankTotal = allTransactions
                        .Where(t => t.PaymentMethod?.Equals("Bank", StringComparison.OrdinalIgnoreCase) == true)
                        .Sum(t => t.Total);
                    
                    CumulativeTotalSales = allTransactions.Sum(t => t.Total);
                    
                    System.Diagnostics.Debug.WriteLine($"All-time totals - Cash: {CumulativeCashTotal}, GCash: {CumulativeGCashTotal}, Bank: {CumulativeBankTotal}, Total: {CumulativeTotalSales}");
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

        // PDF Download Commands
        [RelayCommand]
        private async Task DownloadWeeklyReport()
        {
            try
            {
                IsLoading = true;
                
                var today = DateTime.Today;
                var weekStart = today.AddDays(-7);
                var weekEnd = today.AddDays(1);
                
                // Get transactions for the week
                var transactions = await _database.GetTransactionsByDateRangeAsync(weekStart, weekEnd);
                
                // Generate PDF report
                var pdfService = new Services.PDFReportService();
                var filePath = await pdfService.GenerateWeeklyReportAsync(weekStart, weekEnd, transactions, TopItemsWeekly.ToList());
                
                // Show success message with file location
                await Application.Current.MainPage.DisplayAlert(
                    "Weekly Report Generated", 
                    $"Weekly sales report has been saved successfully!\n\nFile location: {filePath}\n\nThe report includes sales data and inventory deductions for the past week.\n\nTo find the file in your emulator:\n1. Open File Manager\n2. Go to Download folder\n3. Look for the Weekly_Report HTML file\n4. Open it in a browser to print or save as PDF", 
                    "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to generate weekly report: {ex.Message}", "OK");
                System.Diagnostics.Debug.WriteLine($"Error generating weekly report: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task DownloadMonthlyReport()
        {
            try
            {
                IsLoading = true;
                
                var today = DateTime.Today;
                var monthStart = today.AddMonths(-1);
                var monthEnd = today.AddDays(1);
                
                // Get transactions for the month
                var transactions = await _database.GetTransactionsByDateRangeAsync(monthStart, monthEnd);
                
                // Generate PDF report
                var pdfService = new Services.PDFReportService();
                var filePath = await pdfService.GenerateMonthlyReportAsync(monthStart, monthEnd, transactions, TopItemsMonthly.ToList());
                
                // Show success message with file location
                await Application.Current.MainPage.DisplayAlert(
                    "Monthly Report Generated", 
                    $"Monthly sales report has been saved successfully!\n\nFile location: {filePath}\n\nThe report includes sales data and inventory deductions for the past month.\n\nTo find the file in your emulator:\n1. Open File Manager\n2. Go to Download folder\n3. Look for the Monthly_Report HTML file\n4. Open it in a browser to print or save as PDF", 
                    "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to generate monthly report: {ex.Message}", "OK");
                System.Diagnostics.Debug.WriteLine($"Error generating monthly report: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
