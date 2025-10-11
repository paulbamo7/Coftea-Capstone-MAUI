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
        [ObservableProperty] private string notificationType = "Success"; // Success, Error, Info
        [ObservableProperty] private bool isAutoHide = true;
        [ObservableProperty] private int autoHideDelay = 3000; // 3 seconds
        // Badge count for notification bell
        [ObservableProperty] private int notificationCount = 0;

        public NotificationPopupViewModel()
        {
            // Load stored notifications on initialization
            _ = LoadStoredNotificationsAsync();
        }

        public void ShowNotification(string message, string type = "Success", bool autoHide = true, int delay = 3000)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;
                
            NotificationMessage = message;
            NotificationType = type;
            IsAutoHide = autoHide;
            AutoHideDelay = delay;
            NotificationCount++;
        }

        public async Task AddSuccess(string title, string entityName, string idText)
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
        private void Toggle()
        {
            IsNotificationVisible = !IsNotificationVisible;
            if (IsNotificationVisible)
            {
                // Clear badge when the user views notifications
                NotificationCount = 0;
            }
        }

        // Convenience for short-lived toast on bottom-left
        public void ShowToast(string message, int milliseconds = 1500)
        {
            ShowNotification(message, "Success", true, milliseconds);
        }

        [RelayCommand]
        private void CloseNotification()
        {
            IsNotificationVisible = false;
        }

        [RelayCommand]
        private void HideNotification()
        {
            IsNotificationVisible = false;
        }

        // Storage methods
        private async Task LoadStoredNotificationsAsync()
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

        private async Task SaveNotificationsAsync()
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
        private async Task ClearAllNotifications()
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
        public async Task AddNotification(string title, string message, string idText = "", string type = "Info")
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

        // Convenience methods for different notification types
        public async Task AddError(string title, string message, string idText = "")
        {
            await AddNotification(title, message, idText, "Error");
        }

        public async Task AddWarning(string title, string message, string idText = "")
        {
            await AddNotification(title, message, idText, "Warning");
        }

        public async Task AddInfo(string title, string message, string idText = "")
        {
            await AddNotification(title, message, idText, "Info");
        }

        // Method to mark notification as read
        public async Task MarkAsRead(NotificationItem notification)
        {
            if (notification != null)
            {
                notification.IsRead = true;
                await SaveNotificationsAsync();
            }
        }

        // Method to get unread notification count
        public int GetUnreadCount()
        {
            return Notifications.Count(n => !n.IsRead);
        }
    }
}
