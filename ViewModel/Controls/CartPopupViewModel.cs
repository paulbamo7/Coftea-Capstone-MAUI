using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Coftea_Capstone.Models;
using Coftea_Capstone.C_;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace Coftea_Capstone.ViewModel.Controls
{
    public partial class CartPopupViewModel : ObservableObject
    {
        [ObservableProperty] 
        private bool isCartVisible = false;

        [ObservableProperty] 
        private ObservableCollection<CartItem> cartItems = new();

        [ObservableProperty]
        private string customerName = "Sanchez"; // Default customer name

        [ObservableProperty]
        private decimal totalAmount;

        private ObservableCollection<POSPageModel> _originalItems;

        public CartPopupViewModel()
        {
        }

        public void ShowCart(ObservableCollection<POSPageModel> items)
        {
            // Store reference to original items for quantity reset
            _originalItems = items;
            
            // Convert POSPageModel items to CartItem format - combine same products
            CartItems.Clear();
            var productGroups = (items ?? new ObservableCollection<POSPageModel>())
                .Where(item => item.SmallQuantity > 0 || item.MediumQuantity > 0 || item.LargeQuantity > 0)
                .GroupBy(item => item.ProductID);

            foreach (var group in productGroups)
            {
                var firstItem = group.First();
                CartItems.Add(new CartItem
                {
                    ProductId = firstItem.ProductID,
                    ProductName = firstItem.ProductName,
                    ImageSource = firstItem.ImageSet,
                    CustomerName = CustomerName,
                    SugarLevel = "100%",
                    AddOns = new ObservableCollection<string> { "Expresso - P5", "Pearl - P5", "Nata - P10" },
                    SmallQuantity = firstItem.SmallQuantity,
                    MediumQuantity = firstItem.MediumQuantity,
                    LargeQuantity = firstItem.LargeQuantity,
                    SmallPrice = firstItem.SmallPrice,
                    MediumPrice = firstItem.MediumPrice,
                    LargePrice = firstItem.LargePrice,
                    SelectedSize = GetCombinedSizeDisplay(firstItem),
                    Quantity = firstItem.SmallQuantity + firstItem.MediumQuantity + firstItem.LargeQuantity,
                    Price = firstItem.SmallPrice + firstItem.MediumPrice + firstItem.LargePrice
                });
            }
            
            CalculateTotal();
            IsCartVisible = true;
        }

        private void CalculateTotal()
        {
            TotalAmount = CartItems.Sum(item => item.TotalPrice);
        }

        private string GetCombinedSizeDisplay(POSPageModel item)
        {
            var sizes = new List<string>();
            if (item.SmallQuantity > 0) sizes.Add($"Small: {item.SmallQuantity}");
            if (item.MediumQuantity > 0) sizes.Add($"Medium: {item.MediumQuantity}");
            if (item.LargeQuantity > 0) sizes.Add($"Large: {item.LargeQuantity}");
            
            return sizes.Count > 0 ? string.Join(", ", sizes) : "No sizes";
        }

        [RelayCommand]
        private void CloseCart()
        {
            IsCartVisible = false;
        }

        [RelayCommand]
        private void EditCartItem(CartItem item)
        {
            if (item == null) return;
            
            // TODO: Implement edit functionality
            // This could open another popup or navigate to edit page
        }

        [RelayCommand]
        private void DeleteCartItem(CartItem item)
        {
            if (item == null) return;
            
            // Reset quantities in the original POSPageModel
            if (_originalItems != null)
            {
                var originalItem = _originalItems.FirstOrDefault(x => x.ProductID == item.ProductId);
                if (originalItem != null)
                {
                    originalItem.SmallQuantity = 0;
                    originalItem.MediumQuantity = 0;
                    originalItem.LargeQuantity = 0;
                }
            }
            
            CartItems.Remove(item);
            CalculateTotal();
        }

        [RelayCommand]
        private async Task Checkout()
        {
            if (CartItems == null || !CartItems.Any())
                return;

            System.Diagnostics.Debug.WriteLine($"Checkout called with {CartItems.Count} items, total: {TotalAmount}");
            
            // Navigate to payment screen
            IsCartVisible = false;
            
            // Show payment popup using the shared instance from App
            var app = (App)Application.Current;
            if (app?.PaymentPopup != null)
            {
                System.Diagnostics.Debug.WriteLine("Calling ShowPayment on PaymentPopup");
                app.PaymentPopup.ShowPayment(TotalAmount, CartItems.ToList());
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("PaymentPopup is null!");
            }
        }
    }
}
