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
    public partial class InventoryPageViewModel : BaseViewModel
    {
        // ===================== Dependencies & Services =====================
        public AddItemToPOSViewModel AddItemToInventoryPopup { get; } = new AddItemToPOSViewModel();
        public SettingsPopUpViewModel SettingsPopup { get; set; }
        public RetryConnectionPopupViewModel RetryConnectionPopup { get; set; }

        // ===================== State & Models =====================
        private readonly Database _database;

        // Full set loaded from database, used for applying filters
        private ObservableCollection<InventoryPageModel> allInventoryItems = new();

        [ObservableProperty]
        private ObservableCollection<InventoryPageModel> inventoryItems = new();

        // Search text entered by user to filter by item name
        [ObservableProperty]
        private string searchText = string.Empty;

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
            _database = new Database(); 
            SettingsPopup = settingsPopup;
        }

        private RetryConnectionPopupViewModel GetRetryConnectionPopup() => ((App)Application.Current).RetryConnectionPopup;
        // ===================== Lifecycle =====================
        public async Task InitializeAsync()
        {
            await LoadDataAsync();
        }

        // ===================== Data Loading =====================
        public async Task LoadDataAsync()
        {
            await RunWithLoading(async () =>
            {
                StatusMessage = "Loading inventory items...";

                if (!EnsureInternetOrShowRetry(LoadDataAsync, "No internet connection detected. Please check your network settings and try again."))
                    return;

                try
                {
                var inventoryList = await _database.GetInventoryItemsAsyncCached();
                    allInventoryItems = new ObservableCollection<InventoryPageModel>(inventoryList);
                    ApplyCategoryFilter();

                    StatusMessage = InventoryItems.Any()
                        ? "Inventory items loaded successfully."
                        : "No inventory items found.";
                }
                catch (Exception ex)
                {
                    HasError = true;
                    StatusMessage = $"Failed to load inventory items: {ex.Message}";
                    GetRetryConnectionPopup().ShowRetryPopup(LoadDataAsync, $"Failed to load inventory items: {ex.Message}");
                }
            });
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

        // ===================== Filtering =====================
        // Selected category from UI buttons: "Ingredients" or "Supplies"
        [ObservableProperty]
        private string selectedCategory = "";

        // Sort functionality
        [ObservableProperty]
        private int selectedSortIndex = 0;

        [RelayCommand]
        private void FilterByCategory(string category)
        {
            SelectedCategory = category ?? string.Empty;
            ApplyCategoryFilterInternal();
        }

        private void ApplyCategoryFilterInternal()
        {
            IEnumerable<InventoryPageModel> query = allInventoryItems;

            var category = (SelectedCategory ?? string.Empty).Trim();
            if (string.Equals(category, "Supplies", StringComparison.OrdinalIgnoreCase))
            {
                // For supplies, show only Others category
                query = query.Where(i => string.Equals(i.itemCategory?.Trim(), "Others", StringComparison.OrdinalIgnoreCase));
            }
            else if (string.Equals(category, "Ingredients", StringComparison.OrdinalIgnoreCase))
            {
                // For ingredients, show specific categories
                var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "Syrups",
                    "Powdered",
                    "Fruit Series",
                    "Sinkers",
                    "Sinkers & etc.",
                    "Liquid"
                };
                query = query.Where(i => allowed.Contains((i.itemCategory ?? string.Empty).Trim()));
            }

            // Apply search by name
            var nameQuery = (SearchText ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(nameQuery))
            {
                query = query.Where(i => (i.itemName ?? string.Empty).IndexOf(nameQuery, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            // Apply sorting
            query = ApplySorting(query);

            InventoryItems = new ObservableCollection<InventoryPageModel>(query);
        }

        private IEnumerable<InventoryPageModel> ApplySorting(IEnumerable<InventoryPageModel> query)
        {
            return SelectedSortIndex switch
            {
                0 => query.OrderBy(i => i.itemName), // Name (A-Z)
                1 => query.OrderByDescending(i => i.itemName), // Name (Z-A)
                2 => query.OrderBy(i => i.itemQuantity), // Stock (Low to High)
                3 => query.OrderByDescending(i => i.itemQuantity), // Stock (High to Low)
                _ => query
            };
        }

        partial void OnSearchTextChanged(string value)
        {
            ApplyCategoryFilterInternal();
        }

        partial void OnSelectedSortIndexChanged(int value)
        {
            ApplyCategoryFilterInternal();
        }

        public void ApplyCategoryFilter()
        {
            ApplyCategoryFilterInternal();
        }
        
    }
}