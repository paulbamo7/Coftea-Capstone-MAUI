using Coftea_Capstone.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Coftea_Capstone.ViewModel.Controls
{
    public partial class HistoryPopupViewModel : ObservableObject
    {
        [ObservableProperty] 
        private bool isHistoryVisible = false;

        [ObservableProperty] 
        private ObservableCollection<InventoryPageModel> selectedInventoryItems = new();

        [ObservableProperty]
        private ObservableCollection<TransactionHistoryModel> filteredTransactions = new();

        [ObservableProperty]
        private string selectedFilter = "All Time";

        [ObservableProperty]
        private int currentPage = 1;

        [ObservableProperty]
        private int totalPages = 1;

        [ObservableProperty]
        private int itemsPerPage = 10;

        public ObservableCollection<string> FilterOptions { get; } = new()
        {
            "All",
            "Ingredients",
            "Addons", 
            "Supplies"
        };

        public HistoryPopupViewModel()
        {
        }

        public void ShowSelectedItems(ObservableCollection<InventoryPageModel> items)
        {
            System.Diagnostics.Debug.WriteLine($"HistoryPopup.ShowSelectedItems called with {items?.Count ?? 0} items");
            SelectedInventoryItems = items ?? new ObservableCollection<InventoryPageModel>();
            ApplyFilter();
            IsHistoryVisible = true;
            System.Diagnostics.Debug.WriteLine($"IsHistoryVisible set to: {IsHistoryVisible}");
        }

        public void ShowHistory(ObservableCollection<TransactionHistoryModel> transactions)
        {
            System.Diagnostics.Debug.WriteLine($"HistoryPopup.ShowHistory called with {transactions?.Count ?? 0} transactions");
            // Populate the filtered transactions collection directly
            FilteredTransactions.Clear();
            foreach (var transaction in transactions ?? new ObservableCollection<TransactionHistoryModel>())
            {
                FilteredTransactions.Add(transaction);
            }
            ApplyTransactionFilter();
            IsHistoryVisible = true;
            System.Diagnostics.Debug.WriteLine($"IsHistoryVisible set to: {IsHistoryVisible}");
        }

        [RelayCommand]
        private void CloseHistory()
        {
            IsHistoryVisible = false;
        }

        [RelayCommand]
        private void FilterByCategory(string filter)
        {
            SelectedFilter = filter;
            ApplyFilter();
        }

        [RelayCommand]
        private void FilterByTimePeriod(string timePeriod)
        {
            SelectedFilter = timePeriod;
            ApplyTransactionFilter();
        }

        private void ApplyFilter()
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

            CalculatePagination();
        }

        private void CalculatePagination()
        {
            if (SelectedInventoryItems == null || !SelectedInventoryItems.Any())
            {
                TotalPages = 1;
                CurrentPage = 1;
                return;
            }

            TotalPages = (int)Math.Ceiling((double)SelectedInventoryItems.Count / ItemsPerPage);
            if (CurrentPage > TotalPages)
                CurrentPage = TotalPages;
        }

        [RelayCommand]
        private void GoToPage(object pageNumberObj)
        {
            if (int.TryParse(pageNumberObj?.ToString(), out int pageNumber) && 
                pageNumber >= 1 && pageNumber <= TotalPages)
            {
                CurrentPage = pageNumber;
            }
        }

        [RelayCommand]
        private void RefreshHistory()
        {
            ApplyTransactionFilter();
        }

        private void ApplyTransactionFilter()
        {
            if (FilteredTransactions == null || !FilteredTransactions.Any())
            {
                return;
            }

            // Get all transactions from the app's shared collection
            var app = (App)Application.Current;
            var allTransactions = app?.Transactions ?? new ObservableCollection<TransactionHistoryModel>();
            
            // Apply time period filtering
            var filteredItems = allTransactions.AsEnumerable();
            
            if (!string.IsNullOrWhiteSpace(SelectedFilter) && SelectedFilter != "All Time")
            {
                var now = DateTime.Now;
                var filter = SelectedFilter?.Trim() ?? string.Empty;
                
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

            // Update the filtered transactions collection
            FilteredTransactions.Clear();
            foreach (var transaction in filteredItems)
            {
                FilteredTransactions.Add(transaction);
            }

            CalculatePagination();
        }

        [RelayCommand]
        private void ExportHistory()
        {
            // TODO: Implement export functionality for selected inventory items
            // This could export to CSV, PDF, etc.
        }
    }
}
