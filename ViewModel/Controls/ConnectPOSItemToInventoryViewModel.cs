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
        private readonly Database _database = new Database(); // Will use auto-detected host

        // Event to trigger AddProduct in parent VM
        public event Action ConfirmPreviewRequested;

        public ConnectPOSItemToInventoryViewModel()
        {
            // Initialize addons popup
            AddonsPopup = new AddonsSelectionPopupViewModel();
            AddonsPopup.AddonsSelected += OnAddonsSelected;
        }

        private void OnAddonsSelected(List<InventoryPageModel> selectedAddons)
        {
            // Update SelectedAddons with the new selection
            SelectedAddons.Clear();
            foreach (var addon in selectedAddons)
            {
                SelectedAddons.Add(addon);

                // Sync into InventoryItems so save path can persist them
                var existing = InventoryItems.FirstOrDefault(i => i.itemID == addon.itemID);
                if (existing == null)
                {
                    existing = addon;
                    InventoryItems.Add(existing);
                }

                // Mark as selected and carry over editable fields
                existing.IsSelected = true;
                existing.AddonPrice = addon.AddonPrice;
                existing.AddonUnit = addon.AddonUnit;
                existing.InputAmount = addon.InputAmount > 0 ? addon.InputAmount : 1;
                existing.InputUnit = string.IsNullOrWhiteSpace(addon.InputUnit) ? addon.DefaultUnit : addon.InputUnit;
            }
            
            // Notify UI of changes
            OnPropertyChanged(nameof(SelectedAddons));
            OnPropertyChanged(nameof(SelectedInventoryItems));
            OnPropertyChanged(nameof(SelectedIngredientsOnly));

            // Ensure the popup closes and we return to the preview overlay
            IsAddonPopupVisible = false;
            IsPreviewVisible = true;
        }
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
        
        // Addons popup
        [ObservableProperty] private AddonsSelectionPopupViewModel addonsPopup;

        [ObservableProperty] private string selectedFilter = "All";
        [ObservableProperty] private string searchText = string.Empty;
        [ObservableProperty] private string selectedSort = "Name (A-Z)";

        // Preview-bound properties (populated from parent VM)
        [ObservableProperty] private ImageSource selectedImageSource;
        [ObservableProperty] private string productName;
        [ObservableProperty] private string selectedCategory;
        [ObservableProperty] private string smallPrice = string.Empty;
        [ObservableProperty] private string mediumPrice = string.Empty;
        [ObservableProperty] private string largePrice = string.Empty;

        // Dynamic cup sizes from database
        [ObservableProperty] private string smallSizeText = "Small";
        [ObservableProperty] private string mediumSizeText = "Medium";
        [ObservableProperty] private string largeSizeText = "Large";

        // Size button visibility properties based on category
        public bool IsSmallSizeVisible => IsCoffeeCategory(SelectedCategory);
        public bool IsMediumSizeVisible => true; // Always visible
        public bool IsLargeSizeVisible => true; // Always visible

        // Price visibility (used by PreviewPOSItem)
        public bool IsSmallPriceVisible => IsCoffeeCategory(SelectedCategory);
        public bool IsMediumPriceVisible => true; // Always visible
        public bool IsLargePriceVisible => true; // Always visible

        partial void OnSelectedCategoryChanged(string value)
        {
            // Notify visibility change for size buttons and price fields
            OnPropertyChanged(nameof(IsSmallSizeVisible));
            OnPropertyChanged(nameof(IsMediumSizeVisible));
            OnPropertyChanged(nameof(IsLargeSizeVisible));
            OnPropertyChanged(nameof(IsSmallPriceVisible));
            OnPropertyChanged(nameof(IsMediumPriceVisible));
            OnPropertyChanged(nameof(IsLargePriceVisible));

            // If category is NOT Coffee and current size is Small, switch to Medium
            if (!IsCoffeeCategory(value) && 
                string.Equals(SelectedSize, "Small", StringComparison.OrdinalIgnoreCase))
            {
                SetSize("Medium");
            }

            // Ensure preview re-evaluates price visibility
            OnPropertyChanged(nameof(IsSmallPriceVisible));
            OnPropertyChanged(nameof(IsMediumPriceVisible));
            OnPropertyChanged(nameof(IsLargePriceVisible));
        }

        private static bool IsCoffeeCategory(string category)
        {
            var c = (category ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(c)) return false;
            return string.Equals(c, "Coffee", StringComparison.OrdinalIgnoreCase)
                || string.Equals(c, "Americano", StringComparison.OrdinalIgnoreCase)
                || string.Equals(c, "Latte", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsAddonCategory(string category)
        {
            var c = (category ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(c)) return false;
            return string.Equals(c, "Addons", StringComparison.OrdinalIgnoreCase)
                || string.Equals(c, "Sinkers & etc.", StringComparison.OrdinalIgnoreCase)
                || c.Contains("Sinker", StringComparison.OrdinalIgnoreCase);
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
            // Ensure any addon popup is closed when returning
            IsAddonPopupVisible = false;
            if (AddonsPopup != null) AddonsPopup.IsAddonsPopupVisible = false;
            // Show parent overlay again
            var app = (App)Application.Current;
            (app?.POSVM?.AddItemToPOSViewModel)?.SetIsAddItemToPOSVisibleTrue();
            ReturnRequested?.Invoke();
        }

        [RelayCommand]
        private void CloseConnectPOSToInventory()
        {
            IsConnectPOSToInventoryVisible = false;
            // Ensure any addon popup is closed
            IsAddonPopupVisible = false;
            if (AddonsPopup != null) AddonsPopup.IsAddonsPopupVisible = false;
        }

        [RelayCommand]
        private void BackToInventorySelection()
        {
            IsInputIngredientsVisible = false;
            IsConnectPOSToInventoryVisible = true;
            // Ensure any addon popup is closed
            IsAddonPopupVisible = false;
            if (AddonsPopup != null) AddonsPopup.IsAddonsPopupVisible = false;
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
            // Ensure addons popup is closed when preview closes
            IsAddonPopupVisible = false;
            if (AddonsPopup != null) AddonsPopup.IsAddonsPopupVisible = false;
        }

        [RelayCommand]
        private void ConfirmPreview()
        {
            // finalize and close any overlays
            IsConnectPOSToInventoryVisible = false;
            IsInputIngredientsVisible = false;
            IsPreviewVisible = false;
            // Ensure addons popup is closed on confirm
            IsAddonPopupVisible = false;
            if (AddonsPopup != null) AddonsPopup.IsAddonsPopupVisible = false;
            ConfirmPreviewRequested?.Invoke();
        }

        // Addon quantity adjusters used by PreviewPOSItem
        [RelayCommand]
        private void IncreaseAddonQty(InventoryPageModel addon)
        {
            if (addon == null) return;
            addon.AddonQuantity = Math.Max(1, addon.AddonQuantity + 1);
        }

        [RelayCommand]
        private void DecreaseAddonQty(InventoryPageModel addon)
        {
            if (addon == null) return;
            var next = addon.AddonQuantity - 1;
            addon.AddonQuantity = next < 1 ? 1 : next;
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
            
            // Load cup sizes from database
            await LoadCupSizesAsync();
            
            // Preserve current selection state
            var currentSelections = new Dictionary<int, SelectionState>();
            if (AllInventoryItems != null)
            {
                foreach (var item in AllInventoryItems.Where(i => i.IsSelected))
                {
                    currentSelections[item.itemID] = new SelectionState
                    {
                        IsSelected = item.IsSelected,
                        InputAmount = item.InputAmount,
                        InputUnit = item.InputUnit,
                        InputAmountSmall = item.InputAmountSmall,
                        InputUnitSmall = item.InputUnitSmall,
                        InputAmountMedium = item.InputAmountMedium,
                        InputUnitMedium = item.InputUnitMedium,
                        InputAmountLarge = item.InputAmountLarge,
                        InputUnitLarge = item.InputUnitLarge
                    };
                }
            }
            
            AllInventoryItems.Clear();
            InventoryItems.Clear();
            
            // Subscribe to property changes for all items
            AllInventoryItems.CollectionChanged += OnInventoryItemsCollectionChanged;
            
            // Separate cups/straws from other items
            var cupsAndStraws = new List<InventoryPageModel>();
            var otherItems = new List<InventoryPageModel>();
            
            foreach (var it in list)
            {
                // Restore selection state if it was previously selected
                if (currentSelections.ContainsKey(it.itemID))
                {
                    var selection = currentSelections[it.itemID];
                    it.IsSelected = selection.IsSelected;
                    it.InputAmount = selection.InputAmount;
                    it.InputUnit = selection.InputUnit;
                    it.InputAmountSmall = selection.InputAmountSmall;
                    it.InputUnitSmall = selection.InputUnitSmall;
                    it.InputAmountMedium = selection.InputAmountMedium;
                    it.InputUnitMedium = selection.InputUnitMedium;
                    it.InputAmountLarge = selection.InputAmountLarge;
                    it.InputUnitLarge = selection.InputUnitLarge;
                }
                else
                {
                    // Start with all items unchecked by default
                    it.IsSelected = false;
                    // Initialize per-size units from inventory default if not set
                    var defUnit = it.HasUnit ? it.unitOfMeasurement : "pcs";
                    if (string.IsNullOrWhiteSpace(it.InputUnitSmall)) it.InputUnitSmall = defUnit;
                    if (string.IsNullOrWhiteSpace(it.InputUnitMedium)) it.InputUnitMedium = defUnit;
                    if (string.IsNullOrWhiteSpace(it.InputUnitLarge)) it.InputUnitLarge = defUnit;
                }

                // No need to set per-size amount since it's always 1 serving

                // Check if it's a cup or straw
                var nameLower = (it.itemName ?? string.Empty).Trim().ToLowerInvariant();
                var categoryLower = (it.itemCategory ?? string.Empty).Trim().ToLowerInvariant();
                bool isCup = nameLower.Contains("cup") && (categoryLower == "supplies" || categoryLower == "ingredients");
                bool isStraw = nameLower.Contains("straw") && (categoryLower == "supplies" || categoryLower == "ingredients");
                
                // Skip cup items - they are handled automatically by the size buttons
                if (isCup)
                {
                    continue; // Don't add cup items to the display list
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

        private async Task LoadCupSizesAsync()
        {
            try
            {
                // Load cup sizes from database - look for Small Cup, Medium Cup, Large Cup
                var inventoryItems = await _database.GetInventoryItemsAsync();
                
                // Find the specific cup items
                var smallCup = inventoryItems.FirstOrDefault(item => 
                    string.Equals(item.itemName, "Small Cup", StringComparison.OrdinalIgnoreCase));
                var mediumCup = inventoryItems.FirstOrDefault(item => 
                    string.Equals(item.itemName, "Medium Cup", StringComparison.OrdinalIgnoreCase));
                var largeCup = inventoryItems.FirstOrDefault(item => 
                    string.Equals(item.itemName, "Large Cup", StringComparison.OrdinalIgnoreCase));

                // Update button text with actual cup names from database
                if (smallCup != null)
                {
                    SmallSizeText = smallCup.itemName; // "Small Cup"
                }
                if (mediumCup != null)
                {
                    MediumSizeText = mediumCup.itemName; // "Medium Cup"
                }
                if (largeCup != null)
                {
                    LargeSizeText = largeCup.itemName; // "Large Cup"
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading cup sizes: {ex.Message}");
                // Keep default values if loading fails
            }
        }

        private string ExtractSizeFromDescription(string description, string sizeType)
        {
            // This is a simple extraction method - you can enhance this based on your data structure
            // For example, if your description contains "Small: 8oz, Medium: 12oz, Large: 16oz"
            // you could extract the specific size names
            
            // For now, return the size type as-is, but you can implement more sophisticated parsing
            return sizeType;
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
                
                // Update input unit based on the selected size (use remembered size-specific units if available)
                item.InputUnit = size switch
                {
                    "Small" => string.IsNullOrWhiteSpace(item.InputUnitSmall) ? (item.InputUnitSmall = item.InputUnitSmall ?? item.unitOfMeasurement) : item.InputUnitSmall,
                    "Medium" => string.IsNullOrWhiteSpace(item.InputUnitMedium) ? (item.InputUnitMedium = item.InputUnitMedium ?? item.unitOfMeasurement) : item.InputUnitMedium,
                    "Large" => string.IsNullOrWhiteSpace(item.InputUnitLarge) ? (item.InputUnitLarge = item.InputUnitLarge ?? item.unitOfMeasurement) : item.InputUnitLarge,
                    _ => item.InputUnit
                };
            }
            
            // Preserve addon selections when switching sizes
            // Addons should remain selected regardless of size changes
            foreach (var addon in InventoryItems.Where(i => i.IsSelected && IsAddonCategory(i.itemCategory)))
            {
                // Keep addon selections intact
                addon.IsSelected = true;
            }
            
            // Update UI bindings
            OnPropertyChanged(nameof(HasSelectedIngredients));
            OnPropertyChanged(nameof(SelectedInventoryItems));
            OnPropertyChanged(nameof(SelectedIngredientsOnly));
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

        // Public helper to refresh filters and selection-related bindings after external updates
        public void RefreshSelectionAndFilter()
        {
            ApplyFilters();
            UpdateSelectedIngredientsOnly();
            OnPropertyChanged(nameof(HasSelectedIngredients));
            OnPropertyChanged(nameof(SelectedInventoryItems));
            OnPropertyChanged(nameof(SelectedIngredientsOnly));
        }

        public void ApplyFilters()
        {
            var query = AllInventoryItems.AsEnumerable();

            // Exclude cups and straws from display
            query = query.Where(i => {
                var nameLower = (i.itemName ?? string.Empty).Trim().ToLowerInvariant();
                var categoryLower = (i.itemCategory ?? string.Empty).Trim().ToLowerInvariant();
                
                // Exclude specific cup items and straws
                bool isSmallCup = string.Equals(i.itemName, "Small Cup", StringComparison.OrdinalIgnoreCase);
                bool isMediumCup = string.Equals(i.itemName, "Medium Cup", StringComparison.OrdinalIgnoreCase);
                bool isLargeCup = string.Equals(i.itemName, "Large Cup", StringComparison.OrdinalIgnoreCase);
                bool isStraw = string.Equals(i.itemName, "Straw", StringComparison.OrdinalIgnoreCase);
                
                return !isSmallCup && !isMediumCup && !isLargeCup && !isStraw;
            });

            if (!string.IsNullOrWhiteSpace(SelectedFilter) && SelectedFilter != "All")
            {
                var filter = SelectedFilter?.Trim() ?? string.Empty;
                if (string.Equals(filter, "Ingredients", StringComparison.OrdinalIgnoreCase))
                {
                    // Show only allowed ingredient categories
                    var allowed = new[] { "Syrups", "Powdered", "Fruit Series", "Sinkers", "Sinkers & etc.", "Liquid" };
                    query = query.Where(i => allowed.Any(a => string.Equals(i.itemCategory?.Trim(), a, StringComparison.OrdinalIgnoreCase)));
                }
                else if (string.Equals(filter, "Supplies", StringComparison.OrdinalIgnoreCase))
                {
                    // Supplies should only show Others
                    query = query.Where(i => string.Equals(i.itemCategory?.Trim(), "Others", StringComparison.OrdinalIgnoreCase));
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
        private void UnselectAllVisible()
        {
            foreach (var item in InventoryItems)
            {
                var allItem = AllInventoryItems.FirstOrDefault(i => i.itemID == item.itemID);
                if (allItem != null && allItem.IsSelected)
                {
                    var existing = Ingredients.FirstOrDefault(i => i.Name == allItem.itemName);
                    if (existing != null)
                    {
                        Ingredients.Remove(existing);
                    }
                    allItem.IsSelected = false;
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
            System.Diagnostics.Debug.WriteLine($"🔍 OpenAddonPopupCommand called");
            try
            {
                if (AddonsPopup == null)
                {
                    System.Diagnostics.Debug.WriteLine($"🔍 Creating new AddonsPopup ViewModel");
                    AddonsPopup = new AddonsSelectionPopupViewModel();
                    AddonsPopup.AddonsSelected += OnAddonsSelected;
                }

                // Load addons and show popup
                System.Diagnostics.Debug.WriteLine($"🔍 Loading addons...");
                await AddonsPopup.LoadAddonsAsync();
                System.Diagnostics.Debug.WriteLine($"🔍 Setting IsAddonPopupVisible = true");
                IsAddonPopupVisible = true;
                AddonsPopup.IsAddonsPopupVisible = true;
                System.Diagnostics.Debug.WriteLine($"✅ AddonsSelectionPopup opened (IsAddonPopupVisible={IsAddonPopupVisible}, AddonsPopup.IsAddonsPopupVisible={AddonsPopup.IsAddonsPopupVisible})");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error opening addon popup: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
            }
        }

        [RelayCommand]
        private void CloseAddonPopup()
        {
            IsAddonPopupVisible = false;
            if (AddonsPopup != null) AddonsPopup.IsAddonsPopupVisible = false;
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
            if (AddonsPopup != null) AddonsPopup.IsAddonsPopupVisible = false;
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

    public class SelectionState
    {
        public bool IsSelected { get; set; }
        public double InputAmount { get; set; }
        public string InputUnit { get; set; }
        public double InputAmountSmall { get; set; }
        public string InputUnitSmall { get; set; }
        public double InputAmountMedium { get; set; }
        public string InputUnitMedium { get; set; }
        public double InputAmountLarge { get; set; }
        public string InputUnitLarge { get; set; }
    }
}
