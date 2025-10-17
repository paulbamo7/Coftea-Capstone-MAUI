using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Coftea_Capstone.Services
{
    public static class CleanNavigationService
    {
        private static readonly SemaphoreSlim _navLock = new(1, 1);
        private static DateTime _lastNavigation = DateTime.MinValue;
        private static readonly TimeSpan _cooldown = TimeSpan.FromMilliseconds(500);

        public static async Task NavigateToAsync<T>() where T : Page, new()
        {
            await NavigateToAsync(() => new T());
        }

        public static async Task NavigateToAsync(Func<Page> createPage)
        {
            if (createPage == null) return;

            // Prevent rapid navigation
            if (DateTime.UtcNow - _lastNavigation < _cooldown) return;

            await _navLock.WaitAsync();
            try
            {
                _lastNavigation = DateTime.UtcNow;

                var nav = Application.Current?.MainPage as NavigationPage;
                if (nav == null)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        var page = createPage();
                        Application.Current.MainPage = new NavigationPage(page);
                    });
                    return;
                }

                var newPage = createPage();
                if (newPage == null) return;

                // Check if already on the same page type
                if (nav.CurrentPage?.GetType() == newPage.GetType())
                {
                    return;
                }

                // Dispose and remove all pages except root (off main thread)
                var pagesToRemove = nav.Navigation.NavigationStack.Skip(1).ToList();
                foreach (var page in pagesToRemove)
                {
                    try
                    {
                        // Force native view detachment
                        page.Handler?.DisconnectHandler();
                        
                        // Dispose if possible
                        if (page is IDisposable disposable)
                            disposable.Dispose();
                    }
                    catch { }
                }

                // Remove pages and push new page on main thread
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    foreach (var page in pagesToRemove)
                    {
                        try
                        {
                            nav.Navigation.RemovePage(page);
                        }
                        catch { }
                    }
                    await nav.PushAsync(newPage, false);
                });

                // Force aggressive garbage collection
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
            finally
            {
                _navLock.Release();
            }
        }

        public static async Task NavigateBackAsync()
        {
            await _navLock.WaitAsync();
            try
            {
                var nav = Application.Current?.MainPage as NavigationPage;
                if (nav?.Navigation.NavigationStack.Count > 1)
                {
                    var currentPage = nav.CurrentPage;
                    
                    // Dispose the page off main thread
                    try
                    {
                        currentPage.Handler?.DisconnectHandler();
                        if (currentPage is IDisposable disposable)
                            disposable.Dispose();
                    }
                    catch { }
                    
                    // Pop on main thread
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await nav.PopAsync(false);
                    });
                }

                // Force aggressive garbage collection
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
            finally
            {
                _navLock.Release();
            }
        }

        public static async Task NavigateToRootAsync<T>() where T : Page, new()
        {
            await NavigateToRootAsync(() => new T());
        }

        public static async Task NavigateToRootAsync(Func<Page> createPage)
        {
            if (createPage == null) return;

            await _navLock.WaitAsync();
            try
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    // Dispose current navigation
                    if (Application.Current?.MainPage is NavigationPage oldNav)
                    {
                        foreach (var page in oldNav.Navigation.NavigationStack)
                        {
                            try
                            {
                                page.Handler?.DisconnectHandler();
                                if (page is IDisposable disposable)
                                    disposable.Dispose();
                            }
                            catch { }
                        }
                    }

                    // Create new root
                    var newPage = createPage();
                    Application.Current.MainPage = new NavigationPage(newPage);
                });

                // Force aggressive garbage collection
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
            finally
            {
                _navLock.Release();
            }
        }
    }
}
