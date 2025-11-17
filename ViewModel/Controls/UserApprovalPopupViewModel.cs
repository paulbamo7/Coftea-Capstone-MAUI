using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Coftea_Capstone.Models;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Coftea_Capstone.C_;
using Coftea_Capstone.Services;

namespace Coftea_Capstone.ViewModel.Controls
{
    public partial class UserApprovalPopupViewModel : ObservableObject
    {
        private readonly Database _database = new();
        private readonly EmailService _emailService;

        [ObservableProperty]
        private bool isUserApprovalPopupVisible = false;

        [ObservableProperty]
        private ObservableCollection<UserPendingRequest> pendingRequests = new();

        public event Action OnUserApprovedOrDenied;

        public UserApprovalPopupViewModel()
        {
            _emailService = new EmailService();
        }

        [RelayCommand]
        private async Task CloseUserApprovalPopup()
        {
            IsUserApprovalPopupVisible = false;
        }

        [RelayCommand]
        public async Task ApproveRequest(UserPendingRequest request) // Approve a pending user registration
        {
            try
            {
                if (!UserSession.Instance.IsAdmin)
                {
                    await Application.Current.MainPage.DisplayAlert("Unauthorized", "Only admins can approve user registrations.", "OK");
                    return;
                }
                int result = await _database.ApprovePendingRegistrationAsync(request.ID);
                if (result > 0)
                {
                    await Application.Current.MainPage.DisplayAlert("Success", $"User {request.FirstName} {request.LastName} approved successfully!", "OK");
                    await LoadPendingRequests();
                    OnUserApprovedOrDenied?.Invoke(); // Notify parent to refresh
                    
                    // Add notification
                    var app = (App)Application.Current;
                    await app?.NotificationPopup?.AddNotification(
                        "User Approved",
                        $"User {request.FirstName} {request.LastName} has been approved and added to the system",
                        $"Email: {request.Email}",
                        "Info");
                    
                    // Send approval email to the user
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"📧 Attempting to send approval email to: {request.Email}");
                        var emailSent = await _emailService.SendRegistrationSuccessEmailAsync(
                            request.Email,
                            request.FirstName,
                            request.LastName,
                            isAdmin: false
                        );
                        if (emailSent)
                        {
                            System.Diagnostics.Debug.WriteLine($"✅ Approval email sent successfully to {request.Email}!");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ Approval email send returned false (check logs above for details)");
                        }
                    }
                    catch (Exception emailEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Exception caught while sending approval email: {emailEx.Message}");
                        // Don't block approval if email fails
                    }
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Failed to approve request. Request may have already been processed.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to approve request: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        public async Task DenyRequest(UserPendingRequest request) // Deny a pending user registration
        {
            try
            {
                if (!UserSession.Instance.IsAdmin)
                {
                    await Application.Current.MainPage.DisplayAlert("Unauthorized", "Only admins can deny user registrations.", "OK");
                    return;
                }
                int result = await _database.RejectPendingRegistrationAsync(request.ID);
                if (result > 0)
                {
                    await Application.Current.MainPage.DisplayAlert("Success", $"User {request.FirstName} {request.LastName} denied.", "OK");
                    await LoadPendingRequests();
                    OnUserApprovedOrDenied?.Invoke(); // Notify parent to refresh
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Failed to deny request. Request may have already been processed.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to deny request: {ex.Message}", "OK");
            }
        }

        public async Task LoadPendingRequests() // Load pending user registration requests from the database
        {
            try
            {
                var requests = await _database.GetPendingRegistrationsAsync();
                PendingRequests = new ObservableCollection<UserPendingRequest>(requests);
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to load pending requests: {ex.Message}", "OK");
            }
        }

        public async Task ShowUserApprovalPopup() // Show the user approval popup
        {
            await LoadPendingRequests();
            IsUserApprovalPopupVisible = true;
        }
    }
}
