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
		 
		// Set maximum date to today for birthday picker
		BirthdayDatePicker.MaximumDate = DateTime.Today;
	}

	private void OnToggleRegisterPasswordVisibility(object sender, EventArgs e)
	{
		if (RegisterPasswordEntry == null) return;
		RegisterPasswordEntry.IsPassword = !RegisterPasswordEntry.IsPassword;
		if (sender is ImageButton btn)
		{
			btn.Source = RegisterPasswordEntry.IsPassword ? "show.png" : "hidden.png";
		}
	}

	private void OnToggleRegisterConfirmPasswordVisibility(object sender, EventArgs e)
	{
		if (RegisterConfirmPasswordEntry == null) return;
		RegisterConfirmPasswordEntry.IsPassword = !RegisterConfirmPasswordEntry.IsPassword;
		if (sender is ImageButton btn)
		{
			btn.Source = RegisterConfirmPasswordEntry.IsPassword ? "show.png" : "hidden.png";
		}
	}

}