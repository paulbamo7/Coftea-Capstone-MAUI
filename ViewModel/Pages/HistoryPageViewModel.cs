using Coftea_Capstone.C_;
using Coftea_Capstone.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Coftea_Capstone.ViewModel
{
    public partial class HistoryPageViewModel : ObservableObject
    {
        private readonly Database _database;

        [ObservableProperty]
        private ObservableCollection<TransactionHistoryModel> allTransactions = new();

        [ObservableProperty]
        private ObservableCollection<TransactionHistoryModel> filteredTransactions = new();

        [ObservableProperty]
        private string selectedFilter = "Today";

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string statusMessage;

        [ObservableProperty]
        private bool hasError;

        [ObservableProperty]
        private int currentPage = 1;

        [ObservableProperty]
        private int totalPages = 1;

        [ObservableProperty]
        private int itemsPerPage = 10;

        public ObservableCollection<string> FilterOptions { get; } = new()
        {
            "Today",
            "Yesterday", 
            "3 Days Ago",
            "1 Week Ago",
            "2 Weeks Ago",
            "1 Month Ago",
            "All Time"
        };

        public HistoryPageViewModel()
        {
            _database = new Database(host: "0.0.0.0", database: "coftea_db", user: "root", password: "");
        }

        public async Task InitializeAsync()
        {
            await LoadTransactionHistoryAsync();
        }

        [RelayCommand]
        private async Task LoadTransactionHistoryAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading transaction history...";
                HasError = false;

                // TODO: Load actual transaction data from database
                // For now, start with empty collection
                AllTransactions = new ObservableCollection<TransactionHistoryModel>();
                
                ApplyFilter();
            }
            catch (Exception ex)
            {
                HasError = true;
                StatusMessage = $"Failed to load transaction history: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void FilterByTimePeriod(string filter)
        {
            SelectedFilter = filter;
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (AllTransactions == null || !AllTransactions.Any())
            {
                FilteredTransactions.Clear();
                return;
            }

            var filtered = SelectedFilter switch
            {
                "Today" => AllTransactions.Where(t => t.TransactionDate.Date == DateTime.Today),
                "Yesterday" => AllTransactions.Where(t => t.TransactionDate.Date == DateTime.Today.AddDays(-1)),
                "3 Days Ago" => AllTransactions.Where(t => t.TransactionDate.Date >= DateTime.Today.AddDays(-3)),
                "1 Week Ago" => AllTransactions.Where(t => t.TransactionDate.Date >= DateTime.Today.AddDays(-7)),
                "2 Weeks Ago" => AllTransactions.Where(t => t.TransactionDate.Date >= DateTime.Today.AddDays(-14)),
                "1 Month Ago" => AllTransactions.Where(t => t.TransactionDate.Date >= DateTime.Today.AddDays(-30)),
                "All Time" => AllTransactions,
                _ => AllTransactions.Where(t => t.TransactionDate.Date == DateTime.Today)
            };

            FilteredTransactions.Clear();
            foreach (var transaction in filtered.OrderByDescending(t => t.TransactionDate))
            {
                FilteredTransactions.Add(transaction);
            }

            CalculatePagination();
        }

        private void CalculatePagination()
        {
            if (FilteredTransactions == null || !FilteredTransactions.Any())
            {
                TotalPages = 1;
                CurrentPage = 1;
                return;
            }

            TotalPages = (int)Math.Ceiling((double)FilteredTransactions.Count / ItemsPerPage);
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
            ApplyFilter();
        }

        [RelayCommand]
        private void ExportHistory()
        {
            // TODO: Implement export functionality
            // This could export to CSV, PDF, etc.
        }
    }
}
