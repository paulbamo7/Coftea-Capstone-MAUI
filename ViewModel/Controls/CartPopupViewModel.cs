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
using Coftea_Capstone;

namespace Coftea_Capstone.ViewModel.Controls
{
    public partial class CartPopupViewModel : ObservableObject
    {
        [ObservableProperty] 
        private bool isCartVisible = false;

        [ObservableProperty] 
        private ObservableCollection<CartItem> cartItems = new();

        [ObservableProperty]
        private decimal totalAmount;

        [ObservableProperty]
        private string customerName = string.Empty;

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

        public void ShowCart(ObservableCollection<POSPageModel> items) // Load and display cart items
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🛒 ShowCart called with {items?.Count ?? 0} items");
                
                // Store reference to original items for quantity reset
                _originalItems = items;
                
                // Convert POSPageModel items to CartItem format - show ALL items in cart
                CartItems.Clear();
                var flatItems = (items ?? new ObservableCollection<POSPageModel>())
                    .Where(item => item != null && (item.SmallQuantity > 0 || item.MediumQuantity > 0 || item.LargeQuantity > 0))
                    .ToList();
                
                System.Diagnostics.Debug.WriteLine($"🛒 Processing {flatItems.Count} items with quantities > 0 for cart display");

                foreach (var it in flatItems)
                {
                    if (it != null)
                    {
                        // Calculate addon prices
                        decimal addonTotalPrice = 0;
                        var addonNames = new ObservableCollection<string>();
                        
                        if (it.InventoryItems != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"🛒 Processing product: {it.ProductName}, InventoryItems count: {it.InventoryItems.Count}");
                            foreach (var addon in it.InventoryItems)
                            {
                                System.Diagnostics.Debug.WriteLine($"🛒 Addon: {addon.itemName}, IsSelected: {addon.IsSelected}, AddonQuantity: {addon.AddonQuantity}");
                            }
                            
                            var selectedAddons = it.InventoryItems.Where(a => a.IsSelected && a.AddonQuantity > 0).ToList();
                            System.Diagnostics.Debug.WriteLine($"🛒 Selected addons count: {selectedAddons.Count}");
                            
                            // Also check for any addons with quantity > 0 regardless of IsSelected
                            var addonsWithQuantity = it.InventoryItems.Where(a => a.AddonQuantity > 0).ToList();
                            System.Diagnostics.Debug.WriteLine($"🛒 Addons with quantity > 0: {addonsWithQuantity.Count}");
                            foreach (var addon in addonsWithQuantity)
                            {
                                System.Diagnostics.Debug.WriteLine($"🛒 Addon with quantity: {addon.itemName}, IsSelected: {addon.IsSelected}, AddonQuantity: {addon.AddonQuantity}");
                            }
                            
                            foreach (var addon in selectedAddons)
                            {
                                var addonPrice = addon.AddonTotalPrice;
                                addonTotalPrice += addonPrice;
                                var addonName = $"{addon.itemName} (x{addon.AddonQuantity})";
                                addonNames.Add(addonName);
                                System.Diagnostics.Debug.WriteLine($"🛒 Selected Addon: {addon.itemName}, Quantity: {addon.AddonQuantity}, Unit Price: {addon.AddonPrice}, Total: {addonPrice}");
                                System.Diagnostics.Debug.WriteLine($"🛒 Addon name added to collection: '{addonName}'");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"🛒 Product {it.ProductName} has no InventoryItems");
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"Product: {it.ProductName}, Addon Total: {addonTotalPrice}");
                        
                        System.Diagnostics.Debug.WriteLine($"🛒 Creating CartItem for {it.ProductName} with quantities - Small: {it.SmallQuantity}, Medium: {it.MediumQuantity}, Large: {it.LargeQuantity}");
                        
                        var cartItem = new CartItem // Create CartItem from POSPageModel
                        {
                            ProductId = it.ProductID,
                            ProductName = it.ProductName ?? "Unknown Product",
                            ImageSource = it.ImageSet ?? "dotnet_bot.png",
                            CustomerName = CustomerName,
                            SugarLevel = "100%",
                            AddOns = addonNames,
                            SmallQuantity = it.SmallQuantity,
                            MediumQuantity = it.MediumQuantity,
                            LargeQuantity = it.LargeQuantity,
                            SmallPrice = it.SmallPrice,
                            MediumPrice = it.MediumPrice,
                            LargePrice = it.LargePrice,
                            SelectedSize = GetCombinedSizeDisplay(it),
                            Quantity = it.SmallQuantity + it.MediumQuantity + it.LargeQuantity,
                            Price = (it.SmallQuantity * it.SmallPrice) + (it.MediumQuantity * it.MediumPrice) + (it.LargeQuantity * it.LargePrice) + addonTotalPrice
                        };
                        
                        System.Diagnostics.Debug.WriteLine($"🛒 CartItem created with quantities - Small: {cartItem.SmallQuantity}, Medium: {cartItem.MediumQuantity}, Large: {cartItem.LargeQuantity}");

                        // Carry addon items with quantities to checkout for proper deductions
                        if (it.InventoryItems != null)
                        {
                            foreach (var addon in it.InventoryItems)
                            {
                                cartItem.InventoryItems.Add(new InventoryPageModel
                                {
                                    itemID = addon.itemID,
                                    itemName = addon.itemName,
                                    unitOfMeasurement = addon.unitOfMeasurement,
                                    AddonQuantity = addon.AddonQuantity,
                                    IsSelected = addon.IsSelected
                                });
                            }
                        }                                              
                        CartItems.Add(cartItem);
                    }
                }
                CalculateTotal();
                IsCartVisible = true;
            }
            catch (Exception ex)
            {
                // Ensure cart is not visible if there's an error
                IsCartVisible = false;
            }
        }

        private void CalculateTotal() // Recalculate total amount
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

        private string GetCombinedSizeDisplay(POSPageModel item) // Get combined size display string
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

        public void ClearCart() // Clear all cart items
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🧹 Clearing cart popup items");
                CartItems.Clear();
                _originalItems?.Clear();
                TotalAmount = 0;
                CustomerName = string.Empty;
                System.Diagnostics.Debug.WriteLine("✅ Cart popup cleared successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error clearing cart popup: {ex.Message}");
            }
        }

        [RelayCommand]
        private async void EditCartItem(CartItem item) // Reopen item in POS for editing
        {
            if (item == null) return;
            
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔧 EditCartItem: Starting edit for {item.ProductName}");
                System.Diagnostics.Debug.WriteLine($"🔧 Cart item quantities - Small: {item.SmallQuantity}, Medium: {item.MediumQuantity}, Large: {item.LargeQuantity}");
                System.Diagnostics.Debug.WriteLine($"🔧 Cart item ProductId: {item.ProductId}");
                
                // Close the cart popup immediately
                IsCartVisible = false;
                System.Diagnostics.Debug.WriteLine($"🔧 Cart popup closed, IsCartVisible: {IsCartVisible}");
                
                // Navigate to POS page first
                System.Diagnostics.Debug.WriteLine($"🔧 Navigating to POS page...");
                await Coftea_Capstone.Services.SimpleNavigationService.NavigateToAsync("//pos");
                
                // Wait for navigation to complete
                await Task.Delay(1000);
                System.Diagnostics.Debug.WriteLine($"🔧 Navigation completed, waiting for POS page to load...");
                
                // Find the original POSPageModel item
                if (_originalItems != null)
                {
                    var originalItem = _originalItems.FirstOrDefault(x => x.ProductID == item.ProductId);
                    if (originalItem != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"🔧 Found original item: {originalItem.ProductName}");
                        System.Diagnostics.Debug.WriteLine($"🔧 Original item quantities - Small: {originalItem.SmallQuantity}, Medium: {originalItem.MediumQuantity}, Large: {originalItem.LargeQuantity}");
                        System.Diagnostics.Debug.WriteLine($"🔧 Original item ProductID: {originalItem.ProductID}");
                        
                        // Set the selected product with quantities
                        await SetSelectedProductInCurrentPOS(originalItem, item);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Original item not found for ProductID: {item.ProductId}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error in EditCartItem: {ex.Message}");
            }
        }

        public async Task SetSelectedProductInCurrentPOS(POSPageModel product, CartItem cartItem = null) // Set selected product in current POS page
        {
            try
            {
                if (product == null) return;
                
                System.Diagnostics.Debug.WriteLine($"🔧 SetSelectedProductInCurrentPOS: Setting product {product.ProductName}");
                
                // Wait a bit more for navigation to complete
                await Task.Delay(1000);
                
                // Use the shared POS ViewModel from App instead of trying to detect the page
                var app = Application.Current as App;
                var posViewModel = app?.POSVM;
                
                System.Diagnostics.Debug.WriteLine($"🔧 Using shared POS ViewModel: {posViewModel != null}");
                
                if (posViewModel != null)
                {
                    // Find the product in the current product list to ensure it exists
                    var currentProduct = posViewModel.Products?.FirstOrDefault(p => p.ProductID == product.ProductID);
                    if (currentProduct != null)
                    {
                        // Restore quantities from cart item (this has the correct quantities, not the original product)
                        if (cartItem != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"🔧 Restoring quantities from CartItem - Small: {cartItem.SmallQuantity}, Medium: {cartItem.MediumQuantity}, Large: {cartItem.LargeQuantity}");
                            
                            // Set quantities using property setters (they will trigger OnPropertyChanged automatically)
                            currentProduct.SmallQuantity = cartItem.SmallQuantity;
                            currentProduct.MediumQuantity = cartItem.MediumQuantity;
                            currentProduct.LargeQuantity = cartItem.LargeQuantity;
                            
                            System.Diagnostics.Debug.WriteLine($"🔧 Quantities restored successfully - Small: {currentProduct.SmallQuantity}, Medium: {currentProduct.MediumQuantity}, Large: {currentProduct.LargeQuantity}");
                            
                            // Restore addons from cart item
                            if (cartItem.InventoryItems != null && cartItem.InventoryItems.Any())
                            {
                                System.Diagnostics.Debug.WriteLine($"🔧 Restoring {cartItem.InventoryItems.Count} addons from CartItem");
                                
                                // Clear existing addons
                                currentProduct.InventoryItems.Clear();
                                
                                // Copy addons from cart item
                                foreach (var addon in cartItem.InventoryItems)
                                {
                                    var addonCopy = new InventoryPageModel
                                    {
                                        itemID = addon.itemID,
                                        itemName = addon.itemName,
                                        itemCategory = addon.itemCategory,
                                        itemDescription = addon.itemDescription,
                                        itemQuantity = addon.itemQuantity,
                                        unitOfMeasurement = addon.unitOfMeasurement,
                                        minimumQuantity = addon.minimumQuantity,
                                        ImageSet = addon.ImageSet,
                                        IsSelected = addon.IsSelected,
                                        AddonQuantity = addon.AddonQuantity,
                                        AddonPrice = addon.AddonPrice,
                                        AddonUnit = addon.AddonUnit,
                                        InputAmount = addon.InputAmount,
                                        InputUnit = addon.InputUnit
                                    };
                                    currentProduct.InventoryItems.Add(addonCopy);
                                    System.Diagnostics.Debug.WriteLine($"🔧 Restored addon: {addonCopy.itemName}, IsSelected: {addonCopy.IsSelected}, AddonQuantity: {addonCopy.AddonQuantity}");
                                }
                                
                                System.Diagnostics.Debug.WriteLine($"🔧 Addons restored successfully - Total addons: {currentProduct.InventoryItems.Count}");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"🔧 No addons to restore from CartItem");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"🔧 No cartItem provided, keeping original quantities - Small: {currentProduct.SmallQuantity}, Medium: {currentProduct.MediumQuantity}, Large: {currentProduct.LargeQuantity}");
                        }
                        
                        // Set SelectedProduct and force UI refresh
                        posViewModel.SelectedProduct = null;
                        await Task.Delay(100);
                        posViewModel.SelectedProduct = currentProduct;
                        
                        // Additional verification
                        System.Diagnostics.Debug.WriteLine($"🔧 Final verification - SelectedProduct quantities - Small: {posViewModel.SelectedProduct?.SmallQuantity}, Medium: {posViewModel.SelectedProduct?.MediumQuantity}, Large: {posViewModel.SelectedProduct?.LargeQuantity}");
                        
                        System.Diagnostics.Debug.WriteLine($"✅ Selected product set to: {currentProduct.ProductName} with quantities restored");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Product {product.ProductName} not found in POS product list");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Could not get shared POS ViewModel from App");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error in SetSelectedProductInCurrentPOS: {ex.Message}");
            }
        }

        [RelayCommand]
        private void DeleteCartItem(CartItem item) // Remove item from cart
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
        private void IncreaseCartQty(CartItem item) // Increase item quantity
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
        private void DecreaseCartQty(CartItem item) // Decrease item quantity
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
        private async Task Checkout() // Proceed to checkout
        {
            try
            {       
                if (CartItems == null || !CartItems.Any())
                {
                    return;
                }

                // Navigate to payment screen
                IsCartVisible = false;

                
                // Show payment popup using the shared instance from App
                var app = (App)Application.Current;
                if (app?.PaymentPopup != null)
                {
                    app.PaymentPopup.ShowPayment(TotalAmount, CartItems.ToList());
                }
                else
                {
                    // Try to show error notification
                    if (app?.NotificationPopup != null)
                    {
                        app.NotificationPopup.ShowNotification("Payment system is not available", "Error");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error in Checkout: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
            }
        }
    }
}