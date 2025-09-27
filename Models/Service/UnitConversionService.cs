using System;

namespace Coftea_Capstone.Models.Service
{
    public static class UnitConversionService
    {
        // Convert value from sourceUnit to targetUnit. Supports kg<->g and L<->ml
        public static double Convert(double value, string sourceUnit, string targetUnit)
        {
            var fromU = Normalize(sourceUnit);
            var toU = Normalize(targetUnit);

            if (string.IsNullOrWhiteSpace(fromU) || string.IsNullOrWhiteSpace(toU) || fromU == toU)
                return value;

            // Mass
            if ((fromU == "kg" && toU == "g")) return value * 1000d;
            if ((fromU == "g" && toU == "kg")) return value / 1000d;

            // Volume
            if ((fromU == "L" && toU == "ml")) return value * 1000d;
            if ((fromU == "ml" && toU == "L")) return value / 1000d;

            // Unknown combo — return original
            return value;
        }

        public static string Normalize(string unit)
        {
            var u = (unit ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(u)) return string.Empty;
            var lower = u.ToLowerInvariant();
            if (lower == "kg" || lower.Contains("kilogram")) return "kg";
            if (lower == "g" || lower.Contains("gram")) return "g";
            if (lower == "l" || lower.Contains("liter")) return "L";
            if (lower == "ml" || lower.Contains("milliliter")) return "ml";
            if (lower.Contains("pcs")) return "pcs";
            return u;
        }
    }
}


