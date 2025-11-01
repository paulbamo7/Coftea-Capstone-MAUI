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

        [ObservableProperty]
        private ObservableCollection<CreateOrderEditableItem> editableItems = new();

        [ObservableProperty]
        private string infoText = string.Empty;

        [ObservableProperty]
        private int? createdPurchaseOrderId;

        [ObservableProperty]
        private bool showSuccessView;

        public bool IsAdmin { get; set; }

        public CreatePurchaseOrderPopupViewModel()
        {
        }

        public void LoadItems(System.Collections.Generic.List<InventoryPageModel> lowStockItems, bool isAdmin)
        {
            IsAdmin = isAdmin;
            EditableItems.Clear();
            ShowSuccessView = false;
            CreatedPurchaseOrderId = null;

            foreach (var item in lowStockItems)
            {
                var neededQuantity = (int)(item.minimumQuantity - item.itemQuantity);
                var editableItem = new CreateOrderEditableItem
                {
                    InventoryItemId = item.itemID,
                    ItemName = item.itemName,
                    ItemCategory = item.itemCategory ?? "",
                    RequestedQuantity = neededQuantity,
                    ApprovedQuantity = neededQuantity, // Default unit amount to needed quantity
                    Quantity = 1, // Default to 1 unit
                    ApprovedUoM = item.unitOfMeasurement ?? "pcs",
                    OriginalUoM = item.unitOfMeasurement ?? "pcs",
                    UnitPrice = 10.0m, // Default unit price
                    TotalPrice = neededQuantity * 10.0m
                };
                EditableItems.Add(editableItem);
            }

            InfoText = IsAdmin
                ? "This will create and auto-approve the purchase order, send SMS to supplier, and update inventory immediately."
                : "This will create a purchase order and send SMS to supplier. Admin approval required.";
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
                    var currentUser = App.CurrentUser?.Email ?? "Unknown";

                    if (IsAdmin)
                    {
                        // Admin is creating the order - auto-approve it with custom quantities
                        System.Diagnostics.Debug.WriteLine("👑 Admin user detected - auto-approving purchase order");

                        // Prepare custom items for approval
                        var customItemsList = EditableItems.Select(ei => new
                        {
                            ei.InventoryItemId,
                            ei.ItemName,
                            ApprovedQuantity = ei.ApprovedQuantity,
                            ApprovedUoM = ei.ApprovedUoM
                        }).ToList();

                        bool approved = false;
                        try
                        {
                            approved = await _database.UpdatePurchaseOrderStatusAsync(
                                purchaseOrderId,
                                "Approved",
                                currentUser,
                                new { Items = customItemsList });
                        }
                        catch (Exception approvalEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"❌ Exception during auto-approval: {approvalEx.Message}");
                            System.Diagnostics.Debug.WriteLine($"Stack trace: {approvalEx.StackTrace}");
                            approved = false;
                        }

                        // Store the created purchase order ID
                        CreatedPurchaseOrderId = purchaseOrderId;

                        if (approved)
                        {
                            // Send SMS to supplier
                            var smsSent = await PurchaseOrderSMSService.SendPurchaseOrderToSupplierAsync(purchaseOrderId, itemsForDb);

                            // Show success view with PDF button
                            ShowSuccessView = true;
                            InfoText = $"✅ Purchase order #{purchaseOrderId} has been created and auto-approved!\n\n📱 SMS app should have opened. Please press 'Send' to notify the supplier.\n\nInventory has been updated.";
                            
                            System.Diagnostics.Debug.WriteLine($"✅ Purchase order {purchaseOrderId} auto-approved by admin");
                        }
                        else
                        {
                            // Order was created successfully, but auto-approval failed
                            // Show success view with PDF button
                            ShowSuccessView = true;
                            InfoText = $"✅ Purchase order #{purchaseOrderId} has been created successfully!\n\n⚠️ Auto-approval was not completed, but the order is ready for manual approval.\n\nYou can view and approve it in 'Manage Purchase Order'.";
                            
                            System.Diagnostics.Debug.WriteLine($"✅ Purchase order {purchaseOrderId} created successfully, but auto-approval failed");
                        }
                    }
                    else
                    {
                        // Regular user - requires admin approval
                        System.Diagnostics.Debug.WriteLine("👤 Regular user detected - purchase order requires admin approval");

                        // Store the created purchase order ID
                        CreatedPurchaseOrderId = purchaseOrderId;

                        // Send SMS to supplier
                        var smsSent = await PurchaseOrderSMSService.SendPurchaseOrderToSupplierAsync(purchaseOrderId, itemsForDb);

                        // Notify admin via SMS
                        var adminNotified = await PurchaseOrderSMSService.NotifyAdminOfPurchaseOrderAsync(purchaseOrderId, currentUser);

                        // Show success view with PDF button
                        ShowSuccessView = true;
                        InfoText = $"✅ Purchase order #{purchaseOrderId} has been created with your custom amounts!\n\n📱 SMS app should have opened. Please press 'Send' to notify the supplier and admin.\n\nAdmin approval is required.";
                    }
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
        private void Close()
        {
            ShowSuccessView = false;
            CreatedPurchaseOrderId = null;
            IsVisible = false;
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
            OnPropertyChanged(nameof(TotalAmount));
        }

        [ObservableProperty]
        private decimal unitPrice;

        [ObservableProperty]
        private decimal totalPrice;

        [ObservableProperty]
        private string originalUoM = string.Empty;

        public double TotalAmount => ApprovedQuantity * Quantity; // Total = Unit Amount × Quantity

        public List<string> AvailableUoMs { get; set; } = new() { "pcs", "kg", "g", "L", "ml" };
    }
}

