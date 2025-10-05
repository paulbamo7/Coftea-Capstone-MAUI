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

namespace Coftea_Capstone.ViewModel
{
    public partial class POSPageViewModel : ObservableObject
    {
        public SettingsPopUpViewModel SettingsPopup { get; set; }
        public AddItemToPOSViewModel AddItemToPOSViewModel { get; set; }
        public RetryConnectionPopupViewModel RetryConnectionPopup { get; set; }
        public NotificationPopupViewModel NotificationPopup { get; set; }
        public CartPopupViewModel CartPopup { get; set; }
        public HistoryPopupViewModel HistoryPopup { get; set; }
        public PaymentPopupViewModel PaymentPopup { get; set; }
        public OrderCompletePopupViewModel OrderCompletePopup { get; set; }
        public SuccessCardPopupViewModel SuccessCardPopup { get; set; }


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
        private bool isLoading;

        [ObservableProperty]
        private string statusMessage;

        [ObservableProperty]
        private bool hasError;

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

            AddItemToPOSViewModel.ProductAdded += OnProductAdded;
            AddItemToPOSViewModel.ProductUpdated += OnProductUpdated;
            AddItemToPOSViewModel.ConnectPOSToInventoryVM.ReturnRequested += () =>
            {
                AddItemToPOSViewModel.IsAddItemToPOSVisible = true;
            };
        }

        private RetryConnectionPopupViewModel GetRetryConnectionPopup()
        {
            return ((App)Application.Current).RetryConnectionPopup;
        }

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
                
                // Check if Products collection is null or empty to prevent crashes
                if (Products == null || !Products.Any())
                {
                    System.Diagnostics.Debug.WriteLine("ApplyFilters: Products is null or empty, clearing FilteredProducts");
                    FilteredProducts?.Clear();
                    return;
                }

                IEnumerable<POSPageModel> filteredSequence = Products;
                System.Diagnostics.Debug.WriteLine($"ApplyFilters: Starting with {filteredSequence.Count()} products");

                // Main category filtering
                if (!string.IsNullOrWhiteSpace(SelectedMainCategory) &&
                    !string.Equals(SelectedMainCategory, "All", StringComparison.OrdinalIgnoreCase))
                {
                    System.Diagnostics.Debug.WriteLine($"ApplyFilters: Filtering by category '{SelectedMainCategory}'");
                    
                    if (string.Equals(SelectedMainCategory, "Fruit/Soda", StringComparison.OrdinalIgnoreCase))
                    {
                        System.Diagnostics.Debug.WriteLine("ApplyFilters: Applying Fruit/Soda specific filtering");
                        // Include items categorized under Fruit/Soda via either Subcategory or Category
                        filteredSequence = filteredSequence.Where(p =>
                            p != null && (
                                (
                                    !string.IsNullOrWhiteSpace(p.Subcategory) &&
                                    (p.Subcategory.Trim().Equals("Fruit", StringComparison.OrdinalIgnoreCase) ||
                                     p.Subcategory.Trim().Equals("Soda", StringComparison.OrdinalIgnoreCase))
                                )
                                ||
                                (
                                    !string.IsNullOrWhiteSpace(p.Category) &&
                                    (p.Category.Trim().Equals("Fruit", StringComparison.OrdinalIgnoreCase) ||
                                     p.Category.Trim().Equals("Soda", StringComparison.OrdinalIgnoreCase) ||
                                     p.Category.Trim().Equals("Fruit/Soda", StringComparison.OrdinalIgnoreCase))
                                )
                            ));

                        // Optional subcategory refinement
                        if (!string.IsNullOrWhiteSpace(SelectedSubcategory))
                        {
                            filteredSequence = filteredSequence.Where(p =>
                                p != null && (
                                    (!string.IsNullOrWhiteSpace(p.Subcategory) && p.Subcategory.Trim().Equals(SelectedSubcategory, StringComparison.OrdinalIgnoreCase))
                                    || (!string.IsNullOrWhiteSpace(p.Category) && p.Category.Trim().Equals(SelectedSubcategory, StringComparison.OrdinalIgnoreCase))
                                )
                            );
                        }
                    }
                    else
                    {
                        filteredSequence = filteredSequence.Where(p =>
                            p != null && (
                                (!string.IsNullOrWhiteSpace(p.Category) && p.Category.Trim().Equals(SelectedMainCategory, StringComparison.OrdinalIgnoreCase))
                                || (!string.IsNullOrWhiteSpace(p.Subcategory) && p.Subcategory.Trim().Equals(SelectedMainCategory, StringComparison.OrdinalIgnoreCase))
                            )
                        );
                    }
                }

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
            try
            {
                IsLoading = true;
                StatusMessage = "Loading products...";
                HasError = false;

                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    HasError = true;
                    StatusMessage = "No internet connection. Please check your network.";
                    var retryPopup = GetRetryConnectionPopup();
                    if (retryPopup != null)
                    {
                        retryPopup.ShowRetryPopup(LoadDataAsync, "No internet connection detected. Please check your network settings and try again.");
                    }
                    return;
                }

                var productList = await _database.GetProductsAsync();
                Products = new ObservableCollection<POSPageModel>(productList ?? new List<POSPageModel>());
                FilteredProducts = new ObservableCollection<POSPageModel>(productList ?? new List<POSPageModel>());

                // Check stock levels for all products
                await CheckStockLevelsForAllProducts();

                System.Diagnostics.Debug.WriteLine($"LoadDataAsync: Loaded {Products.Count} products");
                foreach (var product in Products.Take(5)) // Log first 5 products for debugging
                {
                    System.Diagnostics.Debug.WriteLine($"Product: {product.ProductName}, Category: '{product.Category}', Subcategory: '{product.Subcategory}'");
                }

                if (Products.Any())
                {
                    StatusMessage = "Products loaded successfully!";
                }
                else
                {
                    StatusMessage = "No products found. Please add some products to the database.";
                }
            }
            catch (Exception ex)
            {
                HasError = true;
                StatusMessage = $"Failed to load products: {ex.Message}";
                
                var retryPopup = GetRetryConnectionPopup();
                if (retryPopup != null)
                {
                    retryPopup.ShowRetryPopup(LoadDataAsync, $"Failed to load products: {ex.Message}");
                }
            }
            finally
            {
                IsLoading = false;
            }
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

            var existing = CartItems.FirstOrDefault(p => p.ProductID == product.ProductID);
            if (existing != null)
            {
                existing.SmallQuantity += product.SmallQuantity;
                existing.MediumQuantity += product.MediumQuantity;
                existing.LargeQuantity += product.LargeQuantity;
            }
            else
            {
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
            }

            // Reset selection quantities
            product.SmallQuantity = 0;
            product.MediumQuantity = 0;
            product.LargeQuantity = 0;

            // Show success notification
            if (NotificationPopup != null)
            {
                NotificationPopup.ShowNotification("Item(s) added to cart successfully!", "Added to Cart");
            }
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
        }

        private async Task CheckStockLevelsForAllProducts()
        {
            try
            {
                foreach (var product in Products)
                {
                    var ingredients = await _database.GetProductAddonsAsync(product.ProductID);
                    
                    // Check if any ingredient has low stock (below minimum quantity)
                    bool hasLowStock = ingredients.Any(ingredient => 
                        ingredient.minimumQuantity > 0 && 
                        ingredient.itemQuantity < ingredient.minimumQuantity);
                    
                    product.IsLowStock = hasLowStock;
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
            CartPopup.ShowCart(CartItems);
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
            ShowCart();
        }
    }
}
