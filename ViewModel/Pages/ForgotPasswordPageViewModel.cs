using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Coftea_Capstone.Models;
using Coftea_Capstone.C_;
using Coftea_Capstone.Services;
using Microsoft.Maui.Controls;
using System.Threading.Tasks;
using System;
using Coftea_Capstone.ViewModel.Controls;

namespace Coftea_Capstone.ViewModel
{
    public partial class ForgotPasswordPageViewModel : ObservableObject
    {
        // ===================== Dependencies & Services =====================
        private readonly Database _database;
        private readonly EmailService _emailService;

        // ===================== State =====================
        [ObservableProperty] 
        private string email;

        [ObservableProperty] 
        private string errorMessage;

        [ObservableProperty] 
        private string successMessage;

        [ObservableProperty] 
        private bool hasError;

        [ObservableProperty] 
        private bool hasSuccess;

        [ObservableProperty] 
        private bool isLoading;

        public bool IsNotLoading => !IsLoading;

        // ===================== Initialization =====================
        public ForgotPasswordPageViewModel()
        {
            _database = new Database();
            _emailService = new EmailService();
        }

        private RetryConnectionPopupViewModel GetRetryConnectionPopup()
        {
            return ((App)Application.Current).RetryConnectionPopup;
        }

        // ===================== Commands =====================
        [RelayCommand]
        private async Task SendResetLink()
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                ShowError("Please enter your email address");
                return;
            }

            if (!IsValidEmail(Email))
            {
                ShowError("Please enter a valid email address");
                return;
            }

            try
            {
                IsLoading = true;
                ClearMessages();

                // Check internet connection
                if (!NetworkService.HasInternetConnection())
                {
                    GetRetryConnectionPopup().ShowRetryPopup(SendResetLink, "No internet connection detected. Please check your network settings and try again.");
                    return;
                }

                // MailHog connection will be tested when sending the email

                // Request password reset from database
                System.Diagnostics.Debug.WriteLine($"Requesting password reset for email: {Email.Trim()}");
                string resetToken = await _database.RequestPasswordResetAsync(Email.Trim());
                System.Diagnostics.Debug.WriteLine($"Reset token received: {!string.IsNullOrEmpty(resetToken)}");
                
                if (!string.IsNullOrEmpty(resetToken))
                {
                    // Send email via MailHog
                    System.Diagnostics.Debug.WriteLine($"Attempting to send email to: {Email.Trim()}");
                    bool emailSent = await _emailService.SendPasswordResetEmailAsync(Email.Trim(), resetToken);
                    System.Diagnostics.Debug.WriteLine($"Email sent successfully: {emailSent}");
                    
                    if (emailSent)
                    {
                        // Navigate to the new reset password page to let the user enter the code and new password
                        await NavigationService.NavigateToAsync(() => new Coftea_Capstone.Views.Pages.ResetPasswordPage(Email.Trim()));
                    }
                    else
                    {
                        ShowError("Failed to send password reset email. Please check if MailHog is running on localhost:1025 and try again.");
                    }
                }
                else
                {
                    ShowError("No account found with this email address.");
                }
            }
            catch (MySqlConnector.MySqlException ex)
            {
                GetRetryConnectionPopup().ShowRetryPopup(SendResetLink, $"Database connection failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                ShowError($"Something went wrong: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task GoBack()
        {
            await Application.Current.MainPage.Navigation.PopAsync();
        }

        // ===================== Helpers =====================
        private void ShowError(string message)
        {
            ErrorMessage = message;
            HasError = true;
            HasSuccess = false;
        }

        private void ShowSuccess(string message)
        {
            SuccessMessage = message;
            HasSuccess = true;
            HasError = false;
        }

        private void ClearMessages()
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;
            HasError = false;
            HasSuccess = false;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
