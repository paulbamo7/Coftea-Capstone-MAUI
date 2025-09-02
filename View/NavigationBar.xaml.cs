using Coftea_Capstone.Pages;

namespace Coftea_Capstone.View;

public partial class NavigationBar : ContentView
{
	public NavigationBar()
	{
		InitializeComponent();
	}

    private void HomeButton_Clicked(object sender, EventArgs e)
    {
        Navigation.PopModalAsync();
        Navigation.PushAsync(new EmployeeDashboard());
    }

    private void POSButton_Clicked(object sender, EventArgs e)
    {
        Navigation.PopModalAsync();
        Navigation.PushAsync(new PointOfSale());
    }

    private void InventoryButton_Clicked(object sender, EventArgs e)
    {
        Navigation.PopModalAsync();
        Navigation.PushAsync(new Inventory());
    }

    private void SalesReportButton_Clicked(object sender, EventArgs e)
    {
        Navigation.PopModalAsync();
        Navigation.PushAsync(new SalesReport());
    }

    private void SettingsButton_Clicked(object sender, EventArgs e)
    {
        Navigation.PopModalAsync();
        Navigation.PushAsync(new Settings());
    }
}