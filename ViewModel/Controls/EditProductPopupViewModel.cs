using Coftea_Capstone.C_;
using Coftea_Capstone.Models;
using Coftea_Capstone.ViewModel.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System;

namespace Coftea_Capstone.ViewModel
{
    public partial class EditProductPopupViewModel : ObservableObject
    {
        private readonly Database _database;
        private readonly AddItemToPOSViewModel _addItemToPOSViewModel;
        public NotificationPopupViewModel NotificationPopup { get; set; }

        // Event for notifying when a product is deleted
        public event Action<POSPageModel> ProductDeleted;

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
            _database = new Database(host: "0.0.0.0", database: "coftea_db", user: "root", password: "");
            _addItemToPOSViewModel = addItemToPOSViewModel;
            
            // Safely get NotificationPopup with null check
            try
            {
                NotificationPopup = ((App)Application.Current)?.NotificationPopup;
            }
            catch
            {
                NotificationPopup = null;
            }
        }

        [RelayCommand]
        private async Task LoadProductsAsync()
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
        private void EditProduct(POSPageModel product)
        {
            if (product == null) return;

            // Close the edit popup
            IsEditProductPopupVisible = false;

            // Set the product for editing in AddItemToPOSViewModel
            _addItemToPOSViewModel.SetEditMode(product);
            _addItemToPOSViewModel.IsAddItemToPOSVisible = true;
        }

        [RelayCommand]
        private void FilterByCategory(string category)
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
        private async Task DeleteProduct(POSPageModel product)
        {
            if (product == null) 
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Product is null. Cannot delete.", "OK");
                return;
            }

            if (Application.Current?.MainPage == null)
            {
                System.Diagnostics.Debug.WriteLine("Application.Current or MainPage is null");
                return;
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
                    if (NotificationPopup != null)
                    {
                        NotificationPopup.ShowNotification("Product deleted successfully!", "Success");
                    }
                    else
                    {
                        await Application.Current.MainPage.DisplayAlert("Success", "Product deleted successfully!", "OK");
                    }
                    AllProducts.Remove(product);
                    Products.Remove(product);
                    
                    // Notify that a product was deleted
                    ProductDeleted?.Invoke(product);
                    return;
                }

                await Application.Current.MainPage.DisplayAlert("Error", "Failed to delete product.", "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeleteProduct error: {ex}");
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to delete product: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private void CloseEditProductPopup()
        {
            IsEditProductPopupVisible = false;
        }

        public async Task ShowEditProductPopup()
        {
            IsEditProductPopupVisible = true;
            await LoadProductsAsync();
        }
    }
}
