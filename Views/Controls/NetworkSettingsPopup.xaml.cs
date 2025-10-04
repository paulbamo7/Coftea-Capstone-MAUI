using Coftea_Capstone.ViewModel.Controls;

namespace Coftea_Capstone.Views.Controls
{
    public partial class NetworkSettingsPopup : ContentView
    {
        public NetworkSettingsPopup()
        {
            InitializeComponent();
            BindingContext = new NetworkSettingsPopupViewModel();
        }
    }
}
