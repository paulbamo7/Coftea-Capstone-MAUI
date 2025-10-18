using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Coftea_Capstone.Views.Controls
{
    public partial class LoadingOverlay : ContentView
    {
        private bool _isAnimating = false;
        private ContentPage _modalPage;

        public LoadingOverlay()
        {
            InitializeComponent();
        }

        public async Task ShowAsync()
        {
            try
            {
                // Create a modal page with the loading overlay (logo only, no text)
                _modalPage = new ContentPage
                {
                    Content = new Grid
                    {
                        BackgroundColor = Color.FromArgb("#C1A892"),
                        Children =
                        {
                            new Image
                            {
                                Source = "coftea_logo.png",
                                WidthRequest = 120,
                                HeightRequest = 120,
                                HorizontalOptions = LayoutOptions.Center,
                                VerticalOptions = LayoutOptions.Center
                            }
                        }
                    },
                    BackgroundColor = Color.FromArgb("#C1A892")
                };
                
                // Push as modal
                await Application.Current.MainPage.Navigation.PushModalAsync(_modalPage, false);
                
                IsVisible = true;
                Opacity = 1;
                StartLogoAnimation();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing loading overlay: {ex.Message}");
            }
        }

        public async Task HideAsync()
        {
            try
            {
                StopLogoAnimation();
                IsVisible = false;
                
                // Pop modal
                if (_modalPage != null)
                {
                    await Application.Current.MainPage.Navigation.PopModalAsync(false);
                    _modalPage = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error hiding loading overlay: {ex.Message}");
            }
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
