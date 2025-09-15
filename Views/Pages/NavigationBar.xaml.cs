using Coftea_Capstone.Models;
using Coftea_Capstone.Views.Pages;
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

        // Remove current stack and push POS
        await nav.PopToRootAsync(false);
        await nav.PushAsync(new PointOfSale());
    }

    private async void HomeButton_Clicked(object sender, EventArgs e)
    {
        var nav = Application.Current.MainPage as NavigationPage;
        if (nav == null) return;

        if (App.CurrentUser == null)
        {
            await nav.PopToRootAsync(false);
            await nav.PushAsync(new LoginPage());
            return;
        }

        await nav.PopToRootAsync(false);

        if (App.CurrentUser.IsAdmin)
            await nav.PushAsync(new AdminDashboard());
        else
            await nav.PushAsync(new EmployeeDashboard());
    }

    private async void InventoryButton_Clicked(object sender, EventArgs e)
    {
        if (App.CurrentUser == null) return;

        var nav = Application.Current.MainPage as NavigationPage;
        if (nav == null) return;

        await nav.PopToRootAsync(false);
        await nav.PushAsync(new Inventory());
    }

    private async void SalesReportButton_Clicked(object sender, EventArgs e)
    {
        if (App.CurrentUser == null) return;

        var nav = Application.Current.MainPage as NavigationPage;
        if (nav == null) return;

        await nav.PopToRootAsync(false);
        await nav.PushAsync(new SalesReport());
    }



    private void SettingsButton_Clicked(object sender, EventArgs e)
    {
        // Open your settings popup (if you have MVVM command binding)
        // For example:
        // (BindingContext as YourViewModel)?.SettingsPopup.ShowSettingsPopupCommand.Execute(null);
    }
}
