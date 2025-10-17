using Microsoft.Maui.Controls;
using Coftea_Capstone.Views.Controls;

namespace Coftea_Capstone.Services
{
    public static class NavigationAnimationService
    {
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
                // Always use non-animated, direct replace without overlays
                await SafeReplacePageAsync(navigationPage, newPage);
#if DEBUG
                // Optional collection during testing to reveal leaks more clearly
                ForceGC();
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex}");
                try
                {
                    // Last-resort fallback to prevent app crash: reset root
                    await RunOnMainThreadAsync(() =>
                    {
                        Application.Current.MainPage = new NavigationPage(newPage);
                        return Task.CompletedTask;
                    });
                }
                catch (Exception ex2)
                {
                    System.Diagnostics.Debug.WriteLine($"Fallback navigation error: {ex2}");
                }
                // Swallow to avoid crashing the app
                return;
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

                // Efficiently reset stack to the new page with minimal redraws
                await RunOnMainThreadAsync(async () =>
                {
                    var nav = navigationPage.Navigation;
                    var stack = nav.NavigationStack;
                    if (stack == null || stack.Count == 0)
                    {
                        await navigationPage.PushAsync(newPage, false);
                        return;
                    }

                    // Insert the new page before the root, then pop to root once
                    var root = stack[0];
                    // Remove intermediate pages to avoid stack overgrowth and retained views
                    foreach (var p in stack.ToList())
                    {
                        if (p != null && p != root && p.GetType() != newPage.GetType())
                        {
                            try { nav.RemovePage(p); } catch { }
                        }
                    }
                    if (root?.GetType() == newPage.GetType())
                    {
                        await navigationPage.PopToRootAsync(false);
                        // Ensure only a single root remains
                        var s1 = nav.NavigationStack;
                        for (int i = s1.Count - 1; i > 0; i--)
                        {
                            try { nav.RemovePage(s1[i]); } catch { }
                        }
                        return;
                    }

                    nav.InsertPageBefore(newPage, root);
                    await navigationPage.PopToRootAsync(false);

                    // Explicitly clean any residual pages after popping to root
                    var s2 = nav.NavigationStack;
                    foreach (var page in s2.Skip(1).ToList())
                    {
                        try { nav.RemovePage(page); } catch { }
                    }
                    return;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SafeReplacePageAsync error: {ex.Message}");
                // Fallback: try simple push
                await RunOnMainThreadAsync(() => navigationPage.PushAsync(newPage, false));
            }
        }

        // Overlays disabled: keep no-op helpers to avoid refactor churn
        private static Task ShowLoadingOverlayAsync(NavigationPage navigationPage) => Task.CompletedTask;
        private static Task HideLoadingOverlayAsync() => Task.CompletedTask;

        [System.Diagnostics.Conditional("DEBUG")]
        private static void ForceGC()
        {
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch { }
        }
    }
}
