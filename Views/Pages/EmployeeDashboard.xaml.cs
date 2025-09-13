using Coftea_Capstone.C_;
using Coftea_Capstone.Models;
using Coftea_Capstone.ViewModel;
using Coftea_Capstone.Views;
using Microsoft.Maui.ApplicationModel.Communication;
using Coftea_Capstone.Views;

namespace Coftea_Capstone.Views.Pages;

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