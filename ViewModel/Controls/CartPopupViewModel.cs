using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Coftea_Capstone.Models;
using Coftea_Capstone.C_;

namespace Coftea_Capstone.ViewModel.Controls
{
    public partial class CartPopupViewModel : ObservableObject
    {
        [ObservableProperty] 
        private bool isCartVisible = false;

        [ObservableProperty] 
        private ObservableCollection<POSPageModel> cartItems = new();

        public CartPopupViewModel()
        {
        }

        public void ShowCart(ObservableCollection<POSPageModel> items)
        {
            CartItems = items ?? new ObservableCollection<POSPageModel>();
            IsCartVisible = true;
        }

        [RelayCommand]
        private void CloseCart()
        {
            IsCartVisible = false;
        }

        [RelayCommand]
        private void EditCartItem(POSPageModel item)
        {
            if (item == null) return;
            
            // TODO: Implement edit functionality
            // This could open another popup or navigate to edit page
        }

        [RelayCommand]
        private void Checkout()
        {
            if (CartItems == null || !CartItems.Any())
                return;

            // TODO: Implement checkout functionality
            // This could process the payment and clear the cart
        }
    }
}
