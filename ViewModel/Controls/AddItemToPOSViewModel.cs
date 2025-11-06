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
using Coftea_Capstone.Models.Service;

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

        public ObservableCollection<string> Categories { get; set; } = new ObservableCollection<string> // Predefined categories
        {
            "Frappe",
            "Fruit/Soda",
            "Milktea",
            "Coffee"
        };

        [ObservableProperty]
        private string selectedCategory;

        public ObservableCollection<string> FruitSodaSubcategories { get; } = new ObservableCollection<string> // Predefined subcategories for Fruit/Soda
        {
            "Fruit",
            "Soda"
        };

        public ObservableCollection<string> CoffeeSubcategories { get; } = new ObservableCollection<string> // Predefined subcategories for Coffee
        {
            "Americano",
            "Latte"
        };

        [ObservableProperty]
        private string selectedSubcategory; 

        public bool IsFruitSodaSubcategoryVisible => string.Equals(SelectedCategory, "Fruit/Soda", StringComparison.OrdinalIgnoreCase); // Show when Fruit/Soda is selected

        public bool IsCoffeeSubcategoryVisible => string.Equals(SelectedCategory, "Coffee", StringComparison.OrdinalIgnoreCase); // Show when Coffee is selected

        // Pricing field visibility properties
        public bool IsSmallPriceVisible => string.Equals(SelectedCategory, "Coffee", StringComparison.OrdinalIgnoreCase);
        
        public bool IsMediumPriceVisible => true; // Always visible
        
        public bool IsLargePriceVisible => true; // Always visible

        public string EffectiveCategory => 
            string.Equals(SelectedCategory, "Fruit/Soda", StringComparison.OrdinalIgnoreCase)
                ? (string.IsNullOrWhiteSpace(SelectedSubcategory) ? null : SelectedSubcategory)
                : string.Equals(SelectedCategory, "Coffee", StringComparison.OrdinalIgnoreCase)
                    ? (string.IsNullOrWhiteSpace(SelectedSubcategory) ? null : SelectedSubcategory)
                    : SelectedCategory; // Use Subcategory for Fruit/Soda and Coffee, else Category

        partial void OnSelectedCategoryChanged(string value) // Handle category changes
        {
            // Validate that selected category is in the Categories list
            if (!string.IsNullOrWhiteSpace(value) && !Categories.Contains(value))
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Invalid category selected: {value}, resetting to null");
                SelectedCategory = null;
                return;
            }

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

        partial void OnSelectedSubcategoryChanged(string value) // Handle subcategory changes
        {
            if (string.IsNullOrWhiteSpace(value)) return;

            // Validate Fruit/Soda subcategory
            if (string.Equals(SelectedCategory, "Fruit/Soda", StringComparison.OrdinalIgnoreCase))
            {
                if (!FruitSodaSubcategories.Contains(value))
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Invalid Fruit/Soda subcategory selected: {value}, resetting to null");
                    SelectedSubcategory = null;
                    return;
                }
            }
            // Validate Coffee subcategory
            else if (string.Equals(SelectedCategory, "Coffee", StringComparison.OrdinalIgnoreCase))
            {
                if (!CoffeeSubcategories.Contains(value))
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Invalid Coffee subcategory selected: {value}, resetting to null");
                    SelectedSubcategory = null;
                    return;
                }
            }
        }

        partial void OnIsEditModeChanged(bool value)
        {
            // Notify price visibility changes when edit mode changes
            OnPropertyChanged(nameof(IsSmallPriceVisible));
            OnPropertyChanged(nameof(IsMediumPriceVisible));
            OnPropertyChanged(nameof(IsLargePriceVisible));
            
            // Update the ConnectPOSToInventoryVM edit mode
            if (ConnectPOSToInventoryVM != null)
            {
                ConnectPOSToInventoryVM.IsEditMode = value;
            }
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
            _database = new Database();

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
        private async Task SaveProduct() // Save product immediately in edit mode
        {
            System.Diagnostics.Debug.WriteLine("🔧 SaveProduct called - saving current product details immediately");
            await AddProduct();
        }

        [RelayCommand]
        public async Task AddProduct() // Validates and adds/updates the product
        {
            // Block immediately if no internet for DB-backed save
            if (!Services.NetworkService.HasInternetConnection())
            {
                try { await Application.Current.MainPage.DisplayAlert("No Internet", "No internet connection. Please check your network and try again.", "OK"); } catch { }
                return;
            }
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
            // Small price is only required for Coffee category
            decimal smallPriceValue = 0;
            bool isCoffeeCategory = string.Equals(SelectedCategory, "Coffee", StringComparison.OrdinalIgnoreCase);
            
            if (isCoffeeCategory)
            {
                // Coffee category requires a valid small price
                if (!decimal.TryParse(SmallPrice, out smallPriceValue) || smallPriceValue <= 0)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Please enter a valid small price for Coffee (must be greater than 0).", "OK");
                    return;
                }
            }
            else
            {
                // Non-Coffee categories: small price is optional, default to 0 (saved as NULL in database)
                if (!string.IsNullOrWhiteSpace(SmallPrice))
                {
                    decimal.TryParse(SmallPrice, out smallPriceValue);
                }
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
                System.Diagnostics.Debug.WriteLine($"💾 Saving product: {product.ProductName}");
                System.Diagnostics.Debug.WriteLine($"💾 Category: {product.Category}, SmallPrice: {product.SmallPrice}, Medium: {product.MediumPrice}, Large: {product.LargePrice}");
                
                if (IsEditMode)
                {
                    // Update existing product
                    product.ProductID = EditingProductId;
                    int rowsAffected = await _database.UpdateProductAsync(product);
                    if (rowsAffected > 0)
                    {
                        // Build product→inventory links (addons/ingredients)
                        var selectedIngredientsOnly = ConnectPOSToInventoryVM?.SelectedIngredientsOnly?.ToList() ?? new();
                        var selectedAddonsOnly = ConnectPOSToInventoryVM?.SelectedAddons?.ToList() ?? new();
                        
                        // Validate unit compatibility before saving
                        foreach (var ingredient in selectedIngredientsOnly)
                        {
                            if (!AreUnitsCompatible(ingredient.InputUnit, ingredient.unitOfMeasurement))
                            {
                                await Application.Current.MainPage.DisplayAlert("Error", 
                                    $"Incompatible units for {ingredient.itemName}: Cannot use {ingredient.InputUnit} with inventory unit {ingredient.unitOfMeasurement}. Please use compatible units (kg/g for mass, L/ml for volume).", 
                                    "OK");
                                return;
                            }
                        }
                        
                        // Check if this product has Small size (only Coffee category)
                        bool hasSmallSize = IsSmallPriceVisible;
                            
                        // Allow items to be saved as BOTH ingredients AND addons
                        // An item can be both a main ingredient (with its own amount) and an optional addon (with a different amount/price)
                        var ingredients = selectedIngredientsOnly
                            .Select(i => {
                                // Ensure fallback unit is never null or empty
                                var fallbackUnit = !string.IsNullOrWhiteSpace(i.unitOfMeasurement) ? i.unitOfMeasurement : "pcs";
                                
                                return (
                                    inventoryItemId: i.itemID,
                                    amount: (i.InputAmount > 0 ? i.InputAmount : 1),
                                    unit: (string?)(i.InputUnit ?? fallbackUnit),
                                    // Only save Small size data if product has Small size
                                    amtS: hasSmallSize ? (i.InputAmountSmall > 0 ? i.InputAmountSmall : 1) : 0,
                                    unitS: hasSmallSize 
                                        ? (!string.IsNullOrWhiteSpace(i.InputUnitSmall) ? i.InputUnitSmall : fallbackUnit)
                                        : fallbackUnit, // Use the inventory item's unit if product doesn't have small size

                                    amtM: (i.InputAmountMedium > 0 ? i.InputAmountMedium : 1),
                                    unitM: !string.IsNullOrWhiteSpace(i.InputUnitMedium) ? i.InputUnitMedium : fallbackUnit,

                                    amtL: (i.InputAmountLarge > 0 ? i.InputAmountLarge : 1),
                                    unitL: !string.IsNullOrWhiteSpace(i.InputUnitLarge) ? i.InputUnitLarge : fallbackUnit
                                );
                            });

                        var addons = selectedAddonsOnly
                            .Select(i => (
                                inventoryItemId: i.itemID,
                                amount: i.InputAmount > 0 ? i.InputAmount : 1,
                                unit: i.InputUnit ?? "g",
                                addonPrice: i.AddonPrice
                            ));

                        try
                        {
                            // Use the per-size overload for editing
                            await _database.SaveProductLinksSplitAsync(product.ProductID, ingredients, addons);
                        }
                        catch (Exception ex)
                        {
                            await Application.Current.MainPage.DisplayAlert("Warning", $"Product updated but failed to link addons/ingredients: {ex.Message}", "OK");
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
                    // Check for duplicate: same name AND same category AND same subcategory
                    var duplicateProduct = existingProducts.FirstOrDefault(p => 
                        string.Equals(p.ProductName, ProductName, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(p.Category, SelectedCategory, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(p.Subcategory ?? string.Empty, EffectiveCategory ?? string.Empty, StringComparison.OrdinalIgnoreCase));
                    
                    if (duplicateProduct != null)
                    {
                        await Application.Current.MainPage.DisplayAlert("Error", "A product with this name, category, and subcategory already exists!", "OK");
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
                    
                    // Validate unit compatibility before saving
                    foreach (var ingredient in selectedIngredientsOnly)
                    {
                        if (!AreUnitsCompatible(ingredient.InputUnit, ingredient.unitOfMeasurement))
                        {
                            await Application.Current.MainPage.DisplayAlert("Error", 
                                $"Incompatible units for {ingredient.itemName}: Cannot use {ingredient.InputUnit} with inventory unit {ingredient.unitOfMeasurement}. Please use compatible units (kg/g for mass, L/ml for volume).", 
                                "OK");
                            return;
                        }
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
                        // Allow items to be saved as BOTH ingredients AND addons
                        // An item can be both a main ingredient (with its own amount) and an optional addon (with a different amount/price)

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
                            // Check if this product has Small size (only Coffee category)
                            bool hasSmallSize = IsSmallPriceVisible;
                            
                            // Rebuild ingredients with per-size values for the overload
                            // Allow items to be saved as BOTH ingredients AND addons
                            // An item can be both a main ingredient (with its own amount) and an optional addon (with a different amount/price)
                            var ingredientsPerSize = selectedIngredientsOnly
                                .Select(i => {
                                    // Ensure fallback unit is never null or empty
                                    var fallbackUnit = !string.IsNullOrWhiteSpace(i.unitOfMeasurement) ? i.unitOfMeasurement : "pcs";
                                    
                                    return (
                                        inventoryItemId: i.itemID,
                                        amount: (i.InputAmount > 0 ? i.InputAmount : 1),
                                        unit: (string?)(i.InputUnit ?? fallbackUnit),
                                        // Only save Small size data if product has Small size
                                        amtS: hasSmallSize ? (i.InputAmountSmall > 0 ? i.InputAmountSmall : 1) : 0,
                                        unitS: hasSmallSize 
                                            ? (!string.IsNullOrWhiteSpace(i.InputUnitSmall) ? i.InputUnitSmall : fallbackUnit)
                                            : fallbackUnit, // Use the inventory item's unit if product doesn't have small size
                                        amtM: (i.InputAmountMedium > 0 ? i.InputAmountMedium : 1),
                                        unitM: !string.IsNullOrWhiteSpace(i.InputUnitMedium) ? i.InputUnitMedium : fallbackUnit,
                                        amtL: (i.InputAmountLarge > 0 ? i.InputAmountLarge : 1),
                                        unitL: !string.IsNullOrWhiteSpace(i.InputUnitLarge) ? i.InputUnitLarge : fallbackUnit
                                    );
                                });

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
                System.Diagnostics.Debug.WriteLine($"❌ Error saving product: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Inner exception: {ex.InnerException.Message}");
                }
                
                string action = IsEditMode ? "update" : "add";
                var errorMessage = ex.Message;
                
                // Provide helpful message for NULL constraint errors
                if (ex.Message.Contains("cannot be null") || ex.Message.Contains("NULL") || ex.Message.Contains("smallPrice"))
                {
                    errorMessage = $"{ex.Message}\n\n💡 Tip: Run this SQL to fix your database:\nALTER TABLE products MODIFY COLUMN smallPrice DECIMAL(10,2) DEFAULT NULL;";
                }
                
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to {action} product: {errorMessage}", "OK");
            }
        }


        [RelayCommand]
        private void CloseAddItemToPOS() // Closes the AddItemToPOS Overlay
        {
            IsAddItemToPOSVisible = false;
            ResetForm();
        }

        public async Task SetEditMode(POSPageModel product) // Prepares the ViewModel for editing an existing product
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
            // Load and set ingredient data directly (like how other properties are set)
            await LoadIngredientDataForEdit(product.ProductID);
        }

        // Shows the AddItemToPOS overlay when returning from child popups
        public void SetIsAddItemToPOSVisibleTrue()
        {
            IsAddItemToPOSVisible = true;
        }

        private async Task LoadIngredientDataForEdit(int productId) // Loads ingredient data for editing (simplified approach)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔧 LoadIngredientDataForEdit: Starting for product ID {productId}");
                
                // Ensure inventory is loaded
                if (ConnectPOSToInventoryVM.AllInventoryItems == null || !ConnectPOSToInventoryVM.AllInventoryItems.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"🔧 LoadIngredientDataForEdit: Loading inventory...");
                    await ConnectPOSToInventoryVM.LoadInventoryAsync();
                }

                var links = await _database.GetProductIngredientsAsync(productId);
                System.Diagnostics.Debug.WriteLine($"🔧 LoadIngredientDataForEdit: Found {links?.Count() ?? 0} linked ingredients");
                System.Diagnostics.Debug.WriteLine($"🔧 LoadIngredientDataForEdit: AllInventoryItems count: {ConnectPOSToInventoryVM.AllInventoryItems?.Count ?? 0}");
                
                // Clear existing selections first
                if (ConnectPOSToInventoryVM.AllInventoryItems != null)
                {
                    foreach (var item in ConnectPOSToInventoryVM.AllInventoryItems)
                    {
                        item.IsSelected = false;
                    }
                }
                
                foreach (var (item, amount, unit, role) in links)
                {
                    System.Diagnostics.Debug.WriteLine($"🔧 LoadIngredientDataForEdit: Processing {item.itemName} (ID: {item.itemID})");
                    
                    // Find matching inventory item
                    var inv = ConnectPOSToInventoryVM.AllInventoryItems?.FirstOrDefault(i => i.itemID == item.itemID);
                    if (inv == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ LoadIngredientDataForEdit: Item {item.itemName} (ID: {item.itemID}) NOT FOUND in AllInventoryItems!");
                        System.Diagnostics.Debug.WriteLine($"⚠️ LoadIngredientDataForEdit: Adding new item {item.itemName} to collections");
                        // If not found yet, add it to collections
                        inv = item;
                        ConnectPOSToInventoryVM.AllInventoryItems.Add(inv);
                        ConnectPOSToInventoryVM.InventoryItems.Add(inv);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"✅ LoadIngredientDataForEdit: Found {inv.itemName} (ID: {inv.itemID}) in AllInventoryItems");
                    }

                    // Mark as selected
                    System.Diagnostics.Debug.WriteLine($"🔧 LoadIngredientDataForEdit: Setting {inv.itemName} as selected");
                    inv.IsSelected = true;
                    
                    // Debug: Verify the selection state was set
                    System.Diagnostics.Debug.WriteLine($"🔧 LoadIngredientDataForEdit: {inv.itemName} IsSelected = {inv.IsSelected}");
                    
                    // Use the original amount and unit from database link as primary values
                    var sharedAmt = amount > 0 ? amount : 1;
                    var sharedUnit = string.IsNullOrWhiteSpace(unit) ? inv.DefaultUnit : unit;
                    
                    // Only use per-size values if they exist and are reasonable (not tiny converted values)
                    inv.InputAmountSmall = (item.InputAmountSmall > 0 && item.InputAmountSmall >= 0.1) ? item.InputAmountSmall : sharedAmt;
                    inv.InputAmountMedium = (item.InputAmountMedium > 0 && item.InputAmountMedium >= 0.1) ? item.InputAmountMedium : sharedAmt;
                    inv.InputAmountLarge = (item.InputAmountLarge > 0 && item.InputAmountLarge >= 0.1) ? item.InputAmountLarge : sharedAmt;
                    inv.InputUnitSmall = !string.IsNullOrWhiteSpace(item.InputUnitSmall) ? item.InputUnitSmall : sharedUnit;
                    inv.InputUnitMedium = !string.IsNullOrWhiteSpace(item.InputUnitMedium) ? item.InputUnitMedium : sharedUnit;
                    inv.InputUnitLarge = !string.IsNullOrWhiteSpace(item.InputUnitLarge) ? item.InputUnitLarge : sharedUnit;
                    
                    // Debug: Log the size-specific values being loaded
                    System.Diagnostics.Debug.WriteLine($"🔧 LoadIngredientDataForEdit: {inv.itemName} size-specific values:");
                    System.Diagnostics.Debug.WriteLine($"🔧   Small: {inv.InputAmountSmall} {inv.InputUnitSmall}");
                    System.Diagnostics.Debug.WriteLine($"🔧   Medium: {inv.InputAmountMedium} {inv.InputUnitMedium}");
                    System.Diagnostics.Debug.WriteLine($"🔧   Large: {inv.InputAmountLarge} {inv.InputUnitLarge}");
                    
                    // Set current working values based on selected size
                    var currentSize = ConnectPOSToInventoryVM.SelectedSize;
                    inv.InputAmount = currentSize switch
                    {
                        "Small" => inv.InputAmountSmall,
                        "Medium" => inv.InputAmountMedium,
                        "Large" => inv.InputAmountLarge,
                        _ => sharedAmt
                    };
                    inv.InputUnit = currentSize switch
                    {
                        "Small" => inv.InputUnitSmall,
                        "Medium" => inv.InputUnitMedium,
                        "Large" => inv.InputUnitLarge,
                        _ => sharedUnit
                    };
                    
                    // Debug: Log the current working values
                    System.Diagnostics.Debug.WriteLine($"🔧 LoadIngredientDataForEdit: {inv.itemName} current working values for {currentSize}:");
                    System.Diagnostics.Debug.WriteLine($"🔧   InputAmount: {inv.InputAmount}");
                    System.Diagnostics.Debug.WriteLine($"🔧   InputUnit: {inv.InputUnit}");
                }

                // Update derived collections
                ConnectPOSToInventoryVM.RefreshSelectionAndFilter();
                
                // Force refresh the SelectedIngredientsOnly collection to ensure it's populated
                ConnectPOSToInventoryVM.ForceRefreshSelectedIngredients();
                
                // Force UI refresh for all selected ingredients to ensure InputAmountText is updated
                // Note: InventoryPageModel handles its own property change notifications
                
                // Force UI refresh - the UpdateSelectedIngredientsOnly method will handle property notifications
                
                System.Diagnostics.Debug.WriteLine($"🔧 LoadIngredientDataForEdit: Completed. SelectedIngredientsOnly count: {ConnectPOSToInventoryVM.SelectedIngredientsOnly?.Count ?? 0}");
                System.Diagnostics.Debug.WriteLine($"🔧 LoadIngredientDataForEdit: Total selected items in AllInventoryItems: {ConnectPOSToInventoryVM.AllInventoryItems.Count(i => i.IsSelected)}");
                
                // Debug: Log all selected items
                if (ConnectPOSToInventoryVM.SelectedIngredientsOnly != null)
                {
                    foreach (var item in ConnectPOSToInventoryVM.SelectedIngredientsOnly)
                    {
                        System.Diagnostics.Debug.WriteLine($"🔧 Final selected ingredient: {item.itemName} (ID: {item.itemID}) - IsSelected: {item.IsSelected}");
                    }
                }
                
                // Load addons for edit mode
                await LoadAddonDataForEdit(productId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"🔧 LoadIngredientDataForEdit: Error - {ex.Message}");
            }
        }

        private async Task LoadAddonDataForEdit(int productId) // Loads addon data for editing
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔧 LoadAddonDataForEdit: Starting for product ID {productId}");
                
                // Get addons from database
                var addonLinks = await _database.GetProductAddonsAsync(productId);
                System.Diagnostics.Debug.WriteLine($"🔧 LoadAddonDataForEdit: Found {addonLinks?.Count ?? 0} linked addons");
                
                if (addonLinks == null || !addonLinks.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"🔧 LoadAddonDataForEdit: No addons found for this product");
                    return;
                }
                
                // Clear existing addon selections
                ConnectPOSToInventoryVM.SelectedAddons.Clear();
                
                foreach (var addon in addonLinks)
                {
                    System.Diagnostics.Debug.WriteLine($"🔧 LoadAddonDataForEdit: Processing addon {addon.itemName} (ID: {addon.itemID})");
                    
                    // Set addon properties from database
                    // DO NOT set IsSelected = true here - addons should NOT appear in ingredient list
                    addon.InputAmount = addon.InputAmountSmall > 0 ? addon.InputAmountSmall : 1; // Use saved amount
                    addon.InputUnit = !string.IsNullOrWhiteSpace(addon.InputUnitSmall) ? addon.InputUnitSmall : addon.unitOfMeasurement;
                    addon.AddonPrice = addon.AddonPrice; // This is already loaded from GetProductAddonsAsync
                    
                    System.Diagnostics.Debug.WriteLine($"🔧 LoadAddonDataForEdit: {addon.itemName} - Amount: {addon.InputAmount}, Unit: {addon.InputUnit}, Price: {addon.AddonPrice}");
                    
                    // Add to SelectedAddons collection ONLY (not to ingredient lists)
                    ConnectPOSToInventoryVM.SelectedAddons.Add(addon);
                    
                    // Update AllInventoryItems with addon configuration but DO NOT mark as selected
                    // This keeps addons separate from ingredients
                    var existingInAll = ConnectPOSToInventoryVM.AllInventoryItems?.FirstOrDefault(i => i.itemID == addon.itemID);
                    if (existingInAll == null)
                    {
                        // If not in AllInventoryItems, add it but keep IsSelected = false
                        addon.IsSelected = false;
                        ConnectPOSToInventoryVM.AllInventoryItems.Add(addon);
                        System.Diagnostics.Debug.WriteLine($"🔧 LoadAddonDataForEdit: Added {addon.itemName} to AllInventoryItems (IsSelected=false)");
                    }
                    else
                    {
                        // Update existing item with addon data but DO NOT set IsSelected
                        // Keep IsSelected as false so it doesn't appear in ingredient UI
                        // BUT: If item is also an ingredient (IsSelected == true), preserve ingredient amounts
                        existingInAll.AddonPrice = addon.AddonPrice;
                        
                        // Only update InputAmount/InputUnit if the item is NOT also an ingredient
                        // If it's an ingredient (IsSelected == true), preserve the ingredient amounts
                        if (!existingInAll.IsSelected)
                        {
                            // Item is only an addon, not an ingredient - update InputAmount
                            existingInAll.InputAmount = addon.InputAmount;
                            existingInAll.InputUnit = addon.InputUnit;
                            // Explicitly ensure IsSelected remains false for addons
                            existingInAll.IsSelected = false;
                            System.Diagnostics.Debug.WriteLine($"🔧 LoadAddonDataForEdit: Updated {existingInAll.itemName} in AllInventoryItems as addon only (InputAmount={addon.InputAmount})");
                        }
                        else
                        {
                            // Item is BOTH ingredient and addon - preserve ingredient InputAmount/InputUnit
                            // Addon amount is stored separately in SelectedAddons collection
                            System.Diagnostics.Debug.WriteLine($"🔧 LoadAddonDataForEdit: {existingInAll.itemName} is both ingredient and addon - preserving ingredient amounts (InputAmount={existingInAll.InputAmount}, Addon amount={addon.InputAmount})");
                        }
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"🔧 LoadAddonDataForEdit: Completed. SelectedAddons count: {ConnectPOSToInventoryVM.SelectedAddons.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ LoadAddonDataForEdit: Error - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ LoadAddonDataForEdit: StackTrace - {ex.StackTrace}");
            }
        }

        private async Task PreloadLinkedIngredientsAsync(int productId) // Preloads linked ingredients for editing
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔧 PreloadLinkedIngredientsAsync: Starting for product ID {productId}");
                
                // Ensure inventory is loaded
                if (ConnectPOSToInventoryVM.AllInventoryItems == null || !ConnectPOSToInventoryVM.AllInventoryItems.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"🔧 PreloadLinkedIngredientsAsync: Loading inventory...");
                    await ConnectPOSToInventoryVM.LoadInventoryAsync();
                }

                var links = await _database.GetProductIngredientsAsync(productId);
                System.Diagnostics.Debug.WriteLine($"🔧 PreloadLinkedIngredientsAsync: Found {links?.Count() ?? 0} linked ingredients");
                
                foreach (var (item, amount, unit, role) in links)
                {
                    System.Diagnostics.Debug.WriteLine($"🔧 PreloadLinkedIngredientsAsync: Processing {item.itemName} (ID: {item.itemID})");
                    
                    // Find matching inventory item
                    var inv = ConnectPOSToInventoryVM.AllInventoryItems.FirstOrDefault(i => i.itemID == item.itemID);
                    if (inv == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"🔧 PreloadLinkedIngredientsAsync: Adding new item {item.itemName} to collections");
                        // If not found yet, add it to collections
                        inv = item;
                        ConnectPOSToInventoryVM.AllInventoryItems.Add(inv);
                        ConnectPOSToInventoryVM.InventoryItems.Add(inv);
                    }

                    // Mark as selected and set input fields from link
                    System.Diagnostics.Debug.WriteLine($"🔧 PreloadLinkedIngredientsAsync: Setting {inv.itemName} as selected");
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
                    inv.SelectedSize = size; // Trigger the size change handler to update InputAmount and InputUnit
                    
                    // Set loading flag to prevent unit propagation during data loading
                    inv._isLoadingFromDatabase = true;
                    
                    inv.InputAmount = size switch
                    {
                        "Small" => inv.InputAmountSmall > 0 ? inv.InputAmountSmall : sharedAmt,
                        "Medium" => inv.InputAmountMedium > 0 ? inv.InputAmountMedium : sharedAmt,
                        "Large" => inv.InputAmountLarge > 0 ? inv.InputAmountLarge : sharedAmt,
                        _ => sharedAmt
                    };
                    inv.InputUnit = size switch
                    {
                        "Small" => !string.IsNullOrWhiteSpace(inv.InputUnitSmall) ? inv.InputUnitSmall : sharedUnit,
                        "Medium" => !string.IsNullOrWhiteSpace(inv.InputUnitMedium) ? inv.InputUnitMedium : sharedUnit,
                        "Large" => !string.IsNullOrWhiteSpace(inv.InputUnitLarge) ? inv.InputUnitLarge : sharedUnit,
                        _ => sharedUnit
                    };
                    
                    // Reset loading flag after setting the unit
                    inv._isLoadingFromDatabase = false;
                    
                    // Initialize the InputUnit to ensure proper data binding
                    // Pass true for edit mode since we're loading saved data from database
                    inv.InitializeInputUnit(isEditMode: true);
                }

                // Update derived collections and flags using public method
                ConnectPOSToInventoryVM.RefreshSelectionAndFilter();
                
                System.Diagnostics.Debug.WriteLine($"🔧 PreloadLinkedIngredientsAsync: Completed. SelectedIngredientsOnly count: {ConnectPOSToInventoryVM.SelectedIngredientsOnly?.Count ?? 0}");
                
                // Force UI update for all selected items to reflect loaded values
                foreach (var selectedItem in ConnectPOSToInventoryVM.SelectedIngredientsOnly)
                {
                    OnPropertyChanged(nameof(selectedItem.InputAmountText));
                    OnPropertyChanged(nameof(selectedItem.InputUnit));
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Warning", $"Failed to preload ingredients: {ex.Message}", "OK");
            }
        }

        private async Task<bool> ValidateInventorySufficiencyAsync() // Validates if selected ingredients have sufficient inventory amount
        {
            var selected = ConnectPOSToInventoryVM?.InventoryItems?.Where(i => i.IsSelected).ToList();
            if (selected == null || selected.Count == 0) return true; // nothing to validate

            List<string> issues = new();
            
            System.Diagnostics.Debug.WriteLine($"\n🔍 ===== VALIDATING INVENTORY SUFFICIENCY =====");
            
            // Validate selected ingredients
            foreach (var it in selected)
            {
                System.Diagnostics.Debug.WriteLine($"\n📦 Checking: {it.itemName}");
                
                // Use the current InputAmount and InputUnit from the UI
                var inputAmount = it.InputAmount > 0 ? it.InputAmount : 1;
                var inputUnit = it.InputUnit ?? it.DefaultUnit;
                var toUnit = (it.unitOfMeasurement ?? string.Empty).Trim();
                
                System.Diagnostics.Debug.WriteLine($"   Input: {inputAmount} {inputUnit}");
                System.Diagnostics.Debug.WriteLine($"   Inventory Unit: {toUnit}");
                System.Diagnostics.Debug.WriteLine($"   Available: {it.itemQuantity} {toUnit}");
                
                // Convert requested amount to the inventory unit
                var amountRequested = ConvertUnits(inputAmount, inputUnit, toUnit);

                System.Diagnostics.Debug.WriteLine($"   Converted Request: {amountRequested} {toUnit}");

                if (amountRequested <= 0)
                {
                    System.Diagnostics.Debug.WriteLine($"   ❌ FAILED: Incompatible unit conversion");
                    issues.Add($"{it.itemName}: incompatible unit conversion ({inputUnit} → {toUnit}).");
                    continue;
                }

                if (amountRequested > it.itemQuantity)
                {
                    System.Diagnostics.Debug.WriteLine($"   ❌ FAILED: Insufficient quantity ({amountRequested} > {it.itemQuantity})");
                    issues.Add($"{it.itemName}: needs {amountRequested:F2} {toUnit}, only {it.itemQuantity:F2} {toUnit} available.");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"   ✅ PASSED: Sufficient quantity");
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

        private async Task ValidateAutomaticCupAndStraw(List<string> issues) // Validates if cups and straw have sufficient inventory amount
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

        private static double ConvertUnits(double amount, string fromUnit, string toUnit) // Converts amount from one unit to another
        {
            System.Diagnostics.Debug.WriteLine($"🔄 ConvertUnits: {amount} {fromUnit} → {toUnit}");
            
            if (string.IsNullOrWhiteSpace(toUnit)) return amount; // no target unit, pass-through
            
            // Use UnitConversionService for consistent unit conversion
            var result = UnitConversionService.Convert(amount, fromUnit, toUnit);
            System.Diagnostics.Debug.WriteLine($"✅ UnitConversionService result: {result}");
            return result;
        }

        private static string NormalizeUnit(string unit) // Normalizes various unit strings to standard short forms
        {
            if (string.IsNullOrWhiteSpace(unit)) return string.Empty;

            // Use UnitConversionService for consistent normalization
            var result = UnitConversionService.Normalize(unit);
            System.Diagnostics.Debug.WriteLine($"📏 NormalizeUnit: '{unit}' → '{result}'");
            return result;
        }

        private static bool IsMassUnit(string unit) // Checks if the unit is a mass unit
        {
            var normalized = NormalizeUnit(unit);
            return normalized == "kg" || normalized == "g";
        }

        private static bool IsVolumeUnit(string unit) // Checks if the unit is a volume unit
        { 
            var normalized = NormalizeUnit(unit);
            return normalized == "L" || normalized == "ml";
        }

        private static bool IsCountUnit(string unit) // Checks if the unit is a count/pieces unit
        {
            var normalized = NormalizeUnit(unit);
            return normalized == "pcs";
        }

        public void ResetForm() // Resets the form fields to default values
        {
            System.Diagnostics.Debug.WriteLine($"🔧 ResetForm called. IsEditMode: {IsEditMode}");
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
                ConnectPOSToInventoryVM.IsEditMode = false;
                ConnectPOSToInventoryVM.IsUpdateAmountsMode = false;
                ConnectPOSToInventoryVM.IsPreviewVisible = false;
                ConnectPOSToInventoryVM.IsAddonPopupVisible = false;
                ConnectPOSToInventoryVM.ProductName = string.Empty;
                ConnectPOSToInventoryVM.SelectedCategory = string.Empty;
                ConnectPOSToInventoryVM.SmallPrice = string.Empty;
                ConnectPOSToInventoryVM.MediumPrice = string.Empty;
                ConnectPOSToInventoryVM.LargePrice = string.Empty;
                ConnectPOSToInventoryVM.SelectedImageSource = null;
                ConnectPOSToInventoryVM.ProductDescription = string.Empty;
                
                // Only clear ingredient and addon selections if not in edit mode
                if (!IsEditMode)
                {
                    // Clear all ingredient and addon selections
                    ConnectPOSToInventoryVM.SelectedIngredientsOnly?.Clear();
                    ConnectPOSToInventoryVM.SelectedAddons?.Clear();
                    
                    // Uncheck all items in the inventory lists
                    if (ConnectPOSToInventoryVM.AllInventoryItems != null)
                    {
                        foreach (var item in ConnectPOSToInventoryVM.AllInventoryItems)
                        {
                            item.IsSelected = false;
                            item.InputAmount = 0;
                            item.InputUnit = string.Empty;
                            item.AddonQuantity = 0;
                        }
                    }
                    
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
                }
                          
                // Reset addons popup if it exists
                if (ConnectPOSToInventoryVM.AddonsPopup != null)
                {
                    ConnectPOSToInventoryVM.AddonsPopup.IsAddonsPopupVisible = false;
                }
            }
        }
        [RelayCommand]
        private void OpenConnectPOSToInventory() // Opens the ConnectPOSToInventory popup
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

            // Load inventory list only if not already loaded (to preserve selections in edit mode)
            System.Diagnostics.Debug.WriteLine($"🔧 OpenConnectPOSToInventory: Checking if inventory needs loading...");
            System.Diagnostics.Debug.WriteLine($"🔧 OpenConnectPOSToInventory: AllInventoryItems null? {ConnectPOSToInventoryVM.AllInventoryItems == null}");
            System.Diagnostics.Debug.WriteLine($"🔧 OpenConnectPOSToInventory: AllInventoryItems count: {ConnectPOSToInventoryVM.AllInventoryItems?.Count ?? 0}");
            System.Diagnostics.Debug.WriteLine($"🔧 OpenConnectPOSToInventory: IsEditMode: {IsEditMode}");
            
            if (ConnectPOSToInventoryVM.AllInventoryItems == null || !ConnectPOSToInventoryVM.AllInventoryItems.Any())
            {
                System.Diagnostics.Debug.WriteLine($"🔧 OpenConnectPOSToInventory: Loading inventory (not loaded yet)");
                _ = ConnectPOSToInventoryVM.LoadInventoryAsync();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"🔧 OpenConnectPOSToInventory: Inventory already loaded, just refreshing filters");
                System.Diagnostics.Debug.WriteLine($"🔧 OpenConnectPOSToInventory: Selected items before ApplyFilters: {ConnectPOSToInventoryVM.AllInventoryItems.Count(i => i.IsSelected)}");
                
                // Refresh filters to update the display without reloading
                ConnectPOSToInventoryVM.ApplyFilters();
                
                System.Diagnostics.Debug.WriteLine($"🔧 OpenConnectPOSToInventory: Selected items after ApplyFilters: {ConnectPOSToInventoryVM.AllInventoryItems.Count(i => i.IsSelected)}");
                System.Diagnostics.Debug.WriteLine($"🔧 OpenConnectPOSToInventory: Items in InventoryItems collection: {ConnectPOSToInventoryVM.InventoryItems.Count}");
                System.Diagnostics.Debug.WriteLine($"🔧 OpenConnectPOSToInventory: Selected items in InventoryItems: {ConnectPOSToInventoryVM.InventoryItems.Count(i => i.IsSelected)}");
            }
            ConnectPOSToInventoryVM.IsConnectPOSToInventoryVisible = true;
        }

        [RelayCommand]
        public async Task PickImageAsync() // Opens file picker to select an image
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
                    // Save the image to app data directory and get the filename
                    var fileName = await Services.ImagePersistenceService.SaveImageAsync(result.FullPath);
                    
                    if (!string.IsNullOrWhiteSpace(fileName))
                    {
                        ImagePath = fileName; // Store only the filename
                        SelectedImageSource = ImageSource.FromFile(Services.ImagePersistenceService.GetImagePath(fileName));
                    }
                    else
                    {
                        await App.Current.MainPage.DisplayAlert("Error", "Failed to save image. Please try again.", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
        }
        
        private static bool AreUnitsCompatible(string inputUnit, string inventoryUnit) // Check if two units are compatible for conversion
        {
            if (string.IsNullOrWhiteSpace(inputUnit) || string.IsNullOrWhiteSpace(inventoryUnit))
                return true; // Allow empty units
            
            // Use UnitConversionService for consistent unit compatibility checking
            var result = UnitConversionService.AreCompatibleUnits(inputUnit, inventoryUnit);
            System.Diagnostics.Debug.WriteLine($"🔍 Unit Compatibility Check: '{inputUnit}' vs '{inventoryUnit}' = {result}");
            return result;
        }
    }
}