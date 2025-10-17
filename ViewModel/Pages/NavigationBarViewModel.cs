using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Coftea_Capstone.Services;
using Coftea_Capstone.Views.Pages;

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
        public ICommand ShowSettingsCommand { get; }

        public bool IsHomeActive => string.Equals(_currentPage, nameof(EmployeeDashboard), StringComparison.Ordinal);
        public bool IsPOSActive => string.Equals(_currentPage, nameof(PointOfSale), StringComparison.Ordinal);
        public bool IsInventoryActive => string.Equals(_currentPage, nameof(Inventory), StringComparison.Ordinal);
        public bool IsSalesReportActive => string.Equals(_currentPage, nameof(SalesReport), StringComparison.Ordinal);

        public NavigationBarViewModel()
        {
            _currentPage = NavigationStateService.CurrentPageTypeName;
            NavigationStateService.CurrentPageChanged += OnCurrentPageChanged;

            GoHomeCommand = new Command(async () => 
            {
                UiOverlayService.CloseGlobalOverlays();
                await NavigationService.NavigateToAsync<EmployeeDashboard>();
            });
            GoPOSCommand = new Command(async () =>
            {
                if (App.CurrentUser == null) return;
                UiOverlayService.CloseGlobalOverlays();
                await NavigationService.NavigateToAsync(() => new PointOfSale());
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
                await NavigationService.NavigateToAsync(() => new Inventory());
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
                await NavigationService.NavigateToAsync(() => new SalesReport());
            });
            ShowSettingsCommand = new Command(() => GlobalSettingsService.ShowSettings());
        }

        private void OnCurrentPageChanged(object sender, string e)
        {
            _currentPage = e;
            RaiseAllActiveProps();
        }

        private void RaiseAllActiveProps()
        {
            OnPropertyChanged(nameof(IsHomeActive));
            OnPropertyChanged(nameof(IsPOSActive));
            OnPropertyChanged(nameof(IsInventoryActive));
            OnPropertyChanged(nameof(IsSalesReportActive));
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
        {
            try { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)); } catch { }
        }
    }
}


