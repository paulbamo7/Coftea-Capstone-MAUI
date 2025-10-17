using Microsoft.Maui.Controls;

namespace Coftea_Capstone.Views.Base
{
    public class AnimatedPage : ContentPage
    {
        protected override void OnAppearing()
        {
            base.OnAppearing();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            try { Handler?.DisconnectHandler(); } catch { }
        }
    }
}
