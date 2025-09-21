using Coftea_Capstone.ViewModel;

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
}