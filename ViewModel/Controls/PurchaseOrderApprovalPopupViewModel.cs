using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Coftea_Capstone.Models;

namespace Coftea_Capstone.ViewModel.Controls
{
    public partial class PurchaseOrderApprovalPopupViewModel : ObservableObject
    {
        private readonly Database _database;

        [ObservableProperty]
        private bool isVisible;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private ObservableCollection<PurchaseOrderDisplayModel> pendingOrders = new();

        public bool HasPendingOrders => PendingOrders.Count > 0;
        public bool HasNoPendingOrders => PendingOrders.Count == 0 && !IsLoading;

        public PurchaseOrderApprovalPopupViewModel()
        {
            _database = new Database();
        }

        public async Task ShowAsync()
        {
            IsVisible = true;
            await LoadPendingOrders();
        }

        [RelayCommand]
        private async Task LoadPendingOrders()
        {
            try
            {
                IsLoading = true;
                System.Diagnostics.Debug.WriteLine("📦 [VM] Loading pending purchase orders...");

                var orders = await _database.GetPendingPurchaseOrdersAsync();
                System.Diagnostics.Debug.WriteLine($"📦 [VM] Received {orders.Count} pending orders from database");

                // If no pending orders, show recent orders as fallback
                if (orders.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("📦 [VM] No pending orders found, loading recent orders as fallback...");
                    orders = await _database.GetAllPurchaseOrdersAsync(20); // Get last 20 orders
                    System.Diagnostics.Debug.WriteLine($"📦 [VM] Received {orders.Count} recent orders from database");
                }

                // Get items for each order
                var displayOrders = new ObservableCollection<PurchaseOrderDisplayModel>();
                foreach (var order in orders)
                {
                    System.Diagnostics.Debug.WriteLine($"📦 [VM] Processing order #{order.PurchaseOrderId}");
                    
                    var items = await _database.GetPurchaseOrderItemsAsync(order.PurchaseOrderId);
                    System.Diagnostics.Debug.WriteLine($"📦 [VM] Order #{order.PurchaseOrderId} has {items.Count} items");
                    
                    var itemsPreview = items.Count > 0
                        ? string.Join(", ", items.Take(3).Select(i => $"{i.ItemName} ({i.RequestedQuantity} {i.UnitOfMeasurement})"))
                        : "No items";

                    if (items.Count > 3)
                        itemsPreview += $" and {items.Count - 3} more...";

                    // Create editable items
                    var editableItems = new ObservableCollection<EditablePurchaseOrderItem>();
                    foreach (var item in items)
                    {
                        var editableItem = new EditablePurchaseOrderItem(item, order.PurchaseOrderId);
                        
                        // Check if item is canceled (approvedQuantity = -1) by querying database
                        var dbItemStatus = await _database.GetPurchaseOrderItemStatusAsync(order.PurchaseOrderId, item.InventoryItemId);
                        if (dbItemStatus.HasValue && dbItemStatus.Value < 0)
                        {
                            editableItem.ItemStatus = "Canceled";
                        }
                        else if (item.ApprovedQuantity > 0)
                        {
                            editableItem.ItemStatus = "Accepted";
                        }
                        else
                        {
                            editableItem.ItemStatus = "Pending";
                        }
                        
                        editableItems.Add(editableItem);
                    }

                    var displayOrder = new PurchaseOrderDisplayModel
                    {
                        PurchaseOrderId = order.PurchaseOrderId,
                        OrderDate = order.OrderDate,
                        SupplierName = order.SupplierName,
                        Status = order.Status,
                        RequestedBy = order.RequestedBy,
                        TotalAmount = order.TotalAmount,
                        CreatedAt = order.CreatedAt,
                        ItemsPreview = itemsPreview,
                        Items = items,
                        EditableItems = editableItems
                    };
                    
                    displayOrders.Add(displayOrder);
                    System.Diagnostics.Debug.WriteLine($"📦 [VM] Added order #{displayOrder.PurchaseOrderId} to display list");
                }

                PendingOrders = displayOrders;
                OnPropertyChanged(nameof(HasPendingOrders));
                OnPropertyChanged(nameof(HasNoPendingOrders));

                System.Diagnostics.Debug.WriteLine($"✅ [VM] UI updated with {PendingOrders.Count} pending orders");
                System.Diagnostics.Debug.WriteLine($"✅ [VM] HasPendingOrders: {HasPendingOrders}, HasNoPendingOrders: {HasNoPendingOrders}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ [VM] Error loading pending orders: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ [VM] Stack trace: {ex.StackTrace}");
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to load purchase orders: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
                System.Diagnostics.Debug.WriteLine($"📦 [VM] Loading finished. IsLoading = {IsLoading}");
            }
        }

