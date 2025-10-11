using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using Coftea_Capstone.Models;

namespace Coftea_Capstone.ViewModel.Controls
{
    public partial class ProfilePopupViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool isProfileVisible = false;

        [ObservableProperty]
        private string username = string.Empty;

        [ObservableProperty]
        private string email = string.Empty;

        [ObservableProperty]
        private string fullName = string.Empty;

        [ObservableProperty]
        private string phoneNumber = string.Empty;

        [ObservableProperty]
        private string department = string.Empty;

        [ObservableProperty]
        private string position = string.Empty;

        [ObservableProperty]
        private bool isAdmin = false;

        [ObservableProperty]
        private string profileImage = "usericon.png";

        [ObservableProperty]
        private bool isLoading = false;

        [ObservableProperty]
        private string statusMessage = string.Empty;

        [ObservableProperty]
        private bool hasError = false;

        public ProfilePopupViewModel()
        {
            LoadUserProfile();
        }

        public void ShowProfile()
        {
            LoadUserProfile();
            IsProfileVisible = true;
        }

        [RelayCommand]
        private void CloseProfile()
        {
            IsProfileVisible = false;
        }

        [RelayCommand]
        private async Task SaveProfile()
        {
            try
            {
                IsLoading = true;
                HasError = false;
                StatusMessage = "Saving profile...";

                // Validate required fields
                if (string.IsNullOrWhiteSpace(Username))
                {
                    StatusMessage = "Username is required";
                    HasError = true;
                    return;
                }

                if (string.IsNullOrWhiteSpace(Email))
                {
                    StatusMessage = "Email is required";
                    HasError = true;
                    return;
                }

                // Save to preferences
                await SaveUserProfileToStorage();

                // Update the global current user if it exists
                if (App.CurrentUser != null)
                {
                    App.CurrentUser.Username = Username;
                    App.CurrentUser.Email = Email;
                    App.CurrentUser.FullName = FullName;
                    App.CurrentUser.PhoneNumber = PhoneNumber;
                    App.CurrentUser.Department = Department;
                    App.CurrentUser.Position = Position;
                }

                StatusMessage = "Profile saved successfully!";
                HasError = false;

                // Close popup after successful save
                await Task.Delay(1000);
                IsProfileVisible = false;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving profile: {ex.Message}";
                HasError = true;
                System.Diagnostics.Debug.WriteLine($"Profile save error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task ChangePassword()
        {
            try
            {
                // This would typically open a password change dialog
                // For now, we'll just show a message
                StatusMessage = "Password change feature coming soon!";
                HasError = false;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                HasError = true;
            }
        }

        [RelayCommand]
        private async Task ChangeProfileImage()
        {
            try
            {
                // This would typically open an image picker
                // For now, we'll just show a message
                StatusMessage = "Profile image change feature coming soon!";
                HasError = false;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                HasError = true;
            }
        }

        private void LoadUserProfile()
        {
            try
            {
                // Load from current user if available
                if (App.CurrentUser != null)
                {
                    Username = App.CurrentUser.Username ?? string.Empty;
                    Email = App.CurrentUser.Email ?? string.Empty;
                    FullName = App.CurrentUser.FullName ?? string.Empty;
                    PhoneNumber = App.CurrentUser.PhoneNumber ?? string.Empty;
                    Department = App.CurrentUser.Department ?? string.Empty;
                    Position = App.CurrentUser.Position ?? string.Empty;
                    IsAdmin = App.CurrentUser.IsAdmin;
                }
                else
                {
                    // Load from preferences as fallback
                    Username = Preferences.Get("Username", string.Empty);
                    Email = Preferences.Get("Email", string.Empty);
                    FullName = Preferences.Get("FullName", string.Empty);
                    PhoneNumber = Preferences.Get("PhoneNumber", string.Empty);
                    Department = Preferences.Get("Department", string.Empty);
                    Position = Preferences.Get("Position", string.Empty);
                    IsAdmin = Preferences.Get("IsAdmin", false);
                }

                StatusMessage = string.Empty;
                HasError = false;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading profile: {ex.Message}";
                HasError = true;
                System.Diagnostics.Debug.WriteLine($"Profile load error: {ex.Message}");
            }
        }

        private async Task SaveUserProfileToStorage()
        {
            try
            {
                // Save to preferences
                Preferences.Set("Username", Username);
                Preferences.Set("Email", Email);
                Preferences.Set("FullName", FullName);
                Preferences.Set("PhoneNumber", PhoneNumber);
                Preferences.Set("Department", Department);
                Preferences.Set("Position", Position);
                Preferences.Set("IsAdmin", IsAdmin);

                System.Diagnostics.Debug.WriteLine("Profile saved to preferences successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving profile to storage: {ex.Message}");
                throw;
            }
        }

        [RelayCommand]
        private void ResetProfile()
        {
            LoadUserProfile();
            StatusMessage = "Profile reset to saved values";
            HasError = false;
        }
    }
}
