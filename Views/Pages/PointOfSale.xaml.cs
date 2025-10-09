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
            
            // Set PaymentPopup binding context to shared instance
            PaymentPopupControl.BindingContext = ((App)Application.Current).PaymentPopup;

            // Start timer
            StartTimer();

            // Hook size changed for responsive behavior
            SizeChanged += OnSizeChanged;
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

    private void OnSizeChanged(object sender, EventArgs e)
    {
        // Basic breakpoints for responsiveness
        double pageWidth = Width;
        if (double.IsNaN(pageWidth) || pageWidth <= 0)
        {
            return;
        }

        bool isPhoneLike = pageWidth < 800; // collapse sidebar and cart
        bool isVeryNarrow = pageWidth < 500; // reduce product span further

        // Sidebar collapse: set column width to 0 when narrow
        if (SidebarColumn is not null)
        {
            SidebarColumn.Width = isPhoneLike ? new GridLength(0) : new GridLength(200);
        }
        if (Sidebar is not null)
        {
            Sidebar.IsVisible = !isPhoneLike;
        }

        // Cart collapse: hide cart column on phone-like widths
        if (CartColumn is not null)
        {
            CartColumn.Width = isPhoneLike ? new GridLength(0) : new GridLength(2, GridUnitType.Star);
        }

        // Adjust product grid span by width
        if (ProductsGridLayout is not null)
        {
            if (isVeryNarrow)
            {
                ProductsGridLayout.Span = 1;
            }
            else if (isPhoneLike)
            {
                ProductsGridLayout.Span = 2;
            }
            else
            {
                ProductsGridLayout.Span = 3;
            }
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

            // Subscribe to property changes (removed loading animation)
            POSViewModel.PropertyChanged += OnViewModelPropertyChanged;

            // Ensure products and persisted cart are loaded
            await POSViewModel.LoadDataAsync();
            await POSViewModel.LoadCartFromStorageAsync();
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

    private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Loading animation removed - no longer needed
    }


    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (_timer is not null)
        {
            _timer.Stop();
            _timer.Tick -= null; 
        }

        // Unsubscribe from property changes
        if (POSViewModel != null)
        {
            POSViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }
    }

    private void OnTestPaymentClicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("Test Payment button clicked");
        var app = (App)Application.Current;
        if (app?.PaymentPopup != null)
        {
            System.Diagnostics.Debug.WriteLine("Calling ShowPayment from test button");
            app.PaymentPopup.ShowPayment(100.00m, new List<Models.CartItem>());
            
            // Also try to show it directly on the control
            PaymentPopupControl.BindingContext = app.PaymentPopup;
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("PaymentPopup is null in test button");
        }
    }

    protected override bool OnBackButtonPressed()
    {
        try
        {
            var app = (App)Application.Current;

            // Close visible popups first
            if (app?.PaymentPopup?.IsPaymentVisible == true)
            {
                app.PaymentPopup.ClosePaymentCommand?.Execute(null);
                return true; // consumed
            }

            if (POSViewModel?.AddItemToPOSViewModel?.IsAddItemToPOSVisible == true)
            {
                POSViewModel.AddItemToPOSViewModel.IsAddItemToPOSVisible = false;
                return true;
            }

            if (POSViewModel?.SettingsPopup?.IsAddItemToPOSVisible == true)
            {
                POSViewModel.SettingsPopup.IsAddItemToPOSVisible = false;
                return true;
            }

            // Addons popup (if available)
            var addonsPopup = POSViewModel?.AddonsPopup;
            if (addonsPopup != null)
            {
                var isVisibleProp = addonsPopup.GetType().GetProperty("IsAddonsPopupVisible");
                var closeCmdProp = addonsPopup.GetType().GetProperty("CloseAddonsPopupCommand");
                if (isVisibleProp != null && closeCmdProp != null)
                {
                    var isVisible = isVisibleProp.GetValue(addonsPopup) as bool?;
                    if (isVisible == true)
                    {
                        var closeCmd = closeCmdProp.GetValue(addonsPopup) as System.Windows.Input.ICommand;
                        closeCmd?.Execute(null);
                        return true;
                    }
                }
            }

            // If we have a navigation stack, go back
            if (Navigation?.NavigationStack?.Count > 1)
            {
                if (Navigation.NavigationStack.Count == 0) return true;
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try { await Navigation.PopAsync(); } catch { }
                });
                return true;
            }
        }
        catch { }

        return base.OnBackButtonPressed();
    }

}
