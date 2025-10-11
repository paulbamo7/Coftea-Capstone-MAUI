using Coftea_Capstone.C_;
using Coftea_Capstone.Models;
using Microsoft.Maui.Networking;
using Coftea_Capstone.Views.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using System;
using Coftea_Capstone.ViewModel.Controls;
using Coftea_Capstone.Models.Service;
using Coftea_Capstone.Services;

namespace Coftea_Capstone.ViewModel
{
    public partial class POSPageViewModel : BaseViewModel
    {
        private readonly CartStorageService _cartStorage = new CartStorageService();
        public SettingsPopUpViewModel SettingsPopup { get; set; }
        public AddItemToPOSViewModel AddItemToPOSViewModel { get; set; }
        public RetryConnectionPopupViewModel RetryConnectionPopup { get; set; }
        public NotificationPopupViewModel NotificationPopup { get; set; }
        public CartPopupViewModel CartPopup { get; set; }
        public HistoryPopupViewModel HistoryPopup { get; set; }
        public PaymentPopupViewModel PaymentPopup { get; set; }
        public OrderCompletePopupViewModel OrderCompletePopup { get; set; }
        public SuccessCardPopupViewModel SuccessCardPopup { get; set; }
        public AddonsSelectionPopupViewModel AddonsPopup { get; set; }


        private readonly Database _database;

        [ObservableProperty]
        private ObservableCollection<POSPageModel> products = new();

        [ObservableProperty]
        private ObservableCollection<POSPageModel> cartItems = new();

        [ObservableProperty]
        private ObservableCollection<POSPageModel> filteredProducts = new();

        [ObservableProperty]
        private string selectedMainCategory;

        [ObservableProperty]
        private string selectedSubcategory;

        public bool IsFruitSodaSubcategoryVisible => !string.IsNullOrWhiteSpace(SelectedMainCategory) && string.Equals(SelectedMainCategory, "Fruit/Soda", StringComparison.OrdinalIgnoreCase);
        
        public bool IsCoffeeSubcategoryVisible => !string.IsNullOrWhiteSpace(SelectedMainCategory) && string.Equals(SelectedMainCategory, "Coffee", StringComparison.OrdinalIgnoreCase);

        partial void OnSelectedMainCategoryChanged(string value)
        {
            System.Diagnostics.Debug.WriteLine($"OnSelectedMainCategoryChanged: value = '{value}'");
            OnPropertyChanged(nameof(IsFruitSodaSubcategoryVisible));
            OnPropertyChanged(nameof(IsCoffeeSubcategoryVisible));
        }

        public bool IsCartVisible => CartItems?.Any() ?? false;

        partial void OnCartItemsChanged(ObservableCollection<POSPageModel> value)
            => OnPropertyChanged(nameof(IsCartVisible));

        [ObservableProperty]
        private bool isAdmin;

        [ObservableProperty]
        private bool isCategoryLoading;

        [ObservableProperty]
        private POSPageModel selectedProduct;

        [ObservableProperty]
        private ObservableCollection<string> availableSizes = new();

        // Size visibility properties for cart based on category
        public bool IsSmallSizeVisibleInCart => string.Equals(SelectedProduct?.Category, "Coffee", StringComparison.OrdinalIgnoreCase);
        public bool IsMediumSizeVisibleInCart => true; // Always visible for all categories
        public bool IsLargeSizeVisibleInCart => true; // Always visible for all categories

        public POSPageViewModel(AddItemToPOSViewModel addItemToPOSViewModel, SettingsPopUpViewModel settingsPopupViewModel)
        {
            _database = new Database(); // Will use auto-detected host
            SettingsPopup = settingsPopupViewModel;
            AddItemToPOSViewModel = addItemToPOSViewModel;
            NotificationPopup = ((App)Application.Current).NotificationPopup;
            RetryConnectionPopup = ((App)Application.Current).RetryConnectionPopup;
            CartPopup = new CartPopupViewModel();
            HistoryPopup = new HistoryPopupViewModel();
            PaymentPopup = ((App)Application.Current).PaymentPopup;
            OrderCompletePopup = ((App)Application.Current).OrderCompletePopup;
            SuccessCardPopup = ((App)Application.Current).SuccessCardPopup;
            AddonsPopup = new AddonsSelectionPopupViewModel();

            // When addons are selected from the popup, attach them to the currently selected product
            AddonsPopup.AddonsSelected += (selectedAddons) =>
            {
                try
                {
                    if (SelectedProduct == null || selectedAddons == null)
                        return;

                    // Replace existing addons with selected ones
                    SelectedProduct.InventoryItems.Clear();
                    foreach (var addon in selectedAddons)
                    {
                        SelectedProduct.InventoryItems.Add(addon);
                    }
                    // Notify price/summary recalculation on UI if bound
                    OnPropertyChanged(nameof(SelectedProduct));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error applying selected addons: {ex.Message}");
                }
            };

            AddItemToPOSViewModel.ProductAdded += OnProductAdded;
            AddItemToPOSViewModel.ProductUpdated += OnProductUpdated;
            AddItemToPOSViewModel.ConnectPOSToInventoryVM.ReturnRequested += () =>
            {
                AddItemToPOSViewModel.IsAddItemToPOSVisible = true;
            };
        }

