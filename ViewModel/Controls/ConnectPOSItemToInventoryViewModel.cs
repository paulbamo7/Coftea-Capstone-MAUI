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
using Coftea_Capstone.Services;

namespace Coftea_Capstone.ViewModel.Controls
{
    public partial class ConnectPOSItemToInventoryViewModel : ObservableObject, IDisposable
    {
        private readonly Database _database = new Database(); // Will use auto-detected host

        // Event to trigger AddProduct in parent VM
        public event Action ConfirmPreviewRequested;

        public ConnectPOSItemToInventoryViewModel()
        {
            // Initialize addons popup
            AddonsPopup = new AddonsSelectionPopupViewModel();
            AddonsPopup.AddonsSelected += OnAddonsSelected;
            
            // Initialize collections
            SelectedIngredientsOnly = new ObservableCollection<InventoryPageModel>();
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
        
        partial void OnIsInputIngredientsVisibleChanged(bool value)
        {
            if (value)
            {
                System.Diagnostics.Debug.WriteLine($"🔧 InputIngredients popup became visible - refreshing collection");
                
                // Ensure the collection is properly initialized
                if (SelectedIngredientsOnly == null)
                {
                    SelectedIngredientsOnly = new ObservableCollection<InventoryPageModel>();
                    System.Diagnostics.Debug.WriteLine($"🔧 SelectedIngredientsOnly collection was null, reinitialized");
                }
                
                // Force refresh when popup becomes visible
                UpdateSelectedIngredientsOnly();
                
                // If the collection is still empty, try to populate it from AllInventoryItems
                if (SelectedIngredientsOnly.Count == 0 && AllInventoryItems != null)
                {
                    System.Diagnostics.Debug.WriteLine($"🔧 SelectedIngredientsOnly is empty, trying to populate from AllInventoryItems");
                    var selectedItems = AllInventoryItems.Where(i => i != null && i.IsSelected).ToList();
                    System.Diagnostics.Debug.WriteLine($"🔧 Found {selectedItems.Count} selected items in AllInventoryItems");
                    
                    foreach (var item in selectedItems)
                    {
                        SelectedIngredientsOnly.Add(item);
                        System.Diagnostics.Debug.WriteLine($"🔧 Added to SelectedIngredientsOnly: {item.itemName}");
                    }
                }
                
                // Update InputAmount and InputUnit for all selected items based on current size
                // This ensures the UI shows the correct saved values when editing
                foreach (var item in SelectedIngredientsOnly)
                {
                    var size = SelectedSize;
                    item.InputAmount = size switch
                    {
                        "Small" => item.InputAmountSmall > 0 ? item.InputAmountSmall : 1,
                        "Medium" => item.InputAmountMedium > 0 ? item.InputAmountMedium : 1,
                        "Large" => item.InputAmountLarge > 0 ? item.InputAmountLarge : 1,
                        _ => 1
                    };
                    
                    var fallbackUnit = !string.IsNullOrWhiteSpace(item.unitOfMeasurement) ? item.unitOfMeasurement : item.DefaultUnit;
                    item.InputUnit = size switch
                    {
                        "Small" => !string.IsNullOrWhiteSpace(item.InputUnitSmall) ? item.InputUnitSmall : fallbackUnit,
                        "Medium" => !string.IsNullOrWhiteSpace(item.InputUnitMedium) ? item.InputUnitMedium : fallbackUnit,
                        "Large" => !string.IsNullOrWhiteSpace(item.InputUnitLarge) ? item.InputUnitLarge : fallbackUnit,
                        _ => fallbackUnit
                    };
                    
                    System.Diagnostics.Debug.WriteLine($"🔧 Updated {item.itemName}: Amount={item.InputAmount}, Unit={item.InputUnit} for size {size}");
                }
                
                OnPropertyChanged(nameof(SelectedIngredientsOnly));
                System.Diagnostics.Debug.WriteLine($"🔧 Final SelectedIngredientsOnly count: {SelectedIngredientsOnly.Count}");
            }
        }

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

        private static bool IsCoffeeCategory(string category) // Identify coffee-related categories
        {
            var c = (category ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(c)) return false;
            return string.Equals(c, "Coffee", StringComparison.OrdinalIgnoreCase)
                || string.Equals(c, "Americano", StringComparison.OrdinalIgnoreCase)
                || string.Equals(c, "Latte", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsAddonCategory(string category) // Identify addon-related categories
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
        
        private bool IsCupOrStraw(InventoryPageModel item) // Identify cups/straws by name and category
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
            // Sync InputAmount to per-size fields for all selected ingredients
            SyncInputAmountsToPerSizeFields();
            
            // finalize and close any overlays
            IsConnectPOSToInventoryVisible = false;
            IsInputIngredientsVisible = false;
            IsPreviewVisible = false;
            // Ensure addons popup is closed on confirm
            IsAddonPopupVisible = false;
            if (AddonsPopup != null) AddonsPopup.IsAddonsPopupVisible = false;
            ConfirmPreviewRequested?.Invoke();
        }

        // Sync InputAmount to per-size fields for all selected ingredients
        private void SyncInputAmountsToPerSizeFields()
        {
            System.Diagnostics.Debug.WriteLine("🔧 Syncing InputAmount to per-size fields");
            
            foreach (var item in SelectedIngredientsOnly)
            {
                if (item.InputAmount > 0)
                {
                    // If no per-size amounts are set, use the general InputAmount for all sizes
                    if (item.InputAmountSmall <= 0)
                    {
                        item.InputAmountSmall = item.InputAmount;
                        System.Diagnostics.Debug.WriteLine($"🔧 Set {item.itemName} Small amount to {item.InputAmount}");
                    }
                    if (item.InputAmountMedium <= 0)
                    {
                        item.InputAmountMedium = item.InputAmount;
                        System.Diagnostics.Debug.WriteLine($"🔧 Set {item.itemName} Medium amount to {item.InputAmount}");
                    }
                    if (item.InputAmountLarge <= 0)
                    {
                        item.InputAmountLarge = item.InputAmount;
                        System.Diagnostics.Debug.WriteLine($"🔧 Set {item.itemName} Large amount to {item.InputAmount}");
                    }
                }
            }
        }
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
        private void OpenInputIngredients() // Show the ingredient input overlay..
        {
            System.Diagnostics.Debug.WriteLine($"🔧 OpenInputIngredients called");
            System.Diagnostics.Debug.WriteLine($"🔧 AllInventoryItems count: {AllInventoryItems?.Count ?? 0}");
            System.Diagnostics.Debug.WriteLine($"🔧 Selected items count: {AllInventoryItems?.Count(i => i.IsSelected) ?? 0}");
            
            // Force refresh the SelectedIngredientsOnly collection
            UpdateSelectedIngredientsOnly();
            
            System.Diagnostics.Debug.WriteLine($"🔧 SelectedIngredientsOnly count after update: {SelectedIngredientsOnly?.Count ?? 0}");
            
            // Log each selected ingredient
            if (SelectedIngredientsOnly != null)
            {
                foreach (var item in SelectedIngredientsOnly)
                {
                    System.Diagnostics.Debug.WriteLine($"🔧 Selected ingredient: {item.itemName} (ID: {item.itemID}) - IsSelected: {item.IsSelected}");
                }
            }
            
            IsConnectPOSToInventoryVisible = false;
            IsPreviewVisible = false;
            IsInputIngredientsVisible = true;
            
            // Force property change notifications
            OnPropertyChanged(nameof(SelectedIngredientsOnly));
            OnPropertyChanged(nameof(HasSelectedIngredients));
            OnPropertyChanged(nameof(SelectedInventoryItems));
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
            if (Application.Current?.MainPage is NavigationPage nav)
            {
                await nav.PushAsync(new Inventory(), false);
            }
        }

        [RelayCommand]
        public async Task LoadInventoryAsync()
        {
            var list = await _database.GetInventoryItemsAsync();
            
            // Load cup sizes from database
            await LoadCupSizesAsync();
            
            // Preserve current selection state AND input values for ALL items (not just selected)
            // This ensures that when editing an existing product, the saved amounts/units are preserved
            var currentSelections = new Dictionary<int, SelectionState>();
            if (AllInventoryItems != null)
            {
                foreach (var item in AllInventoryItems)
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
                    var defUnit = !string.IsNullOrWhiteSpace(it.unitOfMeasurement) ? it.unitOfMeasurement : it.DefaultUnit;
                    if (string.IsNullOrWhiteSpace(it.InputUnitSmall)) it.InputUnitSmall = defUnit;
                    if (string.IsNullOrWhiteSpace(it.InputUnitMedium)) it.InputUnitMedium = defUnit;
                    if (string.IsNullOrWhiteSpace(it.InputUnitLarge)) it.InputUnitLarge = defUnit;
                    // Also initialize the current InputUnit
                    if (string.IsNullOrWhiteSpace(it.InputUnit)) it.InputUnit = defUnit;
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
                var item = sender as InventoryPageModel;
                System.Diagnostics.Debug.WriteLine($"🔧 Item {item?.itemName} IsSelected changed to: {item?.IsSelected}");
                
                // Update the SelectedIngredientsOnly collection when IsSelected changes
                UpdateSelectedIngredientsOnly();
                OnPropertyChanged(nameof(HasSelectedIngredients));
                OnPropertyChanged(nameof(SelectedInventoryItems));
                OnPropertyChanged(nameof(SelectedIngredientsOnly));
            }
            else if (e.PropertyName == nameof(InventoryPageModel.InputAmount)
                  || e.PropertyName == nameof(InventoryPageModel.InputUnit)
                  || e.PropertyName == nameof(InventoryPageModel.InputAmountSmall)
                  || e.PropertyName == nameof(InventoryPageModel.InputUnitSmall)
                  || e.PropertyName == nameof(InventoryPageModel.InputAmountMedium)
                  || e.PropertyName == nameof(InventoryPageModel.InputUnitMedium)
                  || e.PropertyName == nameof(InventoryPageModel.InputAmountLarge)
                  || e.PropertyName == nameof(InventoryPageModel.InputUnitLarge))
            {
                // Persist per-item input edits immediately to prevent loss on UI switches
                _ = PersistCurrentInputsAsync();
            }
        }

        private async Task PersistCurrentInputsAsync()
        {
            try
            {
                // No-op here if persistence requires DB schema. Hook for future save if needed.
                await Task.CompletedTask;
            }
            catch { }
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
            
            // FIRST: Save current input values to the appropriate size properties before switching
            foreach (var item in SelectedIngredientsOnly)
            {
                switch (SelectedSize)
                {
                    case "Small":
                        item.InputAmountSmall = item.InputAmount;
                        item.InputUnitSmall = item.InputUnit;
                        break;
                    case "Medium":
                        item.InputAmountMedium = item.InputAmount;
                        item.InputUnitMedium = item.InputUnit;
                        break;
                    case "Large":
                        item.InputAmountLarge = item.InputAmount;
                        item.InputUnitLarge = item.InputUnit;
                        break;
                }
            }
            
            // THEN: Switch to the new size
            SelectedSize = size;
            
            // Update all selected ingredients to use the new size
            foreach (var item in SelectedIngredientsOnly)
            {
                item.SelectedSize = size;
                
                // Update Amount Used based on the selected size - use saved values from database or temporary values
                item.InputAmount = size switch
                {
                    "Small" => item.InputAmountSmall > 0 ? item.InputAmountSmall : 1,
                    "Medium" => item.InputAmountMedium > 0 ? item.InputAmountMedium : 1,
                    "Large" => item.InputAmountLarge > 0 ? item.InputAmountLarge : 1,
                    _ => 1
                };
                
                // Update input unit based on the selected size - use saved values from database or temporary values
                var fallbackUnit = !string.IsNullOrWhiteSpace(item.unitOfMeasurement) ? item.unitOfMeasurement : item.DefaultUnit;
                item.InputUnit = size switch
                {
                    "Small" => !string.IsNullOrWhiteSpace(item.InputUnitSmall) ? item.InputUnitSmall : fallbackUnit,
                    "Medium" => !string.IsNullOrWhiteSpace(item.InputUnitMedium) ? item.InputUnitMedium : fallbackUnit,
                    "Large" => !string.IsNullOrWhiteSpace(item.InputUnitLarge) ? item.InputUnitLarge : fallbackUnit,
                    _ => fallbackUnit
                };
                
                // Explicitly notify UI that the text binding should update
                OnPropertyChanged(nameof(item.InputAmountText));
                OnPropertyChanged(nameof(item.InputUnit));
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
                // Initialize defaults ONLY if no prior values exist (preserve last saved values)
                var initUnit = allItem.HasUnit ? allItem.unitOfMeasurement : "pcs";

                if (allItem.InputAmountSmall <= 0) allItem.InputAmountSmall = allItem.InputAmountSmall > 0 ? allItem.InputAmountSmall : 1;
                if (allItem.InputAmountMedium <= 0) allItem.InputAmountMedium = allItem.InputAmountMedium > 0 ? allItem.InputAmountMedium : 1;
                if (allItem.InputAmountLarge <= 0) allItem.InputAmountLarge = allItem.InputAmountLarge > 0 ? allItem.InputAmountLarge : 1;
                if (allItem.InputAmount <= 0) allItem.InputAmount = 1;

                if (string.IsNullOrWhiteSpace(allItem.InputUnitSmall)) allItem.InputUnitSmall = string.IsNullOrWhiteSpace(allItem.InputUnitSmall) ? initUnit : allItem.InputUnitSmall;
                if (string.IsNullOrWhiteSpace(allItem.InputUnitMedium)) allItem.InputUnitMedium = string.IsNullOrWhiteSpace(allItem.InputUnitMedium) ? initUnit : allItem.InputUnitMedium;
                if (string.IsNullOrWhiteSpace(allItem.InputUnitLarge)) allItem.InputUnitLarge = string.IsNullOrWhiteSpace(allItem.InputUnitLarge) ? initUnit : allItem.InputUnitLarge;
                if (string.IsNullOrWhiteSpace(allItem.InputUnit)) allItem.InputUnit = initUnit;

                Ingredients.Add(new Ingredient { Name = allItem.itemName, Amount = allItem.InputAmount > 0 ? allItem.InputAmount : 1, Unit = string.IsNullOrWhiteSpace(allItem.InputUnit) ? initUnit : allItem.InputUnit, Selected = true });
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
            System.Diagnostics.Debug.WriteLine($"🔧 UpdateSelectedIngredientsOnly called");
            System.Diagnostics.Debug.WriteLine($"🔧 AllInventoryItems count: {AllInventoryItems?.Count ?? 0}");
            
            // Don't clear the collection if AllInventoryItems is null or empty
            // This prevents the collection from being cleared when the ViewModel is being disposed
            if (AllInventoryItems == null || AllInventoryItems.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"🔧 AllInventoryItems is null or empty, skipping update");
                return;
            }
            
            SelectedIngredientsOnly.Clear();
            var selected = AllInventoryItems?.Where(i => i != null && i.IsSelected).ToList() ?? new List<InventoryPageModel>();
            
            System.Diagnostics.Debug.WriteLine($"🔧 Found {selected.Count} selected items");
            
            foreach (var item in selected)
            {
                bool isCupOrStraw = IsCupOrStraw(item);
                System.Diagnostics.Debug.WriteLine($"🔧 Item: {item.itemName} (ID: {item.itemID}) - IsCupOrStraw: {isCupOrStraw}");
                
                // For now, let's include ALL selected items, not just non-cup/straw items
                // This will help us debug if the issue is with the filtering
                SelectedIngredientsOnly.Add(item);
            }
            
            System.Diagnostics.Debug.WriteLine($"🔧 SelectedIngredientsOnly final count: {SelectedIngredientsOnly.Count}");
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
        
        // Public method to force refresh SelectedIngredientsOnly collection
        public void ForceRefreshSelectedIngredients()
        {
            System.Diagnostics.Debug.WriteLine($"🔧 ForceRefreshSelectedIngredients called");
            UpdateSelectedIngredientsOnly();
            OnPropertyChanged(nameof(SelectedIngredientsOnly));
        }
        
        // Public method to clear all inventory selections (call this when user cancels or completes product creation)
        public void ClearAllSelections()
        {
            System.Diagnostics.Debug.WriteLine($"🔧 ClearAllSelections called");
            
            if (InventoryItems != null)
            {
                foreach (var item in InventoryItems)
                {
                    item.IsSelected = false;
                    item.InputAmount = 0;
                    item.InputUnit = string.Empty;
                    item.AddonQuantity = 0;
                }
            }
            
            // Clear the SelectedIngredientsOnly collection
            SelectedIngredientsOnly?.Clear();
            
            // Notify property changes
            OnPropertyChanged(nameof(HasSelectedIngredients));
            OnPropertyChanged(nameof(SelectedInventoryItems));
            OnPropertyChanged(nameof(SelectedIngredientsOnly));
            
            System.Diagnostics.Debug.WriteLine($"🔧 All selections cleared");
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
                System.Diagnostics.Debug.WriteLine($"🔍 Applying filter: {filter}");
                
                if (string.Equals(filter, "Ingredients", StringComparison.OrdinalIgnoreCase))
                {
                    // Show only allowed ingredient categories
                    var allowed = new[] { "Syrups", "Powdered", "Fruit Series", "Sinkers", "Sinkers & etc.", "Liquid" };
                    System.Diagnostics.Debug.WriteLine($"🔍 Allowed ingredient categories: {string.Join(", ", allowed)}");
                    
                    var beforeCount = query.Count();
                    query = query.Where(i => allowed.Any(a => string.Equals(i.itemCategory?.Trim(), a, StringComparison.OrdinalIgnoreCase)));
                    var afterCount = query.Count();
                    
                    System.Diagnostics.Debug.WriteLine($"🔍 Ingredients filter: {beforeCount} -> {afterCount} items");
                    
                    // Debug: show what categories we actually have
                    var actualCategories = AllInventoryItems.Select(i => i.itemCategory?.Trim()).Distinct().Where(c => !string.IsNullOrEmpty(c));
                    System.Diagnostics.Debug.WriteLine($"🔍 Actual categories in database: {string.Join(", ", actualCategories)}");
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
            System.Diagnostics.Debug.WriteLine($"🔍 FilterByCategory called with: {category}");
            SelectedFilter = category;
            ApplyFilters();
            System.Diagnostics.Debug.WriteLine($"🔍 After filtering, InventoryItems count: {InventoryItems.Count}");
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
        public void Dispose()
        {
            try
            {
                // Unhook all event handlers
                if (AllInventoryItems != null)
                {
                    AllInventoryItems.CollectionChanged -= OnInventoryItemsCollectionChanged;
                    foreach (var item in AllInventoryItems)
                    {
                        if (item != null)
                            item.PropertyChanged -= OnItemPropertyChanged;
                    }
                }

                // Clear collections
                Ingredients?.Clear();
                InventoryItems?.Clear();
                AllInventoryItems?.Clear();
                AvailableAddons?.Clear();
                SelectedAddons?.Clear();
                // Don't clear SelectedIngredientsOnly here as it might be needed for the InputIngredientsAmountUsed popup
                // SelectedIngredientsOnly?.Clear();

                // Clear events
                ConfirmPreviewRequested = null;
                ReturnRequested = null;

                // Dispose addons popup
                (AddonsPopup as IDisposable)?.Dispose();
                AddonsPopup = null;
            }
            catch { }
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
