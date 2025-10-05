using Coftea_Capstone.C_;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Coftea_Capstone.ViewModel.Controls;
using Coftea_Capstone.Models;
using Microsoft.Maui.Networking;

namespace Coftea_Capstone.ViewModel
{
    public partial class InventoryPageViewModel : ObservableObject
    {
        public AddItemToPOSViewModel AddItemToInventoryPopup { get; } = new AddItemToPOSViewModel();
        public SettingsPopUpViewModel SettingsPopup { get; set; }
        public RetryConnectionPopupViewModel RetryConnectionPopup { get; set; }

        private readonly Database _database;

        [ObservableProperty]
        private ObservableCollection<InventoryPageModel> inventoryItems = new();

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string statusMessage;

        [ObservableProperty]
        private bool hasError;

        // Demo controls for showing inventory progress behavior
        [ObservableProperty]
        private double demoQuantity = 50;

        [ObservableProperty]
        private double demoMinimum = 100;

        public double DemoProgress
        {
            get
            {
                if (DemoMinimum <= 0) return 1.0;
                var ratio = DemoQuantity / DemoMinimum;
                if (ratio < 0) return 0;
                if (ratio > 1) return 1;
                return ratio;
            }
        }

        public string DemoStockFillColor => DemoQuantity < DemoMinimum ? "#C62828" : "#2E7D32";

        public InventoryPageViewModel(SettingsPopUpViewModel settingsPopup)
        {
            _database = new Database(); // Will use auto-detected host
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
                StatusMessage = "Loading inventory items...";
                HasError = false;

                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    HasError = true;
                    StatusMessage = "No internet connection. Please check your network.";
                    GetRetryConnectionPopup().ShowRetryPopup(LoadDataAsync, "No internet connection detected. Please check your network settings and try again.");
                    return;
                }

                var inventoryList = await _database.GetInventoryItemsAsync();
                InventoryItems = new ObservableCollection<InventoryPageModel>(inventoryList);

                StatusMessage = InventoryItems.Any() ? "Inventory items loaded successfully." : "No inventory items found.";
            }
            catch (Exception ex)
            {
                HasError = true;
                StatusMessage = $"Failed to load inventory items: {ex.Message}";
                GetRetryConnectionPopup().ShowRetryPopup(LoadDataAsync, $"Failed to load inventory items: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        partial void OnDemoQuantityChanged(double value)
        {
            OnPropertyChanged(nameof(DemoProgress));
            OnPropertyChanged(nameof(DemoStockFillColor));
        }

        partial void OnDemoMinimumChanged(double value)
        {
            OnPropertyChanged(nameof(DemoProgress));
            OnPropertyChanged(nameof(DemoStockFillColor));
        }

        [RelayCommand]
        private void AddInventoryItem(InventoryPageViewModel inventory)
        {

        }
        [RelayCommand]
        private void EditInventoryItem(InventoryPageViewModel inventory)
        {

        }
        [RelayCommand]
        private void RemoveInventoryItem(InventoryPageViewModel inventory)
        {

        }   
        
    }
}
