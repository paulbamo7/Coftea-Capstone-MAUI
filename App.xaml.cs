using Coftea_Capstone.Pages;

namespace Coftea_Capstone
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            var NavigatePage = new NavigationPage(new LoginPage());
            MainPage = NavigatePage;
        }
    }
}
