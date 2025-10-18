using Microsoft.Maui.Controls;

namespace Coftea_Capstone.Services
{
    /// <summary>
    /// Simple navigation service using MAUI Shell navigation
    /// </summary>
    public static class SimpleNavigationService
    {
        /// <summary>
        /// Navigate to a page using Shell routing
        /// </summary>
        public static async Task NavigateToAsync(string route)
        {
            try
            {
                await Shell.Current.GoToAsync(route);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
            }
        }

        /// <summary>
        /// Navigate to a page with parameters
        /// </summary>
        public static async Task NavigateToAsync(string route, Dictionary<string, object> parameters)
        {
            try
            {
                await Shell.Current.GoToAsync(route, parameters);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
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
