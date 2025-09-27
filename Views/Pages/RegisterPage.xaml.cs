using Coftea_Capstone.Views.Controls;
using Coftea_Capstone;

namespace Coftea_Capstone.Views.Pages;

public partial class RegisterPage : ContentPage
{
	public RegisterPage()
	{
		InitializeComponent();
		
		// Set RetryConnectionPopup binding context
		 RetryConnectionPopup.BindingContext = ((App)Application.Current).RetryConnectionPopup;
	}

	private void OnToggleRegisterPasswordVisibility(object sender, EventArgs e)
	{
		if (RegisterPasswordEntry == null) return;
		RegisterPasswordEntry.IsPassword = !RegisterPasswordEntry.IsPassword;
		if (sender is Button btn)
		{
			btn.Text = RegisterPasswordEntry.IsPassword ? "Show" : "Hide";
		}
	}

	private void OnToggleRegisterConfirmPasswordVisibility(object sender, EventArgs e)
	{
		if (RegisterConfirmPasswordEntry == null) return;
		RegisterConfirmPasswordEntry.IsPassword = !RegisterConfirmPasswordEntry.IsPassword;
		if (sender is Button btn)
		{
			btn.Text = RegisterConfirmPasswordEntry.IsPassword ? "Show" : "Hide";
		}
	}

}