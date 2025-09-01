namespace Coftea_Capstone.Pages;

public partial class RegisterPage : ContentPage
{
	public RegisterPage()
	{
		InitializeComponent();
	}
    private void RegisterBtn_Clicked(object sender, EventArgs e)
    {
        Navigation.PopAsync();
        Navigation.PushModalAsync(new LoginPage());
    }
}