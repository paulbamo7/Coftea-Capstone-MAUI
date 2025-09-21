using Coftea_Capstone.ViewModel;

namespace Coftea_Capstone.Views.Pages;

public partial class ForgotPasswordPage : ContentPage
{
    public ForgotPasswordPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        // Set RetryConnectionPopup binding context
        RetryConnectionPopup.BindingContext = ((App)Application.Current).RetryConnectionPopup;
    }
}
