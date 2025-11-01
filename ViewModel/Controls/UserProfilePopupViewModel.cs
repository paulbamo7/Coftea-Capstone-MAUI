using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Coftea_Capstone.Models;
using Coftea_Capstone.C_;

namespace Coftea_Capstone.ViewModel.Controls
{
    public partial class UserProfilePopupViewModel : ObservableObject
    {
        private readonly Database _database = new();

        [ObservableProperty]
        private bool isVisible = false;

        [ObservableProperty]
        private string userName = string.Empty;

        [ObservableProperty]
        private string fullName = string.Empty;

        [ObservableProperty]
        private string email = string.Empty;

        [ObservableProperty]
        private string phoneNumber = string.Empty;

        [ObservableProperty]
        private string address = string.Empty;

        [ObservableProperty]
        private DateTime birthday = DateTime.Now;

        [ObservableProperty]
        private bool canEditInventory = false;

        [ObservableProperty]
        private bool canEditPOSMenu = false;
        
        // Sales Report is access-only, not editable, so we just store it privately for updates
        private bool _canAccessSalesReport = false;

        [ObservableProperty]
        private int userId = 0;

        public void ShowUserProfile(UserInfoModel user) // Show profile from UserInfoModel
        {
            if (user == null) return;

            UserId = user.ID;
            UserName = $"{user.FirstName} {user.LastName}".Trim();
            FullName = $"{user.FirstName} {user.LastName}".Trim();
            Email = MaskEmail(user.Email);
            PhoneNumber = user.PhoneNumber;
            Address = user.Address;
            Birthday = user.Birthday;
            CanEditInventory = user.CanAccessInventory;
<<<<<<< Updated upstream
            CanEditPOSMenu = user.CanAccessSalesReport;
=======
            CanEditPOSMenu = user.CanAccessPOS;
            _canAccessSalesReport = user.CanAccessSalesReport; // Store for updates, but not editable
            
            // Load profile image from database
            ProfileImage = !string.IsNullOrWhiteSpace(user.ProfileImage) ? user.ProfileImage : "usericon.png";
            ProfileImageSource = GetProfileImageSource(ProfileImage);
>>>>>>> Stashed changes

            IsVisible = true;
        }

<<<<<<< Updated upstream
=======
        private ImageSource GetProfileImageSource(string imageName)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"GetProfileImageSource called with: {imageName}");
                
                if (string.IsNullOrWhiteSpace(imageName) || imageName == "usericon.png")
                {
                    System.Diagnostics.Debug.WriteLine("Using default usericon.png");
                    return ImageSource.FromFile("usericon.png");
                }

                // Check if it's a custom profile image in app data
                var appDataPath = System.IO.Path.Combine(Microsoft.Maui.Storage.FileSystem.AppDataDirectory, imageName);
                System.Diagnostics.Debug.WriteLine($"Checking for image at: {appDataPath}");
                
                if (System.IO.File.Exists(appDataPath))
                {
                    System.Diagnostics.Debug.WriteLine($"Custom profile image found at: {appDataPath}");
                    return ImageSource.FromFile(appDataPath);
                }

                System.Diagnostics.Debug.WriteLine($"Custom image not found, checking if it's a resource file");
                
                // Check if it's a bundled resource file (e.g., avatar1.png, avatar2.png)
                try
                {
                    var resourceImage = ImageSource.FromFile(imageName);
                    System.Diagnostics.Debug.WriteLine($"Using resource image: {imageName}");
                    return resourceImage;
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine($"Resource image not found, using default");
                }

                // Fallback to default user icon
                return ImageSource.FromFile("usericon.png");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetProfileImageSource: {ex.Message}");
                return ImageSource.FromFile("usericon.png");
            }
        }

>>>>>>> Stashed changes
        public void ShowUserProfile(UserEntry user) 
        {
            if (user == null) return;

            UserId = user.Id;
            UserName = user.Username;
            FullName = user.Username;
            Email = "N/A"; // UserEntry doesn't have email
            PhoneNumber = "N/A"; // UserEntry doesn't have phone
            Address = "N/A"; // UserEntry doesn't have address
            Birthday = DateTime.Now; // UserEntry doesn't have birthday
            CanEditInventory = user.CanAccessInventory;
            CanEditPOSMenu = user.CanAccessPOS;
            _canAccessSalesReport = user.CanAccessSalesReport; // Store for updates, but not editable

            IsVisible = true;
        }

        private string MaskEmail(string email) // Mask email for privacy
        {
            if (string.IsNullOrEmpty(email) || !email.Contains("@"))
                return email;

            var parts = email.Split('@');
            if (parts[0].Length <= 2)
                return email;

            var masked = parts[0][0] + new string('*', parts[0].Length - 2) + parts[0][^1];
            return $"{masked}@{parts[1]}";
        }

        [RelayCommand]
        private async Task ToggleInventoryPermission() // Toggle inventory permission
        {
            try
            {
                var newValue = !CanEditInventory;
                await _database.UpdateUserAccessAsync(UserId, newValue, CanEditPOSMenu, _canAccessSalesReport);
                CanEditInventory = newValue;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating inventory permission: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task TogglePOSMenuPermission() // Toggle POS menu permission
        {
            try
            {
                var newValue = !CanEditPOSMenu;
                await _database.UpdateUserAccessAsync(UserId, CanEditInventory, newValue, _canAccessSalesReport);
                CanEditPOSMenu = newValue;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating POS menu permission: {ex.Message}");
            }
        }

        [RelayCommand]
        private void Close() // Close the user profile popup
        {
            IsVisible = false;
        }
    }
}
