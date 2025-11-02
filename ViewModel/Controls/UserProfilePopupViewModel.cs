using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Coftea_Capstone.Models;
using Coftea_Capstone.C_;
using Microsoft.Maui.Controls;

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
        private bool canEditInventory = false;

        [ObservableProperty]
        private bool canEditPOSMenu = false;
        
        // Sales Report is access-only, not editable, so we just store it privately for updates
        private bool _canAccessSalesReport = false;
        
        // Public property for displaying Sales Report access (read-only)
        [ObservableProperty]
        private bool canAccessSalesReport = false;

        [ObservableProperty]
        private int userId = 0;

        [ObservableProperty]
        private string profileImage = "usericon.png";

        [ObservableProperty]
        private ImageSource profileImageSource = "usericon.png";

        public async Task ShowUserProfile(UserInfoModel user) // Show profile from UserInfoModel
        {
            if (user == null) return;

            UserId = user.ID;
            
            // Use Username if available, otherwise use FirstName + LastName
            UserName = !string.IsNullOrWhiteSpace(user.Username) ? user.Username : $"{user.FirstName} {user.LastName}".Trim();
            FullName = $"{user.FirstName} {user.LastName}".Trim();
            Email = MaskEmail(user.Email);
            PhoneNumber = user.PhoneNumber;
            
            // If user is admin, set all permissions to true
            if (user.IsAdmin)
            {
                CanEditInventory = true;
                CanEditPOSMenu = true;
                CanAccessSalesReport = true;
                _canAccessSalesReport = true;
            }
            else
            {
                CanEditInventory = user.CanAccessInventory;
                CanEditPOSMenu = user.CanAccessPOS;
                _canAccessSalesReport = user.CanAccessSalesReport; // Store for updates, but not editable
                CanAccessSalesReport = user.CanAccessSalesReport; // For display
            }
            
            // Explicitly notify property changes to ensure DataTriggers fire
            OnPropertyChanged(nameof(CanEditInventory));
            OnPropertyChanged(nameof(CanEditPOSMenu));
            OnPropertyChanged(nameof(CanAccessSalesReport));
            
            // Notify computed properties changed
            OnPropertyChanged(nameof(InventoryAccessText));
            OnPropertyChanged(nameof(POSAccessText));
            OnPropertyChanged(nameof(SalesReportAccessText));
            
            // Load profile image from database - always refresh from database to get latest
            try
            {
                var freshUser = await _database.GetUserByIdAsync(user.ID);
                var profileImageName = freshUser?.ProfileImage ?? user.ProfileImage;
                ProfileImage = !string.IsNullOrWhiteSpace(profileImageName) ? profileImageName : "usericon.png";
                
                System.Diagnostics.Debug.WriteLine($"ShowUserProfile - UserId: {UserId}, ProfileImage from DB: {profileImageName}, Using: {ProfileImage}");
                
                // Try to restore profile image from database if file is missing
                if (!string.IsNullOrWhiteSpace(ProfileImage) && ProfileImage != "usericon.png")
                {
                    var imagePath = System.IO.Path.Combine(Microsoft.Maui.Storage.FileSystem.AppDataDirectory, ProfileImage);
                    if (!System.IO.File.Exists(imagePath))
                    {
                        System.Diagnostics.Debug.WriteLine($"Profile image file missing, attempting to restore from database: {ProfileImage}");
                        await _database.GetUserProfileImageAsync(UserId);
                    }
                }
                
                // Clear first to force image reload, then set new source on main thread
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ProfileImageSource = null;
                    ProfileImageSource = GetProfileImageSource(ProfileImage);
                    System.Diagnostics.Debug.WriteLine($"ProfileImageSource set to: {ProfileImageSource}");
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error loading user profile image: {ex.Message}");
                // Fallback to provided user data
                ProfileImage = !string.IsNullOrWhiteSpace(user.ProfileImage) ? user.ProfileImage : "usericon.png";
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ProfileImageSource = null;
                    ProfileImageSource = GetProfileImageSource(ProfileImage);
                });
            }

            IsVisible = true;
        }

        private ImageSource GetProfileImageSource(string imageFileName)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"GetProfileImageSource called with: {imageFileName}");
                
                if (string.IsNullOrWhiteSpace(imageFileName) || imageFileName == "usericon.png")
                {
                    System.Diagnostics.Debug.WriteLine("Using default usericon.png");
                    return ImageSource.FromFile("usericon.png");
                }

                // Check if it's a custom profile image in app data directory (same as ProfilePopupViewModel)
                var appDataPath = System.IO.Path.Combine(Microsoft.Maui.Storage.FileSystem.AppDataDirectory, imageFileName);
                System.Diagnostics.Debug.WriteLine($"Checking for profile image at: {appDataPath}");
                
                if (System.IO.File.Exists(appDataPath))
                {
                    System.Diagnostics.Debug.WriteLine($"Custom profile image found at: {appDataPath}");
                    return ImageSource.FromFile(appDataPath);
                }

                System.Diagnostics.Debug.WriteLine($"Profile image not found at {appDataPath}, checking if it's a resource file");
                
                // Check if it's a bundled resource file (e.g., avatar1.png, avatar2.png, or usericon.png)
                try
                {
                    var resourceImage = ImageSource.FromFile(imageFileName);
                    System.Diagnostics.Debug.WriteLine($"Using resource image: {imageFileName}");
                    return resourceImage;
                }
                catch (Exception resourceEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Resource image not found: {resourceEx.Message}");
                }

                // Fallback to default user icon
                System.Diagnostics.Debug.WriteLine($"Falling back to default usericon.png");
                return ImageSource.FromFile("usericon.png");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetProfileImageSource: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return ImageSource.FromFile("usericon.png");
            }
        }

        public void ShowUserProfile(UserEntry user) 
        {
            if (user == null) return;

            UserId = user.Id;
            UserName = user.Username;
            FullName = user.Username;
            Email = "N/A"; // UserEntry doesn't have email
            PhoneNumber = "N/A"; // UserEntry doesn't have phone
            
            // If user is admin, set all permissions to true
            if (user.IsAdmin)
            {
                CanEditInventory = true;
                CanEditPOSMenu = true;
                CanAccessSalesReport = true;
                _canAccessSalesReport = true;
            }
            else
            {
                CanEditInventory = user.CanAccessInventory;
                CanEditPOSMenu = user.CanAccessPOS;
                _canAccessSalesReport = user.CanAccessSalesReport; // Store for updates, but not editable
                CanAccessSalesReport = user.CanAccessSalesReport; // For display
            }
            
            // Explicitly notify property changes to ensure DataTriggers fire
            OnPropertyChanged(nameof(CanEditInventory));
            OnPropertyChanged(nameof(CanEditPOSMenu));
            OnPropertyChanged(nameof(CanAccessSalesReport));
            
            // Notify computed properties changed
            OnPropertyChanged(nameof(InventoryAccessText));
            OnPropertyChanged(nameof(POSAccessText));
            OnPropertyChanged(nameof(SalesReportAccessText));

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

        // Computed properties for display
        public string InventoryAccessText => CanEditInventory ? "Can Edit Inventory: Yes" : "Can Edit Inventory: No";
        public string POSAccessText => CanEditPOSMenu ? "Can Edit POS Menu: Yes" : "Can Edit POS Menu: No";
        public string SalesReportAccessText => CanAccessSalesReport ? "Can Access Sales Report: Yes" : "Can Access Sales Report: No";
        
        // Partial methods to notify computed properties when base properties change
        partial void OnCanEditInventoryChanged(bool value)
        {
            OnPropertyChanged(nameof(InventoryAccessText));
        }
        
        partial void OnCanEditPOSMenuChanged(bool value)
        {
            OnPropertyChanged(nameof(POSAccessText));
        }
        
        partial void OnCanAccessSalesReportChanged(bool value)
        {
            OnPropertyChanged(nameof(SalesReportAccessText));
        }

        [RelayCommand]
        private void Close() // Close the user profile popup
        {
            IsVisible = false;
        }
    }
}
