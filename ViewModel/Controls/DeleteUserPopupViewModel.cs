using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Coftea_Capstone.Models;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Coftea_Capstone.C_;

namespace Coftea_Capstone.ViewModel.Controls
{
    public partial class DeleteUserPopupViewModel : ObservableObject
    {
        private readonly Database _database = new();

        [ObservableProperty]
        private bool isDeleteUserPopupVisible = false;

        [ObservableProperty]
        private ObservableCollection<UserEntry> users = new();

        public event Action OnUserDeleted;

        [RelayCommand]
        private async Task CloseDeleteUserPopup()
        {
            IsDeleteUserPopupVisible = false;
        }

        [RelayCommand]
        public async Task DeleteUser(UserEntry user) // Delete the specified user
        {
            try
            {
                if (!UserSession.Instance.IsAdmin)
                {
                    await Application.Current.MainPage.DisplayAlert("Unauthorized", "Only admins can delete users.", "OK");
                    return;
                }

                // Prevent deleting the first user (primary admin)
                if (user.Id == 1)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Cannot Delete", 
                        "Cannot delete the primary admin account (first user). This account is protected.", 
                        "OK");
                    return;
                }

                // Prevent deleting admin users
                if (user.IsAdmin)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Cannot Delete", 
                        "Cannot delete admin users. Please revoke admin privileges first.", 
                        "OK");
                    return;
                }

                // Confirm deletion
                var confirm = await Application.Current.MainPage.DisplayAlert(
                    "Confirm Delete", 
                    $"Are you sure you want to delete user '{user.Username}'? This action cannot be undone.", 
                    "Delete", 
                    "Cancel");

                if (confirm)
                {
                    int result = await _database.DeleteUserAsync(user.Id);
                    if (result > 0)
                    {
                        await Application.Current.MainPage.DisplayAlert("Success", $"User '{user.Username}' has been deleted.", "OK");
                        await LoadUsers();
                        OnUserDeleted?.Invoke(); // Notify parent to refresh
                    }
                    else
                    {
                        await Application.Current.MainPage.DisplayAlert("Error", "Failed to delete user. User may have already been deleted.", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to delete user: {ex.Message}", "OK");
            }
        }

        public async Task LoadUsers() // Load users from database
        {
            try
            {
                var allUsers = await _database.GetAllUsersAsync();
                Users = new ObservableCollection<UserEntry>(allUsers.Select(u => new UserEntry
                {
                    Id = u.ID,
                    Username = string.Join(" ", new[]{u.FirstName, u.LastName}.Where(s => !string.IsNullOrWhiteSpace(s))).Trim(),
                    LastActive = GetLastActiveText(u.ID),
                    DateAdded = GetDateAddedText(u.ID),
                    IsAdmin = u.IsAdmin, // Set IsAdmin property
                    CanAccessInventory = u.ID == 1 ? true : u.CanAccessInventory,
                    CanAccessSalesReport = u.ID == 1 ? true : u.CanAccessSalesReport
                }));
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to load users: {ex.Message}", "OK");
            }
        }

        private string GetLastActiveText(int userId) 
        {
            // For now, return a placeholder. In a real app, you'd query the database for actual last login time
            return DateTime.Now.AddDays(-new Random().Next(1, 30)).ToString("MMM dd, yyyy");
        }

        private string GetDateAddedText(int userId)
        {
            // For now, return a placeholder. In a real app, you'd use the actual creation date
            return DateTime.Now.AddDays(-new Random().Next(30, 365)).ToString("MMM dd, yyyy");
        }

        public async Task ShowDeleteUserPopup()
        {
            await LoadUsers();
            IsDeleteUserPopupVisible = true;
        }
    }
}
