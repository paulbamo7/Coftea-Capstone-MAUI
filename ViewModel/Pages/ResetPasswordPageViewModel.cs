using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Coftea_Capstone.C_;
using Coftea_Capstone.Models;
using Microsoft.Maui.Controls;
using System.Threading.Tasks;

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

		public ResetPasswordPageViewModel(string email)
		{
			_email = email;
		}

		// ===================== Commands =====================
		[RelayCommand]
		private async Task SubmitReset()
		{
			if (string.IsNullOrWhiteSpace(Code))
			{
				await Application.Current.MainPage.DisplayAlert("Error", "Please enter the verification code.", "OK");
				return;
			}
			if (string.IsNullOrWhiteSpace(NewPassword) || string.IsNullOrWhiteSpace(ConfirmPassword))
			{
				await Application.Current.MainPage.DisplayAlert("Error", "Please enter and confirm your new password.", "OK");
				return;
			}
			if (!string.Equals(NewPassword, ConfirmPassword))
			{
				await Application.Current.MainPage.DisplayAlert("Error", "Passwords do not match.", "OK");
				return;
			}
			if (NewPassword.Length < 6)
			{
				await Application.Current.MainPage.DisplayAlert("Error", "Password must be at least 6 characters.", "OK");
				return;
			}

			try
			{
				IsLoading = true;
				System.Diagnostics.Debug.WriteLine($"[ResetPasswordVM] Email: {_email}");
				System.Diagnostics.Debug.WriteLine($"[ResetPasswordVM] Code entered: '{Code}' (Length: {Code?.Length})");
				System.Diagnostics.Debug.WriteLine($"[ResetPasswordVM] Code trimmed: '{Code.Trim()}' (Length: {Code.Trim()?.Length})");
				var ok = await _database.ResetPasswordAsync(_email.Trim(), NewPassword, Code.Trim());
				if (!ok)
				{
					await Application.Current.MainPage.DisplayAlert("Error", "Invalid or expired verification code.", "OK");
					return;
				}

				await Application.Current.MainPage.DisplayAlert("Success", "Your password has been reset.", "OK");
				if (Application.Current?.MainPage is NavigationPage nav)
				{
					await nav.PopToRootAsync(false);
				}
			}
			catch (System.Exception ex)
			{
				await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
			}
			finally
			{
				IsLoading = false;
			}
		}

		[RelayCommand]
		private async Task GoBack()
		{
			if (Application.Current?.MainPage is NavigationPage nav)
			{
				await nav.PopAsync(false);
			}
		}
	}
}
