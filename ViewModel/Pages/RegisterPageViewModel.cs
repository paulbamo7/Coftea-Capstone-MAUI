using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Coftea_Capstone.C_;
using Coftea_Capstone.Views.Pages;
using Coftea_Capstone.ViewModel.Controls;
using Coftea_Capstone.Services;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Maui.Controls;
using Coftea_Capstone.Services;
using Coftea_Capstone.Models;
using System.Text.RegularExpressions;

namespace Coftea_Capstone.ViewModel
{
    public partial class RegisterPageViewModel : ObservableObject
    {
        private readonly Database _database;
        public RetryConnectionPopupViewModel RetryConnectionPopup { get; set; }

        public RegisterPageViewModel()
        {
            _database = new Database();
            
            // Initialize properties
            FirstName = string.Empty;
            LastName = string.Empty;
            Email = string.Empty;
            Password = string.Empty;
            ConfirmPassword = string.Empty;
            PhoneNumber = string.Empty;
            IsTermsAccepted = false;
        }

        private RetryConnectionPopupViewModel GetRetryConnectionPopup()
        {
            return ((App)Application.Current).RetryConnectionPopup;
        }

        // ===================== State =====================
        [ObservableProperty] private bool isTermsAccepted;
        [ObservableProperty] private string email;
        [ObservableProperty] private string password;
        [ObservableProperty] private string confirmPassword;
        [ObservableProperty] private string firstName;
        [ObservableProperty] private string lastName;
        [ObservableProperty] private string phoneNumber;
        
        // Password validation properties
        [ObservableProperty] private bool hasUppercase;
        [ObservableProperty] private bool hasLowercase;
        [ObservableProperty] private bool hasNumber;
        [ObservableProperty] private bool hasSpecialChar;
        [ObservableProperty] private bool hasMinLength;
        [ObservableProperty] private bool passwordsMatch;

        // Password validation methods
        partial void OnPasswordChanged(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                HasUppercase = false;
                HasLowercase = false;
                HasNumber = false;
                HasSpecialChar = false;
                HasMinLength = false;
            }
            else
            {
                HasUppercase = Regex.IsMatch(value, @"[A-Z]");
                HasLowercase = Regex.IsMatch(value, @"[a-z]");
                HasNumber = Regex.IsMatch(value, @"[0-9]");
                HasSpecialChar = Regex.IsMatch(value, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]");
                HasMinLength = value.Length >= 8;
            }
            
            // Update password match when password changes
            if (!string.IsNullOrEmpty(ConfirmPassword))
            {
                PasswordsMatch = Password == ConfirmPassword;
            }
        }

        partial void OnConfirmPasswordChanged(string value)
        {
            PasswordsMatch = !string.IsNullOrEmpty(Password) && !string.IsNullOrEmpty(value) && Password == value;
        }

        // ===================== Commands =====================
        [RelayCommand]
        private async Task BackToLogin() // Navigate back to Login page
        {
            // Don't clear fields - just navigate back
            await Shell.Current.GoToAsync("//login");
        }

        [RelayCommand]
        private async Task Register() // Handle user registration
        {
            // Validations
            if (string.IsNullOrWhiteSpace(FirstName))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "First Name is required.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(LastName))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Last Name is required.", "OK");
                return;
            }

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

            if (string.IsNullOrWhiteSpace(Password))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Password is required.", "OK");
                return;
            }
            if (Password.Length < 8)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Password must be at least 8 characters long.", "OK");
                return;
            }

            if (!HasUppercase || !HasLowercase || !HasNumber || !HasSpecialChar || !HasMinLength)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Password must contain at least one uppercase letter, one lowercase letter, one number, one special character, and be at least 8 characters long.", "OK");
                return;
            }

            if (Password != ConfirmPassword)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Passwords do not match.", "OK");
                return;
            }

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

            if (!IsTermsAccepted)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "You must accept the Terms and Conditions to register.", "OK");
                return;
            }

            try
            {
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
                    // First register becomes admin
                    var user = new UserInfoModel
                    {
                        Email = Email.Trim(),
                        Password = BCrypt.Net.BCrypt.HashPassword(Password),
                        FirstName = FirstName.Trim(),
                        LastName = LastName.Trim(),
                        PhoneNumber = PhoneNumber.Trim(),
                        IsAdmin = true
                    };

                    await _database.AddUserAsync(user);
                    await Application.Current.MainPage.DisplayAlert("Success", "Account created successfully! You are the first user and have been granted admin privileges.", "OK");
                }
                else
                {
                    // Users request for registration approval
                    var pendingRequest = new UserPendingRequest
                    {
                        Email = Email.Trim(),
                        Password = BCrypt.Net.BCrypt.HashPassword(Password),
                        FirstName = FirstName.Trim(),
                        LastName = LastName.Trim(),
                        PhoneNumber = PhoneNumber.Trim(),
                        RequestDate = DateTime.Now
                    };

                    await _database.AddPendingUserRequestAsync(pendingRequest);
                    await Application.Current.MainPage.DisplayAlert("Success", "Registration request submitted! An admin will review and approve your account.", "OK");
                }
                
                // Navigate back to Login page
                await Shell.Current.GoToAsync("//login");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to register: {ex.Message}", "OK");
            }
        }
    }
}