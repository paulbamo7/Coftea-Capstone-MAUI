using Coftea_Capstone.Models;
using Coftea_Capstone.Views.Pages;
using Coftea_Capstone.Services;
using Microsoft.Maui.Controls;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

namespace Coftea_Capstone.Views.Pages;

public partial class NavigationBar : ContentView
{
    private bool _isNavigating = false;
    private DateTime _lastNavigationTime = DateTime.MinValue;
    private readonly TimeSpan _navigationCooldown = TimeSpan.FromMilliseconds(2500); // 2.5 second cooldown
    private readonly object _navigationLock = new object();
    private CancellationTokenSource _currentNavigationCts;
    private string _lastRequestedTarget = string.Empty;
    private string _pendingTarget = string.Empty;
    private DateTime _pendingRequestedAt = DateTime.MinValue;
    private static readonly SemaphoreSlim _navSemaphore = new SemaphoreSlim(1, 1);
    private static readonly SemaphoreSlim _globalNavLock = new SemaphoreSlim(1, 1);
    // Track nav event subscriptions to unhook and avoid leaks
    private NavigationPage _hookedNav;
    private EventHandler<NavigationEventArgs> _pushedHandler;
    private EventHandler<NavigationEventArgs> _poppedHandler;
    private PropertyChangedEventHandler _propertyChangedHandler;
    // DEBUG helper removed

    public NavigationBar()
    {
        InitializeComponent();
        TryHookNavigationEvents();
        UpdateActiveIndicator();
    }

    private void CloseOverlays()
    {
        try
        {
            // Best-effort close of global overlays/popups that may crash navigation when left open
            var app = Application.Current as App;

            // Settings popup
            try { app?.SettingsPopup?.CloseSettingsPopupCommand?.Execute(null); } catch { }

            // Manage POS options popup
            try { app?.ManagePOSPopup?.ClosePOSManagementPopupCommand?.Execute(null); } catch { }

            // Add Item to POS overlay
            try { if (app?.POSVM?.AddItemToPOSViewModel != null) app.POSVM.AddItemToPOSViewModel.IsAddItemToPOSVisible = false; } catch { }

            // Addons popup within ConnectPOSItemToInventory
            var vm = app?.POSVM?.AddItemToPOSViewModel?.ConnectPOSToInventoryVM;
            try { vm?.CloseAddonPopupCommand?.Execute(null); } catch { }
        }
        catch { /* ignore */ }
    }

