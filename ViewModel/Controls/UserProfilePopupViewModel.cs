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

        [ObservableProperty]
        private int userId = 0;

        [ObservableProperty]
        private string profileImage = "usericon.png";

        [ObservableProperty]
        private ImageSource profileImageSource = "usericon.png";

        public void ShowUserProfile(UserInfoModel user) // Show profile from UserInfoModel
        {
            if (user == null) return;

            UserId = user.ID;
            
            // Use Username if available, otherwise use FirstName + LastName
            UserName = !string.IsNullOrWhiteSpace(user.Username) ? user.Username : $"{user.FirstName} {user.LastName}".Trim();
            FullName = $"{user.FirstName} {user.LastName}".Trim();
            Email = MaskEmail(user.Email);
            PhoneNumber = user.PhoneNumber;
            Address = user.Address;
            Birthday = user.Birthday;
            CanEditInventory = user.CanAccessInventory;
            CanEditPOSMenu = user.CanAccessSalesReport;
            
            // Load profile image from database
            ProfileImage = !string.IsNullOrWhiteSpace(user.ProfileImage) ? user.ProfileImage : "usericon.png";
            ProfileImageSource = GetProfileImageSource(ProfileImage);

            IsVisible = true;
        }

        private ImageSource GetProfileImageSource(string imageName)
        {
            if (string.IsNullOrWhiteSpace(imageName))
                return "usericon.png";

            return ImageSource.FromFile(imageName);
        }

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
            CanEditPOSMenu = user.CanAccessSalesReport;

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
                await _database.UpdateUserAccessAsync(UserId, newValue, CanEditPOSMenu);
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
                await _database.UpdateUserAccessAsync(UserId, CanEditInventory, newValue);
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
