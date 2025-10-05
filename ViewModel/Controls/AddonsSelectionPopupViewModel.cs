using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Coftea_Capstone.Models;

namespace Coftea_Capstone.ViewModel.Controls
{
    public partial class AddonsSelectionPopupViewModel : ObservableObject
    {
        private readonly Database _database = new Database(); // Will use auto-detected host

        [ObservableProperty] private bool isAddonsPopupVisible;
        [ObservableProperty] private string selectedFilter = "All";
        [ObservableProperty] private string searchText = string.Empty;

        public ObservableCollection<InventoryPageModel> AvailableAddons { get; set; } = new();
        public ObservableCollection<InventoryPageModel> SelectedAddons { get; set; } = new();

        // Event to notify parent when addons are selected
        public event Action<List<InventoryPageModel>> AddonsSelected;

        [RelayCommand]
        public async Task OpenAddonsPopup()
        {
            await LoadAddonsAsync();
            IsAddonsPopupVisible = true;
        }

        [RelayCommand]
        private void CloseAddonsPopupCommand()
        {
            IsAddonsPopupVisible = false;
        }

        [RelayCommand]
        private void ConfirmAddonSelectionCommand()
        {
            // Get selected addons
            var selectedAddons = AvailableAddons.Where(a => a.IsSelected).ToList();
            
            // Notify parent with selected addons
            AddonsSelected?.Invoke(selectedAddons);
            
            IsAddonsPopupVisible = false;
        }

        [RelayCommand]
        private async Task LoadAddonsAsync()
        {
            try
            {
                var inventoryItems = await _database.GetInventoryItemsAsync();
                AvailableAddons.Clear();
                SelectedAddons.Clear();

                foreach (var item in inventoryItems)
                {
                    var category = (item.itemCategory ?? string.Empty).Trim();
                    bool isAddonCategory = string.Equals(category, "Addons", StringComparison.OrdinalIgnoreCase);
                    bool isSinkersCategory = category.Contains("Sinker", StringComparison.OrdinalIgnoreCase)
                                             || string.Equals(category, "Sinkers & etc.", StringComparison.OrdinalIgnoreCase);

                    if (isAddonCategory || isSinkersCategory)
                    {
                        // Initialize addon properties
                        item.AddonPrice = 0;
                        item.AddonUnit = item.DefaultUnit;
                        item.IsSelected = false; // Start unchecked as requested
                        item.InputAmount = 1; // Default amount
                        item.InputUnit = item.DefaultUnit; // Default unit
                        
                        AvailableAddons.Add(item);
                    }
                }

                // Sort by name for easy browsing
                var sorted = AvailableAddons.OrderBy(a => a.itemName).ToList();
                AvailableAddons.Clear();
                foreach (var addon in sorted)
                {
                    AvailableAddons.Add(addon);
                }

                ApplyFilters();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading addons: {ex.Message}");
            }
        }

        [RelayCommand]
        private void FilterByCategory(string category)
        {
            SelectedFilter = category;
            ApplyFilters();
        }

        partial void OnSearchTextChanged(string value)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            var query = AvailableAddons.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SelectedFilter) && SelectedFilter != "All")
            {
                var filter = SelectedFilter?.Trim() ?? string.Empty;
                if (string.Equals(filter, "Addons", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(i => string.Equals(i.itemCategory?.Trim(), "Addons", StringComparison.OrdinalIgnoreCase));
                }
                else if (string.Equals(filter, "Sinkers", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(i => string.Equals(i.itemCategory?.Trim(), "Sinkers", StringComparison.OrdinalIgnoreCase)
                                           || string.Equals(i.itemCategory?.Trim(), "Sinkers & etc.", StringComparison.OrdinalIgnoreCase));
                }
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var term = SearchText.Trim();
                query = query.Where(i => (i.itemName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                                       || (i.itemCategory?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            // Update the collection
            var filteredList = query.ToList();
            AvailableAddons.Clear();
            foreach (var item in filteredList)
            {
                AvailableAddons.Add(item);
            }
        }

        [RelayCommand]
        private void ToggleAddonSelection(InventoryPageModel addon)
        {
            if (addon == null) return;
            
            addon.IsSelected = !addon.IsSelected;
            
            if (addon.IsSelected)
            {
                // Set default values when selected
                if (addon.InputAmount <= 0) addon.InputAmount = 1;
                if (string.IsNullOrWhiteSpace(addon.InputUnit)) addon.InputUnit = addon.DefaultUnit;
                if (addon.AddonPrice <= 0) addon.AddonPrice = 0;
                if (string.IsNullOrWhiteSpace(addon.AddonUnit)) addon.AddonUnit = addon.DefaultUnit;
            }
        }
    }
}