        [RelayCommand]
        private void ShowNotificationBell()
        {
            // Toggle the global notification popup. Data should be added by callers using NotificationPopup.Notifications.
            NotificationPopup?.ToggleCommand.Execute(null);
        }

        private RetryConnectionPopupViewModel GetRetryConnectionPopup() => ((App)Application.Current).RetryConnectionPopup;

        private async void OnProductAdded(POSPageModel newProduct)
        {
            if (newProduct != null)
            {
                Products?.Add(newProduct); 
                FilteredProducts?.Add(newProduct); 
                await LoadDataAsync();  
            }
        }

        private async void OnProductUpdated(POSPageModel updatedProduct)
        {
            if (updatedProduct == null || Products == null || FilteredProducts == null)
                return;

            // Find and update the existing product in the collections
            var existingProduct = Products.FirstOrDefault(p => p.ProductID == updatedProduct.ProductID);
            if (existingProduct != null)
            {
                var index = Products.IndexOf(existingProduct);
                Products[index] = updatedProduct;
            }

            var existingFilteredProduct = FilteredProducts.FirstOrDefault(p => p.ProductID == updatedProduct.ProductID);
            if (existingFilteredProduct != null)
            {
                var index = FilteredProducts.IndexOf(existingFilteredProduct);
                FilteredProducts[index] = updatedProduct;
            }

            await LoadDataAsync();
        }

        public async Task InitializeAsync()
        {
            if (App.CurrentUser != null)
                IsAdmin = App.CurrentUser.IsAdmin;

            await LoadDataAsync();

        // Load persisted cart
        var loadedCart = await _cartStorage.LoadCartAsync();
        if (loadedCart != null && loadedCart.Any())
        {
            CartItems = loadedCart;
        }
        }

        [RelayCommand]
        private async Task FilterByCategory(object category)
        {
            try
            {
                SelectedMainCategory = category?.ToString() ?? string.Empty;
                System.Diagnostics.Debug.WriteLine($"FilterByCategory: SelectedMainCategory = '{SelectedMainCategory}'");

                // Reset subcategory when switching main category (unless still Fruit/Soda)
                if (!string.Equals(SelectedMainCategory, "Fruit/Soda", StringComparison.OrdinalIgnoreCase))
                    SelectedSubcategory = null;

                ApplyFilters();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FilterByCategory error: {ex.Message}");
                throw;
            }
        }

        [RelayCommand]
        private async Task FilterBySubcategory(object subcategory)
        {
            SelectedSubcategory = subcategory?.ToString() ?? string.Empty;
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"ApplyFilters: Products count = {Products?.Count ?? -1}");

                if (Products == null || !Products.Any())
                {
                    FilteredProducts?.Clear();
                    return;
                }

                var filteredSequence = ProductFilterService.Apply(Products, SelectedMainCategory, SelectedSubcategory);

                FilteredProducts?.Clear();
                if (FilteredProducts != null)
                {
                    foreach (var product in filteredSequence)
                        FilteredProducts.Add(product);
                }

