using Coftea_Capstone.ViewModel;
using Coftea_Capstone.ViewModel.Controls;
using Coftea_Capstone.Services;

namespace Coftea_Capstone.Views.Pages;

public partial class UserManagement : ContentPage
{
    private bool _isDisposed = false;

    public UserManagement()
    {
        InitializeComponent();
        
        // Ensure data loads properly, even if Appearing event doesn't fire consistently with Shell navigation
        Task.Run(async () =>
        {
            await Task.Delay(100); // Small delay to ensure page is fully loaded
            if (BindingContext is UserManagementPageViewModel vm)
            {
                await vm.InitializeAsync();
            }
        });

        // Responsive behavior
        SizeChanged += OnSizeChanged;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        System.Diagnostics.Debug.WriteLine("🔧 UserManagement page appearing");
        NavigationStateService.SetCurrentPageType(typeof(UserManagement));
        
        // Ensure data is loaded when page becomes visible
        if (BindingContext is UserManagementPageViewModel vm)
        {
            _ = vm.InitializeAsync();
        }
    }

    private void OnBellClicked(object sender, EventArgs e)
    {
        var popup = ((App)Application.Current).NotificationPopup;
        popup?.ToggleCommand.Execute(null);
    }

    private void OnSizeChanged(object sender, EventArgs e)
    {
        if (_isDisposed) return;

        double pageWidth = Width;
        if (double.IsNaN(pageWidth) || pageWidth <= 0)
        {
            return;
        }

        bool isPhoneLike = pageWidth < 800;

        if (SidebarColumn is not null)
        {
            SidebarColumn.Width = isPhoneLike ? new GridLength(0) : new GridLength(200);
        }
        if (Sidebar is not null)
        {
            Sidebar.IsVisible = !isPhoneLike;
        }
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

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        _isDisposed = true;

        try
        {
            SizeChanged -= OnSizeChanged;
        }
        catch { }

        // Don't dispose BindingContext or clear CollectionView ItemsSource
        // This prevents data loss when navigating between pages
        // The data will persist and be available when returning to the page

        try
        {
            Handler?.DisconnectHandler();
        }
        catch { }
    }

    private static void ReleaseVisualTree(Microsoft.Maui.IView element)
    {
        if (element == null) return;
        if (element is Image img) img.Source = null;
        else if (element is ImageButton imgBtn) imgBtn.Source = null;
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