    private static void ForceGC()
    {
        try
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            #if ANDROID
            try { Java.Lang.JavaSystem.Gc(); } catch { }
            #endif
        }
        catch { }
    }
    private void TryHookNavigationEvents()
    {
        var nav = Application.Current.MainPage as NavigationPage;
        if (nav == null || ReferenceEquals(_hookedNav, nav)) return;

        UnhookNavigationEvents();

        _hookedNav = nav;
        _pushedHandler = (_, __) => MainThread.BeginInvokeOnMainThread(UpdateActiveIndicator);
        _poppedHandler = (_, __) => MainThread.BeginInvokeOnMainThread(UpdateActiveIndicator);
        _propertyChangedHandler = (s, e) =>
        {
            if (e.PropertyName == nameof(NavigationPage.CurrentPage))
            {
                MainThread.BeginInvokeOnMainThread(UpdateActiveIndicator);
            }
        };

        _hookedNav.Pushed += _pushedHandler;
        _hookedNav.Popped += _poppedHandler;
        _hookedNav.PropertyChanged += _propertyChangedHandler;
    }

    private void UnhookNavigationEvents()
    {
        if (_hookedNav == null) return;
        try
        {
            if (_pushedHandler != null) _hookedNav.Pushed -= _pushedHandler;
            if (_poppedHandler != null) _hookedNav.Popped -= _poppedHandler;
            if (_propertyChangedHandler != null) _hookedNav.PropertyChanged -= _propertyChangedHandler;
        }
        catch { }
        finally
        {
            _pushedHandler = null;
            _poppedHandler = null;
            _propertyChangedHandler = null;
            _hookedNav = null;
        }
    }

    private void UpdateActiveIndicator()
    {
        var nav = Application.Current.MainPage as NavigationPage;
        var current = nav?.CurrentPage;
        if (current == null)
            return;

        Color active = Color.FromArgb("#D4A574");  // Darker brown for better visibility
        Color inactive = Color.FromArgb("#FFEAD6");

        // Reset all
        HomeButton.Background = inactive;
        POSButton.Background = inactive;
        InventoryButton.Background = inactive;
        SalesReportButton.Background = inactive;

        // Set active based on current page type
        if (current is EmployeeDashboard)
        {
            HomeButton.Background = active;
        }
        else if (current is PointOfSale)
        {
            POSButton.Background = active;
        }
        else if (current is Inventory)
        {
            InventoryButton.Background = active;
        }
        else if (current is SalesReport)
        {
            SalesReportButton.Background = active;
        }
    }

    private bool CanNavigate()
    {
        lock (_navigationLock)
        {
            // Check if we're already navigating
            if (_isNavigating)
            {
                System.Diagnostics.Debug.WriteLine("🚫 Navigation blocked: Already navigating");
                return false;
            }

            // Check cooldown period
            var timeSinceLastNavigation = DateTime.Now - _lastNavigationTime;
            if (timeSinceLastNavigation < _navigationCooldown)
            {
                var remainingMs = (_navigationCooldown - timeSinceLastNavigation).TotalMilliseconds;
                System.Diagnostics.Debug.WriteLine($"🚫 Navigation blocked: Cooldown active ({remainingMs:F0}ms remaining)");
                return false;
            }

            return true;
        }
    }

    private async Task<bool> StartNavigationAsync(string target = "")
    {
        // Ensure only one navigation runs across the app at a time
        try
        {
            await _navSemaphore.WaitAsync();
        }
        catch
        {
            return false;
        }

        lock (_navigationLock)
        {
            if (!CanNavigate())
            {
                _navSemaphore.Release();
                return false;
            }

            // Cancel any existing navigation
            _currentNavigationCts?.Cancel();
            _currentNavigationCts?.Dispose();
            _currentNavigationCts = new CancellationTokenSource();

            _isNavigating = true;
            _lastNavigationTime = DateTime.Now;
            _lastRequestedTarget = target ?? string.Empty;
        }
        
        // Disable all navigation buttons to prevent multiple clicks
        DisableNavigationButtons();
        
        System.Diagnostics.Debug.WriteLine("✅ Navigation started");
        return true;
    }

    private void EndNavigation()
    {
        lock (_navigationLock)
        {
            _isNavigating = false;
            _currentNavigationCts?.Dispose();
            _currentNavigationCts = null;
        }
        System.Diagnostics.Debug.WriteLine("✅ Navigation completed");
        try { _navSemaphore.Release(); } catch { }
        
        // Re-enable all navigation buttons with a small delay to prevent rapid clicking
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Task.Delay(650); // Throttle longer to defeat 0.5s spam
            HomeButton.IsEnabled = true;
            POSButton.IsEnabled = true;
            InventoryButton.IsEnabled = true;
            SalesReportButton.IsEnabled = true;
            // If there is a pending target, coalesce to the latest request
            if (!string.IsNullOrEmpty(_pendingTarget))
            {
                var target = _pendingTarget;
                _pendingTarget = string.Empty;
                // Respect cooldown
                var since = DateTime.Now - _lastNavigationTime;
                if (since < _navigationCooldown)
                {
                    await Task.Delay(_navigationCooldown - since);
                }
                try
                {
                    switch (target)
                    {
                        case nameof(PointOfSale):
                            POSButton_Clicked(POSButton, EventArgs.Empty);
                            break;
                        case nameof(EmployeeDashboard):
                            HomeButton_Clicked(HomeButton, EventArgs.Empty);
                            break;
                        case nameof(Inventory):
                            InventoryButton_Clicked(InventoryButton, EventArgs.Empty);
                            break;
                        case nameof(SalesReport):
                            SalesReportButton_Clicked(SalesReportButton, EventArgs.Empty);
                            break;
                    }
                }
                catch { }
            }
        });
    }

    private void DisableNavigationButtons()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            HomeButton.IsEnabled = false;
            POSButton.IsEnabled = false;
            InventoryButton.IsEnabled = false;
            SalesReportButton.IsEnabled = false;
        });
    }
    private async Task SafeNavigateAsync(Func<Task> navAction)
    {
        await _globalNavLock.WaitAsync();
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try { await navAction(); }
                catch (Exception ex) { Debug.WriteLine($"❌ Navigation failed: {ex.Message}"); }
            });
        }
        finally
        {
            _globalNavLock.Release();
        }
    }
    private async void POSButton_Clicked(object sender, EventArgs e)
    {
        if (App.CurrentUser == null) return; // do nothing if logged out
        if (!await StartNavigationAsync(nameof(PointOfSale))) { _pendingTarget = nameof(PointOfSale); return; }
        CloseOverlays();
        try { await SimpleNavigationService.NavigateToAsync(() => new PointOfSale()); }
        finally { EndNavigation(); }
    }

    private async void HomeButton_Clicked(object sender, EventArgs e)
    {
        if (!await StartNavigationAsync(nameof(EmployeeDashboard))) { _pendingTarget = nameof(EmployeeDashboard); return; }
        CloseOverlays();
        try
        {
            if (App.CurrentUser == null)
            {
                await SimpleNavigationService.NavigateToAsync(() => new LoginPage());
            }
            else
            {
                await SimpleNavigationService.NavigateToAsync(() => new EmployeeDashboard());
            }
        }
        finally { EndNavigation(); }
    }

    private async void InventoryButton_Clicked(object sender, EventArgs e)
    {
        if (App.CurrentUser == null) return;
        if (!await StartNavigationAsync(nameof(Inventory))) { _pendingTarget = nameof(Inventory); return; }
        CloseOverlays();
        try
        {
            bool hasAccess = (App.CurrentUser?.IsAdmin ?? false) || (App.CurrentUser?.CanAccessInventory ?? false);
            if (!hasAccess)
            {
                await Application.Current.MainPage.DisplayAlert("Unauthorized", "You don't have permission to access Inventory.", "OK");
                return;
            }
            await SimpleNavigationService.NavigateToAsync(() => new Inventory());
        }
        finally { EndNavigation(); }
    }

    private async void SalesReportButton_Clicked(object sender, EventArgs e)
    {
        if (App.CurrentUser == null) return;
        if (!await StartNavigationAsync(nameof(SalesReport))) { _pendingTarget = nameof(SalesReport); return; }
        CloseOverlays();
        try
        {
            bool hasAccess = (App.CurrentUser?.IsAdmin ?? false) || (App.CurrentUser?.CanAccessSalesReport ?? false);
            if (!hasAccess)
            {
                await Application.Current.MainPage.DisplayAlert("Unauthorized", "You don't have permission to access Sales Reports.", "OK");
                return;
            }
            await SimpleNavigationService.NavigateToAsync(() => new SalesReport());
        }
        finally { EndNavigation(); }
    }



    private void SettingsButton_Clicked(object sender, EventArgs e)
    {
        // Use global settings service to show popup
        GlobalSettingsService.ShowSettings();
    }

    // Cleanup method to prevent memory leaks
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        
        if (Handler == null)
        {
            // Clean up when the view is being disposed
            _currentNavigationCts?.Cancel();
            _currentNavigationCts?.Dispose();
            _currentNavigationCts = null;
        }
    }
}
