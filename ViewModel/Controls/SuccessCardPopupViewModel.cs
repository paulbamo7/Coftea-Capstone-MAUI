using CommunityToolkit.Mvvm.ComponentModel;
using System.Threading.Tasks;

namespace Coftea_Capstone.ViewModel.Controls
{
    public partial class SuccessCardPopupViewModel : ObservableObject
    {
        [ObservableProperty] private bool isVisible = false;
        [ObservableProperty] private string title = string.Empty;
        [ObservableProperty] private string message = string.Empty;
        [ObservableProperty] private string subtext = string.Empty;

        public void Show(string title, string message, string subtext, int milliseconds = 1500) // Show success card popup
        {
            Title = title;
            Message = message;
            Subtext = subtext;

            if (MainThread.IsMainThread)
            {
                IsVisible = true;
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(() => IsVisible = true);
            }

            _ = Task.Run(async () =>
            {
                await Task.Delay(milliseconds);
                await MainThread.InvokeOnMainThreadAsync(() => IsVisible = false);
            });
        }
    }
}