                System.Diagnostics.Debug.WriteLine($"ApplyFilters: Final filtered count = {FilteredProducts?.Count ?? -1}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApplyFilters error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"ApplyFilters stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task LoadDataAsync()
        {
            await RunWithLoading(async () =>
            {
                StatusMessage = "Loading products...";

                if (!EnsureInternetOrShowRetry(LoadDataAsync, "No internet connection detected. Please check your network settings and try again."))
                    return;

                try
                {
                var productList = await _database.GetProductsAsyncCached();
                    Products = new ObservableCollection<POSPageModel>(productList ?? new List<POSPageModel>());
                    FilteredProducts = new ObservableCollection<POSPageModel>(productList ?? new List<POSPageModel>());

                    // Check stock levels for all products
                    await CheckStockLevelsForAllProducts();

                    System.Diagnostics.Debug.WriteLine($"LoadDataAsync: Loaded {Products.Count} products");
                    foreach (var product in Products.Take(5))
                    {
                        System.Diagnostics.Debug.WriteLine($"Product: {product.ProductName}, Category: '{product.Category}', Subcategory: '{product.Subcategory}'");
                    }

                    StatusMessage = Products.Any()
                        ? "Products loaded successfully!"
                        : "No products found. Please add some products to the database.";
                }
                catch (Exception ex)
                {
                    HasError = true;
                    StatusMessage = $"Failed to load products: {ex.Message}";
                    GetRetryConnectionPopup()?.ShowRetryPopup(LoadDataAsync, $"Failed to load products: {ex.Message}");
                }
            });
        }


        [RelayCommand]
        private void AddToCart(POSPageModel product)
        {
            if (product == null || CartItems == null) 
            {
                System.Diagnostics.Debug.WriteLine("AddToCart: product or CartItems is null");
                return;
            }

            // Check if there are any items to add (at least one quantity > 0)
            if (product.SmallQuantity <= 0 && product.MediumQuantity <= 0 && product.LargeQuantity <= 0)
            {
                // Show notification that no items were selected
                if (NotificationPopup != null)
                {
                    NotificationPopup.ShowNotification("Please select at least one item to add to cart.", "No Items Selected");
                }
                return;
            }

            // Always create a new cart line entry even if the same product already exists
            var copy = new POSPageModel
            {
                ProductID = product.ProductID,
                ProductName = product.ProductName,
                SmallPrice = product.SmallPrice,
                MediumPrice = product.MediumPrice,
                LargePrice = product.LargePrice,
                ImageSet = product.ImageSet,
                SmallQuantity = product.SmallQuantity,
                MediumQuantity = product.MediumQuantity,
                LargeQuantity = product.LargeQuantity
            };
            CartItems.Add(copy);

            // Reset selection quantities
            product.SmallQuantity = 0;
            product.MediumQuantity = 0;
            product.LargeQuantity = 0;

            // Do not auto-open notifications; bell controls visibility

        // Persist cart update
        _ = _cartStorage.SaveCartAsync(CartItems);
        }

        [RelayCommand]
        private void SelectProduct(POSPageModel product)
        {
            System.Diagnostics.Debug.WriteLine($"SelectProduct called with product: {product?.ProductName ?? "null"}");
            
            // Don't allow selection if product has low stock
            if (product?.IsLowStock == true)
            {
                System.Diagnostics.Debug.WriteLine($"Product {product.ProductName} has low stock, selection blocked");
                return;
            }
            
            SelectedProduct = product;

            AvailableSizes.Clear();
            if (product.HasSmall) AvailableSizes.Add("Small");
            if (product.HasMedium) AvailableSizes.Add("Medium");
            if (product.HasLarge) AvailableSizes.Add("Large");

            // Notify visibility change for cart size options
            OnPropertyChanged(nameof(IsSmallSizeVisibleInCart));
            OnPropertyChanged(nameof(IsMediumSizeVisibleInCart));
            OnPropertyChanged(nameof(IsLargeSizeVisibleInCart));

            // Load linked addons from DB for this product
            _ = LoadAddonsForSelectedAsync();
        }

        private async Task LoadAddonsForSelectedAsync()
        {
            if (SelectedProduct == null) return;
            try
            {
                var addons = await _database.GetProductAddonsAsync(SelectedProduct.ProductID);
                SelectedProduct.InventoryItems.Clear();
                foreach (var a in addons)
                    SelectedProduct.InventoryItems.Add(a);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load addons: {ex.Message}");
            }
        }

        [RelayCommand]
        private void EditProduct(POSPageModel product)
        {
            if (product == null) return;

            AddItemToPOSViewModel.SetEditMode(product);
            SettingsPopup.OpenAddItemToPOSCommand.Execute(null);
        }

        [RelayCommand]
        private void RemoveFromCart(POSPageModel product)
        {
            if (product == null || CartItems == null) return;

            var itemInCart = CartItems.FirstOrDefault(p => p.ProductID == product.ProductID);
            if (itemInCart != null) CartItems.Remove(itemInCart);

            if (SelectedProduct?.ProductID == product.ProductID)
                SelectedProduct = null;

        // Persist cart update
        _ = _cartStorage.SaveCartAsync(CartItems);
        }

        private async Task CheckStockLevelsForAllProducts()
        {
            try
            {
                foreach (var product in Products)
                {
                    // Load core ingredients for the product (not addons) to validate sufficiency
                    var ingredients = await _database.GetProductIngredientsAsync(product.ProductID);

                    bool insufficientForRecipe = false;
                    foreach (var tuple in ingredients)
                    {
                        var item = tuple.item;
                        var requiredAmount = tuple.amount;
                        var requiredUnit = tuple.unit;

                        // Normalize units
                        var inventoryUnit = UnitConversionService.Normalize(item.unitOfMeasurement);
                        var recipeUnit = UnitConversionService.Normalize(requiredUnit);

                        double requiredInInventoryUnit;
                        // If either unit is missing, treat both as quantity-type and compare raw numbers
                        if (string.IsNullOrWhiteSpace(inventoryUnit) || string.IsNullOrWhiteSpace(recipeUnit))
                        {
                            requiredInInventoryUnit = requiredAmount;
                        }
                        else if (!UnitConversionService.AreCompatibleUnits(recipeUnit, inventoryUnit))
                        {
                            // Incompatible units — conservatively mark insufficient
                            insufficientForRecipe = true;
                            break;
                        }
                        else
                        {
                            requiredInInventoryUnit = UnitConversionService.Convert(requiredAmount, recipeUnit, inventoryUnit);
                        }

                        if (requiredInInventoryUnit > item.itemQuantity)
                        {
                            insufficientForRecipe = true;
                            break;
                        }
                    }

                    // Rule: If inventory for each required ingredient is enough for ONE serving, product is available.
                    // Ignore minimum thresholds for availability; they can be surfaced elsewhere as warnings.
                    product.IsLowStock = insufficientForRecipe;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking stock levels: {ex.Message}");
            }
        }

        [RelayCommand]
        private void IncreaseSmallQty() 
        { 
            if (SelectedProduct != null) 
                SelectedProduct.SmallQuantity++; 
        }

        [RelayCommand]
        private void DecreaseSmallQty() 
        { 
            if (SelectedProduct != null && SelectedProduct.SmallQuantity > 0) 
                SelectedProduct.SmallQuantity--; 
        }

        [RelayCommand]
        private void IncreaseMediumQty() 
        { 
            if (SelectedProduct != null) 
                SelectedProduct.MediumQuantity++; 
        }

        [RelayCommand]
        private void DecreaseMediumQty() 
        { 
            if (SelectedProduct != null && SelectedProduct.MediumQuantity > 0) 
                SelectedProduct.MediumQuantity--; 
        }

        [RelayCommand]
        private void IncreaseLargeQty() 
        { 
            if (SelectedProduct != null) 
                SelectedProduct.LargeQuantity++; 
        }

        [RelayCommand]
        private void DecreaseLargeQty() 
        { 
            if (SelectedProduct != null && SelectedProduct.LargeQuantity > 0) 
                SelectedProduct.LargeQuantity--; 
        }

        [RelayCommand]
        private void ShowCart()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"ShowCart called with {CartItems?.Count ?? 0} items");
                
                if (CartPopup == null)
                {
                    System.Diagnostics.Debug.WriteLine("CartPopup is null!");
                    if (NotificationPopup != null)
                    {
                        NotificationPopup.ShowNotification("Cart is not available", "Error");
                    }
                    return;
                }
                
                CartPopup.ShowCart(CartItems);
                System.Diagnostics.Debug.WriteLine("CartPopup.ShowCart completed successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ShowCart: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                if (NotificationPopup != null)
                {
                    NotificationPopup.ShowNotification($"Failed to open cart: {ex.Message}", "Error");
                }
            }
        }

    public Task SaveCartToStorageAsync() => _cartStorage.SaveCartAsync(CartItems);
    public async Task LoadCartFromStorageAsync()
    {
        var loaded = await _cartStorage.LoadCartAsync();
        if (loaded != null)
            CartItems = loaded;
    }

        [RelayCommand]
        private async Task ShowHistory()
        {
            System.Diagnostics.Debug.WriteLine("ShowHistory command called");

            // Use shared transactions populated from checkout
            var app = (App)Application.Current;
            var transactions = app?.Transactions ?? new ObservableCollection<TransactionHistoryModel>();
            await HistoryPopup.ShowHistory(transactions);
            System.Diagnostics.Debug.WriteLine($"HistoryPopup.IsHistoryVisible: {HistoryPopup.IsHistoryVisible}");
        }

        [RelayCommand]
        private void Cart()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Cart command executed");
                ShowCart();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Cart command: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Show error notification if available
                if (NotificationPopup != null)
                {
                    NotificationPopup.ShowNotification($"Cart error: {ex.Message}", "Error");
                }
            }
        }
    }
}
