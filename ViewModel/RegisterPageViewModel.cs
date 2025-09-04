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
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "coftea.db3");
            _database = new Database();
        }

        [ObservableProperty]
        private string email;

        [ObservableProperty]
        private string password;

        [ObservableProperty]
        private string firstName;
        [ObservableProperty]
        private string lastName;
        [ObservableProperty]
        private bool role;
        [ObservableProperty]
        private DateTime birthday;
        [ObservableProperty]
        private string phoneNumber;
        [ObservableProperty]
        private string address;



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

            var newUser = new UserInfoModel
            {
                Email = Email,
                Password = Password,
                FirstName = FirstName,
                LastName = LastName,
                /*Role = Role,*/
                Birthday = Birthday,
                PhoneNumber = PhoneNumber,
                Address = Address
            };

            await _database.AddUserAsync(newUser);
            await Application.Current.MainPage.DisplayAlert("Success", "Registration successful!", "OK");

            // Navigate back to Login Page
            await Application.Current.MainPage.Navigation.PushAsync(new LoginPage());
        }
    }
}
