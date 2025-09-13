using Coftea_Capstone.C_;
using Coftea_Capstone.Models;
using Coftea_Capstone.Views.Pages;

namespace Coftea_Capstone
{
    public partial class App : Application
    {
        public static UserInfoModel CurrentUser { get; private set; }

        public App()
        {
            InitializeComponent();

            // Start at login page wrapped in NavigationPage
            var NavigatePage = new NavigationPage(new LoginPage());
            MainPage = NavigatePage;
        }

        public static void SetCurrentUser(UserInfoModel user)
        {
            CurrentUser = user;
        }
    }
}