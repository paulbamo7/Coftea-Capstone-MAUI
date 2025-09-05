using Coftea_Capstone.Pages;
using CommunityToolkit.Mvvm.Input;
using Coftea_Capstone.Models;
using Coftea_Capstone.ViewModel;

namespace Coftea_Capstone.View;

public partial class NavigationBar : ContentView
{
    private UserInfoModel user;
	public NavigationBar()
	{
		InitializeComponent();
        user = new UserInfoModel();
	}

    private void HomeButton_Clicked(object sender, EventArgs e)
    {
        Navigation.PopModalAsync();
       
        if (user.IsAdmin)
        {
            Navigation.PushAsync(new AdminDashboard());
        }
        else
        {
            Navigation.PushAsync(new EmployeeDashboard());
        }
        
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