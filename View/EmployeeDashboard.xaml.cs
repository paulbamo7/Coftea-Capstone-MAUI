namespace Coftea_Capstone.Pages;

public partial class EmployeeDashboard : ContentPage
{
	public EmployeeDashboard()
	{
		InitializeComponent();
	}

    private void HomeButton_Clicked(object sender, EventArgs e)
    {
        
    }
    private void PointOfSaleButton_Clicked(object sender, EventArgs e)
    {
        Navigation.PopModalAsync();
        Navigation.PushModalAsync(new PointOfSale());
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