using Coftea_Capstone.C_;
using Coftea_Capstone.Models;
using Coftea_Capstone.Services;
using Coftea_Capstone.Views.Pages;
using Coftea_Capstone.ViewModel;
using Coftea_Capstone.ViewModel.Controls;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Threading;
using Microsoft.Maui.Networking;

namespace Coftea_Capstone
{
    public partial class App : Application
    {

        public static UserInfoModel CurrentUser { get; private set; }
        public static string ResetPasswordEmail { get; set; }

        // Online-only mode

        // Shared ViewModels
        public AddItemToPOSViewModel AddItemPopup { get; private set; }
        public SettingsPopUpViewModel SettingsPopup { get; private set; }
        public POSPageViewModel POSVM { get; private set; }
        public ManagePOSOptionsViewModel ManagePOSPopup { get; private set; }
        public ManageInventoryOptionsViewModel ManageInventoryPopup { get; private set; }
        public EditInventoryPopupViewModel EditInventoryPopup { get; private set; }
        public AddItemToInventoryViewModel AddItemToInventoryPopup { get; private set; }
        public RetryConnectionPopupViewModel RetryConnectionPopup { get; private set; }
        public NotificationPopupViewModel NotificationPopup { get; private set; }
        public PasswordResetPopupViewModel PasswordResetPopup { get; private set; }
        public PaymentPopupViewModel PaymentPopup { get; private set; }
        public OrderCompletePopupViewModel OrderCompletePopup { get; private set; }
        public OrderConfirmedPopupViewModel OrderConfirmedPopup { get; private set; }
        public SuccessCardPopupViewModel SuccessCardPopup { get; private set; }
        public HistoryPopupViewModel HistoryPopup { get; private set; }
        public ProfilePopupViewModel ProfilePopup { get; private set; }
        public ActivityLogPopupViewModel ActivityLogPopup { get; private set; }
        public PurchaseOrderApprovalPopupViewModel PurchaseOrderApprovalPopup { get; private set; }
        public CreatePurchaseOrderPopupViewModel CreatePurchaseOrderPopup { get; private set; }
        
        // Shared Page ViewModels to prevent memory leaks
        public InventoryPageViewModel InventoryVM { get; private set; }
        public SalesReportPageViewModel SalesReportVM { get; private set; }


        // Shared transactions store for History
        public ObservableCollection<TransactionHistoryModel> Transactions { get; private set; }

        // Global loading overlay instance
        public static Views.Controls.LoadingOverlay LoadingOverlay { get; private set; }
             
        public App()
        {
            InitializeComponent();

            // Determine start route BEFORE showing Shell to avoid login flash
            bool isLoggedIn = Preferences.Get("IsLoggedIn", false);
            bool rememberMe = Preferences.Get("RememberMe", false);
            bool isAdmin = Preferences.Get("IsAdmin", false);
            
            // Initialize user if remembered
            if (isLoggedIn && rememberMe && CurrentUser == null)
            {
                var user = new UserInfoModel
                {
                    ID = Preferences.Get("UserID", 0),
                    IsAdmin = isAdmin,
                    Email = Preferences.Get("Email", string.Empty)
                };

                if (isAdmin)
                {
                    user.CanAccessInventory = true;
                    user.CanAccessSalesReport = true;
                }
                SetCurrentUser(user);
            }

            // Initialize view models
            InitializeViewModels();

            // Set initial page based on login status
            if (isLoggedIn && rememberMe)
            {
                // Start with dashboard as the main page
                MainPage = new AppShell();
                
                // Load profile in background
                _ = Task.Run(async () =>
                {
                    try 
                    { 
                        await ProfilePopup.LoadUserProfile(); 
                    } 
                    catch { }
                });
                
                // Set initial route to dashboard
                Shell.Current.GoToAsync("//dashboard").ContinueWith(t =>
                {
                    // This ensures the navigation completes before the UI is shown
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
            else
            {
                // Start with login page
                MainPage = new AppShell();
                Shell.Current.GoToAsync("//login").ContinueWith(t =>
                {
                    // This ensures the navigation completes before the UI is shown
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }

            // Create loading overlay and add it to the visual tree after MainPage is set
            MainThread.BeginInvokeOnMainThread(() =>
            {
                LoadingOverlay = new Views.Controls.LoadingOverlay();
                SimpleNavigationService.InitializeLoadingOverlay(LoadingOverlay);
                
                // Add loading overlay to the page's visual tree
                if (MainPage is Shell shell && shell.CurrentPage != null)
                {
                    // We'll add it dynamically when needed
                }
            });

            // Online-only initialization

            // Ensure database exists and tables are created, then adjust theme colors to match Login page
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    // Initialize image persistence service
                    await Services.ImagePersistenceService.MigrateOldImagesAsync();
                    
                    try
                    {
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15)); // 15 second timeout
                        var db = new Database();
                        await db.EnsureServerAndDatabaseAsync(cts.Token);
                        await db.InitializeDatabaseAsync(cts.Token);
                        await db.CheckAllMinimumStockLevelsAsync();
                    }
                    catch (Exception dbEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[App] ⚠️ Online database unavailable: {dbEx.Message}");
                    }
                }
                catch (OperationCanceledException)
                {
                    System.Diagnostics.Debug.WriteLine("[App] ⏰ Database initialization timeout");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[App] ❌ Initialization error: {ex.Message}");
                }

                // Align app accent colors to Login page palette
                if (Current?.Resources != null)
                {
                    if (Current.Resources.ContainsKey("Primary")) Current.Resources["Primary"] = Color.FromArgb("#5B4F45");
                    if (Current.Resources.ContainsKey("Tertiary")) Current.Resources["Tertiary"] = Color.FromArgb("#5B4F45");
                    if (Current.Resources.ContainsKey("Secondary")) Current.Resources["Secondary"] = Color.FromArgb("#C1A892");
                    
                    // Set global background color for navigation transitions
                    if (Current.Resources.ContainsKey("BackgroundColor")) Current.Resources["BackgroundColor"] = Color.FromArgb("#C1A892");
                }
                
                // Set the main page background color
                if (MainPage != null)
                {
                    MainPage.BackgroundColor = Color.FromArgb("#C1A892");
                }
            });
            // Removed delayed auto-login block to eliminate login flash

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            // Monitor connectivity changes and auto-sync when online
            Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;

            // Persist cart when app goes to background (platform lifecycle events handled elsewhere)
        }

