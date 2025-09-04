using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Coftea_Capstone.Models;
using Coftea_Capstone.Pages;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Maui.Controls;
using Coftea_Capstone.C_;

namespace Coftea_Capstone.ViewModel
{
    public partial class LoginPageViewModel : ObservableObject
    {
        private readonly Database _database;

        public LoginPageViewModel()
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "coftea.db3");
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

                await Application.Current.MainPage.Navigation.PushAsync(new EmployeeDashboard());
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Invalid username or password", "OK");
            }
        }
        [RelayCommand]
        private async Task GoToRegister()
        {
            await Application.Current.MainPage.Navigation.PushAsync(new RegisterPage());
        }
    }
}