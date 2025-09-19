using Coftea_Capstone.ViewModel;

namespace Coftea_Capstone.Views.Pages;

public partial class Inventory : ContentPage
{
    public Inventory()
	{
		InitializeComponent();

        // Use shared SettingsPopup from App directly
        BindingContext = ((App)Application.Current).SettingsPopup;
    }
}