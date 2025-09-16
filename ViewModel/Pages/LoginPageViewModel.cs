using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Coftea_Capstone.Models;
using Coftea_Capstone.C_;
using Coftea_Capstone.Views.Pages;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Coftea_Capstone.Services;
using System.Threading.Tasks;
using System;

namespace Coftea_Capstone.ViewModel
{
    public partial class LoginPageViewModel : ObservableObject
    {
        private readonly Database _database;

        [ObservableProperty] private string email;
        [ObservableProperty] private string password;
        [ObservableProperty] private bool rememberMe;
        [ObservableProperty] private bool isRetryPopupVisible;

        public LoginPageViewModel()
        {
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
        }

        [RelayCommand]
        private async Task Login()
        {
            // Check internet
            if (!await NetworkService.EnsureInternetAsync())
            {
                ShowRetryPopupCommand.Execute(null);
                return;
            }

            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Please enter email and password", "OK");
                ClearEntries();
                return;
            }

            try
            {
                var user = await _database.GetUserByEmailAsync(Email.Trim());
                if (user == null)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "User not found", "OK");
                    ClearEntries();
                    return;
                }

                if (user.Password != Password)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Incorrect password", "OK");
                    ClearEntries();
                    return;
                }

                // Set current user
                App.SetCurrentUser(user);

                // Save preferences
                Preferences.Set("IsAdmin", user.IsAdmin);
                if (RememberMe)
                {
                    // Persist login across app restarts
                    Preferences.Set("IsLoggedIn", true);
                    Preferences.Set("RememberMe", true);
                    Preferences.Set("Email", Email);
                    Preferences.Set("Password", Password);
                }
                else
                {
                    // Do NOT keep user logged in after restart
                    Preferences.Set("IsLoggedIn", false);
                    Preferences.Set("RememberMe", false);
                    Preferences.Remove("Email");
                    Preferences.Remove("Password");
                }

                // Navigate to dashboard (replace stack to avoid brief LoginPage)
                var mainPage = new NavigationPage(user.IsAdmin ? new AdminDashboard() : new EmployeeDashboard());
                Application.Current.MainPage = mainPage;

                ClearEntries();
            }
            catch (MySqlConnector.MySqlException ex)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Database Error",
                    $"Unable to connect to database. ({ex.Message})",
                    "Retry"
                );
                ShowRetryPopupCommand.Execute(null);
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Something went wrong: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private void ShowRetryPopup() => IsRetryPopupVisible = true;

        [RelayCommand]
        private void HideRetryPopup() => IsRetryPopupVisible = false;

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
