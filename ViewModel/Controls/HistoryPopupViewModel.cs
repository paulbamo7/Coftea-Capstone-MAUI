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
                    case "2 Weeks Ago":
                        startDate = DateTime.Today.AddDays(-14);
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
                if (transactions != null && transactions.Any())
                {
                    foreach (var transaction in transactions)
                    {
                        AllTransactions.Add(transaction);
                    }
                    System.Diagnostics.Debug.WriteLine($"Loaded {transactions.Count} transactions from database");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No transactions found in the database for the selected period");
                }              
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading transactions: {ex.Message}");
                AllTransactions.Clear();
                // Optionally show an error message to the user
                await Application.Current.MainPage.DisplayAlert("Error", "Failed to load transaction history. Please try again later.", "OK");
            }
        }

        private async void ApplyTransactionFilter() // Apply the selected time period filter to the transactions
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"ApplyTransactionFilter called with SelectedFilter: {SelectedFilter}");
                
                if (AllTransactions == null || !AllTransactions.Any())
                {
                    FilteredTransactions.Clear();
                    System.Diagnostics.Debug.WriteLine("No transactions to filter");
                    
                    // Check if we should show a message to the user
                    if (IsHistoryVisible)
                    {
                        await Application.Current.MainPage.DisplayAlert("No Transactions", 
                            "No transaction history found for the selected period.", "OK");
                    }
                    return;
                }

                // Apply time period filtering
                var filteredItems = AllTransactions.AsEnumerable();
                
                if (!string.IsNullOrWhiteSpace(SelectedFilter) && SelectedFilter != "All Time")
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
                        case "2 Weeks Ago":
                            filteredItems = filteredItems.Where(t => t.TransactionDate.Date == now.Date.AddDays(-14));
                            break;
                        case "1 Month Ago":
                            filteredItems = filteredItems.Where(t => t.TransactionDate.Date == now.Date.AddDays(-30));
                            break;
                        case "All Time":
                            // Show all transactions
                            break;
                    }
                }

                var filteredList = filteredItems.ToList();
                
                FilteredTransactions.Clear();
                foreach (var transaction in filteredList)
                {
                    FilteredTransactions.Add(transaction);
                }
                
                System.Diagnostics.Debug.WriteLine($"Filtered transactions count: {FilteredTransactions.Count}");
                
                // Show a message if no transactions match the filter
                if (FilteredTransactions.Count == 0 && IsHistoryVisible)
                {
                    await Application.Current.MainPage.DisplayAlert("No Transactions", 
                        "No transactions found for the selected time period.", "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ApplyTransactionFilter: {ex.Message}");
                FilteredTransactions.Clear();
                
                if (IsHistoryVisible)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", 
                        "An error occurred while filtering transactions.", "OK");
                }
            }
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
