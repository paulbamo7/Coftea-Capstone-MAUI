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
        private readonly Database _database = new();

        [ObservableProperty]
        private ObservableCollection<UserEntry> users = new();

        [ObservableProperty]
        private int currentPage = 1;

        [ObservableProperty]
        private string searchText;

        [ObservableProperty]
        private UserApprovalPopupViewModel userApprovalPopup = new();

        [ObservableProperty]
        private UserProfilePopupViewModel userProfilePopup = new();

        public UserManagementPageViewModel()
        {
            // Subscribe to the approval event to refresh user list
            UserApprovalPopup.OnUserApprovedOrDenied += OnUserApprovedOrDenied;
        }

        private async void OnUserApprovedOrDenied()
        {
            await InitializeAsync(); // Refresh the user list
        }

        [RelayCommand]
        private async Task AddUser()
        {
            await UserApprovalPopup.ShowUserApprovalPopup();
        }

        [RelayCommand]
        private Task DeleteUser() => Task.CompletedTask;

        [RelayCommand]
        private Task SortBy() => Task.CompletedTask;

        [RelayCommand]
        private async Task ViewProfile(UserEntry entry)
        {
            if (entry == null) return;
            
            // Get the full user details from database
            var allUsers = await _database.GetAllUsersAsync();
            var fullUserDetails = allUsers.FirstOrDefault(u => u.ID == entry.Id);
            
            if (fullUserDetails != null)
            {
                UserProfilePopup.ShowUserProfile(fullUserDetails);
            }
            else
            {
                // Fallback to UserEntry if full details not found
                UserProfilePopup.ShowUserProfile(entry);
            }
        }

        [RelayCommand]
        private async Task OpenRowMenu(UserEntry entry)
        {
            if (entry == null) return;
            var action = await Application.Current.MainPage.DisplayActionSheet("Options", "Cancel", null, "View Profile", "Edit Inventory", "Edit POS Menu");
            if (action == "View Profile")
            {
                await ViewProfile(entry);
            }
            // Future: handle other actions here
        }

        public async Task InitializeAsync()
        {
            var allUsers = await _database.GetAllUsersAsync();
            Users = new ObservableCollection<UserEntry>(allUsers.Select(u => new UserEntry
            {
                Id = u.ID,
                Username = string.Join(" ", new[]{u.FirstName, u.LastName}.Where(s => !string.IsNullOrWhiteSpace(s))).Trim(),
                LastActive = "—",
                DateAdded = "—",
                // Admin users (ID = 1) always have full access, regular users use database values
                CanAccessInventory = u.ID == 1 ? true : u.CanAccessInventory,
                CanAccessSalesReport = u.ID == 1 ? true : u.CanAccessSalesReport
            }));
        }
    }
}


