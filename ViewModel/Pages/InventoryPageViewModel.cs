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
using Coftea_Capstone.Services;

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
        private bool _hasLoadedData = false; // Track if data has been loaded

        // Full set loaded from database, used for applying filters
        private ObservableCollection<InventoryPageModel> allInventoryItems = new();

        [ObservableProperty]
        private ObservableCollection<InventoryPageModel> inventoryItems = new();

        // Search text entered by user to filter by item name
        [ObservableProperty]
        private string searchText = string.Empty;


        public InventoryPageViewModel(SettingsPopUpViewModel settingsPopup)
        {
            _database = new Database(); 
            SettingsPopup = settingsPopup;
        }

        private RetryConnectionPopupViewModel GetRetryConnectionPopup() => ((App)Application.Current).RetryConnectionPopup;

        public async Task InitializeAsync() // Call this when the page appears
        {
            System.Diagnostics.Debug.WriteLine("🔄 InventoryPageViewModel.InitializeAsync called");
            await LoadDataAsync();
        }

        public async Task LoadDataAsync() // Load inventory data with loading indicator
        {
            // Only show loading if data hasn't been loaded before
            if (!_hasLoadedData)
            {
                await RunWithLoading(async () =>
                {
                    await LoadDataInternal();
                });
            }
            else
            {
                // Data already loaded, just refresh without showing loading
                await LoadDataInternal();
            }
        }

        private async Task LoadDataInternal() // Actual data loading logic
        {
            StatusMessage = "Loading inventory items...";

            if (!EnsureInternetOrShowRetry(LoadDataAsync, "No internet connection detected. Please check your network settings and try again."))
                return;

            try
            {
                var inventoryList = await _database.GetInventoryItemsAsyncCached();
                System.Diagnostics.Debug.WriteLine($"🔍 Loaded {inventoryList?.Count ?? 0} inventory items from database");
                
                if (inventoryList != null && inventoryList.Any())
                {
                    foreach (var item in inventoryList.Take(3)) // Log first 3 items for debugging
                    {
                        System.Diagnostics.Debug.WriteLine($"📦 Item: {item.itemName} | Category: {item.itemCategory} | Quantity: {item.itemQuantity}");
                    }
                }
                
                allInventoryItems = new ObservableCollection<InventoryPageModel>(inventoryList);
                ApplyCategoryFilter();

                StatusMessage = InventoryItems.Any()
                    ? "Inventory items loaded successfully."
                    : "No inventory items found.";

                System.Diagnostics.Debug.WriteLine($"📊 After filtering: {InventoryItems.Count} items displayed");
                
                // Mark data as loaded
                _hasLoadedData = true;
            }
            catch (Exception ex)
            {
                HasError = true;
                StatusMessage = $"Failed to load inventory items: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"❌ Error loading inventory: {ex.Message}");
                GetRetryConnectionPopup().ShowRetryPopup(LoadDataAsync, $"Failed to load inventory items: {ex.Message}");
            }
        }

        public async Task ForceReloadDataAsync() // Force reload data from database 
        {
            _hasLoadedData = false; // Reset the flag to show loading
            await LoadDataAsync();
        }

        // Selected category from UI buttons: "Ingredients" or "Supplies"
        [ObservableProperty]
        private string selectedCategory = "";

        // Sort functionality
        [ObservableProperty]
        private int selectedSortIndex = 0;

        [RelayCommand]
        private void FilterByCategory(string category) // Filter inventory by category
        {
            SelectedCategory = category ?? string.Empty;
            ApplyCategoryFilterInternal();
        }

        private void ApplyCategoryFilterInternal() // Internal method to apply category filter, search, and sorting
        {
            IEnumerable<InventoryPageModel> query = allInventoryItems;
            System.Diagnostics.Debug.WriteLine($"🔍 Starting filter with {allInventoryItems.Count} total items");

            var category = (SelectedCategory ?? string.Empty).Trim();
            System.Diagnostics.Debug.WriteLine($"🏷️ Selected category: '{category}'");
            
            if (string.Equals(category, "All", StringComparison.OrdinalIgnoreCase))
            {
                // Show all items - no filtering
                System.Diagnostics.Debug.WriteLine($"📋 Showing all items: {query.Count()} items");
            }
            else if (string.Equals(category, "Supplies", StringComparison.OrdinalIgnoreCase))
            {
                // For supplies, show only Others category
                query = query.Where(i => string.Equals(i.itemCategory?.Trim(), "Others", StringComparison.OrdinalIgnoreCase));
                System.Diagnostics.Debug.WriteLine($"📦 Filtered for Supplies (Others): {query.Count()} items");
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
                System.Diagnostics.Debug.WriteLine($"🥤 Filtered for Ingredients: {query.Count()} items");
            }
            else
            {
                // Show all items when no specific category is selected
                System.Diagnostics.Debug.WriteLine($"📋 Showing all items: {query.Count()} items");
            }

            // Apply search by name
            var nameQuery = (SearchText ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(nameQuery))
            {
                query = query.Where(i => (i.itemName ?? string.Empty).IndexOf(nameQuery, StringComparison.OrdinalIgnoreCase) >= 0);
                System.Diagnostics.Debug.WriteLine($"🔍 After search '{nameQuery}': {query.Count()} items");
            }

            // Apply sorting
            query = ApplySorting(query);

            InventoryItems = new ObservableCollection<InventoryPageModel>(query);
            System.Diagnostics.Debug.WriteLine($"✅ Final result: {InventoryItems.Count} items in InventoryItems collection");
        }

        private IEnumerable<InventoryPageModel> ApplySorting(IEnumerable<InventoryPageModel> query) // Apply sorting based on selected index
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

        partial void OnSearchTextChanged(string value) // Apply search filter
        {
            ApplyCategoryFilterInternal();
        }

        partial void OnSelectedSortIndexChanged(int value) // Apply sorting
        {
            ApplyCategoryFilterInternal();
        }

        public void ApplyCategoryFilter() // Public method to apply category filter
        {
            ApplyCategoryFilterInternal();
        }

        [RelayCommand]
        private async Task CreatePurchaseOrderAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🛒 Creating purchase order...");
                
                // Get items that are below minimum stock levels
                var lowStockItems = allInventoryItems.Where(item => item.itemQuantity <= item.minimumQuantity).ToList();
                
                if (!lowStockItems.Any())
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "No Purchase Order Needed", 
                        "All inventory items are above minimum stock levels.", 
                        "OK");
                    return;
                }
                
                // Show confirmation dialog with SMS details
                var result = await Application.Current.MainPage.DisplayAlert(
                    "Create Purchase Order", 
                    $"Found {lowStockItems.Count} items below minimum stock levels.\n\nThis will:\n• Create a purchase order\n• Send SMS to Coftea Supplier\n• Notify admin for approval\n\nProceed?", 
                    "Yes", "Cancel");
                
                if (result)
                {
                    // Create purchase order in database
                    var purchaseOrderId = await _database.CreatePurchaseOrderAsync(lowStockItems);
                    
                    if (purchaseOrderId > 0)
                    {
                        // Send SMS to supplier
                        var smsSent = await PurchaseOrderSMSService.SendPurchaseOrderToSupplierAsync(purchaseOrderId, lowStockItems);
                        
                        // Notify admin via SMS
                        var currentUser = App.CurrentUser?.Email ?? "Unknown";
                        var adminNotified = await PurchaseOrderSMSService.NotifyAdminOfPurchaseOrderAsync(purchaseOrderId, currentUser);
                        
                        if (smsSent && adminNotified)
                        {
                            await Application.Current.MainPage.DisplayAlert(
                                "Purchase Order Created", 
                                $"Purchase order #{purchaseOrderId} has been created and sent to Coftea Supplier via SMS. Admin has been notified for approval.", 
                                "OK");
                        }
                        else
                        {
                            await Application.Current.MainPage.DisplayAlert(
                                "Purchase Order Created (Partial)", 
                                $"Purchase order #{purchaseOrderId} created but SMS notifications may have failed. Please check manually.", 
                                "OK");
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"✅ Purchase order {purchaseOrderId} created and SMS sent");
                    }
                    else
                    {
                        await Application.Current.MainPage.DisplayAlert(
                            "Error", 
                            "Failed to create purchase order. Please try again.", 
                            "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error creating purchase order: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert(
                    "Error", 
                    $"Failed to create purchase order: {ex.Message}", 
                    "OK");
            }
        }
        
    }
}