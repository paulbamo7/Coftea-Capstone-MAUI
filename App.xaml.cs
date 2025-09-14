using Coftea_Capstone.C_;
using Coftea_Capstone.Models;
using Coftea_Capstone.Views.Pages;
using Coftea_Capstone.ViewModel;

namespace Coftea_Capstone
{
    public partial class App : Application
    {
        public static UserInfoModel CurrentUser { get; private set; }
        public SettingsPopUpViewModel SettingsPopup { get; private set; }
        public AddItemToPOSViewModel AddItemPopup { get; private set; }
        public App()
        {
            InitializeComponent();

            /*SettingsPopup = new SettingsPopUpViewModel();*/
            AddItemPopup = new AddItemToPOSViewModel();

            // Start at login page wrapped in NavigationPage
            bool isLoggedIn = Preferences.Get("IsLoggedIn", false);
            bool isAdmin = Preferences.Get("IsAdmin", false);

            if (isLoggedIn)
            {
                if (isAdmin)
                    MainPage = new NavigationPage(new AdminDashboard());
                else
                    MainPage = new NavigationPage(new EmployeeDashboard());
            }
            else
            {
                MainPage = new NavigationPage(new LoginPage());
            }   
        }

        public static void SetCurrentUser(UserInfoModel user)
        {
            CurrentUser = user;
        }
    }
}