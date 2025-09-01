using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Coftea_Capstone.C_;
using Coftea_Capstone.Pages;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Maui.Controls;
using Coftea_Capstone.Models;

namespace Coftea_Capstone.ViewModels
{
    public partial class LoginPageViewModel : ObservableObject
    {
        private readonly Database _database;

        public LoginPageViewModel()
        {
            _database = new Database();
        }

        // Observable properties bound to XAML
        [ObservableProperty]
        private string email;

        [ObservableProperty]
        private string password;

       
        // Login command
        [RelayCommand]
        private async Task Login()
        {
            // Always use the public properties (capitalized)
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Please enter username and password", "OK");
                return;
            }

            // Hardcoded credentials (replace with database later)
            if (Email == "admin" && Password == "1234") 
            {
                await Application.Current.MainPage.DisplayAlert("Success", "Login successful!", "OK");

                // Navigate to main page (replace with your actual main page)
                await Application.Current.MainPage.Navigation.PushAsync(new EmployeeDashboard());
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Invalid username or password", "OK");
            }
        }

        [RelayCommand]
        private async Task Register()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Please enter email and password", "OK");
                return;
            }

            var existingUser = await _database.GetUserByEmailAsync(Email);
            if (existingUser != null)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Email already exists!", "OK");
                return;
            }

            var newUser = new LoginPageModel
            {
                Email = Email,
                Password = Password
            };

            await _database.AddUserAsync(newUser);
            await Application.Current.MainPage.DisplayAlert("Success", "Registration successful!", "OK");
            await Application.Current.MainPage.Navigation.PushAsync(new LoginPage());
        }
    }
}