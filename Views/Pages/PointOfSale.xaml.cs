using Coftea_Capstone.ViewModel;
using Microsoft.Maui.Dispatching;
using Coftea_Capstone;

namespace Coftea_Capstone.Views.Pages;

public partial class PointOfSale : ContentPage
{
    private IDispatcherTimer _timer;

    // Use shared POSVM from App
    public POSPageViewModel POSViewModel { get; set; }

    public PointOfSale()
    {
        InitializeComponent();

        // Use the shared POSVM from App
        POSViewModel = ((App)Application.Current).POSVM;


        // Set BindingContext
        BindingContext = POSViewModel;

        // Start timer
        StartTimer();
    }

    private void StartTimer()
    {
        // Set initial time
        TimerLabel.Text = DateTime.Now.ToString("hh:mm:ss tt");

        // Create timer
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (s, e) =>
        {
            TimerLabel.Text = DateTime.Now.ToString("hh:mm:ss tt");
        };
        _timer.Start();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            POSViewModel.AddItemToPOSViewModel.IsAddItemToPOSVisible = false;
            POSViewModel.AddItemToPOSViewModel.IsConnectPOSToInventoryVisible = false;
            POSViewModel.SettingsPopup.IsAddItemToPOSVisible = false;

            await POSViewModel.LoadDataAsync();
        }
        catch (Exception ex)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await DisplayAlert("POS Error", ex.Message, "OK");
            });
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (_timer is not null)
        {
            _timer.Stop();
            _timer.Tick -= null; 
        }
    }
}
