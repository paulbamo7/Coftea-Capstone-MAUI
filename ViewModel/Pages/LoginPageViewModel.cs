using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Coftea_Capstone.Models;
using Coftea_Capstone.C_;
using Coftea_Capstone.Views.Pages;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Coftea_Capstone.Services;
using Coftea_Capstone.ViewModel.Controls;
using System.Threading.Tasks;
using System;
using BCrypt.Net;

namespace Coftea_Capstone.ViewModel
{
    public partial class LoginPageViewModel : ObservableObject
    {
        private readonly Database _database;
        private readonly EmailService _emailService;

        [ObservableProperty] private string email;
        [ObservableProperty] private string password;
        [ObservableProperty] private bool rememberMe;
        
        public RetryConnectionPopupViewModel RetryConnectionPopup { get; set; }
        public PasswordResetPopupViewModel PasswordResetPopup { get; set; }

        public LoginPageViewModel()
        {
            _database = new Database(
                host: "0.0.0.0",
                database: "coftea_db",
                user: "root",
                password: ""
            );
            _emailService = new EmailService();
            PasswordResetPopup = ((App)Application.Current).PasswordResetPopup;

            // Load saved "Remember Me" preferences
            bool savedRememberMe = Preferences.Get("RememberMe", false);
            if (savedRememberMe)
            {
                Email = Preferences.Get("Email", string.Empty);
                Password = Preferences.Get("Password", string.Empty);
                RememberMe = true;
            }
        }

        private RetryConnectionPopupViewModel GetRetryConnectionPopup()
        {
            return ((App)Application.Current).RetryConnectionPopup;
        }

        [RelayCommand]
        private async Task Login()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Please enter email and password", "OK");
                ClearEntries();
                return;
            }

            try
            {
                // Check internet connection
                if (!NetworkService.HasInternetConnection())
                {
                    GetRetryConnectionPopup().ShowRetryPopup(Login, "No internet connection detected. Please check your network settings and try again.");
                    return;
                }

                var user = await _database.GetUserByEmailAsync(Email.Trim());
                if (user == null)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "User not found", "OK");
                    ClearEntries();
                    return;
                }

                bool passwordValid = false;
                try
                {
                    passwordValid = BCrypt.Net.BCrypt.Verify(Password, user.Password);
                }
                catch (System.FormatException)
                {
                    // Likely a legacy plaintext password (invalid salt). Fallback once, then upgrade.
                    passwordValid = string.Equals(Password, user.Password);
                    if (passwordValid)
                    {
                        var newHash = BCrypt.Net.BCrypt.HashPassword(Password);
                        try { await _database.UpdateUserPasswordAsync(user.ID, newHash); } catch { /* ignore */ }
                    }
                }

                if (!passwordValid)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Incorrect password", "OK");
                    ClearEntries();
                    return;
                }

                // Check if user is approved
                if (user.Status != "approved")
                {
                    if (user.Status == "pending")
                    {
                        await Application.Current.MainPage.DisplayAlert("Account Pending", "Your account is still pending admin approval. Please wait for approval before logging in.", "OK");
                    }
                    else if (user.Status == "rejected")
                    {
                        await Application.Current.MainPage.DisplayAlert("Account Rejected", "Your account has been rejected. Please contact an administrator for more information.", "OK");
                    }
                    else
                    {
                        await Application.Current.MainPage.DisplayAlert("Account Not Active", "Your account is not active. Please contact an administrator.", "OK");
                    }
                    ClearEntries();
                    return;
                }

                // Set current user
                App.SetCurrentUser(user);

                // Save preferences
                Preferences.Set("IsLoggedIn", true);
                Preferences.Set("IsAdmin", user.IsAdmin);
                if (RememberMe)
                {
                    Preferences.Set("RememberMe", true);
                    Preferences.Set("Email", Email);
                    Preferences.Set("Password", Password);
                }
                else
                {
                    Preferences.Set("RememberMe", false);
                    Preferences.Remove("Email");
                    Preferences.Remove("Password");
                }

                // Navigate to dashboard (single dashboard with data-bound frames)
                var mainPage = new NavigationPage(new EmployeeDashboard());
                Application.Current.MainPage = mainPage;

                ClearEntries();
            }
            catch (MySqlConnector.MySqlException ex)
            {
                GetRetryConnectionPopup().ShowRetryPopup(Login, $"Database connection failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Something went wrong: {ex.Message}", "OK");
            }
        }


        [RelayCommand]
        private async Task GoToRegister()
        {
            try
            {
                await Application.Current.MainPage.Navigation.PushAsync(new RegisterPage());
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Navigation Error", $"Unable to open Register page: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task ForgotPassword()
        {
            await Application.Current.MainPage.Navigation.PushAsync(new ForgotPasswordPage());
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
