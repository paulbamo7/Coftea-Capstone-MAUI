using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Coftea_Capstone.Services;
using Coftea_Capstone.Views.Pages;
using Microsoft.Maui.ApplicationModel;

namespace Coftea_Capstone.ViewModel.Pages
{
    public class NavigationBarViewModel : INotifyPropertyChanged
    {
        private string _currentPage;
        public event PropertyChangedEventHandler PropertyChanged;

        public ICommand GoHomeCommand { get; }
        public ICommand GoPOSCommand { get; }
        public ICommand GoInventoryCommand { get; }
        public ICommand GoSalesReportCommand { get; }
        public ICommand GoPurchaseOrderCommand { get; }
        public ICommand GoUserManagementCommand { get; }
        public ICommand GoAboutCommand { get; }

        public bool IsHomeActive => string.Equals(_currentPage, nameof(EmployeeDashboard), StringComparison.Ordinal);
        public bool IsPOSActive => string.Equals(_currentPage, nameof(PointOfSale), StringComparison.Ordinal);
        public bool IsInventoryActive => string.Equals(_currentPage, nameof(Inventory), StringComparison.Ordinal);
        public bool IsSalesReportActive => string.Equals(_currentPage, nameof(SalesReport), StringComparison.Ordinal);
        public bool IsPurchaseOrderActive => string.Equals(_currentPage, nameof(PurchaseOrderHistoryPage), StringComparison.Ordinal);
        public bool IsUserManagementActive => string.Equals(_currentPage, nameof(UserManagement), StringComparison.Ordinal);
        public bool IsAboutActive => string.Equals(_currentPage, nameof(AboutPage), StringComparison.Ordinal);

        public bool IsUserManagementVisible => App.CurrentUser?.IsAdmin ?? false;
        public bool IsPurchaseOrderVisible => App.CurrentUser?.IsAdmin ?? false; // Only admin/owner can access
        public bool IsAboutVisible => App.CurrentUser != null;

        public NavigationBarViewModel()
        {
            _currentPage = NavigationStateService.CurrentPageTypeName;
            NavigationStateService.CurrentPageChanged += OnCurrentPageChanged;
            App.CurrentUserChanged += OnCurrentUserChanged;

            GoHomeCommand = new Command(async () => 
            {
                UiOverlayService.CloseGlobalOverlays();
                await SimpleNavigationService.NavigateToAsync("//dashboard");
            });
            GoPOSCommand = new Command(async () =>
            {
                if (App.CurrentUser == null) return;
                UiOverlayService.CloseGlobalOverlays();
                await SimpleNavigationService.NavigateToAsync("//pos");
            });
            GoInventoryCommand = new Command(async () =>
            {
                if (App.CurrentUser == null) return;
                UiOverlayService.CloseGlobalOverlays();
                bool hasAccess = (App.CurrentUser?.IsAdmin ?? false) || (App.CurrentUser?.CanAccessInventory ?? false);
                if (!hasAccess)
                {
                    await Application.Current.MainPage.DisplayAlert("Unauthorized", "You don't have permission to access Inventory.", "OK");
                    return;
                }
                await SimpleNavigationService.NavigateToAsync("//inventory");
            });
            GoSalesReportCommand = new Command(async () =>
            {
                if (App.CurrentUser == null) return;
                UiOverlayService.CloseGlobalOverlays();
                bool hasAccess = (App.CurrentUser?.IsAdmin ?? false) || (App.CurrentUser?.CanAccessSalesReport ?? false);
                if (!hasAccess)
                {
                    await Application.Current.MainPage.DisplayAlert("Unauthorized", "You don't have permission to access Sales Reports.", "OK");
                    return;
                }
                await SimpleNavigationService.NavigateToAsync("//salesreport");
            });
            GoPurchaseOrderCommand = new Command(async () =>
            {
                if (App.CurrentUser == null) return;
                UiOverlayService.CloseGlobalOverlays();
                // Only admin/owner can access Purchase Order
                if (!(App.CurrentUser?.IsAdmin ?? false))
                {
                    await Application.Current.MainPage.DisplayAlert("Unauthorized", "Only administrators can access Purchase Order.", "OK");
                    return;
                }
                await SimpleNavigationService.NavigateToAsync("//purchaseorderhistory");
            });
            GoUserManagementCommand = new Command(async () =>
            {
                if (App.CurrentUser == null) return;
                UiOverlayService.CloseGlobalOverlays();
                if (!(App.CurrentUser?.IsAdmin ?? false))
                {
                    await Application.Current.MainPage.DisplayAlert("Unauthorized", "Only admins can access User Management.", "OK");
                    return;
                }
                await SimpleNavigationService.NavigateToAsync("//usermanagement");
            });

            GoAboutCommand = new Command(async () =>
            {
                UiOverlayService.CloseGlobalOverlays();
                await SimpleNavigationService.NavigateToAsync("//about");
            });

            RaiseAllActiveProps();
        }

        private void OnCurrentPageChanged(object sender, string e)
        {
            _currentPage = e;
            RaiseAllActiveProps();
        }

        private void OnCurrentUserChanged(object sender, EventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(RaiseAllActiveProps);
        }

        private void RaiseAllActiveProps()
        {
            OnPropertyChanged(nameof(IsHomeActive));
            OnPropertyChanged(nameof(IsPOSActive));
            OnPropertyChanged(nameof(IsInventoryActive));
            OnPropertyChanged(nameof(IsSalesReportActive));
            OnPropertyChanged(nameof(IsPurchaseOrderActive));
            OnPropertyChanged(nameof(IsUserManagementActive));
            OnPropertyChanged(nameof(IsAboutActive));
            OnPropertyChanged(nameof(IsUserManagementVisible));
            OnPropertyChanged(nameof(IsPurchaseOrderVisible));
            OnPropertyChanged(nameof(IsAboutVisible));
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
        {
            try { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)); } catch { }
        }
    }
}


