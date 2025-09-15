using Coftea_Capstone.ViewModel;
using Microsoft.Maui.Dispatching;



namespace Coftea_Capstone.Views.Pages;

public partial class PointOfSale : ContentPage
{
    private IDispatcherTimer _timer;
    public POSPageViewModel POSViewModel { get; set; }
    public SettingsPopUpViewModel SettingsPopupViewModel { get; set; }
    public AddItemToPOSViewModel AddItemToPOSViewModel { get; set; }

    public PointOfSale()
    {
        InitializeComponent();
        AddItemToPOSViewModel = new AddItemToPOSViewModel();

        // Step 2: Pass the popup VM into Settings VM
        SettingsPopupViewModel = new SettingsPopUpViewModel(AddItemToPOSViewModel);

        // Step 3: Pass both VMs into the POS VM
        POSViewModel = new POSPageViewModel(AddItemToPOSViewModel, SettingsPopupViewModel);
        StartTimer();

        // Step 4: Set the BindingContext
        BindingContext = POSViewModel;
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
    protected override void OnAppearing()
    {
        base.OnAppearing();
        Task.Run(async () =>
        {
            try
            {

                POSViewModel.AddItemToPOSViewModel.IsAddItemToPOSVisible = false;
                POSViewModel.AddItemToPOSViewModel.IsConnectPOSToInventoryVisible = false;
                POSViewModel.SettingsPopup.IsAddItemToPOSVisible = false;


                await POSViewModel.LoadDataAsync();
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await DisplayAlert("POS Error", ex.Message, "OK");
                });
            }
        });
    }
}
