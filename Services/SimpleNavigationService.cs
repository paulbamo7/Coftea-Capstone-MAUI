using Microsoft.Maui.Controls;
using Coftea_Capstone.Views.Controls;

namespace Coftea_Capstone.Services
{
    /// <summary>
    /// Simple navigation service using MAUI Shell navigation
    /// </summary>
    public static class SimpleNavigationService
    {
        private static LoadingOverlay _loadingOverlay;
        private static HashSet<string> _visitedRoutes = new HashSet<string>();
        
        /// <summary>
        /// Initialize the loading overlay
        /// </summary>
        public static void InitializeLoadingOverlay(LoadingOverlay overlay)
        {
            _loadingOverlay = overlay;
        }
        
        /// <summary>
        /// Navigate to a page using Shell routing with loading overlay (only on first visit)
        /// </summary>
        public static async Task NavigateToAsync(string route)
        {
            try
            {
                // Only show loading overlay on first visit to this route
                bool isFirstVisit = !_visitedRoutes.Contains(route);
                
                if (isFirstVisit && _loadingOverlay != null)
                {
                    await _loadingOverlay.ShowAsync();
                    await Task.Delay(300); // Small delay to show the logo
                }
                
                await Shell.Current.GoToAsync(route);
                
                // Mark route as visited
                _visitedRoutes.Add(route);
                
                // Hide loading overlay after navigation
                if (isFirstVisit && _loadingOverlay != null)
                {
                    await Task.Delay(100); // Brief delay to ensure page is loaded
                    await _loadingOverlay.HideAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
                // Hide loading overlay on error
                if (_loadingOverlay != null)
                {
                    await _loadingOverlay.HideAsync();
                }
            }
        }

        /// <summary>
        /// Navigate to a page with parameters
        /// </summary>
        public static async Task NavigateToAsync(string route, Dictionary<string, object> parameters)
        {
            try
            {
                // Only show loading overlay on first visit to this route
                bool isFirstVisit = !_visitedRoutes.Contains(route);
                
                if (isFirstVisit && _loadingOverlay != null)
                {
                    await _loadingOverlay.ShowAsync();
                    await Task.Delay(300);
                }
                
                await Shell.Current.GoToAsync(route, parameters);
                
                // Mark route as visited
                _visitedRoutes.Add(route);
                
                // Hide loading overlay after navigation
                if (isFirstVisit && _loadingOverlay != null)
                {
                    await Task.Delay(100);
                    await _loadingOverlay.HideAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
                // Hide loading overlay on error
                if (_loadingOverlay != null)
                {
                    await _loadingOverlay.HideAsync();
                }
            }
        }

        /// <summary>
        /// Navigate back to the previous page
        /// </summary>
        public static async Task GoBackAsync()
        {
            try
            {
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Go back error: {ex.Message}");
            }
        }

        /// <summary>
        /// Navigate to root page
        /// </summary>
        public static async Task GoToRootAsync()
        {
            try
            {
                await Shell.Current.GoToAsync("//");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Go to root error: {ex.Message}");
            }
        }
    }
}
