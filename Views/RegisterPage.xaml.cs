namespace Coftea_Capstone.Views;

public partial class RegisterPage : ContentPage
{
	public RegisterPage()
	{
		InitializeComponent();
	}
    private void RegisterBtn_Clicked(object sender, EventArgs e)
    {

        Shell.Current.GoToAsync(nameof(LoginPage));
    }
}