using Coftea_Capstone.ViewModel;
using Microsoft.Maui.Controls;

namespace Coftea_Capstone.Views.Pages;

public partial class Settings : ContentView
{
	public Settings()
	{
		InitializeComponent();
    }

    private void OnFrameTapped(object sender, EventArgs e)
    {
        // Prevent the tap from bubbling up to the background
        // This stops the popup from closing when clicking on the content
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        try
        {
            var confirm = await Application.Current.MainPage.DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");
            if (!confirm) return;

            if (Application.Current is App app)
            {
                app.ResetAppAfterLogout();
            }
        }
        catch { }
    }
}