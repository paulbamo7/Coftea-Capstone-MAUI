using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Coftea_Capstone.C_;
using Coftea_Capstone.Services;
using System.Threading.Tasks;
using Coftea_Capstone.Models;

namespace Coftea_Capstone.ViewModel.Controls
{
    public partial class PasswordResetPopupViewModel : ObservableObject
    {
        private readonly Database _database;
        private readonly EmailService _emailService;

        [ObservableProperty] private bool isPasswordResetPopupVisible = false;
        [ObservableProperty] private string email = string.Empty;
        [ObservableProperty] private string resetToken = string.Empty;
        [ObservableProperty] private string newPassword = string.Empty;
        [ObservableProperty] private string confirmPassword = string.Empty;
        [ObservableProperty] private bool isResetting = false;
        [ObservableProperty] private string errorMessage = string.Empty;

        public PasswordResetPopupViewModel()
        {
            _database = new Database();
            _emailService = new EmailService();
        }

        public void ShowPasswordResetPopup(string email, string token)
        {
            Email = email;
            ResetToken = token;
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
            ErrorMessage = string.Empty;
            IsPasswordResetPopupVisible = true;
        }

        [RelayCommand]
        private async Task ResetPassword()
        {
            if (string.IsNullOrWhiteSpace(NewPassword))
            {
                ErrorMessage = "Please enter a new password.";
                return;
            }

            if (NewPassword.Length < 6)
            {
                ErrorMessage = "Password must be at least 6 characters long.";
                return;
            }

            if (NewPassword != ConfirmPassword)
            {
                ErrorMessage = "Passwords do not match.";
                return;
            }

            try
            {
                IsResetting = true;
                ErrorMessage = string.Empty;

                // Check internet connection
                if (!NetworkService.HasInternetConnection())
                {
                    ErrorMessage = "No internet connection. Please check your network.";
                    return;
                }

                // Reset password in database
                bool success = await _database.ResetPasswordAsync(Email, NewPassword, ResetToken);

                if (success)
                {
                    await Application.Current.MainPage.DisplayAlert("Success", 
                        "Password has been reset successfully! You can now login with your new password.", "OK");
                    
                    IsPasswordResetPopupVisible = false;
                }
                else
                {
                    ErrorMessage = "Invalid or expired reset token. Please request a new password reset.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to reset password: {ex.Message}";
            }
            finally
            {
                IsResetting = false;
            }
        }

        [RelayCommand]
        private void ClosePasswordResetPopup()
        {
            IsPasswordResetPopupVisible = false;
            Email = string.Empty;
            ResetToken = string.Empty;
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
            ErrorMessage = string.Empty;
        }

        [RelayCommand]
        private async Task RequestNewReset()
        {
            try
            {
                IsResetting = true;
                ErrorMessage = string.Empty;

                // Request new password reset
                string newToken = await _database.RequestPasswordResetAsync(Email);
                
                if (!string.IsNullOrEmpty(newToken))
                {
                    // Send new email
                    bool emailSent = await _emailService.SendPasswordResetEmailAsync(Email, newToken);
                    
                    if (emailSent)
                    {
                        ResetToken = newToken; // Update the token
                        await Application.Current.MainPage.DisplayAlert("Success", 
                            "New password reset email sent! Check MailHog at http://localhost:8025", "OK");
                    }
                    else
                    {
                        ErrorMessage = "Failed to send new reset email. Please try again.";
                    }
                }
                else
                {
                    ErrorMessage = "Failed to generate new reset token. Please try again.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to request new reset: {ex.Message}";
            }
            finally
            {
                IsResetting = false;
            }
        }
    }
}
