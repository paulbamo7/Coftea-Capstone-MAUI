using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Coftea_Capstone.C_;
using Coftea_Capstone.Views.Pages;
using Coftea_Capstone.ViewModel.Controls;
using Coftea_Capstone.Services;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Maui.Controls;
using Coftea_Capstone.Models;
using System.Text.RegularExpressions;
using BCrypt.Net;

namespace Coftea_Capstone.ViewModel
{
    public partial class RegisterPageViewModel : ObservableObject
    {
        private readonly Database _database;
        public RetryConnectionPopupViewModel RetryConnectionPopup { get; set; }

        public RegisterPageViewModel()
        {
            _database = new Database(
               host: "0.0.0.0",
               database: "coftea_db",
               user: "root",
               password: ""
            );
        }

        private RetryConnectionPopupViewModel GetRetryConnectionPopup()
        {
            return ((App)Application.Current).RetryConnectionPopup;
        }

        [ObservableProperty] private bool isTermsAccepted;
        [ObservableProperty] private string email;
        [ObservableProperty] private string password;
        [ObservableProperty] private string confirmPassword;
        [ObservableProperty] private string firstName;
        [ObservableProperty] private string lastName;
        [ObservableProperty] private string phoneNumber;
        [ObservableProperty] private string address;
        [ObservableProperty] private DateTime birthday = DateTime.Today;

        [RelayCommand]
        private async Task GoToLogin()
        {
            await Application.Current.MainPage.Navigation.PopAsync();
        }

        [RelayCommand]
        private async Task Register()
        {
            // First Name validation
            if (string.IsNullOrWhiteSpace(FirstName))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "First Name is required.", "OK");
                return;
            }

            // Last Name validation
            if (string.IsNullOrWhiteSpace(LastName))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Last Name is required.", "OK");
                return;
            }

            // Email validation
            if (string.IsNullOrWhiteSpace(Email))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Email is required.", "OK");
                return;
            }
            if (!Regex.IsMatch(Email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Enter a valid email address.", "OK");
                return;
            }

            // Password validation
            if (string.IsNullOrWhiteSpace(Password))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Password is required.", "OK");
                return;
            }
            if (Password.Length < 6)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Password must be at least 6 characters long.", "OK");
                return;
            }

            // Confirm password
            if (Password != ConfirmPassword)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Passwords do not match.", "OK");
                return;
            }

            // Phone number validation
            if (string.IsNullOrWhiteSpace(PhoneNumber))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Phone Number is required.", "OK");
                return;
            }
            if (!Regex.IsMatch(PhoneNumber.Trim(), @"^\+?\d{10,15}$"))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Enter a valid phone number.", "OK");
                return;
            }

            // Address validation
            if (string.IsNullOrWhiteSpace(Address))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Address is required.", "OK");
                return;
            }

            // Birthday validation (optional: cannot be in the future)
            if (Birthday > DateTime.Today)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Birthday cannot be in the future.", "OK");
                return;
            }
            // Terms & Conditions
            if (!IsTermsAccepted)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "You must accept the Terms and Conditions to register.", "OK");
                return;
            }
            try
            {
                // Check internet connection
                if (!NetworkService.HasInternetConnection())
                {
                    GetRetryConnectionPopup().ShowRetryPopup(Register, "No internet connection detected. Please check your network settings and try again.");
                    return;
                }

                // Check if email already exists
                var existingUser = await _database.GetUserByEmailAsync(Email);
                if (existingUser != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Email is already registered.", "OK");
                    return;
                }

                // Check if this will be the first user (admin)
                bool isFirstUser = await _database.IsFirstUserAsync();

                if (isFirstUser)
                {
                    // First user gets direct registration with admin privileges
                    var user = new UserInfoModel
                    {
                        Email = Email.Trim(),
                        Password = BCrypt.Net.BCrypt.HashPassword(Password),
                        FirstName = FirstName.Trim(),
                        LastName = LastName.Trim(),
                        PhoneNumber = PhoneNumber.Trim(),
                        Address = Address.Trim(),
                        Birthday = Birthday,
                        IsAdmin = true
                    };

                    await _database.AddUserAsync(user);
                    await Application.Current.MainPage.DisplayAlert("Success", "Account created successfully! You are the first user and have been granted admin privileges.", "OK");
                }
                else
                {
                    // Subsequent users create pending requests
                    var pendingRequest = new UserPendingRequest
                    {
                        Email = Email.Trim(),
                        Password = BCrypt.Net.BCrypt.HashPassword(Password),
                        FirstName = FirstName.Trim(),
                        LastName = LastName.Trim(),
                        PhoneNumber = PhoneNumber.Trim(),
                        Address = Address.Trim(),
                        Birthday = Birthday,
                        RequestDate = DateTime.Now
                    };

                    await _database.AddPendingUserRequestAsync(pendingRequest);
                    await Application.Current.MainPage.DisplayAlert("Success", "Registration request submitted! An admin will review and approve your account.", "OK");
                }

                // Navigate back to Login page
                await Application.Current.MainPage.Navigation.PopAsync();
            }
            catch (MySqlConnector.MySqlException ex)
            {
                GetRetryConnectionPopup().ShowRetryPopup(Register, $"Database connection failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to register: {ex.Message}", "OK");
            }
        }
    }
}
