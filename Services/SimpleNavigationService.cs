using Microsoft.Maui.Controls;

namespace Coftea_Capstone.Services
{
    public static class SimpleNavigationService
    {
        private static readonly SemaphoreSlim _navLock = new SemaphoreSlim(1, 1);
        private static DateTime _lastNav = DateTime.MinValue;
        private static readonly TimeSpan _cooldown = TimeSpan.FromMilliseconds(400);

        private static async Task RunOnMainAsync(Func<Task> fn)
        {
            if (MainThread.IsMainThread)
            {
                await fn();
            }
            else
            {
                await MainThread.InvokeOnMainThreadAsync(fn);
            }
        }

        public static async Task NavigateToAsync(Func<Page> createPage)
        {
            if (createPage == null) return;

            // Coalesce rapid clicks
            if (DateTime.UtcNow - _lastNav < _cooldown) return;

            await _navLock.WaitAsync();
            try
            {
                _lastNav = DateTime.UtcNow;

                var nav = Application.Current?.MainPage as NavigationPage;
                if (nav == null)
                {
                    // Initialize navigation root if missing
                    await RunOnMainAsync(() =>
                    {
                        Application.Current.MainPage = new NavigationPage(createPage());
                        return Task.CompletedTask;
                    });
                    return;
                }

                var newPage = createPage();
                if (newPage == null) return;

                // Skip if already the same page type
                if (nav.CurrentPage?.GetType() == newPage.GetType()) return;

                await RunOnMainAsync(async () =>
                {
                    var stack = nav.Navigation.NavigationStack;
                    if (stack == null || stack.Count == 0)
                    {
                        await nav.PushAsync(newPage, false);
                        return;
                    }

                    var root = stack[0];
                    // Insert new page before root, pop to root
                    nav.Navigation.InsertPageBefore(newPage, root);
                    await nav.PopToRootAsync(false);
                    // Clean remaining pages to keep stack lean
                    foreach (var page in nav.Navigation.NavigationStack.Skip(1).ToList())
                    {
                        try { nav.Navigation.RemovePage(page); } catch { }
                    }
                });
            }
            catch
            {
                // Last resort: reset app root to avoid crash
                try
                {
                    await RunOnMainAsync(() =>
                    {
                        Application.Current.MainPage = new NavigationPage(createPage());
                        return Task.CompletedTask;
                    });
                }
                catch { }
            }
            finally
            {
                _navLock.Release();
            }
        }
    }
}


