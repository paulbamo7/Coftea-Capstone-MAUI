using Coftea_Capstone.C_;
using Coftea_Capstone.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Coftea_Capstone.ViewModel.Controls
{
    public partial class ArchivePopupViewModel : ObservableObject
    {
        private readonly Database _database;

        [ObservableProperty]
        private bool isArchivePopupVisible = false;

        [ObservableProperty]
        private bool isProductsMode = true; // true for products, false for inventory

        [ObservableProperty]
        private ObservableCollection<POSPageModel> archivedProducts = new();

        [ObservableProperty]
        private ObservableCollection<InventoryPageModel> archivedInventoryItems = new();

        [ObservableProperty]
        private bool isLoading = false;

        [ObservableProperty]
        private string statusMessage = string.Empty;

        [ObservableProperty]
        private bool hasError = false;

        [ObservableProperty]
        private string selectedCategory = "All";

        [ObservableProperty]
        private ObservableCollection<POSPageModel> allArchivedProducts = new();

        [ObservableProperty]
        private ObservableCollection<InventoryPageModel> allArchivedInventoryItems = new();

        public NotificationPopupViewModel NotificationPopup { get; set; }

        // Admin permission check
        public bool IsAdmin => App.CurrentUser?.IsAdmin ?? false;

        public ArchivePopupViewModel()
        {
            _database = new Database();
            NotificationPopup = ((App)Application.Current).NotificationPopup;
        }

        [RelayCommand]
        private async Task LoadArchivedItemsAsync() // Load archived items based on current mode
        {
            IsLoading = true;
            StatusMessage = IsProductsMode ? "Loading archived products..." : "Loading archived inventory items...";
            HasError = false;

            try
            {
                if (IsProductsMode)
                {
                    var products = await _database.GetInactiveProductsAsync();
                    AllArchivedProducts.Clear();
                    ArchivedProducts.Clear();
                    foreach (var product in products)
                    {
                        AllArchivedProducts.Add(product);
                        ArchivedProducts.Add(product);
                    }
                    StatusMessage = $"Loaded {products.Count} archived products";
                    ApplyCategoryFilter();
                }
                else
                {
                    var items = await _database.GetInactiveInventoryItemsAsync();
                    AllArchivedInventoryItems.Clear();
                    ArchivedInventoryItems.Clear();
                    foreach (var item in items)
                    {
                        AllArchivedInventoryItems.Add(item);
                        ArchivedInventoryItems.Add(item);
                    }
                    StatusMessage = $"Loaded {items.Count} archived inventory items";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading archived items: {ex.Message}";
                HasError = true;
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task RestoreProduct(POSPageModel product) // Restore archived product
        {
            if (product == null) return;

            if (!IsAdmin)
            {
                await Application.Current.MainPage.DisplayAlert("Permission Denied",
                    "Only administrators can restore products.", "OK");
                return;
            }

            bool confirm = await Application.Current.MainPage.DisplayAlert(
                "Restore Product",
                $"Restore '{product.ProductName}'?",
                "Yes", "No");

            if (!confirm) return;

            try
            {
                int rowsAffected = await _database.RestoreProductAsync(product.ProductID);

                if (rowsAffected > 0)
                {
                    AllArchivedProducts.Remove(product);
                    ArchivedProducts.Remove(product);

                    await NotificationPopup?.AddNotification(
                        "Product Restored",
                        $"{product.ProductName} has been restored",
                        $"ID: {product.ProductID}",
                        "Success");

                    StatusMessage = $"Restored {product.ProductName}";
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Failed to restore product.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
        }

        [RelayCommand]
        private async Task RestoreInventoryItem(InventoryPageModel item) // Restore archived inventory item
        {
            if (item == null) return;

            if (!IsAdmin)
            {
                await Application.Current.MainPage.DisplayAlert("Permission Denied",
                    "Only administrators can restore inventory items.", "OK");
                return;
            }

            bool confirm = await Application.Current.MainPage.DisplayAlert(
                "Restore Inventory Item",
                $"Restore '{item.itemName}'?",
                "Yes", "No");

            if (!confirm) return;

            try
            {
                int rowsAffected = await _database.RestoreInventoryItemAsync(item.itemID);

                if (rowsAffected > 0)
                {
                    AllArchivedInventoryItems.Remove(item);
                    ArchivedInventoryItems.Remove(item);

                    await NotificationPopup?.AddNotification(
                        "Inventory Item Restored",
                        $"{item.itemName} has been restored",
                        $"ID: {item.itemID}",
                        "Success");

                    StatusMessage = $"Restored {item.itemName}";
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Failed to restore inventory item.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
        }

        [RelayCommand]
        private void FilterByCategory(string category) // Filter archived products by category
        {
            if (!IsProductsMode) return;

            SelectedCategory = category;
            ApplyCategoryFilter();
        }

        private void ApplyCategoryFilter()
        {
            if (!IsProductsMode) return;

            ArchivedProducts.Clear();

            if (SelectedCategory == "All")
            {
                foreach (var product in AllArchivedProducts)
                {
                    ArchivedProducts.Add(product);
                }
            }
            else
            {
                foreach (var product in AllArchivedProducts)
                {
                    var productCategory = product.Category ?? "";
                    var productSubcategory = product.Subcategory ?? "";

                    bool matches = false;
                    if (SelectedCategory == "All")
                    {
                        matches = true;
                    }
                    else
                    {
                        matches = productCategory.Equals(SelectedCategory, StringComparison.OrdinalIgnoreCase) ||
                                  productSubcategory.Equals(SelectedCategory, StringComparison.OrdinalIgnoreCase);
                    }

                    if (matches)
                    {
                        ArchivedProducts.Add(product);
                    }
                }
            }
        }

        [RelayCommand]
        private async Task SwitchMode(bool productsMode) // Switch between products and inventory modes
        {
            IsProductsMode = productsMode;
            SelectedCategory = "All";
            await LoadArchivedItemsAsync();
        }

        [RelayCommand]
        private void CloseArchivePopup() // Close archive popup
        {
            IsArchivePopupVisible = false;
        }

        [RelayCommand]
        private async Task OpenArchivePopup(bool productsMode) // Open archive popup in specified mode
        {
            IsProductsMode = productsMode;
            IsArchivePopupVisible = true;
            await LoadArchivedItemsAsync();
        }
    }
}

