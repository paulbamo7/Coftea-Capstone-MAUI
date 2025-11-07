using System;
using System.Globalization;

namespace Coftea_Capstone.Models.Reports
{
    public class SalesAggregateBreakdown
    {
        public string PaymentMethod { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Revenue { get; set; }

        public string QuantityDisplay => Quantity.ToString("N0", CultureInfo.InvariantCulture);
        public string RevenueDisplay => Revenue <= 0m
            ? "₱0.00"
            : FormattableString.Invariant($"₱{Revenue:N2}");

        public void Add(int quantity, decimal revenue)
        {
            Quantity += quantity;
            Revenue += revenue;
        }
    }

    public class SalesAggregateRow
    {
        public string GroupKey { get; set; } = string.Empty;
        public string GroupLabel { get; set; } = string.Empty;
        public string GroupingDescription { get; set; } = string.Empty;

        public int TotalQuantity { get; set; }
        public decimal TotalRevenue { get; set; }

        public SalesAggregateBreakdown Cash { get; } = new() { PaymentMethod = "Cash" };
        public SalesAggregateBreakdown GCash { get; } = new() { PaymentMethod = "GCash" };
        public SalesAggregateBreakdown Bank { get; } = new() { PaymentMethod = "Bank" };

        public string TotalQuantityDisplay => TotalQuantity.ToString("N0", CultureInfo.InvariantCulture);
        public string TotalRevenueDisplay => TotalRevenue <= 0m
            ? "₱0.00"
            : FormattableString.Invariant($"₱{TotalRevenue:N2}");

        public void AddOverall(int quantity, decimal revenue)
        {
            TotalQuantity += quantity;
            TotalRevenue += revenue;
        }
    }
}