        [RelayCommand]
        private async Task ApproveOrder(PurchaseOrderDisplayModel order)
        {
            try
            {
                // Validate all items have valid quantities
                var invalidItems = order.EditableItems.Where(i => i.ApprovedQuantity <= 0 || string.IsNullOrWhiteSpace(i.ApprovedUoM)).ToList();
                if (invalidItems.Any())
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Validation Error",
                        $"Please ensure all items have valid quantities (> 0) and unit of measurement selected.",
                        "OK");
                    return;
                }

                var confirm = await Application.Current.MainPage.DisplayAlert(
                    "Approve Purchase Order",
                    $"Approve Purchase Order #{order.PurchaseOrderId}?\n\n" +
                    $"This will update inventory quantities using the custom amounts you've set.",
                    "Approve", "Cancel");

                if (!confirm) return;

                System.Diagnostics.Debug.WriteLine($"✅ Approving purchase order #{order.PurchaseOrderId}");

                var currentUser = App.CurrentUser?.Email ?? "Admin";
                
                // Prepare custom quantities and UoMs for approval
                var customItemsList = order.EditableItems.Select(ei => new
                {
                    ei.InventoryItemId,
                    ei.ItemName,
                    ApprovedQuantity = ei.ApprovedQuantity,
                    ApprovedUoM = ei.ApprovedUoM
                }).ToList();

                var success = await _database.UpdatePurchaseOrderStatusAsync(
                    order.PurchaseOrderId, 
                    "Approved", 
                    currentUser,
                    new { Items = customItemsList });

                if (success)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Success",
                        $"Purchase Order #{order.PurchaseOrderId} has been approved!\nInventory has been updated with your custom amounts.",
                        "OK");

                    // Reload orders
                    await LoadPendingOrders();

