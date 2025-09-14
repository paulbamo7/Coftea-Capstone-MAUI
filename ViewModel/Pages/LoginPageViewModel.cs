using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Coftea_Capstone.Models;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Coftea_Capstone.C_;
using Coftea_Capstone.Views.Pages;

namespace Coftea_Capstone.ViewModel
{
    public partial class LoginPageViewModel : ObservableObject
    {
        private readonly Database _database;
        [ObservableProperty] private bool rememberMe;

        public LoginPageViewModel()
        {
            // MySQL connection (XAMPP)
            _database = new Database(
                host: "0.0.0.0",
                database: "coftea_db",
                user: "root",
                password: ""         
            ); 
            if (Preferences.ContainsKey("RememberMe") && Preferences.Get("RememberMe", false))
            {
                Email = Preferences.Get("Email", string.Empty);
                Password = Preferences.Get("Password", string.Empty);
                RememberMe = true;
            }
        }

        [ObservableProperty]
        private string email;

        [ObservableProperty]
        private string password;

        [ObservableProperty]
        private bool isAdmin;

        [RelayCommand]
        private async Task Login()
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                await Application.Current.MainPage.DisplayAlert("Connection Error", "No internet connection detected. Please check your connection.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Please enter username and password", "OK");
                ClearEntries();
                return;
            }

            var user = await _database.GetUserByEmailAsync(Email);

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
            if (RememberMe)
            {
                Preferences.Set("RememberMe", true);
                Preferences.Set("Email", Email);
                Preferences.Set("Password", Password); // ⚠️ Plaintext, better use token or hash
            }
            else
            {
                Preferences.Remove("RememberMe");
                Preferences.Remove("Email");
                Preferences.Remove("Password");
            }
            // Save current user in App state
            App.SetCurrentUser(user);
            Preferences.Set("IsLoggedIn", true);
            Preferences.Set("IsAdmin", user.IsAdmin);

            if (user.IsAdmin)
            {
                await Application.Current.MainPage.DisplayAlert("Success", "Welcome Admin!", "OK");
                await (Application.Current.MainPage as NavigationPage)
                    .PushAsync(new AdminDashboard());
                ClearEntries();
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Success", "Welcome User!", "OK");
                await (Application.Current.MainPage as NavigationPage)
                    .PushAsync(new EmployeeDashboard());
                ClearEntries();
            }
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