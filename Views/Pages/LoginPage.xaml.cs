using Coftea_Capstone;
using Coftea_Capstone.C_;
using Coftea_Capstone.Models;
using Coftea_Capstone.Views.Controls;
using SQLite;

namespace Coftea_Capstone.Views.Pages;

public partial class LoginPage : ContentPage
{
    private readonly Database _database;
    public LoginPage()
    {
        InitializeComponent();
        _database = new Database();
        
        // Set RetryConnectionPopup binding context
        RetryConnectionPopup.BindingContext = ((App)Application.Current).RetryConnectionPopup;
    }

    private void OnToggleLoginPasswordVisibility(object sender, EventArgs e)
    {
        if (LoginPasswordEntry == null) return;
        LoginPasswordEntry.IsPassword = !LoginPasswordEntry.IsPassword;
        if (sender is Button btn)
        {
            btn.Text = LoginPasswordEntry.IsPassword ? "Show" : "Hide";
        }
    }
}