using Coftea_Capstone.Views.Pages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace Coftea_Capstone.ViewModel
{
    public partial class ManagePOSOptionsViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool isPOSManagementPopupVisible;

        public ManagePOSOptionsViewModel()
        {

        }

        [RelayCommand]
        private void AddItem()
        {
            IsPOSManagementPopupVisible = false;

        }

        [RelayCommand]
        private void EditItem()
        {
            IsPOSManagementPopupVisible = false;

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
