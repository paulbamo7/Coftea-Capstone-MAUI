using Coftea_Capstone.ViewModel;
using Coftea_Capstone.ViewModel.Controls;
using Coftea_Capstone.ViewModel.Pages;
using Coftea_Capstone.Services;
using Coftea_Capstone.Views;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;

namespace Coftea_Capstone.Views.Pages;

public partial class Inventory : ContentPage
{
    public Inventory()
	{
		InitializeComponent();

        // Use shared InventoryVM from App to prevent memory leaks
        var app = (App)Application.Current;
        var vm = app.InventoryVM;
        BindingContext = vm;

        // Set reference to UpdateInventoryDetails control for reset functionality
        var addItemToInventoryVM = app.ManageInventoryPopup.AddItemToInventoryVM;
        if (addItemToInventoryVM != null)
        {
            addItemToInventoryVM.UpdateInventoryDetailsControl = UpdateInventoryDetailsControl;
        }

        // Subscribe to inventory change notifications to refresh the list
        MessagingCenter.Subscribe<AddItemToInventoryViewModel>(this, "InventoryChanged", async (sender) =>
        {
            System.Diagnostics.Debug.WriteLine("🔄 InventoryChanged message received - forcing inventory refresh");
            // Force a full reload to ensure fresh data from database
            await vm.ForceReloadDataAsync();
        });

        // Subscribe to ViewModel reinitialization messages
        MessagingCenter.Subscribe<App>(this, "ViewModelsReinitialized", async (sender) =>
        {
            System.Diagnostics.Debug.WriteLine("🔄 ViewModelsReinitialized message received in Inventory - refreshing popup bindings");
            await RefreshPopupBindings();
        });
        
        MessagingCenter.Subscribe<LoginPageViewModel>(this, "ViewModelsReadyAfterLogin", async (sender) =>
        {
            System.Diagnostics.Debug.WriteLine("🔄 ViewModelsReadyAfterLogin message received in Inventory - refreshing popup bindings");
            await RefreshPopupBindings();
        });
        
        // Subscribe to profile image change messages
        MessagingCenter.Subscribe<ProfilePopupViewModel, ImageSource>(this, "ProfileImageChanged", async (sender, newImageSource) =>
        {
            System.Diagnostics.Debug.WriteLine("🔄 ProfileImageChanged message received in Inventory - refreshing profile image");
            await RefreshPopupBindings();
        });

        // RetryConnectionPopup is now handled globally through App.xaml.cs

        // Initialize data immediately when page is created
        Appearing += async (_, __) => 
        {
            // Update navigation state for indicator
            NavigationStateService.SetCurrentPageType(typeof(Inventory));
            
            // Refresh popup bindings to ensure we have the latest ViewModel instances
            // Small delay to ensure ViewModels are fully initialized
            await Task.Delay(100);
            await RefreshPopupBindings();
            
            System.Diagnostics.Debug.WriteLine("🔄 Inventory page appearing - reloading data");
            await vm.InitializeAsync();
        };
        
        // Also try to load data immediately in case Appearing doesn't fire
        _ = Task.Run(async () => 
        {
            await Task.Delay(100); // Small delay to ensure page is fully loaded
            await vm.InitializeAsync();
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Unsubscribe messaging to avoid retaining the page
        MessagingCenter.Unsubscribe<AddItemToInventoryViewModel>(this, "InventoryChanged");
        MessagingCenter.Unsubscribe<App>(this, "ViewModelsReinitialized");
        MessagingCenter.Unsubscribe<LoginPageViewModel>(this, "ViewModelsReadyAfterLogin");
        MessagingCenter.Unsubscribe<ProfilePopupViewModel, ImageSource>(this, "ProfileImageChanged");

        // Break external references from singletons to page controls
        try
        {
            var addItemToInventoryVM = ((App)Application.Current).ManageInventoryPopup?.AddItemToInventoryVM;
            if (addItemToInventoryVM != null)
            {
                addItemToInventoryVM.UpdateInventoryDetailsControl = null;
            }
        }
        catch { }

        // Keep BindingContext and ItemsSource to avoid re-creating VM and losing visuals on return
        // Don't clear the CollectionView's ItemsSource as it causes data to disappear when returning

        // Force native view detachment to help GC on Android
        try { Handler?.DisconnectHandler(); } catch { }
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
		var popup = ((App)Application.Current).NotificationPopup;
		popup?.ToggleCommand.Execute(null);
    }

    private void OnSortChanged(object sender, EventArgs e)
    {
        if (BindingContext is InventoryPageViewModel vm)
        {
            vm.ApplyCategoryFilter();
        }
    }

    private void OnProfileClicked(object sender, EventArgs e)
    {
        try
        {
            var app = (App)Application.Current;
            app?.ProfilePopup?.ShowProfile();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error opening profile: {ex.Message}");
        }
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
                                System.Diagnostics.Debug.WriteLine("✅ Inventory Notifications reloaded after binding refresh");
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"⚠️ Error reloading notifications in Inventory: {ex.Message}");
                            }
                        });
                        System.Diagnostics.Debug.WriteLine("✅ Inventory NotificationPopup binding refreshed");
                    }
                    
                    if (app?.ProfilePopup != null && ProfilePopupControl != null)
                    {
                        ProfilePopupControl.BindingContext = app.ProfilePopup;
                        System.Diagnostics.Debug.WriteLine("✅ Inventory ProfilePopup binding refreshed");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Error in Inventory RefreshPopupBindings MainThread: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Error in Inventory RefreshPopupBindings: {ex.Message}");
        }
    }

    protected override bool OnBackButtonPressed()
    {
        return BackButtonHandler.HandleBackButton(this);
    }
}