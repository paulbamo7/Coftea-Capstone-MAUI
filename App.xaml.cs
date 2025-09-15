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

        public App()
        {
            InitializeComponent();

            // Restore session
            bool isLoggedIn = Preferences.Get("IsLoggedIn", false);
            if (isLoggedIn)
            {
                string email = Preferences.Get("Email", string.Empty);
                bool isAdmin = Preferences.Get("IsAdmin", false);

                CurrentUser = new UserInfoModel
                {
                    Email = email,
                    IsAdmin = isAdmin
                };

                if (isAdmin)
                {
                    MainPage = new NavigationPage(new AdminDashboard());
                }
                else
                {
                    MainPage = new NavigationPage(new EmployeeDashboard());
                }
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

        protected override void OnStart()
        {
            Connectivity.ConnectivityChanged += (s, e) =>
            {
                if (e.NetworkAccess != NetworkAccess.Internet)
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await Application.Current.MainPage.DisplayAlert(
                            "Network Lost",
                            "You are offline. Some features may not work.",
                            "OK"
                        );
                    });
                }
            };
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

    }
}