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
        public string UserFullName { get; set; }
        public int? UserId { get; set; }
        public string ChangedBy { get; set; } // "USER", "SYSTEM", "POS", "IMPORT"
        public double? Cost { get; set; }
        public string OrderId { get; set; } // Reference to POS order or purchase order
        public string ProductName { get; set; } // POS product name that used this ingredient
        public string ProductSize { get; set; } // Product size (Small, Medium, Large)
        public DateTime Timestamp { get; set; }
        public string Notes { get; set; }

        // Display properties for UI
        public string FormattedTimestamp => Timestamp.ToString("MMM dd, yyyy HH:mm");
        
        // Table-specific display properties
        public int RowNumber { get; set; } // Set by ViewModel
        
        public string FormattedTimestampShort => Timestamp.ToString("yyyy-MM-dd HH:mm");
        
        public string ActionText
        {
            get
            {
                return Action switch
                {
                    "DEDUCTED" => "Reduced Stock",
                    "ADDED" => "Added Stock",
                    "UPDATED" => "Adjusted Stock",
                    "PURCHASE_ORDER" => "Purchase Order",
                    "RETRACTED" => "Retracted",
                    _ => Action
                };
            }
        }
        
        public string QuantityChangeText
        {
            get
            {
                if (Action == "DEDUCTED" || Action == "RETRACTED" || QuantityChanged < 0)
                    return $"-{Math.Abs(QuantityChanged)}";
                else if (Action == "ADDED" || QuantityChanged > 0)
                    return $"+{QuantityChanged}";
                else
                    return "—";
            }
        }
        
        public string PreviousQuantityText => PreviousQuantity.ToString("0.##");
        
        public string NewQuantityText => NewQuantity.ToString("0.##");
        
        public string RemarksText
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Reason))
                {
                    return Reason switch
                    {
                        "POS_ORDER" => $"Sold to customer",
                        "MANUAL_ADJUSTMENT" => "Manual correction",
                        "PURCHASE_ORDER" => "New delivery received",
                        "PURCHASE_ORDER_RETRACT" => "Purchase order retracted",
                        "WASTAGE" => "Wastage/Expired",
                        "RETURN" => "Customer return",
                        _ => Reason
                    };
                }
                if (!string.IsNullOrWhiteSpace(Notes))
                    return Notes;
                return "—";
            }
        }
        
        public string ReferenceId
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(OrderId))
                {
                    // Format reference ID based on action
                    return Action switch
                    {
                        "DEDUCTED" => $"SALE-{OrderId}",
                        "ADDED" => $"STOCKIN-{OrderId}",
                        "UPDATED" => $"ADJ-{OrderId}",
                        "PURCHASE_ORDER" => $"PO-{OrderId}",
                        "RETRACTED" => $"RETRACT-{OrderId}",
                        _ => OrderId
                    };
                }
                return "—";
            }
        }
        
        public string UsedForProductText
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ProductName))
                    return "—";
                
                // Include size if available
                if (!string.IsNullOrWhiteSpace(ProductSize))
                    return $"{ProductName} - {ProductSize}";
                
                return ProductName;
            }
        }
        
        public string RowBackgroundColor => RowNumber % 2 == 0 ? "#F9F9F9" : "#FFFFFF";
        
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
                    "RETRACTED" => "Retracted",
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
                    "RETRACTED" => "#FF9800", // Orange for retracted items
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
                    "RETRACTED" => "↩️",
                    _ => "📋"
                };
            }
        }

        public string UserDisplay
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(UserFullName))
                    return UserFullName;
                if (!string.IsNullOrWhiteSpace(UserEmail))
                    return UserEmail.Split('@')[0];
                return "Unknown";
            }
        }

        public string CostDisplay
        {
            get
            {
                if (Cost.HasValue && Cost.Value > 0)
                    return $"₱{Cost.Value:F2}";
                return "N/A";
            }
        }

        public string ChangedByDisplay
        {
            get
            {
                return ChangedBy switch
                {
                    "SYSTEM" => "🤖 System",
                    "POS" => "💰 POS",
                    "IMPORT" => "📥 Import",
                    _ => "👤 User"
                };
            }
        }
    }
}
