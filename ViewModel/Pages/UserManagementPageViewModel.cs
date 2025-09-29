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
        private async Task GrantAccess(UserEntry entry)
        {
            if (entry == null) return;
            // Toggle both access flags for simplicity; adjust per UI as needed
            var newInventory = !entry.CanAccessInventory;
            var newSales = !entry.CanAccessSalesReport;
            await _database.UpdateUserAccessAsync(entry.Id, newInventory, newSales);
            entry.CanAccessInventory = newInventory;
            entry.CanAccessSalesReport = newSales;
            OnPropertyChanged(nameof(Users));
        }

        [RelayCommand]
        private Task DeleteUser() => Task.CompletedTask;

        [RelayCommand]
        private Task SortBy() => Task.CompletedTask;

        [RelayCommand]
        private async Task ViewProfile(UserEntry entry)
        {
            if (entry == null) return;
            await Application.Current.MainPage.DisplayAlert("Details",
                $"Full Name: {entry.Username}\n\nPermissions:\n- Inventory: {(entry.CanAccessInventory ? "Yes" : "No")}\n- Sales Report: {(entry.CanAccessSalesReport ? "Yes" : "No")}",
                "Close");
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
                CanAccessInventory = u.CanAccessInventory,
                CanAccessSalesReport = u.CanAccessSalesReport
            }));
        }
    }
}


