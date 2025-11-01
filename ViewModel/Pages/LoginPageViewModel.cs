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

namespace Coftea_Capstone.ViewModel
{
    public partial class LoginPageViewModel : ObservableObject
    {
        // ===================== Dependencies & Services =====================
        private readonly Database _database;
        private readonly EmailService _emailService;

        // ===================== State =====================
        [ObservableProperty] private string email;
        [ObservableProperty] private string password;
        [ObservableProperty] private bool rememberMe;
        
        public RetryConnectionPopupViewModel RetryConnectionPopup { get; set; }
        public PasswordResetPopupViewModel PasswordResetPopup { get; set; }

        public LoginPageViewModel()
        {
            _database = new Database();
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

        private RetryConnectionPopupViewModel GetRetryConnectionPopup() // Retry Connection Error Popup
        {
            return ((App)Application.Current).RetryConnectionPopup;
        }

        [RelayCommand]
        private async Task Login() // Login command
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
                    passwordValid = string.Equals(Password, user.Password);
                    if (passwordValid)
                    {
                        var newHash = BCrypt.Net.BCrypt.HashPassword(Password);
                        try { await _database.UpdateUserPasswordAsync(user.ID, newHash); } catch 
                        {

                        }
                    }
                }

                if (!passwordValid)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Incorrect password", "OK");
                    ClearEntries();
                    return;
                }

                if (user.IsAdmin) // Ensures that admin has access to all modules
                {
                    user.CanAccessInventory = true;
                    user.CanAccessSalesReport = true;
                }
                App.SetCurrentUser(user);

                // Save preferences
                Preferences.Set("IsLoggedIn", true);
                Preferences.Set("IsAdmin", user.IsAdmin);
                Preferences.Set("UserID", user.ID);
                
                System.Diagnostics.Debug.WriteLine($"💾 Saving login preferences - RememberMe: {RememberMe}");
                
                if (RememberMe) 
                {
                    Preferences.Set("RememberMe", true);
                    Preferences.Set("Email", Email);
                    Preferences.Set("Password", Password);
                    System.Diagnostics.Debug.WriteLine($"✅ Remember Me enabled - Email: {Email}");
                }
                else
                {
                    Preferences.Set("RememberMe", false);
                    Preferences.Remove("Email");
                    Preferences.Remove("Password");
                    System.Diagnostics.Debug.WriteLine("❌ Remember Me disabled - Credentials cleared");
                }
                
                // Verify preferences were saved
                System.Diagnostics.Debug.WriteLine($"🔍 Verification - IsLoggedIn: {Preferences.Get("IsLoggedIn", false)}, RememberMe: {Preferences.Get("RememberMe", false)}");

                // Ensure popups are closed and refreshed after login
                var app = (App)Application.Current;
                if (app?.SettingsPopup != null)
                {
                    app.SettingsPopup.IsSettingsPopupVisible = false;
                }
                if (app?.NotificationPopup != null)
                {
                    app.NotificationPopup.IsNotificationVisible = false;
                }
                if (app?.ProfilePopup != null)
                {
                    app.ProfilePopup.IsProfileVisible = false;
                    // Immediately clear profile image to prevent showing wrong account's image
                    app.ProfilePopup.ProfileImageSource = null;
                    app.ProfilePopup.ProfileImage = "usericon.png";
                    // Reload profile for new user immediately on main thread
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await app.ProfilePopup.LoadUserProfile();
                    });
                }

                // Navigate to dashboard
                await SimpleNavigationService.NavigateToAsync("//dashboard");
                
                // Small delay to ensure navigation completes
                await Task.Delay(200);
                
                // Force UI refresh after navigation
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    // Force profile image refresh by clearing and reloading
                    if (app?.ProfilePopup != null)
                    {
                        app.ProfilePopup.ProfileImageSource = null;
                        _ = Task.Run(async () => await app.ProfilePopup.LoadUserProfile());
                    }
                });

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
        private async Task GoToRegister() // Navigate to Register page
        {
            try
            {
                await SimpleNavigationService.NavigateToAsync("//register");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Navigation Error", $"Unable to open Register page: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task ForgotPassword() // Navigate to Forgot Password page
        {
            try
            {
                await SimpleNavigationService.NavigateToAsync("//forgotpassword");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Navigation Error", $"Unable to open Forgot Password page: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private void ClearEntries() // Clears input fields
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