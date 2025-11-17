using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Coftea_Capstone.C_;
using Coftea_Capstone.Models;
using Coftea_Capstone.Services;
using Microsoft.Maui.Controls;
using System.Threading.Tasks;
using System;
using Coftea_Capstone.ViewModel.Controls;

namespace Coftea_Capstone.ViewModel
{
	public partial class ResetPasswordPageViewModel : ObservableObject
	{
		private readonly Database _database = new();
		private readonly string _email;

		// ===================== State =====================
		[ObservableProperty] private string code;
		[ObservableProperty] private string newPassword;
		[ObservableProperty] private string confirmPassword;
		[ObservableProperty] private bool isLoading;
		[ObservableProperty] private string errorMessage;
		[ObservableProperty] private string successMessage;
		[ObservableProperty] private bool hasError;
		[ObservableProperty] private bool hasSuccess;

		public bool IsNotLoading => !IsLoading;

		public ResetPasswordPageViewModel(string email)
		{
			_email = email;
			// Clear any previous messages when ViewModel is created
			ClearMessages();
		}

		private RetryConnectionPopupViewModel GetRetryConnectionPopup() // Access the RetryConnectionPopupViewModel
		{ 
			return ((App)Application.Current).RetryConnectionPopup;
		}

		//  Commands 
		[RelayCommand]
		private async Task SubmitReset() // Submit the password reset
        {
			// Clear previous messages
			ClearMessages();

			// Validation
			if (string.IsNullOrWhiteSpace(Code))
			{
				ShowError("Please enter the verification code.");
				return;
			}
			if (Code.Trim().Length != 6)
			{
				ShowError("Verification code must be 6 digits.");
				return;
			}
			if (string.IsNullOrWhiteSpace(NewPassword) || string.IsNullOrWhiteSpace(ConfirmPassword))
			{
				ShowError("Please enter and confirm your new password.");
				return;
			}
			if (!string.Equals(NewPassword, ConfirmPassword))
			{
				ShowError("Passwords do not match.");
				return;
			}
			if (NewPassword.Length < 6)
			{
				ShowError("Password must be at least 6 characters.");
				return;
			}

			try
			{
				IsLoading = true;

				// Check internet connection
				if (!NetworkService.HasInternetConnection())
				{
					GetRetryConnectionPopup().ShowRetryPopup(SubmitReset, "No internet connection detected. Please check your network settings and try again.");
					return;
				}

				System.Diagnostics.Debug.WriteLine($"[ResetPasswordVM] Email: {_email}");
				System.Diagnostics.Debug.WriteLine($"[ResetPasswordVM] Code entered: '{Code}' (Length: {Code?.Length})");
				System.Diagnostics.Debug.WriteLine($"[ResetPasswordVM] Code trimmed: '{Code.Trim()}' (Length: {Code.Trim()?.Length})");
				
				var ok = await _database.ResetPasswordAsync(_email.Trim(), NewPassword, Code.Trim());
				if (!ok)
				{
					ShowError("Invalid or expired verification code. Please check your email and try again.");
					return;
				}

				ShowSuccess("Your password has been reset successfully!");
				
				// Clear form
				Code = string.Empty;
				NewPassword = string.Empty;
				ConfirmPassword = string.Empty;

				// Clear the stored email
				App.ResetPasswordEmail = null;
				
				// Navigate back to login after a short delay
				await Task.Delay(2000);
				await SimpleNavigationService.NavigateToAsync("//login");
			}
			catch (MySqlConnector.MySqlException ex)
			{
				GetRetryConnectionPopup().ShowRetryPopup(SubmitReset, $"Database connection failed: {ex.Message}");
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
		private async Task ResendCode() // Resend the verification code
        {
			try
			{
				IsLoading = true;
				ClearMessages();

				// Check internet connection
				if (!NetworkService.HasInternetConnection())
				{
					GetRetryConnectionPopup().ShowRetryPopup(ResendCode, "No internet connection detected. Please check your network settings and try again.");
					return;
				}

				// Request a new password reset token
				string newToken = await _database.RequestPasswordResetAsync(_email.Trim());
				if (!string.IsNullOrEmpty(newToken))
				{
					// Send email via MailHog
					var emailService = new EmailService();
					bool emailSent = await emailService.SendPasswordResetEmailAsync(_email.Trim(), newToken);
					
					if (emailSent)
					{
						ShowSuccess("A new verification code has been sent to your email.");
					}
					else
					{
						ShowError("Failed to send verification code. Please check if MailHog is running on localhost:1025 and try again.");
					}
				}
				else
				{
					ShowError("Failed to generate new verification code. Please try again.");
				}
			}
			catch (MySqlConnector.MySqlException ex)
			{
				GetRetryConnectionPopup().ShowRetryPopup(ResendCode, $"Database connection failed: {ex.Message}");
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
        private async Task GoBack() // Navigate back to login page
        {
            // Clear all fields
            Code = string.Empty;
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
            ClearMessages();
            
            // Clear the stored email when going back
            App.ResetPasswordEmail = null;

            await SimpleNavigationService.NavigateToAsync("//login");
        }

    
        private void ShowError(string message) // Display an error message
        {
			ErrorMessage = message;
			HasError = true;
			HasSuccess = false;
		}

		private void ShowSuccess(string message) // Display a success message
        {
			SuccessMessage = message;
			HasSuccess = true;
			HasError = false;
		}

		public void ClearMessages() // Clear error and success messages (made public for page access)
        {
			ErrorMessage = string.Empty;
			SuccessMessage = string.Empty;
			HasError = false;
			HasSuccess = false;
		}
	}
}
