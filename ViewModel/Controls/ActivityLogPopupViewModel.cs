using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Coftea_Capstone.Models;
using Coftea_Capstone.ViewModel.Others;

namespace Coftea_Capstone.ViewModel.Controls
{
    public partial class ActivityLogPopupViewModel : BaseViewModel
    {
        private readonly Database _database;
        private ObservableCollection<InventoryActivityLog> _allActivityLog = new();
        private string _selectedFilter = "All";

        [ObservableProperty]
        private bool isVisible;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string statusMessage = "Ready";

        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        private string selectedFilter = "All";

        [ObservableProperty]
        private ObservableCollection<InventoryActivityLog> filteredActivityLog = new();

        [ObservableProperty]
        private ObservableCollection<InventoryActivityLog> pagedActivityLog = new();

        [ObservableProperty]
        private int currentPage = 1;

        [ObservableProperty]
        private int pageSize = 30;

        public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)(FilteredActivityLog?.Count ?? 0) / PageSize));

        public bool CanGoToPreviousPage => CurrentPage > 1;
        
        public bool CanGoToNextPage => CurrentPage < TotalPages;

        [ObservableProperty]
        private bool isDateFilterVisible = false;

        [ObservableProperty]
        private bool isCloseConfirmationVisible;

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

        public ActivityLogPopupViewModel()
        {
            _database = new Database();
        }

        partial void OnIsVisibleChanged(bool value)
        {
            if (!value)
            {
                IsCloseConfirmationVisible = false;
            }
        }

        private void UpdatePagedItems()
        {
            try
            {
                if (FilteredActivityLog == null)
                {
                    PagedActivityLog = new ObservableCollection<InventoryActivityLog>();
                    StatusMessage = "No entries";
                    return;
                }

                var total = FilteredActivityLog.Count;
                var totalPages = TotalPages;
                var page = Math.Max(1, Math.Min(CurrentPage, totalPages));
                var skip = (page - 1) * PageSize;
                var pageItems = FilteredActivityLog.Skip(skip).Take(PageSize).ToList();

                // Re-number rows for the current page view
                for (int i = 0; i < pageItems.Count; i++)
                {
                    pageItems[i].RowNumber = skip + i + 1;
                }

                PagedActivityLog = new ObservableCollection<InventoryActivityLog>(pageItems);
                StatusMessage = $"Showing {skip + pageItems.Count} of {total} entries (Page {page}/{totalPages})";
                
                // Notify pagination button states
                OnPropertyChanged(nameof(CanGoToPreviousPage));
                OnPropertyChanged(nameof(CanGoToNextPage));
                OnPropertyChanged(nameof(TotalPages));
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error paginating: {ex.Message}";
            }
        }

        partial void OnCurrentPageChanged(int value)
        {
            UpdatePagedItems();
            OnPropertyChanged(nameof(CanGoToPreviousPage));
            OnPropertyChanged(nameof(CanGoToNextPage));
        }

        partial void OnPageSizeChanged(int value)
        {
            if (value <= 0) PageSize = 30;
            UpdatePagedItems();
        }

        [RelayCommand]
        public async Task LoadActivityLogAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading activity log...";

                var activityLog = await _database.GetRecentInventoryActivityAsync(100);
                _allActivityLog = new ObservableCollection<InventoryActivityLog>(activityLog);
                
                ApplyFilters();
                StatusMessage = $"Loaded {_allActivityLog.Count} activity entries";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading activity log: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"❌ Error loading activity log: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void FilterByAction(string action)
        {
            SelectedFilter = action;
            ApplyFilters();
        }

        [RelayCommand]
        private void Search()
        {
            ApplyFilters();
        }

        [RelayCommand]
        private async Task Refresh()
        {
            await LoadActivityLogAsync();
        }

        [RelayCommand]
        private void NextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage += 1;
            }
        }

        [RelayCommand]
        private void PrevPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage -= 1;
            }
        }

        [RelayCommand]
        private void Close()
        {
            if (IsCloseConfirmationVisible)
            {
                return;
            }

            IsCloseConfirmationVisible = true;
        }

        [RelayCommand]
        private void ConfirmClose()
        {
            IsCloseConfirmationVisible = false;
            IsVisible = false;
        }

        [RelayCommand]
        private void CancelClose()
        {
            IsCloseConfirmationVisible = false;
        }

        partial void OnSearchTextChanged(string value)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            try
            {
                var query = _allActivityLog.AsEnumerable();

                // Apply action filter
                if (!string.IsNullOrEmpty(SelectedFilter) && SelectedFilter != "All")
                {
                    // Filter by Action (DEDUCTED, ADDED, etc.)
                    query = query.Where(log => log.Action == SelectedFilter);
                }

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchTerm = SearchText.ToLowerInvariant();
                    query = query.Where(log => 
                        (log.ItemName?.ToLowerInvariant().Contains(searchTerm) ?? false) ||
                        (log.Reason?.ToLowerInvariant().Contains(searchTerm) ?? false) ||
                        (log.UserEmail?.ToLowerInvariant().Contains(searchTerm) ?? false) ||
                        (log.OrderId?.ToLowerInvariant().Contains(searchTerm) ?? false));
                }

                // Apply date filter
                if (_hasDateFilter)
                {
                    var startDate = FilterStartDate.Date;
                    var endDate = FilterEndDate.Date.AddDays(1).AddSeconds(-1); // Include entire end date
                    query = query.Where(log => log.Timestamp >= startDate && log.Timestamp <= endDate);
                }

                // Assign row numbers for table display
                var filteredList = query.ToList();
                for (int i = 0; i < filteredList.Count; i++)
                {
                    filteredList[i].RowNumber = i + 1;
                }

                FilteredActivityLog = new ObservableCollection<InventoryActivityLog>(filteredList);
                // Reset to first page on new filter
                CurrentPage = 1;
                UpdatePagedItems();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error applying filters: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"❌ Error applying filters: {ex.Message}");
            }
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
            IsDateFilterVisible = false;
            OnPropertyChanged(nameof(HasDateFilter));
            OnPropertyChanged(nameof(DateFilterText));
            ApplyFilters();
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
            ApplyFilters();
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
    }
}
