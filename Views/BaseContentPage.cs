using Microsoft.Maui.Controls;

namespace Coftea_Capstone.Views
{
    public static class BackButtonHandler
    {
        public static bool HandleBackButton(ContentPage page)
        {
            // Check if we're on a root page (login or dashboard)
            var currentRoute = Shell.Current?.CurrentState?.Location?.ToString() ?? "";
            var rootPages = new[] { "//login", "//dashboard" };
            bool isRootPage = rootPages.Any(root => currentRoute.StartsWith(root, StringComparison.OrdinalIgnoreCase));

            if (isRootPage)
            {
                // On root pages, allow default behavior (close app)
                return false; // Don't consume, allow default
            }

            // For other pages, try to navigate back
            _ = Task.Run(async () =>
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    try
                    {
                        // Try Shell navigation back
                        if (Shell.Current != null)
                        {
                            await Shell.Current.GoToAsync("..");
                        }
                    }
                    catch
                    {
                        // If navigation fails, try going to dashboard
                        try
                        {
                            if (Shell.Current != null)
                            {
                                await Shell.Current.GoToAsync("//dashboard");
                            }
                        }
                        catch
                        {
                            // If all else fails, allow default behavior
                        }
                    }
                });
            });

            return true; // Consume the back button press
        }
    }
}

