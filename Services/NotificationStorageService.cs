using System.Collections.ObjectModel;
using System.Text.Json;
using Microsoft.Maui.Storage;
using Coftea_Capstone.ViewModel.Controls;

namespace Coftea_Capstone.Services
{
    public class NotificationStorageService
    {
        private const string NotificationsFileName = "notifications.json";
        private const int MaxStoredNotifications = 50; // Store up to 50 notifications

        private static string GetNotificationsFilePath()
            => Path.Combine(FileSystem.AppDataDirectory, NotificationsFileName);

        public async Task SaveNotificationsAsync(ObservableCollection<NotificationItem> notifications)
        {
            try
            {
                // Convert to DTO for serialization
                var notificationDtos = notifications?.Select(n => new NotificationItemDto
                {
                    Title = n.Title,
                    Message = n.Message,
                    IdText = n.IdText,
                    Type = n.Type,
                    CreatedAt = n.CreatedAt,
                    IsRead = n.IsRead
                }).ToList() ?? new List<NotificationItemDto>();

                // Limit to max stored notifications
                if (notificationDtos.Count > MaxStoredNotifications)
                {
                    notificationDtos = notificationDtos
                        .OrderByDescending(n => n.CreatedAt)
                        .Take(MaxStoredNotifications)
                        .ToList();
                }

                var json = JsonSerializer.Serialize(notificationDtos, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                var path = GetNotificationsFilePath();
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                await File.WriteAllTextAsync(path, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving notifications: {ex.Message}");
                // Don't throw - notification persistence should not crash the app
            }
        }

        public async Task<ObservableCollection<NotificationItem>> LoadNotificationsAsync()
        {
            try
            {
                var path = GetNotificationsFilePath();
                if (!File.Exists(path))
                    return new ObservableCollection<NotificationItem>();

                var json = await File.ReadAllTextAsync(path);
                var notificationDtos = JsonSerializer.Deserialize<List<NotificationItemDto>>(json);

                if (notificationDtos == null)
                    return new ObservableCollection<NotificationItem>();

                var notifications = new ObservableCollection<NotificationItem>();
                foreach (var dto in notificationDtos)
                {
                    notifications.Add(new NotificationItem
                    {
                        Title = dto.Title,
                        Message = dto.Message,
                        IdText = dto.IdText,
                        Type = dto.Type,
                        CreatedAt = dto.CreatedAt,
                        IsRead = dto.IsRead
                    });
                }

                return notifications;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading notifications: {ex.Message}");
                return new ObservableCollection<NotificationItem>();
            }
        }

        public async Task ClearNotificationsAsync()
        {
            try
            {
                var path = GetNotificationsFilePath();
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing notifications: {ex.Message}");
            }
        }

        // DTO for serialization
        private class NotificationItemDto
        {
            public string Title { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public string IdText { get; set; } = string.Empty;
            public string Type { get; set; } = "Info";
            public DateTime CreatedAt { get; set; }
            public bool IsRead { get; set; } = false;
        }
    }
}
