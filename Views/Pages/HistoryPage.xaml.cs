using Coftea_Capstone.ViewModel;

namespace Coftea_Capstone.Views.Pages;

public partial class HistoryPage : ContentPage
{
    public HistoryPageViewModel HistoryViewModel { get; set; }

    public HistoryPage()
    {
        InitializeComponent();
        
        HistoryViewModel = new HistoryPageViewModel();
        BindingContext = HistoryViewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        try
        {
            await HistoryViewModel.InitializeAsync();
        }
        catch (Exception ex)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await DisplayAlert("History Error", ex.Message, "OK");
            });
        }
    }
}
