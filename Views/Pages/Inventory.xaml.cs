using Coftea_Capstone.ViewModel;

namespace Coftea_Capstone.Views.Pages;

public partial class Inventory : ContentPage
{
    private readonly InventoryPageViewModel _viewModel;
    public Inventory()
	{
		InitializeComponent();

        _viewModel = new InventoryPageViewModel();

        // Set as BindingContext    
        BindingContext = _viewModel;
    }
}