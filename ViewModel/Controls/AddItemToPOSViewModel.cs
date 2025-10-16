using Coftea_Capstone.C_;
using Coftea_Capstone.Views.Pages;
using Coftea_Capstone.Views.Controls;
using Coftea_Capstone.Views;
using Coftea_Capstone.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Coftea_Capstone.ViewModel.Controls;

namespace Coftea_Capstone.ViewModel
{
    public partial class AddItemToPOSViewModel : ObservableObject
    {
        public ConnectPOSItemToInventoryViewModel ConnectPOSToInventoryVM { get; set; }

        [ObservableProperty]
        private string selectedImagePath;

        [ObservableProperty]
        private ImageSource selectedImageSource;
        [ObservableProperty]
        private bool isConnectPOSToInventoryVisible = false;

        private readonly Database _database;

        public ObservableCollection<string> Categories { get; set; } = new ObservableCollection<string>
        {
            "Frappe",
            "Fruit/Soda",
            "Milktea",
            "Coffee"
        };

        [ObservableProperty]
        private string selectedCategory;

        public ObservableCollection<string> FruitSodaSubcategories { get; } = new ObservableCollection<string>
        {
            "Fruit",
            "Soda"
        };

        public ObservableCollection<string> CoffeeSubcategories { get; } = new ObservableCollection<string>
        {
            "Americano",
            "Latte"
        };

        [ObservableProperty]
        private string selectedSubcategory;

        public bool IsFruitSodaSubcategoryVisible => string.Equals(SelectedCategory, "Fruit/Soda", StringComparison.OrdinalIgnoreCase);
        
        public bool IsCoffeeSubcategoryVisible => string.Equals(SelectedCategory, "Coffee", StringComparison.OrdinalIgnoreCase);

        // Pricing field visibility properties
        public bool IsSmallPriceVisible => string.Equals(SelectedCategory, "Coffee", StringComparison.OrdinalIgnoreCase);
        
        public bool IsMediumPriceVisible => true; // Always visible
        
        public bool IsLargePriceVisible => true; // Always visible

        public string EffectiveCategory =>
            string.Equals(SelectedCategory, "Fruit/Soda", StringComparison.OrdinalIgnoreCase)
                ? (string.IsNullOrWhiteSpace(SelectedSubcategory) ? null : SelectedSubcategory)
                : string.Equals(SelectedCategory, "Coffee", StringComparison.OrdinalIgnoreCase)
                    ? (string.IsNullOrWhiteSpace(SelectedSubcategory) ? null : SelectedSubcategory)
                    : SelectedCategory;

        partial void OnSelectedCategoryChanged(string value)
        {
            // Notify visibility change for subcategory UI
            OnPropertyChanged(nameof(IsFruitSodaSubcategoryVisible));
            OnPropertyChanged(nameof(IsCoffeeSubcategoryVisible));
            
            // Notify visibility change for pricing fields
            OnPropertyChanged(nameof(IsSmallPriceVisible));
            OnPropertyChanged(nameof(IsMediumPriceVisible));
            OnPropertyChanged(nameof(IsLargePriceVisible));

            // Only reset subcategory when leaving Fruit/Soda or Coffee, and not during edit mode
            if (!IsEditMode && 
                !string.Equals(value, "Fruit/Soda", StringComparison.OrdinalIgnoreCase) && 
                !string.Equals(value, "Coffee", StringComparison.OrdinalIgnoreCase))
                SelectedSubcategory = null;
        }

        [ObservableProperty]
        private bool isAddItemToPOSVisible;

        [ObservableProperty]
        private string productName;

        [ObservableProperty]
        private string smallPrice = string.Empty;

        [ObservableProperty]
        private string mediumPrice = string.Empty;

        [ObservableProperty]
        private string largePrice = string.Empty;

        [ObservableProperty]
        private string imagePath;

        [ObservableProperty]
        private string productDescription;

        [ObservableProperty]
        private string productColorCode;

        [ObservableProperty]
        private bool isEditMode = false;

        [ObservableProperty]
        private int editingProductId;

        public event Action<POSPageModel> ProductAdded;
        public event Action<POSPageModel> ProductUpdated;

