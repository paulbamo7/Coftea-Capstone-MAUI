using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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

        [ObservableProperty]
        private ObservableCollection<PurchaseOrderDisplayModel> processedOrders = new();

        private List<PurchaseOrderDisplayModel> _pendingOrderCache = new();
        private const int ItemsPerPage = 10;
        private const int ProcessedOrdersLimit = 10;

        [ObservableProperty]
        private int currentPage = 1;

        [ObservableProperty]
        private int totalPages = 1;

        public bool HasPendingOrders => PendingOrders.Count > 0;
        public bool HasNoPendingOrders => PendingOrders.Count == 0 && !IsLoading;
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public string PageInfo => $"Page {CurrentPage} of {TotalPages}";
        public bool HasProcessedOrders => ProcessedOrders.Count > 0;

        public PurchaseOrderApprovalPopupViewModel()
        {
            _database = new Database();
        }

        public async Task ShowAsync()
        {
            IsVisible = true;
            CurrentPage = 1;
            TotalPages = 1;
            await LoadPendingOrders();
            
            // Ensure commands are initialized
            OnPropertyChanged(nameof(HasPreviousPage));
            OnPropertyChanged(nameof(HasNextPage));
            OnPropertyChanged(nameof(PageInfo));
            System.Diagnostics.Debug.WriteLine($"🔍 ShowAsync: CurrentPage={CurrentPage}, TotalPages={TotalPages}, HasPrevious={HasPreviousPage}, HasNext={HasNextPage}");
        }

        [RelayCommand]
        private async Task LoadPendingOrders()
        {
            try
            {
                IsLoading = true;
                System.Diagnostics.Debug.WriteLine("📦 [VM] Loading pending purchase orders...");

                var pendingOrderModels = await _database.GetPendingPurchaseOrdersAsync();
                System.Diagnostics.Debug.WriteLine($"📦 [VM] Received {pendingOrderModels.Count} pending orders from database");

                var pendingDisplayOrders = await BuildDisplayOrdersAsync(pendingOrderModels, includeEditableItems: true);

                // Load processed orders separately
                var recentOrders = await _database.GetAllPurchaseOrdersAsync(20);
                var processedOrderModels = recentOrders
                    .Where(o => !string.Equals(o.Status?.Trim(), "pending", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(ProcessedOrdersLimit)
                    .ToList();

                var processedDisplayOrders = await BuildDisplayOrdersAsync(processedOrderModels, includeEditableItems: false);

                // Store pending orders and update pagination
                _pendingOrderCache = pendingDisplayOrders;
                TotalPages = (int)Math.Ceiling((double)_pendingOrderCache.Count / ItemsPerPage);
                if (TotalPages == 0) TotalPages = 1;
                
                // Apply pagination
                ApplyPagination();

                // Update processed orders collection
                UpdateProcessedOrders(processedDisplayOrders);
                
                OnPropertyChanged(nameof(HasPendingOrders));
                OnPropertyChanged(nameof(HasNoPendingOrders));
                OnPropertyChanged(nameof(HasProcessedOrders));
                OnPropertyChanged(nameof(PageInfo));

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

        private void ApplyPagination()
        {
            var startIndex = (CurrentPage - 1) * ItemsPerPage;
            var endIndex = Math.Min(startIndex + ItemsPerPage, _pendingOrderCache.Count);
            var pageOrders = _pendingOrderCache.Skip(startIndex).Take(ItemsPerPage).ToList();

            PendingOrders.Clear();
            foreach (var order in pageOrders)
            {
                PendingOrders.Add(order);
            }

            OnPropertyChanged(nameof(HasPendingOrders));
            OnPropertyChanged(nameof(HasNoPendingOrders));
            OnPropertyChanged(nameof(HasPreviousPage));
            OnPropertyChanged(nameof(HasNextPage));
            OnPropertyChanged(nameof(PageInfo));

            System.Diagnostics.Debug.WriteLine($"📄 [VM] Applied pagination: Page {CurrentPage} of {TotalPages} ({PendingOrders.Count} orders displayed)");
        }

        partial void OnCurrentPageChanged(int value)
        {
            ApplyPagination();
            OnPropertyChanged(nameof(HasPreviousPage));
            OnPropertyChanged(nameof(HasNextPage));
        }

        partial void OnTotalPagesChanged(int value)
        {
            OnPropertyChanged(nameof(HasPreviousPage));
            OnPropertyChanged(nameof(HasNextPage));
            OnPropertyChanged(nameof(PageInfo));
        }

        [RelayCommand]
        private void NextPage()
        {
            System.Diagnostics.Debug.WriteLine($"🔍 NextPageCommand called: CurrentPage={CurrentPage}, TotalPages={TotalPages}, HasNext={HasNextPage}");
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                System.Diagnostics.Debug.WriteLine($"➡️ [VM] Navigated to page {CurrentPage}");
                OnPropertyChanged(nameof(HasPreviousPage));
                OnPropertyChanged(nameof(HasNextPage));
                OnPropertyChanged(nameof(PageInfo));
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Cannot go to next page: CurrentPage={CurrentPage} >= TotalPages={TotalPages}");
            }
        }

        [RelayCommand]
        private void PreviousPage()
        {
            System.Diagnostics.Debug.WriteLine($"🔍 PreviousPageCommand called: CurrentPage={CurrentPage}, TotalPages={TotalPages}, HasPrevious={HasPreviousPage}");
            if (CurrentPage > 1)
            {
                CurrentPage--;
                System.Diagnostics.Debug.WriteLine($"⬅️ [VM] Navigated to page {CurrentPage}");
                OnPropertyChanged(nameof(HasPreviousPage));
                OnPropertyChanged(nameof(HasNextPage));
                OnPropertyChanged(nameof(PageInfo));
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Cannot go to previous page: CurrentPage={CurrentPage} <= 1");
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

                    // Refresh POS page products after purchase order approval
                    if (app?.POSVM != null)
                    {
                        await app.POSVM.LoadDataAsync();
                        System.Diagnostics.Debug.WriteLine("✅ POS products refreshed after purchase order approval");
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
                // Check if any items are already accepted - if so, cannot reject
                if (order.HasAcceptedItems)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Cannot Reject",
                        $"Cannot reject Purchase Order #{order.PurchaseOrderId} because some items have already been accepted.\n\nPlease retract the accepted items first if you want to reject the order.",
                        "OK");
                    return;
                }

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
                System.Diagnostics.Debug.WriteLine($"🔍 AcceptItem called for: {item.ItemName}");
                System.Diagnostics.Debug.WriteLine($"🔍 ApprovedQuantity: {item.ApprovedQuantity}, ApprovedUoM: '{item.ApprovedUoM}', IsPending: {item.IsPending}");
                
                if (!item.IsPending)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Item Already Processed",
                        $"This item has already been {item.ItemStatus.ToLower()}. Cannot accept again.",
                        "OK");
                    return;
                }
                
                if (item.ApprovedQuantity <= 0)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Validation Error",
                        $"Please enter a valid quantity (> 0) for {item.ItemName}. Current value: {item.ApprovedQuantity}",
                        "OK");
                    return;
                }
                
                if (string.IsNullOrWhiteSpace(item.ApprovedUoM))
                {
                    // Try to use OriginalUoM as fallback
                    if (!string.IsNullOrWhiteSpace(item.OriginalUoM))
                    {
                        item.ApprovedUoM = item.OriginalUoM;
                        System.Diagnostics.Debug.WriteLine($"🔧 Using OriginalUoM as fallback: {item.OriginalUoM}");
                    }
                    else
                    {
                        await Application.Current.MainPage.DisplayAlert(
                            "Validation Error",
                            $"Please select a unit of measurement for {item.ItemName}.",
                            "OK");
                        return;
                    }
                }

                var totalQuantity = item.ApprovedQuantity * item.Quantity;
                var confirm = await Application.Current.MainPage.DisplayAlert(
                    "Accept Item",
                    $"Accept {item.ItemName}?\n\nAmount: {item.ApprovedQuantity} {item.ApprovedUoM}\nQuantity: x{item.Quantity}\nTotal: {totalQuantity} {item.ApprovedUoM}\n\nThis will immediately add the item to inventory.",
                    "Accept", "Cancel");

                if (!confirm) return;

                System.Diagnostics.Debug.WriteLine($"✅ Accepting item: {item.ItemName} ({item.ApprovedQuantity} {item.ApprovedUoM})");

                var currentUser = App.CurrentUser?.Email ?? "Admin";
                
                // Accept this specific item - add to inventory
                System.Diagnostics.Debug.WriteLine($"🔍 Calling AcceptPurchaseOrderItemAsync with: PO={item.PurchaseOrderId}, ItemID={item.InventoryItemId}, Qty={item.ApprovedQuantity}, UoM='{item.ApprovedUoM}', User='{currentUser}'");
                
                var success = await _database.AcceptPurchaseOrderItemAsync(
                    item.PurchaseOrderId,
                    item.InventoryItemId,
                    item.ApprovedQuantity,
                    item.ApprovedUoM,
                    currentUser,
                    item.Quantity);

                System.Diagnostics.Debug.WriteLine($"🔍 AcceptPurchaseOrderItemAsync returned: {success}");

                if (success)
                {
                    item.ItemStatus = "Accepted";
                    
                    // Find the parent order and notify property change for HasAcceptedItems
                    var parentOrder = PendingOrders.FirstOrDefault(o => o.EditableItems.Contains(item));
                    if (parentOrder != null)
                    {
                        parentOrder.OnPropertyChanged(nameof(PurchaseOrderDisplayModel.HasAcceptedItems));
                        parentOrder.OnPropertyChanged(nameof(PurchaseOrderDisplayModel.HasPendingItems));
                    }
                    
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

                    // Refresh POS page products after accepting item
                    if (app?.POSVM != null)
                    {
                        await app.POSVM.LoadDataAsync();
                        System.Diagnostics.Debug.WriteLine("✅ POS products refreshed after accepting purchase order item");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ AcceptPurchaseOrderItemAsync returned false for {item.ItemName}");
                    await Application.Current.MainPage.DisplayAlert(
                        "Error",
                        $"Failed to accept item {item.ItemName}. Please check:\n\n- Item exists in inventory (ID: {item.InventoryItemId})\n- Purchase order item exists (PO: {item.PurchaseOrderId})\n- Check debug console for details",
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
        private async Task AcceptAllItems(PurchaseOrderDisplayModel order)
        {
            try
            {
                var pendingItems = order.EditableItems.Where(i => i.IsPending).ToList();
                
                if (!pendingItems.Any())
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "No Pending Items",
                        "There are no pending items in this purchase order.",
                        "OK");
                    return;
                }
                
                var confirm = await Application.Current.MainPage.DisplayAlert(
                    "Accept All Items",
                    $"Accept all {pendingItems.Count} pending item(s) in Purchase Order #{order.PurchaseOrderId}?\n\nThis will add all items to inventory immediately.",
                    "Accept All", "Cancel");
                
                if (!confirm) return;
                
                System.Diagnostics.Debug.WriteLine($"✅ Accepting all {pendingItems.Count} items in order #{order.PurchaseOrderId}");
                
                var currentUser = App.CurrentUser?.Email ?? "Admin";
                int successCount = 0;
                int failCount = 0;
                
                foreach (var item in pendingItems)
                {
                    try
                    {
                        // Ensure ApprovedUoM is set
                        if (string.IsNullOrWhiteSpace(item.ApprovedUoM))
                        {
                            item.ApprovedUoM = item.OriginalUoM ?? "pcs";
                        }
                        
                        // Ensure ApprovedQuantity is valid
                        if (item.ApprovedQuantity <= 0)
                        {
                            item.ApprovedQuantity = item.RequestedQuantity;
                        }
                        
                        var success = await _database.AcceptPurchaseOrderItemAsync(
                            item.PurchaseOrderId,
                            item.InventoryItemId,
                            item.ApprovedQuantity,
                            item.ApprovedUoM,
                            currentUser,
                            item.Quantity);
                        
                        if (success)
                        {
                            item.ItemStatus = "Accepted";
                            successCount++;
                        }
                        else
                        {
                            failCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Error accepting item {item.ItemName}: {ex.Message}");
                        failCount++;
                    }
                }
                
                // Reload orders to refresh the display
                await LoadPendingOrders();
                
                // Refresh inventory if the ViewModel is available
                var app = (App)Application.Current;
                if (app?.InventoryVM != null)
                {
                    await app.InventoryVM.ForceReloadDataAsync();
                }

                // Refresh POS page products after accepting all items
                if (app?.POSVM != null)
                {
                    await app.POSVM.LoadDataAsync();
                    System.Diagnostics.Debug.WriteLine("✅ POS products refreshed after accepting all purchase order items");
                }
                
                if (failCount > 0)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Partial Success",
                        $"Accepted {successCount} item(s) successfully.\n{failCount} item(s) failed. Please check and accept them individually.",
                        "OK");
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Success",
                        $"All {successCount} item(s) have been accepted and added to inventory!",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error accepting all items: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to accept all items: {ex.Message}", "OK");
            }
        }
        
        [RelayCommand]
        private async Task CancelAllItems(PurchaseOrderDisplayModel order)
        {
            try
            {
                var pendingItems = order.EditableItems.Where(i => i.IsPending).ToList();
                
                if (!pendingItems.Any())
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "No Pending Items",
                        "There are no pending items in this purchase order.",
                        "OK");
                    return;
                }
                
                var confirm = await Application.Current.MainPage.DisplayAlert(
                    "Cancel All Items",
                    $"Cancel all {pendingItems.Count} pending item(s) in Purchase Order #{order.PurchaseOrderId}?\n\nThis will mark all items as canceled. They will NOT be added to inventory.",
                    "Cancel All", "Back");
                
                if (!confirm) return;
                
                System.Diagnostics.Debug.WriteLine($"❌ Canceling all {pendingItems.Count} items in order #{order.PurchaseOrderId}");
                
                var currentUser = App.CurrentUser?.Email ?? "Admin";
                int successCount = 0;
                int failCount = 0;
                
                foreach (var item in pendingItems)
                {
                    try
                    {
                        var success = await _database.CancelPurchaseOrderItemAsync(
                            item.PurchaseOrderId,
                            item.InventoryItemId,
                            currentUser);
                        
                        if (success)
                        {
                            item.ItemStatus = "Canceled";
                            successCount++;
                        }
                        else
                        {
                            failCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Error canceling item {item.ItemName}: {ex.Message}");
                        failCount++;
                    }
                }
                
                // Reload orders to refresh the display
                await LoadPendingOrders();
                
                if (failCount > 0)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Partial Success",
                        $"Canceled {successCount} item(s) successfully.\n{failCount} item(s) failed. Please check and cancel them individually.",
                        "OK");
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Success",
                        $"All {successCount} item(s) have been canceled.",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error canceling all items: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to cancel all items: {ex.Message}", "OK");
            }
        }
        
        [RelayCommand]
        private async Task RetractItem(EditablePurchaseOrderItem item)
        {
            try
            {
                if (!item.IsAccepted)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Cannot Retract",
                        $"This item is not accepted. Only accepted items can be retracted.",
                        "OK");
                    return;
                }

                var confirm = await Application.Current.MainPage.DisplayAlert(
                    "Retract Item",
                    $"Retract {item.ItemName}?\n\nThis will remove the item from inventory that was previously added when accepting this purchase order item.",
                    "Retract", "Cancel");

                if (!confirm) return;

                System.Diagnostics.Debug.WriteLine($"🔙 Retracting item: {item.ItemName}");

                var currentUser = App.CurrentUser?.Email ?? "Admin";
                
                // Retract this specific item - remove from inventory
                var success = await _database.RetractPurchaseOrderItemAsync(
                    item.PurchaseOrderId,
                    item.InventoryItemId,
                    currentUser);

                if (success)
                {
                    // Reload the item from database to get the original quantity value
                    var items = await _database.GetPurchaseOrderItemsAsync(item.PurchaseOrderId);
                    var dbItem = items.FirstOrDefault(i => i.InventoryItemId == item.InventoryItemId);
                    
                    item.ItemStatus = "Pending";
                    item.ApprovedQuantity = item.RequestedQuantity; // Reset to requested quantity
                    item.ApprovedUoM = item.OriginalUoM; // Restore to original requested UoM
                    // Restore original quantity from database (default to 1 if not found)
                    item.Quantity = dbItem?.Quantity > 0 ? dbItem.Quantity : 1;
                    
                    // Find the parent order and notify property change for HasAcceptedItems
                    var parentOrder = PendingOrders.FirstOrDefault(o => o.EditableItems.Contains(item));
                    if (parentOrder != null)
                    {
                        parentOrder.OnPropertyChanged(nameof(PurchaseOrderDisplayModel.HasAcceptedItems));
                        parentOrder.OnPropertyChanged(nameof(PurchaseOrderDisplayModel.HasPendingItems));
                    }
                    
                    await Application.Current.MainPage.DisplayAlert(
                        "Retracted",
                        $"{item.ItemName} has been retracted and removed from inventory.",
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
                        "Failed to retract item. Please try again.",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error retracting item: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to retract item: {ex.Message}", "OK");
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
        private async Task ViewPurchaseOrderHistory()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("📜 Navigating to purchase order history...");
                await Shell.Current.GoToAsync("//purchaseorderhistory");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error navigating to purchase order history: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to open purchase order history: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private void Close()
        {
            IsVisible = false;
        }

        private async Task<List<PurchaseOrderDisplayModel>> BuildDisplayOrdersAsync(IEnumerable<PurchaseOrderModel> orders, bool includeEditableItems)
        {
            var displayOrders = new List<PurchaseOrderDisplayModel>();

            foreach (var order in orders)
            {
                var items = await _database.GetPurchaseOrderItemsAsync(order.PurchaseOrderId);
                var itemsPreview = BuildItemsPreview(items);

                var displayOrder = new PurchaseOrderDisplayModel
                {
                    PurchaseOrderId = order.PurchaseOrderId,
                    OrderDate = order.OrderDate,
                    SupplierName = order.SupplierName,
                    Status = order.Status,
                    RequestedBy = order.RequestedBy,
                    ApprovedBy = order.ApprovedBy,
                    TotalAmount = order.TotalAmount,
                    CreatedAt = order.CreatedAt,
                    ItemsPreview = itemsPreview,
                    Items = items
                };

                if (includeEditableItems)
                {
                    var editableItems = new ObservableCollection<EditablePurchaseOrderItem>();
                    foreach (var item in items)
                    {
                        var editableItem = new EditablePurchaseOrderItem(item, order.PurchaseOrderId);
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
                    displayOrder.EditableItems = editableItems;
                    displayOrder.InitializeItemSubscriptions();
                }
                else
                {
                    displayOrder.EditableItems = new ObservableCollection<EditablePurchaseOrderItem>();
                }

                displayOrders.Add(displayOrder);
            }

            return displayOrders;
        }

        private static string BuildItemsPreview(List<PurchaseOrderItemModel> items)
        {
            if (items.Count == 0) return "No items";

            var preview = string.Join(", ", items.Take(3).Select(i => $"{i.ItemName} ({i.RequestedQuantity} {i.UnitOfMeasurement})"));
            if (items.Count > 3)
            {
                preview += $" and {items.Count - 3} more...";
            }
            return preview;
        }

        private void UpdateProcessedOrders(IEnumerable<PurchaseOrderDisplayModel> orders)
        {
            ProcessedOrders.Clear();
            foreach (var order in orders)
            {
                ProcessedOrders.Add(order);
            }
            OnPropertyChanged(nameof(HasProcessedOrders));
        }
    }

    // Display model with items preview
    public class PurchaseOrderDisplayModel : PurchaseOrderModel, INotifyPropertyChanged
    {
        public string ItemsPreview { get; set; } = string.Empty;
        public ObservableCollection<EditablePurchaseOrderItem> EditableItems { get; set; } = new();
        public List<PurchaseOrderItemModel> Items { get; set; } = new();
        public bool IsExpanded { get; set; } = false;
        
        public PurchaseOrderDisplayModel()
        {
            // Subscribe to item collection changes
            EditableItems.CollectionChanged += (sender, e) =>
            {
                OnPropertyChanged(nameof(HasAcceptedItems));
                OnPropertyChanged(nameof(HasPendingItems));
                
                // Subscribe to new items
                if (e.NewItems != null)
                {
                    foreach (EditablePurchaseOrderItem item in e.NewItems)
                    {
                        item.PropertyChanged += Item_PropertyChanged;
                    }
                }
                
                // Unsubscribe from removed items
                if (e.OldItems != null)
                {
                    foreach (EditablePurchaseOrderItem item in e.OldItems)
                    {
                        item.PropertyChanged -= Item_PropertyChanged;
                    }
                }
            };
        }
        
        public void InitializeItemSubscriptions()
        {
            // Subscribe to each existing item's status changes
            foreach (var item in EditableItems)
            {
                item.PropertyChanged -= Item_PropertyChanged; // Remove existing to avoid duplicates
                item.PropertyChanged += Item_PropertyChanged;
            }
        }
        
        private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(EditablePurchaseOrderItem.ItemStatus))
            {
                OnPropertyChanged(nameof(HasAcceptedItems));
                OnPropertyChanged(nameof(HasPendingItems));
            }
        }
        
        // Check if any items in this order are accepted
        public bool HasAcceptedItems => EditableItems?.Any(item => item.IsAccepted) ?? false;
        
        // Check if any items in this order are pending
        public bool HasPendingItems => EditableItems?.Any(item => item.IsPending) ?? false;
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        public void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
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
                ItemStatus = "Pending";
            }
            
            // Load quantity from database model (default to 1 if not set)
            Quantity = item.Quantity > 0 ? item.Quantity : 1;
            
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

