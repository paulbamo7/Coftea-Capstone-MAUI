using CommunityToolkit.Mvvm.Input;
using Coftea_Capstone.Models;
using Coftea_Capstone.ViewModel;
using Coftea_Capstone.Views;

namespace Coftea_Capstone.Views.Pages;

public partial class NavigationBar : ContentView
{
    private UserInfoModel user;

    public NavigationBar()
    {
        InitializeComponent();
        user = App.CurrentUser;
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
        /*Navigation.PopModalAsync();
        Navigation.PushAsync(new SettingsPopUp());*/
        //ParentViewModel.SettingsPopup.ShowSettingsPopupCommand.Execute(null);
    }
}