using Coftea_Capstone.ViewModel;
using Coftea_Capstone.ViewModel.Controls;
using Coftea_Capstone.Views.Controls;

namespace Coftea_Capstone.Views.Pages;

public partial class SalesReport : ContentPage
{
    private bool _isDisposed = false;

	public SalesReport()
	{
		InitializeComponent();
		
		// Use shared SalesReportVM from App to prevent memory leaks
		var app = (App)Application.Current;
		var viewModel = app.SalesReportVM;
		BindingContext = viewModel;
		
		// RetryConnectionPopup is now handled globally through App.xaml.cs
		
		// Initialize the view model
		_ = viewModel.InitializeAsync();

		// Responsive behavior
		SizeChanged += OnSizeChanged;
	}

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        _isDisposed = true;

        // Detach handlers
        try
        {
            SizeChanged -= OnSizeChanged;
        }
        catch { }

        // Release visual tree in background
        _ = Task.Run(() =>
        {
            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        ReleaseVisualTree(Content);
                    }
                    catch { }
                });
            }
            catch { }
        });

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
            cv.ItemsSource = null;
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
        _ = popup?.AddSuccess("Sales Report", "Generated Daily Report (ID: DR20251006)", "ID: DR20251006");
        _ = popup?.AddSuccess("Sales Report", "Exported to CSV (ID: CSV106)", "ID: CSV106");
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
}