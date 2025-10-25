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

        public ActivityLogPopupViewModel()
        {
            _database = new Database();
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
        private void Close()
        {
            IsVisible = false;
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

                FilteredActivityLog = new ObservableCollection<InventoryActivityLog>(query);
                StatusMessage = $"Showing {FilteredActivityLog.Count} of {_allActivityLog.Count} entries";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error applying filters: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"❌ Error applying filters: {ex.Message}");
            }
        }
    }
}
