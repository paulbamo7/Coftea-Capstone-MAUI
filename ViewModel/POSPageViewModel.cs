using Coftea_Capstone.C_;
using Coftea_Capstone.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Coftea_Capstone.ViewModel
{
    public partial class POSPageViewModel : ObservableObject
    {
        private readonly Database _database;
        [ObservableProperty]
        private ObservableCollection<POSPageModel> products;

        [ObservableProperty]
        private ObservableCollection<POSPageModel> cartItems;

        public POSPageViewModel(Database database)
        {
            _database = new Database(App.dbPath);
        }

        public async Task LoadDataAsync()
        {
            var productList = await _database.GetProductsAsync();

            Products = new ObservableCollection<POSPageModel>(productList);
      
        }

        [RelayCommand]
        private void AddToCart(POSPageModel product)
        {
            if (product != null)
                CartItems.Add(product); // use the generated property
        }
        [RelayCommand]
        private void RemoveFromCart(POSPageModel product)
        {
            if (product != null && CartItems.Contains(product))
                CartItems.Remove(product); // use the generated property
        }
        [RelayCommand]
        private void EditProduct(POSPageModel product)
        {

        }
        [RelayCommand]
        private void RemoveProduct(POSPageModel product)
        {

        }
        [RelayCommand]
        private void AddProduct()
        {
        }


    }
}
