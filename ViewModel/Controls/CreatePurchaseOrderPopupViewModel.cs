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

        public bool IsAdmin { get; set; }

        public CreatePurchaseOrderPopupViewModel()
        {
        }

        public void LoadItems(System.Collections.Generic.List<InventoryPageModel> lowStockItems, bool isAdmin)
        {
            IsAdmin = isAdmin;
            EditableItems.Clear();

            foreach (var item in lowStockItems)
            {
                var neededQuantity = (int)(item.minimumQuantity - item.itemQuantity);
                var editableItem = new CreateOrderEditableItem
                {
                    InventoryItemId = item.itemID,
                    ItemName = item.itemName,
                    ItemCategory = item.itemCategory ?? "",
                    RequestedQuantity = neededQuantity,
                    ApprovedQuantity = neededQuantity, // Default to needed quantity
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

                        var approved = await _database.UpdatePurchaseOrderStatusAsync(
                            purchaseOrderId,
                            "Approved",
                            currentUser,
                            new { Items = customItemsList });

                        if (approved)
                        {
                            // Send SMS to supplier
                            var smsSent = await PurchaseOrderSMSService.SendPurchaseOrderToSupplierAsync(purchaseOrderId, itemsForDb);

                            await Application.Current.MainPage.DisplayAlert(
                                "Purchase Order Created & Approved",
                                $"Purchase order #{purchaseOrderId} has been created and auto-approved with your custom amounts!\n\n" +
                                $"📱 SMS app should have opened.\n" +
                                $"Please press 'Send' to notify the supplier.\n\n" +
                                $"Inventory has been updated.",
                                "OK");

                            IsVisible = false;
                            System.Diagnostics.Debug.WriteLine($"✅ Purchase order {purchaseOrderId} auto-approved by admin");
                        }
                        else
                        {
                            await Application.Current.MainPage.DisplayAlert(
                                "Purchase Order Creation Error",
                                $"Purchase order #{purchaseOrderId} was created but could not be auto-approved.\n\n" +
                                $"Please check the purchase order in the system and approve it manually.",
                                "OK");
                        }
                    }
                    else
                    {
                        // Regular user - requires admin approval
                        System.Diagnostics.Debug.WriteLine("👤 Regular user detected - purchase order requires admin approval");

                        // Send SMS to supplier
                        var smsSent = await PurchaseOrderSMSService.SendPurchaseOrderToSupplierAsync(purchaseOrderId, itemsForDb);

                        // Notify admin via SMS
                        var adminNotified = await PurchaseOrderSMSService.NotifyAdminOfPurchaseOrderAsync(purchaseOrderId, currentUser);

                        await Application.Current.MainPage.DisplayAlert(
                            "Purchase Order Created",
                            $"Purchase order #{purchaseOrderId} has been created with your custom amounts!\n\n" +
                            $"📱 SMS app should have opened.\n" +
                            $"Please press 'Send' to notify the supplier and admin.",
                            "OK");

                        IsVisible = false;
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
        private void Close()
        {
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
    }
}

