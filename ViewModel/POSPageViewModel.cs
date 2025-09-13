using Coftea_Capstone.C_;
using Coftea_Capstone.Models;
using Coftea_Capstone.Views.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Coftea_Capstone.ViewModel
{
    public partial class POSPageViewModel : ObservableObject
    {
        // Popup controller
        public SettingsPopUpViewModel SettingsPopup { get; } = new SettingsPopUpViewModel();
        public AddItemToInventoryViewModel AddItemPopup { get; } = new AddItemToInventoryViewModel();

        private readonly Database _database;

        [ObservableProperty]
        private ObservableCollection<POSPageModel> products = new();

        [ObservableProperty]
        private ObservableCollection<POSPageModel> cartItems = new();

        [ObservableProperty]
        private bool isAdmin;

        

        [ObservableProperty]
        private string productName;


        [ObservableProperty]
        private double smallprice;

        [ObservableProperty]
        private double largeprice;

        [ObservableProperty]
        private string image;
       
        public POSPageViewModel()
        {
            _database = new Database(
                host: "localhost",
                database: "coftea_db",
                user: "root",
                password: ""
            );
            
        }

        private async void OnProductAdded(POSPageModel newProduct)
        {
            Products.Add(newProduct);   // update UI immediately
            await LoadDataAsync();      // sync with DB

        }     

        public async Task InitializeAsync(string email)
        {
            if (App.CurrentUser != null)
            {
                IsAdmin = App.CurrentUser.IsAdmin;
            }

            await LoadDataAsync();
        }

        public async Task LoadDataAsync()
        {
            var productList = await _database.GetProductsAsync();
            Products = new ObservableCollection<POSPageModel>(productList);
        }

        [RelayCommand]
        private void AddToCart(POSPageModel product)
        {
            if (product == null) return;

            var existing = CartItems.FirstOrDefault(p => p.ProductID == product.ProductID);
            if (existing != null)
            {
                existing.Quantity++;
                OnPropertyChanged(nameof(CartItems));
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
                    Quantity = 1
                };
                CartItems.Add(copy);
            }

        }

        [RelayCommand]
        private void RemoveFromCart(POSPageModel product)
        {
            if (product != null && CartItems.Contains(product))
                CartItems.Remove(product);
        }

        [RelayCommand]
        private void EditProduct(POSPageModel product)
        {
            if (product == null) return;

            // Example: open "AddItemToPOS" popup pre-filled
            SettingsPopup.OpenAddItemToPOSCommand.Execute(null);
        }

        [RelayCommand]
        private void RemoveProduct(POSPageModel product)
        {
            if (product == null) return;

            // Example: remove from Products
            Products.Remove(product);

            // TODO: Call _database.RemoveProductAsync(product.ProductID);
        }
    }
}
