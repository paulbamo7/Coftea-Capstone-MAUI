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
}

