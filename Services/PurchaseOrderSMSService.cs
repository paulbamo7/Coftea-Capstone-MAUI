using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Coftea_Capstone.Models;

namespace Coftea_Capstone.Services
{
    public static class PurchaseOrderSMSService
    {
        private static readonly string SUPPLIER_PHONE = "+1234567890"; // Coftea Supplier phone number
        private static readonly string ADMIN_PHONE = "+0987654321"; // Admin phone number for notifications

        /// <summary>
        /// Sends SMS to supplier with purchase order details
        /// </summary>
        public static async Task<bool> SendPurchaseOrderToSupplierAsync(int purchaseOrderId, List<InventoryPageModel> items)
        {
            try
            {
                var message = BuildPurchaseOrderMessage(purchaseOrderId, items);
                
                // In a real implementation, you would use an SMS service like Twilio, AWS SNS, etc.
                // For now, we'll simulate the SMS sending
                System.Diagnostics.Debug.WriteLine($"📱 SMS to Supplier ({SUPPLIER_PHONE}):");
                System.Diagnostics.Debug.WriteLine($"📝 Message: {message}");
                
                // Simulate SMS sending delay
                await Task.Delay(1000);
                
                System.Diagnostics.Debug.WriteLine("✅ SMS sent to supplier successfully");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error sending SMS to supplier: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sends SMS notification to admin about new purchase order
        /// </summary>
        public static async Task<bool> NotifyAdminOfPurchaseOrderAsync(int purchaseOrderId, string requestedBy)
        {
            try
            {
                var message = $"New Purchase Order #{purchaseOrderId} created by {requestedBy}. Please review and approve.";
                
                System.Diagnostics.Debug.WriteLine($"📱 SMS to Admin ({ADMIN_PHONE}):");
                System.Diagnostics.Debug.WriteLine($"📝 Message: {message}");
                
                // Simulate SMS sending delay
                await Task.Delay(1000);
                
                System.Diagnostics.Debug.WriteLine("✅ SMS sent to admin successfully");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error sending SMS to admin: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sends SMS to supplier when purchase order is approved
        /// </summary>
        public static async Task<bool> NotifySupplierOfApprovalAsync(int purchaseOrderId, List<PurchaseOrderItemModel> items)
        {
            try
            {
                var message = BuildApprovalMessage(purchaseOrderId, items);
                
                System.Diagnostics.Debug.WriteLine($"📱 SMS to Supplier ({SUPPLIER_PHONE}):");
                System.Diagnostics.Debug.WriteLine($"📝 Message: {message}");
                
                // Simulate SMS sending delay
                await Task.Delay(1000);
                
                System.Diagnostics.Debug.WriteLine("✅ Approval SMS sent to supplier successfully");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error sending approval SMS to supplier: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sends SMS to supplier when purchase order is rejected
        /// </summary>
        public static async Task<bool> NotifySupplierOfRejectionAsync(int purchaseOrderId, string reason = "")
        {
            try
            {
                var message = $"Purchase Order #{purchaseOrderId} has been rejected. Reason: {reason}";
                
                System.Diagnostics.Debug.WriteLine($"📱 SMS to Supplier ({SUPPLIER_PHONE}):");
                System.Diagnostics.Debug.WriteLine($"📝 Message: {message}");
                
                // Simulate SMS sending delay
                await Task.Delay(1000);
                
                System.Diagnostics.Debug.WriteLine("✅ Rejection SMS sent to supplier successfully");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error sending rejection SMS to supplier: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Builds the purchase order message for supplier
        /// </summary>
        private static string BuildPurchaseOrderMessage(int purchaseOrderId, List<InventoryPageModel> items)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"COFTEA PURCHASE ORDER #{purchaseOrderId}");
            sb.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm}");
            sb.AppendLine($"Supplier: Coftea Supplier");
            sb.AppendLine();
            sb.AppendLine("ITEMS NEEDED:");
            
            foreach (var item in items)
            {
                var neededQuantity = item.minimumQuantity - item.itemQuantity;
                sb.AppendLine($"• {item.itemName} - {neededQuantity} {item.unitOfMeasurement}");
            }
            
            sb.AppendLine();
            sb.AppendLine("Please confirm delivery date and pricing.");
            sb.AppendLine("Reply with 'CONFIRM' to accept this order.");
            
            return sb.ToString();
        }

        /// <summary>
        /// Builds the approval message for supplier
        /// </summary>
        private static string BuildApprovalMessage(int purchaseOrderId, List<PurchaseOrderItemModel> items)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"COFTEA PURCHASE ORDER #{purchaseOrderId} - APPROVED");
            sb.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm}");
            sb.AppendLine();
            sb.AppendLine("APPROVED ITEMS:");
            
            foreach (var item in items)
            {
                sb.AppendLine($"• {item.ItemName} - {item.RequestedQuantity} {item.UnitOfMeasurement} @ ${item.UnitPrice:F2}");
            }
            
            var totalAmount = items.Sum(i => i.TotalPrice);
            sb.AppendLine();
            sb.AppendLine($"TOTAL AMOUNT: ${totalAmount:F2}");
            sb.AppendLine();
            sb.AppendLine("Please proceed with delivery.");
            sb.AppendLine("Reply with 'DELIVERED' when items are delivered.");
            
            return sb.ToString();
        }

        /// <summary>
        /// Simulates receiving SMS response from supplier
        /// </summary>
        public static async Task<string> SimulateSupplierResponseAsync(int purchaseOrderId, string action)
        {
            try
            {
                // Simulate network delay
                await Task.Delay(2000);
                
                var response = action switch
                {
                    "CONFIRM" => $"Purchase Order #{purchaseOrderId} confirmed by supplier. Delivery expected within 2-3 business days.",
                    "DELIVERED" => $"Purchase Order #{purchaseOrderId} delivered by supplier. Inventory will be updated.",
                    _ => $"Received response for Purchase Order #{purchaseOrderId}: {action}"
                };
                
                System.Diagnostics.Debug.WriteLine($"📱 SMS from Supplier: {response}");
                return response;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error simulating supplier response: {ex.Message}");
                return "Error processing supplier response";
            }
        }
    }
}
