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
                        editableItems.Add(new EditablePurchaseOrderItem(item));
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
        private double approvedQuantity;

        [ObservableProperty]
        private string approvedUoM = string.Empty;

        [ObservableProperty]
        private decimal unitPrice;

        [ObservableProperty]
        private decimal totalPrice;

        [ObservableProperty]
        private string originalUoM = string.Empty;

        public List<string> AvailableUoMs { get; set; } = new() { "pcs", "kg", "g", "L", "ml" };

        public EditablePurchaseOrderItem(PurchaseOrderItemModel item)
        {
            PurchaseOrderItemId = item.PurchaseOrderItemId;
            InventoryItemId = item.InventoryItemId;
            ItemName = item.ItemName;
            ItemCategory = item.ItemCategory;
            RequestedQuantity = item.RequestedQuantity;
            ApprovedQuantity = item.RequestedQuantity; // Default to requested quantity
            ApprovedUoM = item.UnitOfMeasurement;
            OriginalUoM = item.UnitOfMeasurement;
            UnitPrice = item.UnitPrice;
            TotalPrice = item.TotalPrice;
        }
    }
}

