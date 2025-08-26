using SQLite;

namespace Coftea_Capstone.Pages;

public partial class LoginPage : ContentPage
{
    
    public LoginPage()
	{
		InitializeComponent();
    }

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        Navigation.PopModalAsync();
        Navigation.PushModalAsync(new RegisterPage());
    }
    private void LoginBtn_Clicked(object sender, EventArgs e)
    {
        Navigation.PopModalAsync();
        Navigation.PushModalAsync(new EmployeeDashboard());
    }
    
}