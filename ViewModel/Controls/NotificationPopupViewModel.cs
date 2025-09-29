using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace Coftea_Capstone.ViewModel.Controls
{
    public partial class NotificationPopupViewModel : ObservableObject
    {
        [ObservableProperty] private bool isNotificationVisible = false;
        [ObservableProperty] private string notificationMessage = "";
        [ObservableProperty] private string notificationType = "Success"; // Success, Error, Info
        [ObservableProperty] private bool isAutoHide = true;
        [ObservableProperty] private int autoHideDelay = 3000; // 3 seconds

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
            IsNotificationVisible = true;

            if (IsAutoHide)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(AutoHideDelay);
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        IsNotificationVisible = false;
                    });
                });
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
