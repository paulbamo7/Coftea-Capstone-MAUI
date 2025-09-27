using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using Coftea_Capstone.C_;
using Coftea_Capstone.Models;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Maui.Controls;

namespace Coftea_Capstone.ViewModel.Controls
{
    public partial class AddAddonsToItemViewModel : ObservableObject
    {
        private readonly Database _database = new Database(host: "0.0.0.0", database: "coftea_db", user: "root", password: "");

        [ObservableProperty] private bool isAddAddonsVisible;
        [ObservableProperty] private string selectedFilter = "All";
        [ObservableProperty] private string searchText = string.Empty;
        [ObservableProperty] private string selectedSort = "Name (A-Z)";

        public ObservableCollection<InventoryPageModel> InventoryItems { get; set; } = new();
        public ObservableCollection<InventoryPageModel> AllInventoryItems { get; set; } = new();
        public ObservableCollection<AddonItem> SelectedAddons { get; set; } = new();

        // Event to notify parent when addons are selected
        public event Action<ObservableCollection<AddonItem>> AddonsSelected;

        public AddAddonsToItemViewModel()
        {
            _ = LoadInventoryAsync();
        }

        [RelayCommand]
        public async Task LoadInventoryAsync()
        {
            var list = await _database.GetInventoryItemsAsync();
            AllInventoryItems.Clear();
            InventoryItems.Clear();
            foreach (var it in list)
            {
                // Cup items are automatically selected and not manually selectable
                if (it.IsCupItem)
                {
                    it.IsSelected = true;
                    // Don't add cups to addons collection - they're handled separately
                }
                else
                {
                    it.IsSelected = SelectedAddons.Any(a => a.Name == it.itemName);
                }
                
                AllInventoryItems.Add(it);
            }
            ApplyFilters();
        }

        [RelayCommand]
        private async Task RefreshInventory()
        {
            // Reset filters to show all
            SelectedFilter = "All";
            SearchText = string.Empty;
            await LoadInventoryAsync();
        }

        [RelayCommand]
        private void ToggleSelect(InventoryPageModel item)
        {
            if (item == null) return;
            
            // Prevent cup items from being deselected
            if (item.IsCupItem)
            {
                return; // Cup items are always selected and cannot be toggled
            }
            
            var existing = SelectedAddons.FirstOrDefault(a => a.Name == item.itemName);
            if (existing != null)
            {
                SelectedAddons.Remove(existing);
                item.IsSelected = false;
                return;
            }
            
            // Add to selected addons
            SelectedAddons.Add(new AddonItem 
            { 
                Name = item.itemName, 
                Category = item.itemCategory,
                Price = 0.0m // Default price, user can edit in preview
            });
            item.IsSelected = true;
        }

        [RelayCommand]
        private void FilterByCategory(string category)
        {
            SelectedFilter = category;
            ApplyFilters();
        }

        [RelayCommand]
        private void SelectAllVisible()
        {
            foreach (var item in InventoryItems)
            {
                if (!item.IsSelected && !item.IsCupItem)
                {
                    SelectedAddons.Add(new AddonItem 
                    { 
                        Name = item.itemName, 
                        Category = item.itemCategory,
                        Price = 0.0m
                    });
                    item.IsSelected = true;
                }
            }
        }

        [RelayCommand]
        private void CloseAddAddons()
        {
            IsAddAddonsVisible = false;
        }

        [RelayCommand]
        private void AddSelectedAddons()
        {
            // Notify parent with selected addons
            AddonsSelected?.Invoke(SelectedAddons);
            IsAddAddonsVisible = false;
        }

        private void ApplyFilters()
        {
            var query = AllInventoryItems.AsEnumerable();

            // Always exclude cup items from the filtered collection
            query = query.Where(i => !i.IsCupItem);

            if (!string.IsNullOrWhiteSpace(SelectedFilter) && SelectedFilter != "All")
            {
                var filter = SelectedFilter?.Trim() ?? string.Empty;
                if (string.Equals(filter, "Addons", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(i => string.Equals(i.itemCategory?.Trim(), "Addons", StringComparison.OrdinalIgnoreCase)
                                           || string.Equals(i.itemCategory?.Trim(), "Sinkers", StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    query = query.Where(i => string.Equals(i.itemCategory, filter, StringComparison.OrdinalIgnoreCase));
                }
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var term = SearchText.Trim();
                query = query.Where(i => (i.itemName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                                       || (i.itemCategory?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            query = SelectedSort switch
            {
                "Name (Z-A)" => query.OrderByDescending(i => i.itemName),
                "Quantity (Low to High)" => query.OrderBy(i => i.itemQuantity),
                "Quantity (High to Low)" => query.OrderByDescending(i => i.itemQuantity),
                _ => query.OrderBy(i => i.itemName)
            };

            InventoryItems.Clear();
            foreach (var it in query) InventoryItems.Add(it);
        }

        partial void OnSearchTextChanged(string value)
        {
            ApplyFilters();
        }

        partial void OnSelectedSortChanged(string value)
        {
            ApplyFilters();
        }

        public partial class AddonItem : ObservableObject
        {
            [ObservableProperty] private string name;
            [ObservableProperty] private string category;
            [ObservableProperty] private decimal price;
        }
    }
}
