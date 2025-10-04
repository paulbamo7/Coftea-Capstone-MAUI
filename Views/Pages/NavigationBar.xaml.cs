using Coftea_Capstone.Models;
using Coftea_Capstone.Views.Pages;
using Coftea_Capstone.Services;
using Microsoft.Maui.Controls;

namespace Coftea_Capstone.Views.Pages;

public partial class NavigationBar : ContentView
{
    public NavigationBar()
    {
        InitializeComponent();
    }
    private async void POSButton_Clicked(object sender, EventArgs e)
    {
        if (App.CurrentUser == null) return; // do nothing if logged out

        var nav = Application.Current.MainPage as NavigationPage;
        if (nav == null) return;

        // Replace current page with POS using animation
        await nav.ReplaceWithAnimationAsync(new PointOfSale());
    }

    private async void HomeButton_Clicked(object sender, EventArgs e)
    {
        var nav = Application.Current.MainPage as NavigationPage;
        if (nav == null) return;

        if (App.CurrentUser == null)
        {
            await nav.ReplaceWithAnimationAsync(new LoginPage());
            return;
        }

        await nav.ReplaceWithAnimationAsync(new EmployeeDashboard());
    }

    private async void InventoryButton_Clicked(object sender, EventArgs e)
    {
        if (App.CurrentUser == null) return;

        // Admin users (ID = 1) always have access, or check individual permission
        bool hasAccess = (App.CurrentUser?.ID == 1) || (App.CurrentUser?.CanAccessInventory ?? false);
        
        if (!hasAccess)
        {
            await Application.Current.MainPage.DisplayAlert("Unauthorized", "You don't have permission to access Inventory.", "OK");
            return;
        }

        var nav = Application.Current.MainPage as NavigationPage;
        if (nav == null) return;

        await nav.ReplaceWithAnimationAsync(new Inventory());
    }

    private async void SalesReportButton_Clicked(object sender, EventArgs e)
    {
        if (App.CurrentUser == null) return;

        // Admin users (ID = 1) always have access, or check individual permission
        bool hasAccess = (App.CurrentUser?.ID == 1) || (App.CurrentUser?.CanAccessSalesReport ?? false);
        
        if (!hasAccess)
        {
            await Application.Current.MainPage.DisplayAlert("Unauthorized", "You don't have permission to access Sales Reports.", "OK");
            return;
        }

        var nav = Application.Current.MainPage as NavigationPage;
        if (nav == null) return;

        await nav.ReplaceWithAnimationAsync(new SalesReport());
    }



    private void SettingsButton_Clicked(object sender, EventArgs e)
    {
        // Use global settings service to show popup
        GlobalSettingsService.ShowSettings();
    }
}
