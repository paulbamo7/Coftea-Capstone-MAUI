using System.Globalization;
using System.Text.RegularExpressions;
using Coftea_Capstone.Models.Service;

namespace Coftea_Capstone.Views.Controls;

public partial class UpdateInventoryDetails : ContentView
{
    private double _currentStockValue = 0.0;
    private string _currentStockUnit = "g";

    public UpdateInventoryDetails()
    {
        InitializeComponent();

        // Initialize defaults
        if (StockUnitPicker != null)
        {
            StockUnitPicker.SelectedIndex = 0; // default to "g"
        }
        if (AddUnitPicker != null)
        {
            AddUnitPicker.SelectedIndex = 0; // default to match grams
        }
        UpdateTotalDisplay();
    }

    private void OnStockQuantityChanged(object sender, EventArgs e)
    {
        _currentStockValue = ParseDoubleOrZero(StockQuantityEntry?.Text);
        // Clear the add field when current stock changes to prevent confusion
        if (AddStockEntry != null)
        {
            AddStockEntry.Text = string.Empty;
        }
        UpdateTotalDisplay();
    }

    private void OnStockUnitChanged(object sender, EventArgs e)
    {
        var selected = StockUnitPicker?.SelectedItem as string;
        if (!string.IsNullOrWhiteSpace(selected))
        {
            _currentStockUnit = UnitConversionService.Normalize(selected);
        }
        UpdateTotalDisplay();
    }

    // Removed: Add unit no longer changes stock unit; it mirrors SelectedUoM from VM via binding

    private async void OnApplyAddStockClicked(object sender, EventArgs e)
    {
        var (addValue, addUnit, ok) = TryParseQuantityWithUnit(AddStockEntry?.Text);
        // If unit omitted in text, fall back to selected AddUnitPicker
        if (ok && string.IsNullOrWhiteSpace(addUnit) && AddUnitPicker?.SelectedItem is string selUnit && !string.IsNullOrWhiteSpace(selUnit))
        {
            addUnit = UnitConversionService.Normalize(selUnit);
        }
        if (!ok)
        {
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert("Invalid Input", "Please enter a value with unit, e.g. '1kg' or '500 g'", "OK");
            }
            return;
        }

        if (!UnitConversionService.AreCompatibleUnits(addUnit, _currentStockUnit))
        {
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert("Unit Mismatch", $"Cannot convert from {addUnit} to {_currentStockUnit}.", "OK");
            }
            return;
        }

        // If current unit is a base unit (g/L) and user adds a compatible different unit, switch stock unit to the added unit
        bool isBaseMass = string.Equals(_currentStockUnit, "g", StringComparison.OrdinalIgnoreCase);
        bool isBaseVol = string.Equals(_currentStockUnit, "l", StringComparison.OrdinalIgnoreCase);
        if ((isBaseMass || isBaseVol) && !string.Equals(_currentStockUnit, addUnit, StringComparison.OrdinalIgnoreCase))
        {
            // Convert existing stock to the add unit and switch the stock unit to match the addition
            _currentStockValue = UnitConversionService.Convert(_currentStockValue, _currentStockUnit, addUnit);
            _currentStockUnit = addUnit;
            // Try to update the stock unit picker display to match
            var display = FormatUnitDisplay(addUnit);
            if (StockUnitPicker != null && !string.IsNullOrWhiteSpace(display))
            {
                StockUnitPicker.SelectedItem = display;
            }
        }

        var convertedAdd = UnitConversionService.Convert(addValue, addUnit, _currentStockUnit);
        _currentStockValue += convertedAdd;
        StockQuantityEntry.Text = _currentStockValue.ToString(CultureInfo.InvariantCulture);
        AddStockEntry.Text = string.Empty;
        // Update display and include the converted addition amount
        var (displayValue, displayUnit) = UnitConversionService.ConvertToBestUnit(_currentStockValue, _currentStockUnit);
        if (ResultLabel != null)
        {
            ResultLabel.Text = $"Total: {displayValue:0.###} {displayUnit} (added {convertedAdd:0.###} {_currentStockUnit})";
        }
    }

    private void UpdateTotalDisplay()
    {
        // Parse add input like "1kg" or "1000 g"
        var (addValue, addUnit, ok) = TryParseQuantityWithUnit(AddStockEntry?.Text);
        // If unit omitted in text, fall back to selected AddUnitPicker
        if (ok && string.IsNullOrWhiteSpace(addUnit) && AddUnitPicker?.SelectedItem is string selUnit && !string.IsNullOrWhiteSpace(selUnit))
        {
            addUnit = UnitConversionService.Normalize(selUnit);
        }

        double totalInStockUnit = _currentStockValue;

        if (ok)
        {
            // convert add value to stock unit if compatible
            if (UnitConversionService.AreCompatibleUnits(addUnit, _currentStockUnit))
            {
                var convertedAdd = UnitConversionService.Convert(addValue, addUnit, _currentStockUnit);
                totalInStockUnit += convertedAdd;
            }
        }

        // Convert to best display unit for readability
        var (displayValue, displayUnit) = UnitConversionService.ConvertToBestUnit(totalInStockUnit, _currentStockUnit);
        if (ResultLabel != null)
        {
            ResultLabel.Text = $"Total: {displayValue:0.###} {displayUnit}";
        }
    }

    private static double ParseDoubleOrZero(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0.0;
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) return v;
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out v)) return v;
        return 0.0;
    }

    private static (double value, string unit, bool ok) TryParseQuantityWithUnit(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return (0, "", false);

        var trimmed = input.Trim();
        // First try: number + unit (allows optional space). Examples: "1kg", "1000 g", "1.5 L"
        var withUnit = Regex.Match(trimmed, @"^\s*([0-9]+(?:[\.,][0-9]+)?)\s*([a-zA-Z]+)\s*$");
        if (withUnit.Success)
        {
            var numStr = withUnit.Groups[1].Value.Replace(',', '.');
            var unitRaw = withUnit.Groups[2].Value;
            if (!double.TryParse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var valueWU))
                return (0, "", false);
            var unitWU = UnitConversionService.Normalize(unitRaw);
            return (valueWU, unitWU, !string.IsNullOrWhiteSpace(unitWU));
        }

        // Second try: number only; caller will provide unit via picker
        var numberOnly = Regex.Match(trimmed, @"^\s*([0-9]+(?:[\.,][0-9]+)?)\s*$");
        if (numberOnly.Success)
        {
            var numStr = numberOnly.Groups[1].Value.Replace(',', '.');
            if (double.TryParse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var valueNO))
                return (valueNO, "", true);
        }

        return (0, "", false);
    }

    private static string FormatUnitDisplay(string normalized)
    {
        var u = UnitConversionService.Normalize(normalized);
        return u switch
        {
            "kg" => "Kilograms (kg)",
            "g" => "Grams (g)",
            "l" => "Liters (L)",
            "ml" => "Milliliters (ml)",
            "pcs" => "Pieces (pcs)",
            _ => normalized
        };
    }
}