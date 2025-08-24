using SQLite;

namespace Coftea_Capstone.Views;

public partial class LoginPage : ContentPage
{
    
    public LoginPage()
	{
		InitializeComponent();
        Routing.RegisterRoute(nameof(Employee), typeof(Employee));
        Routing.RegisterRoute(nameof(RegisterPage), typeof(RegisterPage));
    }

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        Shell.Current.GoToAsync(nameof(RegisterPage));
    }
    private void LoginBtn_Clicked(object sender, EventArgs e)
    {
 
        Shell.Current.GoToAsync(nameof(Employee));
    }
    
}