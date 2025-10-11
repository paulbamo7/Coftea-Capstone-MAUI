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

        // Subscribe to inventory change notifications to refresh the list
        MessagingCenter.Subscribe<AddItemToInventoryViewModel>(this, "InventoryChanged", async (sender) =>
        {
            await vm.LoadDataAsync();
        });

        // Set RetryConnectionPopup binding context for the inline popup control
        RetryConnectionPopup.BindingContext = ((App)Application.Current).RetryConnectionPopup;

        Appearing += async (_, __) => await vm.InitializeAsync();

    }

    private void OnBellClicked(object sender, EventArgs e)
    {
		var popup = ((App)Application.Current).NotificationPopup;
		_ = popup?.AddSuccess("Inventory", "Listed Item: Caramel Syrup (ID: CS7890)", "ID: CS7890");
		_ = popup?.AddSuccess("Inventory", "Updated Stock: Arabica Beans (ID: AB4567)", "ID: AB4567");
		popup?.ToggleCommand.Execute(null);
    }
}