        public AddItemToPOSViewModel()
        {
            _database = new Database(); // Will use auto-detected host

            IsAddItemToPOSVisible = false;
            IsConnectPOSToInventoryVisible = false;

            ConnectPOSToInventoryVM = new ConnectPOSItemToInventoryViewModel();
            ConnectPOSToInventoryVM.ReturnRequested += () =>
            {
                IsAddItemToPOSVisible = true;
            };
            ConnectPOSToInventoryVM.ConfirmPreviewRequested += async () =>
            {
                await AddProduct();
            };
        }

        [RelayCommand]
        private void CloseAddItemToPOS()
        {
            IsAddItemToPOSVisible = false;
            IsEditMode = false;
            ProductName = string.Empty;
            SmallPrice = string.Empty;
            MediumPrice = string.Empty;
            LargePrice = string.Empty;
            SelectedCategory = string.Empty;
            SelectedSubcategory = null;
            ImagePath = string.Empty;
            SelectedImageSource = null;
            ProductDescription = string.Empty;
            EditingProductId = 0;
        }

        public void SetEditMode(POSPageModel product)
        {
            IsEditMode = true;
            EditingProductId = product.ProductID;
            ProductName = product.ProductName;
            SmallPrice = product.SmallPrice > 0 ? product.SmallPrice.ToString() : string.Empty;
            MediumPrice = product.MediumPrice > 0 ? product.MediumPrice.ToString() : string.Empty;
            LargePrice = product.LargePrice > 0 ? product.LargePrice.ToString() : string.Empty;
            ImagePath = product.ImageSet;
            SelectedImageSource = !string.IsNullOrEmpty(product.ImageSet) ? ImageSource.FromFile(product.ImageSet) : null;
            ProductDescription = product.ProductDescription ?? string.Empty;

            // Set category and subcategory based on the product's data (prefer Subcategory when present)
            if (!string.IsNullOrEmpty(product.Subcategory))
            {
                if (product.Subcategory == "Fruit" || product.Subcategory == "Soda")
                {
                    SelectedCategory = "Fruit/Soda";
                    SelectedSubcategory = product.Subcategory;
                }
                else if (product.Subcategory == "Americano" || product.Subcategory == "Latte")
                {
                    SelectedCategory = "Coffee";
                    SelectedSubcategory = product.Subcategory;
                }
                else
                {
                    SelectedCategory = product.Category;
                    SelectedSubcategory = product.Subcategory;
                }
            }
            else if (!string.IsNullOrEmpty(product.Category))
            {
                // No explicit subcategory saved; infer from category if possible
                if (product.Category == "Fruit" || product.Category == "Soda")
                {
                    SelectedCategory = "Fruit/Soda";
                    SelectedSubcategory = product.Category;
                }
                else if (product.Category == "Americano" || product.Category == "Latte")
                {
                    SelectedCategory = "Coffee";
                    SelectedSubcategory = product.Category;
                }
                else
                {
                    SelectedCategory = product.Category;
                    SelectedSubcategory = null;
                }
            }
            // Preload existing linked ingredients and mark them selected
            _ = PreloadLinkedIngredientsAsync(product.ProductID);
        }

        // Helper to show the AddItemToPOS overlay when returning from child popups
        public void SetIsAddItemToPOSVisibleTrue()
        {
            IsAddItemToPOSVisible = true;
        }

