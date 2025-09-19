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

            var product = new POSPageModel
            {
                ProductName = ProductName,
                SmallPrice = SmallPrice,
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

                    int rowsAffected = await _database.SaveProductAsync(product);
                    if (rowsAffected > 0)
                    {
                        await Application.Current.MainPage.DisplayAlert("Success", "Product added successfully!", "OK");
                        ResetForm();
                        ProductAdded?.Invoke(product);
                    }
                }
            }
            catch (Exception ex)
            {
                string action = IsEditMode ? "update" : "add";
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to {action} product: {ex.Message}", "OK");
            }
        }

        private void ResetForm()
        {
            ProductName = string.Empty;
            SmallPrice = 0;
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
            ConnectPOSToInventoryVM.LargePrice = LargePrice;
            ConnectPOSToInventoryVM.SelectedImageSource = SelectedImageSource;
            // Propagate description to child VM
            ConnectPOSToInventoryVM.ProductDescription = ProductDescription;

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