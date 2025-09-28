using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using Coftea_Capstone.ViewModel.Controls;
using Coftea_Capstone.C_;
using Coftea_Capstone.Models;
using Coftea_Capstone.Views.Pages;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Maui.Controls;

namespace Coftea_Capstone.ViewModel.Controls
{
    public partial class ConnectPOSItemToInventoryViewModel : ObservableObject
    {
        private readonly Database _database = new Database(host: "0.0.0.0", database: "coftea_db", user: "root", password: "");

        // Event to trigger AddProduct in parent VM
        public event Action ConfirmPreviewRequested;
        [ObservableProperty] private bool isConnectPOSToInventoryVisible;
        [ObservableProperty] private bool isPreviewVisible;
        [ObservableProperty] private bool isInputIngredientsVisible;

        public ObservableCollection<Ingredient> Ingredients { get; set; } = new();
        public ObservableCollection<InventoryPageModel> InventoryItems { get; set; } = new();
        public ObservableCollection<InventoryPageModel> AllInventoryItems { get; set; } = new();
        
        // Addon functionality
        public ObservableCollection<InventoryPageModel> AvailableAddons { get; set; } = new();
        public ObservableCollection<InventoryPageModel> SelectedAddons { get; set; } = new();
        [ObservableProperty] private bool isAddonPopupVisible;

        [ObservableProperty] private string selectedFilter = "All";
        [ObservableProperty] private string searchText = string.Empty;
        [ObservableProperty] private string selectedSort = "Name (A-Z)";

        // Preview-bound properties (populated from parent VM)
        [ObservableProperty] private ImageSource selectedImageSource;
        [ObservableProperty] private string productName;
        [ObservableProperty] private string selectedCategory;
        [ObservableProperty] private decimal smallPrice;
        [ObservableProperty] private decimal mediumPrice;
        [ObservableProperty] private decimal largePrice;

        // Size selection for ingredient inputs
        [ObservableProperty] private string selectedSize = "Small";
        [ObservableProperty] private string productDescription;

        // Event to notify AddItem popup
        public event Action ReturnRequested;

        [RelayCommand]
        private void ReturnToAddItemToPOS()
        {
            IsConnectPOSToInventoryVisible = false;
            ReturnRequested?.Invoke();
        }

        [RelayCommand]
        private void CloseConnectPOSToInventory()
        {
            IsConnectPOSToInventoryVisible = false;
        }
        [RelayCommand]
        private void ShowPreview()
        {
            IsPreviewVisible = true;
        }

        [RelayCommand]
        private void ClosePreview()
        {
            IsPreviewVisible = false;
        }

        [RelayCommand]
        private void ConfirmPreview()
        {
            // finalize and close any overlays
            IsConnectPOSToInventoryVisible = false;
            IsInputIngredientsVisible = false;
            IsPreviewVisible = false;
            ConfirmPreviewRequested?.Invoke();
        }

        [RelayCommand]
        private void OpenInputIngredients()
        {
            IsConnectPOSToInventoryVisible = false;
            IsPreviewVisible = false;
            IsInputIngredientsVisible = true;
        }

        [RelayCommand]
        private void BackToInventorySelection()
        {
            IsInputIngredientsVisible = false;
            IsConnectPOSToInventoryVisible = true;
        }


        [RelayCommand]
        private async Task OpenInventorySelection()
        {
            IsConnectPOSToInventoryVisible = false;
            // Navigate to Inventory page to allow selection there
            await Application.Current.MainPage.Navigation.PushAsync(new Inventory());
        }

