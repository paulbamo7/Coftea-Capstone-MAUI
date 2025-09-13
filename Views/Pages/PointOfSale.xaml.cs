using Coftea_Capstone.C_;
using Coftea_Capstone.Models;
using Coftea_Capstone.ViewModel;
using Microsoft.Maui.ApplicationModel.Communication;
using System.Timers;

namespace Coftea_Capstone.Views.Pages;

public partial class PointOfSale : ContentPage
{
    private readonly POSPageViewModel _viewModel;
    private readonly System.Timers.Timer _timer;

    public PointOfSale()
	{
		InitializeComponent();
        // Create ViewModel
        _viewModel = new POSPageViewModel();

        // Set as BindingContext    
        BindingContext = _viewModel;
        _timer = new System.Timers.Timer(1000); // 1 second
        _timer.Elapsed += OnTimedEvent;
        _timer.AutoReset = true;
        _timer.Enabled = true;
    }

    private void OnTimedEvent(object sender, ElapsedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            TimerLabel.Text = DateTime.Now.ToString("hh:mm:ss tt");
            // Example: 12:45:22 PM
        });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync(App.CurrentUser?.Email);
        if (BindingContext is POSPageViewModel vm && App.CurrentUser != null)
        {
            vm.IsAdmin = App.CurrentUser.IsAdmin;
        }   
        await _viewModel.LoadDataAsync();
    }

    private void HomeButton_Clicked(object sender, EventArgs e)
    {

    }

    private void POSButton_Clicked(object sender, EventArgs e)
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