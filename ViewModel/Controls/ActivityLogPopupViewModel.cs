using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Coftea_Capstone.Models;
using Coftea_Capstone.ViewModel.Others;
using Coftea_Capstone.Services;

namespace Coftea_Capstone.ViewModel.Controls
{
    public partial class ActivityLogPopupViewModel : BaseViewModel
    {
        private readonly Database _database;
        private readonly IPDFReportService _pdfReportService;
        private ObservableCollection<InventoryActivityLog> _allActivityLog = new();
        private string _selectedFilter = "All";
        private bool _awaitingPrintConfirmation;

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
        private bool isPrintDialogVisible;

        [ObservableProperty]
        private string printDialogTitle = string.Empty;

        [ObservableProperty]
        private string printDialogMessage = string.Empty;

        [ObservableProperty]
        private string printDialogAcceptText = "Generate";

        [ObservableProperty]
        private string printDialogRejectText = "Cancel";

        [ObservableProperty]
        private bool printDialogHasReject = true;

        [ObservableProperty]
        private bool isPrintProcessing;

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
            _pdfReportService = new PDFReportService();
        }

        partial void OnIsVisibleChanged(bool value)
        {
            if (!value)
            {
                IsCloseConfirmationVisible = false;
                IsPrintDialogVisible = false;
                _awaitingPrintConfirmation = false;
                IsPrintProcessing = false;
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
            if (IsPrintDialogVisible)
            {
                return;
            }

            if (IsDateFilterVisible)
            {
                IsDateFilterVisible = false;
                return;
            }

            // Close the popup directly
            IsVisible = false;
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

        [RelayCommand]
        private void ShowPrintDialog()
        {
            if (IsPrintProcessing)
            {
                return;
            }

            var entryCount = FilteredActivityLog?.Count ?? 0;
            var rangeLabel = _hasDateFilter
                ? $"{FilterStartDate:MMMM dd, yyyy} - {FilterEndDate:MMMM dd, yyyy}"
                : "the current activity log view";

            PrintDialogTitle = "Export Activity Log?";
            PrintDialogMessage = entryCount > 0
                ? $"Generate a PDF summary for {entryCount} activity entries covering {rangeLabel}?"
                : $"There are no activity log entries in the current view. You can still export an empty report for {rangeLabel}.";
            PrintDialogAcceptText = "Generate";
            PrintDialogRejectText = "Cancel";
            PrintDialogHasReject = true;
            _awaitingPrintConfirmation = true;
            IsPrintDialogVisible = true;
        }

        [RelayCommand]
        private void RejectPrintDialog()
        {
            if (IsPrintProcessing)
            {
                return;
            }

            _awaitingPrintConfirmation = false;
            IsPrintDialogVisible = false;
        }

        [RelayCommand]
        private async Task AcceptPrintDialogAsync()
        {
            if (IsPrintProcessing)
            {
                return;
            }

            if (_awaitingPrintConfirmation)
            {
                await GenerateActivityLogPdfAsync();
                return;
            }

            IsPrintDialogVisible = false;
        }

        private async Task GenerateActivityLogPdfAsync()
        {
            try
            {
                IsPrintProcessing = true;
                PrintDialogHasReject = false;
                PrintDialogAcceptText = "Close";
                PrintDialogRejectText = string.Empty;
                _awaitingPrintConfirmation = false;

                PrintDialogTitle = "Exporting Activity Log…";
                PrintDialogMessage = "Please wait while we generate the PDF report.";

                var entries = FilteredActivityLog?.ToList() ?? new List<InventoryActivityLog>();

                if (entries.Count == 0)
                {
                    var rangeLabel = _hasDateFilter
                        ? $"{FilterStartDate:MMMM dd, yyyy} - {FilterEndDate:MMMM dd, yyyy}"
                        : "the selected criteria";

                    PrintDialogTitle = "No Entries Found";
                    PrintDialogMessage = $"There are no inventory activity entries to export for {rangeLabel}.";
                    return;
                }

                var startDate = _hasDateFilter
                    ? FilterStartDate.Date
                    : entries.Min(e => e?.Timestamp ?? DateTime.Now).Date;
                var endDate = _hasDateFilter
                    ? FilterEndDate.Date
                    : entries.Max(e => e?.Timestamp ?? DateTime.Now).Date;

                var filePath = await _pdfReportService.GenerateActivityLogPDFAsync(startDate, endDate, entries);

                PrintDialogTitle = "Activity Log Exported";
                PrintDialogMessage = $"PDF saved successfully at:\n{filePath}";
            }
            catch (Exception ex)
            {
                PrintDialogTitle = "Export Failed";
                PrintDialogMessage = $"We couldn’t generate the activity log PDF.\nReason: {ex.Message}";
            }
            finally
            {
                IsPrintProcessing = false;
            }
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
