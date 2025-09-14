using Coftea_Capstone.ViewModel;
using Coftea_Capstone.Views.Controls;

namespace Coftea_Capstone.Views.Pages;

public partial class PointOfSale : ContentPage
{
    public POSPageViewModel POSViewModel { get; set; }
    public SettingsPopUpViewModel SettingsPopupViewModel { get; set; }
    public AddItemToPOSViewModel AddItemToPOSViewModel { get; set; }

    public PointOfSale()
    {
        InitializeComponent();
        AddItemToPOSViewModel = new AddItemToPOSViewModel();

        // Step 2: Pass the popup VM into Settings VM
        SettingsPopupViewModel = new SettingsPopUpViewModel(AddItemToPOSViewModel);

        // Step 3: Pass both VMs into the POS VM
        POSViewModel = new POSPageViewModel(AddItemToPOSViewModel, SettingsPopupViewModel);

        // Step 4: Set the BindingContext
        BindingContext = POSViewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Reset popup visibility on appearing
        POSViewModel.AddItemToPOSViewModel.IsAddItemToPOSVisible = false;
        POSViewModel.SettingsPopup.IsAddItemToPOSVisible = false; 
        await POSViewModel.LoadDataAsync();
    }
}
