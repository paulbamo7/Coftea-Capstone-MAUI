using Coftea_Capstone.C_;
using Coftea_Capstone.Models;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace Coftea_Capstone.ViewModel
{
    public partial class AddItemToPOSViewModel : ObservableObject
    {
        private readonly Database _database;

        // Form fields
        [ObservableProperty]
        private string productName;

        [ObservableProperty]
        private double smallPrice;

        [ObservableProperty]
        private double largePrice;

        [ObservableProperty]
        private string menuCategory;

        [ObservableProperty]
        private string imagePath; // Can store file path or URL

        [ObservableProperty]
        private string status; // e.g., "Available", "Out of stock"

        public AddItemToPOSViewModel()
        {
            _database = new Database(
                host: "localhost",
                database: "coftea_db",
                user: "root",
                password: ""
            );
        }

        [RelayCommand]
        private async Task AddProduct()
        {
            if (string.IsNullOrWhiteSpace(ProductName))
                return; // prevent saving empty product

            var newProduct = new POSPageModel
            {
                ProductName = ProductName,
                SmallPrice = SmallPrice,
                LargePrice = LargePrice,
                ImageSet = ImagePath
            };
            var existingProduct = await _database.GetProductsAsync();
            if (existingProduct != null)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Product already exists!", "OK");
                return;
            }
            try
            {
                await _database.SaveProductAsync(newProduct); 

                await Application.Current.MainPage.DisplayAlert("Success", "Product Added successfully!", "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to Add: {ex.Message}", "OK");
            }
        }
    }
}