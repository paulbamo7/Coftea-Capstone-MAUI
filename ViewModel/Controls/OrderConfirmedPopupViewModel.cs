using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using Coftea_Capstone.Models;

namespace Coftea_Capstone.ViewModel.Controls
{
    public partial class OrderConfirmedPopupViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool isVisible = false;

        [ObservableProperty]
        private double opacity = 0;

        [ObservableProperty]
        private string orderDetails = "";

        [ObservableProperty]
        private decimal totalAmount = 0;

        [ObservableProperty]
        private string paymentMethod = "";

        [ObservableProperty]
        private ObservableCollection<PaymentBreakdownItem> paymentBreakdown = new();

        public bool HasMultiplePaymentMethods => PaymentBreakdown.Count > 1;
        public bool HasSinglePaymentMethod => PaymentBreakdown.Count <= 1;

        private CancellationTokenSource _hideCancellationTokenSource;

        public async Task ShowOrderConfirmationAsync(string orderId, decimal total, string paymentMethod, List<CartItem> cartItems = null) // Show order confirmed popup
        {
            // Cancel any existing hide operation
            _hideCancellationTokenSource?.Cancel();

            // Set the data
            OrderDetails = orderId;
            TotalAmount = total;
            PaymentMethod = paymentMethod;

            // Build payment breakdown from cart items
            PaymentBreakdown.Clear();
            if (cartItems != null && cartItems.Count > 0)
            {
                foreach (var item in cartItems)
                {
                    PaymentBreakdown.Add(new PaymentBreakdownItem
                    {
                        ProductName = item.ProductName,
                        PaymentMethod = string.IsNullOrEmpty(item.PaymentMethod) ? paymentMethod : item.PaymentMethod,
                        Amount = item.Price,
                        Quantity = item.TotalQuantity
                    });
                }
            }

            // Notify property changes for visibility bindings
            OnPropertyChanged(nameof(HasMultiplePaymentMethods));
            OnPropertyChanged(nameof(HasSinglePaymentMethod));

            // Show the popup with animation
            IsVisible = true;
            await Task.Delay(50); // Small delay to ensure UI is ready

            // No animation
            Opacity = 1;

            // Auto-hide after 5 seconds (increased to give time to read breakdown)
            _hideCancellationTokenSource = new CancellationTokenSource();
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(5000, _hideCancellationTokenSource.Token);
                    
                    // No animation
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        IsVisible = false;
                    });
                }
                catch (OperationCanceledException)
                {
                    // Animation was cancelled, do nothing
                }
            });
        }
    }
}
