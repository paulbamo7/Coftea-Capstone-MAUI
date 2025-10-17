using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Controls;

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

        private CancellationTokenSource _hideCancellationTokenSource;

        public async Task ShowOrderConfirmationAsync(string orderId, decimal total, string paymentMethod)
        {
            // Cancel any existing hide operation
            _hideCancellationTokenSource?.Cancel();

            // Set the data
            OrderDetails = orderId;
            TotalAmount = total;
            PaymentMethod = paymentMethod;

            // Show the popup with animation
            IsVisible = true;
            await Task.Delay(50); // Small delay to ensure UI is ready

            // No animation
            Opacity = 1;

            // Auto-hide after 3 seconds
            _hideCancellationTokenSource = new CancellationTokenSource();
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(3000, _hideCancellationTokenSource.Token);
                    
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

        private async Task AnimateOpacity(double from, double to, uint duration)
        {
            // No animation
            Opacity = to;
        }

        public void HideImmediately()
        {
            _hideCancellationTokenSource?.Cancel();
            IsVisible = false;
            Opacity = 0;
        }
    }
}
