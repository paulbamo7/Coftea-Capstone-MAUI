using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Coftea_Capstone.Models;
using Coftea_Capstone.Services;
using Coftea_Capstone.Models.Service;

namespace Coftea_Capstone.ViewModel.Controls
{
    public partial class PaymentPopupViewModel : ObservableObject
    {
        private readonly CartStorageService _cartStorage = new CartStorageService();
        [ObservableProperty]
        private bool isPaymentVisible = false;

        partial void OnIsPaymentVisibleChanged(bool value)
        {
            System.Diagnostics.Debug.WriteLine($"PaymentPopup IsPaymentVisible changed to: {value}");
        }

        [ObservableProperty]
        private decimal totalAmount;

        [ObservableProperty]
        private decimal amountPaid;

        [ObservableProperty]
        private decimal change;

        [ObservableProperty]
        private string paymentStatus = "Pending";

        [ObservableProperty]
        private string selectedPaymentMethod = "Cash";

        // Convenience flags used by UI/logic
        public bool IsCashSelected => string.Equals(SelectedPaymentMethod, "Cash", System.StringComparison.OrdinalIgnoreCase);
        public bool IsGCashSelected => string.Equals(SelectedPaymentMethod, "GCash", System.StringComparison.OrdinalIgnoreCase);
        public bool IsBankSelected => string.Equals(SelectedPaymentMethod, "Bank", System.StringComparison.OrdinalIgnoreCase);

        [ObservableProperty]
        private List<CartItem> cartItems = new();

        public PaymentPopupViewModel()
        {
            IsPaymentVisible = false;
            TotalAmount = 0;
            AmountPaid = 0;
            Change = 0;
            PaymentStatus = "Pending";
            CartItems = new List<CartItem>();
            
            // Debug: Log that PaymentPopup is initialized as hidden
            System.Diagnostics.Debug.WriteLine("PaymentPopupViewModel initialized with IsPaymentVisible = false");
        }

        public void ShowPayment(decimal total, List<CartItem> items) // Show payment popup with total and cart items
        {
            System.Diagnostics.Debug.WriteLine($"ShowPayment called with total: {total}, items: {items.Count}");
            TotalAmount = total;
            CartItems = items;
            AmountPaid = 0;
            Change = 0;
            PaymentStatus = "Pending";
            IsPaymentVisible = true;
            System.Diagnostics.Debug.WriteLine($"PaymentPopup IsPaymentVisible set to: {IsPaymentVisible}");
            
            // Force property change notification
            OnPropertyChanged(nameof(IsPaymentVisible));
        }

        [RelayCommand]
        private void ClosePayment() // Close payment popup
        {
            IsPaymentVisible = false;
        }

        [RelayCommand]
        private void SelectPaymentMethod(string method) // Cash, GCash, Bank
        {
            SelectedPaymentMethod = method;
            System.Diagnostics.Debug.WriteLine($"Payment method selected: {method}");

            // Auto-fill payment for non-cash methods so user doesn't need to type amount
            if (IsGCashSelected || IsBankSelected)
            {
                AmountPaid = TotalAmount;
                Change = 0;
                PaymentStatus = "Ready to Confirm";
            }
            else
            {
                // Cash – require amount
                AmountPaid = 0;
                Change = -TotalAmount;
                PaymentStatus = "Pending";
            }
        }

        public void UpdateAmountPaid(string amount) // Update amount paid from input
        {
            if (decimal.TryParse(amount, out decimal paid))
            {
                AmountPaid = paid;
                Change = AmountPaid - TotalAmount;
                
                if (Change >= 0)
                {
                    PaymentStatus = "Ready to Confirm";
                }
                else
                {
                    PaymentStatus = "Insufficient Amount";
                }
            }
        }

        [RelayCommand]
        private async Task ConfirmPayment() // Confirm and process payment
        {
            System.Diagnostics.Debug.WriteLine("🔵 ========== ConfirmPayment CALLED ==========");
            System.Diagnostics.Debug.WriteLine($"🔵 TotalAmount: {TotalAmount}, AmountPaid: {AmountPaid}");
            System.Diagnostics.Debug.WriteLine($"🔵 CartItems count: {CartItems?.Count ?? 0}");
            System.Diagnostics.Debug.WriteLine($"🔵 SelectedPaymentMethod: {SelectedPaymentMethod}");
            
            // For non-cash methods (GCash/Bank), auto-approve regardless of AmountPaid
            var isNonCash = IsGCashSelected || IsBankSelected;
            System.Diagnostics.Debug.WriteLine($"🔵 Is non-cash payment: {isNonCash}");
            if (!isNonCash)
            {
                if (AmountPaid < TotalAmount)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Insufficient Payment",
                        $"Amount paid (₱{AmountPaid:F2}) is less than total (₱{TotalAmount:F2})",
                        "OK");
                    return;
                }
            }

