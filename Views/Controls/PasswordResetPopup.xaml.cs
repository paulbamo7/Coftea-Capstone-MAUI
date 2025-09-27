using Coftea_Capstone.ViewModel.Controls;

namespace Coftea_Capstone.Views.Controls;

public partial class PasswordResetPopup : ContentView
{
    public PasswordResetPopup()
    {
        InitializeComponent();
    }

    private void OnToggleNewPasswordVisibility(object sender, EventArgs e)
    {
        if (NewPasswordEntry == null) return;
        NewPasswordEntry.IsPassword = !NewPasswordEntry.IsPassword;
        if (sender is Button btn)
        {
            btn.Text = NewPasswordEntry.IsPassword ? "Show" : "Hide";
        }
    }

    private void OnToggleConfirmPasswordVisibility(object sender, EventArgs e)
    {
        if (ConfirmPasswordEntry == null) return;
        ConfirmPasswordEntry.IsPassword = !ConfirmPasswordEntry.IsPassword;
        if (sender is Button btn)
        {
            btn.Text = ConfirmPasswordEntry.IsPassword ? "Show" : "Hide";
        }
    }
}
