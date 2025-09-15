using Coftea_Capstone.C_;
using Coftea_Capstone.Models;
using Coftea_Capstone.Services;
using Coftea_Capstone.Views.Pages;
using Coftea_Capstone.ViewModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.Threading.Tasks;

namespace Coftea_Capstone
{
    public partial class App : Application
    {

        public static UserInfoModel CurrentUser { get; private set; }

        // Shared ViewModels
        public AddItemToPOSViewModel AddItemPopup { get; private set; }
        public SettingsPopUpViewModel SettingsPopup { get; private set; }
        public POSPageViewModel POSVM { get; private set; }

        public App()
        {
            InitializeComponent();

            InitializeViewModels();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                bool isLoggedIn = Preferences.Get("IsLoggedIn", false);
                bool isAdmin = Preferences.Get("IsAdmin", false);

                if (isLoggedIn)
                {
                    NavigateToDashboard(isAdmin);
                }
                else
                {
                    MainPage = new NavigationPage(new LoginPage());
                }
            });

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        private void InitializeViewModels()
        {
            AddItemPopup = new AddItemToPOSViewModel();
            SettingsPopup = new SettingsPopUpViewModel(AddItemPopup);
            POSVM = new POSPageViewModel(AddItemPopup, SettingsPopup);
        }

        private void NavigateToDashboard(bool isAdmin)
        {
            if (isAdmin)
                MainPage = new NavigationPage(new AdminDashboard());
            else
                MainPage = new NavigationPage(new EmployeeDashboard());
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
                HandleException(ex);
        }

        private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            HandleException(e.Exception);
            e.SetObserved();
        }

        private void HandleException(Exception ex)
        {
            string message = !NetworkService.HasInternetConnection()
                ? "No internet connection. Please check your network."
                : $"Unexpected error: {ex.Message}";

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await App.Current.MainPage.DisplayAlert("Error", message, "OK");
            });
        }

        public static void SetCurrentUser(UserInfoModel user)
        {
            CurrentUser = user;
        }

        // Called after logout to reset everything
        public void ResetAppAfterLogout()
        {
            SetCurrentUser(null);
            Preferences.Set("IsLoggedIn", false);
            Preferences.Set("IsAdmin", false);
            Preferences.Remove("Email");
            Preferences.Remove("Password");
            Preferences.Remove("RememberMe");

            InitializeViewModels(); // reset all viewmodels

            // Create a new NavigationPage and set it as MainPage
            MainPage = new NavigationPage(new LoginPage());
        }

    }
}
