using System;

namespace Coftea_Capstone.Services
{
    public static class NavigationStateService
    {
        private static string _currentPageTypeName = string.Empty;
        private static readonly WeakEventManager _weakEventManager = new WeakEventManager();

        public static string CurrentPageTypeName => _currentPageTypeName;

        public static void SetCurrentPageType(Type pageType)
        {
            var name = pageType?.Name ?? string.Empty;
            if (string.Equals(_currentPageTypeName, name, StringComparison.Ordinal))
                return;
            _currentPageTypeName = name;
            try { _weakEventManager.HandleEvent(null, _currentPageTypeName, nameof(CurrentPageChanged)); } catch { }
        }

        // Expose a weak event for subscribers
        public static event EventHandler<string> CurrentPageChanged
        {
            add { _weakEventManager.AddEventHandler(value, nameof(CurrentPageChanged)); }
            remove { _weakEventManager.RemoveEventHandler(value, nameof(CurrentPageChanged)); }
        }
    }
}
