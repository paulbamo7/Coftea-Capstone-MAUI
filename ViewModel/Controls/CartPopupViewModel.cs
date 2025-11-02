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
                        
                        // Collect addons from Small, Medium, Large collections and legacy InventoryItems
                        var allSelectedAddons = new List<(InventoryPageModel addon, string size)>();
                        
                        // Add Small addons
                        if (it.SmallAddons != null && it.SmallQuantity > 0)
                        {
                            foreach (var addon in it.SmallAddons)
                            {
                                if (addon != null && addon.IsSelected && addon.AddonQuantity > 0)
                                {
                                    allSelectedAddons.Add((addon, "Small"));
                                    System.Diagnostics.Debug.WriteLine($"🛒 Small addon found: {addon.itemName}, Quantity: {addon.AddonQuantity}");
                                }
                            }
                        }
                        
                        // Add Medium addons
                        if (it.MediumAddons != null && it.MediumQuantity > 0)
                        {
                            foreach (var addon in it.MediumAddons)
                            {
                                if (addon != null && addon.IsSelected && addon.AddonQuantity > 0)
                                {
                                    allSelectedAddons.Add((addon, "Medium"));
                                    System.Diagnostics.Debug.WriteLine($"🛒 Medium addon found: {addon.itemName}, Quantity: {addon.AddonQuantity}");
                                }
                            }
                        }
                        
                        // Add Large addons
                        if (it.LargeAddons != null && it.LargeQuantity > 0)
                        {
                            foreach (var addon in it.LargeAddons)
                            {
                                if (addon != null && addon.IsSelected && addon.AddonQuantity > 0)
                                {
                                    allSelectedAddons.Add((addon, "Large"));
                                    System.Diagnostics.Debug.WriteLine($"🛒 Large addon found: {addon.itemName}, Quantity: {addon.AddonQuantity}");
                                }
                            }
                        }
                        
                        // Legacy: Add old InventoryItems if any (for backward compatibility)
                        if (it.InventoryItems != null)
                        {
                            foreach (var addon in it.InventoryItems)
                            {
                                if (addon != null && addon.IsSelected && addon.AddonQuantity > 0)
                                {
                                    allSelectedAddons.Add((addon, ""));
                                    System.Diagnostics.Debug.WriteLine($"🛒 Legacy addon found: {addon.itemName}, Quantity: {addon.AddonQuantity}");
                                }
                            }
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"🛒 Total selected addons: {allSelectedAddons.Count}");
                        
                        foreach (var (addon, size) in allSelectedAddons)
                        {
                            // Calculate price based on size quantity
                            decimal addonPriceForSize = addon.AddonPrice * addon.AddonQuantity;
                            
                            // Multiply by size quantity if applicable
                            if (size == "Small" && it.SmallQuantity > 0)
                            {
                                addonPriceForSize *= it.SmallQuantity;
                            }
                            else if (size == "Medium" && it.MediumQuantity > 0)
                            {
                                addonPriceForSize *= it.MediumQuantity;
                            }
                            else if (size == "Large" && it.LargeQuantity > 0)
                            {
                                addonPriceForSize *= it.LargeQuantity;
                            }
                            
                            addonTotalPrice += addonPriceForSize;
                            
                            // Format addon name with size prefix
                            string addonName = string.IsNullOrEmpty(size)
                                ? $"{addon.itemName} (x{addon.AddonQuantity})"
                                : $"{size}: {addon.itemName} (x{addon.AddonQuantity})";
                            
                            addonNames.Add(addonName);
                            System.Diagnostics.Debug.WriteLine($"🛒 Selected Addon: {addonName}, Quantity: {addon.AddonQuantity}, Unit Price: {addon.AddonPrice}, Total: {addonPriceForSize}");
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"Product: {it.ProductName}, Addon Total: {addonTotalPrice}");
                        
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
                            Price = (it.SmallQuantity * (it.SmallPrice ?? 0)) + (it.MediumQuantity * it.MediumPrice) + (it.LargeQuantity * it.LargePrice) + addonTotalPrice
                        };

                        // Carry addon items with quantities to checkout for proper deductions
                        // Copy Small size addons (only selected ones)
                        if (it.SmallAddons != null && it.SmallQuantity > 0)
                        {
                            foreach (var addon in it.SmallAddons)
                            {
                                if (addon == null || !addon.IsSelected) continue;
                                cartItem.SmallAddons.Add(new InventoryPageModel
                                {
                                    itemID = addon.itemID,
                                    itemName = addon.itemName,
                                    unitOfMeasurement = addon.unitOfMeasurement,
                                    AddonQuantity = addon.AddonQuantity,
                                    IsSelected = addon.IsSelected,
                                    AddonPrice = addon.AddonPrice,
                                    AddonUnit = addon.AddonUnit
                                });
                            }
                        }
                        
                        // Copy Medium size addons (only selected ones)
                        if (it.MediumAddons != null && it.MediumQuantity > 0)
                        {
                            foreach (var addon in it.MediumAddons)
                            {
                                if (addon == null || !addon.IsSelected) continue;
                                cartItem.MediumAddons.Add(new InventoryPageModel
                                {
                                    itemID = addon.itemID,
                                    itemName = addon.itemName,
                                    unitOfMeasurement = addon.unitOfMeasurement,
                                    AddonQuantity = addon.AddonQuantity,
                                    IsSelected = addon.IsSelected,
                                    AddonPrice = addon.AddonPrice,
                                    AddonUnit = addon.AddonUnit
                                });
                            }
                        }
                        
                        // Copy Large size addons (only selected ones)
                        if (it.LargeAddons != null && it.LargeQuantity > 0)
                        {
                            foreach (var addon in it.LargeAddons)
                            {
                                if (addon == null || !addon.IsSelected) continue;
                                cartItem.LargeAddons.Add(new InventoryPageModel
                                {
                                    itemID = addon.itemID,
                                    itemName = addon.itemName,
                                    unitOfMeasurement = addon.unitOfMeasurement,
                                    AddonQuantity = addon.AddonQuantity,
                                    IsSelected = addon.IsSelected,
                                    AddonPrice = addon.AddonPrice,
                                    AddonUnit = addon.AddonUnit
                                });
                            }
                        }
                        
                        // Legacy: Copy old InventoryItems if any (for backward compatibility)
                        if (it.InventoryItems != null)
                        {
                            foreach (var addon in it.InventoryItems)
                            {
                                if (addon == null || !addon.IsSelected) continue;
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
                
                // IMPORTANT: Remove the item from cart first (to avoid duplicates when re-adding after edit)
                // But preserve addon information before removing
                List<InventoryPageModel> preservedAddons = null;
                
                if (_originalItems != null)
                {
                    var matchingCartItem = _originalItems.FirstOrDefault(x =>
                        x.ProductID == item.ProductId &&
                        x.SmallQuantity == item.SmallQuantity &&
                        x.MediumQuantity == item.MediumQuantity &&
                        x.LargeQuantity == item.LargeQuantity);
                    
                    if (matchingCartItem != null)
                    {
                        // Preserve addon information before removing
                        if (matchingCartItem.InventoryItems != null && matchingCartItem.InventoryItems.Any(a => a.IsSelected || a.AddonQuantity > 0))
                        {
                            preservedAddons = new List<InventoryPageModel>();
                            foreach (var addon in matchingCartItem.InventoryItems)
                            {
                                if (addon.IsSelected || addon.AddonQuantity > 0)
                                {
                                    preservedAddons.Add(new InventoryPageModel
                                    {
                                        itemID = addon.itemID,
                                        itemName = addon.itemName,
                                        itemCategory = addon.itemCategory,
                                        itemDescription = addon.itemDescription,
                                        itemQuantity = addon.itemQuantity,
                                        unitOfMeasurement = addon.unitOfMeasurement,
                                        minimumQuantity = addon.minimumQuantity,
                                        maximumQuantity = addon.maximumQuantity,
                                        ImageSet = addon.ImageSet,
                                        IsSelected = addon.IsSelected,
                                        AddonQuantity = addon.AddonQuantity,
                                        AddonPrice = addon.AddonPrice,
                                        AddonUnit = addon.AddonUnit,
                                        InputAmount = addon.InputAmount,
                                        InputUnit = addon.InputUnit
                                    });
                                    System.Diagnostics.Debug.WriteLine($"🔧 Preserved addon: {addon.itemName}, IsSelected: {addon.IsSelected}, AddonQuantity: {addon.AddonQuantity}");
                                }
                            }
                            System.Diagnostics.Debug.WriteLine($"🔧 Preserved {preservedAddons.Count} addons for restoration");
                        }
                        
                        _originalItems.Remove(matchingCartItem);
                        System.Diagnostics.Debug.WriteLine($"🔧 Removed item from cart for editing");
                    }
                }
                
                // Close the cart popup
                IsCartVisible = false;
                System.Diagnostics.Debug.WriteLine($"🔧 EditCartItem: Cart popup closed, waiting for UI to update...");
                
                // Wait for popup to fully close before accessing underlying page
                await Task.Delay(200);
                
                // Find the product in the product list and restore it with quantities AND addons
                if (_originalItems != null)
                {
                        // Navigate to POS page if not already there, then set the selected product with quantities
                    await SetSelectedProductInCurrentPOS(item, preservedAddons);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error in EditCartItem: {ex.Message}");
            }
        }

        private async Task SetSelectedProductInCurrentPOS(CartItem cartItem, List<InventoryPageModel> preservedAddons = null) // Set selected product in current POS page from cart item
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔧 SetSelectedProductInCurrentPOS: Starting for {cartItem?.ProductName ?? "null"} (ID: {cartItem?.ProductId ?? 0})");
                System.Diagnostics.Debug.WriteLine($"🔧 CartItem quantities - Small: {cartItem?.SmallQuantity ?? 0}, Medium: {cartItem?.MediumQuantity ?? 0}, Large: {cartItem?.LargeQuantity ?? 0}");
                
                if (cartItem == null) 
                {
                    System.Diagnostics.Debug.WriteLine($"❌ SetSelectedProductInCurrentPOS: cartItem is null");
                    return;
                }
                
                // App uses Shell navigation, not NavigationPage
                // Try to get current page from Shell
                ContentPage currentPage = null;
                
                if (Shell.Current?.CurrentPage is ContentPage shellPage)
                {
                    currentPage = shellPage;
                    System.Diagnostics.Debug.WriteLine($"✅ SetSelectedProductInCurrentPOS: Found current page from Shell: {shellPage.GetType().Name}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"🔧 SetSelectedProductInCurrentPOS: Current page not found via Shell, navigating to POS...");
                    try
                    {
                        await Coftea_Capstone.Services.SimpleNavigationService.NavigateToAsync("//pos");
                        await Task.Delay(500); // Wait for navigation to complete
                        
                        if (Shell.Current?.CurrentPage is ContentPage newPage)
                        {
                            currentPage = newPage;
                            System.Diagnostics.Debug.WriteLine($"✅ SetSelectedProductInCurrentPOS: Found page after navigation: {newPage.GetType().Name}");
                        }
                    }
                    catch (Exception navEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Navigation to POS failed: {navEx.Message}");
                    }
                }

                if (currentPage == null)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ SetSelectedProductInCurrentPOS: Could not find current page - cannot proceed");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"🔧 SetSelectedProductInCurrentPOS: Current page type: {currentPage.GetType().Name}");

                // Check if current page is POS page
                if (currentPage is PointOfSale posPage)
                {
                    System.Diagnostics.Debug.WriteLine($"✅ SetSelectedProductInCurrentPOS: Found PointOfSale page");
                    
                    // Get the POS page's ViewModel and set the selected product
                    if (posPage.BindingContext is POSPageViewModel posViewModel)
                    {
                        System.Diagnostics.Debug.WriteLine($"✅ SetSelectedProductInCurrentPOS: Found POSPageViewModel");
                        System.Diagnostics.Debug.WriteLine($"🔧 SetSelectedProductInCurrentPOS: Products count: {posViewModel.Products?.Count ?? 0}");
                        
                        // Find the product in the current product list
                        var currentProduct = posViewModel.Products?.FirstOrDefault(p => p.ProductID == cartItem.ProductId);
                        System.Diagnostics.Debug.WriteLine($"🔧 SetSelectedProductInCurrentPOS: Search result - Found: {currentProduct != null}, Looking for ProductID={cartItem.ProductId}");
                        if (currentProduct != null)
                        {
                            // Select using the ViewModel command so all related UI state updates
                            try { posViewModel.SelectProductCommand.Execute(currentProduct); } catch { }
                            await Task.Delay(50);

                            // Restore quantities AFTER selection to ensure they stick
                                currentProduct.SmallQuantity = cartItem.SmallQuantity;
                                currentProduct.MediumQuantity = cartItem.MediumQuantity;
                                currentProduct.LargeQuantity = cartItem.LargeQuantity;
                                System.Diagnostics.Debug.WriteLine($"🔧 Restored quantities - Small: {cartItem.SmallQuantity}, Medium: {cartItem.MediumQuantity}, Large: {cartItem.LargeQuantity}");

                            // Restore addons if preserved
                            if (preservedAddons != null && preservedAddons.Any())
                            {
                                System.Diagnostics.Debug.WriteLine($"🔧 Restoring {preservedAddons.Count} preserved addons...");
                                
                                // Wait longer for addons to load from database (LoadAddonsForSelectedAsync is async)
                                // Keep checking until InventoryItems has items or timeout
                                int retries = 0;
                                while ((currentProduct.InventoryItems == null || currentProduct.InventoryItems.Count == 0) && retries < 10)
                                {
                                    await Task.Delay(100);
                                    retries++;
                                }
                                
                                System.Diagnostics.Debug.WriteLine($"🔧 Addon loading check: InventoryItems count = {currentProduct.InventoryItems?.Count ?? 0}, retries = {retries}");
                                
                                if (currentProduct.InventoryItems != null && currentProduct.InventoryItems.Any())
                                {
                                    foreach (var addon in currentProduct.InventoryItems.ToList())
                                    {
                                        var preserved = preservedAddons.FirstOrDefault(a => a.itemID == addon.itemID);
                                        if (preserved != null)
                                        {
                                            addon.IsSelected = preserved.IsSelected;
                                            addon.AddonQuantity = preserved.AddonQuantity;
                                            System.Diagnostics.Debug.WriteLine($"✅ Restored addon: {addon.itemName}, IsSelected: {addon.IsSelected}, AddonQuantity: {addon.AddonQuantity}");
                                        }
                                        else
                                        {
                                            // Reset addons that weren't in cart
                                            addon.IsSelected = false;
                                            addon.AddonQuantity = 0;
                                        }
                                    }
                                    System.Diagnostics.Debug.WriteLine($"✅ Finished restoring addons");
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"⚠️ InventoryItems is null or empty after waiting, cannot restore addons");
                                }
                            }

                            // Force UI updates by reassigning SelectedProduct
                            var temp = posViewModel.SelectedProduct;
                            posViewModel.SelectedProduct = null;
                            posViewModel.SelectedProduct = temp;
                            System.Diagnostics.Debug.WriteLine($"✅ Selected product set to: {currentProduct.ProductName} in current POS page");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ Product {cartItem.ProductName} (ID: {cartItem.ProductId}) not found in current product list");
                            System.Diagnostics.Debug.WriteLine($"🔧 Available Product IDs: {string.Join(", ", posViewModel.Products?.Select(p => $"{p.ProductID}({p.ProductName})") ?? Enumerable.Empty<string>())}");
                            // Try to refresh the product list and try again
                            await posViewModel.LoadDataAsync();
                            currentProduct = posViewModel.Products?.FirstOrDefault(p => p.ProductID == cartItem.ProductId);
                            if (currentProduct != null)
                            {
                                try { posViewModel.SelectProductCommand.Execute(currentProduct); } catch { }
                                await Task.Delay(50);

                                    currentProduct.SmallQuantity = cartItem.SmallQuantity;
                                    currentProduct.MediumQuantity = cartItem.MediumQuantity;
                                    currentProduct.LargeQuantity = cartItem.LargeQuantity;
                                    System.Diagnostics.Debug.WriteLine($"🔧 Restored quantities after refresh - Small: {cartItem.SmallQuantity}, Medium: {cartItem.MediumQuantity}, Large: {cartItem.LargeQuantity}");

                                // Restore addons after refresh
                                if (preservedAddons != null && preservedAddons.Any())
                                {
                                    System.Diagnostics.Debug.WriteLine($"🔧 Restoring {preservedAddons.Count} preserved addons after refresh...");
                                    
                                    // Wait for addons to load
                                    int retries = 0;
                                    while ((currentProduct.InventoryItems == null || currentProduct.InventoryItems.Count == 0) && retries < 10)
                                    {
                                        await Task.Delay(100);
                                        retries++;
                                    }
                                    
                                    if (currentProduct.InventoryItems != null && currentProduct.InventoryItems.Any())
                                    {
                                        foreach (var addon in currentProduct.InventoryItems.ToList())
                                        {
                                            var preserved = preservedAddons.FirstOrDefault(a => a.itemID == addon.itemID);
                                            if (preserved != null)
                                            {
                                                addon.IsSelected = preserved.IsSelected;
                                                addon.AddonQuantity = preserved.AddonQuantity;
                                                System.Diagnostics.Debug.WriteLine($"✅ Restored addon after refresh: {addon.itemName}, IsSelected: {addon.IsSelected}, AddonQuantity: {addon.AddonQuantity}");
                                            }
                                            else
                                            {
                                                addon.IsSelected = false;
                                                addon.AddonQuantity = 0;
                                            }
                                        }
                                    }
                                }

                                var temp2 = posViewModel.SelectedProduct;
                                posViewModel.SelectedProduct = null;
                                posViewModel.SelectedProduct = temp2;
                                System.Diagnostics.Debug.WriteLine($"✅ Selected product set to: {currentProduct.ProductName} after refresh");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"❌ SetSelectedProductInCurrentPOS: Product still not found after refresh");
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ SetSelectedProductInCurrentPOS: POS page BindingContext is not POSPageViewModel (type: {posPage.BindingContext?.GetType().Name ?? "null"})");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ SetSelectedProductInCurrentPOS: Current page is not POS page (type: {currentPage.GetType().Name}), but we should have already navigated - cannot proceed");
                    // We've already tried to navigate above, so if we're still not on POS, something's wrong
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ SetSelectedProductInCurrentPOS: Error setting selected product: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Inner exception: {ex.InnerException.Message}");
                }
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
