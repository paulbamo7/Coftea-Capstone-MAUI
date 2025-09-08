using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Coftea_Capstone.ViewModel
{
    public partial class SettingsPopUpViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool isSettingsPopupVisible = false;

        [RelayCommand]
        private void ShowSettingsPopup() => IsSettingsPopupVisible = true;

        [RelayCommand]
        private void CloseSettingsPopup() => IsSettingsPopupVisible = false;
    }
}