        [RelayCommand]
        public async Task LoadInventoryAsync()
        {
            var list = await _database.GetInventoryItemsAsync();
            AllInventoryItems.Clear();
            InventoryItems.Clear();
            foreach (var it in list)
            {
                it.IsSelected = Ingredients.Any(g => g.Name == it.itemName);
                // Initialize per-size units from inventory default if not set
                var defUnit = it.HasUnit ? it.unitOfMeasurement : "pcs";
                if (string.IsNullOrWhiteSpace(it.InputUnitSmall)) it.InputUnitSmall = defUnit;
                if (string.IsNullOrWhiteSpace(it.InputUnitMedium)) it.InputUnitMedium = defUnit;
                if (string.IsNullOrWhiteSpace(it.InputUnitLarge)) it.InputUnitLarge = defUnit;

                // Auto-select size-related supplies by default: cups and straws
                var nameLower = (it.itemName ?? string.Empty).Trim().ToLowerInvariant();
                var categoryLower = (it.itemCategory ?? string.Empty).Trim().ToLowerInvariant();
                bool isCup = nameLower.Contains("cup") && (categoryLower == "supplies" || categoryLower == "ingredients");
                bool isStraw = nameLower.Contains("straw") && (categoryLower == "supplies" || categoryLower == "ingredients");
                if (isCup || isStraw)
                {
                    // default 1 piece per size
                    it.InputAmountSmall = 1;
                    it.InputUnitSmall = "pcs";
                    it.InputAmountMedium = 1;
                    it.InputUnitMedium = "pcs";
                    it.InputAmountLarge = 1;
                    it.InputUnitLarge = "pcs";
                    it.InputAmount = 1;
                    it.InputUnit = "pcs";
                    it.IsSelected = true;
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
        private void SetSize(string size)
        {
            if (string.IsNullOrWhiteSpace(size)) return;
            SelectedSize = size;
        }

        [RelayCommand]
        private void ToggleSelect(InventoryPageModel item)
        {
            if (item == null) return;
            var existing = Ingredients.FirstOrDefault(i => i.Name == item.itemName);
            if (existing != null)
            {
                Ingredients.Remove(existing);
                item.IsSelected = false;
                return;
            }
            // Initialize default unit based on inventory definition
            var initUnit = item.HasUnit ? item.unitOfMeasurement : "pcs";
            Ingredients.Add(new Ingredient { Name = item.itemName, Amount = 1, Unit = initUnit, Selected = true });
            item.IsSelected = true;
        }

        private void ApplyFilters()
        {
            var query = AllInventoryItems.AsEnumerable();

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

        partial void OnSelectedSortChanged(string value)
        {
            ApplyFilters();
        }

        [RelayCommand]
        private void SelectAllVisible()
        {
            foreach (var item in InventoryItems)
            {
                if (!item.IsSelected)
                {
                    Ingredients.Add(new Ingredient { Name = item.itemName, Amount = 1, Selected = true });
                    item.IsSelected = true;
                }
            }
        }

        [RelayCommand]
        private async Task AddAddons()
        {
            // Load addons from inventory (items categorized as "Addons")
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
                    item.IsSelected = false;
                    AvailableAddons.Add(item);
                }
            }
            // Keep list easy to scan
            var sorted = AvailableAddons.OrderBy(a => a.itemName).ToList();
            AvailableAddons.Clear();
            foreach (var a in sorted) AvailableAddons.Add(a);
        }

        // Popup controls
        [RelayCommand]
        private async Task OpenAddonPopup()
        {
            if (AvailableAddons.Count == 0)
            {
                await AddAddons();
            }
            IsAddonPopupVisible = true;
        }

        [RelayCommand]
        private void CloseAddonPopup()
        {
            IsAddonPopupVisible = false;
        }

        [RelayCommand]
        private void ConfirmAddonSelection()
        {
            // Sync selected addons into InventoryItems and SelectedAddons
            SelectedAddons.Clear();
            foreach (var addon in AvailableAddons)
            {
                if (!addon.IsSelected) continue;
                SelectedAddons.Add(addon);

                var existing = InventoryItems.FirstOrDefault(i => i.itemID == addon.itemID);
                if (existing == null)
                {
                    existing = addon;
                    InventoryItems.Add(existing);
                }
                // Ensure it shows in preview and carry over editable fields
                existing.IsSelected = true;
                existing.AddonPrice = addon.AddonPrice;
                existing.AddonUnit = addon.AddonUnit;
            }

            IsAddonPopupVisible = false;
        }

        [RelayCommand]
        private void ToggleAddonSelection(InventoryPageModel addon)
        {
            if (addon == null) return;
            
            if (addon.IsSelected)
            {
                // Remove from selected addons
                var existing = SelectedAddons.FirstOrDefault(a => a.itemID == addon.itemID);
                if (existing != null)
                {
                    SelectedAddons.Remove(existing);
                }
            }
            else
            {
                // Add to selected addons
                SelectedAddons.Add(addon);
            }
            
            addon.IsSelected = !addon.IsSelected;
        }
        public partial class Ingredient : ObservableObject
        {
            [ObservableProperty] private string name;
            [ObservableProperty] private double amount;
            [ObservableProperty] private string unit;
            [ObservableProperty] private bool selected;
        }
    }
}
