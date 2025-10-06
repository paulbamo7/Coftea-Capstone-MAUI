using Coftea_Capstone.C_;
using Coftea_Capstone.Models;
using Coftea_Capstone.ViewModel;
using Coftea_Capstone.Views;
using Microsoft.Maui.ApplicationModel.Communication;
using Coftea_Capstone.Views;

namespace Coftea_Capstone.Views.Pages;

public partial class EmployeeDashboard : ContentPage
{
	public EmployeeDashboard()
	{
		InitializeComponent();
		
		// Use shared SettingsPopup from App directly
		BindingContext = ((App)Application.Current).SettingsPopup;
		
		// Load today's metrics when dashboard loads
		_ = LoadTodaysMetrics();
	}

    private void OnBellClicked(object sender, EventArgs e)
    {
        ((App)Application.Current).NotificationPopup?.ToggleCommand.Execute(null);
    }

	private async Task LoadTodaysMetrics()
	{
		try
		{
			var settingsPopup = ((App)Application.Current).SettingsPopup;
			if (settingsPopup != null)
			{
				await settingsPopup.LoadTodaysMetricsAsync();
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Failed to load today's metrics: {ex.Message}");
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