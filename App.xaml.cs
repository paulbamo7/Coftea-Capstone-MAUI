using Coftea_Capstone.C_;
using Coftea_Capstone.Models;
using Coftea_Capstone.Services;
using Coftea_Capstone.Views.Pages;
using Coftea_Capstone.ViewModel;
using Coftea_Capstone.ViewModel.Controls;
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
        public ManagePOSOptionsViewModel ManagePOSPopup { get; private set; }
        public ManageInventoryOptionsViewModel ManageInventoryPopup { get; private set; }
        public AddItemToInventoryViewModel AddItemToInventoryPopup { get; private set; }
        public RetryConnectionPopupViewModel RetryConnectionPopup { get; private set; }
        public NotificationPopupViewModel NotificationPopup { get; private set; }
        public PasswordResetPopupViewModel PasswordResetPopup { get; private set; }

        public App()
        {
            InitializeComponent();

            InitializeViewModels();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                bool isLoggedIn = Preferences.Get("IsLoggedIn", false);
                bool rememberMe = Preferences.Get("RememberMe", false);
                bool isAdmin = Preferences.Get("IsAdmin", false);

                // Only auto-login if both IsLoggedIn and RememberMe are true
                if (isLoggedIn && rememberMe)
                {
                    // Hydrate a minimal CurrentUser so navigation works after auto-login
                    if (CurrentUser == null)
                    {
                        SetCurrentUser(new UserInfoModel
                        {
                            IsAdmin = isAdmin,
                            Email = Preferences.Get("Email", string.Empty)
                        });
                    }
                    NavigateToDashboard(isAdmin);
                }
                else
                {
                    // Clear login state if Remember Me is not checked
                    if (isLoggedIn && !rememberMe)
                    {
                        Preferences.Set("IsLoggedIn", false);
                        Preferences.Set("IsAdmin", false);
                    }
                    MainPage = new NavigationPage(new LoginPage());
                }
            });

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        private void InitializeViewModels()
        {
            AddItemPopup = new AddItemToPOSViewModel();
            var editProductPopup = new EditProductPopupViewModel(AddItemPopup);
            ManagePOSPopup = new ManagePOSOptionsViewModel(AddItemPopup, editProductPopup);
            AddItemToInventoryPopup = new AddItemToInventoryViewModel();
            ManageInventoryPopup = new ManageInventoryOptionsViewModel(AddItemToInventoryPopup);
            SettingsPopup = new SettingsPopUpViewModel(AddItemPopup, ManagePOSPopup, ManageInventoryPopup);
            POSVM = new POSPageViewModel(AddItemPopup, SettingsPopup);
            RetryConnectionPopup = new RetryConnectionPopupViewModel();
            NotificationPopup = new NotificationPopupViewModel();
            PasswordResetPopup = new PasswordResetPopupViewModel();
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
