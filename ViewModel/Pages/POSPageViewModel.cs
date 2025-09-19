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

        public bool IsFruitSodaSubcategoryVisible => string.Equals(SelectedMainCategory, "Fruit/Soda", StringComparison.OrdinalIgnoreCase);

        partial void OnSelectedMainCategoryChanged(string value)
            => OnPropertyChanged(nameof(IsFruitSodaSubcategoryVisible));

        public bool IsCartVisible => CartItems.Any();

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
        private POSPageModel selectedProduct;

        [ObservableProperty]
        private ObservableCollection<string> availableSizes = new();

        public POSPageViewModel(AddItemToPOSViewModel addItemToPOSViewModel, SettingsPopUpViewModel settingsPopupViewModel)
        {
            _database = new Database(host: "0.0.0.0", database: "coftea_db", user: "root", password: "");
            SettingsPopup = settingsPopupViewModel;
            AddItemToPOSViewModel = addItemToPOSViewModel;

            AddItemToPOSViewModel.ProductAdded += OnProductAdded;
            AddItemToPOSViewModel.ProductUpdated += OnProductUpdated;
            AddItemToPOSViewModel.ConnectPOSToInventoryVM.ReturnRequested += () =>
            {
                AddItemToPOSViewModel.IsAddItemToPOSVisible = true;
            };
        }

        private async void OnProductAdded(POSPageModel newProduct)
        {
            Products.Add(newProduct); 
            FilteredProducts.Add(newProduct); 
            await LoadDataAsync();  
        }

        private async void OnProductUpdated(POSPageModel updatedProduct)
        {
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
        private void FilterByCategory(object category)
        {
            SelectedMainCategory = category?.ToString() ?? string.Empty;

            // Reset subcategory when switching main category (unless still Fruit/Soda)
            if (!string.Equals(SelectedMainCategory, "Fruit/Soda", StringComparison.OrdinalIgnoreCase))
                SelectedSubcategory = null;

            ApplyFilters();
        }

        [RelayCommand]
        private void FilterBySubcategory(object subcategory)
        {
            SelectedSubcategory = subcategory?.ToString() ?? string.Empty;
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            IEnumerable<POSPageModel> filteredSequence = Products;

            // Main category filtering
            if (!string.IsNullOrWhiteSpace(SelectedMainCategory) &&
                !string.Equals(SelectedMainCategory, "All", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(SelectedMainCategory, "Fruit/Soda", StringComparison.OrdinalIgnoreCase))
                {
                    // Include items categorized under Fruit/Soda via either Subcategory or Category
                    filteredSequence = filteredSequence.Where(p =>
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
                        ));

                    // Optional subcategory refinement
                    if (!string.IsNullOrWhiteSpace(SelectedSubcategory))
                    {
                        filteredSequence = filteredSequence.Where(p =>
                            (!string.IsNullOrWhiteSpace(p.Subcategory) && p.Subcategory.Trim().Equals(SelectedSubcategory, StringComparison.OrdinalIgnoreCase))
                            || (!string.IsNullOrWhiteSpace(p.Category) && p.Category.Trim().Equals(SelectedSubcategory, StringComparison.OrdinalIgnoreCase))
                        );
                    }
                }
                else
                {
                    filteredSequence = filteredSequence.Where(p =>
                        (!string.IsNullOrWhiteSpace(p.Category) && p.Category.Trim().Equals(SelectedMainCategory, StringComparison.OrdinalIgnoreCase))
                        || (!string.IsNullOrWhiteSpace(p.Subcategory) && p.Subcategory.Trim().Equals(SelectedMainCategory, StringComparison.OrdinalIgnoreCase))
                    );
                }
            }

            FilteredProducts.Clear();
            foreach (var product in filteredSequence)
                FilteredProducts.Add(product);
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
                    return;
                }

                var productList = await _database.GetProductsAsync();
                Products = new ObservableCollection<POSPageModel>(productList);
                FilteredProducts = new ObservableCollection<POSPageModel>(productList);

                StatusMessage = Products.Any() ? "Products loaded successfully." : "No products found.";
            }
            catch (Exception ex)
            {
                HasError = true;
                StatusMessage = $"Failed to load products: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void AddToCart(POSPageModel product)
        {
            if (product == null) return;

            var existing = CartItems.FirstOrDefault(p => p.ProductID == product.ProductID);
            if (existing != null)
            {
                existing.SmallQuantity += product.SmallQuantity;
                existing.LargeQuantity += product.LargeQuantity;
            }
            else
            {
                var copy = new POSPageModel
                {
                    ProductID = product.ProductID,
                    ProductName = product.ProductName,
                    SmallPrice = product.SmallPrice,
                    LargePrice = product.LargePrice,
                    ImageSet = product.ImageSet,
                    SmallQuantity = product.SmallQuantity,
                    LargeQuantity = product.LargeQuantity
                };
                CartItems.Add(copy);
            }

            // Reset selection quantities
            product.SmallQuantity = 0;
            product.LargeQuantity = 0;
        }

        [RelayCommand]
        private void SelectProduct(POSPageModel product)
        {
            SelectedProduct = product;

            AvailableSizes.Clear();
            if (product.HasSmall) AvailableSizes.Add("Small");
            if (product.HasLarge) AvailableSizes.Add("Large");
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
            if (product == null) return;

            var itemInCart = CartItems.FirstOrDefault(p => p.ProductID == product.ProductID);
            if (itemInCart != null) CartItems.Remove(itemInCart);

            if (SelectedProduct?.ProductID == product.ProductID)
                SelectedProduct = null;
        }

        [RelayCommand]
        private void IncreaseSmallQty() => SelectedProduct.SmallQuantity++;

        [RelayCommand]
        private void DecreaseSmallQty() { if (SelectedProduct.SmallQuantity > 0) SelectedProduct.SmallQuantity--; }

        [RelayCommand]
        private void IncreaseLargeQty() => SelectedProduct.LargeQuantity++;

        [RelayCommand]
        private void DecreaseLargeQty() { if (SelectedProduct.LargeQuantity > 0) SelectedProduct.LargeQuantity--; }
    }
}