            // Validate inventory availability before processing payment
            var inventoryValidation = await ValidateInventoryAvailabilityAsync();
            if (!inventoryValidation.IsValid)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Insufficient Inventory",
                    inventoryValidation.ErrorMessage,
                    "OK");
                return;
            }

            // Confirm payment
            bool confirm = true;
            if (!isNonCash)
            {
                confirm = await Application.Current.MainPage.DisplayAlert(
                    "Confirm Payment",
                    $"Total: ₱{TotalAmount:F2}\nPaid: ₱{AmountPaid:F2}\nChange: ₱{Change:F2}\n\nProceed with payment?",
                    "Confirm",
                    "Cancel");
            }

            if (!confirm)
                return;

            System.Diagnostics.Debug.WriteLine("🔵 About to call ProcessPaymentWithSteps");
            
            // Assign payment method to each cart item BEFORE clearing
            var cartItemsCopy = new List<CartItem>();
            foreach (var item in CartItems)
            {
                item.PaymentMethod = SelectedPaymentMethod; // Assign the payment method to each item
                cartItemsCopy.Add(new CartItem
                {
                    ProductName = item.ProductName,
                    PaymentMethod = item.PaymentMethod,
                    Price = item.Price,
                    SmallQuantity = item.SmallQuantity,
                    MediumQuantity = item.MediumQuantity,
                    LargeQuantity = item.LargeQuantity
                });
            }
            
            // Process payment with realistic steps
            await ProcessPaymentWithSteps();
            System.Diagnostics.Debug.WriteLine("🔵 ProcessPaymentWithSteps completed");

            System.Diagnostics.Debug.WriteLine("🔵 About to call SaveTransaction");
            // Save transaction to database and shared store
            var savedTransactions = await SaveTransaction();
            System.Diagnostics.Debug.WriteLine($"🔵 SaveTransaction completed. Saved {savedTransactions?.Count ?? 0} transactions");

            // Clear cart on successful payment
            try
            {
                System.Diagnostics.Debug.WriteLine("🧹 Starting cart clear process...");
                
                // Clear the actual cart items in the payment popup
                CartItems.Clear();
                System.Diagnostics.Debug.WriteLine("✅ PaymentPopup CartItems cleared");
                
                // Also clear the cart in the POS page ViewModel
                var currentApp = (App)Application.Current;
                System.Diagnostics.Debug.WriteLine($"Current MainPage type: {currentApp?.MainPage?.GetType().Name}");
                
                // For Shell-based navigation, access the POS ViewModel directly from App
                if (currentApp?.POSVM != null)
                {
                    await currentApp.POSVM.ClearCartAsync();
                    System.Diagnostics.Debug.WriteLine("✅ Cart cleared in POS page ViewModel via App reference");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ POSPageViewModel not found in App");
                    
                    // Fallback: Try to find POS page in Shell
                    if (currentApp?.MainPage is Shell shell)
                    {
                        System.Diagnostics.Debug.WriteLine("Shell found, attempting to access current page...");
                        var currentPage = shell.CurrentPage;
                        System.Diagnostics.Debug.WriteLine($"Current Shell page: {currentPage?.GetType().Name}");
                        
                        if (currentPage is Coftea_Capstone.Views.Pages.PointOfSale posPage)
                        {
                            if (posPage.BindingContext is POSPageViewModel posViewModel)
                            {
                                await posViewModel.ClearCartAsync();
                                System.Diagnostics.Debug.WriteLine("✅ Cart cleared in POS page ViewModel via Shell.CurrentPage");
                            }
                        }
                    }
                }
                
                System.Diagnostics.Debug.WriteLine("✅ Cart clear process completed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error clearing cart: {ex.Message}\n{ex.StackTrace}");
            }

            // Show success: order-complete popup with auto-disappear
            PaymentStatus = "Payment Confirmed";
            var appInstance = (App)Application.Current;
            
            // Use the actual transaction ID for order number consistency
            var orderNumber = savedTransactions?.FirstOrDefault()?.TransactionId.ToString() ?? "Unknown";
            
            if (appInstance?.OrderCompletePopup != null)
            {
                await appInstance.OrderCompletePopup.ShowOrderCompleteAsync(orderNumber);
            }
            
            // Show order confirmation popup in bottom right with payment breakdown
            var orderConfirmedPopup = appInstance?.OrderConfirmedPopup;
            if (orderConfirmedPopup != null)
            {
                await orderConfirmedPopup.ShowOrderConfirmationAsync(orderNumber, TotalAmount, SelectedPaymentMethod, cartItemsCopy);
            }
            
            // No automatic toast here; user can open notifications manually via bell

            // Add notifications for each saved transaction (badge only; panel shows details when opened)
            var notif = ((App)Application.Current)?.NotificationPopup;
            if (notif != null && savedTransactions != null)
            {
                foreach (var t in savedTransactions)
                {
                    await notif.AddSuccess("Transaction", $"Completed: {t.DrinkName}", $"ID: {t.TransactionId}");
                }
            }

                    // Refresh today's section (recent orders + today's totals) in Sales Report
                    try
                    {
                        var salesApp = (App)Application.Current;
                        if (salesApp?.SalesReportVM != null)
                        {
                            await salesApp.SalesReportVM.RefreshTodayAsync();
                            System.Diagnostics.Debug.WriteLine("✅ Sales report 'Today' refreshed (orders + totals)");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Could not refresh sales report today: {ex.Message}");
                    }

            // Update Employee Dashboard recent orders + totals immediately
            var app = (App)Application.Current;
            var settingsPopup = app?.SettingsPopup;
            if (settingsPopup != null && savedTransactions != null)
            {
                try
                {
                    // Add each saved transaction to dashboard's recent orders on UI thread
                    foreach (var t in savedTransactions)
                    {
                        var tx = t; // avoid modified closure
                        // Try to resolve product image by name; fall back to default icon
                        string productImage = "drink.png";
                        try
                        {
                            var dbImg = new Models.Database();
                            var prod = await dbImg.GetProductByNameAsync(tx.DrinkName);
                            if (prod != null && !string.IsNullOrWhiteSpace(prod.ImageSet))
                            {
                                productImage = prod.ImageSet;
                            }
                        }
                        catch { }

                        Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                        {
                            settingsPopup.AddRecentOrder(
                                tx.TransactionId,
                                tx.DrinkName,
                                productImage,
                                tx.Total
                            );
                        });
                    }

                    // Reload dashboard aggregates from DB to ensure consistency
                    await settingsPopup.LoadTodaysMetricsAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Could not refresh employee dashboard: {ex.Message}");
                }
            }

            // Close payment popup
            IsPaymentVisible = false;
        }
        private async Task ProcessPaymentWithSteps() // Simulate payment processing steps
        {
            try
            {
                if (SelectedPaymentMethod == "Cash")
                {
                    // Cash payment - simple processing
                    PaymentStatus = "Processing Cash Payment...";
                    await Task.Delay(800);
                    PaymentStatus = "Payment Successful";
                }
                else if (SelectedPaymentMethod == "GCash")
                {
                    // GCash payment simulation
                    PaymentStatus = "Initializing GCash Payment...";
                    await Task.Delay(1000);
                    
                    PaymentStatus = "Generating QR Code...";
                    await Task.Delay(1200);
                    
                    PaymentStatus = "Waiting for Customer Scan...";
                    await Task.Delay(1500);
                    
                    PaymentStatus = "Verifying Payment...";
                    await Task.Delay(1000);
                    
                    PaymentStatus = "Payment Confirmed";
                    await Task.Delay(500);
                }
                else if (SelectedPaymentMethod == "Bank")
                {
                    // Bank transfer simulation
                    PaymentStatus = "Preparing Bank Transfer...";
                    await Task.Delay(1000);
                    
                    PaymentStatus = "Validating Account Details...";
                    await Task.Delay(1200);
                    
                    PaymentStatus = "Processing Transfer...";
                    await Task.Delay(1500);
                    
                    PaymentStatus = "Verifying Transaction...";
                    await Task.Delay(1000);
                    
                    PaymentStatus = "Transfer Completed";
                    await Task.Delay(500);
                }
            }
            catch (Exception ex)
            {
                PaymentStatus = "Payment Failed";
                System.Diagnostics.Debug.WriteLine($"Payment processing error: {ex.Message}");
                await Task.Delay(1000);
            }
        }
        private async Task<List<TransactionHistoryModel>> SaveTransaction() // Save transaction to database and in-memory store
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔄 Starting transaction save for {CartItems.Count} items");
                
                var app = (App)Application.Current;
                var transactions = app?.Transactions;
                var database = new Models.Database(); // Will use auto-detected host

                if (transactions != null)
                {
                    // Get the next available transaction ID from the database to ensure persistence
                    var nextId = await database.GetNextTransactionIdAsync();
                    var saved = new List<TransactionHistoryModel>();

                    // Create a single transaction for the entire order
                    var orderTotal = CartItems.Sum(item => item.TotalPrice);
                    var transactionId = nextId;
                    
                    // Create a combined transaction record for the entire order
                    // Calculate size-specific prices for the transaction
                    var smallTotal = CartItems.Sum(item => (item.SmallPrice ?? 0) * item.SmallQuantity);
                    var mediumTotal = CartItems.Sum(item => item.MediumPrice * item.MediumQuantity);
                    var largeTotal = CartItems.Sum(item => item.LargePrice * item.LargeQuantity);
                    var addonTotal = CartItems.Sum(item => Math.Max(0, item.TotalPrice - (((item.SmallPrice ?? 0) * item.SmallQuantity) + (item.MediumPrice * item.MediumQuantity) + (item.LargePrice * item.LargeQuantity))));

                    var orderTransaction = new TransactionHistoryModel
                    {
                        TransactionId = transactionId,
                        DrinkName = CartItems.Count == 1 ? CartItems[0].ProductName : $"{CartItems.Count} items",
                        Size = CartItems.Count == 1 ? CartItems[0].SelectedSize : "Multiple",
                        Quantity = CartItems.Sum(item => item.Quantity),
                        Price = orderTotal,
                        SmallPrice = (decimal)smallTotal,
                        MediumPrice = (decimal)mediumTotal,
                        LargePrice = (decimal)largeTotal,
                        AddonPrice = (decimal)addonTotal,
                        Vat = 0m,
                        Total = orderTotal,
                        AddOns = string.Join(", ", CartItems.Where(item => !string.IsNullOrWhiteSpace(item.AddOnsDisplay) && item.AddOnsDisplay != "No add-ons").Select(item => $"{item.ProductName}: {item.AddOnsDisplay}")),
                        CustomerName = CartItems.FirstOrDefault()?.CustomerName ?? "",
                        PaymentMethod = SelectedPaymentMethod,
                        Status = "Completed",
                        TransactionDate = DateTime.Now
                    };

                    // Save the combined transaction to database
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    await database.SaveTransactionAsync(orderTransaction);
                    System.Diagnostics.Debug.WriteLine($"✅ Database save successful for order: {transactionId}");
                    
                    // Deduct inventory for each item
                    System.Diagnostics.Debug.WriteLine($"🔧 Starting inventory deduction for {CartItems.Count} cart items");
                    foreach (var item in CartItems)
                    {
                        System.Diagnostics.Debug.WriteLine($"💾 ========== Deducting inventory for: {item.ProductName} ==========");
                        System.Diagnostics.Debug.WriteLine($"💾 Item details - Small: {item.SmallQuantity}, Medium: {item.MediumQuantity}, Large: {item.LargeQuantity}");
                        try
                        {
                            await DeductInventoryForItemAsync(database, item);
                            System.Diagnostics.Debug.WriteLine($"✅ Inventory deduction successful for: {item.ProductName}");
                        }
                        catch (Exception itemEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"❌ Failed to deduct inventory for {item.ProductName}: {itemEx.Message}");
                        }
                    }
                    System.Diagnostics.Debug.WriteLine($"🔧 Completed inventory deduction for all cart items");
                    
                    // Add to in-memory collection for history popup
                    transactions.Add(orderTransaction);
                    saved.Add(orderTransaction);

                    System.Diagnostics.Debug.WriteLine($"✅ Order transaction {transactionId} saved successfully with {CartItems.Count} items");
                    
                    // Refresh sales report data
                    try 
                    {
                        var currentApp = (App)Application.Current;
                        if (currentApp?.SalesReportVM != null)
                        {
                            await currentApp.SalesReportVM.LoadDataAsync();
                            System.Diagnostics.Debug.WriteLine("✅ Sales report data refreshed after transaction");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Error refreshing sales report: {ex.Message}");
                    }
                    
                    // Refresh inventory data
                    try 
                    {
                        var currentApp = (App)Application.Current;
                        if (currentApp?.InventoryVM != null)
                        {
                            await MainThread.InvokeOnMainThreadAsync(async () =>
                            {
                                await currentApp.InventoryVM.LoadDataAsync();
                                System.Diagnostics.Debug.WriteLine("✅ Inventory data refreshed after transaction");
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Error refreshing inventory: {ex.Message}");
                    }
                    
                    return saved;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("❌ App.Transactions is null");
                    await Application.Current.MainPage.DisplayAlert("Error", "Transaction storage not available", "OK");
                }
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("⏰ Transaction save timed out");
                await Application.Current.MainPage.DisplayAlert("Error", "Transaction save timed out. Please try again.", "OK");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ PaymentPopupViewModel error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                
                string errorMessage = "Failed to save transaction";
                if (ex.Message.Contains("foreign key constraint"))
                {
                    errorMessage = "Database error: User session invalid. Please log in again.";
                }
                else if (ex.Message.Contains("Connection"))
                {
                    errorMessage = "Database connection error. Please check your network.";
                }
                else if (ex.Message.Contains("timeout"))
                {
                    errorMessage = "Database operation timed out. Please check your connection.";
                }
                else
                {
                    errorMessage = $"Failed to save transaction: {ex.Message}";
                }
                
                await Application.Current.MainPage.DisplayAlert("Error", errorMessage, "OK");
            }
            return null;
        }

        private async Task DeductInventoryForItemAsync(Models.Database database, CartItem cartItem) // Deduct inventory based on cart item
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔧 DeductInventoryForItemAsync: Starting for {cartItem.ProductName}");
                System.Diagnostics.Debug.WriteLine($"🔧 Cart item quantities - Small: {cartItem.SmallQuantity}, Medium: {cartItem.MediumQuantity}, Large: {cartItem.LargeQuantity}");
                
                // Get product by name to get the product ID
                var product = await database.GetProductByNameAsyncCached(cartItem.ProductName);
                if (product == null)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Product not found: {cartItem.ProductName}");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"🔧 Found product: {product.ProductName} (ID: {product.ProductID})");

                // Validate and fix product-ingredient connections to ensure consistent deduction
                await database.ValidateAndFixProductIngredientsAsync(product.ProductID);

                // Get all ingredients for this product
                var ingredients = await database.GetProductIngredientsAsync(product.ProductID);
                if (!ingredients.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"❌ No ingredients found for product: {cartItem.ProductName}");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"🔧 Found {ingredients.Count()} ingredients for product: {cartItem.ProductName}");

                // Prepare deductions dictionary to accumulate amounts for the same ingredient
                var deductionsDict = new Dictionary<string, double>();

                // Helper local function to add deductions for a specific size using per-size amounts
                void AddSizeDeductions(string size, int qty)
                {
                    System.Diagnostics.Debug.WriteLine($"🔧 AddSizeDeductions called: Size={size}, Quantity={qty}");
                    if (qty <= 0) 
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Skipping {size} deductions - quantity is 0 or negative");
                        return;
                    }
                    foreach (var (ingredient, amount, unit, role) in ingredients)
                    {
                        // Choose per-size amount/unit when available; fall back to shared
                        // Use per-size amounts if they exist and are greater than 0, otherwise use shared amount
                        double perServing = size switch
                        {
                            "Small" => (ingredient.InputAmountSmall > 0) ? ingredient.InputAmountSmall : amount,
                            "Medium" => (ingredient.InputAmountMedium > 0) ? ingredient.InputAmountMedium : amount,
                            "Large" => (ingredient.InputAmountLarge > 0) ? ingredient.InputAmountLarge : amount,
                            _ => amount
                        };
                        
                        // Use per-size units if they exist and are not empty, otherwise use shared unit
                        string perUnit = size switch
                        {
                            "Small" => (!string.IsNullOrWhiteSpace(ingredient.InputUnitSmall)) ? ingredient.InputUnitSmall : unit,
                            "Medium" => (!string.IsNullOrWhiteSpace(ingredient.InputUnitMedium)) ? ingredient.InputUnitMedium : unit,
                            "Large" => (!string.IsNullOrWhiteSpace(ingredient.InputUnitLarge)) ? ingredient.InputUnitLarge : unit,
                            _ => unit
                        };
                        
                        // Final fallback to inventory unit if perUnit is still empty
                        if (string.IsNullOrWhiteSpace(perUnit))
                        {
                            perUnit = ingredient.unitOfMeasurement;
                        }

                        var targetUnit = UnitConversionService.Normalize(ingredient.unitOfMeasurement);
                        var converted = ConvertUnits(perServing, perUnit, targetUnit);
                        var total = Math.Round(converted, 6) * qty; // keep small ml/g deductions
                        
                        System.Diagnostics.Debug.WriteLine($"🔧 {ingredient.itemName} ({size}): {perServing} {perUnit} -> {converted} {targetUnit} * {qty} = {total}");
                        System.Diagnostics.Debug.WriteLine($"🔧   Per-size amounts: Small={ingredient.InputAmountSmall}, Medium={ingredient.InputAmountMedium}, Large={ingredient.InputAmountLarge}");
                        System.Diagnostics.Debug.WriteLine($"🔧   Per-size units: Small='{ingredient.InputUnitSmall}', Medium='{ingredient.InputUnitMedium}', Large='{ingredient.InputUnitLarge}'");
                        System.Diagnostics.Debug.WriteLine($"🔧   Shared amount={amount}, Shared unit='{unit}', Inventory unit='{ingredient.unitOfMeasurement}'");
                        
                        // Accumulate amounts for the same ingredient across different sizes
                        if (deductionsDict.ContainsKey(ingredient.itemName))
                        {
                            deductionsDict[ingredient.itemName] += total;
                        }
                        else
                        {
                            deductionsDict[ingredient.itemName] = total;
                        }
                    }
                }

                // Add per-size deductions based on cart quantities
                AddSizeDeductions("Small", cartItem.SmallQuantity);
                AddSizeDeductions("Medium", cartItem.MediumQuantity);
                AddSizeDeductions("Large", cartItem.LargeQuantity);

                // Include selected addons based on configured per-serving amounts
                // Fetch addon link definitions (amount/unit per serving)
                var addonLinks = await database.GetProductAddonsAsync(product.ProductID);
                System.Diagnostics.Debug.WriteLine($"🔧 ADDON DEDUCTION: Found {addonLinks?.Count ?? 0} addon links for product {product.ProductName}");
                
                if (addonLinks != null && addonLinks.Count > 0 && cartItem.InventoryItems != null)
                {
                    System.Diagnostics.Debug.WriteLine($"🔧 ADDON DEDUCTION: Cart has {cartItem.InventoryItems.Count} inventory items");
                    
                    foreach (var linkedAddon in addonLinks)
                    {
                        System.Diagnostics.Debug.WriteLine($"🔧 ADDON DEDUCTION: Processing linked addon: {linkedAddon.itemName} (ID: {linkedAddon.itemID})");
                        
                        // Find this addon in the cart with user-selected quantity
                        var cartAddon = cartItem.InventoryItems.FirstOrDefault(ai => ai.itemID == linkedAddon.itemID);
                        if (cartAddon == null)
                        {
                            System.Diagnostics.Debug.WriteLine($"   ⚠️ Addon not found in cart inventory items");
                            continue;
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"   ✅ Found in cart: IsSelected={cartAddon.IsSelected}, AddonQuantity={cartAddon.AddonQuantity}");
                        
                        if (!cartAddon.IsSelected || cartAddon.AddonQuantity <= 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"   ⚠️ Addon skipped: not selected or quantity <= 0");
                            continue;
                        }

                        // Use the user-entered amount/unit for addons (prioritize InputUnit over inventory unit)
                        var perServingAmount = linkedAddon.InputAmount > 0
                            ? linkedAddon.InputAmount
                            : (linkedAddon.InputAmountSmall > 0 ? linkedAddon.InputAmountSmall : 0);
                        
                        // Use the saved InputUnit from the addon configuration
                        var perServingUnit = !string.IsNullOrWhiteSpace(linkedAddon.InputUnit)
                            ? linkedAddon.InputUnit
                            : (!string.IsNullOrWhiteSpace(linkedAddon.InputUnitSmall)
                                ? linkedAddon.InputUnitSmall
                                : cartAddon.unitOfMeasurement);

                        System.Diagnostics.Debug.WriteLine($"   📊 Per-serving: {perServingAmount} {perServingUnit}");

                        if (perServingAmount <= 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"   ⚠️ Per-serving amount is 0, skipping addon");
                            continue; // nothing to deduct for this addon
                        }

                        // Convert to inventory base unit for deduction
                        // (Database stores inventory in base units like L, kg, etc.)
                        var inventoryBaseUnit = UnitConversionService.Normalize(cartAddon.unitOfMeasurement);
                        var convertedAmount = ConvertUnits(perServingAmount, perServingUnit, inventoryBaseUnit);

                        System.Diagnostics.Debug.WriteLine($"   🔄 Converting: {perServingAmount} {perServingUnit} → {convertedAmount} {inventoryBaseUnit}");

                        // Deduct addon based on total quantity ordered across all sizes
                        // AddonQuantity is per drink, so multiply by total drinks ordered
                        var totalDrinks = cartItem.SmallQuantity + cartItem.MediumQuantity + cartItem.LargeQuantity;
                        System.Diagnostics.Debug.WriteLine($"   📦 Total drinks: {totalDrinks}, Addon quantity per drink: {cartAddon.AddonQuantity}");
                        
                        if (totalDrinks > 0)
                        {
                            var totalAddonDeduction = convertedAmount * cartAddon.AddonQuantity * (double)totalDrinks;
                            System.Diagnostics.Debug.WriteLine($"   ➖ DEDUCTING: {totalAddonDeduction} {inventoryBaseUnit} ({convertedAmount} x {cartAddon.AddonQuantity} x {totalDrinks})");
                            
                            // Accumulate addon amounts for the same ingredient
                            if (deductionsDict.ContainsKey(cartAddon.itemName))
                            {
                                deductionsDict[cartAddon.itemName] += totalAddonDeduction;
                                System.Diagnostics.Debug.WriteLine($"   📝 Accumulated total for {cartAddon.itemName}: {deductionsDict[cartAddon.itemName]}");
                            }
                            else
                            {
                                deductionsDict[cartAddon.itemName] = totalAddonDeduction;
                                System.Diagnostics.Debug.WriteLine($"   📝 First deduction for {cartAddon.itemName}: {totalAddonDeduction}");
                            }
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"🔧 ADDON DEDUCTION: No addons to deduct (addonLinks={addonLinks?.Count ?? 0}, cartItems={cartItem.InventoryItems?.Count ?? 0})");
                }

                // Add automatic cups and straws per size (1 per serving)
                if (cartItem.SmallQuantity > 0)
                    await AddAutomaticCupAndStrawForSize(deductionsDict, "Small", cartItem.SmallQuantity);
                if (cartItem.MediumQuantity > 0)
                    await AddAutomaticCupAndStrawForSize(deductionsDict, "Medium", cartItem.MediumQuantity);
                if (cartItem.LargeQuantity > 0)
                    await AddAutomaticCupAndStrawForSize(deductionsDict, "Large", cartItem.LargeQuantity);

                // Convert dictionary back to list for database call
                var deductions = deductionsDict.Select(kvp => (kvp.Key, kvp.Value)).ToList();

                System.Diagnostics.Debug.WriteLine($"🔧 Final deductions for {cartItem.ProductName}:");
                foreach (var deduction in deductions)
                {
                    System.Diagnostics.Debug.WriteLine($"   - {deduction.Key}: {deduction.Value}");
                }

                // Deduct inventory
                System.Diagnostics.Debug.WriteLine($"🔧 Checking deductions.Any(): {deductions.Any()}");
                System.Diagnostics.Debug.WriteLine($"🔧 Deductions count: {deductions.Count}");
                
                if (deductions.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"🔧 About to call DeductInventoryAsync with {deductions.Count} deductions for product: {cartItem.ProductName}");
                    try
                    {
                        var affectedRows = await database.DeductInventoryAsync(deductions, cartItem.ProductName);
                        System.Diagnostics.Debug.WriteLine($"✅ DeductInventoryAsync completed. Affected rows: {affectedRows} for {cartItem.ProductName}");
                    }
                    catch (Exception deductEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ DeductInventoryAsync threw exception: {deductEx.Message}");
                        System.Diagnostics.Debug.WriteLine($"❌ Deduct exception stack: {deductEx.StackTrace}");
                        throw;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ No deductions to process for {cartItem.ProductName}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ERROR deducting inventory for {cartItem.ProductName}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Inner exception: {ex.InnerException.Message}");
                }
                // Don't throw - we don't want to fail the transaction if inventory deduction fails
                // But we should log this prominently
            }
        }

        private async Task AddAutomaticCupAndStrawForSize(Dictionary<string, double> deductionsDict, string size, int quantity) // Add cup and straw deductions based on size
        {
            try
            {
                var database = new Models.Database();
                
                // Add appropriate cup based on size
                var normalizedSize = (size ?? string.Empty).Trim().ToLowerInvariant();
                string cupName = normalizedSize switch
                {
                    "small" => "Small Cup",
                    "medium" => "Medium Cup", 
                    "large" => "Large Cup",
                    _ => "Medium Cup" // Default to medium
                };

                var cupItem = await database.GetInventoryItemByNameCachedAsync(cupName);
                if (cupItem != null)
                {
                    // Accumulate cup amounts for the same item
                    if (deductionsDict.ContainsKey(cupName))
                    {
                        deductionsDict[cupName] += quantity;
                    }
                    else
                    {
                        deductionsDict[cupName] = quantity;
                    }
                }

                // Add straw (1 per item)
                var strawItem = await database.GetInventoryItemByNameCachedAsync("Straw");
                if (strawItem != null)
                {
                    // Accumulate straw amounts for the same item
                    if (deductionsDict.ContainsKey("Straw"))
                    {
                        deductionsDict["Straw"] += quantity;
                    }
                    else
                    {
                        deductionsDict["Straw"] = quantity;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding automatic cup and straw: {ex.Message}");
            }
        }

        private static double ConvertUnits(double amount, string fromUnit, string toUnit) // Convert amount from one unit to another
        {
            if (string.IsNullOrWhiteSpace(toUnit)) return amount;
            
            // Use UnitConversionService for consistent unit conversion
            return UnitConversionService.Convert(amount, fromUnit, toUnit);
        }

        private static string NormalizeUnit(string unit) // Normalize unit strings to standard short forms
        {
            if (string.IsNullOrWhiteSpace(unit)) return string.Empty;

            // Use UnitConversionService for consistent normalization
            return UnitConversionService.Normalize(unit);
        }

        private static bool IsMassUnit(string unit) // Check if unit is a mass unit
        {
            var normalized = NormalizeUnit(unit);
            return normalized == "kg" || normalized == "g";
        }

        private static bool IsVolumeUnit(string unit) // Check if unit is a volume unit
        {
            var normalized = NormalizeUnit(unit);
            return normalized == "L" || normalized == "ml";
        }

        private static bool IsCountUnit(string unit) // Check if unit is a count/pieces unit
        {
            var normalized = NormalizeUnit(unit);
            return normalized == "pcs" || normalized == "pc";
        }

        private async Task<InventoryValidationResult> ValidateInventoryAvailabilityAsync() // Validate if inventory is sufficient for cart items
        {
            try
            {
                var database = new Models.Database();
                var issues = new List<string>();

                foreach (var item in CartItems)
                {
                    // Get product by name to get the product ID
                var product = await database.GetProductByNameAsyncCached(item.ProductName);
                    if (product == null)
                    {
                        issues.Add($"Product not found: {item.ProductName}");
                        continue;
                    }

                    // Get all ingredients for this product
                    var ingredients = await database.GetProductIngredientsAsync(product.ProductID);
                    
                    // Calculate total quantity across all sizes
                    int totalQuantity = (item.SmallQuantity > 0 ? item.SmallQuantity : 0)
                        + (item.MediumQuantity > 0 ? item.MediumQuantity : 0)
                        + (item.LargeQuantity > 0 ? item.LargeQuantity : 0);
                    if (totalQuantity <= 0)
                    {
                        issues.Add($"Invalid quantity for {item.ProductName}: {totalQuantity}");
                        continue;
                    }

                    // If product has no ingredients, skip ingredient validation (product is always available)
                    if (ingredients == null || !ingredients.Any())
                    {
                        System.Diagnostics.Debug.WriteLine($"Product {item.ProductName}: No ingredients, skipping ingredient validation");
                        // Still check cups/straws below
                    }
                    else
                    {
                        // Check each ingredient
                        foreach (var (ingredient, amount, unit, role) in ingredients)
                    {
                        // Convert units if needed
                        var convertedAmount = ConvertUnits(amount, unit, ingredient.unitOfMeasurement);
                        
                        // Calculate required amount
                        var requiredAmount = convertedAmount * totalQuantity;
                        
                        // Check if we have enough inventory
                        if (ingredient.itemQuantity < requiredAmount)
                        {
                            var shortage = requiredAmount - ingredient.itemQuantity;
                            issues.Add($"Insufficient {ingredient.itemName}: Need {requiredAmount:F2} {ingredient.unitOfMeasurement}, have {ingredient.itemQuantity:F2} (short by {shortage:F2})");
                        }
                    }

                    // Check automatic cup and straw per size
                    if (item.SmallQuantity > 0)
                        await ValidateAutomaticCupAndStrawForSize(database, issues, "Small", item.SmallQuantity);
                    if (item.MediumQuantity > 0)
                        await ValidateAutomaticCupAndStrawForSize(database, issues, "Medium", item.MediumQuantity);
                    if (item.LargeQuantity > 0)
                        await ValidateAutomaticCupAndStrawForSize(database, issues, "Large", item.LargeQuantity);
                }
                }

                return new InventoryValidationResult
                {
                    IsValid = !issues.Any(),
                    ErrorMessage = issues.Any() ? string.Join("\n", issues) : ""
                };
            }
            catch (Exception ex)
            {
                return new InventoryValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Error validating inventory: {ex.Message}"
                };
            }
        }

        private async Task ValidateAutomaticCupAndStrawForSize(Models.Database database, List<string> issues, string size, int quantity) // Validate cup and straw availability based on size
        {
            try
            {
                // Check appropriate cup based on size
                var normalizedSize2 = (size ?? string.Empty).Trim().ToLowerInvariant();
                string cupName = normalizedSize2 switch
                {
                    "small" => "Small Cup",
                    "medium" => "Medium Cup", 
                    "large" => "Large Cup",
                    _ => "Medium Cup" // Default to medium
                };

                var cupItem = await database.GetInventoryItemByNameCachedAsync(cupName);
                if (cupItem != null && cupItem.itemQuantity < quantity)
                {
                    var shortage = quantity - cupItem.itemQuantity;
                    issues.Add($"Insufficient {cupName}: Need {quantity}, have {cupItem.itemQuantity} (short by {shortage})");
                }

                // Check straw (1 per item)
                var strawItem = await database.GetInventoryItemByNameCachedAsync("Straw");
                if (strawItem != null && strawItem.itemQuantity < quantity)
                {
                    var shortage = quantity - strawItem.itemQuantity;
                    issues.Add($"Insufficient Straw: Need {quantity}, have {strawItem.itemQuantity} (short by {shortage})");
                }
            }
            catch (Exception ex)
            {
                issues.Add($"Error validating cup and straw: {ex.Message}");
            }
        }
    }

    public class InventoryValidationResult // Result of inventory validation
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = "";
    }
}
