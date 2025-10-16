using Coftea_Capstone.ViewModel;

namespace Coftea_Capstone.Views.Pages;

public partial class ResetPasswordPage : ContentPage
{
    public ResetPasswordPage(string email)
    {
        InitializeComponent();
        BindingContext = new ResetPasswordPageViewModel(email);
    }
}


