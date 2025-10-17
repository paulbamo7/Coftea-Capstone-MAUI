using Coftea_Capstone.ViewModel;
using Coftea_Capstone.ViewModel.Controls;

namespace Coftea_Capstone.Views.Pages;

public partial class UserManagement : ContentPage
{
    private bool _isDisposed = false;

    public UserManagement()
    {
        InitializeComponent();
        if (BindingContext is UserManagementPageViewModel vm)
        {
            _ = vm.InitializeAsync();
        }

        // Responsive behavior
        SizeChanged += OnSizeChanged;
    }

    private void OnBellClicked(object sender, EventArgs e)
    {
        var popup = ((App)Application.Current).NotificationPopup;
        _ = popup?.AddSuccess("User Management", "Successfully created user. ID: UM1001", "ID: UM1001");
        _ = popup?.AddSuccess("User Management", "Approved user request. ID: UR995", "ID: UR995");
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

        _ = Task.Run(() =>
        {
            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        ReleaseVisualTree(Content);
                        BindingContext = null;
                    }
                    catch { }
                });
            }
            catch { }
        });

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


