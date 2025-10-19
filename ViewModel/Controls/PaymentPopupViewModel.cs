using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Coftea_Capstone.Models;
using Coftea_Capstone.Services;

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
            // For non-cash methods (GCash/Bank), auto-approve regardless of AmountPaid
            var isNonCash = IsGCashSelected || IsBankSelected;
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

            // Process payment with realistic steps
            await ProcessPaymentWithSteps();

            // Save transaction to database and shared store
            var savedTransactions = await SaveTransaction();

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
                
                // Try direct access to POS page
                if (currentApp?.MainPage is NavigationPage navPage)
                {
                    System.Diagnostics.Debug.WriteLine($"NavigationPage found, CurrentPage: {navPage.CurrentPage?.GetType().Name}");
                    if (navPage.CurrentPage is Coftea_Capstone.Views.Pages.PointOfSale posPage)
                    {
                        if (posPage.BindingContext is POSPageViewModel posViewModel)
                        {
                            await posViewModel.ClearCartAsync();
                            System.Diagnostics.Debug.WriteLine("✅ Cart cleared in POS page ViewModel via NavigationPage");
                        }
                    }
                }
                // Try TabbedPage access
                else if (currentApp?.MainPage is TabbedPage tabbedPage)
                {
                    System.Diagnostics.Debug.WriteLine("TabbedPage found, searching for POS page...");
                    foreach (var page in tabbedPage.Children)
                    {
                        if (page is Coftea_Capstone.Views.Pages.PointOfSale posPage)
                        {
                            if (posPage.BindingContext is POSPageViewModel posViewModel)
                            {
                                await posViewModel.ClearCartAsync();
                                System.Diagnostics.Debug.WriteLine("✅ Cart cleared in POS page ViewModel via TabbedPage");
                                break;
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
            if (appInstance?.OrderCompletePopup != null)
            {
                var orderNumber = new Random().Next(1000, 9999).ToString();
                await appInstance.OrderCompletePopup.ShowOrderCompleteAsync(orderNumber);
            }
            
            // Show order confirmation popup in bottom right
            var orderConfirmedPopup = appInstance?.OrderConfirmedPopup;
            if (orderConfirmedPopup != null)
            {
                var orderNumber = new Random().Next(1000, 9999).ToString();
                await orderConfirmedPopup.ShowOrderConfirmationAsync(orderNumber, TotalAmount, SelectedPaymentMethod);
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

            // Add to recent orders
            var app = (App)Application.Current;
            var settingsPopup = app?.SettingsPopup;
            if (settingsPopup != null && CartItems.Any())
            {
                var firstItem = CartItems.First();
                var orderNumber = new Random().Next(25, 100); // Generate random order number
                settingsPopup.AddRecentOrder(
                    orderNumber,
                    firstItem.ProductName,
                    firstItem.ImageSource,
                    TotalAmount
                );
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
                    int nextId = (transactions.Count > 0 ? transactions.Max(t => t.TransactionId) : 0) + 1;
                    var saved = new List<TransactionHistoryModel>();

                    foreach (var item in CartItems)
                    {
                        System.Diagnostics.Debug.WriteLine($"💾 Saving transaction for: {item.ProductName}");
                        
                        // Calculate addon price from the difference between total price and base product price
                        var baseProductPrice = (item.SmallPrice * item.SmallQuantity) + (item.MediumPrice * item.MediumQuantity) + (item.LargePrice * item.LargeQuantity);
                        var addonPrice = Math.Max(0, item.TotalPrice - baseProductPrice);
                        System.Diagnostics.Debug.WriteLine($"Transaction: {item.ProductName}, Base Price: {baseProductPrice}, Total Price: {item.TotalPrice}, Addon Price: {addonPrice}");
                        
                        System.Diagnostics.Debug.WriteLine($"💾 Creating transaction for: {item.ProductName}");
                        System.Diagnostics.Debug.WriteLine($"💾 AddOnsDisplay: '{item.AddOnsDisplay}'");
                        System.Diagnostics.Debug.WriteLine($"💾 AddOns collection count: {item.AddOns?.Count ?? 0}");
                        if (item.AddOns != null)
                        {
                            foreach (var addon in item.AddOns)
                            {
                                System.Diagnostics.Debug.WriteLine($"💾 Addon in collection: '{addon}'");
                            }
                        }
                        
                        var transaction = new TransactionHistoryModel
                        {
                            TransactionId = nextId++,
                            DrinkName = item.ProductName,
                            Size = item.SelectedSize,
                            Quantity = item.Quantity,
                            Price = item.Price,
                            SmallPrice = item.SmallPrice, // Store unit prices, not total prices
                            MediumPrice = item.MediumPrice, // Store unit prices, not total prices
                            LargePrice = item.LargePrice, // Store unit prices, not total prices
                            AddonPrice = addonPrice,
                            Vat = 0m,
                            Total = item.TotalPrice,
                            AddOns = string.IsNullOrWhiteSpace(item.AddOnsDisplay) || item.AddOnsDisplay == "No add-ons" ? "" : item.AddOnsDisplay,
                            CustomerName = item.CustomerName,
                            PaymentMethod = SelectedPaymentMethod,
                            Status = "Completed",
                            TransactionDate = DateTime.Now
                        };
                        
                        System.Diagnostics.Debug.WriteLine($"💾 Transaction AddOns: '{transaction.AddOns}'");

                        // Save to database with timeout
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                        await database.SaveTransactionAsync(transaction);
                        System.Diagnostics.Debug.WriteLine($"✅ Database save successful for: {item.ProductName}");
                        
                        // Deduct inventory for this item
                        await DeductInventoryForItemAsync(database, item);
                        System.Diagnostics.Debug.WriteLine($"✅ Inventory deduction successful for: {item.ProductName}");
                        
                        // Also add to in-memory collection for history popup
                        transactions.Add(transaction);
                        saved.Add(transaction);
                    }

                    System.Diagnostics.Debug.WriteLine($"✅ All {saved.Count} transactions saved successfully");
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
                // Get product by name to get the product ID
                var product = await database.GetProductByNameAsyncCached(cartItem.ProductName);
                if (product == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Product not found: {cartItem.ProductName}");
                    return;
                }

                // Get all ingredients for this product
                var ingredients = await database.GetProductIngredientsAsync(product.ProductID);
                if (!ingredients.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"No ingredients found for product: {cartItem.ProductName}");
                    return;
                }

                // Prepare deductions list (per size)
                var deductions = new List<(string name, double amount)>();

                // Helper local function to add deductions for a specific size using per-size amounts
                void AddSizeDeductions(string size, int qty)
                {
                    if (qty <= 0) return;
                    foreach (var (ingredient, amount, unit, role) in ingredients)
                    {
                        // Choose per-size amount/unit when available; fall back to shared
                        double perServing = size switch
                        {
                            "Small" => ingredient.InputAmountSmall > 0 ? ingredient.InputAmountSmall : amount,
                            "Medium" => ingredient.InputAmountMedium > 0 ? ingredient.InputAmountMedium : amount,
                            "Large" => ingredient.InputAmountLarge > 0 ? ingredient.InputAmountLarge : amount,
                            _ => amount
                        };
                        string perUnit = size switch
                        {
                            "Small" => !string.IsNullOrWhiteSpace(ingredient.InputUnitSmall) ? ingredient.InputUnitSmall : unit,
                            "Medium" => !string.IsNullOrWhiteSpace(ingredient.InputUnitMedium) ? ingredient.InputUnitMedium : unit,
                            "Large" => !string.IsNullOrWhiteSpace(ingredient.InputUnitLarge) ? ingredient.InputUnitLarge : unit,
                            _ => unit
                        } ?? ingredient.unitOfMeasurement;

                        var converted = ConvertUnits(perServing, perUnit, ingredient.unitOfMeasurement);
                        var total = converted * qty;
                        deductions.Add((ingredient.itemName, total));
                    }
                }

                // Add per-size deductions based on cart quantities
                AddSizeDeductions("Small", cartItem.SmallQuantity);
                AddSizeDeductions("Medium", cartItem.MediumQuantity);
                AddSizeDeductions("Large", cartItem.LargeQuantity);

                // Include selected addons based on configured per-serving amounts
                // Fetch addon link definitions (amount/unit per serving)
                var addonLinks = await database.GetProductAddonsAsync(product.ProductID);
                if (addonLinks != null && addonLinks.Count > 0 && cartItem.InventoryItems != null)
                {
                    foreach (var linkedAddon in addonLinks)
                    {
                        // Find this addon in the cart with user-selected quantity
                        var cartAddon = cartItem.InventoryItems.FirstOrDefault(ai => ai.itemID == linkedAddon.itemID);
                        if (cartAddon == null) continue;
                        if (!cartAddon.IsSelected || cartAddon.AddonQuantity <= 0) continue;

                        // Use shared per-serving amount/unit for addons (same across sizes), fallback to inventory unit
                        var perServingAmount = linkedAddon.InputAmount > 0
                            ? linkedAddon.InputAmount
                            : (linkedAddon.InputAmountSmall > 0 ? linkedAddon.InputAmountSmall : 0);
                        var perServingUnit = !string.IsNullOrWhiteSpace(linkedAddon.unitOfMeasurement)
                            ? linkedAddon.unitOfMeasurement
                            : (cartAddon.unitOfMeasurement ?? string.Empty);

                        if (perServingAmount <= 0)
                            continue; // nothing to deduct for this addon

                        var convertedAddonAmount = ConvertUnits(perServingAmount, perServingUnit, cartAddon.unitOfMeasurement);

                        // Deduct addon based on total quantity ordered across all sizes
                        // AddonQuantity is per drink, so multiply by total drinks ordered
                        var totalDrinks = cartItem.SmallQuantity + cartItem.MediumQuantity + cartItem.LargeQuantity;
                        if (totalDrinks > 0)
                        {
                            var totalAddonDeduction = convertedAddonAmount * cartAddon.AddonQuantity * (double)totalDrinks;
                            deductions.Add((cartAddon.itemName, totalAddonDeduction));
                        }
                    }
                }

                // Add automatic cups and straws per size (1 per serving)
                if (cartItem.SmallQuantity > 0)
                    await AddAutomaticCupAndStrawForSize(deductions, "Small", cartItem.SmallQuantity);
                if (cartItem.MediumQuantity > 0)
                    await AddAutomaticCupAndStrawForSize(deductions, "Medium", cartItem.MediumQuantity);
                if (cartItem.LargeQuantity > 0)
                    await AddAutomaticCupAndStrawForSize(deductions, "Large", cartItem.LargeQuantity);

                // Deduct inventory
                if (deductions.Any())
                {
                    var affectedRows = await database.DeductInventoryAsync(deductions);
                    System.Diagnostics.Debug.WriteLine($"Deducted inventory for {affectedRows} items for {cartItem.ProductName}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deducting inventory for {cartItem.ProductName}: {ex.Message}");
                // Don't throw - we don't want to fail the transaction if inventory deduction fails
            }
        }

        private async Task AddAutomaticCupAndStrawForSize(List<(string name, double amount)> deductions, string size, int quantity) // Add cup and straw deductions based on size
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
                    deductions.Add((cupName, quantity));
                }

                // Add straw (1 per item)
                var strawItem = await database.GetInventoryItemByNameCachedAsync("Straw");
                if (strawItem != null)
                {
                    deductions.Add(("Straw", quantity));
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
            
            // Normalize units to short form
            fromUnit = NormalizeUnit(fromUnit);
            toUnit = NormalizeUnit(toUnit);

            // If units are the same, no conversion needed
            if (string.Equals(fromUnit, toUnit, StringComparison.OrdinalIgnoreCase))
            {
                return amount;
            }

            // Mass conversions
            if (IsMassUnit(fromUnit) && IsMassUnit(toUnit))
            {
                return fromUnit.ToLowerInvariant() switch
                {
                    "kg" when toUnit.ToLowerInvariant() == "g" => amount * 1000,
                    "g" when toUnit.ToLowerInvariant() == "kg" => amount / 1000,
                    _ => amount
                };
            }

            // Volume conversions
            if (IsVolumeUnit(fromUnit) && IsVolumeUnit(toUnit))
            {
                return fromUnit.ToLowerInvariant() switch
                {
                    "l" when toUnit.ToLowerInvariant() == "ml" => amount * 1000,
                    "ml" when toUnit.ToLowerInvariant() == "l" => amount / 1000,
                    _ => amount
                };
            }

            // Count
            if (IsCountUnit(fromUnit) && IsCountUnit(toUnit))
            {
                return amount;
            }

            // If from is empty, assume already in inventory unit
            if (string.IsNullOrWhiteSpace(fromUnit)) return amount;

            // Unknown or mismatched units → block deduction by returning 0
            return 0;
        }

        private static string NormalizeUnit(string unit) // Normalize unit strings to standard short forms
        {
            if (string.IsNullOrWhiteSpace(unit)) return string.Empty;

            var u = unit.Trim().ToLowerInvariant();
            // Strip decorations like "liters (l)" → "liters l"
            u = u.Replace("(", " ").Replace(")", " ").Replace(".", " ");
            // Collapse multiple spaces
            u = System.Text.RegularExpressions.Regex.Replace(u, "\\s+", " ").Trim();

            // Prefer specific tokens first - check longer units before shorter ones
            if (u.Contains("ml")) return "ml";
            if (u.Contains(" l" ) || u.StartsWith("l") || u.Contains("liter")) return "l";
            if (u.Contains("kg") || u.Contains("kilogram")) return "kg";
            // Ensure 'g' check after 'kg' and exclude 'kilogram' by checking it's not preceded by 'kilo'
            if (u.Contains(" g") || u == "g" || (u.Contains("gram") && !u.Contains("kilogram"))) return "g";
            if (u.Contains("pcs") || u.Contains("piece")) return "pcs";

            return u;
        }

        private static bool IsMassUnit(string unit) // Check if unit is a mass unit
        {
            var normalized = NormalizeUnit(unit);
            return normalized == "kg" || normalized == "g";
        }

        private static bool IsVolumeUnit(string unit) // Check if unit is a volume unit
        {
            var normalized = NormalizeUnit(unit);
            return normalized == "l" || normalized == "ml";
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
