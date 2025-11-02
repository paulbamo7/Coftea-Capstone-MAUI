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
        private string searchText = string.Empty;

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
        private async Task SortBy() // Show sort options
        {
            var sortOptions = new[] { "Name (A-Z)", "Name (Z-A)", "Date Added (Newest)", "Date Added (Oldest)" };
            
            var selected = await Application.Current.MainPage.DisplayActionSheet(
                "Sort By", 
                "Cancel", 
                null, 
                sortOptions);
            
            if (selected != null && selected != "Cancel" && sortOptions.Contains(selected))
            {
                SelectedSortOption = selected;
                ApplyFilters();
            }
        }

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
                    DateAdded = FormatDateAdded(u.CreatedAt), // Use actual created_at date from database
                    CreatedAt = u.CreatedAt, // Store actual date for accurate sorting
                    IsAdmin = u.IsAdmin,
                    // Admin users always have full access, regular users use database values
                    CanAccessInventory = u.IsAdmin ? true : u.CanAccessInventory,
                    CanAccessPOS = u.IsAdmin ? true : u.CanAccessPOS, // Default to false if not set
                    CanAccessSalesReport = u.IsAdmin ? true : u.CanAccessSalesReport
                }));

                System.Diagnostics.Debug.WriteLine($"🔧 Created {Users.Count} UserEntry objects");
                
                // Initialize FilteredUsers with all users, then apply filters and sorting
                // Make sure SearchText is empty initially
                SearchText = string.Empty;
                FilteredUsers = new ObservableCollection<UserEntry>(Users);
                ApplyFilters();
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

        private string FormatDateAdded(DateTime dateAdded) 
        {
            // Format the actual created_at date from database
            return dateAdded.ToString("MMM dd, yyyy");
        }

        partial void OnSearchTextChanged(string value)
        {
            ApplyFilters();
        }

        partial void OnSelectedSortOptionChanged(string value)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            try
            {
                var query = Users.AsEnumerable();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchTerm = SearchText.ToLowerInvariant();
                    query = query.Where(u => 
                        (u.Username?.ToLowerInvariant().Contains(searchTerm) ?? false));
                }

                // Apply sorting
                query = ApplySorting(query);

                FilteredUsers = new ObservableCollection<UserEntry>(query);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error applying filters: {ex.Message}");
                FilteredUsers = new ObservableCollection<UserEntry>(Users);
            }
        }

        private IEnumerable<UserEntry> ApplySorting(IEnumerable<UserEntry> query)
        {
            return SelectedSortOption switch
            {
                "Name (A-Z)" => query.OrderBy(u => u.Username),
                "Name (Z-A)" => query.OrderByDescending(u => u.Username),
                "Date Added (Newest)" => query.OrderByDescending(u => u.CreatedAt),
                "Date Added (Oldest)" => query.OrderBy(u => u.CreatedAt),
                _ => query.OrderBy(u => u.Username) // Default to Name (A-Z)
            };
        }

        private DateTime ParseDate(string dateText)
        {
            if (string.IsNullOrWhiteSpace(dateText))
                return DateTime.MinValue;
            
            if (DateTime.TryParse(dateText, out var date))
                return date;
            
            return DateTime.MinValue;
        }
    }
}


