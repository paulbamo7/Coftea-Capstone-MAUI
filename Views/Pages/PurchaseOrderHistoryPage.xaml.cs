using Coftea_Capstone.ViewModel.Pages;
using Coftea_Capstone.Views;
using Coftea_Capstone.Services;

namespace Coftea_Capstone.Views.Pages;

public partial class PurchaseOrderHistoryPage : ContentPage
{
    private bool _isDisposed = false;

    public PurchaseOrderHistoryPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Check if user is admin/owner - only they can access Purchase Order
        if (App.CurrentUser == null || !App.CurrentUser.IsAdmin)
        {
            await Application.Current.MainPage.DisplayAlert("Unauthorized", "Only administrators can access Purchase Order.", "OK");
            await Shell.Current.GoToAsync("//dashboard");
            return;
        }
        
        if (BindingContext is PurchaseOrderHistoryPageViewModel vm)
        {
            await vm.LoadPurchaseOrdersCommand.ExecuteAsync(null);
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _isDisposed = true;

        // Clean up data to free memory before navigation
        try
        {
            if (BindingContext is PurchaseOrderHistoryPageViewModel vm)
            {
                // Clear collections to free memory
                vm.PurchaseOrders?.Clear();
                vm.OrderDetailItems?.Clear();
                vm.SelectedOrderDetails = null;
                vm.IsDetailsPopupVisible = false;
            }

            // Release visual tree
            ReleaseVisualTree(Content);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OnDisappearing: {ex.Message}");
        }

        // Force native view detachment
        try
        {
            Handler?.DisconnectHandler();
        }
        catch { }
    }

    private static void ReleaseVisualTree(Microsoft.Maui.IView element)
    {
        if (element == null) return;

        if (element is CollectionView cv)
        {
            // Don't clear CollectionView ItemsSource to prevent data loss when navigating
            // cv.ItemsSource = null;
        }
        else if (element is ListView lv)
        {
            lv.ItemsSource = null;
        }

        if (element is ContentView contentView && contentView.Content != null)
        {
            ReleaseVisualTree(contentView.Content);
        }
        else if (element is Layout layout)
        {
            foreach (var child in layout.Children)
            {
                ReleaseVisualTree(child);
            }
        }
        else if (element is ScrollView sv && sv.Content != null)
        {
            ReleaseVisualTree(sv.Content);
        }
        else if (element is ContentPage page && page.Content != null)
        {
            ReleaseVisualTree(page.Content);
        }
    }

    private void OnBellClicked(object sender, EventArgs e)
    {
        var popup = ((App)Application.Current).NotificationPopup;
        popup?.ToggleCommand.Execute(null);
    }

    private void OnProfileClicked(object sender, EventArgs e)
    {
        try
        {
            var app = (App)Application.Current;
            app?.ProfilePopup?.ShowProfile();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error opening profile: {ex.Message}");
        }
    }

    protected override bool OnBackButtonPressed()
    {
        return BackButtonHandler.HandleBackButton(this);
    }
}

