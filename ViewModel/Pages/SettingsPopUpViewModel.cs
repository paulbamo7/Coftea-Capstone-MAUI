using Coftea_Capstone.Views.Pages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Coftea_Capstone;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;

namespace Coftea_Capstone.ViewModel
{
    public partial class SettingsPopUpViewModel : ObservableObject
    {
        private readonly AddItemToPOSViewModel _addItemToPOSViewModel;
        private readonly ManagePOSOptionsViewModel _managePOSOptionsViewModel;
        private readonly ManageInventoryOptionsViewModel _manageInventoryOptionsViewModel;

        [ObservableProperty] private bool isSettingsPopupVisible = false;
        [ObservableProperty] private bool isAddItemToPOSVisible = false;
        [ObservableProperty] private bool isAddItemToInventoryVisible = false;

        public ManagePOSOptionsViewModel ManagePOSOptionsVM => _managePOSOptionsViewModel;
        public ManageInventoryOptionsViewModel ManageInventoryOptionsVM => _manageInventoryOptionsViewModel;

        public SettingsPopUpViewModel(AddItemToPOSViewModel addItemToPOSViewModel, ManagePOSOptionsViewModel managePOSOptionsViewModel, ManageInventoryOptionsViewModel manageInventoryOptionsViewModel)
        {
            _addItemToPOSViewModel = addItemToPOSViewModel;
            _managePOSOptionsViewModel = managePOSOptionsViewModel;
            _manageInventoryOptionsViewModel = manageInventoryOptionsViewModel;
        }

        [RelayCommand]
        private void ShowSettingsPopup() => IsSettingsPopupVisible = true;

        [RelayCommand]
        private void CloseSettingsPopup() => IsSettingsPopupVisible = false;

        [RelayCommand]
        private void OpenAddItemToPOS()
        {
            IsSettingsPopupVisible = false;
            _addItemToPOSViewModel.IsAddItemToPOSVisible = true;
        }

        [RelayCommand]
        private void OpenManageInventoryOptions()
        {
            IsSettingsPopupVisible = false;
            _manageInventoryOptionsViewModel.IsInventoryManagementPopupVisible = true;
        }

        [RelayCommand]
        private void OpenAddItemToInventory()
        {
            IsSettingsPopupVisible = false;
            IsAddItemToInventoryVisible = true;
        }

        [RelayCommand]
        private void OpenManagePOSOptions()
        {
            IsSettingsPopupVisible = false;
            _managePOSOptionsViewModel.IsPOSManagementPopupVisible = true;
        }

        [RelayCommand]
        private void CloseAddItemToInventory()
        {
            IsSettingsPopupVisible = false;
            IsAddItemToInventoryVisible = false;
        }

        [RelayCommand]
        private void Logout()
        {
            if (Application.Current is App app)
            {
                app.ResetAppAfterLogout(); // now it works
            }
        }
    }
}
