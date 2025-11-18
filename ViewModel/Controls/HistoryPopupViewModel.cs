using Coftea_Capstone.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Coftea_Capstone.ViewModel.Controls
{
    public partial class HistoryPopupViewModel : ObservableObject, IDisposable
    {
        private readonly Database _database = new(); // Will use auto-detected host

        [ObservableProperty] 
        private bool isHistoryVisible = false;

        [ObservableProperty] 
        private ObservableCollection<InventoryPageModel> selectedInventoryItems = new();

        [ObservableProperty]
        private ObservableCollection<TransactionHistoryModel> filteredTransactions = new();

        [ObservableProperty]
        private ObservableCollection<TransactionHistoryModel> allTransactions = new();

        [ObservableProperty]
        private string selectedFilter = "Today";

        [ObservableProperty]
        private bool isDateFilterVisible = false;

        [ObservableProperty]
        private DateTime filterStartDate = DateTime.Now.AddDays(-7);

        [ObservableProperty]
        private DateTime filterEndDate = DateTime.Now;

        public DateTime Today => DateTime.Today;

        private bool _hasDateFilter = false;

        public bool HasDateFilter => _hasDateFilter;
        
        public string DateFilterText => _hasDateFilter 
            ? $"Filtered: {FilterStartDate:MMM dd, yyyy} - {FilterEndDate:MMM dd, yyyy}"
            : string.Empty;

        public HistoryPopupViewModel()
        {
            // Set default filter to Today
            SelectedFilter = "Today";
        }

        public void ShowSelectedItems(ObservableCollection<InventoryPageModel> items) // Show inventory items popup
        {
            System.Diagnostics.Debug.WriteLine($"HistoryPopup.ShowSelectedItems called with {items?.Count ?? 0} items");
            SelectedInventoryItems = items ?? new ObservableCollection<InventoryPageModel>();
            ApplyFilter();
            IsHistoryVisible = true;
            System.Diagnostics.Debug.WriteLine($"IsHistoryVisible set to: {IsHistoryVisible}");
        }

        public async Task ShowHistory(ObservableCollection<TransactionHistoryModel> transactions = null) // Show transaction history popup
        {
            System.Diagnostics.Debug.WriteLine($"HistoryPopup.ShowHistory called");
            
            try
            {
                // Ensure a sane default; do not toggle while user is switching
                if (string.IsNullOrWhiteSpace(SelectedFilter))
                    SelectedFilter = "Today";
                
                // Load all transactions from database
                await LoadAllTransactionsAsync();
                
                // Also merge any in-memory transactions if provided, de-duplicated by
                // TransactionId+Date+DrinkName to avoid duplicates when IDs are temporary
                if (transactions != null && transactions.Any())
                {
                    foreach (var transaction in transactions)
                    {
                        bool exists = AllTransactions.Any(t =>
                            t.TransactionId == transaction.TransactionId ||
                            (t.TransactionDate == transaction.TransactionDate &&
                             string.Equals(t.DrinkName, transaction.DrinkName, StringComparison.OrdinalIgnoreCase) &&
                             t.Total == transaction.Total &&
                             t.Quantity == transaction.Quantity));

                        if (!exists)
                            AllTransactions.Add(transaction);
                    }
                }
                
                ApplyTransactionFilter();
                IsHistoryVisible = true;
                System.Diagnostics.Debug.WriteLine($"IsHistoryVisible set to: {IsHistoryVisible}, Total transactions: {AllTransactions.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading transaction history: {ex.Message}");
                // Show empty history on error
                AllTransactions.Clear();
                FilteredTransactions.Clear();
                IsHistoryVisible = true;
            }
        }

        [RelayCommand]
        private void CloseHistory() // Close the history popup
        {
            IsHistoryVisible = false;
        }

        [RelayCommand]
        private void FilterByCategory(string filter) // Filter inventory items by category
        {
            SelectedFilter = filter;
            ApplyFilter();
        }

        [RelayCommand]
        private async Task FilterByTimePeriod(string timePeriod) // Filter transactions by time period
        {
            System.Diagnostics.Debug.WriteLine($"FilterByTimePeriod called with: {timePeriod}");
            
            // Clear date filter when selecting a time period filter
            _hasDateFilter = false;
            OnPropertyChanged(nameof(HasDateFilter));
            OnPropertyChanged(nameof(DateFilterText));
            
            // Update the selected filter to trigger UI updates
            SelectedFilter = timePeriod?.Trim() ?? "Today";
            
            System.Diagnostics.Debug.WriteLine($"SelectedFilter set to: {SelectedFilter}");
            
            await LoadAllTransactionsAsync();
            ApplyTransactionFilter();
            
            System.Diagnostics.Debug.WriteLine($"Filtered transactions: {FilteredTransactions.Count}");
        }

        private void ApplyFilter() // Apply category filter to selected inventory items
        {
            if (SelectedInventoryItems == null || !SelectedInventoryItems.Any())
            {
                return;
            }

            // Apply category filtering based on SelectedFilter
            var filteredItems = SelectedInventoryItems.AsEnumerable();
            
            if (!string.IsNullOrWhiteSpace(SelectedFilter) && SelectedFilter != "All")
            {
                var filter = SelectedFilter?.Trim() ?? string.Empty;
                
                // Handle time period filters
                if (filter == "Today" || filter == "This Week" || filter == "This Month" || filter == "All Time")
                {
                    // For time period filters, we would need to filter by date
                    // This would require additional date filtering logic
                    // For now, show all items
                }
                else
                {
                    // Handle category filters
                    filteredItems = filteredItems.Where(item => 
                        string.Equals(item.itemCategory, filter, StringComparison.OrdinalIgnoreCase));
                }
            }

            // Update the collection with filtered items
            var filteredCollection = new ObservableCollection<InventoryPageModel>(filteredItems);
            SelectedInventoryItems.Clear();
            foreach (var item in filteredCollection)
            {
                SelectedInventoryItems.Add(item);
            }
        }


        [RelayCommand]
        private void RefreshHistory() // Refresh the transaction history based on the selected filter
        { 
            ApplyTransactionFilter();
        }

        private async Task LoadAllTransactionsAsync() // Load all transactions from the database based on the selected filter
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"LoadAllTransactionsAsync called with SelectedFilter: {SelectedFilter}");
                
                // Load transactions based on selected filter
                DateTime startDate;
                DateTime endDate = DateTime.Today.AddDays(1);
                
                switch (SelectedFilter)
                {
                    case "Today":
                        startDate = DateTime.Today;
                        break;
                    case "Yesterday":
                        startDate = DateTime.Today.AddDays(-1);
                        endDate = DateTime.Today;
                        break;
                    case "3 Days Ago":
                        startDate = DateTime.Today.AddDays(-3);
                        break;
                    case "1 Week Ago":
                        startDate = DateTime.Today.AddDays(-7);
                        break;
                    case "1 Month Ago":
                        startDate = DateTime.Today.AddDays(-30);
                        break;
                    case "All Time":
                        startDate = DateTime.MinValue.AddDays(1); // support earliest
                        endDate = DateTime.Today.AddDays(1);
                        break;
                    default:
                        startDate = DateTime.Today.AddDays(-30); // Default to 30 days
                        break;
                }

                var transactions = await _database.GetTransactionsByDateRangeAsync(startDate, endDate);
                
                AllTransactions.Clear();
                foreach (var transaction in transactions)
                {
                    AllTransactions.Add(transaction);
                }              
            }
            catch (Exception ex)
            {
                AllTransactions.Clear(); // Clear on error
            }
        }

        private void ApplyTransactionFilter() // Apply the selected time period filter to the transactions
        {
            System.Diagnostics.Debug.WriteLine($"ApplyTransactionFilter called with SelectedFilter: {SelectedFilter}");
            
            if (AllTransactions == null || !AllTransactions.Any())
            {
                FilteredTransactions.Clear();
                System.Diagnostics.Debug.WriteLine("No transactions to filter");
                return;
            }

            // Apply time period filtering
            var filteredItems = AllTransactions.AsEnumerable();
            
            // Apply date range filter if active
            if (_hasDateFilter)
            {
                var startDate = FilterStartDate.Date;
                var endDate = FilterEndDate.Date.AddDays(1).AddSeconds(-1); // Include entire end date
                filteredItems = filteredItems.Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate);
            }
            else if (!string.IsNullOrWhiteSpace(SelectedFilter) && SelectedFilter != "All Time")
            {
                var now = DateTime.Now;
                var filter = SelectedFilter?.Trim() ?? string.Empty;
                
                System.Diagnostics.Debug.WriteLine($"Applying filter: {filter}");
                
                switch (filter)
                {
                    case "Today":
                        filteredItems = filteredItems.Where(t => t.TransactionDate.Date == now.Date);
                        break;
                    case "Yesterday":
                        filteredItems = filteredItems.Where(t => t.TransactionDate.Date == now.Date.AddDays(-1));
                        break;
                    case "3 Days Ago":
                        filteredItems = filteredItems.Where(t => t.TransactionDate.Date == now.Date.AddDays(-3));
                        break;
                    case "1 Week Ago":
                        filteredItems = filteredItems.Where(t => t.TransactionDate.Date == now.Date.AddDays(-7));
                        break;
                    case "1 Month Ago":
                        filteredItems = filteredItems.Where(t => t.TransactionDate.Date == now.Date.AddDays(-30));
                        break;
                    case "All Time":
                        // Show all transactions
                        break;
                }
            }

            FilteredTransactions.Clear();
            foreach (var transaction in filteredItems)
            {
                FilteredTransactions.Add(transaction);
            }
            
            System.Diagnostics.Debug.WriteLine($"Filtered transactions count: {FilteredTransactions.Count}");
        }

        [RelayCommand]
        private void ShowDateFilter()
        {
            IsDateFilterVisible = true;
        }

        [RelayCommand]
        private void HideDateFilter()
        {
            IsDateFilterVisible = false;
        }

        [RelayCommand]
        private void ApplyDateFilter()
        {
            _hasDateFilter = true;
            SelectedFilter = "All Time"; // Clear time period filter when using date filter
            IsDateFilterVisible = false;
            OnPropertyChanged(nameof(HasDateFilter));
            OnPropertyChanged(nameof(DateFilterText));
            ApplyTransactionFilter();
        }

        [RelayCommand]
        private void ClearDateFilter()
        {
            _hasDateFilter = false;
            FilterStartDate = DateTime.Now.AddDays(-7);
            FilterEndDate = DateTime.Now;
            IsDateFilterVisible = false;
            OnPropertyChanged(nameof(HasDateFilter));
            OnPropertyChanged(nameof(DateFilterText));
            ApplyTransactionFilter();
        }

        [RelayCommand]
        private void SetToday()
        {
            FilterStartDate = DateTime.Now.Date;
            FilterEndDate = DateTime.Now.Date;
        }

        [RelayCommand]
        private void SetYesterday()
        {
            var yesterday = DateTime.Now.AddDays(-1).Date;
            FilterStartDate = yesterday;
            FilterEndDate = yesterday;
        }

        [RelayCommand]
        private void SetLast7Days()
        {
            FilterStartDate = DateTime.Now.AddDays(-7).Date;
            FilterEndDate = DateTime.Now.Date;
        }

        [RelayCommand]
        private void SetLast30Days()
        {
            FilterStartDate = DateTime.Now.AddDays(-30).Date;
            FilterEndDate = DateTime.Now.Date;
        }

        public void Dispose() // Cleanup
        {
            try
            {
                SelectedInventoryItems?.Clear();
                FilteredTransactions?.Clear();
                AllTransactions?.Clear();
            }
            catch { }
        }

    }
}
