using Coftea_Capstone.ViewModel.Pages;

namespace Coftea_Capstone.Views.Pages;

public partial class PurchaseOrderHistoryPage : ContentPage
{
    public PurchaseOrderHistoryPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        if (BindingContext is PurchaseOrderHistoryPageViewModel vm)
        {
            await vm.LoadPurchaseOrdersCommand.ExecuteAsync(null);
        }
    }

    private void OnBellClicked(object sender, EventArgs e)
    {
        var popup = ((App)Application.Current).NotificationPopup;
        popup?.ToggleCommand.Execute(null);
    }

    private void OnProfileClicked(object sender, EventArgs e)
    {
        try
        {
            var app = (App)Application.Current;
            app?.ProfilePopup?.ShowProfile();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error opening profile: {ex.Message}");
        }
    }
}

