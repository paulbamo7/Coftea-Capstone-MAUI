using Coftea_Capstone.C_;
using Coftea_Capstone.Models;
using CommunityToolkit.Mvvm.ComponentModel;
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
        public ConnectPOSItemToInventoryViewModel ConnectPOSToinventory { get; set; }

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

        // Event to notify POSPageViewModel
        public event Action<POSPageModel> ProductAdded;

        public AddItemToPOSViewModel()
        {
            _database = new Database(host: "0.0.0.0", database: "coftea_db", user: "root", password: "");
            IsAddItemToPOSVisible = false;
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
                await Application.Current.MainPage.DisplayAlert("Error", "Product Name is required", "OK");
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

                    // Reset fields
                    ProductName = string.Empty;
                    SmallPrice = 0;
                    LargePrice = 0;
                    SelectedCategory = string.Empty;
                    ImagePath = string.Empty;

                    // Close popup
                    IsAddItemToPOSVisible = false;

                    // Notify POSPageViewModel
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
            
            IsAddItemToPOSVisible = true;
        }
    }
}
