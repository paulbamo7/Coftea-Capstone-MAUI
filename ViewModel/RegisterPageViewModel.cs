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
            _database = new Database(App.dbPath);
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
            if (string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(Password) ||
                string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "All fields are required.", "OK");
                return;
            }

            if (Password != ConfirmPassword)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Passwords do not match.", "OK");
                return;
            }

            var user = new UserInfoModel
            {
                Email = Email.Trim(),
                Password = Password,
                FirstName = FirstName?.Trim() ?? string.Empty,
                LastName = LastName?.Trim() ?? string.Empty,
                PhoneNumber = PhoneNumber?.Trim() ?? string.Empty,
                Address = Address?.Trim() ?? string.Empty,
                Birthday = Birthday,
            };

            try
            {
                await _database.AddUserAsync(user);
                await Application.Current.MainPage.DisplayAlert("Success", "Account created successfully!", "OK");

                // Go back to login
                await Application.Current.MainPage.Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to register: {ex.Message}", "OK");
            }
        }
    }
}