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

namespace Coftea_Capstone
{
    public partial class App : Application
    {

        public static UserInfoModel CurrentUser { get; private set; }

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
        
        // Shared Page ViewModels to prevent memory leaks
        public InventoryPageViewModel InventoryVM { get; private set; }
        public SalesReportPageViewModel SalesReportVM { get; private set; }


        // Shared transactions store for History
        public ObservableCollection<TransactionHistoryModel> Transactions { get; private set; }

        public App()
        {
            InitializeComponent();

            InitializeViewModels();

            // Set AppShell as main page
            MainPage = new AppShell();

            // Ensure database exists and tables are created, then adjust theme colors to match Login page
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15)); // 15 second timeout
                    var db = new Database();
                    await db.EnsureServerAndDatabaseAsync(cts.Token);
                    await db.InitializeDatabaseAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    System.Diagnostics.Debug.WriteLine("⏰ Database initialization timeout");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Database initialization error: {ex.Message}");
                    // Swallow init errors here; UI will display connection issues elsewhere
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

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                bool isLoggedIn = Preferences.Get("IsLoggedIn", false);
                bool rememberMe = Preferences.Get("RememberMe", false);
                bool isAdmin = Preferences.Get("IsAdmin", false);

                // Only auto-login if both IsLoggedIn and RememberMe are true
                if (isLoggedIn && rememberMe)
                {
                    // Hydrate a minimal CurrentUser so navigation works after auto-login
                    if (CurrentUser == null)
                    {
                        var user = new UserInfoModel
                        {
                            IsAdmin = isAdmin,
                            Email = Preferences.Get("Email", string.Empty)
                        };
                        
                        // Ensure admin users always have full access
                        if (isAdmin)
                        {
                            user.CanAccessInventory = true;
                            user.CanAccessSalesReport = true;
                        }
                        
                        SetCurrentUser(user);
                    }
                    NavigateToDashboard(isAdmin);
                }
                else
                {
                    // Clear login state if Remember Me is not checked
                    if (isLoggedIn && !rememberMe)
                    {
                        Preferences.Set("IsLoggedIn", false);
                        Preferences.Set("IsAdmin", false);
                    }
                    await SimpleNavigationService.NavigateToAsync("//login");
                }
            });

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            // Persist cart when app goes to background (platform lifecycle events handled elsewhere)
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
            
            // Initialize shared page ViewModels
            InventoryVM = new InventoryPageViewModel(SettingsPopup);
            SalesReportVM = new SalesReportPageViewModel(SettingsPopup);

            // Initialize shared transactions store
            Transactions = new ObservableCollection<TransactionHistoryModel>();
            
            // Manual IP address configuration
            // Set your PC's IP address here for database connection
            NetworkConfigurationService.SetManualDatabaseHost("192.168.1.7");
            
            // Simple database connection test without automatic IP detection
            _ = Task.Run(async () => {
                try
                {
                    var db = new Database();
                    await db.EnsureServerAndDatabaseAsync();
                    System.Diagnostics.Debug.WriteLine($"✅ Database connection successful");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ App Startup - Connection Error: {ex.Message}");
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
                
                string message = !NetworkService.HasInternetConnection()
                    ? "No internet connection. Please check your network."
                    : $"Unexpected error: {ex.Message}";

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
