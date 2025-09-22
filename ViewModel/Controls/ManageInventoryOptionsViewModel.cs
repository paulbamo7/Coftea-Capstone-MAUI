using Coftea_Capstone.Views.Pages;
using Coftea_Capstone.ViewModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace Coftea_Capstone.ViewModel
{
    public partial class ManageInventoryOptionsViewModel : ObservableObject
    {
        private readonly AddItemToInventoryViewModel _addItemToInventoryViewModel;
        private readonly EditInventoryPopupViewModel _editInventoryPopupViewModel;

        [ObservableProperty]
        private bool isInventoryManagementPopupVisible;

        public AddItemToInventoryViewModel AddItemToInventoryVM => _addItemToInventoryViewModel;
        public EditInventoryPopupViewModel EditInventoryPopupVM => _editInventoryPopupViewModel;

        public ManageInventoryOptionsViewModel(AddItemToInventoryViewModel addItemToInventoryViewModel, EditInventoryPopupViewModel editInventoryPopupViewModel)
        {
            _addItemToInventoryViewModel = addItemToInventoryViewModel;
            _editInventoryPopupViewModel = editInventoryPopupViewModel;
        }

        [RelayCommand]
        private void AddItem()
        {
            IsInventoryManagementPopupVisible = false;
            if (_addItemToInventoryViewModel != null)
            {
                _addItemToInventoryViewModel.IsAddItemToInventoryVisible = true;
            }
        }

        [RelayCommand]
        private async Task EditItem()
        {
            IsInventoryManagementPopupVisible = false;
            await _editInventoryPopupViewModel.ShowEditInventoryPopup();
        }

        [RelayCommand]
        private async Task DeleteItem()
        {
            IsInventoryManagementPopupVisible = false;
            // TODO: Implement delete inventory items functionality
            await Application.Current.MainPage.DisplayAlert("Info", "Delete functionality will be implemented soon.", "OK");
        }

        [RelayCommand]
        private void CloseInventoryManagementPopup()
        {
            IsInventoryManagementPopupVisible = false;
        }
    }
}