        private async Task PreloadLinkedIngredientsAsync(int productId)
        {
            try
            {
                // Ensure inventory is loaded
                if (ConnectPOSToInventoryVM.AllInventoryItems == null || !ConnectPOSToInventoryVM.AllInventoryItems.Any())
                {
                    await ConnectPOSToInventoryVM.LoadInventoryAsync();
                }

                var links = await _database.GetProductIngredientsAsync(productId);
                foreach (var (item, amount, unit, role) in links)
                {
                    // Find matching inventory item
                    var inv = ConnectPOSToInventoryVM.AllInventoryItems.FirstOrDefault(i => i.itemID == item.itemID);
                    if (inv == null)
                    {
                        // If not found yet, add it to collections
                        inv = item;
                        ConnectPOSToInventoryVM.AllInventoryItems.Add(inv);
                        ConnectPOSToInventoryVM.InventoryItems.Add(inv);
                    }

                    // Mark as selected and set input fields from link
                    inv.IsSelected = true;
                    // Preserve per-size values loaded from DB if present
                    var hasS = inv.InputAmountSmall > 0;
                    var hasM = inv.InputAmountMedium > 0;
                    var hasL = inv.InputAmountLarge > 0;

                    // Shared fallback from link when a size is missing
                    var sharedAmt = amount > 0 ? amount : 1;
                    var sharedUnit = string.IsNullOrWhiteSpace(unit) ? inv.DefaultUnit : unit;

                    if (!hasS)
                    {
                        inv.InputAmountSmall = sharedAmt;
                        inv.InputUnitSmall = sharedUnit;
                    }
                    if (!hasM)
                    {
                        inv.InputAmountMedium = sharedAmt;
                        inv.InputUnitMedium = sharedUnit;
                    }
                    if (!hasL)
                    {
                        inv.InputAmountLarge = sharedAmt;
                        inv.InputUnitLarge = sharedUnit;
                    }

                    // Set current working amount/unit to the current selected size in the popup
                    var size = ConnectPOSToInventoryVM.SelectedSize;
                    inv.InputAmount = size switch
                    {
                        "Small" => inv.InputAmountSmall,
                        "Medium" => inv.InputAmountMedium,
                        "Large" => inv.InputAmountLarge,
                        _ => sharedAmt
                    };
                    inv.InputUnit = size switch
                    {
                        "Small" => string.IsNullOrWhiteSpace(inv.InputUnitSmall) ? sharedUnit : inv.InputUnitSmall,
                        "Medium" => string.IsNullOrWhiteSpace(inv.InputUnitMedium) ? sharedUnit : inv.InputUnitMedium,
                        "Large" => string.IsNullOrWhiteSpace(inv.InputUnitLarge) ? sharedUnit : inv.InputUnitLarge,
                        _ => sharedUnit
                    };
                }

                // Update derived collections and flags using public method
                ConnectPOSToInventoryVM.RefreshSelectionAndFilter();
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Warning", $"Failed to preload ingredients: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        public async Task AddProduct()
        {
            if (string.IsNullOrWhiteSpace(ProductName))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Product name is required.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedCategory))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Please select a category.", "OK");
                return;
            }

            if (string.Equals(SelectedCategory, "Fruit/Soda", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(SelectedSubcategory))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Please select a subcategory (Fruit or Soda).", "OK");
                return;
            }

            if (string.Equals(SelectedCategory, "Coffee", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(SelectedSubcategory))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Please select a subcategory (Americano or Latte).", "OK");
                return;
            }

            // Parse and validate prices
            if (!decimal.TryParse(SmallPrice, out decimal smallPriceValue) && string.Equals(SelectedCategory, "Coffee", StringComparison.OrdinalIgnoreCase))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Please enter a valid small price.", "OK");
                return;
            }

