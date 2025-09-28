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

                // Pull summary from service (real implementation can fetch from DB)
                var startDate = DateTime.Today.AddDays(-30); // Last 30 days
                var endDate = DateTime.Today.AddDays(1); // Include today
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

        private void ApplyCategoryFilter()
        {
            // Show only one category at a time
            if (SelectedCategory == "Milktea")
            {
                ShowCoffee = false;
                ShowMilktea = true;
                return;
            }

            // Any other selection (including Overview, Americano, etc.) shows Coffee-only
            ShowCoffee = true;
            ShowMilktea = false;
        }
    }
}
