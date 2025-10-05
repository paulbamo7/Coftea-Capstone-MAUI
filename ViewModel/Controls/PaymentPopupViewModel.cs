using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Coftea_Capstone.Models;

namespace Coftea_Capstone.ViewModel.Controls
{
    public partial class PaymentPopupViewModel : ObservableObject
    {
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

        public void ShowPayment(decimal total, List<CartItem> items)
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
        private void ClosePayment()
        {
            IsPaymentVisible = false;
        }

        public void UpdateAmountPaid(string amount)
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
        private async Task ConfirmPayment()
        {
            if (AmountPaid < TotalAmount)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Insufficient Payment",
                    $"Amount paid (₱{AmountPaid:F2}) is less than total (₱{TotalAmount:F2})",
                    "OK");
                return;
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
            bool confirm = await Application.Current.MainPage.DisplayAlert(
                "Confirm Payment",
                $"Total: ₱{TotalAmount:F2}\nPaid: ₱{AmountPaid:F2}\nChange: ₱{Change:F2}\n\nProceed with payment?",
                "Confirm",
                "Cancel");

            if (!confirm)
                return;

            // Process payment
            PaymentStatus = "Processing...";
            await Task.Delay(1000);

            // Save transaction to database and shared store
            await SaveTransaction();

            // Show success: full order-complete popup and a short toast
            PaymentStatus = "Payment Confirmed";
            var appInstance = (App)Application.Current;
            appInstance?.OrderCompletePopup?.Show();
            appInstance?.NotificationPopup?.ShowToast($"Transaction completed. Change: ₱{Change:F2}", 2000);

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

        private async Task SaveTransaction()
        {
            try
            {
                var app = (App)Application.Current;
                var transactions = app?.Transactions;
                var database = new Models.Database(); // Will use auto-detected host

                if (transactions != null)
                {
                    int nextId = (transactions.Count > 0 ? transactions.Max(t => t.TransactionId) : 0) + 1;

                    foreach (var item in CartItems)
                    {
                        var transaction = new TransactionHistoryModel
                        {
                            TransactionId = nextId++,
                            DrinkName = item.ProductName,
                            Size = item.SelectedSize,
                            Quantity = item.Quantity,
                            Price = item.Price,
                            Vat = 0m,
                            Total = item.TotalPrice,
                            AddOns = item.AddOnsDisplay,
                            CustomerName = item.CustomerName,
                            PaymentMethod = "Cash",
                            Status = "Completed",
                            TransactionDate = DateTime.Now
                        };

                        // Save to database
                        await database.SaveTransactionAsync(transaction);
                        
                        // Deduct inventory for this item
                        await DeductInventoryForItemAsync(database, item);
                        
                        // Also add to in-memory collection for history popup
                        transactions.Add(transaction);
                    }
                }
            }
            catch (System.Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    $"Failed to save transaction: {ex.Message}",
                    "OK");
            }
        }

