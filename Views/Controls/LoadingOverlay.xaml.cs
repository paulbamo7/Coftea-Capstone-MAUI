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
            Opacity = 0;
            await this.FadeTo(1, 300, Easing.CubicOut);
            StartLogoAnimation();
        }

        public async Task HideAsync()
        {
            StopLogoAnimation();
            await this.FadeTo(0, 300, Easing.CubicIn);
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
                await LogoImage.RotateTo(360, 2000, Easing.Linear);
                LogoImage.Rotation = 0;
            }
        }
    }
}
