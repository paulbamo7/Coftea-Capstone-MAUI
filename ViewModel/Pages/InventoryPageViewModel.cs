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
using Coftea_Capstone.Services;
using Microsoft.Maui.Controls;

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
            
            // Subscribe to inventory change notifications
            MessagingCenter.Subscribe<AddItemToInventoryViewModel>(this, "InventoryChanged", async (sender) =>
            {
                System.Diagnostics.Debug.WriteLine("🔄 Received InventoryChanged message, refreshing inventory...");
                await ForceReloadDataAsync();
            });
            
            MessagingCenter.Subscribe<EditInventoryPopupViewModel>(this, "InventoryChanged", async (sender) =>
            {
                System.Diagnostics.Debug.WriteLine("🔄 Received InventoryChanged message from EditInventory, refreshing inventory...");
                await ForceReloadDataAsync();
            });
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
                // Invalidate cache first to ensure we get fresh data
                _database.InvalidateInventoryCache();
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
            System.Diagnostics.Debug.WriteLine("🔄 ForceReloadDataAsync called - invalidating cache and reloading...");
            _database.InvalidateInventoryCache(); // Invalidate cache to ensure fresh data
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
                // For supplies, include both Supplies and Others categories
                query = query.Where(i =>
                    string.Equals(i.itemCategory?.Trim(), "Supplies", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(i.itemCategory?.Trim(), "Others", StringComparison.OrdinalIgnoreCase));
                System.Diagnostics.Debug.WriteLine($"📦 Filtered for Supplies category: {query.Count()} items");
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
            foreach (var item in InventoryItems)
            {
                item.IsExpanded = false;
            }
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
        private void ToggleInventoryItem(InventoryPageModel item)
        {
            if (item == null) return;

            var wasExpanded = item.IsExpanded;

            foreach (var inv in InventoryItems)
            {
                if (inv != null && inv != item)
                {
                    inv.IsExpanded = false;
                }
            }

            item.IsExpanded = !wasExpanded;
        }

        [RelayCommand]
        private async Task EditInventoryItemAsync(InventoryPageModel item)
        {
            if (item == null) return;

            try
            {
                var app = (App)Application.Current;
                var editPopup = app?.EditInventoryPopup;
                if (editPopup?.EditItemCommand != null)
                {
                    await editPopup.EditItemCommand.ExecuteAsync(item);
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Inventory editor is not available right now.", "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error opening inventory editor: {ex.Message}");
                try
                {
                    await Application.Current.MainPage.DisplayAlert("Error", $"Failed to open inventory editor: {ex.Message}", "OK");
                }
                catch { }
            }
        }
        
        [RelayCommand]
        private async Task CreatePurchaseOrderAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🛒 Creating purchase order...");
                
                // Get items that are below minimum stock levels (if any)
                var lowStockItems = allInventoryItems.Where(item => item.itemQuantity <= item.minimumQuantity).ToList();
                
                // Always show the popup, even if there are no low stock items
                // This allows users to create purchase orders in advance
                var app = (App)Application.Current;
                if (app?.CreatePurchaseOrderPopup != null)
                {
                    var currentUserIsAdmin = App.CurrentUser?.IsAdmin ?? false;
                    await app.CreatePurchaseOrderPopup.LoadItems(lowStockItems, currentUserIsAdmin);
                    app.CreatePurchaseOrderPopup.IsVisible = true;
                }
                else
                {
                    // Fallback to old method if popup not available
                    await Application.Current.MainPage.DisplayAlert(
                        "Error", 
                        "Purchase order popup is not initialized.", 
                        "OK");
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

        [RelayCommand]
        private void ManageInventory()
        {
            System.Diagnostics.Debug.WriteLine("ManageInventory called");
            
            // Check if user is admin OR has been granted inventory access
            var currentUser = App.CurrentUser;
            bool hasAccess = (currentUser?.IsAdmin ?? false) || (currentUser?.CanAccessInventory ?? false);
            
            if (!hasAccess)
            {
                Application.Current?.MainPage?.DisplayAlert("Access Denied", 
                    "You don't have permission to manage inventory. Please contact an administrator.", "OK");
                return;
            }
            
            // Get the ManageInventoryOptionsViewModel from the app
            var manageInventoryPopup = ((App)Application.Current).ManageInventoryPopup;
            if (manageInventoryPopup != null)
            {
                System.Diagnostics.Debug.WriteLine($"Setting ManageInventoryPopup visibility to true. Current value: {manageInventoryPopup.IsInventoryManagementPopupVisible}");
                manageInventoryPopup.IsInventoryManagementPopupVisible = true;
                System.Diagnostics.Debug.WriteLine($"ManageInventoryPopup visibility set to: {manageInventoryPopup.IsInventoryManagementPopupVisible}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ManageInventoryPopup is null");
            }
        }

        [RelayCommand]
        private async Task ShowActivityLogAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("📋 Showing inventory activity log...");
                
                // Get the ActivityLogPopup from the app
                var app = (App)Application.Current;
                var activityLogPopup = app.ActivityLogPopup;
                
                if (activityLogPopup != null)
                {
                    // Load activity log data
                    await activityLogPopup.LoadActivityLogAsync();
                    activityLogPopup.IsVisible = true;
                    System.Diagnostics.Debug.WriteLine("✅ Activity log popup shown");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("❌ ActivityLogPopup is null");
                    await Application.Current.MainPage.DisplayAlert("Error", "Activity log feature is not available.", "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error showing activity log: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to load activity log: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task ShowPurchaseOrdersAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("📦 Showing purchase orders...");
                
                // Get the PurchaseOrderApprovalPopup from the app
                var app = (App)Application.Current;
                var purchaseOrderPopup = app.PurchaseOrderApprovalPopup;
                
                if (purchaseOrderPopup != null)
                {
                    // Show the popup (it will load data when shown)
                    await purchaseOrderPopup.ShowAsync();
                    System.Diagnostics.Debug.WriteLine("✅ Purchase order popup shown");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("❌ PurchaseOrderApprovalPopup is null");
                    await Application.Current.MainPage.DisplayAlert("Error", "Purchase order feature is not available.", "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error showing purchase orders: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to load purchase orders: {ex.Message}", "OK");
            }
        }
        
    }
}