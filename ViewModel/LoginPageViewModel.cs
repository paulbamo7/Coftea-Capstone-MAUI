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
            _database = new Database(App.dbPath);
        }

        // Observable properties bound to XAML
        [ObservableProperty]
        private string email;

        [ObservableProperty]
        private string password;

        [ObservableProperty]
        private bool isAdmin;

        // Login command
        [RelayCommand]
        private async Task Login()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Please enter username and password", "OK");
                return;
            }

            var user = await _database.GetUserByEmailAsync(Email);

            if (user == null)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "User not found", "OK");
                return;
            }

            if (user.Password != Password)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Incorrect password", "OK");
                return;
            }

            if (user.IsAdmin)
            {
                await Application.Current.MainPage.DisplayAlert("Success", "Welcome Admin!", "OK");
                App.SetCurrentUser(user);
                await (Application.Current.MainPage as NavigationPage)
                    .PushAsync(new AdminDashboard());
               
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Success", "Welcome User!", "OK");
                App.SetCurrentUser(user);
                await (Application.Current.MainPage as NavigationPage)
                    .PushAsync(new EmployeeDashboard());
            }
        }

        [RelayCommand]
        private async Task GoToRegister()
        {
            await Application.Current.MainPage.Navigation.PushAsync(new RegisterPage());
        }
    }
}