using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Coftea_Capstone.ViewModel.Controls
{
    public partial class NotificationPopupViewModel : ObservableObject
    {
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

        public void AddSuccess(string title, string entityName, string idText)
        {
            Notifications.Insert(0, new NotificationItem
            {
                Title = title,
                Message = $"Successfully {entityName}. {idText}",
                IdText = idText
            });
            // keep last 10
            while (Notifications.Count > 10)
                Notifications.RemoveAt(Notifications.Count - 1);
            NotificationCount++;
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
    }
}
