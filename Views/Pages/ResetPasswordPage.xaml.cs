using Coftea_Capstone.ViewModel;

namespace Coftea_Capstone.Views.Pages;

public partial class ResetPasswordPage : ContentPage
{
    public ResetPasswordPage()
    {
        InitializeComponent();
        // Get email from App static property
        string email = App.ResetPasswordEmail ?? string.Empty;
        BindingContext = new ResetPasswordPageViewModel(email);
        
        // Set RetryConnectionPopup binding context
        RetryConnectionPopup.BindingContext = ((App)Application.Current).RetryConnectionPopup;
    }

    private void OnToggleNewPasswordVisibility(object sender, EventArgs e)
    {
        if (NewPasswordEntry == null) return;
        NewPasswordEntry.IsPassword = !NewPasswordEntry.IsPassword;
        if (sender is ImageButton btn)
        {
            btn.Source = NewPasswordEntry.IsPassword ? "show.png" : "hidden.png";
        }
    }

    private void OnToggleConfirmPasswordVisibility(object sender, EventArgs e)
    {
        if (ConfirmPasswordEntry == null) return;
        ConfirmPasswordEntry.IsPassword = !ConfirmPasswordEntry.IsPassword;
        if (sender is ImageButton btn)
        {
            btn.Source = ConfirmPasswordEntry.IsPassword ? "show.png" : "hidden.png";
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
        // Avoid clearing image sources to prevent blank visuals when returning
        // Don't clear CollectionView ItemsSource to prevent data loss when navigating
        // if (element is CollectionView cv) cv.ItemsSource = null;
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


