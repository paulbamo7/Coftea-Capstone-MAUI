using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Controls;
using SQLite;

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
        [ObservableProperty]
        private bool isSelected;

        // POS selection inputs
        [ObservableProperty]
        private double inputAmount = 1;

        [ObservableProperty]
        private string inputUnit;

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
            => string.IsNullOrWhiteSpace(unitOfMeasurement)
                ? $"{itemQuantity}"
                : $"{itemQuantity} {unitOfMeasurement}";

        public string MinimumDisplay
            => minimumQuantity > 0
                ? (string.IsNullOrWhiteSpace(unitOfMeasurement)
                    ? $"/ Min {minimumQuantity}"
                    : $"/ Min {minimumQuantity} {unitOfMeasurement}")
                : string.Empty;
    }
}
