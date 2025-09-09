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
        public SettingsPopUpViewModel SettingsPopup { get; } = new SettingsPopUpViewModel();

        private readonly Database _database;

        [ObservableProperty]
        private ObservableCollection<POSPageModel> products;

        [ObservableProperty]
        private ObservableCollection<POSPageModel> cartItems;

        
        [ObservableProperty]
        private bool isAdmin;

        [ObservableProperty]
        private bool isAddItemVisible = false;

        public POSPageViewModel()
        {
            _database = new Database(
               host: "localhost",
               database: "coftea_db",   // 👈 must match your phpMyAdmin database name
               user: "root",            // default XAMPP MySQL user
               password: ""             // default is empty (no password)
           );
        }

        public async Task InitializeAsync(string email)
        {

            if (App.CurrentUser != null)
            {
                IsAdmin = App.CurrentUser.IsAdmin;
            }
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
    }
}