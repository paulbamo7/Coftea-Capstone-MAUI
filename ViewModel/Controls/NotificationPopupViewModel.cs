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
        // Badge count for notification bell
        [ObservableProperty] private int notificationCount = 0;
        // Expanded state for showing more notifications
        [ObservableProperty] private bool isExpanded = false;

        public NotificationPopupViewModel()
        {
            // Load stored notifications on initialization
            _ = LoadStoredNotificationsAsync();
        }

        public void ShowNotification(string message, string type = "Success", bool autoHide = true, int delay = 2200) // 2.2 seconds
        {
            if (string.IsNullOrWhiteSpace(message))
                return;
                
            NotificationCount++;
        }

        public async Task AddSuccess(string title, string entityName, string idText) // Success popup
        {
            // Skip inventory/stock notifications (they have been disabled)
            if (title.Equals("Inventory", StringComparison.OrdinalIgnoreCase) ||
                entityName.Contains("Stock", StringComparison.OrdinalIgnoreCase) ||
                entityName.Contains("Listed Item", StringComparison.OrdinalIgnoreCase) ||
                entityName.Contains("Updated Stock", StringComparison.OrdinalIgnoreCase))
            {
                System.Diagnostics.Debug.WriteLine($"📝 Skipping inventory notification: {title} - {entityName}");
                return;
            }
            
            var notification = new NotificationItem
            {
                Title = title,
                Message = $"Successfully {entityName}. {idText}",
                IdText = idText,
                Type = "Success",
                CreatedAt = DateTime.Now
            };
            
            Notifications.Insert(0, notification);
            
            // Keep last 20 in memory, but store more persistently
            while (Notifications.Count > 20)
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
            IsExpanded = false; // Reset expanded state when closing
        }


        [RelayCommand]
        private void ToggleExpand() // Toggle expanded state
        {
            IsExpanded = !IsExpanded;
        }
        public async Task LoadStoredNotificationsAsync() // Load notifications from storage
        {
            try
            {
                var storedNotifications = await _storageService.LoadNotificationsAsync();
                if (storedNotifications != null && storedNotifications.Any())
                {
                    // Clear current notifications and load stored ones
                    Notifications.Clear();
                    
                    // Filter out inventory/stock notifications
                    var filteredNotifications = storedNotifications.Where(n => 
                        !n.Title.Equals("Inventory", StringComparison.OrdinalIgnoreCase) &&
                        !n.Title.Contains("Inventory", StringComparison.OrdinalIgnoreCase) &&
                        !n.Message.Contains("Stock", StringComparison.OrdinalIgnoreCase) &&
                        !n.Message.Contains("Listed Item", StringComparison.OrdinalIgnoreCase) &&
                        !n.Message.Contains("Updated Stock", StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                    
                    foreach (var notification in filteredNotifications)
                    {
                        Notifications.Add(notification);
                    }
                    
                    // Update notification count
                    NotificationCount = Notifications.Count;
                    
                    // If we filtered any out, save the filtered list back to storage
                    if (filteredNotifications.Count < storedNotifications.Count)
                    {
                        await SaveNotificationsAsync();
                    }
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
            // Skip inventory/stock notifications (they have been disabled)
            if (title.Contains("Inventory", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("Stock", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("Listed Item", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("Updated Stock", StringComparison.OrdinalIgnoreCase))
            {
                System.Diagnostics.Debug.WriteLine($"📝 Skipping inventory notification: {title} - {message}");
                return;
            }
            
            var notification = new NotificationItem
            {
                Title = title,
                Message = message,
                IdText = idText,
                Type = type,
                CreatedAt = DateTime.Now
            };
            
            Notifications.Insert(0, notification);
            
            // Keep last 20 in memory
            while (Notifications.Count > 20)
                Notifications.RemoveAt(Notifications.Count - 1);
            
            NotificationCount++;
            
            // Save to storage
            await SaveNotificationsAsync();
        }
    }
}
