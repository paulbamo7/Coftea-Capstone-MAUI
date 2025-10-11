using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Coftea_Capstone.Models;
using Coftea_Capstone.C_;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Coftea_Capstone.Views.Pages;
using Coftea_Capstone.ViewModel;

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
            try
            {
                // Initialize collections
                CartItems = new ObservableCollection<CartItem>();
                System.Diagnostics.Debug.WriteLine("CartPopupViewModel initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing CartPopupViewModel: {ex.Message}");
                // Ensure collections are still initialized even if there's an error
                CartItems = new ObservableCollection<CartItem>();
            }
        }

        public void ShowCart(ObservableCollection<POSPageModel> items)
        {
            try
            {
                // Store reference to original items for quantity reset
                _originalItems = items;
                
                // Convert POSPageModel items to CartItem format - combine same products
                CartItems.Clear();
                var flatItems = (items ?? new ObservableCollection<POSPageModel>())
                    .Where(item => item != null && (item.SmallQuantity > 0 || item.MediumQuantity > 0 || item.LargeQuantity > 0))
                    .ToList();

                foreach (var it in flatItems)
                {
                    if (it != null)
                    {
                        CartItems.Add(new CartItem
                        {
                            ProductId = it.ProductID,
                            ProductName = it.ProductName ?? "Unknown Product",
                            ImageSource = it.ImageSet ?? "dotnet_bot.png",
                            CustomerName = CustomerName,
                            SugarLevel = "100%",
                            AddOns = new ObservableCollection<string>(),
                            SmallQuantity = it.SmallQuantity,
                            MediumQuantity = it.MediumQuantity,
                            LargeQuantity = it.LargeQuantity,
                            SmallPrice = it.SmallPrice,
                            MediumPrice = it.MediumPrice,
                            LargePrice = it.LargePrice,
                            SelectedSize = GetCombinedSizeDisplay(it),
                            Quantity = it.SmallQuantity + it.MediumQuantity + it.LargeQuantity,
                            Price = it.SmallPrice + it.MediumPrice + it.LargePrice
                        });
                    }
                }
                
                CalculateTotal();
                IsCartVisible = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing cart: {ex.Message}");
                // Ensure cart is not visible if there's an error
                IsCartVisible = false;
            }
        }

        private void CalculateTotal()
        {
            try
            {
                TotalAmount = CartItems?.Sum(item => item?.TotalPrice ?? 0) ?? 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error calculating total: {ex.Message}");
                TotalAmount = 0;
            }
        }

        private string GetCombinedSizeDisplay(POSPageModel item)
        {
            try
            {
                if (item == null) return "No sizes";
                
                var sizes = new List<string>();
                if (item.SmallQuantity > 0) sizes.Add($"Small: {item.SmallQuantity}");
                if (item.MediumQuantity > 0) sizes.Add($"Medium: {item.MediumQuantity}");
                if (item.LargeQuantity > 0) sizes.Add($"Large: {item.LargeQuantity}");
                
                return sizes.Count > 0 ? string.Join(", ", sizes) : "No sizes";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting size display: {ex.Message}");
                return "No sizes";
            }
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
            
            // Close the cart popup immediately
            IsCartVisible = false;
            
            // Find the original POSPageModel item
            if (_originalItems != null)
            {
                var originalItem = _originalItems.FirstOrDefault(x => x.ProductID == item.ProductId);
                if (originalItem != null)
                {
                    // Set the selected product in the current POS page
                    SetSelectedProductInCurrentPOS(originalItem);
                }
            }
        }

        private void SetSelectedProductInCurrentPOS(POSPageModel product)
        {
            try
            {
                if (product == null) return;
                
                // Get the current page
                var nav = Application.Current?.MainPage as NavigationPage;
                if (nav?.CurrentPage == null) return;

                // Check if current page is POS page
                if (nav.CurrentPage is PointOfSale posPage)
                {
                    // Get the POS page's ViewModel and set the selected product
                    if (posPage.BindingContext is POSPageViewModel posViewModel)
                    {
                        posViewModel.SelectedProduct = product;
                        System.Diagnostics.Debug.WriteLine($"Selected product set to: {product.ProductName} in current POS page");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Current page is not POS page, cannot set selected product");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting selected product: {ex.Message}");
            }
        }

        [RelayCommand]
        private void DeleteCartItem(CartItem item)
        {
            if (item == null) return;
            
            // Remove matching line from the underlying POS cart and persist
            if (_originalItems != null)
            {
                // Try to match the exact cart line by product and quantities/prices
                var matching = _originalItems.FirstOrDefault(x =>
                    x.ProductID == item.ProductId &&
                    x.SmallQuantity == item.SmallQuantity &&
                    x.MediumQuantity == item.MediumQuantity &&
                    x.LargeQuantity == item.LargeQuantity &&
                    x.SmallPrice == item.SmallPrice &&
                    x.MediumPrice == item.MediumPrice &&
                    x.LargePrice == item.LargePrice);

                if (matching != null)
                {
                    _originalItems.Remove(matching);
                }

                // Persist the updated cart via POS view model
                var nav = Application.Current.MainPage as NavigationPage;
                if (nav?.CurrentPage is PointOfSale posPage && posPage.BindingContext is POSPageViewModel posViewModel)
                {
                    _ = posViewModel.SaveCartToStorageAsync();
                }
            }
            
            CartItems.Remove(item);
            CalculateTotal();
        }

        // Quantity controls for cart
        [RelayCommand]
        private void IncreaseCartQty(CartItem item)
        {
            if (item == null) return;
            // Prefer bumping the currently selected size if any, else small
            if (item.SmallQuantity + item.MediumQuantity + item.LargeQuantity == 0)
            {
                item.SmallQuantity = 1;
            }
            else
            {
                // Default bump small for simplicity
                item.SmallQuantity += 1;
            }
            OnPropertyChanged(nameof(CartItems));
            CalculateTotal();
        }

        [RelayCommand]
        private void DecreaseCartQty(CartItem item)
        {
            if (item == null) return;
            var total = item.SmallQuantity + item.MediumQuantity + item.LargeQuantity;
            if (total <= 1)
            {
                // keep at least 1
                return;
            }
            if (item.SmallQuantity > 0) item.SmallQuantity -= 1;
            else if (item.MediumQuantity > 0) item.MediumQuantity -= 1;
            else if (item.LargeQuantity > 0) item.LargeQuantity -= 1;
            OnPropertyChanged(nameof(CartItems));
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
