using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Coftea_Capstone.Models;
using System.Threading.Tasks;
using Coftea_Capstone.C_;
using System.Linq;
using Microsoft.Maui.Controls;

namespace Coftea_Capstone.ViewModel
{
    public partial class UserManagementPageViewModel : ObservableObject
    {
        private readonly Database _database = new();

        [ObservableProperty]
        private ObservableCollection<UserEntry> users = new();

        [ObservableProperty]
        private ObservableCollection<PendingRegistrationModel> pendingRegistrations = new();

        [ObservableProperty]
        private int currentPage = 1;

        [ObservableProperty]
        private string searchText;

        [ObservableProperty]
        private bool showPendingUsers = true;

        [ObservableProperty]
        private bool isPendingUsersPopupVisible = false;

        [ObservableProperty]
        private bool isDeleteUserPopupVisible = false;

        [ObservableProperty]
        private UserEntry selectedUser;

        [RelayCommand]
        private async Task AddUser()
        {
            IsPendingUsersPopupVisible = true;
            await LoadPendingRegistrationsAsync();
        }

        [RelayCommand]
        private void ClosePendingUsersPopup()
        {
            IsPendingUsersPopupVisible = false;
        }

        [RelayCommand]
        private void DeleteUser()
        {
            // For now, just show the popup. In a real implementation, you'd select a user first
            IsDeleteUserPopupVisible = true;
        }

        [RelayCommand]
        private void CloseDeleteUserPopup()
        {
            IsDeleteUserPopupVisible = false;
        }

        [RelayCommand]
        private async Task ConfirmDeleteUser()
        {
            if (SelectedUser == null) return;

            try
            {
                // Add your delete logic here
                await Application.Current.MainPage.DisplayAlert("Success", "User deleted successfully!", "OK");
                IsDeleteUserPopupVisible = false;
                await LoadAllUsersAsync(); // Refresh the user list
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to delete user: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private Task SortBy() => Task.CompletedTask;

        [RelayCommand]
        private async Task ApproveUser(PendingRegistrationModel registration)
        {
            if (registration == null) return;

            bool confirm = await Application.Current.MainPage.DisplayAlert(
                "Approve Registration", 
                $"Are you sure you want to approve {registration.FirstName} {registration.LastName} ({registration.Email})?", 
                "Yes", "No");

            if (!confirm) return;

            try
            {
                await _database.ApprovePendingRegistrationAsync(registration.ID);
                await Application.Current.MainPage.DisplayAlert("Success", "Registration approved successfully! User can now log in.", "OK");
                await LoadPendingRegistrationsAsync();
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to approve registration: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task RejectUser(PendingRegistrationModel registration)
        {
            if (registration == null) return;

            bool confirm = await Application.Current.MainPage.DisplayAlert(
                "Reject Registration", 
                $"Are you sure you want to reject {registration.FirstName} {registration.LastName} ({registration.Email})?", 
                "Yes", "No");

            if (!confirm) return;

            try
            {
                await _database.RejectPendingRegistrationAsync(registration.ID);
                await Application.Current.MainPage.DisplayAlert("Success", "Registration rejected successfully!", "OK");
                await LoadPendingRegistrationsAsync();
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to reject registration: {ex.Message}", "OK");
            }
        }

        public async Task InitializeAsync()
        {
            await LoadAllUsersAsync();
            await LoadPendingRegistrationsAsync();
        }

        private async Task LoadAllUsersAsync()
        {
            var allUsers = await _database.GetAllUsersAsync();
            Users = new ObservableCollection<UserEntry>(allUsers.Select(u => new UserEntry
            {
                UserId = u.ID,
                Username = string.Join(" ", new[]{u.FirstName, u.LastName}.Where(s => !string.IsNullOrWhiteSpace(s))).Trim(),
                Email = u.Email,
                Status = u.Status,
                LastActive = "—",
                DateAdded = "—",
                CanEditInventory = false,
                CanEditPOS = false,
                CanEditBalanceSheet = false
            }));
        }

        private async Task LoadPendingRegistrationsAsync()
        {
            var pendingRegistrationsList = await _database.GetPendingRegistrationsAsync();
            PendingRegistrations = new ObservableCollection<PendingRegistrationModel>(pendingRegistrationsList);
        }
    }
}


