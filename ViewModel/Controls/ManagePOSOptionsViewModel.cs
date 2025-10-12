using Coftea_Capstone.Views.Pages;
using Coftea_Capstone.ViewModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace Coftea_Capstone.ViewModel
{
    public partial class ManagePOSOptionsViewModel : ObservableObject
    {
        private readonly AddItemToPOSViewModel _addItemToPOSViewModel;
        private readonly EditProductPopupViewModel _editProductPopupViewModel;

        [ObservableProperty]
        private bool isPOSManagementPopupVisible;

        partial void OnIsPOSManagementPopupVisibleChanged(bool value)
        {
            System.Diagnostics.Debug.WriteLine($"ManagePOSOptionsViewModel.IsPOSManagementPopupVisible changed to: {value}");
        }

        public EditProductPopupViewModel EditProductPopupVM => _editProductPopupViewModel;

        public ManagePOSOptionsViewModel(AddItemToPOSViewModel addItemToPOSViewModel, EditProductPopupViewModel editProductPopupViewModel)
        {
            _addItemToPOSViewModel = addItemToPOSViewModel;
            _editProductPopupViewModel = editProductPopupViewModel;
        }

        [RelayCommand]
        private void AddItem()
        {
            IsPOSManagementPopupVisible = false;
            if (_addItemToPOSViewModel != null)
            {
                _addItemToPOSViewModel.IsAddItemToPOSVisible = true;
            }
        }

        [RelayCommand]
        private async Task EditItem()
        {
            IsPOSManagementPopupVisible = false;
            await _editProductPopupViewModel.ShowEditProductPopup();
        }

        [RelayCommand]
        private void DeleteItem()
        {
            IsPOSManagementPopupVisible = false;

        }

        [RelayCommand]
        private void ClosePOSManagementPopup()
        {
            IsPOSManagementPopupVisible = false;
        }
    }
}