                    // Refresh inventory if the ViewModel is available
                    var app = (App)Application.Current;
                    if (app?.InventoryVM != null)
                    {
                        await app.InventoryVM.ForceReloadDataAsync();
                    }
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Error",
                        "Failed to approve purchase order. Please try again.",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error approving order: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to approve order: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task RejectOrder(PurchaseOrderDisplayModel order)
        {
            try
            {
                var reason = await Application.Current.MainPage.DisplayPromptAsync(
                    "Reject Purchase Order",
                    $"Enter reason for rejecting Purchase Order #{order.PurchaseOrderId}:",
                    "Reject", "Cancel",
                    placeholder: "Reason for rejection...",
                    maxLength: 200);

                if (string.IsNullOrWhiteSpace(reason)) return;

                System.Diagnostics.Debug.WriteLine($"❌ Rejecting purchase order #{order.PurchaseOrderId}");

                var currentUser = App.CurrentUser?.Email ?? "Admin";
                var success = await _database.UpdatePurchaseOrderStatusAsync(order.PurchaseOrderId, "Rejected", currentUser);

                if (success)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Rejected",
                        $"Purchase Order #{order.PurchaseOrderId} has been rejected.",
                        "OK");

                    // Reload orders
                    await LoadPendingOrders();
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Error",
                        "Failed to reject purchase order. Please try again.",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error rejecting order: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to reject order: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task AcceptItem(EditablePurchaseOrderItem item)
        {
            try
            {
                if (item.ApprovedQuantity <= 0 || string.IsNullOrWhiteSpace(item.ApprovedUoM))
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Validation Error",
                        "Please enter a valid quantity (> 0) and select a unit of measurement.",
                        "OK");
                    return;
                }

                var confirm = await Application.Current.MainPage.DisplayAlert(
                    "Accept Item",
                    $"Accept {item.ItemName}?\n\nApproved Quantity: {item.ApprovedQuantity} {item.ApprovedUoM}\n\nThis will immediately add the item to inventory.",
                    "Accept", "Cancel");

                if (!confirm) return;

                System.Diagnostics.Debug.WriteLine($"✅ Accepting item: {item.ItemName} ({item.ApprovedQuantity} {item.ApprovedUoM})");

                var currentUser = App.CurrentUser?.Email ?? "Admin";
                
                // Accept this specific item - add to inventory
                var success = await _database.AcceptPurchaseOrderItemAsync(
                    item.PurchaseOrderId,
                    item.InventoryItemId,
                    item.ApprovedQuantity,
                    item.ApprovedUoM,
                    currentUser);

                if (success)
                {
                    item.ItemStatus = "Accepted";
                    await Application.Current.MainPage.DisplayAlert(
                        "Success",
                        $"{item.ItemName} has been accepted and added to inventory!",
                        "OK");

                    // Reload orders to refresh the display
                    await LoadPendingOrders();

                    // Refresh inventory if the ViewModel is available
                    var app = (App)Application.Current;
                    if (app?.InventoryVM != null)
                    {
                        await app.InventoryVM.ForceReloadDataAsync();
                    }
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Error",
                        "Failed to accept item. Please try again.",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error accepting item: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to accept item: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task CancelItem(EditablePurchaseOrderItem item)
        {
            try
            {
                var confirm = await Application.Current.MainPage.DisplayAlert(
                    "Cancel Item",
                    $"Cancel {item.ItemName}?\n\nThis item will not be added to inventory.",
                    "Cancel Item", "Back");

                if (!confirm) return;

                System.Diagnostics.Debug.WriteLine($"❌ Canceling item: {item.ItemName}");

                var currentUser = App.CurrentUser?.Email ?? "Admin";
                
                // Cancel this specific item - mark as canceled, don't add to inventory
                var success = await _database.CancelPurchaseOrderItemAsync(
                    item.PurchaseOrderId,
                    item.InventoryItemId,
                    currentUser);

                if (success)
                {
                    item.ItemStatus = "Canceled";
                    await Application.Current.MainPage.DisplayAlert(
                        "Canceled",
                        $"{item.ItemName} has been canceled and will not be added to inventory.",
                        "OK");

                    // Reload orders to refresh the display
                    await LoadPendingOrders();
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Error",
                        "Failed to cancel item. Please try again.",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error canceling item: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to cancel item: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task ExportPurchaseOrderPDF(PurchaseOrderDisplayModel order)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"📄 Starting PDF generation for Purchase Order #{order.PurchaseOrderId}");
                
                var pdfService = new Services.PDFReportService();
                var items = await _database.GetPurchaseOrderItemsAsync(order.PurchaseOrderId);
                
                System.Diagnostics.Debug.WriteLine($"📄 Found {items?.Count ?? 0} items for purchase order");
                
                var filePath = await pdfService.GeneratePurchaseOrderPDFAsync(order.PurchaseOrderId, order, items);
                
                System.Diagnostics.Debug.WriteLine($"📄 PDF generated successfully at: {filePath}");
                
                await Application.Current.MainPage.DisplayAlert(
                    "PDF Generated",
                    $"Purchase Order PDF has been saved successfully!\n\nFile location: {filePath}\n\nTo find the file:\n1. Open File Manager\n2. Go to Download folder\n3. Look for Purchase_Order_{order.PurchaseOrderId}_*.pdf",
                    "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error exporting purchase order PDF: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Inner exception: {ex.InnerException.Message}");
                }
                await Application.Current.MainPage.DisplayAlert(
                    "Error", 
                    $"Failed to export purchase order PDF:\n\n{ex.Message}\n\nPlease check the debug console for more details.", 
                    "OK");
            }
        }

        [RelayCommand]
        private void IncreaseItemUnitAmount(EditablePurchaseOrderItem item)
        {
            if (item != null && item.IsPending)
            {
                item.ApprovedQuantity += 1;
            }
        }

        [RelayCommand]
        private void DecreaseItemUnitAmount(EditablePurchaseOrderItem item)
        {
            if (item != null && item.IsPending && item.ApprovedQuantity > 0)
            {
                item.ApprovedQuantity -= 1;
            }
        }

        [RelayCommand]
        private void IncreaseItemQuantity(EditablePurchaseOrderItem item)
        {
            if (item != null && item.IsPending)
            {
                item.Quantity += 1;
            }
        }

        [RelayCommand]
        private void DecreaseItemQuantity(EditablePurchaseOrderItem item)
        {
            if (item != null && item.IsPending && item.Quantity > 1)
            {
                item.Quantity -= 1;
            }
        }

        [RelayCommand]
        private void Close()
        {
            IsVisible = false;
        }
    }

