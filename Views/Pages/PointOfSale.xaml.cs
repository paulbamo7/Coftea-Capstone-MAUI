using Coftea_Capstone.ViewModel;
using Coftea_Capstone.ViewModel.Controls;
using Coftea_Capstone.ViewModel.Pages;
using Microsoft.Maui.Dispatching;
using Coftea_Capstone;
using Coftea_Capstone.Services;
using Microsoft.Maui.Controls;
using Coftea_Capstone.Models;
using Coftea_Capstone.C_;
using System.Threading.Tasks;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;

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

            // Subscribe to ViewModel reinitialization messages
            MessagingCenter.Subscribe<App>(this, "ViewModelsReinitialized", async (sender) =>
            {
                System.Diagnostics.Debug.WriteLine("🔄 ViewModelsReinitialized message received in POS - refreshing popup bindings");
                await RefreshPopupBindings();
            });
            
            MessagingCenter.Subscribe<LoginPageViewModel>(this, "ViewModelsReadyAfterLogin", async (sender) =>
            {
                System.Diagnostics.Debug.WriteLine("🔄 ViewModelsReadyAfterLogin message received in POS - refreshing popup bindings");
                await RefreshPopupBindings();
            });
            
            // Subscribe to profile image change messages
            MessagingCenter.Subscribe<ProfilePopupViewModel, ImageSource>(this, "ProfileImageChanged", async (sender, newImageSource) =>
            {
                System.Diagnostics.Debug.WriteLine("🔄 ProfileImageChanged message received in POS - refreshing profile image");
                await RefreshPopupBindings();
            });

            MessagingCenter.Subscribe<POSPageViewModel, POSPageModel>(this, "POSProductAddedToCart", async (sender, product) =>
            {
                await RunCartAnimationAsync(product);
            });

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
            if (Content is Grid mainGrid && mainGrid.ColumnDefinitions.Count > 0)
            {
                mainGrid.ColumnDefinitions[0].Width = isPhoneLike ? new GridLength(0) : new GridLength(200);
            }
            
            if (Sidebar is not null)
            {
                Sidebar.IsVisible = !isPhoneLike;
            }

            // Cart collapse: hide cart column on phone-like widths  
            if (Content is Grid mainGridContent)
            {
                var mainContentGrid = GetMainContentGrid(mainGridContent);
                if (mainContentGrid != null && mainContentGrid.ColumnDefinitions.Count > 1)
                {
                    mainContentGrid.ColumnDefinitions[1].Width = isPhoneLike ? new GridLength(0) : new GridLength(2, GridUnitType.Star);
                }
            }

            // Adjust product grid span by width
            if (ProductsCollectionView?.ItemsLayout is GridItemsLayout gridLayout)
            {
                if (isVeryNarrow)
                {
                    gridLayout.Span = 1;
                }
                else if (isPhoneLike)
                {
                    gridLayout.Span = 2;
                }
                else
                {
                    gridLayout.Span = 3;
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
        
        // Update navigation state for indicator
        NavigationStateService.SetCurrentPageType(typeof(PointOfSale));

        try
        {
            if (POSViewModel == null)
            {
                await DisplayAlert("POS Error", "POSViewModel is not available", "OK");
                await SimpleNavigationService.GoBackAsync();
                return;
            }

            // Refresh popup bindings to ensure we have the latest ViewModel instances
            // Small delay to ensure ViewModels are fully initialized
            await Task.Delay(100);
            await RefreshPopupBindings();

            // Refresh stock levels when page appears to reflect any inventory changes
            await POSViewModel.CheckStockLevelsForAllProducts();

            if (POSViewModel.AddItemToPOSViewModel != null)
                POSViewModel.AddItemToPOSViewModel.IsAddItemToPOSVisible = false;
           
            if (POSViewModel.SettingsPopup != null)
                POSViewModel.SettingsPopup.IsAddItemToPOSVisible = false;
            
            // Clear the right frame when switching pages
            if (POSViewModel != null)
            {
                POSViewModel.SelectedProduct = null;
            }

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

        // Unsubscribe from MessagingCenter to avoid memory leaks
        try
        {
            MessagingCenter.Unsubscribe<App>(this, "ViewModelsReinitialized");
            MessagingCenter.Unsubscribe<LoginPageViewModel>(this, "ViewModelsReadyAfterLogin");
            MessagingCenter.Unsubscribe<ProfilePopupViewModel, ImageSource>(this, "ProfileImageChanged");
            MessagingCenter.Unsubscribe<POSPageViewModel, POSPageModel>(this, "POSProductAddedToCart");
        }
        catch { }

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
            // Don't clear CollectionView ItemsSource to prevent data loss when navigating
            // cv.ItemsSource = null;
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

    private Grid GetMainContentGrid(Grid mainGrid)
    {
        if (mainGrid == null || mainGrid.Children.Count == 0)
            return null;

        try
        {
            // The Products + Cart grid should be in the main grid's row 2
            // Main content is at Grid.Column="1" of the outer grid
            // Inside that, we have a Grid with RowDefinitions="Auto, Auto, *"
            // The Products + Cart is at Grid.Row="2"
            
            if (mainGrid.Children.Count > 1)
            {
                // Get the main content area (second column, which is a Grid)
                var mainContent = mainGrid.Children[1];
                if (mainContent is Grid contentGrid && contentGrid.Children.Count > 2)
                {
                    // Get the third row (Grid.Row="2") which contains Products + Cart
                    return contentGrid.Children[2] as Grid;
                }
            }
        }
        catch { }
        
        return null;
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

    private async Task RefreshPopupBindings()
    {
        try
        {
            var app = (App)Application.Current;
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    // Explicitly rebind all popup controls to the new ViewModel instances
                    if (app?.SettingsPopup != null && SettingsPopupControl != null)
                    {
                        SettingsPopupControl.BindingContext = app.SettingsPopup;
                        System.Diagnostics.Debug.WriteLine("✅ POS SettingsPopup binding refreshed");
                    }
                    
                    if (app?.NotificationPopup != null && NotificationPopupControl != null)
                    {
                        NotificationPopupControl.BindingContext = app.NotificationPopup;
                        // Force reload notifications after binding refresh
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                // Small delay to ensure ViewModel is fully bound
                                await Task.Delay(200);
                                await app.NotificationPopup.LoadStoredNotificationsAsync();
                                System.Diagnostics.Debug.WriteLine("✅ POS Notifications reloaded after binding refresh");
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"⚠️ Error reloading notifications in POS: {ex.Message}");
                            }
                        });
                        System.Diagnostics.Debug.WriteLine("✅ POS NotificationPopup binding refreshed");
                    }
                    
                    if (app?.ProfilePopup != null && ProfilePopupControl != null)
                    {
                        ProfilePopupControl.BindingContext = app.ProfilePopup;
                        System.Diagnostics.Debug.WriteLine("✅ POS ProfilePopup binding refreshed");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Error in POS RefreshPopupBindings MainThread: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Error in POS RefreshPopupBindings: {ex.Message}");
        }
    }

    private async Task RunCartAnimationAsync(POSPageModel product)
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    if (SelectedProductImage != null
                        && CartIconBorder != null
                        && AnimationLayer != null
                        && CartFlyoutImage != null)
                    {
                        var startRect = await GetValidBoundsAsync(SelectedProductImage, AnimationLayer);
                        var endRect = await GetValidBoundsAsync(CartIconBorder, AnimationLayer);

                        if (startRect.Width > 0 && startRect.Height > 0 && endRect.Width > 0 && endRect.Height > 0)
                        {
                            AbsoluteLayout.SetLayoutBounds(CartFlyoutImage, startRect);
                            AbsoluteLayout.SetLayoutFlags(CartFlyoutImage, AbsoluteLayoutFlags.None);

                            CartFlyoutImage.Source = SelectedProductImage.Source;
                            CartFlyoutImage.TranslationX = 0;
                            CartFlyoutImage.TranslationY = 0;
                            CartFlyoutImage.Scale = 1;
                            CartFlyoutImage.Opacity = 1;
                            CartFlyoutImage.IsVisible = true;

                            var deltaX = (endRect.X + endRect.Width / 2) - (startRect.X + startRect.Width / 2);
                            var deltaY = (endRect.Y + endRect.Height / 2) - (startRect.Y + startRect.Height / 2);

                            var flyTask = CartFlyoutImage.TranslateTo(deltaX, deltaY, 450, Easing.SinInOut);
                            var shrinkTask = CartFlyoutImage.ScaleTo(0.4, 450, Easing.SinIn);
                            await Task.WhenAll(flyTask, shrinkTask);
                            await CartFlyoutImage.FadeTo(0, 150, Easing.SinOut);

                            CartFlyoutImage.IsVisible = false;
                            CartFlyoutImage.TranslationX = 0;
                            CartFlyoutImage.TranslationY = 0;
                            CartFlyoutImage.Scale = 1;
                            CartFlyoutImage.Opacity = 0;
                        }
                    }

                    if (CartIconBorder != null)
                    {
                        var originalScale = CartIconBorder.Scale;
                        if (originalScale <= 0)
                        {
                            CartIconBorder.Scale = 1.0;
                            originalScale = 1.0;
                        }
                        await CartIconBorder.ScaleTo(1.15, 150, Easing.CubicOut);
                        await CartIconBorder.ScaleTo(originalScale, 150, Easing.CubicIn);
                    }
                }
                catch (Exception animationEx)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Cart animation error: {animationEx.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Failed to run cart animation: {ex.Message}");
        }
    }

    private async Task<Rect> GetValidBoundsAsync(VisualElement element, VisualElement relativeTo, int maxAttempts = 5)
    {
        if (element == null || relativeTo == null)
        {
            return Rect.Zero;
        }

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var bounds = GetRelativeBounds(element, relativeTo);
            if (bounds.Width > 0 && bounds.Height > 0)
            {
                return bounds;
            }

            await Task.Delay(80);
        }

        return Rect.Zero;
    }

    private Rect GetRelativeBounds(VisualElement element, VisualElement relativeTo)
    {
        if (element == null || relativeTo == null)
        {
            return Rect.Zero;
        }

        double x = element.X + element.TranslationX;
        double y = element.Y + element.TranslationY;
        double width = element.Width;
        double height = element.Height;

        var parent = element.Parent as VisualElement;
        while (parent != null && parent != relativeTo)
        {
            x += parent.X + parent.TranslationX;
            y += parent.Y + parent.TranslationY;
            parent = parent.Parent as VisualElement;
        }

        if (parent == null)
        {
            return new Rect(x, y, width, height);
        }

        return new Rect(x, y, width, height);
    }

}

