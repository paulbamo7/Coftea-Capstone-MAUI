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
        private readonly EmailService _emailService;
        public RetryConnectionPopupViewModel RetryConnectionPopup { get; set; }

        public RegisterPageViewModel()
        {
            _database = new Database();
            _emailService = new EmailService();
            
            // Initialize properties
            FirstName = string.Empty;
            LastName = string.Empty;
            Email = string.Empty;
            Password = string.Empty;
            ConfirmPassword = string.Empty;
        }

        private RetryConnectionPopupViewModel GetRetryConnectionPopup()
        {
            return ((App)Application.Current).RetryConnectionPopup;
        }

        // ===================== State =====================
        [ObservableProperty] private string email;
        [ObservableProperty] private string password;
        [ObservableProperty] private string confirmPassword;
        [ObservableProperty] private string firstName;
        [ObservableProperty] private string lastName;
        [ObservableProperty] private string firstNameValidationMessage;
        [ObservableProperty] private string lastNameValidationMessage;
        [ObservableProperty] private string passwordValidationMessage;
        [ObservableProperty] private string confirmPasswordValidationMessage;
        
        // Password validation properties
        [ObservableProperty] private bool hasUppercase;
        [ObservableProperty] private bool hasLowercase;
        [ObservableProperty] private bool hasNumber;
        [ObservableProperty] private bool hasSpecialChar;
        [ObservableProperty] private bool hasMinLength;
        [ObservableProperty] private bool passwordsMatch;
        
        // Email validation properties
        [ObservableProperty] private bool isEmailValid = true;
        [ObservableProperty] private bool isEmailAvailable = true;
        [ObservableProperty] private string emailValidationMessage = "";

        partial void OnFirstNameChanged(string value)
        {
            FirstNameValidationMessage = string.Empty;
        }

        partial void OnLastNameChanged(string value)
        {
            LastNameValidationMessage = string.Empty;
        }

        // Password validation methods
        partial void OnPasswordChanged(string value)
        {
            PasswordValidationMessage = string.Empty;
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
            ConfirmPasswordValidationMessage = string.Empty;
            PasswordsMatch = !string.IsNullOrEmpty(Password) && !string.IsNullOrEmpty(value) && Password == value;
        }

        partial void OnEmailChanged(string value)
        {
            // Reset validation state
            IsEmailValid = true;
            IsEmailAvailable = true;
            EmailValidationMessage = "";

            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            // Validate email format
            if (!Regex.IsMatch(value.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                IsEmailValid = false;
                EmailValidationMessage = "Invalid email format";
                return;
            }

            IsEmailValid = true;

            // Check if email is already registered (debounced) - fire and forget async task
            _ = CheckEmailAvailabilityAsync(value);
        }

        private async Task CheckEmailAvailabilityAsync(string email)
        {
            try
            {
                await Task.Delay(500); // Debounce: wait 500ms after user stops typing
                
                // Check again if email hasn't changed
                if (Email == email && !string.IsNullOrWhiteSpace(email))
                {
                    if (!Services.NetworkService.HasInternetConnection())
                    {
                        return; // Can't check without internet
                    }

                    // Check if email exists in approved users
                    var existingUser = await _database.GetUserByEmailAsync(email.Trim());
                    if (existingUser != null)
                    {
                        IsEmailAvailable = false;
                        EmailValidationMessage = "Email is already registered";
                        return;
                    }

                    // Check if email exists in pending requests
                    var existingPendingRequest = await _database.GetPendingUserRequestByEmailAsync(email.Trim());
                    if (existingPendingRequest != null)
                    {
                        IsEmailAvailable = false;
                        EmailValidationMessage = "Email is already registered (pending approval)";
                        return;
                    }

                    // Email is available
                    IsEmailAvailable = true;
                    EmailValidationMessage = "";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking email availability: {ex.Message}");
                // Don't block user if check fails
            }
        }

        // ===================== Commands =====================
        [RelayCommand]
        private async Task BackToLogin() // Navigate back to Login page
        {
            // Clear all fields before navigating back
            ClearFields();
            await Shell.Current.GoToAsync("//login");
        }

        [RelayCommand]
        private async Task Register() // Handle user registration
        {
            ResetValidationMessages();

            bool hasValidationError = false;

            if (string.IsNullOrWhiteSpace(FirstName))
            {
                FirstNameValidationMessage = "First name is required.";
                hasValidationError = true;
            }

            if (string.IsNullOrWhiteSpace(LastName))
            {
                LastNameValidationMessage = "Last name is required.";
                hasValidationError = true;
            }

            if (string.IsNullOrWhiteSpace(Email))
            {
                EmailValidationMessage = "Email is required.";
                hasValidationError = true;
            }
            else if (!Regex.IsMatch(Email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                EmailValidationMessage = "Enter a valid email address.";
                hasValidationError = true;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                PasswordValidationMessage = "Password is required.";
                hasValidationError = true;
            }
            else if (Password.Length < 8)
            {
                PasswordValidationMessage = "Password must be at least 8 characters long.";
                hasValidationError = true;
            }
            else if (!HasUppercase || !HasLowercase || !HasNumber || !HasSpecialChar || !HasMinLength)
            {
                PasswordValidationMessage = "Include uppercase, lowercase, number, and special character.";
                hasValidationError = true;
            }

            if (string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                ConfirmPasswordValidationMessage = "Confirm password is required.";
                hasValidationError = true;
            }
            else if (Password != ConfirmPassword)
            {
                ConfirmPasswordValidationMessage = "Passwords do not match.";
                hasValidationError = true;
            }

            if (hasValidationError)
            {
                return;
            }


            try
            {
            // Require internet for registration
            if (!Services.NetworkService.HasInternetConnection())
            {
                try { await Application.Current.MainPage.DisplayAlert("No Internet", "No internet connection. Please check your network and try again.", "OK"); } catch { }
                return;
            }
                
                // Final email availability check - do this synchronously before registration
                // Check if email already exists in approved users
                var existingUser = await _database.GetUserByEmailAsync(Email.Trim());
                if (existingUser != null)
                {
                    IsEmailAvailable = false;
                    EmailValidationMessage = "Email is already registered.";
                    return;
                }

                // Final check: Check if email already exists in pending requests
                var existingPendingRequest = await _database.GetPendingUserRequestByEmailAsync(Email);
                if (existingPendingRequest != null)
                {
                    IsEmailAvailable = false;
                    EmailValidationMessage = "Email is already registered (pending approval).";
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
                        IsAdmin = true
                    };

                    await _database.AddUserAsync(user);
                    
                    // Send registration success email
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"📧 Attempting to send registration email to: {Email.Trim()}");
                        var emailSent = await _emailService.SendRegistrationSuccessEmailAsync(
                            Email.Trim(), 
                            FirstName.Trim(), 
                            LastName.Trim(), 
                            isAdmin: true
                        );
                        if (emailSent)
                        {
                            System.Diagnostics.Debug.WriteLine($"✅ Registration email sent successfully!");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ Registration email send returned false (check logs above for details)");
                        }
                    }
                    catch (Exception emailEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Exception caught while sending registration email: {emailEx.Message}");
                        System.Diagnostics.Debug.WriteLine($"   Type: {emailEx.GetType().Name}");
                        System.Diagnostics.Debug.WriteLine($"   Stack: {emailEx.StackTrace}");
                        // Don't block registration if email fails
                    }
                    
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
                        RequestDate = DateTime.Now
                    };

                    await _database.AddPendingUserRequestAsync(pendingRequest);
                    
                    // Send registration success email (pending approval)
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"📧 Attempting to send registration email to: {Email.Trim()}");
                        var emailSent = await _emailService.SendRegistrationSuccessEmailAsync(
                            Email.Trim(), 
                            FirstName.Trim(), 
                            LastName.Trim(), 
                            isAdmin: false
                        );
                        if (emailSent)
                        {
                            System.Diagnostics.Debug.WriteLine($"✅ Registration email sent successfully!");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ Registration email send returned false (check logs above for details)");
                        }
                    }
                    catch (Exception emailEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Exception caught while sending registration email: {emailEx.Message}");
                        System.Diagnostics.Debug.WriteLine($"   Type: {emailEx.GetType().Name}");
                        System.Diagnostics.Debug.WriteLine($"   Stack: {emailEx.StackTrace}");
                        // Don't block registration if email fails
                    }
                    
                    await Application.Current.MainPage.DisplayAlert("Success", "Registration request submitted! An admin will review and approve your account.", "OK");
                }
                
                // Clear all fields after successful registration
                ClearFields();
                
                // Navigate back to Login page
                await Shell.Current.GoToAsync("//login");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to register: {ex.Message}", "OK");
            }
        }

        private void ClearFields()
        {
            FirstName = string.Empty;
            LastName = string.Empty;
            Email = string.Empty;
            Password = string.Empty;
            ConfirmPassword = string.Empty;
            
            // Reset validation states
            IsEmailValid = true;
            IsEmailAvailable = true;
            EmailValidationMessage = "";
            HasUppercase = false;
            HasLowercase = false;
            HasNumber = false;
            HasSpecialChar = false;
            HasMinLength = false;
            PasswordsMatch = false;
            ResetValidationMessages();
        }

        private void ResetValidationMessages()
        {
            FirstNameValidationMessage = string.Empty;
            LastNameValidationMessage = string.Empty;
            PasswordValidationMessage = string.Empty;
            ConfirmPasswordValidationMessage = string.Empty;
            EmailValidationMessage = string.Empty;
            IsEmailValid = true;
            IsEmailAvailable = true;
        }
    }
}