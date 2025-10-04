using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
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

        // Size button visibility properties based on category
        public bool IsSmallSizeVisible => !string.Equals(SelectedCategory, "Coffee", StringComparison.OrdinalIgnoreCase);
        public bool IsMediumSizeVisible => true; // Always visible
        public bool IsLargeSizeVisible => true; // Always visible

        partial void OnSelectedCategoryChanged(string value)
        {
            // Notify visibility change for size buttons
            OnPropertyChanged(nameof(IsSmallSizeVisible));
            OnPropertyChanged(nameof(IsMediumSizeVisible));
            OnPropertyChanged(nameof(IsLargeSizeVisible));

            // If category is Coffee and current size is Small, switch to Medium
            if (string.Equals(value, "Coffee", StringComparison.OrdinalIgnoreCase) && 
                string.Equals(SelectedSize, "Small", StringComparison.OrdinalIgnoreCase))
            {
                SetSize("Medium");
            }
        }

        // Size selection for ingredient inputs
        [ObservableProperty] private string selectedSize = "Small";
        [ObservableProperty] private string productDescription;
        // True when any inventory item is marked selected; used to toggle inputs visibility
        public bool HasSelectedIngredients => AllInventoryItems.Any(i => i.IsSelected && !IsCupOrStraw(i));

        // Only the inventory items that are selected; used for ingredient inputs
        public IEnumerable<InventoryPageModel> SelectedInventoryItems => AllInventoryItems.Where(i => i.IsSelected);
        
        // Only the inventory items that are selected and NOT cups/straws; used for ingredient inputs display
        public ObservableCollection<InventoryPageModel> SelectedIngredientsOnly { get; set; } = new();
        
        private bool IsCupOrStraw(InventoryPageModel item)
        {
            if (item == null) return false;
            var nameLower = (item.itemName ?? string.Empty).Trim().ToLowerInvariant();
            var categoryLower = (item.itemCategory ?? string.Empty).Trim().ToLowerInvariant();
            bool isCup = nameLower.Contains("cup") && (categoryLower == "supplies" || categoryLower == "ingredients");
            bool isStraw = nameLower.Contains("straw") && (categoryLower == "supplies" || categoryLower == "ingredients");
            return isCup || isStraw;
        }

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
        private void BackToInventorySelection()
        {
            IsInputIngredientsVisible = false;
            IsConnectPOSToInventoryVisible = true;
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
        private void ShowSelectedItemsHistory()
        {
            // Convert selected items to ObservableCollection for HistoryPopup
            var selectedItems = new ObservableCollection<InventoryPageModel>(SelectedInventoryItems);
            
            // Find the HistoryPopupViewModel and show the selected items
            var app = (App)Application.Current;
            if (app?.HistoryPopup != null)
            {
                app.HistoryPopup.ShowSelectedItems(selectedItems);
            }
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
            
            // Subscribe to property changes for all items
            AllInventoryItems.CollectionChanged += OnInventoryItemsCollectionChanged;
            
            // Separate cups/straws from other items
            var cupsAndStraws = new List<InventoryPageModel>();
            var otherItems = new List<InventoryPageModel>();
            
            foreach (var it in list)
            {
                // Start with all items unchecked by default
                it.IsSelected = false;
                // Initialize per-size units from inventory default if not set
                var defUnit = it.HasUnit ? it.unitOfMeasurement : "pcs";
                if (string.IsNullOrWhiteSpace(it.InputUnitSmall)) it.InputUnitSmall = defUnit;
                if (string.IsNullOrWhiteSpace(it.InputUnitMedium)) it.InputUnitMedium = defUnit;
                if (string.IsNullOrWhiteSpace(it.InputUnitLarge)) it.InputUnitLarge = defUnit;

                // No need to set per-size amount since it's always 1 serving

                // Check if it's a cup or straw
                var nameLower = (it.itemName ?? string.Empty).Trim().ToLowerInvariant();
                var categoryLower = (it.itemCategory ?? string.Empty).Trim().ToLowerInvariant();
                bool isCup = nameLower.Contains("cup") && (categoryLower == "supplies" || categoryLower == "ingredients");
                bool isStraw = nameLower.Contains("straw") && (categoryLower == "supplies" || categoryLower == "ingredients");
                
                if (isCup)
                {
                    // Create size-specific cup entries
                    var smallCup = new InventoryPageModel
                    {
                        itemID = it.itemID + 1000, // Unique ID for small cup
                        itemName = "Small Cup",
                        itemCategory = it.itemCategory,
                        itemQuantity = it.itemQuantity,
                        unitOfMeasurement = it.unitOfMeasurement,
                        minimumQuantity = it.minimumQuantity,
                        ImageSet = it.ImageSet,
                        itemDescription = it.itemDescription,
                        InputAmountSmall = 1,
                        InputUnitSmall = "pcs",
                        InputAmountMedium = 0,
                        InputUnitMedium = "pcs",
                        InputAmountLarge = 0,
                        InputUnitLarge = "pcs",
                        InputAmount = 1,
                        InputUnit = "pcs",
                        IsSelected = false
                    };
                    
                    var mediumCup = new InventoryPageModel
                    {
                        itemID = it.itemID + 2000, // Unique ID for medium cup
                        itemName = "Medium Cup",
                        itemCategory = it.itemCategory,
                        itemQuantity = it.itemQuantity,
                        unitOfMeasurement = it.unitOfMeasurement,
                        minimumQuantity = it.minimumQuantity,
                        ImageSet = it.ImageSet,
                        itemDescription = it.itemDescription,
                        InputAmountSmall = 0,
                        InputUnitSmall = "pcs",
                        InputAmountMedium = 1,
                        InputUnitMedium = "pcs",
                        InputAmountLarge = 0,
                        InputUnitLarge = "pcs",
                        InputAmount = 1,
                        InputUnit = "pcs",
                        IsSelected = false
                    };
                    
                    var largeCup = new InventoryPageModel
                    {
                        itemID = it.itemID + 3000, // Unique ID for large cup
                        itemName = "Large Cup",
                        itemCategory = it.itemCategory,
                        itemQuantity = it.itemQuantity,
                        unitOfMeasurement = it.unitOfMeasurement,
                        minimumQuantity = it.minimumQuantity,
                        ImageSet = it.ImageSet,
                        itemDescription = it.itemDescription,
                        InputAmountSmall = 0,
                        InputUnitSmall = "pcs",
                        InputAmountMedium = 0,
                        InputUnitMedium = "pcs",
                        InputAmountLarge = 1,
                        InputUnitLarge = "pcs",
                        InputAmount = 1,
                        InputUnit = "pcs",
                        IsSelected = false
                    };
                    
                    cupsAndStraws.Add(smallCup);
                    cupsAndStraws.Add(mediumCup);
                    cupsAndStraws.Add(largeCup);
                }
                else if (isStraw)
                {
                    // Straws are used for all sizes
                    it.InputAmountSmall = 1;
                    it.InputUnitSmall = "pcs";
                    it.InputAmountMedium = 1;
                    it.InputUnitMedium = "pcs";
                    it.InputAmountLarge = 1;
                    it.InputUnitLarge = "pcs";
                    it.InputAmount = 1;
                    it.InputUnit = "pcs";
                    it.IsSelected = false;
                    cupsAndStraws.Add(it);
                }
                else
                {
                    otherItems.Add(it);
                }
            }
            
            // Add cups and straws to AllInventoryItems but not to the display list
            foreach (var item in cupsAndStraws)
            {
                AllInventoryItems.Add(item);
            }
            
            // Add other items to both lists
            foreach (var item in otherItems)
            {
                AllInventoryItems.Add(item);
            }
            
            ApplyFilters();
            UpdateSelectedIngredientsOnly();
            OnPropertyChanged(nameof(HasSelectedIngredients));
            OnPropertyChanged(nameof(SelectedInventoryItems));
            OnPropertyChanged(nameof(SelectedIngredientsOnly));
        }

        private void OnInventoryItemsCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Subscribe to property changes for new items
            if (e.NewItems != null)
            {
                foreach (InventoryPageModel item in e.NewItems)
                {
                    item.PropertyChanged += OnItemPropertyChanged;
                }
            }
            
            // Unsubscribe from removed items
            if (e.OldItems != null)
            {
                foreach (InventoryPageModel item in e.OldItems)
                {
                    item.PropertyChanged -= OnItemPropertyChanged;
                }
            }
        }

        private void OnItemPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(InventoryPageModel.IsSelected))
            {
                // Update the SelectedIngredientsOnly collection when IsSelected changes
                UpdateSelectedIngredientsOnly();
                OnPropertyChanged(nameof(HasSelectedIngredients));
                OnPropertyChanged(nameof(SelectedInventoryItems));
                OnPropertyChanged(nameof(SelectedIngredientsOnly));
            }
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
            
            // Save current input amounts to the current size before switching
            foreach (var item in SelectedIngredientsOnly)
            {
                // Save the current InputAmount to the appropriate size-specific property
                switch (item.SelectedSize)
                {
                    case "Small":
                        item.InputAmountSmall = item.InputAmount;
                        break;
                    case "Medium":
                        item.InputAmountMedium = item.InputAmount;
                        break;
                    case "Large":
                        item.InputAmountLarge = item.InputAmount;
                        break;
                }
            }
            
            SelectedSize = size;
            
            // Update all selected ingredients to use the new size
            foreach (var item in SelectedIngredientsOnly)
            {
                item.SelectedSize = size;
                
                // Update Amount Used based on the selected size
                item.InputAmount = size switch
                {
                    "Small" => item.InputAmountSmall,
                    "Medium" => item.InputAmountMedium,
                    "Large" => item.InputAmountLarge,
                    _ => 1
                };
                
                // No need to update per-size amount since it's always 1 serving
            }
        }

        [RelayCommand]
        private void SelectSize(InventoryPageModel item)
        {
            if (item == null) return;

            // Cycle through sizes: Small -> Medium -> Large -> Small
            item.SelectedSize = item.SelectedSize switch
            {
                "Small" => "Medium",
                "Medium" => "Large", 
                "Large" => "Small",
                _ => "Small"
            };

            // No need to update per-size amount since it's always 1 serving
        }

        [RelayCommand]
        private void ToggleSelect(InventoryPageModel item)
        {
            if (item == null) return;
            
            // Find the item in AllInventoryItems to update its selection status
            var allItem = AllInventoryItems.FirstOrDefault(i => i.itemID == item.itemID);
            if (allItem == null) return;
            
            var existing = Ingredients.FirstOrDefault(i => i.Name == allItem.itemName);
            if (existing != null)
            {
                Ingredients.Remove(existing);
                allItem.IsSelected = false;
            }
            else
            {
                // Initialize default unit and amounts based on inventory definition
                var initUnit = allItem.HasUnit ? allItem.unitOfMeasurement : "pcs";
                
                // Set default amounts to 1 for all sizes
                allItem.InputAmountSmall = 1;
                allItem.InputAmountMedium = 1;
                allItem.InputAmountLarge = 1;
                allItem.InputAmount = 1;
                
                // Set default units
                allItem.InputUnitSmall = initUnit;
                allItem.InputUnitMedium = initUnit;
                allItem.InputUnitLarge = initUnit;
                allItem.InputUnit = initUnit;
                
                Ingredients.Add(new Ingredient { Name = allItem.itemName, Amount = 1, Unit = initUnit, Selected = true });
                allItem.IsSelected = true;
            }
            
            // Update the filtered list to reflect changes
            ApplyFilters();
            
            // Update SelectedIngredientsOnly collection
            UpdateSelectedIngredientsOnly();
            
            // Notify property changes
            OnPropertyChanged(nameof(HasSelectedIngredients));
            OnPropertyChanged(nameof(SelectedInventoryItems));
            OnPropertyChanged(nameof(SelectedIngredientsOnly));
        }

        private void UpdateSelectedIngredientsOnly()
        {
            SelectedIngredientsOnly.Clear();
            var selected = AllInventoryItems.Where(i => i.IsSelected).ToList();
            
            foreach (var item in selected)
            {
                SelectedIngredientsOnly.Add(item);
            }
        }

        private void ApplyFilters()
        {
            var query = AllInventoryItems.AsEnumerable();

            // Exclude cups and straws from display
            query = query.Where(i => {
                var nameLower = (i.itemName ?? string.Empty).Trim().ToLowerInvariant();
                var categoryLower = (i.itemCategory ?? string.Empty).Trim().ToLowerInvariant();
                bool isCup = nameLower.Contains("cup") && (categoryLower == "supplies" || categoryLower == "ingredients");
                bool isStraw = nameLower.Contains("straw") && (categoryLower == "supplies" || categoryLower == "ingredients");
                return !isCup && !isStraw;
            });

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
            OnPropertyChanged(nameof(SelectedInventoryItems));
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
                // Find the corresponding item in AllInventoryItems
                var allItem = AllInventoryItems.FirstOrDefault(i => i.itemID == item.itemID);
                if (allItem != null && !allItem.IsSelected)
                {
                    Ingredients.Add(new Ingredient { Name = allItem.itemName, Amount = 1, Selected = true });
                    allItem.IsSelected = true;
                }
            }
            OnPropertyChanged(nameof(HasSelectedIngredients));
            OnPropertyChanged(nameof(SelectedInventoryItems));
            OnPropertyChanged(nameof(SelectedIngredientsOnly));
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
            OnPropertyChanged(nameof(HasSelectedIngredients));
            OnPropertyChanged(nameof(SelectedInventoryItems));
            OnPropertyChanged(nameof(SelectedIngredientsOnly));
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
