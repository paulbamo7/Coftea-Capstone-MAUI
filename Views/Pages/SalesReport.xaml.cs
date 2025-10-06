using Coftea_Capstone.ViewModel;
using Coftea_Capstone.ViewModel.Controls;

namespace Coftea_Capstone.Views.Pages;

public partial class SalesReport : ContentPage
{
	public SalesReport()
	{
		InitializeComponent();
		
		// Create and set the SalesReportPageViewModel
		var settingsPopup = ((App)Application.Current).SettingsPopup;
		var viewModel = new SalesReportPageViewModel(settingsPopup);
		BindingContext = viewModel;
		
		// Set RetryConnectionPopup binding context
		RetryConnectionPopup.BindingContext = ((App)Application.Current).RetryConnectionPopup;
		
		// Initialize the view model
		_ = viewModel.InitializeAsync();
	}

    private void OnBellClicked(object sender, EventArgs e)
    {
        var popup = ((App)Application.Current).NotificationPopup;
        popup?.AddSuccess("Sales Report", "Generated Daily Report (ID: DR20251006)", "ID: DR20251006");
        popup?.AddSuccess("Sales Report", "Exported to CSV (ID: CSV106)", "ID: CSV106");
        popup?.ToggleCommand.Execute(null);
    }
}