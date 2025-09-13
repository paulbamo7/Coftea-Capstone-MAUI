using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Coftea_Capstone.ViewModel
{
    public partial class SettingsPopUpViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool isSettingsPopupVisible = false;
        [ObservableProperty]
        private bool isAddItemToPOSVisible = false;
        [ObservableProperty]
        private bool isAddItemToInventoryVisible = false;

        [RelayCommand]

        private void ShowSettingsPopup()
        {
            IsSettingsPopupVisible = true;
        }

        [RelayCommand]
        private void CloseSettingsPopup() 
        { 
            IsSettingsPopupVisible = false;
        }

        [RelayCommand]
        private void OpenAddItemToPOS()
        {
            IsSettingsPopupVisible = false;
            IsAddItemToPOSVisible = true;
        }
        [RelayCommand]
        private void CloseAddItemToPOS()
        {
            IsAddItemToPOSVisible = false;
        }

        [RelayCommand]
        private void OpenAddItemToInventory()
        {
            IsSettingsPopupVisible = false;
            IsAddItemToInventoryVisible = true;
        }
        [RelayCommand]
        private void CloseAddItemToInventory()
        {
            IsSettingsPopupVisible = false;
            IsAddItemToInventoryVisible = false;
        }
    }
}