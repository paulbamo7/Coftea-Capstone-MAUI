using Microsoft.Maui.Controls;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Coftea_Capstone.Services
{
    /// <summary>
    /// Thread-safe navigation service that ensures all operations happen on the main thread
    /// and properly manages page lifecycle to prevent crashes and memory leaks.
    /// </summary>
    public static class NavigationService
    {
        private static readonly SemaphoreSlim _navLock = new(1, 1);
        private static DateTime _lastNavTime = DateTime.MinValue;
        private static readonly TimeSpan _minNavInterval = TimeSpan.FromMilliseconds(400);
        private static bool _isNavigating = false;

        /// <summary>
        /// Navigate to a new page, replacing the current navigation stack.
        /// </summary>
        public static async Task NavigateToAsync<T>() where T : Page, new()
        {
            await NavigateToAsync(() => new T());
        }

        /// <summary>
        /// Navigate to a new page using a factory function.
        /// </summary>
        public static async Task NavigateToAsync(Func<Page> pageFactory)
        {
            if (pageFactory == null) return;
            if (_isNavigating) return; // Prevent concurrent navigation

            // Debounce rapid navigation attempts
            var now = DateTime.UtcNow;
            if (now - _lastNavTime < _minNavInterval) return;

            if (!await _navLock.WaitAsync(0))
                return; // Skip if navigation already in progress

            try
            {
                _isNavigating = true;
                _lastNavTime = now;

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    try
                    {
                        var app = Application.Current;
                        if (app == null) return;

                        var currentMainPage = app.MainPage;
                        var nav = currentMainPage as NavigationPage;

                        // Create new page on main thread
                        var newPage = pageFactory();
                        if (newPage == null) return;

                        // Update navigation state for nav bar
                        try
                        {
                            NavigationStateService.SetCurrentPageType(newPage.GetType());
                        }
                        catch { }

                        if (nav == null)
                        {
                            // No navigation page exists, create new root
                            app.MainPage = new NavigationPage(newPage)
                            {
                                BarBackgroundColor = Colors.Transparent,
                                BarTextColor = Colors.Black
                            };
                            
                            // Cleanup old page safely
                            _ = SafeCleanupPageAsync(currentMainPage);
                            return;
                        }

                        var currentPage = nav.CurrentPage;

                        // Skip if already on this page type
                        if (currentPage?.GetType() == newPage.GetType())
                        {
                            return;
                        }

                        // Navigate: push new page without animation
                        await nav.PushAsync(newPage, false);

                        // Clean up old pages after navigation completes
                        _ = CleanupNavigationStackAsync(nav);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Navigation error: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ NavigateToAsync outer error: {ex.Message}");
            }
            finally
            {
                _isNavigating = false;
                _navLock.Release();
            }
        }

        /// <summary>
        /// Navigate back to the previous page.
        /// </summary>
        public static async Task GoBackAsync()
        {
            if (_isNavigating) return;
            if (!await _navLock.WaitAsync(0))
                return;

            try
            {
                _isNavigating = true;

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    try
                    {
                        var app = Application.Current;
                        if (app == null) return;

                        var nav = app.MainPage as NavigationPage;
                        if (nav == null) return;

                        var stack = nav.Navigation?.NavigationStack;
                        if (stack == null || stack.Count <= 1) return;

                        var currentPage = nav.CurrentPage;
                        
                        // Pop without animation
                        await nav.PopAsync(false);
                        
                        // Update state
                        if (nav.CurrentPage != null)
                        {
                            try
                            {
                                NavigationStateService.SetCurrentPageType(nav.CurrentPage.GetType());
                            }
                            catch { }
                        }
                        
                        // Cleanup popped page
                        _ = SafeCleanupPageAsync(currentPage);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ GoBackAsync error: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ GoBackAsync outer error: {ex.Message}");
            }
            finally
            {
                _isNavigating = false;
                _navLock.Release();
            }
        }

        /// <summary>
        /// Replace the entire navigation stack with a new root page.
        /// </summary>
        public static async Task SetRootAsync<T>() where T : Page, new()
        {
            await SetRootAsync(() => new T());
        }

        /// <summary>
        /// Replace the entire navigation stack with a new root page.
        /// </summary>
        public static async Task SetRootAsync(Func<Page> pageFactory)
        {
            if (pageFactory == null) return;
            if (_isNavigating) return;

            if (!await _navLock.WaitAsync(0))
                return;

            try
            {
                _isNavigating = true;

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    try
                    {
                        var app = Application.Current;
                        if (app == null) return;

                        var oldMainPage = app.MainPage;
                        var newPage = pageFactory();
                        
                        if (newPage != null)
                        {
                            app.MainPage = new NavigationPage(newPage)
                            {
                                BarBackgroundColor = Colors.Transparent,
                                BarTextColor = Colors.Black
                            };
                            
                            try
                            {
                                NavigationStateService.SetCurrentPageType(newPage.GetType());
                            }
                            catch { }
                            
                            // Cleanup old tree
                            _ = SafeCleanupPageAsync(oldMainPage);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ SetRootAsync error: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ SetRootAsync outer error: {ex.Message}");
            }
            finally
            {
                _isNavigating = false;
                _navLock.Release();
            }
        }

        /// <summary>
        /// Safely cleanup a page's resources on the main thread.
        /// </summary>
        private static async Task SafeCleanupPageAsync(IView page)
        {
            if (page == null) return;

            // Wait a bit for any animations to complete
            await Task.Delay(300);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                try
                {
                    // Clear binding context first
                    if (page is Page p)
                    {
                        try
                        {
                            p.BindingContext = null;
                        }
                        catch { }
                    }

                    // Disconnect handler (must be on main thread)
                    try
                    {
                        page.Handler?.DisconnectHandler();
                    }
                    catch { }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Cleanup error: {ex.Message}");
                }
            });

            // Dispose off main thread if supported
            if (page is IDisposable disposable)
            {
                try
                {
                    disposable.Dispose();
                }
                catch { }
            }
        }

        /// <summary>
        /// Clean up old pages from navigation stack, keeping only root and current.
        /// </summary>
        private static async Task CleanupNavigationStackAsync(NavigationPage nav)
        {
            if (nav == null) return;

            await Task.Delay(500); // Wait for navigation to settle

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                try
                {
                    var stack = nav.Navigation?.NavigationStack;
                    if (stack == null || stack.Count <= 2) return;

                    // Remove all pages except root (index 0) and current (last)
                    for (int i = stack.Count - 2; i > 0; i--)
                    {
                        try
                        {
                            var pageToRemove = stack[i];
                            nav.Navigation.RemovePage(pageToRemove);
                            
                            // Cleanup the removed page
                            _ = SafeCleanupPageAsync(pageToRemove);
                        }
                        catch { }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Stack cleanup error: {ex.Message}");
                }
            });
        }
    }
}
