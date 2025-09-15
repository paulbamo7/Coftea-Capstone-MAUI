using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Coftea_Capstone.Models;
using Coftea_Capstone.C_;
using Coftea_Capstone.Views.Pages;
using Microsoft.Maui.Controls;
using Coftea_Capstone.Services;
using System.Threading.Tasks;

namespace Coftea_Capstone.ViewModel
{
    public partial class LoginPageViewModel : ObservableObject
    {
        private readonly Database _database;

        // ────────────────────────────────
        // Observable Properties
        // ────────────────────────────────
        [ObservableProperty] private string email;
        [ObservableProperty] private string password;
        [ObservableProperty] private bool rememberMe;
        [ObservableProperty] private bool isAdmin;
        [ObservableProperty]
        private bool isRetryPopupVisible;



        // ────────────────────────────────
        // Constructor
        // ────────────────────────────────
        public LoginPageViewModel()
        {
            // MySQL connection (XAMPP)
            _database = new Database(
                host: "0.0.0.0",
                database: "coftea_db",
                user: "root",
                password: ""
            );

            // Load saved "Remember Me" preferences
            bool savedRememberMe = Preferences.Get("RememberMe", false);
            if (savedRememberMe)
            {
                Email = Preferences.Get("Email", string.Empty);
                Password = Preferences.Get("Password", string.Empty);
                RememberMe = true;
            }
            else
            {
                RememberMe = false;
            }
        }

        [RelayCommand]
        private async Task Login()
        {
            if (!await NetworkService.EnsureInternetAsync())
            {
                return;
            }

            // Check internet connection
            if (!NetworkService.HasInternetConnection())
            {
                ShowRetryPopupCommand.Execute(null);
                return;
            }

            // Validate fields
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    "Please enter username and password",
                    "OK"
                );
                ClearEntries();
                return;
            }

            try
            {
                // ────────────────────────────────
                // Fetch user (may throw MySqlException)
                // ────────────────────────────────
                var user = await _database.GetUserByEmailAsync(Email);

                if (user == null)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Error",
                        "User not found",
                        "OK"
                    );
                    ClearEntries();
                    return;
                }

                if (user.Password != Password)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Error",
                        "Incorrect password",
                        "OK"
                    );
                    ClearEntries();
                    return;
                }

                // Save current user in App state
                App.SetCurrentUser(user);

                // Save preferences
                if (RememberMe)
                {
                    Preferences.Set("RememberMe", true);
                    Preferences.Set("Email", Email);
                    Preferences.Set("Password", Password);
                    Preferences.Set("IsLoggedIn", true);
                    Preferences.Set("IsAdmin", user.IsAdmin);
                }
                else
                {
                    Preferences.Set("RememberMe", false);
                    Preferences.Remove("Email");
                    Preferences.Remove("Password");
                    Preferences.Remove("IsLoggedIn");
                    Preferences.Remove("IsAdmin");
                }

                // Redirect to dashboard
                if (user.IsAdmin)
                {
                    await Application.Current.MainPage.DisplayAlert("Success", "Welcome Admin!", "OK");
                    await (Application.Current.MainPage as NavigationPage)
                        .PushAsync(new AdminDashboard());
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Success", "Welcome User!", "OK");
                    await (Application.Current.MainPage as NavigationPage)
                        .PushAsync(new EmployeeDashboard());
                }

                ClearEntries();
            }
            catch (MySqlConnector.MySqlException ex)
            {
                // Handle MySQL errors (e.g., server down, timeout, wrong host)
                await Application.Current.MainPage.DisplayAlert(
                    "Database Error",
                    $"Unable to connect to database. ({ex.Message})",
                    "Retry"
                );

                // Show your retry popup
                ShowRetryPopupCommand.Execute(null);
            }
            catch (Exception ex)
            {
                // Fallback for other unexpected errors
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    $"Something went wrong: {ex.Message}",
                    "OK"
                );
            }
        }

        [RelayCommand]
        private void ShowRetryPopup()
        {
            IsRetryPopupVisible = true;
        }

        [RelayCommand]
        private void HideRetryPopup()
        {
            IsRetryPopupVisible = false;
        }
        [RelayCommand]
        private async Task GoToRegister()
        {
            await Application.Current.MainPage.Navigation.PushAsync(new RegisterPage());
        }

        [RelayCommand]
        private void ClearEntries()
        {
            if (!RememberMe)
            {
                Email = string.Empty;
                Password = string.Empty;
            }
            else
            {
                Password = string.Empty;
            }
        }
    }
}
