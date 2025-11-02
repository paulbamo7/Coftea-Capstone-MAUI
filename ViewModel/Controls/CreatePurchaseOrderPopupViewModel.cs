using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Coftea_Capstone.Models;
using Coftea_Capstone.Services;

namespace Coftea_Capstone.ViewModel.Controls
{
    public partial class CreatePurchaseOrderPopupViewModel : ObservableObject
    {
        private readonly Database _database = new();

        [ObservableProperty]
        private bool isVisible;

        [ObservableProperty]
        private bool isLoading;

        private ObservableCollection<CreateOrderEditableItem> _editableItems = new();
        public ObservableCollection<CreateOrderEditableItem> EditableItems
        {
            get => _editableItems;
            set
            {
                if (_editableItems != null)
                {
                    _editableItems.CollectionChanged -= OnEditableItemsChanged;
                }
                _editableItems = value;
                if (_editableItems != null)
                {
                    _editableItems.CollectionChanged += OnEditableItemsChanged;
                }
                OnPropertyChanged();
            }
        }

        private void OnEditableItemsChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Notify UI that EditableItems changed so buttons can update
            OnPropertyChanged(nameof(EditableItems));
            // Also notify FilteredInventoryItems to refresh
            FilteredInventoryItems = new ObservableCollection<InventoryPageModel>(FilteredInventoryItems);
        }

        [ObservableProperty]
        private string infoText = string.Empty;

        [ObservableProperty]
        private int? createdPurchaseOrderId;

        [ObservableProperty]
        private bool showSuccessView;

        [ObservableProperty]
        private bool isManualSelectionVisible;

        [ObservableProperty]
        private ObservableCollection<InventoryPageModel> allInventoryItems = new();

        [ObservableProperty]
        private ObservableCollection<InventoryPageModel> filteredInventoryItems = new();

        [ObservableProperty]
        private string searchText = string.Empty;

        public bool IsAdmin { get; set; }

