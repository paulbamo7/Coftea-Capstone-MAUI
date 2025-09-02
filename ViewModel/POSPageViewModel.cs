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
        [ObservableProperty]
        private ObservableCollection<POSPageModel> products;

        [ObservableProperty]
        private ObservableCollection<POSPageModel> cartItems;

        public POSPageViewModel()
        {
            Products = new ObservableCollection<POSPageModel>
        {
            new POSPageModel { ProductID=1, Name="Caramel Latte", Price=120, Image="latte.png" },
            new POSPageModel { ProductID=2, Name="Milk Tea", Price=100, Image="milktea.png" },
            new POSPageModel { ProductID=3, Name="Espresso", Price=80, Image="espresso.png" }
            
        };

            CartItems = new ObservableCollection<POSPageModel>();
        }

        [RelayCommand]
        private void AddToCart(POSPageModel product)
        {
            if (product != null)
                CartItems.Add(product); // use the generated property
        }

    }
}
