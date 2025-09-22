using Coftea_Capstone.ViewModel;

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

        // Subscribe to inventory change notifications to refresh the list
        MessagingCenter.Subscribe<AddItemToInventoryViewModel>(this, "InventoryChanged", async (sender) =>
        {
            await vm.LoadDataAsync();
        });

        // Set RetryConnectionPopup binding context for the inline popup control
        RetryConnectionPopup.BindingContext = ((App)Application.Current).RetryConnectionPopup;

        Appearing += async (_, __) => await vm.InitializeAsync();
    }
}