using Coftea_Capstone.Services;
using Microsoft.Maui.Controls;

namespace Coftea_Capstone.Views.Pages;

public partial class AboutPage : ContentPage
{
    public AboutPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        NavigationStateService.SetCurrentPageType(typeof(AboutPage));
    }

    private void OnBellClicked(object sender, EventArgs e)
    {
        var popup = ((App)Application.Current).NotificationPopup;
        popup?.ToggleCommand.Execute(null);
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

