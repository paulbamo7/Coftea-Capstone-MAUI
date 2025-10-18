using Coftea_Capstone.ViewModel;
using Microsoft.Maui.Dispatching;
using Coftea_Capstone;
using Coftea_Capstone.Services;

namespace Coftea_Capstone.Views.Pages;

public partial class PointOfSale : ContentPage
{
    private IDispatcherTimer _timer;
    private EventHandler _timerTickHandler;
    private bool _isDisposed = false;

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

            // RetryConnectionPopup is now handled globally through App.xaml.cs
            
            // PaymentPopup binding context is now set via XAML binding
            
            // Ensure CartPopup binding context is set (important for tablet compatibility)
            // The CartPopup is bound via XAML but we need to ensure it's properly initialized

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
                await SimpleNavigationService.GoBackAsync();
            });
        }
    }

    private void OnSizeChanged(object sender, EventArgs e)
    {
        // Prevent execution if page is being disposed
        if (_isDisposed) return;

        // Basic breakpoints for responsiveness
        double pageWidth = Width;
        if (double.IsNaN(pageWidth) || pageWidth <= 0)
        {
            return;
        }

        try
        {
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
        catch
        {
            // Ignore layout errors during disposal
        }
    }

    private void StartTimer()
    {
        try
        {
            // Set initial time with null check
            if (TimerLabel != null)
            {
                TimerLabel.Text = DateTime.Now.ToString("hh:mm:ss tt");
            }

            // Create timer
            _timer = Dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timerTickHandler = (s, e) =>
            {
                // Null check to prevent crash during disposal
                if (!_isDisposed && TimerLabel != null)
                {
                    try
                    {
                        TimerLabel.Text = DateTime.Now.ToString("hh:mm:ss tt");
                    }
                    catch { }
                }
            };
            _timer.Tick += _timerTickHandler;
            _timer.Start();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Timer start error: {ex.Message}");
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            if (POSViewModel == null)
            {
                await DisplayAlert("POS Error", "POSViewModel is not available", "OK");
                await SimpleNavigationService.GoBackAsync();
                return;
            }

            if (POSViewModel.AddItemToPOSViewModel != null)
                POSViewModel.AddItemToPOSViewModel.IsAddItemToPOSVisible = false;
           
            if (POSViewModel.SettingsPopup != null)
                POSViewModel.SettingsPopup.IsAddItemToPOSVisible = false;

            // Subscribe to property changes (guard against duplicates)
            POSViewModel.PropertyChanged -= OnViewModelPropertyChanged;
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
                await SimpleNavigationService.GoBackAsync();
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

        // Mark as disposed to prevent timer/layout updates
        _isDisposed = true;

        // Stop and cleanup timer
        if (_timer is not null)
        {
            try
            {
                _timer.Stop();
                if (_timerTickHandler != null)
                {
                    _timer.Tick -= _timerTickHandler;
                    _timerTickHandler = null;
                }
                _timer = null;
            }
            catch { }
        }

        // Unsubscribe from property changes
        if (POSViewModel != null)
        {
            try
            {
                POSViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }
            catch { }
        }

        // Detach size changed handler
        try
        {
            SizeChanged -= OnSizeChanged;
        }
        catch { }

        // PaymentPopup binding is now handled via XAML

        // Release visual tree in background to avoid blocking main thread
        _ = Task.Run(() =>
        {
            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        ReleaseVisualTree(Content);
                    }
                    catch { }
                });
            }
            catch { }
        });

        // Force native view detachment
        try
        {
            Handler?.DisconnectHandler();
        }
        catch { }
    }

    private static void ReleaseVisualTree(Microsoft.Maui.IView element)
    {
        if (element == null) return;

        try
        {

        if (element is CollectionView cv)
        {
            cv.ItemsSource = null;
        }
        else if (element is ListView lv)
        {
            lv.ItemsSource = null;
        }

        if (element is ContentView contentView && contentView.Content != null)
        {
            ReleaseVisualTree(contentView.Content);
        }
        else if (element is Layout layout)
        {
            foreach (var child in layout.Children)
            {
                ReleaseVisualTree(child);
            }
        }
        else if (element is ScrollView sv && sv.Content != null)
        {
            ReleaseVisualTree(sv.Content);
        }
        else if (element is ContentPage page && page.Content != null)
        {
            ReleaseVisualTree(page.Content);
        }
        }
        catch { }
    }

    private void OnTestPaymentClicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("Test Payment button clicked");
        var app = (App)Application.Current;
        if (app?.PaymentPopup != null)
        {
            System.Diagnostics.Debug.WriteLine("Calling ShowPayment from test button");
            app.PaymentPopup.ShowPayment(100.00m, new List<Models.CartItem>());
            
            // PaymentPopup binding is handled via XAML
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

            // Navigate back using new NavigationService
            if (Navigation?.NavigationStack?.Count > 1)
            {
                // Use async void to fire and forget
                _ = SimpleNavigationService.GoBackAsync();
                return true;
            }
        }
        catch { }

        return base.OnBackButtonPressed();
    }

}
