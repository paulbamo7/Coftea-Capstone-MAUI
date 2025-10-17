# Navigation Service Migration - Performance Fix

## Problem Summary
The app was experiencing two critical performance issues:
1. **Frame Skipping**: `[Choreographer] Skipped 58 frames! The application may be doing too much work on its main thread.`
2. **GREF Memory Leaks**: `[monodroid-gc] 46086 outstanding GREFs. Performing a full GC!`

## Root Causes

### 1. Main Thread Blocking
The old `CleanNavigationService` was doing heavy work on the main thread:
- Disposing handlers synchronously
- Calling `GC.Collect()` multiple times (extremely expensive)
- Processing entire navigation stacks on main thread
- Blocking UI during cleanup operations

### 2. GREF Leaks
- Static event in `NavigationStateService` retained page subscribers
- Pages not properly cleaned up after navigation
- Handlers not disconnected asynchronously
- Aggressive GC calls actually prevented proper cleanup

## Solution: New NavigationService

### Key Improvements

#### 1. Minimal Main-Thread Work
```csharp
// OLD - Blocked main thread
foreach (var page in pagesToRemove)
{
    page.Handler?.DisconnectHandler();  // Main thread
    if (page is IDisposable d) d.Dispose();  // Main thread
}
GC.Collect();  // VERY EXPENSIVE on main thread
GC.WaitForPendingFinalizers();
GC.Collect();

// NEW - Async cleanup in background
_ = Task.Run(async () =>
{
    await Task.Delay(100); // Let UI settle
    await MainThread.InvokeOnMainThreadAsync(() =>
    {
        // Only UI operations on main thread
        nav.Navigation.RemovePage(pageToRemove);
    });
    CleanupPageAsync(pageToRemove); // Background cleanup
});
```

#### 2. Proper Handler Cleanup
```csharp
private static void CleanupPageAsync(IView page)
{
    _ = Task.Run(async () =>
    {
        await Task.Delay(200); // Let animations complete
        
        // Disconnect handler on main thread (MAUI requirement)
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            page.Handler?.DisconnectHandler();
        });
        
        // Clear binding context
        if (page is Page p)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                p.BindingContext = null;
            });
        }
        
        // Dispose if supported
        if (page is IDisposable disposable)
        {
            disposable.Dispose();
        }
    });
}
```

#### 3. No Manual GC Calls
- Removed all `GC.Collect()` calls
- Let .NET runtime handle garbage collection naturally
- Prevents main thread blocking and improves performance

#### 4. Weak Event Pattern
```csharp
// NavigationStateService now uses WeakEventManager
private static readonly WeakEventManager _weakEventManager = new();

public static event EventHandler<string> CurrentPageChanged
{
    add { _weakEventManager.AddEventHandler(value, nameof(CurrentPageChanged)); }
    remove { _weakEventManager.RemoveEventHandler(value, nameof(CurrentPageChanged)); }
}
```

### API Changes

| Old API | New API | Notes |
|---------|---------|-------|
| `CleanNavigationService.NavigateToAsync<T>()` | `NavigationService.NavigateToAsync<T>()` | Same signature |
| `CleanNavigationService.NavigateToAsync(factory)` | `NavigationService.NavigateToAsync(factory)` | Same signature |
| `CleanNavigationService.NavigateBackAsync()` | `NavigationService.GoBackAsync()` | Renamed for clarity |
| `CleanNavigationService.NavigateToRootAsync<T>()` | `NavigationService.SetRootAsync<T>()` | Renamed for clarity |
| `CleanNavigationService.NavigateToRootAsync(factory)` | `NavigationService.SetRootAsync(factory)` | Renamed for clarity |

## Files Updated

### Core Navigation
- ✅ `Services/NavigationService.cs` - **NEW** optimized service
- ✅ `Services/NavigationStateService.cs` - Added weak event pattern
- ⚠️ `Services/CleanNavigationService.cs` - **CAN BE DELETED**

### ViewModels
- ✅ `ViewModel/Pages/LoginPageViewModel.cs`
- ✅ `ViewModel/Pages/ForgotPasswordPageViewModel.cs`
- ✅ `ViewModel/Pages/NavigationBarViewModel.cs`

### Views
- ✅ `Views/Pages/EmployeeDashboard.xaml.cs`
- ✅ `Views/Pages/TestPage.xaml.cs`

### App
- ✅ `App.xaml.cs`

## Performance Improvements

### Before
```
[Choreographer] Skipped 58 frames!
[monodroid-gc] 46086 outstanding GREFs. Performing a full GC!
```
- Main thread blocked during navigation
- Frame drops and UI stuttering
- Memory leaks causing frequent GCs
- GREF count continuously increasing

### After (Expected)
- Smooth 60 FPS navigation
- No frame skipping
- GREF count stable after initial navigation
- Proper memory cleanup without manual GC

## Testing Checklist

- [ ] Login flow (Login → Dashboard)
- [ ] Register flow (Login → Register → Back)
- [ ] Password reset flow (Login → Forgot Password → Reset → Back)
- [ ] Navigation bar switching (Dashboard ↔ POS ↔ Inventory ↔ Sales Report)
- [ ] Rapid navigation (click nav buttons quickly)
- [ ] Logout and re-login
- [ ] Monitor logcat for:
  - No "Skipped frames" warnings
  - GREF count stabilizes
  - No navigation crashes

## Best Practices Going Forward

### ✅ DO
- Use `NavigationService.NavigateToAsync()` for all page navigation
- Use `NavigationService.SetRootAsync()` for root page changes
- Let pages clean up in `OnDisappearing()` (disconnect handlers, clear item sources)
- Use weak events for static services

### ❌ DON'T
- Don't call `GC.Collect()` manually
- Don't do heavy work on main thread during navigation
- Don't use direct `Navigation.PushAsync()` or `PopAsync()`
- Don't create new `NavigationPage` instances directly
- Don't use strong static events that retain pages

## Rollback Plan

If issues occur, you can temporarily revert by:
1. Rename `NavigationService.cs` to `NavigationService.cs.bak`
2. Rename `CleanNavigationService.cs` to `NavigationService.cs`
3. Update all `NavigationService` calls back to `CleanNavigationService`

However, this will bring back the performance issues.

## Next Steps

1. **Build and test** the app thoroughly
2. **Monitor logcat** during navigation testing
3. **Delete** `Services/CleanNavigationService.cs` once confirmed working
4. **Report** any remaining performance issues with specific reproduction steps

---

**Migration Date**: 2025-10-18  
**Reason**: Fix frame skipping and GREF memory leaks  
**Status**: ✅ Complete - Ready for testing
