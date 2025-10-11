using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using Coftea_Capstone.Models;
using Microsoft.Maui.Controls;

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
        private ImageSource profileImageSource = "usericon.png";

        [ObservableProperty]
        private bool isLoading = false;

        [ObservableProperty]
        private string statusMessage = string.Empty;

        [ObservableProperty]
        private bool hasError = false;

        [ObservableProperty]
        private bool isImagePickerVisible = false;

        [ObservableProperty]
        private bool canAccessInventory = false;

        [ObservableProperty]
        private bool canAccessSalesReport = false;

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
                    App.CurrentUser.ProfileImage = ProfileImage;
                    App.CurrentUser.CanAccessInventory = CanAccessInventory;
                    App.CurrentUser.CanAccessSalesReport = CanAccessSalesReport;
                }

                // Save to database
                await SaveProfileToDatabase();

                StatusMessage = "Profile saved successfully!";
                HasError = false;

                // Refresh profile display across the app
                RefreshProfileDisplay();

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
        private void ToggleImagePicker()
        {
            IsImagePickerVisible = !IsImagePickerVisible;
            if (IsImagePickerVisible)
            {
                StatusMessage = "Select a new profile image";
                HasError = false;
            }
        }

        [RelayCommand]
        private async Task ChangeProfileImage()
        {
            try
            {
                IsLoading = true;
                HasError = false;

                // Use the device's file picker to select an image
                var customFileType = new FilePickerFileType(
                    new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.iOS, new[] { "public.image" } },
                        { DevicePlatform.Android, new[] { "image/*" } },
                        { DevicePlatform.WinUI, new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" } },
                        { DevicePlatform.macOS, new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" } }
                    });

                var options = new PickOptions
                {
                    PickerTitle = "Select Profile Image",
                    FileTypes = customFileType
                };

                var result = await FilePicker.Default.PickAsync(options);
                if (result != null)
                {
                    // Copy the selected image to app data directory
                    var fileName = $"profile_{Guid.NewGuid()}{Path.GetExtension(result.FileName)}";
                    var targetPath = Path.Combine(FileSystem.AppDataDirectory, fileName);
                    
                    using var stream = await result.OpenReadAsync();
                    using var fileStream = File.Create(targetPath);
                    await stream.CopyToAsync(fileStream);
                    
                    ProfileImage = fileName;
                    ProfileImageSource = GetProfileImageSource(fileName);

                    // Update the global user profile
                    if (App.CurrentUser != null)
                    {
                        App.CurrentUser.ProfileImage = ProfileImage;
                    }

                    // Save to database
                    await SaveProfileImageToDatabase();

                    StatusMessage = "Profile image updated successfully!";
                    HasError = false;
                    IsImagePickerVisible = false;

                    // Refresh profile display across the app
                    RefreshProfileDisplay();

                    await Task.Delay(2000);
                    StatusMessage = string.Empty;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error changing profile image: {ex.Message}";
                HasError = true;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SaveProfileImageToDatabase()
        {
            try
            {
                var database = new Models.Database();
                var currentUserId = App.CurrentUser?.ID ?? 1;
                
                // Update user profile image in database
                await database.UpdateUserProfileImageAsync(currentUserId, ProfileImage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving profile image to database: {ex.Message}");
            }
        }

        private async void LoadUserProfile()
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
                    IsAdmin = App.CurrentUser.IsAdmin;
                    ProfileImage = App.CurrentUser.ProfileImage ?? "usericon.png";
                    ProfileImageSource = GetProfileImageSource(ProfileImage);
                    CanAccessInventory = App.CurrentUser.CanAccessInventory;
                    CanAccessSalesReport = App.CurrentUser.CanAccessSalesReport;
                }
                else
                {
                    // Load from database as fallback
                    await LoadUserFromDatabase();
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

        private async Task LoadUserFromDatabase()
        {
            try
            {
                var database = new Models.Database();
                var currentUserId = App.CurrentUser?.ID ?? 1; // Default to user ID 1 if not set
                
                // Load user details from database
                var user = await database.GetUserByIdAsync(currentUserId);
                if (user != null)
                {
                    Username = user.Username ?? string.Empty;
                    Email = user.Email ?? string.Empty;
                    FullName = user.FullName ?? string.Empty;
                    PhoneNumber = user.PhoneNumber ?? string.Empty;
                    IsAdmin = user.IsAdmin;
                    ProfileImage = user.ProfileImage ?? "usericon.png";
                    CanAccessInventory = user.CanAccessInventory;
                    CanAccessSalesReport = user.CanAccessSalesReport;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading user from database: {ex.Message}");
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
                Preferences.Set("IsAdmin", IsAdmin);
                Preferences.Set("ProfileImage", ProfileImage);
                Preferences.Set("CanAccessInventory", CanAccessInventory);
                Preferences.Set("CanAccessSalesReport", CanAccessSalesReport);

                System.Diagnostics.Debug.WriteLine("Profile saved to preferences successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving profile to storage: {ex.Message}");
                throw;
            }
        }

        private async Task SaveProfileToDatabase()
        {
            try
            {
                var database = new Models.Database();
                var currentUserId = App.CurrentUser?.ID ?? 1;
                
                // Update user profile in database
                await database.UpdateUserProfileAsync(currentUserId, Username, Email, FullName, PhoneNumber, ProfileImage, CanAccessInventory, CanAccessSalesReport);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving profile to database: {ex.Message}");
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

        // Method to refresh profile display across the app
        public void RefreshProfileDisplay()
        {
            try
            {
                // Trigger property change notifications
                OnPropertyChanged(nameof(Username));
                OnPropertyChanged(nameof(Email));
                OnPropertyChanged(nameof(FullName));
                OnPropertyChanged(nameof(PhoneNumber));
                OnPropertyChanged(nameof(IsAdmin));
                OnPropertyChanged(nameof(ProfileImage));
                OnPropertyChanged(nameof(ProfileImageSource));
                OnPropertyChanged(nameof(CanAccessInventory));
                OnPropertyChanged(nameof(CanAccessSalesReport));
                
                System.Diagnostics.Debug.WriteLine("Profile display refreshed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing profile display: {ex.Message}");
            }
        }

        private ImageSource GetProfileImageSource(string imageFileName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(imageFileName) || imageFileName == "usericon.png")
                {
                    return ImageSource.FromFile("usericon.png");
                }

                // Check if it's a custom profile image in app data
                var appDataPath = Path.Combine(FileSystem.AppDataDirectory, imageFileName);
                if (File.Exists(appDataPath))
                {
                    return ImageSource.FromFile(appDataPath);
                }

                // Fallback to default user icon
                return ImageSource.FromFile("usericon.png");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading profile image: {ex.Message}");
                return ImageSource.FromFile("usericon.png");
            }
        }
    }
}
