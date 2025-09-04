using Coftea_Capstone.ViewModel;
using Coftea_Capstone.Models;
using Coftea_Capstone.C_;

namespace Coftea_Capstone.Pages;

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
        _viewModel = new POSPageViewModel(database);

        // Set as BindingContext
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
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