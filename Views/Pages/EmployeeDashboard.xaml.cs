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
		
		// Load today's metrics when dashboard loads with a small delay to prevent Task exceptions
		_ = Task.Run(async () =>
		{
			await Task.Delay(1000); // Wait 1 second before loading metrics
			await LoadTodaysMetricsAsync();
		});
	}

    private void OnBellClicked(object sender, EventArgs e)
    {
        ((App)Application.Current).NotificationPopup?.ToggleCommand.Execute(null);
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
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error opening profile: {ex.Message}");
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