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
        private async Task CreateTestPendingRequest()
        {
            try
            {
                var testRequest = new UserPendingRequest
                {
                    Email = "test@example.com",
                    Password = "hashedpassword",
                    FirstName = "Test",
                    LastName = "User",
                    PhoneNumber = "123-456-7890",
                    Address = "123 Test St",
                    Birthday = DateTime.Now.AddYears(-25),
                    RequestDate = DateTime.Now
                };

                await _database.AddPendingUserRequestAsync(testRequest);
                await Application.Current.MainPage.DisplayAlert("Success", "Test pending request created!", "OK");
                await UserApprovalPopup.LoadPendingRequests();
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to create test request: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private Task DeleteUser() => Task.CompletedTask;

        [RelayCommand]
        private Task SortBy() => Task.CompletedTask;

        public async Task InitializeAsync()
        {
            var allUsers = await _database.GetAllUsersAsync();
            Users = new ObservableCollection<UserEntry>(allUsers.Select(u => new UserEntry
            {
                Username = string.Join(" ", new[]{u.FirstName, u.LastName}.Where(s => !string.IsNullOrWhiteSpace(s))).Trim(),
                LastActive = "—",
                DateAdded = "—",
                CanEditInventory = false,
                CanEditPOS = false,
                CanEditBalanceSheet = false
            }));
        }
    }
}