        public CreatePurchaseOrderPopupViewModel()
        {
            // Initialize EditableItems with CollectionChanged handler
            if (_editableItems != null)
            {
                _editableItems.CollectionChanged += OnEditableItemsChanged;
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            FilterInventoryItems();
        }

        public async Task LoadItems(System.Collections.Generic.List<InventoryPageModel> lowStockItems, bool isAdmin)
        {
            IsAdmin = isAdmin;
            EditableItems.Clear();
            ShowSuccessView = false;
            CreatedPurchaseOrderId = null;
            IsManualSelectionVisible = false;
            SearchText = string.Empty;

            // Load all inventory items for manual selection
            try
            {
                var allItems = await _database.GetInventoryItemsAsyncCached();
                AllInventoryItems = new ObservableCollection<InventoryPageModel>(allItems ?? new List<InventoryPageModel>());
                FilteredInventoryItems = new ObservableCollection<InventoryPageModel>(AllInventoryItems);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error loading inventory items: {ex.Message}");
                AllInventoryItems = new ObservableCollection<InventoryPageModel>();
                FilteredInventoryItems = new ObservableCollection<InventoryPageModel>();
            }

            foreach (var item in lowStockItems)
            {
                var neededQuantity = (int)(item.minimumQuantity - item.itemQuantity);
                var editableItem = new CreateOrderEditableItem
                {
                    InventoryItemId = item.itemID,
                    ItemName = item.itemName,
                    ItemCategory = item.itemCategory ?? "",
                    RequestedQuantity = neededQuantity,
                    CurrentStockQuantity = item.itemQuantity, // Store current stock quantity
                    ApprovedQuantity = neededQuantity, // Default unit amount to needed quantity
                    Quantity = 1, // Default to 1 unit
                    ApprovedUoM = item.unitOfMeasurement ?? "pcs",
                    OriginalUoM = item.unitOfMeasurement ?? "pcs",
                    UnitPrice = 10.0m, // Default unit price
                    TotalPrice = neededQuantity * 10.0m
                };
                EditableItems.Add(editableItem);
            }

            InfoText = "This will create a purchase order. Please accept items in 'Manage Purchase Order' to update inventory.";
        }

        [RelayCommand]
        private async Task CreateOrder()
        {
            try
            {
                // Validate all items have valid quantities
                var invalidItems = EditableItems.Where(i => i.ApprovedQuantity <= 0 || string.IsNullOrWhiteSpace(i.ApprovedUoM)).ToList();
                if (invalidItems.Any())
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Validation Error",
                        "Please ensure all items have valid quantities (> 0) and unit of measurement selected.",
                        "OK");
                    return;
                }

                IsLoading = true;

                // Convert editable items back to InventoryPageModel for database
                var itemsForDb = EditableItems.Select(ei => new InventoryPageModel
                {
                    itemID = ei.InventoryItemId,
                    itemName = ei.ItemName,
                    itemCategory = ei.ItemCategory,
                    minimumQuantity = ei.ApprovedQuantity, // Use approved quantity
                    unitOfMeasurement = ei.ApprovedUoM
                }).ToList();

                // Create purchase order with custom quantities
                var purchaseOrderId = await _database.CreatePurchaseOrderWithCustomQuantitiesAsync(itemsForDb, new { Items = EditableItems });

                if (purchaseOrderId > 0)
                {
                    // Store the created purchase order ID
                    CreatedPurchaseOrderId = purchaseOrderId;

                    // Show success view with PDF button
                    // All purchase orders (admin and regular users) now require explicit approval in ManagePurchaseOrder
                    ShowSuccessView = true;
                    InfoText = $"✅ Purchase order #{purchaseOrderId} has been created with your custom amounts!\n\nPlease accept the items in 'Manage Purchase Order' to update inventory.";
                    
                    System.Diagnostics.Debug.WriteLine($"✅ Purchase order {purchaseOrderId} created - requires approval in ManagePurchaseOrder");
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Error",
                        "Failed to create purchase order. Please try again.",
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
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task ExportPDFAsync()
        {
            if (!CreatedPurchaseOrderId.HasValue)
                return;

            try
            {
                IsLoading = true;
                
                // Get purchase order and items
                var orders = await _database.GetAllPurchaseOrdersAsync(100);
                var order = orders.FirstOrDefault(o => o.PurchaseOrderId == CreatedPurchaseOrderId.Value);
                
                if (order == null)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Error",
                        "Purchase order not found.",
                        "OK");
                    return;
                }

                var items = await _database.GetPurchaseOrderItemsAsync(CreatedPurchaseOrderId.Value);
                
                // Generate PDF
                var pdfService = new PDFReportService();
                var filePath = await pdfService.GeneratePurchaseOrderPDFAsync(
                    CreatedPurchaseOrderId.Value,
                    order,
                    items);

                await Application.Current.MainPage.DisplayAlert(
                    "PDF Generated",
                    $"Purchase Order PDF has been saved successfully!\n\nFile location: {filePath}\n\nTo find the file:\n1. Open File Manager\n2. Go to Download folder\n3. Look for Purchase_Order_{CreatedPurchaseOrderId.Value}_*.pdf",
                    "OK");

                System.Diagnostics.Debug.WriteLine($"✅ PDF exported successfully: {filePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error exporting PDF: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    $"Failed to export PDF:\n\n{ex.Message}",
                    "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void IncreaseUnitAmount(CreateOrderEditableItem item)
        {
            if (item != null)
            {
                item.ApprovedQuantity += 1;
            }
        }

        [RelayCommand]
        private void DecreaseUnitAmount(CreateOrderEditableItem item)
        {
            if (item != null && item.ApprovedQuantity > 0)
            {
                item.ApprovedQuantity -= 1;
            }
        }

        [RelayCommand]
        private void IncreaseQuantity(CreateOrderEditableItem item)
        {
            if (item != null)
            {
                item.Quantity += 1;
            }
        }

        [RelayCommand]
        private void DecreaseQuantity(CreateOrderEditableItem item)
        {
            if (item != null && item.Quantity > 1)
            {
                item.Quantity -= 1;
            }
        }

        [RelayCommand]
        private void ToggleManualSelection()
        {
            IsManualSelectionVisible = !IsManualSelectionVisible;
        }

        [RelayCommand]
        private void FilterInventoryItems()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredInventoryItems = new ObservableCollection<InventoryPageModel>(AllInventoryItems);
                return;
            }

            var searchLower = SearchText.ToLowerInvariant();
            var filtered = AllInventoryItems.Where(item =>
                item.itemName.ToLowerInvariant().Contains(searchLower) ||
                (item.itemCategory ?? "").ToLowerInvariant().Contains(searchLower)
            ).ToList();

            FilteredInventoryItems = new ObservableCollection<InventoryPageModel>(filtered);
        }

        [RelayCommand]
        private void AddManualItem(InventoryPageModel item)
        {
            if (item == null) return;

            // Check if item is already in EditableItems
            var existingItem = EditableItems.FirstOrDefault(ei => ei.InventoryItemId == item.itemID);
            if (existingItem != null)
            {
                // Remove if already exists
                EditableItems.Remove(existingItem);
                System.Diagnostics.Debug.WriteLine($"✅ Removed {item.itemName} from purchase order manually");
                OnPropertyChanged(nameof(EditableItems)); // Notify UI to refresh
                return;
            }

            // Add new item
            var editableItem = new CreateOrderEditableItem
            {
                InventoryItemId = item.itemID,
                ItemName = item.itemName,
                ItemCategory = item.itemCategory ?? "",
                RequestedQuantity = 1,
                CurrentStockQuantity = item.itemQuantity,
                ApprovedQuantity = 1, // Default to 1
                Quantity = 1, // Default to 1 unit
                ApprovedUoM = item.unitOfMeasurement ?? "pcs",
                OriginalUoM = item.unitOfMeasurement ?? "pcs",
                UnitPrice = 10.0m, // Default unit price
                TotalPrice = 1 * 10.0m
            };
            EditableItems.Add(editableItem);

            System.Diagnostics.Debug.WriteLine($"✅ Added {item.itemName} to purchase order manually");
            OnPropertyChanged(nameof(EditableItems)); // Notify UI to refresh
        }

        // Helper method to check if item is already in EditableItems
        public bool IsItemInOrder(int inventoryItemId)
        {
            return EditableItems.Any(ei => ei.InventoryItemId == inventoryItemId);
        }

        [RelayCommand]
        private void RemoveItem(CreateOrderEditableItem item)
        {
            if (item != null && EditableItems.Contains(item))
            {
                EditableItems.Remove(item);
                System.Diagnostics.Debug.WriteLine($"✅ Removed {item.ItemName} from purchase order");
            }
        }

        [RelayCommand]
        private void Close()
        {
            ShowSuccessView = false;
            CreatedPurchaseOrderId = null;
            IsVisible = false;
            IsManualSelectionVisible = false;
            SearchText = string.Empty;
        }
    }

