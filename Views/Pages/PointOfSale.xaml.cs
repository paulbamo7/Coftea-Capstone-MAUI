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

        try
        {
            // Use the shared POSVM from App
            POSViewModel = ((App)Application.Current).POSVM;

            if (POSViewModel == null)
            {
                throw new InvalidOperationException("POSViewModel is not initialized");
            }

            // Set BindingContext
            BindingContext = POSViewModel;

            // Set RetryConnectionPopup binding context
            RetryConnectionPopup.BindingContext = ((App)Application.Current).RetryConnectionPopup;

            // Start timer
            StartTimer();
        }
        catch (Exception ex)
        {
            // Show error and navigate back
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("POS Error", $"Failed to initialize POS page: {ex.Message}", "OK");
                await Navigation.PopAsync();
            });
        }
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
            if (POSViewModel == null)
            {
                await DisplayAlert("POS Error", "POSViewModel is not available", "OK");
                await Navigation.PopAsync();
                return;
            }

            if (POSViewModel.AddItemToPOSViewModel != null)
                POSViewModel.AddItemToPOSViewModel.IsAddItemToPOSVisible = false;
           
            if (POSViewModel.SettingsPopup != null)
                POSViewModel.SettingsPopup.IsAddItemToPOSVisible = false;

            await POSViewModel.LoadDataAsync();
        }
        catch (Exception ex)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await DisplayAlert("POS Error", $"Failed to load POS data: {ex.Message}", "OK");
                await Navigation.PopAsync();
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
