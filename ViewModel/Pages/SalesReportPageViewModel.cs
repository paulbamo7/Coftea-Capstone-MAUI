using Coftea_Capstone.C_;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Coftea_Capstone.ViewModel.Controls;
using Coftea_Capstone.Models;
using Microsoft.Maui.Networking;

namespace Coftea_Capstone.ViewModel
{
    public partial class SalesReportPageViewModel : ObservableObject
    {
        public SettingsPopUpViewModel SettingsPopup { get; set; }
        public RetryConnectionPopupViewModel RetryConnectionPopup { get; set; }

        private readonly Database _database;

        [ObservableProperty]
        private ObservableCollection<SalesReportPageModel> salesReports = new();

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string statusMessage;

        [ObservableProperty]
        private bool hasError;

        public SalesReportPageViewModel(SettingsPopUpViewModel settingsPopup)
        {
            _database = new Database(
                host: "192.168.1.4",
                database: "coftea_db",
                user: "maui",
                password: "password123"
            );
            SettingsPopup = settingsPopup;
        }

        private RetryConnectionPopupViewModel GetRetryConnectionPopup()
        {
            return ((App)Application.Current).RetryConnectionPopup;
        }

        public async Task InitializeAsync()
        {
            await LoadDataAsync();
        }

        public async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading sales reports...";
                HasError = false;

                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    HasError = true;
                    StatusMessage = "No internet connection. Please check your network.";
                    GetRetryConnectionPopup().ShowRetryPopup(LoadDataAsync, "No internet connection detected. Please check your network settings and try again.");
                    return;
                }

                // TODO: Implement GetSalesReportsAsync in Database class
                // var salesReportList = await _database.GetSalesReportsAsync();
                // SalesReports = new ObservableCollection<SalesReportPageModel>(salesReportList);

                // For now, create some dummy data
                var dummyReports = new List<SalesReportPageModel>
                {
                    new SalesReportPageModel { ReportID = 1, OrderItemID = 1, ReportDate = DateTime.Now.AddDays(-1), TotalOrder = 150 },
                    new SalesReportPageModel { ReportID = 2, OrderItemID = 2, ReportDate = DateTime.Now.AddDays(-2), TotalOrder = 200 },
                    new SalesReportPageModel { ReportID = 3, OrderItemID = 3, ReportDate = DateTime.Now.AddDays(-3), TotalOrder = 175 }
                };
                SalesReports = new ObservableCollection<SalesReportPageModel>(dummyReports);

                StatusMessage = SalesReports.Any() ? "Sales reports loaded successfully." : "No sales reports found.";
            }
            catch (Exception ex)
            {
                HasError = true;
                StatusMessage = $"Failed to load sales reports: {ex.Message}";
                GetRetryConnectionPopup().ShowRetryPopup(LoadDataAsync, $"Failed to load sales reports: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