        private async void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
        {
            if (e.NetworkAccess == NetworkAccess.Internet)
            {
                System.Diagnostics.Debug.WriteLine("🌐 Internet connection restored - syncing pending operations...");
                try
                {
                    var offlineQueue = new Services.OfflineQueueService();
                    var syncedCount = await offlineQueue.SyncPendingOperationsAsync();
                    if (syncedCount > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"✅ Synced {syncedCount} pending operations");
                        // Show notification if there осталось pending operations
                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            try
                            {
                                if (MainPage != null && NotificationPopup != null)
                                {
                                    NotificationPopup.ShowNotification(
                                        $"Synced {syncedCount} pending operation(s) to database.",
                                        "Sync Complete");
                                }
                            }
                            catch { }
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error syncing on connection restore: {ex.Message}");
                }
            }
        }

        private void SetRootPage(Page page)
        {
            try { MainPage?.Handler?.DisconnectHandler(); } catch { }
            MainPage = new AppShell();
            try { NavigationStateService.SetCurrentPageType(page.GetType()); } catch { }
        }

        private void InitializeViewModels()
        {
            // Initialize shared popups first so dependent VMs can reference them safely
            NotificationPopup = new NotificationPopupViewModel();

            AddItemPopup = new AddItemToPOSViewModel();
            var editProductPopup = new EditProductPopupViewModel(AddItemPopup);
            ManagePOSPopup = new ManagePOSOptionsViewModel(AddItemPopup, editProductPopup);
            AddItemToInventoryPopup = new AddItemToInventoryViewModel();
            EditInventoryPopup = new EditInventoryPopupViewModel(AddItemToInventoryPopup);
            ManageInventoryPopup = new ManageInventoryOptionsViewModel(AddItemToInventoryPopup, EditInventoryPopup);
            SettingsPopup = new SettingsPopUpViewModel(AddItemPopup, ManagePOSPopup, ManageInventoryPopup);
            POSVM = new POSPageViewModel(AddItemPopup, SettingsPopup);
            RetryConnectionPopup = new RetryConnectionPopupViewModel();
            PasswordResetPopup = new PasswordResetPopupViewModel();
            PaymentPopup = new PaymentPopupViewModel();
            OrderCompletePopup = new OrderCompletePopupViewModel();
            OrderConfirmedPopup = new OrderConfirmedPopupViewModel();
            SuccessCardPopup = new SuccessCardPopupViewModel();
            HistoryPopup = new HistoryPopupViewModel();
            ProfilePopup = new ProfilePopupViewModel();
            ActivityLogPopup = new ActivityLogPopupViewModel();
            PurchaseOrderApprovalPopup = new PurchaseOrderApprovalPopupViewModel();
            CreatePurchaseOrderPopup = new CreatePurchaseOrderPopupViewModel();
            
            // Initialize shared page ViewModels
            InventoryVM = new InventoryPageViewModel(SettingsPopup);
            SalesReportVM = new SalesReportPageViewModel(SettingsPopup);

            // Initialize shared transactions store
            Transactions = new ObservableCollection<TransactionHistoryModel>();
            
            // Disable automatic IP detection temporarily to prevent startup errors
            // Users can enable it manually through settings if needed
            NetworkConfigurationService.DisableAutomaticIPDetection();
            System.Diagnostics.Debug.WriteLine("🚫 Automatic IP detection disabled to prevent startup errors");
            
            // Validate product-ingredient connections in background to fix any inconsistencies
            _ = Task.Run(async () =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("🔧 Starting background validation of product-ingredient connections...");
                    var database = new Models.Database();
                    await database.ValidateAllProductIngredientsAsync();
                    System.Diagnostics.Debug.WriteLine("✅ Background product-ingredient validation completed");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error during background product validation: {ex.Message}");
                }
            });
        }

        private async void NavigateToDashboard(bool isAdmin)
        {
            // Route all users to EmployeeDashboard; frames are data-bound
            await SimpleNavigationService.NavigateToAsync("//dashboard");
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
                HandleException(ex);
        }

        private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"🚨 Unobserved Task Exception: {e.Exception}");
            System.Diagnostics.Debug.WriteLine($"🚨 Stack trace: {e.Exception.StackTrace}");
            
            // Handle the exception gracefully
            HandleException(e.Exception);
            e.SetObserved(); // Mark as handled to prevent app crash
        }

        private void HandleException(Exception ex)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🚨 Handling exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"🚨 Exception type: {ex.GetType().Name}");
                
                // With offline-first system, database errors are handled gracefully
                // Only show connection errors for features that REQUIRE internet (like email)
                string message = ex.Message.Contains("email") || ex.Message.Contains("password reset")
                    ? "This feature requires an internet connection. Please check your network."
                    : $"An error occurred: {ex.Message}";

                // Only show alert if we have a valid MainPage
                if (MainPage != null)
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        try
                        {
                            await MainPage.DisplayAlert("Error", message, "OK");
                        }
                        catch (Exception alertEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"❌ Failed to show error alert: {alertEx.Message}");
                        }
                    });
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ MainPage is null, cannot show error alert");
                }
            }
            catch (Exception handleEx)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error in HandleException: {handleEx.Message}");
            }
        }

        public static void SetCurrentUser(UserInfoModel user)
        {
            CurrentUser = user;
            // Also set the UserSession for admin checks
            if (user != null)
            {
                UserSession.Instance.SetUser(user.Email, user.IsAdmin);
            }
            else
            {
                UserSession.Instance.Clear();
            }
        }

        // Called after logout to reset everything
        public async Task ResetAppAfterLogout()
        {
            SetCurrentUser(null);
            Preferences.Set("IsLoggedIn", false);
            Preferences.Set("IsAdmin", false);
            Preferences.Remove("UserID");
            Preferences.Remove("Email");
            Preferences.Remove("Password");
            Preferences.Remove("RememberMe");

            // Dispose existing ViewModels before recreating
            DisposeViewModels();
            InitializeViewModels(); // reset all viewmodels

            // Navigate to login page
            await SimpleNavigationService.NavigateToAsync("//login");
        }

        private void DisposeViewModels()
        {
            try
            {
                // Dispose ViewModels that implement IDisposable
                (AddItemPopup as IDisposable)?.Dispose();
                (SettingsPopup as IDisposable)?.Dispose();
                (POSVM as IDisposable)?.Dispose();
                (ManagePOSPopup as IDisposable)?.Dispose();
                (ManageInventoryPopup as IDisposable)?.Dispose();
                (EditInventoryPopup as IDisposable)?.Dispose();
                (AddItemToInventoryPopup as IDisposable)?.Dispose();
                (RetryConnectionPopup as IDisposable)?.Dispose();
                (NotificationPopup as IDisposable)?.Dispose();
                (PasswordResetPopup as IDisposable)?.Dispose();
                (PaymentPopup as IDisposable)?.Dispose();
                (OrderCompletePopup as IDisposable)?.Dispose();
                (OrderConfirmedPopup as IDisposable)?.Dispose();
                (SuccessCardPopup as IDisposable)?.Dispose();
                (HistoryPopup as IDisposable)?.Dispose();
                (ProfilePopup as IDisposable)?.Dispose();

                // Clear collections to help GC
                Transactions?.Clear();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disposing ViewModels: {ex.Message}");
            }
        }

    }
}
