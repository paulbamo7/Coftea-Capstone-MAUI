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

        // Date range selection
        public IReadOnlyList<string> AvailableDateRanges { get; } = new List<string>
        {
            "Today",
            "Yesterday",
            "7 Days",
            "14 Days",
            "30 Days"
        };

        [ObservableProperty]
        private string selectedDateRange = "Today";

        // Category-specific data collections
        [ObservableProperty]
        private ObservableCollection<TrendItem> currentCategoryToday = new();

        [ObservableProperty]
        private ObservableCollection<TrendItem> currentCategoryWeekly = new();

        [ObservableProperty]
        private int currentCategoryTodayOrders;

        [ObservableProperty]
        private int currentCategoryWeeklyOrders;

        // Current category display properties
        [ObservableProperty]
        private string currentCategoryTitle = "Top 3 Popular Coffee Today";

        [ObservableProperty]
        private string currentCategoryWeeklyTitle = "Top Coffee Trends Weekly";

        [ObservableProperty]
        private DateTime currentDateTime = DateTime.Now;

        public SalesReportPageViewModel(SettingsPopUpViewModel settingsPopup, Services.ISalesReportService salesReportService = null)
        {
            _database = new Database(
                host: "192.168.1.4",
                database: "coftea_db",
                user: "maui",
                password: "password123"
            );
            SettingsPopup = settingsPopup;
            _salesReportService = salesReportService ?? new Services.DatabaseSalesReportService();
        }

        private RetryConnectionPopupViewModel GetRetryConnectionPopup()
        {
            return ((App)Application.Current).RetryConnectionPopup;
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

                // Calculate date range based on selected date range
                var (startDate, endDate) = GetDateRange(SelectedDateRange);

                // Pull summary from service with date range
                var summary = await _salesReportService.GetSummaryAsync(startDate, endDate);

                SalesReports = new ObservableCollection<SalesReportPageModel>(summary.Reports ?? new List<SalesReportPageModel>());

                ActiveDays = summary.ActiveDays;
                MostBoughtToday = summary.MostBoughtToday;
                TrendingToday = summary.TrendingToday;
                TotalSalesToday = summary.TotalSalesToday;
                TotalOrdersToday = summary.TotalOrdersToday;
                TotalOrdersThisWeek = summary.TotalOrdersThisWeek;

                TopCoffeeToday = new ObservableCollection<TrendItem>(summary.TopCoffeeToday ?? new List<TrendItem>());
                TopMilkteaToday = new ObservableCollection<TrendItem>(summary.TopMilkteaToday ?? new List<TrendItem>());
                TopCoffeeWeekly = new ObservableCollection<TrendItem>(summary.TopCoffeeWeekly ?? new List<TrendItem>());
                TopMilkteaWeekly = new ObservableCollection<TrendItem>(summary.TopMilkteaWeekly ?? new List<TrendItem>());

                StatusMessage = SalesReports.Any() ? "Sales reports loaded successfully." : "No sales reports found.";

                // Notify derived properties
                OnPropertyChanged(nameof(TopCoffeeTodayOrders));
                OnPropertyChanged(nameof(TopMilkteaTodayOrders));

                // Apply initial category filter
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

        private (DateTime startDate, DateTime endDate) GetDateRange(string dateRange)
        {
            var today = DateTime.Today;
            var now = DateTime.Now;

            return dateRange switch
            {
                "Today" => (today, now),
                "Yesterday" => (today.AddDays(-1), today.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59)),
                "7 Days" => (today.AddDays(-6), now),
                "14 Days" => (today.AddDays(-13), now),
                "30 Days" => (today.AddDays(-29), now),
                _ => (today, now)
            };
        }

        partial void OnSelectedCategoryChanged(string value)
        {
            ApplyCategoryFilter();
        }

        partial void OnSelectedDateRangeChanged(string value)
        {
            // Reload data when date range changes
            _ = LoadDataAsync();
        }

        [RelayCommand]
        private void SelectCategory(string category)
        {
            SelectedCategory = category;
        }

        [RelayCommand]
        private void SelectDateRange(string dateRange)
        {
            SelectedDateRange = dateRange;
        }

        private void ApplyCategoryFilter()
        {
            // Update current category data based on selection
            switch (SelectedCategory)
            {
                case "Overview":
                    CurrentCategoryToday = new ObservableCollection<TrendItem>(TopCoffeeToday.Take(3));
                    CurrentCategoryWeekly = new ObservableCollection<TrendItem>(TopCoffeeWeekly);
                    CurrentCategoryTitle = "Top 3 Popular Coffee Today";
                    CurrentCategoryWeeklyTitle = "Top Coffee Trends Weekly";
                    break;
                case "Frappe":
                    CurrentCategoryToday = GetCategoryData("Frappe", TopCoffeeToday);
                    CurrentCategoryWeekly = GetCategoryData("Frappe", TopCoffeeWeekly);
                    CurrentCategoryTitle = "Top 3 Popular Frappe Today";
                    CurrentCategoryWeeklyTitle = "Top Frappe Trends Weekly";
                    break;
                case "Fruit/Soda":
                    CurrentCategoryToday = GetCategoryData("Fruit/Soda", TopCoffeeToday);
                    CurrentCategoryWeekly = GetCategoryData("Fruit/Soda", TopCoffeeWeekly);
                    CurrentCategoryTitle = "Top 3 Popular Fruit/Soda Today";
                    CurrentCategoryWeeklyTitle = "Top Fruit/Soda Trends Weekly";
                    break;
                case "Milktea":
                    CurrentCategoryToday = new ObservableCollection<TrendItem>(TopMilkteaToday.Take(3));
                    CurrentCategoryWeekly = new ObservableCollection<TrendItem>(TopMilkteaWeekly);
                    CurrentCategoryTitle = "Top 3 Popular Milktea Today";
                    CurrentCategoryWeeklyTitle = "Top Milktea Trends Weekly";
                    break;
                case "Coffee":
                    CurrentCategoryToday = GetCategoryData("Coffee", TopCoffeeToday);
                    CurrentCategoryWeekly = GetCategoryData("Coffee", TopCoffeeWeekly);
                    CurrentCategoryTitle = "Top 3 Popular Coffee Today";
                    CurrentCategoryWeeklyTitle = "Top Coffee Trends Weekly";
                    break;
            }

            // Update order counts
            CurrentCategoryTodayOrders = CurrentCategoryToday?.Sum(i => i.Count) ?? 0;
            CurrentCategoryWeeklyOrders = CurrentCategoryWeekly?.Sum(i => i.Count) ?? 0;
        }

        private ObservableCollection<TrendItem> GetCategoryData(string category, ObservableCollection<TrendItem> sourceData)
        {
            // Filter data based on category (this would be more sophisticated in a real implementation)
            var filteredData = sourceData.Where(item => 
                item.Name.Contains(category, StringComparison.OrdinalIgnoreCase) ||
                (category == "Frappe" && item.Name.Contains("Frappe", StringComparison.OrdinalIgnoreCase)) ||
                (category == "Fruit/Soda" && (item.Name.Contains("Soda", StringComparison.OrdinalIgnoreCase))) ||
                (category == "Coffee" && (item.Name.Contains("Americano", StringComparison.OrdinalIgnoreCase) || 
                                         item.Name.Contains("Latte", StringComparison.OrdinalIgnoreCase) || 
                                         item.Name.Contains("Cappuccino", StringComparison.OrdinalIgnoreCase) ||
                                         item.Name.Contains("Salted Caramel", StringComparison.OrdinalIgnoreCase) ||
                                         item.Name.Contains("Vanilla Blanca", StringComparison.OrdinalIgnoreCase) ||
                                         item.Name.Contains("Chocolate", StringComparison.OrdinalIgnoreCase) ||
                                         item.Name.Contains("Java Chip", StringComparison.OrdinalIgnoreCase)))
            ).Take(3);

            return new ObservableCollection<TrendItem>(filteredData);
        }
    }
}
