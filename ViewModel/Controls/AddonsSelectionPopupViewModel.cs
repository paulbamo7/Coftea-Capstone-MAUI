using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Coftea_Capstone.Models;

namespace Coftea_Capstone.ViewModel.Controls
{
    public partial class AddonsSelectionPopupViewModel : ObservableObject, IDisposable
    {
        private readonly Database _database = new Database(); // Will use auto-detected host

        [ObservableProperty] private bool isAddonsPopupVisible;
        [ObservableProperty] private string selectedFilter = "All";
        [ObservableProperty] private string searchText = string.Empty; 

        public ObservableCollection<InventoryPageModel> AvailableAddons { get; set; } = new(); // Filtered addons displayed in UI
        private List<InventoryPageModel> _allAddons = new(); // Master list of all addons
        public ObservableCollection<InventoryPageModel> SelectedAddons { get; set; } = new(); // Currently selected addons

        public event Action<List<InventoryPageModel>> AddonsSelected; // List of selected addons

        // Preload/remember last selections when editing
        public void SyncSelectionsFrom(IEnumerable<InventoryPageModel> items)
        {
            if (items == null) return;
            var byId = items.Where(i => i != null).ToDictionary(i => i.itemID, i => i);
            foreach (var addon in AvailableAddons)
            {
                if (addon == null) continue;
                if (byId.TryGetValue(addon.itemID, out var existing))
                {
                    addon.IsSelected = existing.IsSelected || existing.AddonQuantity > 0;
                    addon.AddonQuantity = existing.AddonQuantity > 0 ? existing.AddonQuantity : (addon.IsSelected ? 1 : 0);
                    if (existing.InputAmount > 0) addon.InputAmount = existing.InputAmount;
                    if (!string.IsNullOrWhiteSpace(existing.InputUnit)) addon.InputUnit = existing.InputUnit;
                    if (existing.AddonPrice > 0) addon.AddonPrice = existing.AddonPrice;
                }
            }
        }

        [RelayCommand]
        public async Task OpenAddonsPopup() // Load addons and show popup
        {
            System.Diagnostics.Debug.WriteLine($"🔍 AddonsSelectionPopupViewModel.OpenAddonsPopup called");
            try
            {
                await LoadAddonsAsync();
                System.Diagnostics.Debug.WriteLine($"✅ Addons loaded, setting IsAddonsPopupVisible = true");
                IsAddonsPopupVisible = true;
                System.Diagnostics.Debug.WriteLine($"✅ IsAddonsPopupVisible set to: {IsAddonsPopupVisible}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error in OpenAddonsPopup: {ex.Message}");
            }
        }

        [RelayCommand]
        private void CloseAddonPopup() // Close popup without saving
        {
            IsAddonsPopupVisible = false;
        }

        [RelayCommand]
        private void ConfirmAddonSelection() // Confirm selected addons and notify parent
        {
            // Get selected addons and ensure they have proper quantities
            var selectedAddons = AvailableAddons.Where(a => a.IsSelected).ToList();
            
            foreach (var addon in selectedAddons)
            {
                // Ensure addon has proper quantity
                if (addon.AddonQuantity <= 0)
                {
                    addon.AddonQuantity = 1;
                }
                System.Diagnostics.Debug.WriteLine($"🔧 Confirming addon selection: {addon.itemName}, IsSelected: {addon.IsSelected}, AddonQuantity: {addon.AddonQuantity}");
            }
            
            // Notify parent with selected addons
            AddonsSelected?.Invoke(selectedAddons);
            
            IsAddonsPopupVisible = false;
        }

        [RelayCommand]
        public async Task LoadAddonsAsync() // Load addons from database
        {
            try
            {
                var inventoryItems = await _database.GetInventoryItemsAsync();
                _allAddons.Clear();
                AvailableAddons.Clear();
                SelectedAddons.Clear();

                // Only include items in the "Sinkers & etc." category
                var filtered = inventoryItems.Where(i => string.Equals((i.itemCategory ?? string.Empty).Trim(), "Sinkers & etc.", StringComparison.OrdinalIgnoreCase));

                foreach (var item in filtered)
                {
                    // Initialize addon-related defaults
                    item.AddonPrice = 0;
                    item.AddonUnit = item.DefaultUnit;
                    item.IsSelected = false;
                    item.InputAmount = 1;
                    item.InputUnit = item.DefaultUnit;
                    _allAddons.Add(item);
                }

                // Sort by name for easy browsing
                _allAddons = _allAddons.OrderBy(a => a.itemName).ToList();

                // Force available list to remain filtered to Sinkers & etc.; clear any category filter
                SelectedFilter = "All";
                SearchText = string.Empty;
                
                // Apply initial filter to populate AvailableAddons
                ApplyFilters();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading addons: {ex.Message}");
            }
        }

        [RelayCommand]
        private void FilterByCategory(string category) // Filter addons by category
        {
            SelectedFilter = category;
            ApplyFilters();
        }

        partial void OnSearchTextChanged(string value) // Apply search filter
        {
            ApplyFilters();
        }

        private void ApplyFilters() // Apply current filters to AvailableAddons
        {
            var query = _allAddons.AsEnumerable();

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
        private void ToggleAddonSelection(InventoryPageModel addon) // Toggle selection state of an addon
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
                // Set AddonQuantity to 1 when selected
                if (addon.AddonQuantity <= 0) addon.AddonQuantity = 1;
            }
            else
            {
                // Reset AddonQuantity when deselected
                addon.AddonQuantity = 0;
            }
        }

        public void Dispose() // Cleanup
        {
            try
            {
                AvailableAddons?.Clear();
                SelectedAddons?.Clear();
                AddonsSelected = null;
            }
            catch { }
        }
    }
}
