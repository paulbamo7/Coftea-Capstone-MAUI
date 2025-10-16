﻿using System;
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
        public InventoryPageModel()
        {
            // Initialize commands used by POS addons UI
            ToggleSelectedCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() =>
            {
                IsSelected = !IsSelected;
                if (IsSelected && AddonQuantity < 1)
                {
                    AddonQuantity = 1;
                }
                else if (!IsSelected)
                {
                    AddonQuantity = 0;
                }
            });

            IncreaseAddonQtyCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() =>
            {
                if (AddonQuantity < 1)
                {
                    AddonQuantity = 1;
                }
                else
                {
                    AddonQuantity += 1;
                }
                IsSelected = true;
            });

            DecreaseAddonQtyCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() =>
            {
                var next = AddonQuantity - 1;
                if (next <= 0)
                {
                    AddonQuantity = 0;
                    IsSelected = false;
                }
                else
                {
                    AddonQuantity = next;
                }
            });
        }

        public int itemID { get; set; }
        public string itemName { get; set; }
        private double _itemQuantity;
        public double itemQuantity
        {
            get { return _itemQuantity; }
            set
            {
                if (SetProperty(ref _itemQuantity, value))
                {
                    OnPropertyChanged(nameof(StockProgress));
                    OnPropertyChanged(nameof(StockFillColor));
                    OnPropertyChanged(nameof(StockDisplay));
                    OnPropertyChanged(nameof(StockText));
                    OnPropertyChanged(nameof(StockProgressWidth));
                    OnPropertyChanged(nameof(NewStockDisplay));
                    OnPropertyChanged(nameof(NewStockDisplayFormatted));
                    OnPropertyChanged(nameof(CurrentStockDisplay));
                }
            }
        }
        public string itemCategory { get; set; }
        public string ImageSet { get; set; }
        public string itemDescription { get; set; }
        public string unitOfMeasurement { get; set; }
        private double _minimumQuantity;
        public double minimumQuantity
        {
            get { return _minimumQuantity; }
            set
            {
                if (SetProperty(ref _minimumQuantity, value))
                {
                    OnPropertyChanged(nameof(StockProgress));
                    OnPropertyChanged(nameof(StockFillColor));
                    OnPropertyChanged(nameof(MinimumDisplay));
                    OnPropertyChanged(nameof(StockProgressWidth));
                    OnPropertyChanged(nameof(NewStockDisplay));
                    OnPropertyChanged(nameof(NewStockDisplayFormatted));
                    OnPropertyChanged(nameof(MinimumStockDisplay));
                }
            }
        }

        private double _maximumQuantity;
        public double maximumQuantity
        {
            get { return _maximumQuantity; }
            set
            {
                if (SetProperty(ref _maximumQuantity, value))
                {
                    OnPropertyChanged(nameof(NewStockDisplay));
                    OnPropertyChanged(nameof(NewStockDisplayFormatted));
                    OnPropertyChanged(nameof(MaximumStockDisplay));
                }
            }
        }
        [ObservableProperty]
        private bool isSelected;

        // POS selection inputs
        [ObservableProperty]
        private double inputAmount = 1;

        // MVVM-friendly text proxy for amount entry
        public string InputAmountText
        {
            get => InputAmount.ToString(System.Globalization.CultureInfo.InvariantCulture);
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    InputAmount = 0;
                    return;
                }
                if (double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v)
                    || double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.CurrentCulture, out v))
                {
                    InputAmount = v;
                }
            }
        }

        partial void OnInputAmountChanged(double value)
        {
            // Persist the edited amount into the currently selected size slot
            switch (SelectedSize)
            {
                case "Small":
                    InputAmountSmall = value;
                    break;
                case "Medium":
                    InputAmountMedium = value;
                    break;
                case "Large":
                    InputAmountLarge = value;
                    break;
            }
            // Trigger recalculation of total deduction
            OnPropertyChanged(nameof(TotalDeduction));
            // Notify bound text proxy so Entry updates immediately
            OnPropertyChanged(nameof(InputAmountText));
        }

        // Size-specific amounts
        [ObservableProperty]
        private double inputAmountSmall = 1;

        [ObservableProperty]
        private double inputAmountMedium = 1;

        [ObservableProperty]
        private double inputAmountLarge = 1;

        [ObservableProperty]
        private string inputUnit;

        // Size selection
        [ObservableProperty]
        private string selectedSize = "Small";

        partial void OnSelectedSizeChanged(string value)
        {
            // When size changes, surface that size's amount and unit into the shared fields bound by the UI
            switch (value)
            {
                case "Small":
                    InputAmount = InputAmountSmall;
                    InputUnit = string.IsNullOrWhiteSpace(InputUnitSmall) ? unitOfMeasurement : InputUnitSmall;
                    break;
                case "Medium":
                    InputAmount = InputAmountMedium;
                    InputUnit = string.IsNullOrWhiteSpace(InputUnitMedium) ? unitOfMeasurement : InputUnitMedium;
                    break;
                case "Large":
                    InputAmount = InputAmountLarge;
                    InputUnit = string.IsNullOrWhiteSpace(InputUnitLarge) ? unitOfMeasurement : InputUnitLarge;
                    break;
            }
            // Ensure text proxy refreshes to reflect the newly selected size's amount
            OnPropertyChanged(nameof(InputAmountText));
        }

        // Calculated total deduction - now simply equals the input amount since it's always 1 serving
        public double TotalDeduction
        {
            get
            {
                return InputAmount;
            }
        }

        // Size-specific editable units
        [ObservableProperty]
        private string inputUnitSmall;

        [ObservableProperty]
        private string inputUnitMedium;

        [ObservableProperty]
        private string inputUnitLarge;

        partial void OnInputUnitChanged(string value)
        {
            // Persist the edited unit into the currently selected size slot only
            switch (SelectedSize)
            {
                case "Small":
                    InputUnitSmall = value;
                    break;
                case "Medium":
                    InputUnitMedium = value;
                    break;
                case "Large":
                    InputUnitLarge = value;
                    break;
            }
        }

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

        // Addon properties for preview
        [ObservableProperty]
        private decimal addonPrice = 5.00m; // Default addon price

        partial void OnAddonPriceChanged(decimal value)
        {
            OnPropertyChanged(nameof(AddonTotalPrice));
        }

        [ObservableProperty]
        private string addonUnit;

        // Quantity for addon selections in preview/cart
        [ObservableProperty]
        private int addonQuantity = 0;

        partial void OnAddonQuantityChanged(int value)
        {
            OnPropertyChanged(nameof(AddonTotalPrice));
        }

        partial void OnIsSelectedChanged(bool value)
        {
            if (!value)
            {
                // When unchecked, reset quantity to 0
                if (AddonQuantity != 0)
                {
                    AddonQuantity = 0;
                }
            }
            else
            {
                // When checked, ensure at least quantity 1
                if (AddonQuantity < 1)
                {
                    AddonQuantity = 1;
                }
            }
            OnPropertyChanged(nameof(AddonTotalPrice));
        }

        // Subtotal used for display in POS: base price when unselected, subtotal when selected
        public decimal AddonTotalPrice 
        { 
            get 
            {
                var result = (IsSelected && AddonQuantity > 0) ? (AddonPrice * AddonQuantity) : AddonPrice;
                System.Diagnostics.Debug.WriteLine($"💰 AddonTotalPrice for {itemName}: IsSelected={IsSelected}, AddonQuantity={AddonQuantity}, AddonPrice={AddonPrice}, Result={result}");
                return result;
            }
        }

        // Commands for POS addons interaction
        public CommunityToolkit.Mvvm.Input.IRelayCommand ToggleSelectedCommand { get; }
        public CommunityToolkit.Mvvm.Input.IRelayCommand IncreaseAddonQtyCommand { get; }
        public CommunityToolkit.Mvvm.Input.IRelayCommand DecreaseAddonQtyCommand { get; }

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

        public string DefaultUnit
        {
            get
            {
                var unit = NormalizeUnit(unitOfMeasurement);
                if (string.IsNullOrWhiteSpace(unit)) 
                {
                    // Set default unit based on category
                    return GetDefaultUnitForCategory(itemCategory);
                }
                return unit.Equals("kg", StringComparison.OrdinalIgnoreCase)
                    || unit.Equals("g", StringComparison.OrdinalIgnoreCase)
                    || unit.Equals("l", StringComparison.OrdinalIgnoreCase)
                    || unit.Equals("ml", StringComparison.OrdinalIgnoreCase)
                    ? unit
                    : GetDefaultUnitForCategory(itemCategory);
            }
        }

        private string GetDefaultUnitForCategory(string category)
        {
            return category?.ToLowerInvariant() switch
            {
                "fruit/soda" => "kg",
                "coffee" => "kg",
                "milktea" => "kg",
                "frappe" => "kg",
                "liquid" => "L",
                "supplies" => "pcs",
                _ => "pcs"
            };
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

        // Progress bar color: green when at/above minimum, red when below
        public string StockFillColor
        {
            get
            {
                if (minimumQuantity <= 0) return "#2E7D32"; // default green when no minimum
                return itemQuantity < minimumQuantity ? "#C62828" : "#2E7D32";
            }
        }

        // Width for progress bar (0-200 range for visual display)
        public double StockProgressWidth
        {
            get
            {
                if (minimumQuantity <= 0) return 200; // full width when no minimum
                var ratio = itemQuantity / minimumQuantity;
                if (ratio < 0) return 0;
                if (ratio > 2) return 200; // cap at 200 for very high stock
                return ratio * 100; // scale to 0-200 range
            }
        }

        // Text display for stock progress bar
        public string StockText
        {
            get
            {
                return StockDisplay;
            }
        }

        // New format: Current Stock 300 ml | Maximum Stock: 2kg | Minimum Stock: 500 g
        public string NewStockDisplay
        {
            get
            {
                var shortUnit = NormalizeUnit(unitOfMeasurement);
                var currentStock = string.IsNullOrWhiteSpace(shortUnit) 
                    ? $"{itemQuantity}" 
                    : $"{itemQuantity} {shortUnit}";

                var maxStock = maximumQuantity > 0 
                    ? (string.IsNullOrWhiteSpace(shortUnit) 
                        ? $"{maximumQuantity}" 
                        : $"{maximumQuantity} {shortUnit}")
                    : "Not Set";

                var minStock = minimumQuantity > 0 
                    ? (string.IsNullOrWhiteSpace(shortUnit) 
                        ? $"{minimumQuantity}" 
                        : $"{minimumQuantity} {shortUnit}")
                    : "Not Set";

                return $"Current Stock: {currentStock} | Maximum Stock: {maxStock} | Minimum Stock: {minStock}";
            }
        }

        // Color-coded single line display using FormattedString
        public FormattedString NewStockDisplayFormatted
        {
            get
            {
                var shortUnit = NormalizeUnit(unitOfMeasurement);
                var currentStock = string.IsNullOrWhiteSpace(shortUnit) 
                    ? $"{itemQuantity}" 
                    : $"{itemQuantity} {shortUnit}";

                var maxStock = maximumQuantity > 0 
                    ? (string.IsNullOrWhiteSpace(shortUnit) 
                        ? $"{maximumQuantity}" 
                        : $"{maximumQuantity} {shortUnit}")
                    : "Not Set";

                var minStock = minimumQuantity > 0 
                    ? (string.IsNullOrWhiteSpace(shortUnit) 
                        ? $"{minimumQuantity}" 
                        : $"{minimumQuantity} {shortUnit}")
                    : "Not Set";

                var formattedString = new FormattedString();
                
                // Current Stock (Green)
                formattedString.Spans.Add(new Span { Text = "Current Stock: ", TextColor = Colors.Gray });
                formattedString.Spans.Add(new Span { Text = currentStock, TextColor = Colors.Green, FontAttributes = FontAttributes.Bold });
                formattedString.Spans.Add(new Span { Text = " | " });
                
                // Maximum Stock (Blue)
                formattedString.Spans.Add(new Span { Text = "Maximum Stock: ", TextColor = Colors.Gray });
                formattedString.Spans.Add(new Span { Text = maxStock, TextColor = Colors.Blue, FontAttributes = FontAttributes.Bold });
                formattedString.Spans.Add(new Span { Text = " | " });
                
                // Minimum Stock (Red)
                formattedString.Spans.Add(new Span { Text = "Minimum Stock: ", TextColor = Colors.Gray });
                formattedString.Spans.Add(new Span { Text = minStock, TextColor = Colors.Red, FontAttributes = FontAttributes.Bold });

                return formattedString;
            }
        }

        // Color-coded stock display properties
        public string CurrentStockDisplay
        {
            get
            {
                var shortUnit = NormalizeUnit(unitOfMeasurement);
                return string.IsNullOrWhiteSpace(shortUnit) 
                    ? $"{itemQuantity}" 
                    : $"{itemQuantity} {shortUnit}";
            }
        }

        public string MaximumStockDisplay
        {
            get
            {
                if (maximumQuantity <= 0) return "Not Set";
                var shortUnit = NormalizeUnit(unitOfMeasurement);
                return string.IsNullOrWhiteSpace(shortUnit) 
                    ? $"{maximumQuantity}" 
                    : $"{maximumQuantity} {shortUnit}";
            }
        }

        public string MinimumStockDisplay
        {
            get
            {
                if (minimumQuantity <= 0) return "Not Set";
                var shortUnit = NormalizeUnit(unitOfMeasurement);
                return string.IsNullOrWhiteSpace(shortUnit) 
                    ? $"{minimumQuantity}" 
                    : $"{minimumQuantity} {shortUnit}";
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
