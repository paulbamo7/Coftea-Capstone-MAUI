using Coftea_Capstone.ViewModel;
using Coftea_Capstone.ViewModel.Controls;
using Coftea_Capstone.Views.Controls;
using Coftea_Capstone.Views;
using Coftea_Capstone.Services;

namespace Coftea_Capstone.Views.Pages;

public partial class SalesReport : ContentPage
{
    private bool _isDisposed = false;

	public SalesReport()
	{
		InitializeComponent();
		
		// Update navigation state for indicator
		Appearing += async (_, __) => 
		{
			NavigationStateService.SetCurrentPageType(typeof(SalesReport));
			
			// Check if day has changed and refresh today's data
			var app = (App)Application.Current;
			var viewModel = app.SalesReportVM;
			var today = DateTime.Today;
			
			// Check if we need to refresh today's data (day changed)
			if (viewModel != null)
			{
				await viewModel.CheckAndRefreshIfDayChangedAsync();
			}
		};
		
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

    protected override bool OnBackButtonPressed()
    {
        return BackButtonHandler.HandleBackButton(this);
    }

    private void OnDialogBackgroundTapped(object sender, EventArgs e)
    {
        // Consume the tap to prevent background interaction
        // Don't close dialog on background tap - user must use buttons
    }

    private void OnDialogBackgroundPan(object sender, PanUpdatedEventArgs e)
    {
        // Consume pan gestures to prevent background dragging
        e.Handled = true;
    }

    private void OnDialogFrameTapped(object sender, EventArgs e)
    {
        // Consume the tap on the frame to prevent event bubbling
        // This allows buttons inside to work properly
    }

    private async void OnGenerateButtonClicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("🔵 Generate button clicked directly!");
        try
        {
            var app = (App)Application.Current;
            var viewModel = app.SalesReportVM;
            if (viewModel != null)
            {
                System.Diagnostics.Debug.WriteLine("🔵 Calling AcceptReportDialogAsyncCommand");
                // Manually trigger the command if it exists
                var command = viewModel.AcceptReportDialogAsyncCommand;
                if (command != null && command.CanExecute(null))
                {
                    System.Diagnostics.Debug.WriteLine("🔵 Command exists and can execute, executing...");
                    await command.ExecuteAsync(null);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ AcceptReportDialogAsyncCommand issue - Command is null: {command == null}, CanExecute: {command?.CanExecute(null)}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("❌ ViewModel is null!");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error in OnGenerateButtonClicked: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
        }
    }
}