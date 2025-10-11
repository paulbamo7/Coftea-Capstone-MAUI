using Coftea_Capstone.ViewModel;
using Coftea_Capstone.ViewModel.Controls;
using Coftea_Capstone.Views.Controls;

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
		
		// RetryConnectionPopup is now handled globally through App.xaml.cs
		
		// Initialize the view model
		_ = viewModel.InitializeAsync();

		// Responsive behavior
		SizeChanged += OnSizeChanged;
	}

    private void OnBellClicked(object sender, EventArgs e)
    {
        var popup = ((App)Application.Current).NotificationPopup;
        _ = popup?.AddSuccess("Sales Report", "Generated Daily Report (ID: DR20251006)", "ID: DR20251006");
        _ = popup?.AddSuccess("Sales Report", "Exported to CSV (ID: CSV106)", "ID: CSV106");
        popup?.ToggleCommand.Execute(null);
    }

    private void OnSizeChanged(object sender, EventArgs e)
    {
        double pageWidth = Width;
        if (double.IsNaN(pageWidth) || pageWidth <= 0)
        {
            return;
        }

        bool isPhoneLike = pageWidth < 800;

        if (SidebarColumn is not null)
        {
            SidebarColumn.Width = isPhoneLike ? new GridLength(0) : new GridLength(200);
        }
        if (Sidebar is not null)
        {
            Sidebar.IsVisible = !isPhoneLike;
        }
    }
}