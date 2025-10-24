using System;
using System.ComponentModel.DataAnnotations;

namespace Coftea_Capstone.Models
{
    public class PurchaseOrderModel
    {
        [Key]
        public int PurchaseOrderId { get; set; }
        
        public DateTime OrderDate { get; set; }
        
        public string SupplierName { get; set; } = "Coftea Supplier";
        
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Completed
        
        public string RequestedBy { get; set; } = string.Empty; // User who created the order
        
        public string ApprovedBy { get; set; } = string.Empty; // Admin who approved/rejected
        
        public DateTime? ApprovedDate { get; set; }
        
        public string Notes { get; set; } = string.Empty;
        
        public decimal TotalAmount { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set; }
    }
    
    public class PurchaseOrderItemModel
    {
        [Key]
        public int PurchaseOrderItemId { get; set; }
        
        public int PurchaseOrderId { get; set; }
        
        public int InventoryItemId { get; set; }
        
        public string ItemName { get; set; } = string.Empty;
        
        public string ItemCategory { get; set; } = string.Empty;
        
        public int RequestedQuantity { get; set; }
        
        public int ApprovedQuantity { get; set; } = 0;
        
        public decimal UnitPrice { get; set; }
        
        public decimal TotalPrice { get; set; }
        
        public string UnitOfMeasurement { get; set; } = string.Empty;
        
        public string Notes { get; set; } = string.Empty;
    }
}
