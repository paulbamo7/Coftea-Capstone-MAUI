using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace Coftea_Capstone.ViewModel.Controls
{
    public partial class OrderCompletePopupViewModel : ObservableObject
    {
        [ObservableProperty] private bool isVisible;

        [RelayCommand]
        private void Close()
        {
            IsVisible = false;
        }

        public void Show()
        {
            if (MainThread.IsMainThread)
            {
                IsVisible = true;
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(() => IsVisible = true);
            }
        }
    }
}