            if (!decimal.TryParse(MediumPrice, out decimal mediumPriceValue))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Please enter a valid medium price.", "OK");
                return;
            }

            if (!decimal.TryParse(LargePrice, out decimal largePriceValue))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Please enter a valid large price.", "OK");
                return;
            }

            // Only validate Small price if it's visible (Coffee category)
            if (string.Equals(SelectedCategory, "Coffee", StringComparison.OrdinalIgnoreCase) && smallPriceValue <= 0)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Small price must be greater than 0.", "OK");
                return;
            }

            if (mediumPriceValue <= 0)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Medium price must be greater than 0.", "OK");
                return;
            }

            if (largePriceValue <= 0)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Large price must be greater than 0.", "OK");
                return;
            }

            // Image is optional; no validation required

            // Validate inventory sufficiency for selected ingredients
            var inventoryOk = await ValidateInventorySufficiencyAsync();
            if (!inventoryOk)
            {
                return;
            }

            var product = new POSPageModel
            {
                ProductName = ProductName,
                SmallPrice = smallPriceValue,
                MediumPrice = mediumPriceValue,
                LargePrice = largePriceValue,
                Category = SelectedCategory,
                Subcategory = EffectiveCategory,
                // Persist empty string when no image is provided to avoid provider null issues
                ImageSet = string.IsNullOrWhiteSpace(ImagePath) ? string.Empty : ImagePath,
                ProductDescription = ProductDescription,
                ColorCode = ProductColorCode
            };

            try
            {
                if (IsEditMode)
                {
                    // Update existing product
                    product.ProductID = EditingProductId;
                    int rowsAffected = await _database.UpdateProductAsync(product);
                    if (rowsAffected > 0)
                    {
                        // If user selected inventory links during edit, replace existing links
                        var selectedIngredientsOnly = ConnectPOSToInventoryVM?.SelectedIngredientsOnly?.ToList() ?? new();
                        var selectedAddonsOnly = ConnectPOSToInventoryVM?.SelectedAddons?.ToList() ?? new();
                        if (selectedIngredientsOnly.Count > 0 || selectedAddonsOnly.Count > 0)
                        {
                            // Treat everything selected in ConnectPOS as ingredients by default,
                            // except items explicitly chosen in the Addons popup (SelectedAddons)
                            var addonIds = new HashSet<int>(selectedAddonsOnly.Select(a => a.itemID));
                            var ingredients = selectedIngredientsOnly
                                .Where(i => !addonIds.Contains(i.itemID))
                                .Select(i => (
                                    inventoryItemId: i.itemID,
                                    amount: ConvertUnits(i.InputAmount > 0 ? i.InputAmount : 1, i.InputUnit, i.unitOfMeasurement),
                                    unit: (string?)i.unitOfMeasurement,
                                    amtS: ConvertUnits(i.InputAmountSmall > 0 ? i.InputAmountSmall : 1, i.InputUnitSmall, i.unitOfMeasurement),
                                    unitS: (string?) (string.IsNullOrWhiteSpace(i.InputUnitSmall) ? i.unitOfMeasurement : i.InputUnitSmall),
                                    amtM: ConvertUnits(i.InputAmountMedium > 0 ? i.InputAmountMedium : 1, i.InputUnitMedium, i.unitOfMeasurement),
                                    unitM: (string?) (string.IsNullOrWhiteSpace(i.InputUnitMedium) ? i.unitOfMeasurement : i.InputUnitMedium),
                                    amtL: ConvertUnits(i.InputAmountLarge > 0 ? i.InputAmountLarge : 1, i.InputUnitLarge, i.unitOfMeasurement),
                                    unitL: (string?) (string.IsNullOrWhiteSpace(i.InputUnitLarge) ? i.unitOfMeasurement : i.InputUnitLarge)
                                ));

                            var addons = selectedAddonsOnly
                                .Select(i => (
                                    inventoryItemId: i.itemID,
                                    amount: i.InputAmount > 0 ? i.InputAmount : 1,
                                    unit: i.InputUnit ?? "g",
                                    addonPrice: i.AddonPrice
                                ));

                            try
                            {
                                await _database.SaveProductLinksSplitAsync(product.ProductID, ingredients, addons);
                            }
                            catch (Exception ex)
                            {
                                await Application.Current.MainPage.DisplayAlert("Warning", $"Product updated but failed to link addons/ingredients: {ex.Message}", "OK");
                            }
                        }
                        await Application.Current.MainPage.DisplayAlert("Success", "Product updated successfully!", "OK");
                        // Do NOT deduct inventory here; deduction happens only upon successful transaction/payment
                        ResetForm();
                        ProductUpdated?.Invoke(product);
                    }
                }
                else
                {
                    // Add new product
                    var existingProducts = await _database.GetProductsAsync();
                    if (existingProducts.Any(p => p.ProductName == ProductName))
                    {
                        await Application.Current.MainPage.DisplayAlert("Error", "Product already exists!", "OK");
                        return;
                    }

                    // Require at least one ingredient or addon BEFORE inserting product
                    var selectedIngredientsOnly = ConnectPOSToInventoryVM?.SelectedIngredientsOnly?.ToList() ?? new();
                    var selectedAddonsOnly = ConnectPOSToInventoryVM?.SelectedAddons?.ToList() ?? new();
                    if (selectedIngredientsOnly.Count == 0 && selectedAddonsOnly.Count == 0)
                    {
                        await Application.Current.MainPage.DisplayAlert("Link ingredients", "Select ingredients or addons for this product. Opening selector...", "OK");
                        if (ConnectPOSToInventoryVM != null)
                        {
                            ConnectPOSToInventoryVM.IsConnectPOSToInventoryVisible = true;
                        }
                        return;
                    }

                    // Insert product and get new ID
                    int newProductId = await _database.SaveProductReturningIdAsync(product);
                    if (newProductId <= 0)
                    {
                        await Application.Current.MainPage.DisplayAlert("Error", "Failed to save product.", "OK");
                        return;
                    }

                    product.ProductID = newProductId;

                    // Build product→inventory links (addons/ingredients)
                    {
						var addonIds = new HashSet<int>(selectedAddonsOnly.Select(a => a.itemID));
						var ingredients = selectedIngredientsOnly
							.Where(i => !addonIds.Contains(i.itemID))
							.Select(i => (
								inventoryItemId: i.itemID,
								amount: (i.InputAmount > 0 ? i.InputAmount : 1),
								unit: (string?)(i.InputUnit ?? i.unitOfMeasurement)
							));

						var addons = selectedAddonsOnly
							.Select(i => {
								System.Diagnostics.Debug.WriteLine($"🔧 Addon: {i.itemName}");
								System.Diagnostics.Debug.WriteLine($"🔧 InputAmount: {i.InputAmount}, InputUnit: '{i.InputUnit}'");
								System.Diagnostics.Debug.WriteLine($"🔧 InventoryUnit: '{i.unitOfMeasurement}'");
								System.Diagnostics.Debug.WriteLine($"🔧 DefaultUnit: '{i.DefaultUnit}'");
								
								// Keep original values without any conversion
								var finalAmount = i.InputAmount > 0 ? i.InputAmount : 1;
								var finalUnit = i.InputUnit ?? "g"; // Default to grams if no unit specified
								
								System.Diagnostics.Debug.WriteLine($"🔧 Final values: amount={finalAmount}, unit='{finalUnit}'");
								
								return (
									inventoryItemId: i.itemID,
									amount: finalAmount,
									unit: finalUnit,
									addonPrice: i.AddonPrice
								);
							});

                        try
                        {
                            // Rebuild ingredients with per-size values for the overload
                            var ingredientsPerSize = selectedIngredientsOnly
                                .Where(i => !addonIds.Contains(i.itemID))
                                .Select(i => (
                                    inventoryItemId: i.itemID,
                                    amount: (i.InputAmount > 0 ? i.InputAmount : 1),
                                    unit: (string?)(i.InputUnit ?? i.unitOfMeasurement),
                                    amtS: (i.InputAmountSmall > 0 ? i.InputAmountSmall : 1),
                                    unitS: (string?)(string.IsNullOrWhiteSpace(i.InputUnitSmall) ? i.unitOfMeasurement : i.InputUnitSmall),
                                    amtM: (i.InputAmountMedium > 0 ? i.InputAmountMedium : 1),
                                    unitM: (string?)(string.IsNullOrWhiteSpace(i.InputUnitMedium) ? i.unitOfMeasurement : i.InputUnitMedium),
                                    amtL: (i.InputAmountLarge > 0 ? i.InputAmountLarge : 1),
                                    unitL: (string?)(string.IsNullOrWhiteSpace(i.InputUnitLarge) ? i.unitOfMeasurement : i.InputUnitLarge)
                                ));

                            await _database.SaveProductLinksSplitAsync(newProductId, ingredientsPerSize, addons);
                        }
                        catch (Exception ex)
                        {
                            await Application.Current.MainPage.DisplayAlert("Warning", $"Product saved but failed to link addons/ingredients: {ex.Message}", "OK");
                        }
                    }

                    // Close preview popup first, then show toast-style success card (bottom-right)
                    ConnectPOSToInventoryVM.IsConnectPOSToInventoryVisible = false;
                    IsAddItemToPOSVisible = false;
                    var app = (App)Application.Current;
                    app?.SuccessCardPopup?.Show(
                        "Product Added To Menu",
                        $"{product.ProductName} has been added to the menu",
                        $"ID: {newProductId}",
                        1500);
                    // Do NOT deduct inventory here; deduction happens only upon successful transaction/payment
                    ResetForm();
                    ProductAdded?.Invoke(product);
                        }
            }
            catch (Exception ex)
            {
                string action = IsEditMode ? "update" : "add";
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to {action} product: {ex.Message}", "OK");
            }
        }

        private async Task<bool> ValidateInventorySufficiencyAsync()
        {
            var selected = ConnectPOSToInventoryVM?.InventoryItems?.Where(i => i.IsSelected).ToList();
            if (selected == null || selected.Count == 0) return true; // nothing to validate

            List<string> issues = new();
            
            // Validate selected ingredients
            foreach (var it in selected)
            {
                // Use the current InputAmount and InputUnit from the UI
                var inputAmount = it.InputAmount > 0 ? it.InputAmount : 1;
                var inputUnit = it.InputUnit ?? it.DefaultUnit;
                var toUnit = (it.unitOfMeasurement ?? string.Empty).Trim();
                
                // Convert requested amount to the inventory unit
                var amountRequested = ConvertUnits(inputAmount, inputUnit, toUnit);

                if (amountRequested <= 0)
                {
                    issues.Add($"{it.itemName}: incompatible unit conversion ({inputUnit} → {toUnit}).");
                    continue;
                }

                if (amountRequested > it.itemQuantity)
                {
                    issues.Add($"{it.itemName}: needs {amountRequested:F2} {toUnit}, only {it.itemQuantity:F2} {toUnit} available.");
                }
            }
            
            // Validate automatic cup and straw
            await ValidateAutomaticCupAndStraw(issues);

            if (issues.Count > 0)
            {
                string msg = "Insufficient inventory:\n" + string.Join("\n", issues);
                await Application.Current.MainPage.DisplayAlert("Out of stock", msg, "OK");
                return false;
            }

            return true;
        }

        private async Task ValidateAutomaticCupAndStraw(List<string> issues)
        {
            try
            {
                // Get the selected size and input amount
                var selectedSize = ConnectPOSToInventoryVM?.SelectedSize ?? "Medium";
                var inputAmount = ConnectPOSToInventoryVM?.SelectedIngredientsOnly?.FirstOrDefault()?.InputAmount ?? 1;
                
                // Get appropriate cup name
                string cupName = selectedSize switch
                {
                    "Small" => "Small Cup",
                    "Medium" => "Medium Cup", 
                    "Large" => "Large Cup",
                    _ => "Medium Cup"
                };
                
                // Validate cup availability
                var cupItem = await _database.GetInventoryItemByNameAsync(cupName);
                if (cupItem != null)
                {
                    if (inputAmount > cupItem.itemQuantity)
                    {
                        issues.Add($"{cupName}: needs {inputAmount} pcs, only {cupItem.itemQuantity} pcs available.");
                    }
                }
                else
                {
                    issues.Add($"{cupName}: not found in inventory.");
                }
                
                // Validate straw availability
                var strawItem = await _database.GetInventoryItemByNameAsync("Straw");
                if (strawItem != null)
                {
                    if (inputAmount > strawItem.itemQuantity)
                    {
                        issues.Add($"Straw: needs {inputAmount} pcs, only {strawItem.itemQuantity} pcs available.");
                    }
                }
                else
                {
                    issues.Add("Straw: not found in inventory.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error validating automatic cup and straw: {ex.Message}");
                issues.Add("Error validating cup and straw availability.");
            }
        }

        private async Task DeductSelectedIngredientsAsync()
        {
            if (ConnectPOSToInventoryVM?.InventoryItems == null || !ConnectPOSToInventoryVM.InventoryItems.Any(i => i.IsSelected))
                return;

            // Get selected ingredients with their input amounts and units
            var selected = ConnectPOSToInventoryVM.InventoryItems.Where(i => i.IsSelected);
            var pairs = new List<(string name, double amount)>();
            
            // Add selected ingredients
            foreach (var i in selected)
            {
                var inputAmount = i.InputAmount > 0 ? i.InputAmount : 1;
                var inputUnit = i.InputUnit ?? i.DefaultUnit;
                var convertedAmount = ConvertUnits(inputAmount, inputUnit, i.unitOfMeasurement);
                
                pairs.Add((name: i.itemName, amount: convertedAmount));
            }
            
            // Automatically add cup and straw based on selected size and input amount
            await AddAutomaticCupAndStraw(pairs);

            try
            {
                var affectedRows = await _database.DeductInventoryAsync(pairs);
                if (affectedRows > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Successfully deducted inventory for {affectedRows} items");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Warning", $"Product saved but failed to deduct inventory: {ex.Message}", "OK");
            }
        }

        private async Task AddAutomaticCupAndStraw(List<(string name, double amount)> pairs)
        {
            try
            {
                // Get the selected size from the ConnectPOSItemToInventoryViewModel
                var selectedSize = ConnectPOSToInventoryVM?.SelectedSize ?? "Medium";
                var inputAmount = ConnectPOSToInventoryVM?.SelectedIngredientsOnly?.FirstOrDefault()?.InputAmount ?? 1;
                
                // Add appropriate cup based on size
                string cupName = selectedSize switch
                {
                    "Small" => "Small Cup",
                    "Medium" => "Medium Cup", 
                    "Large" => "Large Cup",
                    _ => "Medium Cup"
                };
                
                // Add 1 cup per serving (input amount)
                pairs.Add((name: cupName, amount: inputAmount));
                
                // Add 1 straw per serving (input amount)
                pairs.Add((name: "Straw", amount: inputAmount));
                
                System.Diagnostics.Debug.WriteLine($"Automatically added: {inputAmount} {cupName} and {inputAmount} Straw");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding automatic cup and straw: {ex.Message}");
            }
        }

		private static bool IsAddonCategory(string? category)
		{
			var normalized = (category ?? string.Empty).Trim();
			if (string.IsNullOrEmpty(normalized)) return false;
			return string.Equals(normalized, "Addons", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(normalized, "Sinkers & etc.", StringComparison.OrdinalIgnoreCase)
				|| normalized.Contains("Sinker", StringComparison.OrdinalIgnoreCase);
		}

        private static double ConvertUnits(double amount, string fromUnit, string toUnit)
        {
            if (string.IsNullOrWhiteSpace(toUnit)) return amount; // no target unit, pass-through
            
            // Normalize units to short form
            fromUnit = NormalizeUnit(fromUnit);
            toUnit = NormalizeUnit(toUnit);

            // If units are the same, no conversion needed
            if (string.Equals(fromUnit, toUnit, StringComparison.OrdinalIgnoreCase))
            {
                return amount;
            }

            // Mass conversions
            if (IsMassUnit(fromUnit) && IsMassUnit(toUnit))
            {
                return fromUnit.ToLowerInvariant() switch
                {
                    "kg" when toUnit.ToLowerInvariant() == "g" => amount * 1000,
                    "g" when toUnit.ToLowerInvariant() == "kg" => amount / 1000,
                    _ => amount
                };
            }

            // Volume conversions
            if (IsVolumeUnit(fromUnit) && IsVolumeUnit(toUnit))
            {
                return fromUnit.ToLowerInvariant() switch
                {
                    "l" when toUnit.ToLowerInvariant() == "ml" => amount * 1000,
                    "ml" when toUnit.ToLowerInvariant() == "l" => amount / 1000,
                    _ => amount
                };
            }

            // Count
            if (IsCountUnit(fromUnit) && IsCountUnit(toUnit))
            {
                return amount;
            }

            // If from is empty, assume already in inventory unit
            if (string.IsNullOrWhiteSpace(fromUnit)) return amount;

            // Unknown or mismatched units → block deduction by returning 0
            return 0;
        }

        private static string NormalizeUnit(string unit)
        {
            if (string.IsNullOrWhiteSpace(unit)) return string.Empty;
            
            var normalized = unit.Trim().ToLowerInvariant();
            
            // Handle full unit names from database
            if (normalized.Contains("kilograms") || normalized == "kg") return "kg";
            if (normalized.Contains("grams") || normalized == "g") return "g";
            if (normalized.Contains("liters") || normalized == "l") return "l";
            if (normalized.Contains("milliliters") || normalized == "ml") return "ml";
            if (normalized.Contains("pcs") || normalized.Contains("pieces")) return "pcs";
            
            return normalized;
        }

        private static bool IsMassUnit(string unit)
        {
            var normalized = NormalizeUnit(unit);
            return normalized == "kg" || normalized == "g";
        }

        private static bool IsVolumeUnit(string unit)
        {
            var normalized = NormalizeUnit(unit);
            return normalized == "l" || normalized == "ml";
        }

        private static bool IsCountUnit(string unit)
        {
            var normalized = NormalizeUnit(unit);
            return normalized == "pcs" || string.IsNullOrEmpty(normalized);
        }

        private void ResetForm()
        {
            ProductName = string.Empty;
            SmallPrice = string.Empty;
            MediumPrice = string.Empty;
            LargePrice = string.Empty;
            SelectedCategory = string.Empty;
            SelectedSubcategory = null;
            ImagePath = string.Empty;
            SelectedImageSource = null;
            ProductDescription = string.Empty;
            ProductColorCode = string.Empty;
            IsAddItemToPOSVisible = false;
            IsEditMode = false;
            EditingProductId = 0;
            
            // Reset the ConnectPOSToInventoryVM as well
            if (ConnectPOSToInventoryVM != null)
            {
                ConnectPOSToInventoryVM.IsConnectPOSToInventoryVisible = false;
                ConnectPOSToInventoryVM.IsInputIngredientsVisible = false;
                ConnectPOSToInventoryVM.IsPreviewVisible = false;
                ConnectPOSToInventoryVM.IsAddonPopupVisible = false;
                ConnectPOSToInventoryVM.ProductName = string.Empty;
                ConnectPOSToInventoryVM.SelectedCategory = string.Empty;
                ConnectPOSToInventoryVM.SmallPrice = string.Empty;
                ConnectPOSToInventoryVM.MediumPrice = string.Empty;
                ConnectPOSToInventoryVM.LargePrice = string.Empty;
                ConnectPOSToInventoryVM.SelectedImageSource = null;
                ConnectPOSToInventoryVM.ProductDescription = string.Empty;
                
                // Clear inventory selections
                if (ConnectPOSToInventoryVM.InventoryItems != null)
                {
                    foreach (var item in ConnectPOSToInventoryVM.InventoryItems)
                    {
                        item.IsSelected = false;
                        item.InputAmount = 0;
                        item.InputUnit = string.Empty;
                        item.AddonQuantity = 0;
                    }
                }
                
                // Reset addons popup if it exists
                if (ConnectPOSToInventoryVM.AddonsPopup != null)
                {
                    ConnectPOSToInventoryVM.AddonsPopup.IsAddonsPopupVisible = false;
                }
            }
        }
        [RelayCommand]
        private void OpenConnectPOSToInventory()
        {
            // Hide parent view and show the popup from the child VM
            IsAddItemToPOSVisible = false;
            // Populate preview data on child VM
            ConnectPOSToInventoryVM.ProductName = ProductName;
            // Use main category so Coffee immediately enables Small without requiring subcategory
            ConnectPOSToInventoryVM.SelectedCategory = SelectedCategory;
            ConnectPOSToInventoryVM.SmallPrice = SmallPrice;
            ConnectPOSToInventoryVM.MediumPrice = MediumPrice;
            ConnectPOSToInventoryVM.LargePrice = LargePrice;
            ConnectPOSToInventoryVM.SelectedImageSource = SelectedImageSource;
            // Propagate description to child VM
            ConnectPOSToInventoryVM.ProductDescription = ProductDescription;

            // Load inventory list then show
            _ = ConnectPOSToInventoryVM.LoadInventoryAsync();
            ConnectPOSToInventoryVM.IsConnectPOSToInventoryVisible = true;
        }

        [RelayCommand]
        public async Task PickImageAsync()
        {
            try
            {
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Select an image",
                    FileTypes = FilePickerFileType.Images
                });

                if (result != null)
                {
                    ImagePath = result.FullPath;


                    SelectedImageSource = ImageSource.FromFile(result.FullPath);
                }
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }
}