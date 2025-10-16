using Coftea_Capstone.ViewModel;
using Coftea_Capstone.ViewModel.Controls;

namespace Coftea_Capstone.Views.Pages;

public partial class Inventory : ContentPage
{
    public Inventory()
	{
		InitializeComponent();

        // Set InventoryPageViewModel as BindingContext
        var settingsVm = ((App)Application.Current).SettingsPopup;
        var vm = new InventoryPageViewModel(settingsVm);
        BindingContext = vm;

        // Set reference to UpdateInventoryDetails control for reset functionality
        var addItemToInventoryVM = ((App)Application.Current).ManageInventoryPopup.AddItemToInventoryVM;
        if (addItemToInventoryVM != null)
        {
            addItemToInventoryVM.UpdateInventoryDetailsControl = UpdateInventoryDetailsControl;
        }

        // Subscribe to inventory change notifications to refresh the list
        MessagingCenter.Subscribe<AddItemToInventoryViewModel>(this, "InventoryChanged", async (sender) =>
        {
            await vm.LoadDataAsync();
        });

        // RetryConnectionPopup is now handled globally through App.xaml.cs

        Appearing += async (_, __) => await vm.InitializeAsync();

    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Unsubscribe messaging to avoid retaining the page
        MessagingCenter.Unsubscribe<AddItemToInventoryViewModel>(this, "InventoryChanged");

        // Release heavy UI bindings if any
        try
        {
            if (Content != null)
            {
                ReleaseVisualTree(Content);
            }
        }
        catch { }

        // Drop heavy bindings
        BindingContext = null;
    }

    private static void ReleaseVisualTree(Microsoft.Maui.IView element)
    {
        if (element == null) return;

        if (element is Image img)
        {
            img.Source = null;
        }
        else if (element is ImageButton imgBtn)
        {
            imgBtn.Source = null;
        }
        else if (element is CollectionView cv)
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

    private void OnBellClicked(object sender, EventArgs e)
    {
		var popup = ((App)Application.Current).NotificationPopup;
		_ = popup?.AddSuccess("Inventory", "Listed Item: Caramel Syrup (ID: CS7890)", "ID: CS7890");
		_ = popup?.AddSuccess("Inventory", "Updated Stock: Arabica Beans (ID: AB4567)", "ID: AB4567");
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
}