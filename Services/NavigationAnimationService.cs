using Microsoft.Maui.Controls;
using Coftea_Capstone.Views.Controls;

namespace Coftea_Capstone.Services
{
    public static class NavigationAnimationService
    {
        private static LoadingOverlay _loadingOverlay;
        private static readonly object _navigationLock = new object();
        private static bool _isNavigating = false;
        public static async Task PushWithAnimationAsync(this NavigationPage navigationPage, Page page, bool animated = false)
        {
            if (!animated)
            {
                await navigationPage.PushAsync(page, false);
                return;
            }

            // Push the page without animation first
            await navigationPage.PushAsync(page, false);

            // Wait for the page to be fully loaded
            await Task.Delay(100);

            // Set initial state for the new page
            page.Opacity = 0;
            page.TranslationX = 300; // Start from the right

            // Animate the page in
            await Task.WhenAll(
                page.FadeTo(1, 300, Easing.CubicOut),
                page.TranslateTo(0, 0, 300, Easing.CubicOut)
            );
        }

        public static async Task PopWithAnimationAsync(this NavigationPage navigationPage, bool animated = false)
        {
            if (!animated)
            {
                await navigationPage.PopAsync(false);
                return;
            }

            var currentPage = navigationPage.CurrentPage;
            if (currentPage == null) return;

            // Animate the current page out
            await Task.WhenAll(
                currentPage.FadeTo(0, 250, Easing.CubicIn),
                currentPage.TranslateTo(300, 0, 250, Easing.CubicIn)
            );

            // Pop the page
            await navigationPage.PopAsync(false);
        }

        public static async Task PopToRootWithAnimationAsync(this NavigationPage navigationPage, bool animated = false)
        {
            if (!animated)
            {
                await navigationPage.PopToRootAsync(false);
                return;
            }

            var currentPage = navigationPage.CurrentPage;
            if (currentPage == null) return;

            // Animate the current page out
            await Task.WhenAll(
                currentPage.FadeTo(0, 250, Easing.CubicIn),
                currentPage.TranslateTo(300, 0, 250, Easing.CubicIn)
            );

            // Pop to root
            await navigationPage.PopToRootAsync(false);
        }

        public static async Task ReplaceWithAnimationAsync(this NavigationPage navigationPage, Page newPage, bool animated = false)
        {
            // Check if navigation is already in progress
            lock (_navigationLock)
            {
                if (_isNavigating)
                {
                    System.Diagnostics.Debug.WriteLine("🚫 NavigationAnimationService: Navigation already in progress, skipping");
                    return;
                }
                _isNavigating = true;
            }

            try
            {
                if (!animated)
                {
                    // Use safer navigation method
                    await SafeReplacePageAsync(navigationPage, newPage);
                    return;
                }

                var currentPage = navigationPage.CurrentPage;
                if (currentPage == null) return;

                // Show loading overlay
                await ShowLoadingOverlayAsync(navigationPage);

                // Animate current page out
                await Task.WhenAll(
                    currentPage.FadeTo(0, 250, Easing.CubicIn),
                    currentPage.TranslateTo(-300, 0, 250, Easing.CubicIn)
                );

                // Use safer navigation method
                await SafeReplacePageAsync(navigationPage, newPage);

                // Wait for the new page to be fully loaded
                await Task.Delay(100);

                // Set initial state for the new page
                newPage.Opacity = 0;
                newPage.TranslationX = 300;

                // Animate new page in
                await Task.WhenAll(
                    newPage.FadeTo(1, 300, Easing.CubicOut),
                    newPage.TranslateTo(0, 0, 300, Easing.CubicOut)
                );

                // Hide loading overlay
                await HideLoadingOverlayAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
                // Ensure loading overlay is hidden even if navigation fails
                await HideLoadingOverlayAsync();
                throw;
            }
            finally
            {
                lock (_navigationLock)
                {
                    _isNavigating = false;
                }
            }
        }

        private static async Task SafeReplacePageAsync(NavigationPage navigationPage, Page newPage)
        {
            try
            {
                // Check if we're already on the same page type
                if (navigationPage.CurrentPage?.GetType() == newPage.GetType())
                {
                    System.Diagnostics.Debug.WriteLine("🚫 SafeReplacePageAsync: Already on the same page, skipping navigation");
                    return;
                }

                // Clear navigation stack and push new page
                var navigationStack = navigationPage.Navigation.NavigationStack.ToList();
                
                // Pop all pages except the root
                while (navigationPage.Navigation.NavigationStack.Count > 1)
                {
                    await navigationPage.PopAsync(false);
                }
                
                // Push the new page
                await navigationPage.PushAsync(newPage, false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SafeReplacePageAsync error: {ex.Message}");
                // Fallback: try simple push
                await navigationPage.PushAsync(newPage, false);
            }
        }

        private static async Task ShowLoadingOverlayAsync(NavigationPage navigationPage)
        {
            if (_loadingOverlay == null)
            {
                _loadingOverlay = new LoadingOverlay();
            }
            else
            {
                // Remove from current parent if it exists to prevent IllegalStateException
                ViewParentHelper.SafeRemoveFromParent(_loadingOverlay);
            }

            // Add overlay to the current page's root content
            if (navigationPage.CurrentPage is ContentPage currentPage)
            {
                // Always wrap in a grid to ensure proper positioning
                if (currentPage.Content is not Grid)
                {
                    var wrapperGrid = new Grid();
                    wrapperGrid.Children.Add(currentPage.Content);
                    currentPage.Content = wrapperGrid;
                }

                if (currentPage.Content is Grid rootGrid)
                {
                    // Use safe method to add overlay to prevent IllegalStateException
                    ViewParentHelper.SafeAddToParent(_loadingOverlay, rootGrid);
                }
            }

            await _loadingOverlay.ShowAsync();
        }

        private static async Task HideLoadingOverlayAsync()
        {
            if (_loadingOverlay != null)
            {
                await _loadingOverlay.HideAsync();
                
                // Remove from parent with additional safety checks
                ViewParentHelper.SafeRemoveFromParent(_loadingOverlay);
            }
        }
    }
}
