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

        public event Action<POSPageModel> ProductAdded;

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
            ProductName = string.Empty;
            SmallPrice = 0;
            LargePrice = 0;
            SelectedCategory = string.Empty;
            ImagePath = string.Empty;
        }

        [RelayCommand]
        private async Task AddProduct()
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

            var newProduct = new POSPageModel
            {
                ProductName = ProductName,
                SmallPrice = SmallPrice,
                LargePrice = LargePrice,
                Category = SelectedCategory,
                ImageSet = ImagePath
            };

            try
            {
                var existingProducts = await _database.GetProductsAsync();
                if (existingProducts.Any(p => p.ProductName == ProductName))
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Product already exists!", "OK");
                    return;
                }

                int rowsAffected = await _database.SaveProductAsync(newProduct);
                if (rowsAffected > 0)
                {
                    await Application.Current.MainPage.DisplayAlert("Success", "Product added successfully!", "OK");

                    ProductName = string.Empty;
                    SmallPrice = 0;
                    LargePrice = 0;
                    SelectedCategory = string.Empty;
                    ImagePath = string.Empty;
                    IsAddItemToPOSVisible = false;

                    ProductAdded?.Invoke(newProduct);
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to add product: {ex.Message}", "OK");
            }
        }
        [RelayCommand]
        private void OpenConnectPOSToInventory()
        {
            IsAddItemToPOSVisible = false;          
            IsConnectPOSToInventoryVisible = true;
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