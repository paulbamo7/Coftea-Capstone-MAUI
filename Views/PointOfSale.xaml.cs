using Coftea_Capstone.C_;
using Coftea_Capstone.Models;
using Coftea_Capstone.ViewModel;
using Coftea_Capstone.View;
using Microsoft.Maui.ApplicationModel.Communication;
using Coftea_Capstone.Pages;

namespace Coftea_Capstone.Views;

public partial class PointOfSale : ContentPage
{
    private readonly POSPageViewModel _viewModel;
    public PointOfSale()
	{
		InitializeComponent();
        
        // Initialize DB
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "coftea.db3");
        var database = new Database(dbPath);

        // Create ViewModel
        _viewModel = new POSPageViewModel();

        // Set as BindingContext
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync(App.CurrentUser?.Email);
        if (BindingContext is POSPageViewModel vm && App.CurrentUser != null)
        {
            vm.IsAdmin = App.CurrentUser.IsAdmin;
        }   
        await _viewModel.LoadDataAsync();
    }

    private void HomeButton_Clicked(object sender, EventArgs e)
    {

    }

    private void POSButton_Clicked(object sender, EventArgs e)
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