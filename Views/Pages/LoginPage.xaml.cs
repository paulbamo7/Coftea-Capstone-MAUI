using Coftea_Capstone;
using Coftea_Capstone.C_;
using Coftea_Capstone.Models;
using Coftea_Capstone.Views.Controls;
using Coftea_Capstone.ViewModel;
using Microsoft.Maui.Storage;
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
        if (sender is ImageButton btn)
        {
            btn.Source = LoginPasswordEntry.IsPassword ? "show.png" : "hidden.png";
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        // Clear fields when returning to login page
        if (BindingContext is LoginPageViewModel viewModel)
        {
            bool rememberMe = Preferences.Get("RememberMe", false);
            if (!rememberMe)
            {
                viewModel.Email = string.Empty;
                viewModel.RememberMe = false;
            }
            // Always clear password for security when returning to login page
            viewModel.Password = string.Empty;
            viewModel.PasswordErrorMessage = string.Empty;
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        try
        {
            if (Content != null)
            {
                ReleaseVisualTree(Content);
            }
        }
        catch { }
    }

    private static void ReleaseVisualTree(Microsoft.Maui.IView element)
    {
        if (element == null) return;
        // Intentionally avoid clearing Image and ImageButton sources so that static assets
        // like the login logo are preserved when navigating back to this page.
        // Don't clear CollectionView ItemsSource to prevent data loss when navigating
        // else if (element is CollectionView cv) cv.ItemsSource = null;
        else if (element is ListView lv) lv.ItemsSource = null;
        if (element is ContentView contentView && contentView.Content != null)
            ReleaseVisualTree(contentView.Content);
        else if (element is Layout layout)
            foreach (var child in layout.Children) ReleaseVisualTree(child);
        else if (element is ScrollView sv && sv.Content != null)
            ReleaseVisualTree(sv.Content);
        else if (element is ContentPage page && page.Content != null)
            ReleaseVisualTree(page.Content);
    }
}