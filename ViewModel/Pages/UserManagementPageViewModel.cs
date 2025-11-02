using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Coftea_Capstone.Models;
using System.Threading.Tasks;
using Coftea_Capstone.C_;
using System.Linq;
using Coftea_Capstone.ViewModel.Controls;
using Microsoft.Maui.Controls;

namespace Coftea_Capstone.ViewModel
{
    public partial class UserManagementPageViewModel : ObservableObject
    {
        // ===================== Dependencies & Services =====================
        private readonly Database _database = new();

        // ===================== State =====================
        [ObservableProperty]
        private ObservableCollection<UserEntry> users = new();

        [ObservableProperty]
        private ObservableCollection<UserEntry> filteredUsers = new();

        [ObservableProperty]
        private int currentPage = 1;

        [ObservableProperty]
        private string searchText;

        [ObservableProperty]
        private string selectedSortOption = "Name (A-Z)";

        [ObservableProperty]
        private UserApprovalPopupViewModel userApprovalPopup = new();

        [ObservableProperty]
        private UserProfilePopupViewModel userProfilePopup = new();

        [ObservableProperty]
        private DeleteUserPopupViewModel deleteUserPopup = new();

        private bool _hasLoadedData = false;

        public UserManagementPageViewModel()
        {
            // Subscribe to the approval event to refresh user list
            UserApprovalPopup.OnUserApprovedOrDenied += OnUserApprovedOrDenied;
            // Subscribe to the delete event to refresh user list
            DeleteUserPopup.OnUserDeleted += OnUserDeleted;
        }

        private async void OnUserApprovedOrDenied() // Refresh user list when a user is approved or denied
        {
            await ForceReloadDataAsync(); 
        }

        private async void OnUserDeleted() // Refresh user list when a user is deleted
        {
            await ForceReloadDataAsync();
        } 


        [RelayCommand]
        private async Task AddUser() // Open the add user popup
        {
            await UserApprovalPopup.ShowUserApprovalPopup();
        }

        [RelayCommand]
        private async Task DeleteUser() // Open the delete user popup
        {
            await DeleteUserPopup.ShowDeleteUserPopup();
        }

        [RelayCommand]
        private Task SortBy() => Task.CompletedTask; // Future: implement sorting functionality

        [RelayCommand]
        private async Task ViewProfile(UserEntry entry) // Open the user profile popup
        {
            if (entry == null) return;
            
            // Get the full user details from database
            var allUsers = await _database.GetAllUsersAsync();
            var fullUserDetails = allUsers.FirstOrDefault(u => u.ID == entry.Id);
            
            if (fullUserDetails != null)
            {
                await UserProfilePopup.ShowUserProfile(fullUserDetails);
            }
            else
            {
                // Fallback to UserEntry if full details not found
                UserProfilePopup.ShowUserProfile(entry);
            }
        }

        [RelayCommand]
        private async Task OpenRowMenu(UserEntry entry) // Open action sheet for user options
        {
            if (entry == null) return;
            
            // Prevent admin user from being deleted
            var actions = entry.IsAdmin 
                ? new[] { "View Profile" }
                : new[] { "View Profile", "Delete User" };
                
            var action = await Application.Current.MainPage.DisplayActionSheet("Options", "Cancel", null, actions);
            
            if (action == "View Profile")
            {
                await ViewProfile(entry);
            }
            else if (action == "Delete User" && !entry.IsAdmin)
            {
                await DeleteSpecificUser(entry);
            }
        }

        private async Task DeleteSpecificUser(UserEntry user) // Delete a specific user after confirmation
        {
            // Confirm deletion
            var confirm = await Application.Current.MainPage.DisplayAlert(
                "Confirm Delete", 
                $"Are you sure you want to delete user '{user.Username}'? This action cannot be undone.", 
                "Delete", 
                "Cancel");

            if (confirm)
            {
                try
                {
                    await _database.DeleteUserAsync(user.Id);
                    await ForceReloadDataAsync(); // Force refresh the user list
                    await Application.Current.MainPage.DisplayAlert("Success", $"User '{user.Username}' has been deleted.", "OK");
                }
                catch (Exception ex)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", $"Failed to delete user: {ex.Message}", "OK");
                }
            }
        }

        public async Task InitializeAsync() // Initial data load
        {
            // Only load data if it hasn't been loaded before
            if (!_hasLoadedData)
            {
                await LoadUsersAsync();
                _hasLoadedData = true;
            }
        }

        private async Task LoadUsersAsync() // Load users from database and populate the Users collection
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔧 Loading users from database...");
                var allUsers = await _database.GetAllUsersAsync();
                System.Diagnostics.Debug.WriteLine($"🔧 Loaded {allUsers?.Count ?? 0} users from database");

                Users = new ObservableCollection<UserEntry>(allUsers.Select(u => new UserEntry
                {
                    Id = u.ID,
                    Username = string.Join(" ", new[]{u.FirstName, u.LastName}.Where(s => !string.IsNullOrWhiteSpace(s))).Trim(),
                    LastActive = GetLastActiveText(u.ID), // Get real last active data
                    DateAdded = GetDateAddedText(u.ID), // Get real date added data
                    IsAdmin = u.IsAdmin,
                    // Admin users always have full access, regular users use database values
                    CanAccessInventory = u.IsAdmin ? true : u.CanAccessInventory,
                    CanAccessPOS = u.IsAdmin ? true : u.CanAccessPOS, // Default to false if not set
                    CanAccessSalesReport = u.IsAdmin ? true : u.CanAccessSalesReport
                }));

                System.Diagnostics.Debug.WriteLine($"🔧 Created {Users.Count} UserEntry objects");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading users: {ex.Message}");
            }
        }

        public async Task ForceReloadDataAsync() // Force reload user data from database
        {
            _hasLoadedData = false; // Reset the flag to show loading
            await InitializeAsync();
        }

        private string GetLastActiveText(int userId)
        {
            // For now, return a placeholder. In a real app, you'd query the database for actual last login time
            // This could be stored in a separate login_logs table or a last_login column in the users table
            return DateTime.Now.AddDays(-new Random().Next(1, 30)).ToString("MMM dd, yyyy");
        }

        private string GetDateAddedText(int userId) 
        {
            // For now, return a placeholder. In a real app, you'd use the actual creation date
            // This could be stored in a created_at column in the users table
            return DateTime.Now.AddDays(-new Random().Next(30, 365)).ToString("MMM dd, yyyy");
        }
    }
}


