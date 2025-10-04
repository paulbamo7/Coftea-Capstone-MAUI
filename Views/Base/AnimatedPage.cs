using Microsoft.Maui.Controls;

namespace Coftea_Capstone.Views.Base
{
    public class AnimatedPage : ContentPage
    {
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            // Add a subtle fade-in animation when the page appears
            this.Opacity = 0;
            await this.FadeTo(1, 300, Easing.CubicOut);
        }
    }
}
