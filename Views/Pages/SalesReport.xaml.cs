using Coftea_Capstone.ViewModel;

namespace Coftea_Capstone.Views.Pages;

public partial class SalesReport : ContentPage
{
	public SalesReport()
	{
		InitializeComponent();
		
		// Use shared SettingsPopup from App directly
		BindingContext = ((App)Application.Current).SettingsPopup;
	}
}