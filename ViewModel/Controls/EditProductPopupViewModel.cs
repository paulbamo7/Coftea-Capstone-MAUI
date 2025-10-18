using Coftea_Capstone.C_;
using Coftea_Capstone.Models;
using Coftea_Capstone.ViewModel.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Coftea_Capstone.ViewModel
{
    public partial class EditProductPopupViewModel : ObservableObject
    {
        private readonly Database _database;
        private readonly AddItemToPOSViewModel _addItemToPOSViewModel;
        public NotificationPopupViewModel NotificationPopup { get; set; }

        [ObservableProperty]
        private bool isEditProductPopupVisible = false;

        [ObservableProperty]
        private ObservableCollection<POSPageModel> products = new();

        [ObservableProperty]
        private bool isLoading = false;

        [ObservableProperty]
        private string statusMessage = string.Empty;

        [ObservableProperty]
        private bool hasError = false;

        [ObservableProperty]
        private string selectedCategory = "All";

        [ObservableProperty]
        private ObservableCollection<POSPageModel> allProducts = new();

        public EditProductPopupViewModel(AddItemToPOSViewModel addItemToPOSViewModel)
        {
            _database = new Database(); // Will use auto-detected host
            _addItemToPOSViewModel = addItemToPOSViewModel;
            NotificationPopup = ((App)Application.Current).NotificationPopup;
        }

        [RelayCommand]
        private async Task LoadProductsAsync() // Load products from database
        {
            IsLoading = true;
            StatusMessage = "Loading products...";
            HasError = false;

            try
            {
                var productsList = await _database.GetProductsAsync();
                AllProducts.Clear();
                Products.Clear();
                foreach (var product in productsList)
                {
                    AllProducts.Add(product);
                    Products.Add(product);
                }
                StatusMessage = $"Loaded {productsList.Count} products";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading products: {ex.Message}";
                HasError = true;
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void EditProduct(POSPageModel product) // Open edit popup for selected product
        {
            if (product == null) return;

            // Close the edit popup
            IsEditProductPopupVisible = false;

            // Set the product for editing in AddItemToPOSViewModel
            _addItemToPOSViewModel.SetEditMode(product);
            _addItemToPOSViewModel.IsAddItemToPOSVisible = true;
        }

        [RelayCommand]
        private void FilterByCategory(string category) // Filter products by category
        {
            SelectedCategory = category;
            
            Products.Clear();
            
            if (category == "All")
            {
                foreach (var product in AllProducts)
                {
                    Products.Add(product);
                }
            }
            else
            {
                foreach (var product in AllProducts)
                {
                    if (product.Category == category || product.Subcategory == category)
                    {
                        Products.Add(product);
                    }
                }
            }
        }

        [RelayCommand]
        private async Task DeleteProduct(POSPageModel product) // Delete selected product
        {
            if (product == null) return;

            // Check for dependencies first
            try
            {
                bool hasDependencies = await _database.HasProductTransactionDependenciesAsync(product.ProductID);
                if (hasDependencies)
                {
                    await Application.Current.MainPage.DisplayAlert("Cannot Delete", 
                        "This product cannot be deleted because it has associated transaction records. Please contact an administrator.", "OK");
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking dependencies: {ex.Message}");
                // Continue with deletion attempt
            }

            bool confirm = await Application.Current.MainPage.DisplayAlert(
                "Confirm Delete", 
                $"Are you sure you want to delete '{product.ProductName}'?", 
                "Yes", "No");

            if (!confirm) return;

            try
            {
                int rowsAffected = await _database.DeleteProductAsync(product.ProductID);

                // Double-check deletion even if rowsAffected is 0 (edge cases with triggers/permissions)
                var check = await _database.GetProductByIdAsync(product.ProductID);
                bool stillExists = check != null;

                if (rowsAffected > 0 || !stillExists)
                {
                    // Do not auto-open notifications here
                    AllProducts.Remove(product);
                    Products.Remove(product);
                    return;
                }

                await Application.Current.MainPage.DisplayAlert("Error", "Failed to delete product.", "OK");
            }
            catch (InvalidOperationException ex)
            {
                // Handle foreign key constraint violations with user-friendly message
                await Application.Current.MainPage.DisplayAlert("Cannot Delete", 
                    "This product cannot be deleted because it has associated transaction records. Please contact an administrator.", "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to delete product: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private void CloseEditProductPopup() // Close the edit product popup
        {
            IsEditProductPopupVisible = false;
        }

        public async Task ShowEditProductPopup() // Show the edit product popup
        {
            IsEditProductPopupVisible = true;
            await LoadProductsAsync();
        }
    }
}
