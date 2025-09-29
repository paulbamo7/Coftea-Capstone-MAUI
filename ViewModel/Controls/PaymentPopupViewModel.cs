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
            appInstance?.NotificationPopup?.ShowToast($"Payment confirmed! Change: ₱{Change:F2}", 1500);

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
                var database = new Models.Database(host: "0.0.0.0", database: "coftea_db", user: "root", password: "");

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
    }
}
