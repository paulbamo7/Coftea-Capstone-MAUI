using Coftea_Capstone.ViewModel;

namespace Coftea_Capstone.Views.Pages;

public partial class AdminDashboard : ContentPage
{
	public AdminDashboard()
	{
		InitializeComponent();
		
		// Use shared SettingsPopup from App directly
		BindingContext = ((App)Application.Current).SettingsPopup;
	}

    private void HomeButton_Clicked(object sender, EventArgs e)
    {

    }
    private void PointOfSaleButton_Clicked(object sender, EventArgs e)
    {

    }
    private void InventoryButton_Clicked(object sender, EventArgs e)
    {

    }
    private void SalesReportButton_Clicked(object sender, EventArgs e)
    {

    }
    private void SettingsButton_Clicked(object sender, EventArgs e)
    {

    }
}