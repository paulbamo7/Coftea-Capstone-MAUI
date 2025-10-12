using Coftea_Capstone.Models;
using Coftea_Capstone.Views.Pages;
using Coftea_Capstone.Services;
using Microsoft.Maui.Controls;
using System.ComponentModel;

namespace Coftea_Capstone.Views.Pages;

public partial class NavigationBar : ContentView
{
    private bool _isNavigating = false;
    private DateTime _lastNavigationTime = DateTime.MinValue;
    private readonly TimeSpan _navigationCooldown = TimeSpan.FromMilliseconds(1500); // 1.5 second cooldown
    private readonly object _navigationLock = new object();
    private CancellationTokenSource _currentNavigationCts;

    public NavigationBar()
    {
        InitializeComponent();
        TryHookNavigationEvents();
        UpdateActiveIndicator();
    }
    private void TryHookNavigationEvents()
    {
        var nav = Application.Current.MainPage as NavigationPage;
        if (nav == null) return;
        nav.Pushed += (_, __) => MainThread.BeginInvokeOnMainThread(UpdateActiveIndicator);
        nav.Popped += (_, __) => MainThread.BeginInvokeOnMainThread(UpdateActiveIndicator);
        nav.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(NavigationPage.CurrentPage))
            {
                MainThread.BeginInvokeOnMainThread(UpdateActiveIndicator);
            }
        };
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

    private async Task<bool> StartNavigationAsync()
    {
        lock (_navigationLock)
        {
            if (!CanNavigate())
                return false;

            // Cancel any existing navigation
            _currentNavigationCts?.Cancel();
            _currentNavigationCts?.Dispose();
            _currentNavigationCts = new CancellationTokenSource();

            _isNavigating = true;
            _lastNavigationTime = DateTime.Now;
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
        
        // Re-enable all navigation buttons with a small delay to prevent rapid clicking
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Task.Delay(100); // Small delay to prevent rapid re-enabling
            HomeButton.IsEnabled = true;
            POSButton.IsEnabled = true;
            InventoryButton.IsEnabled = true;
            SalesReportButton.IsEnabled = true;
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
    private async void POSButton_Clicked(object sender, EventArgs e)
    {
        if (App.CurrentUser == null) return; // do nothing if logged out

        if (!await StartNavigationAsync()) return;

        try
        {
            var nav = Application.Current.MainPage as NavigationPage;
            if (nav == null) return;

            // Check if we're already on POS page
            if (nav.CurrentPage is PointOfSale)
            {
                System.Diagnostics.Debug.WriteLine("🚫 Already on POS page, skipping navigation");
                return;
            }

            // Use the cancellation token from StartNavigationAsync
            _currentNavigationCts?.Token.ThrowIfCancellationRequested();
            
            // Replace current page with POS using animation
            await nav.ReplaceWithAnimationAsync(new PointOfSale(), animated: false);
            UpdateActiveIndicator();
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine("❌ Navigation cancelled");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Navigation error: {ex.Message}");
        }
        finally
        {
            EndNavigation();
        }
    }

    private async void HomeButton_Clicked(object sender, EventArgs e)
    {
        if (!await StartNavigationAsync()) return;

        try
        {
            var nav = Application.Current.MainPage as NavigationPage;
            if (nav == null) return;

            // Use the cancellation token from StartNavigationAsync
            _currentNavigationCts?.Token.ThrowIfCancellationRequested();

            if (App.CurrentUser == null)
            {
                // Check if we're already on Login page
                if (nav.CurrentPage is LoginPage)
                {
                    System.Diagnostics.Debug.WriteLine("🚫 Already on Login page, skipping navigation");
                    return;
                }
                await nav.ReplaceWithAnimationAsync(new LoginPage(), animated: false);
                return;
            }

            // Check if we're already on Dashboard page
            if (nav.CurrentPage is EmployeeDashboard)
            {
                System.Diagnostics.Debug.WriteLine("🚫 Already on Dashboard page, skipping navigation");
                return;
            }

            await nav.ReplaceWithAnimationAsync(new EmployeeDashboard(), animated: false);
            UpdateActiveIndicator();
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine("❌ Navigation cancelled");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Navigation error: {ex.Message}");
        }
        finally
        {
            EndNavigation();
        }
    }

    private async void InventoryButton_Clicked(object sender, EventArgs e)
    {
        if (App.CurrentUser == null) return;

        if (!await StartNavigationAsync()) return;

        try
        {
            // Admin users always have access, or check individual permission
            bool hasAccess = (App.CurrentUser?.IsAdmin ?? false) || (App.CurrentUser?.CanAccessInventory ?? false);
            
            if (!hasAccess)
            {
                await Application.Current.MainPage.DisplayAlert("Unauthorized", "You don't have permission to access Inventory.", "OK");
                return;
            }

            var nav = Application.Current.MainPage as NavigationPage;
            if (nav == null) return;

            // Check if we're already on Inventory page
            if (nav.CurrentPage is Inventory)
            {
                System.Diagnostics.Debug.WriteLine("🚫 Already on Inventory page, skipping navigation");
                return;
            }

            // Use the cancellation token from StartNavigationAsync
            _currentNavigationCts?.Token.ThrowIfCancellationRequested();

            await nav.ReplaceWithAnimationAsync(new Inventory(), animated: false);
            UpdateActiveIndicator();
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine("❌ Navigation cancelled");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Navigation error: {ex.Message}");
        }
        finally
        {
            EndNavigation();
        }
    }

    private async void SalesReportButton_Clicked(object sender, EventArgs e)
    {
        if (App.CurrentUser == null) return;

        if (!await StartNavigationAsync()) return;

        try
        {
            // Admin users always have access, or check individual permission
            bool hasAccess = (App.CurrentUser?.IsAdmin ?? false) || (App.CurrentUser?.CanAccessSalesReport ?? false);
            
            if (!hasAccess)
            {
                await Application.Current.MainPage.DisplayAlert("Unauthorized", "You don't have permission to access Sales Reports.", "OK");
                return;
            }

            var nav = Application.Current.MainPage as NavigationPage;
            if (nav == null) return;

            // Check if we're already on SalesReport page
            if (nav.CurrentPage is SalesReport)
            {
                System.Diagnostics.Debug.WriteLine("🚫 Already on SalesReport page, skipping navigation");
                return;
            }

            // Use the cancellation token from StartNavigationAsync
            _currentNavigationCts?.Token.ThrowIfCancellationRequested();

            await nav.ReplaceWithAnimationAsync(new SalesReport(), animated: false);
            UpdateActiveIndicator();
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine("❌ Navigation cancelled");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Navigation error: {ex.Message}");
        }
        finally
        {
            EndNavigation();
        }
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
