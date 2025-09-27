using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Controls;
using SQLite;
using Coftea_Capstone.Models.Service;

namespace Coftea_Capstone.Models
{
    public partial class InventoryPageModel : ObservableObject
    {
        public int itemID { get; set; }
        public string itemName { get; set; }
        public double itemQuantity { get; set; } 
        public string itemCategory { get; set; }
        public string ImageSet { get; set; }
        public string itemDescription { get; set; }
        public string unitOfMeasurement { get; set; }
        public double minimumQuantity { get; set; }
        public double maximumStockLevel { get; set; } // Track the maximum stock level ever reached
        [ObservableProperty]
        private bool isSelected;

        // POS selection inputs
        [ObservableProperty]
        private double inputAmount = 1;

        // Size-specific amounts
        [ObservableProperty]
        private double inputAmountSmall = 1;

        [ObservableProperty]
        private double inputAmountMedium = 1;

        [ObservableProperty]
        private double inputAmountLarge = 1;

        [ObservableProperty]
        private string inputUnit;

        // Size-specific editable units
        [ObservableProperty]
        private string inputUnitSmall;

        [ObservableProperty]
        private string inputUnitMedium;

        [ObservableProperty]
        private string inputUnitLarge;

        // Computed/assigned price used per size (for POS previews and cart)
        [ObservableProperty]
        private decimal priceUsedSmall;

        [ObservableProperty]
        private decimal priceUsedMedium;

        [ObservableProperty]
        private decimal priceUsedLarge;

        // Add-on price per serving (used when item acts as an addon)
        [ObservableProperty]
        private decimal addonPriceSmall;

        [ObservableProperty]
        private decimal addonPriceMedium;

        [ObservableProperty]
        private decimal addonPriceLarge;

        public IList<string> AllowedUnits
        {
            get
            {
                var unit = (unitOfMeasurement ?? string.Empty).Trim().ToLowerInvariant();
                return unit switch
                {
                    "kg" or "g" => new List<string> { "kg", "g" },
                    "l" or "ml" => new List<string> { "L", "ml" },
                    "pcs" => new List<string> { "pcs" },
                    _ => new List<string> { "pcs", "kg", "g", "L", "ml" }
                };
            }
        }

        // Whether this inventory item should use a unit of measurement (UoM) picker
        public bool HasUnit
        {
            get
            {
                var unit = NormalizeUnit(unitOfMeasurement).ToLowerInvariant();
                return unit == "kg" || unit == "g" || unit == "l" || unit == "ml";
            }
        }

        public bool IsQuantityType => !HasUnit;

        // Whether this item is a cup (automatically selected, not manually selectable)
        public bool IsCupItem
        {
            get
            {
                var name = (itemName ?? string.Empty).ToLowerInvariant();
                return name.Contains("cup") && (name.Contains("small") || name.Contains("medium") || name.Contains("large"));
            }
        }

        public string DefaultUnit
        {
            get
            {
                var unit = NormalizeUnit(unitOfMeasurement);
                if (string.IsNullOrWhiteSpace(unit)) return "pcs";
                return unit.Equals("kg", StringComparison.OrdinalIgnoreCase)
                    || unit.Equals("g", StringComparison.OrdinalIgnoreCase)
                    || unit.Equals("l", StringComparison.OrdinalIgnoreCase)
                    || unit.Equals("ml", StringComparison.OrdinalIgnoreCase)
                    ? unit
                    : "pcs";
            }
        }
        
        public ImageSource ImageSource =>
            string.IsNullOrWhiteSpace(ImageSet) ? "placeholder.png" : ImageSource.FromFile(ImageSet);

        // Computed properties for UI display
        public double StockProgress
        {
            get
            {
                if (minimumQuantity <= 0) return 1.0;
                var ratio = itemQuantity / minimumQuantity;
                if (ratio < 0) return 0;
                if (ratio > 1) return 1;
                return ratio;
            }
        }

