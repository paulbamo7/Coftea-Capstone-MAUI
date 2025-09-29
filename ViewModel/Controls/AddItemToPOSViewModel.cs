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

        [ObservableProperty]
        private string selectedSubcategory;

        public bool IsFruitSodaSubcategoryVisible => string.Equals(SelectedCategory, "Fruit/Soda", StringComparison.OrdinalIgnoreCase);

        public string EffectiveCategory =>
            string.Equals(SelectedCategory, "Fruit/Soda", StringComparison.OrdinalIgnoreCase)
                ? (string.IsNullOrWhiteSpace(SelectedSubcategory) ? null : SelectedSubcategory)
                : SelectedCategory;

        partial void OnSelectedCategoryChanged(string value)
        {
            // Notify visibility change for subcategory UI
            OnPropertyChanged(nameof(IsFruitSodaSubcategoryVisible));

            // Reset subcategory when leaving Fruit/Soda
            if (!string.Equals(value, "Fruit/Soda", StringComparison.OrdinalIgnoreCase))
                SelectedSubcategory = null;
        }

        [ObservableProperty]
        private bool isAddItemToPOSVisible;

        [ObservableProperty]
        private string productName;

        [ObservableProperty]
        private decimal smallPrice;

        [ObservableProperty]
        private decimal mediumPrice;

        [ObservableProperty]
        private decimal largePrice;

        [ObservableProperty]
        private string imagePath;

        [ObservableProperty]
        private string productDescription;

        [ObservableProperty]
        private bool isEditMode = false;

        [ObservableProperty]
        private int editingProductId;

        public event Action<POSPageModel> ProductAdded;
        public event Action<POSPageModel> ProductUpdated;

        public AddItemToPOSViewModel()
        {
            _database = new Database(host: "0.0.0.0", database: "coftea_db", user: "root", password: "");

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
            SmallPrice = 0;
            MediumPrice = 0;
            LargePrice = 0;
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
            SmallPrice = product.SmallPrice;
            MediumPrice = product.MediumPrice;
            LargePrice = product.LargePrice;
            ImagePath = product.ImageSet;
            SelectedImageSource = !string.IsNullOrEmpty(product.ImageSet) ? ImageSource.FromFile(product.ImageSet) : null;
            ProductDescription = product.ProductDescription ?? string.Empty;

            // Set category and subcategory based on the product's data
            if (!string.IsNullOrEmpty(product.Category))
            {
                if (product.Category == "Fruit" || product.Category == "Soda")
                {
                    SelectedCategory = "Fruit/Soda";
                    SelectedSubcategory = product.Category;
                }
                else
                {
                    SelectedCategory = product.Category;
                    SelectedSubcategory = null;
                }
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

            if (SmallPrice <= 0)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Small price must be greater than 0.", "OK");
                return;
            }

            if (MediumPrice <= 0)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Medium price must be greater than 0.", "OK");
                return;
            }

            if (LargePrice <= 0)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Large price must be greater than 0.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(ImagePath))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Please select an image.", "OK");
                return;
            }

            // Validate inventory sufficiency for selected ingredients
            var inventoryOk = await ValidateInventorySufficiencyAsync();
            if (!inventoryOk)
            {
                return;
            }

            var product = new POSPageModel
            {
                ProductName = ProductName,
                SmallPrice = SmallPrice,
                MediumPrice = MediumPrice,
                LargePrice = LargePrice,
                Category = SelectedCategory,
                Subcategory = EffectiveCategory,
                ImageSet = ImagePath,
                ProductDescription = ProductDescription
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
                        await Application.Current.MainPage.DisplayAlert("Success", "Product updated successfully!", "OK");
                        await DeductSelectedIngredientsAsync();
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

                    // Insert product and get new ID
                    int newProductId = await _database.SaveProductReturningIdAsync(product);
                    if (newProductId <= 0)
                    {
                        await Application.Current.MainPage.DisplayAlert("Error", "Failed to save product.", "OK");
                        return;
                    }

                    product.ProductID = newProductId;

                    // Build product→inventory links (addons/ingredients)
                    var selected = ConnectPOSToInventoryVM?.InventoryItems?.Where(i => i.IsSelected).ToList() ?? new();
                    if (selected.Count > 0)
                    {
                        var links = selected.Select(i => (
                            inventoryItemId: i.itemID,
                            amount: ConvertUnits(i.InputAmount > 0 ? i.InputAmount : 1, i.InputUnit, i.unitOfMeasurement),
                            unit: (string?)i.unitOfMeasurement,
                            role: string.Equals(i.itemCategory, "Addons", StringComparison.OrdinalIgnoreCase) ? "addon" : "ingredient"
                        ));

                        try
                        {
                            await _database.SaveProductIngredientLinksAsync(newProductId, links);
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
                    await DeductSelectedIngredientsAsync();
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
            foreach (var it in selected)
            {
                // Convert requested amount to the inventory unit
                var fromUnit = (it.InputUnit ?? string.Empty).Trim();
                var toUnit = (it.unitOfMeasurement ?? string.Empty).Trim();
                var amountRequested = ConvertUnits(it.InputAmount > 0 ? it.InputAmount : 1, fromUnit, toUnit);

                if (amountRequested <= 0)
                {
                    issues.Add($"{it.itemName}: incompatible unit ({fromUnit} → {toUnit}).");
                    continue;
                }

                if (amountRequested > it.itemQuantity)
                {
                    issues.Add($"{it.itemName}: needs {amountRequested} {toUnit}, only {it.itemQuantity} left.");
                }
            }

            if (issues.Count > 0)
            {
                string msg = "Insufficient inventory:\n" + string.Join("\n", issues);
                await Application.Current.MainPage.DisplayAlert("Out of stock", msg, "OK");
                return false;
            }

            return true;
        }

        private async Task DeductSelectedIngredientsAsync()
        {
            if (ConnectPOSToInventoryVM?.Ingredients == null || ConnectPOSToInventoryVM.Ingredients.Count == 0)
                return;

            // Prefer per-item input from InventoryItems if available, fallback to Ingredients list
            var selected = ConnectPOSToInventoryVM.InventoryItems.Where(i => i.IsSelected);
            var pairs = selected.Select(i => (
                name: i.itemName,
                amount: ConvertUnits(i.InputAmount > 0 ? i.InputAmount : 1, i.InputUnit, i.unitOfMeasurement)
            ));

            try
            {
                await _database.DeductInventoryAsync(pairs);
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Warning", $"Product saved but failed to deduct inventory: {ex.Message}", "OK");
            }
        }

        private static double ConvertUnits(double amount, string fromUnit, string toUnit)
        {
            if (string.IsNullOrWhiteSpace(toUnit)) return amount; // no target unit, pass-through
            fromUnit = (fromUnit ?? string.Empty).Trim().ToLowerInvariant();
            toUnit = (toUnit ?? string.Empty).Trim().ToLowerInvariant();

            // Mass conversions
            if ((fromUnit == "kg" || fromUnit == "g") && (toUnit == "kg" || toUnit == "g"))
            {
                return fromUnit == toUnit
                    ? amount
                    : (fromUnit == "kg" && toUnit == "g") ? amount * 1000
                    : (fromUnit == "g" && toUnit == "kg") ? amount / 1000
                    : amount;
            }

            // Volume conversions
            if ((fromUnit == "l" || fromUnit == "ml") && (toUnit == "l" || toUnit == "ml"))
            {
                return fromUnit == toUnit
                    ? amount
                    : (fromUnit == "l" && toUnit == "ml") ? amount * 1000
                    : (fromUnit == "ml" && toUnit == "l") ? amount / 1000
                    : amount;
            }

            // Count
            if ((fromUnit == "pcs" || string.IsNullOrEmpty(fromUnit)) && toUnit == "pcs")
            {
                return amount;
            }

            // If from is empty, assume already in inventory unit
            if (string.IsNullOrWhiteSpace(fromUnit)) return amount;

            // Unknown or mismatched units → block deduction by returning 0
            return 0;
        }

        private void ResetForm()
        {
            ProductName = string.Empty;
            SmallPrice = 0;
            MediumPrice = 0;
            LargePrice = 0;
            SelectedCategory = string.Empty;
            SelectedSubcategory = null;
            ImagePath = string.Empty;
            SelectedImageSource = null;
            ProductDescription = string.Empty;
            IsAddItemToPOSVisible = false;
            IsEditMode = false;
            EditingProductId = 0;
        }
        [RelayCommand]
        private void OpenConnectPOSToInventory()
        {
            // Keep parent view visible and show the popup from the child VM
            IsAddItemToPOSVisible = true;
            // Populate preview data on child VM
            ConnectPOSToInventoryVM.ProductName = ProductName;
            ConnectPOSToInventoryVM.SelectedCategory = EffectiveCategory;
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