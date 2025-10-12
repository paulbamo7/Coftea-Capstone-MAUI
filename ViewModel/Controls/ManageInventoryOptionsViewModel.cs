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

        partial void OnIsInventoryManagementPopupVisibleChanged(bool value)
        {
            System.Diagnostics.Debug.WriteLine($"ManageInventoryOptionsViewModel.IsInventoryManagementPopupVisible changed to: {value}");
        }

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
            // Open the edit inventory popup where each row has Edit/Delete actions
            await _editInventoryPopupViewModel.ShowEditInventoryPopup();
        }

        [RelayCommand]
        private void CloseInventoryManagementPopup()
        {
            IsInventoryManagementPopupVisible = false;
        }
    }
}
