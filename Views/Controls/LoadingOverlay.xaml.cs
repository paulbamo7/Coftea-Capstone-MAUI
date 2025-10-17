using Microsoft.Maui.Controls;

namespace Coftea_Capstone.Views.Controls
{
    public partial class LoadingOverlay : ContentView
    {
        private bool _isAnimating = false;

        public LoadingOverlay()
        {
            InitializeComponent();
        }

        public async Task ShowAsync()
        {
            IsVisible = true;
            Opacity = 1;
            StartLogoAnimation();
        }

        public async Task HideAsync()
        {
            StopLogoAnimation();
            IsVisible = false;
        }

        private void StartLogoAnimation()
        {
            if (_isAnimating) return;
            _isAnimating = true;
            AnimateLogo();
        }

        private void StopLogoAnimation()
        {
            _isAnimating = false;
        }

        private async void AnimateLogo()
        {
            while (_isAnimating)
            {
                // No rotation animation
                await Task.Delay(2000);
            }
        }
    }
}