    // Display model with items preview
    public class PurchaseOrderDisplayModel : PurchaseOrderModel
    {
        public string ItemsPreview { get; set; } = string.Empty;
        public ObservableCollection<EditablePurchaseOrderItem> EditableItems { get; set; } = new();
        public List<PurchaseOrderItemModel> Items { get; set; } = new();
        public bool IsExpanded { get; set; } = false;
    }

    // Editable item model with custom amount and UoM
    public partial class EditablePurchaseOrderItem : ObservableObject
    {
        [ObservableProperty]
        private int purchaseOrderItemId;

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
            OnPropertyChanged(nameof(TotalAmount));
        }

        [ObservableProperty]
        private decimal unitPrice;

        [ObservableProperty]
        private decimal totalPrice;

        [ObservableProperty]
        private string originalUoM = string.Empty;

        [ObservableProperty]
        private string itemStatus = "Pending"; // Pending, Accepted, Canceled

        [ObservableProperty]
        private int purchaseOrderId; // Reference to parent order

        public bool IsPending => ItemStatus == "Pending";
        public bool IsAccepted => ItemStatus == "Accepted";
        public bool IsCanceled => ItemStatus == "Canceled";

        public double TotalAmount => ApprovedQuantity * Quantity; // Total = Unit Amount × Quantity

        public List<string> AvailableUoMs { get; set; } = new() { "pcs", "kg", "g", "L", "ml" };

        public EditablePurchaseOrderItem(PurchaseOrderItemModel item, int purchaseOrderId)
        {
            PurchaseOrderItemId = item.PurchaseOrderItemId;
            InventoryItemId = item.InventoryItemId;
            ItemName = item.ItemName;
            ItemCategory = item.ItemCategory;
            RequestedQuantity = item.RequestedQuantity;
            
            // Check if item is already processed
            // We need to query the database to check the actual approvedQuantity value
            // For now, if approvedQuantity > 0, it's accepted
            if (item.ApprovedQuantity > 0)
            {
                ApprovedQuantity = item.ApprovedQuantity;
                ItemStatus = "Accepted";
            }
            else
            {
                ApprovedQuantity = item.RequestedQuantity; // Default unit amount to requested quantity
                Quantity = 1; // Default to 1 unit
                ItemStatus = "Pending";
            }
            
            // Check database for canceled status (approvedQuantity = -1)
            // This will be set after we load from database
            
            ApprovedUoM = item.UnitOfMeasurement ?? string.Empty;
            OriginalUoM = item.UnitOfMeasurement ?? string.Empty;
            UnitPrice = item.UnitPrice;
            TotalPrice = item.TotalPrice;
            PurchaseOrderId = purchaseOrderId;
        }
    }
}

