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
        BindingContext = null;
    }

    private static void ReleaseVisualTree(Microsoft.Maui.IView element)
    {
        if (element == null) return;
        if (element is Image img) img.Source = null;
        else if (element is ImageButton imgBtn) imgBtn.Source = null;
        else if (element is CollectionView cv) cv.ItemsSource = null;
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