using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using Coftea_Capstone.ViewModel.Controls;
using Coftea_Capstone.C_;
using Coftea_Capstone.Models;
using Coftea_Capstone.Views.Pages;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Maui.Controls;

namespace Coftea_Capstone.ViewModel.Controls
{
    public partial class ConnectPOSItemToInventoryViewModel : ObservableObject
    {
        private readonly Database _database = new Database(host: "0.0.0.0", database: "coftea_db", user: "root", password: "");

        // Event to trigger AddProduct in parent VM
        public event Action ConfirmPreviewRequested;
        [ObservableProperty] private bool isConnectPOSToInventoryVisible;
        [ObservableProperty] private bool isPreviewVisible;

        public ObservableCollection<Ingredient> Ingredients { get; set; } = new();
        public ObservableCollection<InventoryPageModel> InventoryItems { get; set; } = new();
        public ObservableCollection<InventoryPageModel> AllInventoryItems { get; set; } = new();

        [ObservableProperty] private string selectedFilter = "All";
        [ObservableProperty] private string searchText = string.Empty;
        [ObservableProperty] private string selectedSort = "Name (A-Z)";

        // Preview-bound properties (populated from parent VM)
        [ObservableProperty] private ImageSource selectedImageSource;
        [ObservableProperty] private string productName;
        [ObservableProperty] private string selectedCategory;
        [ObservableProperty] private decimal smallPrice;
        [ObservableProperty] private decimal largePrice;
        [ObservableProperty] private string productDescription;

        // Event to notify AddItem popup
        public event Action ReturnRequested;

        [RelayCommand]
        private void ReturnToAddItemToPOS()
        {
            IsConnectPOSToInventoryVisible = false;
            ReturnRequested?.Invoke();
        }

        [RelayCommand]
        private void CloseConnectPOSToInventory()
        {
            IsConnectPOSToInventoryVisible = false;
        }
        [RelayCommand]
        private void ShowPreview()
        {
            IsPreviewVisible = true;
        }

        [RelayCommand]
        private void ClosePreview()
        {
            IsPreviewVisible = false;
        }

        [RelayCommand]
        private void ConfirmPreview()
        {
            IsConnectPOSToInventoryVisible = false; // close overlay
            IsPreviewVisible = false;
            ConfirmPreviewRequested?.Invoke();
        }

        [RelayCommand]
        private async Task OpenInventorySelection()
        {
            IsConnectPOSToInventoryVisible = false;
            // Navigate to Inventory page to allow selection there
            await Application.Current.MainPage.Navigation.PushAsync(new Inventory());
        }

        [RelayCommand]
        public async Task LoadInventoryAsync()
        {
            var list = await _database.GetInventoryItemsAsync();
            AllInventoryItems.Clear();
            InventoryItems.Clear();
            foreach (var it in list)
            {
                it.IsSelected = Ingredients.Any(g => g.Name == it.itemName);
                AllInventoryItems.Add(it);
            }
            ApplyFilters();
        }

        [RelayCommand]
        private void ToggleSelect(InventoryPageModel item)
        {
            if (item == null) return;
            var existing = Ingredients.FirstOrDefault(i => i.Name == item.itemName);
            if (existing != null)
            {
                Ingredients.Remove(existing);
                item.IsSelected = false;
                return;
            }
            Ingredients.Add(new Ingredient { Name = item.itemName, Amount = 1, Selected = true });
            item.IsSelected = true;
        }

        private void ApplyFilters()
        {
            var query = AllInventoryItems.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SelectedFilter) && SelectedFilter != "All")
            {
                query = query.Where(i => string.Equals(i.itemCategory, SelectedFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var term = SearchText.Trim();
                query = query.Where(i => (i.itemName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                                       || (i.itemCategory?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            query = SelectedSort switch
            {
                "Name (Z-A)" => query.OrderByDescending(i => i.itemName),
                "Quantity (Low to High)" => query.OrderBy(i => i.itemQuantity),
                "Quantity (High to Low)" => query.OrderByDescending(i => i.itemQuantity),
                _ => query.OrderBy(i => i.itemName)
            };

            InventoryItems.Clear();
            foreach (var it in query) InventoryItems.Add(it);
        }

        [RelayCommand]
        private void FilterByCategory(string category)
        {
            SelectedFilter = category;
            ApplyFilters();
        }

        partial void OnSearchTextChanged(string value)
        {
            ApplyFilters();
        }

        partial void OnSelectedSortChanged(string value)
        {
            ApplyFilters();
        }

        [RelayCommand]
        private void SelectAllVisible()
        {
            foreach (var item in InventoryItems)
            {
                if (!item.IsSelected)
                {
                    Ingredients.Add(new Ingredient { Name = item.itemName, Amount = 1, Selected = true });
                    item.IsSelected = true;
                }
            }
        }
        public partial class Ingredient : ObservableObject
        {
            [ObservableProperty] private string name;
            [ObservableProperty] private double amount;
            [ObservableProperty] private string unit;
            [ObservableProperty] private bool selected;
        }
    }
}
