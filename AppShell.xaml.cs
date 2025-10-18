using Coftea_Capstone.Views.Pages;
using Coftea_Capstone.Views.Controls;

namespace Coftea_Capstone
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            RegisterRoutes();
            ConfigureTransitions();
            SetupNavigationHandlers();
        }

        private void RegisterRoutes()
        {
            // Register all page routes
            Routing.RegisterRoute("login", typeof(LoginPage));
            Routing.RegisterRoute("register", typeof(RegisterPage));
            Routing.RegisterRoute("forgotpassword", typeof(ForgotPasswordPage));
            Routing.RegisterRoute("resetpassword", typeof(ResetPasswordPage));
            Routing.RegisterRoute("dashboard", typeof(EmployeeDashboard));
            Routing.RegisterRoute("pos", typeof(PointOfSale));
            Routing.RegisterRoute("inventory", typeof(Inventory));
            Routing.RegisterRoute("salesreport", typeof(SalesReport));
            Routing.RegisterRoute("usermanagement", typeof(UserManagement));
        }

        private void ConfigureTransitions()
        {
            // Configure smooth transitions
            Shell.SetNavBarIsVisible(this, false);
            Shell.SetTabBarIsVisible(this, false);
        }

        private void SetupNavigationHandlers()
        {
            // Show loading screen when navigation starts
            Navigated += OnNavigated;
            Navigating += OnNavigating;
        }

        private async void OnNavigating(object sender, ShellNavigatingEventArgs e)
        {
            // Show loading screen during navigation
            LoadingScreen.Show();
            await Task.Delay(100); // Small delay to ensure loading screen is visible
        }

        private async void OnNavigated(object sender, ShellNavigatedEventArgs e)
        {
            // Hide loading screen after navigation completes
            await Task.Delay(300); // Show loading screen for a bit
            LoadingScreen.Hide();
        }
    }
}
