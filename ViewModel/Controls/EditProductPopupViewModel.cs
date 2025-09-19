using Coftea_Capstone.C_;
using Coftea_Capstone.Models;
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
            if (product == null) return;

            bool confirm = await Application.Current.MainPage.DisplayAlert(
                "Confirm Delete", 
                $"Are you sure you want to delete '{product.ProductName}'?", 
                "Yes", "No");

            if (confirm)
            {
                try
                {
                    int rowsAffected = await _database.DeleteProductAsync(product.ProductID);
                    if (rowsAffected > 0)
                    {
                        await Application.Current.MainPage.DisplayAlert("Success", "Product deleted successfully!", "OK");
                        
                        // Remove from collections
                        AllProducts.Remove(product);
                        Products.Remove(product);
                        
                        StatusMessage = $"Product deleted. {Products.Count} products remaining.";
                    }
                    else
                    {
                        await Application.Current.MainPage.DisplayAlert("Error", "Failed to delete product.", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", $"Failed to delete product: {ex.Message}", "OK");
                }
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
