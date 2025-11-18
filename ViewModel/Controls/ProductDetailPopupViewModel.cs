using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Coftea_Capstone.Models;
using Coftea_Capstone.C_;

namespace Coftea_Capstone.ViewModel.Controls
{
    public partial class ProductDetailPopupViewModel : ObservableObject
    {
        private readonly Database _database = new Database();

        [ObservableProperty]
        private bool isVisible;

        [ObservableProperty]
        private string productName = string.Empty;

        [ObservableProperty]
        private string productCategory = string.Empty;

        // Today's stats
        [ObservableProperty]
        private int todayOrders;

        [ObservableProperty]
        private decimal todaySales;

        // Week's stats
        [ObservableProperty]
        private int weekOrders;

        [ObservableProperty]
        private decimal weekSales;

        // Month's stats
        [ObservableProperty]
        private int monthOrders;

        [ObservableProperty]
        private decimal monthSales;

        [ObservableProperty]
        private bool isLoading;

		// Period context (optional: use Sales Report selected dates)
		[ObservableProperty]
		private DateTime? periodStartDate;

		[ObservableProperty]
		private DateTime? periodEndDate;

		[ObservableProperty]
		private int periodOrders;

        [ObservableProperty]
        private decimal periodSales;

        // Date range display properties
        public string TodayDateRange
        {
            get
            {
                var today = DateTime.Today;
                return today.ToString("MMM dd, yyyy");
            }
        }

        public string WeekDateRange
        {
            get
            {
                var today = DateTime.Today;
                // Calculate Monday of current week
                var daysUntilMonday = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
                var monday = today.AddDays(-daysUntilMonday);
                return $"{monday:MMM dd} - {today:MMM dd, yyyy}";
            }
        }

        public string MonthDateRange
        {
            get
            {
                var today = DateTime.Today;
                var firstOfMonth = new DateTime(today.Year, today.Month, 1);
                return $"{firstOfMonth:MMM dd} - {today:MMM dd, yyyy}";
            }
        }

        [RelayCommand]
        private void Close()
        {
            IsVisible = false;
        }

		public async Task LoadProductDetailsAsync(string productName, DateTime? start = null, DateTime? end = null)
        {
            try
            {
                IsLoading = true;
                ProductName = productName;
				PeriodStartDate = start;
				PeriodEndDate = end;

                var today = DateTime.Today;
                var todayStart = today;
                var todayEnd = today.AddDays(1);

                // Calculate week starting from Monday
                var daysUntilMonday = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
                var weekStart = today.AddDays(-daysUntilMonday);
                var weekEnd = today.AddDays(1);

                // Calculate month starting from the 1st
                var monthStart = new DateTime(today.Year, today.Month, 1);
                var monthEnd = today.AddDays(1);

                // Get transactions only for the date ranges we need (optimize performance)
                var earliestDate = monthStart; // We need at least a month back
                var latestDate = todayEnd;
                
				var allTransactions = await _database.GetTransactionsByDateRangeAsync(earliestDate, latestDate);

                // Filter by product name
                var productTransactions = allTransactions
                    .Where(t => t?.DrinkName?.Equals(productName, StringComparison.OrdinalIgnoreCase) == true)
                    .ToList();

				// Calculate today's stats
                var todayTransactions = productTransactions
                    .Where(t => t.TransactionDate >= todayStart && t.TransactionDate < todayEnd)
                    .ToList();
                TodayOrders = todayTransactions.Sum(t => t.Quantity > 0 ? t.Quantity : 1);
                TodaySales = todayTransactions.Sum(t => t.Total);

                // Calculate week's stats
                var weekTransactions = productTransactions
                    .Where(t => t.TransactionDate >= weekStart && t.TransactionDate < weekEnd)
                    .ToList();
                WeekOrders = weekTransactions.Sum(t => t.Quantity > 0 ? t.Quantity : 1);
                WeekSales = weekTransactions.Sum(t => t.Total);

                // Calculate month's stats
                var monthTransactions = productTransactions
                    .Where(t => t.TransactionDate >= monthStart && t.TransactionDate < monthEnd)
                    .ToList();
                MonthOrders = monthTransactions.Sum(t => t.Quantity > 0 ? t.Quantity : 1);
                MonthSales = monthTransactions.Sum(t => t.Total);

				// Calculate selected period (if provided)
				if (start.HasValue && end.HasValue)
				{
					var periodTx = productTransactions
						.Where(t => t.TransactionDate >= start.Value && t.TransactionDate < end.Value)
						.ToList();
					PeriodOrders = periodTx.Sum(t => t.Quantity > 0 ? t.Quantity : 1);
					PeriodSales = periodTx.Sum(t => t.Total);
				}

                // Get product category
                var products = await _database.GetProductsAsyncCached();
                var product = products.FirstOrDefault(p => p.ProductName?.Equals(productName, StringComparison.OrdinalIgnoreCase) == true);
                ProductCategory = product?.Category ?? "Unknown";

                // Notify date range properties changed
                OnPropertyChanged(nameof(TodayDateRange));
                OnPropertyChanged(nameof(WeekDateRange));
                OnPropertyChanged(nameof(MonthDateRange));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading product details: {ex.Message}");
                TodayOrders = 0;
                TodaySales = 0;
                WeekOrders = 0;
                WeekSales = 0;
                MonthOrders = 0;
                MonthSales = 0;
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}

