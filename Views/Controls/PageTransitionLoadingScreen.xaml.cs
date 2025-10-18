using Microsoft.Maui.Controls;

namespace Coftea_Capstone.Views.Controls
{
    public partial class PageTransitionLoadingScreen : ContentView
    {
        public PageTransitionLoadingScreen()
        {
            InitializeComponent();
        }

        public void Show()
        {
            IsVisible = true;
        }

        public void Hide()
        {
            IsVisible = false;
        }
    }
}
