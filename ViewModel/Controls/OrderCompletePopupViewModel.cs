using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace Coftea_Capstone.ViewModel.Controls
{
    public partial class OrderCompletePopupViewModel : ObservableObject
    {
        [ObservableProperty] private bool isVisible;
        [ObservableProperty] private double opacity = 0;
        [ObservableProperty] private string orderDetails = "";

        private CancellationTokenSource _hideCancellationTokenSource;

        [RelayCommand]
        private void Close()
        {
            _hideCancellationTokenSource?.Cancel();
            IsVisible = false;
            Opacity = 0;
        }

        public async Task ShowOrderCompleteAsync(string orderId = null)
        {
            // Cancel any existing hide operation
            _hideCancellationTokenSource?.Cancel();

            // Set the order details
            OrderDetails = orderId ?? new Random().Next(1000, 9999).ToString();

            // Show the popup with animation
            IsVisible = true;
            await Task.Delay(50); // Small delay to ensure UI is ready

            // Fade in animation
            await AnimateOpacity(0, 1, 300);

            // Auto-hide after 3 seconds
            _hideCancellationTokenSource = new CancellationTokenSource();
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(3000, _hideCancellationTokenSource.Token);
                    
                    // Fade out animation
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await AnimateOpacity(1, 0, 300);
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
            var animation = new Animation(v => Opacity = v, from, to);
            // Use Application.Current.MainPage as the IAnimatable target
            await Task.Run(() => animation.Commit(Application.Current.MainPage, "OrderCompleteOpacityAnimation", 16, duration, Easing.CubicOut));
        }

        public void HideImmediately()
        {
            _hideCancellationTokenSource?.Cancel();
            IsVisible = false;
            Opacity = 0;
        }

        // Legacy method for backward compatibility
        public void Show()
        {
            _ = ShowOrderCompleteAsync();
        }
    }
}