    // Editable item for creating purchase orders
    public partial class CreateOrderEditableItem : ObservableObject
    {
        [ObservableProperty]
        private int inventoryItemId;

        [ObservableProperty]
        private string itemName = string.Empty;

        [ObservableProperty]
        private string itemCategory = string.Empty;

        [ObservableProperty]
        private int requestedQuantity;

        [ObservableProperty]
        private double approvedQuantity; // Unit amount (e.g., 3 L per unit)

        partial void OnApprovedQuantityChanged(double value)
        {
            OnPropertyChanged(nameof(TotalAmount));
        }

        [ObservableProperty]
        private int quantity = 1; // Quantity multiplier (e.g., x3 units)

        partial void OnQuantityChanged(int value)
        {
            OnPropertyChanged(nameof(TotalAmount));
        }

        [ObservableProperty]
        private string approvedUoM = string.Empty;

        partial void OnApprovedUoMChanged(string value)
        {
            // Validate that selected UoM is in the AvailableUoMs list
            if (!string.IsNullOrWhiteSpace(value) && !AvailableUoMs.Contains(value))
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Invalid UoM selected: {value}, resetting to original UoM");
                ApprovedUoM = OriginalUoM ?? "pcs";
                return;
            }
            OnPropertyChanged(nameof(TotalAmount));
        }

        [ObservableProperty]
        private decimal unitPrice;

        [ObservableProperty]
        private decimal totalPrice;

        [ObservableProperty]
        private string originalUoM = string.Empty;

        [ObservableProperty]
        private double currentStockQuantity; // Current stock quantity in inventory

        public double TotalAmount => ApprovedQuantity * Quantity; // Total = Unit Amount × Quantity

        public List<string> AvailableUoMs { get; set; } = new() { "pcs", "kg", "g", "L", "ml" };
    }
}

