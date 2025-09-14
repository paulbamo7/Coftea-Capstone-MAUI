using Coftea_Capstone.Views.Pages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Coftea_Capstone.ViewModel
{
    public partial class SettingsPopUpViewModel : ObservableObject
    {

        private readonly AddItemToPOSViewModel _addItemToPOSViewModel;
        [ObservableProperty]
        private bool isSettingsPopupVisible = false;
        [ObservableProperty]
        private bool isAddItemToPOSVisible = false;
        [ObservableProperty]
        private bool isAddItemToInventoryVisible = false;

        public SettingsPopUpViewModel(AddItemToPOSViewModel addItemToPOSViewModel)
        {
            _addItemToPOSViewModel = addItemToPOSViewModel;
        }   

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
            _addItemToPOSViewModel.IsAddItemToPOSVisible = true;
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
        [RelayCommand]
        private async Task Logout()
        {
            // Clear current user
            App.SetCurrentUser(null);

            // Clear login-related preferences
            Preferences.Set("IsLoggedIn", false);
            Preferences.Set("IsAdmin", false);
            Preferences.Remove("Email");
            Preferences.Remove("Password");
            Preferences.Remove("RememberMe");

            // Navigate back to login page
            Application.Current.MainPage = new NavigationPage(new LoginPage());
        }
    }
}