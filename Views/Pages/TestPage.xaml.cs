using Microsoft.Maui.Controls;
using Coftea_Capstone.Services;

namespace Coftea_Capstone.Views.Pages;

public partial class TestPage : ContentPage
{
    public TestPage()
    {
        InitializeComponent();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await SimpleNavigationService.GoBackAsync();
    }
}


