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

            System.Diagnostics.Debug.WriteLine($"🔧 UnitConversionService.AreCompatibleUnits: '{unit1}' -> '{u1}', '{unit2}' -> '{u2}'");

            // Same unit
            if (u1 == u2) 
            {
                System.Diagnostics.Debug.WriteLine($"✅ Same units: {u1}");
                return true;
            }

            // Check if both are mass units
            if (IsMassUnit(u1) && IsMassUnit(u2)) 
            {
                System.Diagnostics.Debug.WriteLine($"✅ Both mass units: {u1}, {u2}");
                return true;
            }

            // Check if both are volume units
            if (IsVolumeUnit(u1) && IsVolumeUnit(u2)) 
            {
                System.Diagnostics.Debug.WriteLine($"✅ Both volume units: {u1}, {u2}");
                return true;
            }

            // Check if both are pieces
            if (IsPiecesUnit(u1) && IsPiecesUnit(u2)) 
            {
                System.Diagnostics.Debug.WriteLine($"✅ Both pieces units: {u1}, {u2}");
                return true;
            }

            System.Diagnostics.Debug.WriteLine($"❌ Incompatible units: {u1}, {u2}");
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
            System.Diagnostics.Debug.WriteLine($"🔧 Normalize: '{unit}' -> '{lower}'");
            
            // Mass units
            if (lower == "kg" || lower.Contains("kilogram")) 
            {
                System.Diagnostics.Debug.WriteLine($"📏 Normalized to kg: '{unit}'");
                return "kg";
            }
            if (lower == "g" || lower.Contains("gram")) 
            {
                System.Diagnostics.Debug.WriteLine($"📏 Normalized to g: '{unit}'");
                return "g";
            }
            
            // Volume units
            if (lower == "l" || lower.Contains("liter") || lower.Contains("litre")) 
            {
                System.Diagnostics.Debug.WriteLine($"📏 Normalized to L: '{unit}'");
                return "L";
            }
            if (lower == "ml" || lower.Contains("milliliter") || lower.Contains("millilitre")) 
            {
                System.Diagnostics.Debug.WriteLine($"📏 Normalized to ml: '{unit}'");
                return "ml";
            }
            
            // Pieces
            if (lower.Contains("pcs") || lower.Contains("piece")) 
            {
                System.Diagnostics.Debug.WriteLine($"📏 Normalized to pcs: '{unit}'");
                return "pcs";
            }
            
            System.Diagnostics.Debug.WriteLine($"📏 No normalization applied: '{unit}'");
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

        // Get a common unit for two compatible units
        public static string GetCommonUnit(string unit1, string unit2)
        {
            var u1 = Normalize(unit1);
            var u2 = Normalize(unit2);

            // If units are the same, return that unit
            if (u1 == u2) return u1;

            // For mass units, prefer the smaller unit (g)
            if (IsMassUnit(u1) && IsMassUnit(u2))
            {
                return "g";
            }

            // For volume units, prefer the smaller unit (ml)
            if (IsVolumeUnit(u1) && IsVolumeUnit(u2))
            {
                return "ml";
            }

            // For pieces, return as is
            if (IsPiecesUnit(u1) && IsPiecesUnit(u2))
            {
                return "pcs";
            }

            // Fallback to first unit
            return u1;
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

        // Test method to verify unit conversion is working
        public static void TestUnitConversion()
        {
            System.Diagnostics.Debug.WriteLine("🧪 Testing Unit Conversion...");
            
            // Test ml and L compatibility
            var mlLCompatible = AreCompatibleUnits("ml", "L");
            System.Diagnostics.Debug.WriteLine($"ml and L compatible: {mlLCompatible}");
            
            // Test g and kg compatibility
            var gKgCompatible = AreCompatibleUnits("g", "kg");
            System.Diagnostics.Debug.WriteLine($"g and kg compatible: {gKgCompatible}");
            
            // Test conversion
            var mlToL = Convert(1000, "ml", "L");
            System.Diagnostics.Debug.WriteLine($"1000 ml = {mlToL} L");
            
            var gToKg = Convert(1000, "g", "kg");
            System.Diagnostics.Debug.WriteLine($"1000 g = {gToKg} kg");
            
            System.Diagnostics.Debug.WriteLine("✅ Unit conversion test completed");
        }
    }
}


