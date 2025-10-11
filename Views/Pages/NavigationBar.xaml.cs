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
    private readonly TimeSpan _navigationCooldown = TimeSpan.FromMilliseconds(500); // 500ms cooldown

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
            System.Diagnostics.Debug.WriteLine($"🚫 Navigation blocked: Cooldown active ({timeSinceLastNavigation.TotalMilliseconds:F0}ms remaining)");
            return false;
        }

        return true;
    }

    private async Task<bool> StartNavigationAsync()
    {
        if (!CanNavigate())
            return false;

        _isNavigating = true;
        _lastNavigationTime = DateTime.Now;
        
        // Disable all navigation buttons to prevent multiple clicks
        DisableNavigationButtons();
        
        System.Diagnostics.Debug.WriteLine("✅ Navigation started");
        return true;
    }

    private void EndNavigation()
    {
        _isNavigating = false;
        System.Diagnostics.Debug.WriteLine("✅ Navigation completed");
        
        // Re-enable all navigation buttons
        MainThread.BeginInvokeOnMainThread(() =>
        {
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

            // Replace current page with POS using animation
            await nav.ReplaceWithAnimationAsync(new PointOfSale(), animated: false);
            UpdateActiveIndicator();
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

            if (App.CurrentUser == null)
            {
                await nav.ReplaceWithAnimationAsync(new LoginPage(), animated: false);
                return;
            }

            await nav.ReplaceWithAnimationAsync(new EmployeeDashboard(), animated: false);
            UpdateActiveIndicator();
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
            // Admin users (ID = 1) always have access, or check individual permission
            bool hasAccess = (App.CurrentUser?.ID == 1) || (App.CurrentUser?.CanAccessInventory ?? false);
            
            if (!hasAccess)
            {
                await Application.Current.MainPage.DisplayAlert("Unauthorized", "You don't have permission to access Inventory.", "OK");
                return;
            }

            var nav = Application.Current.MainPage as NavigationPage;
            if (nav == null) return;

            await nav.ReplaceWithAnimationAsync(new Inventory(), animated: false);
            UpdateActiveIndicator();
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
            // Admin users (ID = 1) always have access, or check individual permission
            bool hasAccess = (App.CurrentUser?.ID == 1) || (App.CurrentUser?.CanAccessSalesReport ?? false);
            
            if (!hasAccess)
            {
                await Application.Current.MainPage.DisplayAlert("Unauthorized", "You don't have permission to access Sales Reports.", "OK");
                return;
            }

            var nav = Application.Current.MainPage as NavigationPage;
            if (nav == null) return;

            await nav.ReplaceWithAnimationAsync(new SalesReport(), animated: false);
            UpdateActiveIndicator();
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
}
