using Coftea_Capstone.Views.Pages;
using Microsoft.Maui.Controls;

namespace Coftea_Capstone
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            RegisterRoutes();
            ConfigureTransitions();
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
    }
}
