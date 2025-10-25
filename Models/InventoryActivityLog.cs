using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Coftea_Capstone.Models
{
    public partial class InventoryActivityLog : ObservableObject
    {
        public int LogId { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public string ItemCategory { get; set; }
        public string Action { get; set; } // "DEDUCTED", "ADDED", "UPDATED", "PURCHASE_ORDER"
        public double QuantityChanged { get; set; }
        public double PreviousQuantity { get; set; }
        public double NewQuantity { get; set; }
        public string UnitOfMeasurement { get; set; }
        public string Reason { get; set; } // "POS_ORDER", "MANUAL_ADJUSTMENT", "PURCHASE_ORDER", "WASTAGE", etc.
        public string UserEmail { get; set; }
        public string OrderId { get; set; } // Reference to POS order or purchase order
        public DateTime Timestamp { get; set; }
        public string Notes { get; set; }

        // Display properties for UI
        public string FormattedTimestamp => Timestamp.ToString("MMM dd, yyyy HH:mm");
        
        public string ActionDisplay
        {
            get
            {
                return Action switch
                {
                    "DEDUCTED" => "Deducted",
                    "ADDED" => "Added",
                    "UPDATED" => "Updated",
                    "PURCHASE_ORDER" => "Purchase Order",
                    _ => Action
                };
            }
        }

        public string QuantityChangeDisplay
        {
            get
            {
                var unit = string.IsNullOrWhiteSpace(UnitOfMeasurement) ? "" : $" {UnitOfMeasurement}";
                var sign = QuantityChanged >= 0 ? "+" : "";
                return $"{sign}{QuantityChanged}{unit}";
            }
        }

        public string StockLevelDisplay
        {
            get
            {
                var unit = string.IsNullOrWhiteSpace(UnitOfMeasurement) ? "" : $" {UnitOfMeasurement}";
                return $"{PreviousQuantity}{unit} → {NewQuantity}{unit}";
            }
        }

        public string ReasonDisplay
        {
            get
            {
                return Reason switch
                {
                    "POS_ORDER" => "POS Order",
                    "MANUAL_ADJUSTMENT" => "Manual Adjustment",
                    "PURCHASE_ORDER" => "Purchase Order",
                    "WASTAGE" => "Wastage",
                    "RETURN" => "Return",
                    _ => Reason
                };
            }
        }

        public string ColorCode
        {
            get
            {
                return Action switch
                {
                    "DEDUCTED" => "#FF5722", // Orange/Red for deductions
                    "ADDED" => "#4CAF50", // Green for additions
                    "UPDATED" => "#2196F3", // Blue for updates
                    "PURCHASE_ORDER" => "#9C27B0", // Purple for purchase orders
                    _ => "#757575" // Gray for unknown
                };
            }
        }

        public string ActionIcon
        {
            get
            {
                return Action switch
                {
                    "DEDUCTED" => "➖",
                    "ADDED" => "➕",
                    "UPDATED" => "✏️",
                    "PURCHASE_ORDER" => "📦",
                    _ => "📋"
                };
            }
        }
    }
}
