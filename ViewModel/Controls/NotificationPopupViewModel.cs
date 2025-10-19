using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Coftea_Capstone.Services;

namespace Coftea_Capstone.ViewModel.Controls
{
    public partial class NotificationPopupViewModel : ObservableObject
    {
        private readonly NotificationStorageService _storageService = new NotificationStorageService();
        
        public ObservableCollection<NotificationItem> Notifications { get; } = new();
        [ObservableProperty] private bool isNotificationVisible = false;
        [ObservableProperty] private string notificationMessage = "";
        [ObservableProperty] private string notificationType = "Success"; // Success, Error, Info, Warning
        [ObservableProperty] private bool isAutoHide = true;
        [ObservableProperty] private int autoHideDelay = 3000; // 3 seconds
        // Badge count for notification bell
        [ObservableProperty] private int notificationCount = 0;

        public NotificationPopupViewModel()
        {
            // Load stored notifications on initialization
            _ = LoadStoredNotificationsAsync();
        }

        public void ShowNotification(string message, string type = "Success", bool autoHide = true, int delay = 2200) // 2.2 seconds
        {
            if (string.IsNullOrWhiteSpace(message))
                return;
                
            NotificationMessage = message;
            NotificationType = type;
            IsAutoHide = autoHide;
            AutoHideDelay = delay;
            NotificationCount++;
        }

        public async Task AddSuccess(string title, string entityName, string idText) // Success popup
        {
            var notification = new NotificationItem
            {
                Title = title,
                Message = $"Successfully {entityName}. {idText}",
                IdText = idText,
                Type = "Success",
                CreatedAt = DateTime.Now
            };
            
            Notifications.Insert(0, notification);
            
            // Keep last 10 in memory, but store more persistently
            while (Notifications.Count > 10)
                Notifications.RemoveAt(Notifications.Count - 1);
            
            NotificationCount++;
            
            // Save to storage
            await SaveNotificationsAsync();
        }

        [RelayCommand]
        private void Toggle() // Toggle notification popup visibility
        {
            IsNotificationVisible = !IsNotificationVisible;
            if (IsNotificationVisible)
            {
                // Clear badge when the user views notifications
                NotificationCount = 0;
            }
        }

        [RelayCommand]
        private void CloseNotification() // Close notification popup
        {
            IsNotificationVisible = false;
        }

        [RelayCommand]
        private void HideNotification() // Hide notification popup
        {
            IsNotificationVisible = false;
        }
        private async Task LoadStoredNotificationsAsync() // Load notifications from storage
        {
            try
            {
                var storedNotifications = await _storageService.LoadNotificationsAsync();
                if (storedNotifications != null && storedNotifications.Any())
                {
                    // Clear current notifications and load stored ones
                    Notifications.Clear();
                    foreach (var notification in storedNotifications)
                    {
                        Notifications.Add(notification);
                    }
                    
                    // Update notification count
                    NotificationCount = Notifications.Count;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading stored notifications: {ex.Message}");
            }
        }

        private async Task SaveNotificationsAsync() // Save notifications to storage
        {
            try
            {
                await _storageService.SaveNotificationsAsync(Notifications);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving notifications: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task ClearAllNotifications() // Clear all notifications
        {
            try
            {
                Notifications.Clear();
                NotificationCount = 0;
                await _storageService.ClearNotificationsAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing notifications: {ex.Message}");
            }
        }

        // Method to add any type of notification with storage
        public async Task AddNotification(string title, string message, string idText = "", string type = "Info") // type: Info, Warning, Error
        {
            var notification = new NotificationItem
            {
                Title = title,
                Message = message,
                IdText = idText,
                Type = type,
                CreatedAt = DateTime.Now
            };
            
            Notifications.Insert(0, notification);
            
            // Keep last 10 in memory
            while (Notifications.Count > 10)
                Notifications.RemoveAt(Notifications.Count - 1);
            
            NotificationCount++;
            
            // Save to storage
            await SaveNotificationsAsync();
        }
    }
}
