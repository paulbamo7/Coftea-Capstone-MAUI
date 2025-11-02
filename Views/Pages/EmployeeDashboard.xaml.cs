using Coftea_Capstone.C_;
using Coftea_Capstone.Models;
using Coftea_Capstone.ViewModel;
using Coftea_Capstone.Views;
using Microsoft.Maui.ApplicationModel.Communication;
using Coftea_Capstone.Views;
using Coftea_Capstone.Services;
using Microsoft.Maui.Controls;
using Coftea_Capstone.ViewModel.Controls;

namespace Coftea_Capstone.Views.Pages;

public partial class EmployeeDashboard : ContentPage
{
	public EmployeeDashboard()
	{
		InitializeComponent();
		
		// Subscribe to ViewModel reinitialization messages
		MessagingCenter.Subscribe<App>(this, "ViewModelsReinitialized", async (sender) =>
		{
			System.Diagnostics.Debug.WriteLine("🔄 ViewModelsReinitialized message received - refreshing popup bindings");
			await RefreshPopupBindings();
		});
		
		MessagingCenter.Subscribe<LoginPageViewModel>(this, "ViewModelsReadyAfterLogin", async (sender) =>
		{
			System.Diagnostics.Debug.WriteLine("🔄 ViewModelsReadyAfterLogin message received - refreshing popup bindings");
			await RefreshPopupBindings();
		});
		
		// Subscribe to profile image change messages
		MessagingCenter.Subscribe<ProfilePopupViewModel, ImageSource>(this, "ProfileImageChanged", async (sender, newImageSource) =>
		{
			System.Diagnostics.Debug.WriteLine("🔄 ProfileImageChanged message received - refreshing profile image");
			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				try
				{
					if (ProfileImage != null)
					{
						var app = (App)Application.Current;
						// Clear existing binding and set new source directly
						ProfileImage.RemoveBinding(Image.SourceProperty);
						ProfileImage.Source = newImageSource;
						
						// Restore binding so future changes are reflected
						if (app?.ProfilePopup != null)
						{
							ProfileImage.SetBinding(Image.SourceProperty, 
								new Binding 
								{ 
									Source = app.ProfilePopup, 
									Path = nameof(app.ProfilePopup.ProfileImageSource),
									Mode = BindingMode.OneWay
								});
						}
						
						System.Diagnostics.Debug.WriteLine("✅ Profile image updated in EmployeeDashboard and binding restored");
					}
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"⚠️ Error updating profile image in EmployeeDashboard: {ex.Message}");
				}
			});
		});
		
		// Update navigation state for indicator
		Appearing += async (_, __) => 
		{
			NavigationStateService.SetCurrentPageType(typeof(EmployeeDashboard));
			// Refresh binding context to ensure we have the latest SettingsPopup instance
			// Small delay to ensure ViewModels are fully initialized
			await Task.Delay(100);
			await RefreshPopupBindings();
			await LoadTodaysMetricsAsync();
		};
		
	// Use shared SettingsPopup from App directly
		BindingContext = ((App)Application.Current).SettingsPopup;
		
		// Load today's metrics when dashboard loads with a small delay to prevent Task exceptions
		_ = Task.Run(async () =>
		{
			await Task.Delay(1000); // Wait 1 second before loading metrics
			await LoadTodaysMetricsAsync();
		});
	}

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Unsubscribe from messaging
        MessagingCenter.Unsubscribe<App>(this, "ViewModelsReinitialized");
        MessagingCenter.Unsubscribe<LoginPageViewModel>(this, "ViewModelsReadyAfterLogin");
        MessagingCenter.Unsubscribe<ProfilePopupViewModel, ImageSource>(this, "ProfileImageChanged");

        // Drop heavy bindings to encourage GC
        try
        {
            if (Content != null)
            {
                ReleaseVisualTree(Content);
            }
        }
        catch { }

        // Force native view detachment to help GC
        try { Handler?.DisconnectHandler(); } catch { }
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
                        System.Diagnostics.Debug.WriteLine("✅ SettingsPopup binding refreshed");
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
                                System.Diagnostics.Debug.WriteLine("✅ Notifications reloaded after binding refresh");
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"⚠️ Error reloading notifications: {ex.Message}");
                            }
                        });
                        System.Diagnostics.Debug.WriteLine("✅ NotificationPopup binding refreshed");
                    }
                    
                    if (app?.ProfilePopup != null && ProfilePopupControl != null)
                    {
                        ProfilePopupControl.BindingContext = app.ProfilePopup;
                        System.Diagnostics.Debug.WriteLine("✅ ProfilePopup binding refreshed");
                    }
                    
                    // Also refresh the page's main BindingContext
                    if (app?.SettingsPopup != null)
                    {
                        BindingContext = app.SettingsPopup;
                        System.Diagnostics.Debug.WriteLine("✅ Page BindingContext refreshed");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Error in RefreshPopupBindings MainThread: {ex.Message}");
                }
            });
            
            // Force profile image refresh
            if (app?.ProfilePopup != null)
            {
                // Reload profile for new user first (this will set ProfileImageSource)
                await app.ProfilePopup.LoadUserProfile();
                
                // Wait a moment for the profile to load
                await Task.Delay(200);
                
                // Refresh profile image binding on main thread
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        if (ProfileImage != null && app?.ProfilePopup != null)
                        {
                            // Remove existing binding
                            ProfileImage.RemoveBinding(Image.SourceProperty);
                            
                            // Get the current image source from ViewModel
                            var currentImageSource = app.ProfilePopup.ProfileImageSource;
                            
                            // Set directly first to ensure image appears
                            ProfileImage.Source = currentImageSource;
                            
                            // Restore binding with new source
                            ProfileImage.SetBinding(Image.SourceProperty, 
                                new Binding 
                                { 
                                    Source = app.ProfilePopup, 
                                    Path = nameof(app.ProfilePopup.ProfileImageSource),
                                    Mode = BindingMode.OneWay
                                });
                            
                            System.Diagnostics.Debug.WriteLine($"✅ Profile image refreshed - ImageSource: {currentImageSource}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Error refreshing profile image: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"⚠️ Stack trace: {ex.StackTrace}");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Error refreshing popup bindings: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"⚠️ Stack trace: {ex.StackTrace}");
        }
    }

    private static void ReleaseVisualTree(Microsoft.Maui.IView element)
    {
        if (element == null) return;

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

    private void OnBellClicked(object sender, EventArgs e)
    {
        try
        {
            var app = (App)Application.Current;
            if (app?.NotificationPopup != null)
            {
                app.NotificationPopup.ToggleCommand?.Execute(null);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("⚠️ NotificationPopup is null - ViewModels may not be initialized");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Error opening notification popup: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"⚠️ Stack trace: {ex.StackTrace}");
        }
    }

    private void OnProfileClicked(object sender, EventArgs e)
    {
        try
        {
            var app = (App)Application.Current;
            if (app?.ProfilePopup != null)
            {
                app.ProfilePopup.ShowProfile();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("⚠️ ProfilePopup is null - ViewModels may not be initialized");
                // Try to refresh bindings and retry
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await RefreshPopupBindings();
                    if (app?.ProfilePopup != null)
                    {
                        app.ProfilePopup.ShowProfile();
                    }
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Error opening profile: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"⚠️ Stack trace: {ex.StackTrace}");
        }
    }
    
    private void OnSettingsClicked(object sender, EventArgs e)
    {
        try
        {
            var app = (App)Application.Current;
            if (app?.SettingsPopup != null)
            {
                app.SettingsPopup.ShowSettingsPopup();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("⚠️ SettingsPopup is null - ViewModels may not be initialized");
                // Try to refresh bindings and retry
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await RefreshPopupBindings();
                    if (app?.SettingsPopup != null)
                    {
                        app.SettingsPopup.ShowSettingsPopup();
                    }
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Error opening settings: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"⚠️ Stack trace: {ex.StackTrace}");
        }
    }

	private async Task LoadTodaysMetricsAsync()
	{
		try
		{
			using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15)); // Increased timeout
			var settingsPopup = ((App)Application.Current).SettingsPopup;
			if (settingsPopup != null)
			{
				await settingsPopup.LoadTodaysMetricsAsync();
				System.Diagnostics.Debug.WriteLine("✅ LoadTodaysMetrics completed successfully");
			}
			else
			{
				System.Diagnostics.Debug.WriteLine("⚠️ SettingsPopup is null, skipping metrics load");
			}
		}
		catch (OperationCanceledException)
		{
			System.Diagnostics.Debug.WriteLine("⏰ LoadTodaysMetrics timeout - this is normal on slow connections");
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"❌ LoadTodaysMetrics error: {ex.Message}");
			System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
			// Don't rethrow - this is a background operation
		}
	}

    private void HomeButton_Clicked(object sender, EventArgs e)
    {
        
    }
    private void PointOfSaleButton_Clicked(object sender, EventArgs e)
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