        public string StockDisplay
        {
            get
            {
                var shortUnit = NormalizeUnit(unitOfMeasurement);
                return string.IsNullOrWhiteSpace(shortUnit)
                    ? $"{itemQuantity}"
                    : $"{itemQuantity} {shortUnit}";
            }
        }

        public string MinimumDisplay
        {
            get
            {
                if (minimumQuantity <= 0) return string.Empty;
                var shortUnit = NormalizeUnit(unitOfMeasurement);
                return string.IsNullOrWhiteSpace(shortUnit)
                    ? $"/ Min {minimumQuantity}"
                    : $"/ Min {minimumQuantity} {shortUnit}";
            }
        }

        // Progress bar properties for UI
        public double StockProgressWidth
        {
            get
            {
                // Use the dynamic maximum stock level, or current quantity if it's higher
                var maxStock = Math.Max(maximumStockLevel, itemQuantity);
                if (maxStock <= 0) return 0; // Avoid division by zero
                
                var progress = itemQuantity / maxStock;
                // When full (progress = 1), return full width (200)
                // When empty (progress = 0), return 0
                // Otherwise, return proportional width
                return progress * 200;
            }
        }

        public string StockText
        {
            get
            {
                // Show current stock vs dynamic maximum
                var maxStock = Math.Max(maximumStockLevel, itemQuantity);
                var shortUnit = NormalizeUnit(unitOfMeasurement);
                
                if (string.IsNullOrWhiteSpace(shortUnit))
                {
                    return $"{itemQuantity}/{maxStock}";
                }
                else
                {
                    return $"{itemQuantity} {shortUnit}/{maxStock} {shortUnit}";
                }
            }
        }

        // Method to update maximum stock level when adding new stock
        public void UpdateMaximumStockLevel(double newQuantity)
        {
            if (newQuantity > maximumStockLevel)
            {
                maximumStockLevel = newQuantity;
            }
        }

        // Normalize long-form units like "Pieces (pcs)" to short codes like "pcs"
        private static string NormalizeUnit(string raw)
        {
            var unit = (raw ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(unit)) return string.Empty;
            var lower = unit.ToLowerInvariant();
            if (lower.Contains("pcs")) return "pcs";
            if (lower.Contains("kilograms") || lower == "kg") return "kg";
            if (lower.Contains("grams") || lower == "g") return "g";
            if (lower.Contains("liters") || lower == "l") return "L";
            if (lower.Contains("milliliters") || lower == "ml") return "ml";
            return unit; // return as-is if already short or unknown
        }

        // Base-unit conversions: mass to grams, volume to milliliters
        public double InputAmountSmallInBase
        {
            get
            {
                var u = NormalizeUnit(InputUnitSmall ?? unitOfMeasurement);
                if (u == "kg") return UnitConversionService.Convert(InputAmountSmall, "kg", "g");
                if (u == "g") return InputAmountSmall;
                if (u == "L") return UnitConversionService.Convert(InputAmountSmall, "L", "ml");
                if (u == "ml") return InputAmountSmall;
                return InputAmountSmall; // pcs or unknown — return as-is
            }
        }

        public double InputAmountMediumInBase
        {
            get
            {
                var u = NormalizeUnit(InputUnitMedium ?? unitOfMeasurement);
                if (u == "kg") return UnitConversionService.Convert(InputAmountMedium, "kg", "g");
                if (u == "g") return InputAmountMedium;
                if (u == "L") return UnitConversionService.Convert(InputAmountMedium, "L", "ml");
                if (u == "ml") return InputAmountMedium;
                return InputAmountMedium;
            }
        }

        public double InputAmountLargeInBase
        {
            get
            {
                var u = NormalizeUnit(InputUnitLarge ?? unitOfMeasurement);
                if (u == "kg") return UnitConversionService.Convert(InputAmountLarge, "kg", "g");
                if (u == "g") return InputAmountLarge;
                if (u == "L") return UnitConversionService.Convert(InputAmountLarge, "L", "ml");
                if (u == "ml") return InputAmountLarge;
                return InputAmountLarge;
            }
        }
    }
}
