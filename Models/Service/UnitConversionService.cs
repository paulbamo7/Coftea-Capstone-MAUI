using System;
using System.Collections.Generic;

namespace Coftea_Capstone.Models.Service
{
    public static class UnitConversionService
    {
        // Conversion factors for different units
        private static readonly Dictionary<string, Dictionary<string, double>> ConversionFactors = new()
        {
            // Mass conversions (to grams as base unit)
            ["kg"] = new Dictionary<string, double> { ["g"] = 1000.0, ["kg"] = 1.0 },
            ["g"] = new Dictionary<string, double> { ["kg"] = 0.001, ["g"] = 1.0 },
            
            // Volume conversions (to milliliters as base unit)
            ["L"] = new Dictionary<string, double> { ["ml"] = 1000.0, ["L"] = 1.0 },
            ["ml"] = new Dictionary<string, double> { ["L"] = 0.001, ["ml"] = 1.0 },
            
            // Pieces (no conversion needed)
            ["pcs"] = new Dictionary<string, double> { ["pcs"] = 1.0 }
        };

        // Convert value from sourceUnit to targetUnit
        public static double Convert(double value, string sourceUnit, string targetUnit)
        {
            var fromU = Normalize(sourceUnit);
            var toU = Normalize(targetUnit);

            if (string.IsNullOrWhiteSpace(fromU) || string.IsNullOrWhiteSpace(toU) || fromU == toU)
                return value;

            // Check if both units are in the same category
            if (!AreCompatibleUnits(fromU, toU))
                return value; // Can't convert between different unit types

            // Get conversion factor
            var factor = GetConversionFactor(fromU, toU);
            return value * factor;
        }

        // Check if two units are compatible for conversion
        public static bool AreCompatibleUnits(string unit1, string unit2)
        {
            var u1 = Normalize(unit1);
            var u2 = Normalize(unit2);

            // Same unit
            if (u1 == u2) return true;

            // Check if both are mass units
            if (IsMassUnit(u1) && IsMassUnit(u2)) return true;

            // Check if both are volume units
            if (IsVolumeUnit(u1) && IsVolumeUnit(u2)) return true;

            // Check if both are pieces
            if (IsPiecesUnit(u1) && IsPiecesUnit(u2)) return true;

            return false;
        }

        // Get the best unit for display (prefer smaller units for better precision)
        public static string GetBestDisplayUnit(string unit, double value)
        {
            var normalizedUnit = Normalize(unit);
            
            // For mass: prefer grams if value is less than 1 kg
            if (IsMassUnit(normalizedUnit))
            {
                return value < 1.0 ? "g" : "kg";
            }
            
            // For volume: prefer milliliters if value is less than 1 L
            if (IsVolumeUnit(normalizedUnit))
            {
                return value < 1.0 ? "ml" : "L";
            }
            
            // For pieces, keep as is
            return normalizedUnit;
        }

        // Convert to the best display unit
        public static (double value, string unit) ConvertToBestUnit(double value, string unit)
        {
            var bestUnit = GetBestDisplayUnit(unit, value);
            var convertedValue = Convert(value, unit, bestUnit);
            return (convertedValue, bestUnit);
        }

        // Get conversion factor between two units
        private static double GetConversionFactor(string fromUnit, string toUnit)
        {
            if (ConversionFactors.TryGetValue(fromUnit, out var factors) && 
                factors.TryGetValue(toUnit, out var factor))
            {
                return factor;
            }
            return 1.0; // No conversion available
        }

        // Check if unit is a mass unit
        private static bool IsMassUnit(string unit)
        {
            return unit == "kg" || unit == "g";
        }

        // Check if unit is a volume unit
        private static bool IsVolumeUnit(string unit)
        {
            return unit == "L" || unit == "ml";
        }

        // Check if unit is pieces
        private static bool IsPiecesUnit(string unit)
        {
            return unit == "pcs";
        }

        public static string Normalize(string unit)
        {
            var u = (unit ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(u)) return string.Empty;
            
            var lower = u.ToLowerInvariant();
            
            // Mass units
            if (lower == "kg" || lower.Contains("kilogram")) return "kg";
            if (lower == "g" || lower.Contains("gram")) return "g";
            
            // Volume units
            if (lower == "l" || lower == "liter" || lower == "litre") return "L";
            if (lower == "ml" || lower.Contains("milliliter") || lower.Contains("millilitre")) return "ml";
            
            // Pieces
            if (lower.Contains("pcs") || lower.Contains("piece")) return "pcs";
            
            return u;
        }

        // Get all available units for a given category
        public static List<string> GetAvailableUnits(string category)
        {
            return category?.ToLowerInvariant() switch
            {
                "mass" or "weight" => new List<string> { "kg", "g" },
                "volume" or "liquid" => new List<string> { "L", "ml" },
                "pieces" or "count" => new List<string> { "pcs" },
                _ => new List<string> { "kg", "g", "L", "ml", "pcs" }
            };
        }

        // Format unit for display
        public static string FormatUnit(string unit)
        {
            return Normalize(unit) switch
            {
                "kg" => "Kilograms (kg)",
                "g" => "Grams (g)",
                "L" => "Liters (L)",
                "ml" => "Milliliters (ml)",
                "pcs" => "Pieces (pcs)",
                _ => unit
            };
        }
    }
}


