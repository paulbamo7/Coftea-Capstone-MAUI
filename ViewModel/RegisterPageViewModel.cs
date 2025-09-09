using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Coftea_Capstone.C_;
using Coftea_Capstone.Pages;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Maui.Controls;
using Coftea_Capstone.Models;

namespace Coftea_Capstone.ViewModel
{
    public partial class RegisterPageViewModel : ObservableObject
    {
        private readonly Database _database;
        public RegisterPageViewModel()
        {
            // MySQL connection (XAMPP)
            _database = new Database(
                host: "localhost",
                database: "coftea_db",   // 👈 must match your phpMyAdmin database name
                user: "root",            // default XAMPP MySQL user
                password: ""             // default is empty (no password)
            );
        }

        [ObservableProperty] 
        private string email;
        [ObservableProperty] 
        private string password;
        [ObservableProperty] 
        private string confirmPassword;
        [ObservableProperty] 
        private string firstName;
        [ObservableProperty] 
        private string lastName;
        [ObservableProperty] 
        private string phoneNumber;
        [ObservableProperty] 
        private string address;
        [ObservableProperty] 
        private DateTime birthday = DateTime.Today;

        [RelayCommand]
        private async Task Register()
        {
            // Validation
            if (string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(Password) ||
                string.IsNullOrWhiteSpace(ConfirmPassword) ||
                string.IsNullOrWhiteSpace(FirstName))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Please fill in all required fields.", "OK");
                return;
            }

            if (Password != ConfirmPassword)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Passwords do not match.", "OK");
                return;
            }

            // Check if email already exists
            var existingUser = await _database.GetUserByEmailAsync(Email);
            if (existingUser != null)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Email already registered.", "OK");
                return;
            }

            // Create user model
            var user = new UserInfoModel
            {
                Email = Email.Trim(),
                Password = Password,
                FirstName = FirstName?.Trim() ?? string.Empty,
                LastName = LastName?.Trim() ?? string.Empty,
                PhoneNumber = PhoneNumber?.Trim() ?? string.Empty,
                Address = Address?.Trim() ?? string.Empty,
                Birthday = Birthday,
               /* IsAdmin = false  // default new users as employees*/
            };

            try
            {
                await _database.AddUserAsync(user);
                await Application.Current.MainPage.DisplayAlert("Success", "Account created successfully!", "OK");

                // Navigate back to Login page
                await Application.Current.MainPage.Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to register: {ex.Message}", "OK");
            }
        }
    }
}