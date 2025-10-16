using Microsoft.Maui.Controls;
using Coftea_Capstone.Views.Controls;

namespace Coftea_Capstone.Services
{
    public static class NavigationAnimationService
    {
        private static LoadingOverlay _loadingOverlay;
        private static readonly object _navigationLock = new object();
        private static bool _isNavigating = false;
        private static readonly SemaphoreSlim _navSemaphore = new SemaphoreSlim(1, 1);
        private static DateTime _lastNavigationTime = DateTime.MinValue;
        private static readonly TimeSpan _navigationCooldown = TimeSpan.FromMilliseconds(350);

        private static bool IsCooldownActive()
        {
            var since = DateTime.UtcNow - _lastNavigationTime;
            return since < _navigationCooldown;
        }

        private static void MarkNavigation()
        {
            _lastNavigationTime = DateTime.UtcNow;
        }

        private static async Task RunOnMainThreadAsync(Func<Task> action)
        {
            if (MainThread.IsMainThread)
            {
                await action();
            }
            else
            {
                await MainThread.InvokeOnMainThreadAsync(action);
            }
        }
        public static async Task PushWithAnimationAsync(this NavigationPage navigationPage, Page page, bool animated = false)
        {
            if (navigationPage == null || page == null) return;

            // Coalesce rapid requests
            if (IsCooldownActive()) return;

            await _navSemaphore.WaitAsync();
            try
            {
                if (navigationPage.CurrentPage?.GetType() == page.GetType())
                {
                    System.Diagnostics.Debug.WriteLine("🚫 PushWithAnimationAsync: Already on the same page, skipping");
                    return;
                }

                MarkNavigation();

                if (!animated)
                {
                    await RunOnMainThreadAsync(() => navigationPage.PushAsync(page, false));
                    return;
                }

                // Animations disabled (kept for parity)
                await RunOnMainThreadAsync(() => navigationPage.PushAsync(page, false));
            }
            finally
            {
                _navSemaphore.Release();
            }
        }

        public static async Task PopWithAnimationAsync(this NavigationPage navigationPage, bool animated = false)
        {
            if (navigationPage == null) return;

            // Coalesce rapid requests
            if (IsCooldownActive()) return;

            await _navSemaphore.WaitAsync();
            try
            {
                if (navigationPage.Navigation?.NavigationStack?.Count <= 1)
                {
                    System.Diagnostics.Debug.WriteLine("🚫 PopWithAnimationAsync: No pages to pop");
                    return;
                }

                MarkNavigation();

                if (!animated)
                {
                    await RunOnMainThreadAsync(() => navigationPage.PopAsync(false));
                    return;
                }

                // Animations disabled
                await RunOnMainThreadAsync(() => navigationPage.PopAsync(false));
            }
            finally
            {
                _navSemaphore.Release();
            }
        }

        public static async Task PopToRootWithAnimationAsync(this NavigationPage navigationPage, bool animated = false)
        {
            if (navigationPage == null) return;

            // Coalesce rapid requests
            if (IsCooldownActive()) return;

            await _navSemaphore.WaitAsync();
            try
            {
                MarkNavigation();

                if (!animated)
                {
                    await RunOnMainThreadAsync(() => navigationPage.PopToRootAsync(false));
                    return;
                }

                // Animations disabled
                await RunOnMainThreadAsync(() => navigationPage.PopToRootAsync(false));
            }
            finally
            {
                _navSemaphore.Release();
            }
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
                if (navigationPage == null || newPage == null) return;

                // Coalesce rapid requests
                if (IsCooldownActive()) return;

                await _navSemaphore.WaitAsync();
                MarkNavigation();

                if (!animated)
                {
                    // Use safer navigation method
                    await SafeReplacePageAsync(navigationPage, newPage);
                    return;
                }

                var currentPage = navigationPage.CurrentPage;
                if (currentPage == null) return;

                // Animations disabled
                await ShowLoadingOverlayAsync(navigationPage);
                await SafeReplacePageAsync(navigationPage, newPage);
                await Task.Delay(50);
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
                _navSemaphore.Release();
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
                    await RunOnMainThreadAsync(() => navigationPage.PopAsync(false));
                }

                // Push the new page
                await RunOnMainThreadAsync(() => navigationPage.PushAsync(newPage, false));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SafeReplacePageAsync error: {ex.Message}");
                // Fallback: try simple push
                await RunOnMainThreadAsync(() => navigationPage.PushAsync(newPage, false));
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
