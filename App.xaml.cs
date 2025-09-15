using Coftea_Capstone.C_;
using Coftea_Capstone.Models;
using Coftea_Capstone.Services;
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
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

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
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
                HandleException(ex);
        }

        private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            HandleException(e.Exception);
            e.SetObserved(); // Prevent app crash
        }

        private void HandleException(Exception ex)
        {
            string message;

            if (!NetworkService.HasInternetConnection())
                message = "No internet connection. Please check your network.";
            else
                message = $"Unexpected error: {ex.Message}";

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await App.Current.MainPage.DisplayAlert("Error", message, "OK");
            });
        }
        public static void SetCurrentUser(UserInfoModel user)
        {
            CurrentUser = user;
        }
    }
}