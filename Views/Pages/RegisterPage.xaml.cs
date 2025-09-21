namespace Coftea_Capstone.Views.Pages;

public partial class RegisterPage : ContentPage
{
	public RegisterPage()
	{
		InitializeComponent();
		
		// Set RetryConnectionPopup binding context
		RetryConnectionPopup.BindingContext = ((App)Application.Current).RetryConnectionPopup;
	}
    private void RegisterBtn_Clicked(object sender, EventArgs e)
    {
        Navigation.PopAsync();
        Navigation.PushModalAsync(new LoginPage());
    }
}