        private async Task DeductInventoryForItemAsync(Models.Database database, CartItem cartItem)
        {
            try
            {
                // Get product by name to get the product ID
                var product = await database.GetProductByNameAsync(cartItem.ProductName);
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

                // Calculate total quantity across sizes
                int totalQuantity = (cartItem.SmallQuantity + cartItem.MediumQuantity + cartItem.LargeQuantity);
                if (totalQuantity <= 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Invalid quantity for {cartItem.ProductName}: {totalQuantity}");
                    return;
                }

                // Prepare deductions list
                var deductions = new List<(string name, double amount)>();

                foreach (var (ingredient, amount, unit, role) in ingredients)
                {
                    // Convert units if needed
                    var convertedAmount = ConvertUnits(amount, unit, ingredient.unitOfMeasurement);
                    
                    // Multiply by total quantity
                    var totalAmount = convertedAmount * totalQuantity;
                    
                    deductions.Add((ingredient.itemName, totalAmount));
                }

                // Add automatic cups by size and straw for total pieces
                await AddAutomaticCupsAndStrawAsync(deductions, cartItem.SmallQuantity, cartItem.MediumQuantity, cartItem.LargeQuantity, totalQuantity);

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

        private async Task AddAutomaticCupsAndStrawAsync(List<(string name, double amount)> deductions, int smallQty, int mediumQty, int largeQty, int totalQty)
        {
            try
            {
                var database = new Models.Database();
                // Add cups per size
                if (smallQty > 0)
                {
                    var smallCup = await database.GetInventoryItemByNameAsync("Small Cup");
                    if (smallCup != null) deductions.Add(("Small Cup", smallQty));
                }
                if (mediumQty > 0)
                {
                    var mediumCup = await database.GetInventoryItemByNameAsync("Medium Cup");
                    if (mediumCup != null) deductions.Add(("Medium Cup", mediumQty));
                }
                if (largeQty > 0)
                {
                    var largeCup = await database.GetInventoryItemByNameAsync("Large Cup");
                    if (largeCup != null) deductions.Add(("Large Cup", largeQty));
                }

                // Add straw (1 per item total)
                var strawItem = await database.GetInventoryItemByNameAsync("Straw");
                if (strawItem != null)
                {
                    deductions.Add(("Straw", totalQty));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding automatic cup and straw: {ex.Message}");
            }
        }

        private static double ConvertUnits(double amount, string fromUnit, string toUnit)
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

        private static string NormalizeUnit(string unit)
        {
            if (string.IsNullOrWhiteSpace(unit)) return string.Empty;
            
            return unit.ToLowerInvariant() switch
            {
                "kilograms" or "kilogram" or "kg" => "kg",
                "grams" or "gram" or "g" => "g",
                "liters" or "liter" or "l" or "l." => "l",
                "milliliters" or "milliliter" or "ml" or "ml." => "ml",
                "pieces" or "piece" or "pcs" or "pc" => "pcs",
                _ => unit.ToLowerInvariant()
            };
        }

        private static bool IsMassUnit(string unit)
        {
            var normalized = NormalizeUnit(unit);
            return normalized == "kg" || normalized == "g";
        }

        private static bool IsVolumeUnit(string unit)
        {
            var normalized = NormalizeUnit(unit);
            return normalized == "l" || normalized == "ml";
        }

        private static bool IsCountUnit(string unit)
        {
            var normalized = NormalizeUnit(unit);
            return normalized == "pcs" || normalized == "pc";
        }

        private async Task<InventoryValidationResult> ValidateInventoryAvailabilityAsync()
        {
            try
            {
                var database = new Models.Database();
                var issues = new List<string>();

                foreach (var item in CartItems)
                {
                    // Get product by name to get the product ID
                    var product = await database.GetProductByNameAsync(item.ProductName);
                    if (product == null)
                    {
                        issues.Add($"Product not found: {item.ProductName}");
                        continue;
                    }

                    // Get all ingredients for this product
                    var ingredients = await database.GetProductIngredientsAsync(product.ProductID);
                    if (!ingredients.Any())
                    {
                        issues.Add($"No ingredients configured for: {item.ProductName}");
                        continue;
                    }

                    // Calculate total quantity across sizes
                    int totalQuantity = (item.SmallQuantity + item.MediumQuantity + item.LargeQuantity);
                    if (totalQuantity <= 0)
                    {
                        issues.Add($"Invalid quantity for {item.ProductName}: {totalQuantity}");
                        continue;
                    }

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

                    // Check automatic cups per size and straw
                    await ValidateAutomaticCupsAndStrawAsync(database, issues, item.SmallQuantity, item.MediumQuantity, item.LargeQuantity, totalQuantity);
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

        private async Task ValidateAutomaticCupsAndStrawAsync(Models.Database database, List<string> issues, int smallQty, int mediumQty, int largeQty, int totalQty)
        {
            try
            {
                // Check cups by size
                var smallCup = await database.GetInventoryItemByNameAsync("Small Cup");
                if (smallQty > 0 && smallCup != null && smallCup.itemQuantity < smallQty)
                {
                    var shortage = smallQty - smallCup.itemQuantity;
                    issues.Add($"Insufficient Small Cup: Need {smallQty}, have {smallCup.itemQuantity} (short by {shortage})");
                }

                var mediumCup = await database.GetInventoryItemByNameAsync("Medium Cup");
                if (mediumQty > 0 && mediumCup != null && mediumCup.itemQuantity < mediumQty)
                {
                    var shortage = mediumQty - mediumCup.itemQuantity;
                    issues.Add($"Insufficient Medium Cup: Need {mediumQty}, have {mediumCup.itemQuantity} (short by {shortage})");
                }

                var largeCup = await database.GetInventoryItemByNameAsync("Large Cup");
                if (largeQty > 0 && largeCup != null && largeCup.itemQuantity < largeQty)
                {
                    var shortage = largeQty - largeCup.itemQuantity;
                    issues.Add($"Insufficient Large Cup: Need {largeQty}, have {largeCup.itemQuantity} (short by {shortage})");
                }

                // Check straw (1 per item total)
                var strawItem = await database.GetInventoryItemByNameAsync("Straw");
                if (strawItem != null && strawItem.itemQuantity < totalQty)
                {
                    var shortage = totalQty - strawItem.itemQuantity;
                    issues.Add($"Insufficient Straw: Need {totalQty}, have {strawItem.itemQuantity} (short by {shortage})");
                }
            }
            catch (Exception ex)
            {
                issues.Add($"Error validating cup and straw: {ex.Message}");
            }
        }
    }

    public class InventoryValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = "";
    }
}
