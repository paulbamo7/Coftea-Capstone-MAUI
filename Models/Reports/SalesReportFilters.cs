using System.Collections.Generic;

namespace Coftea_Capstone.Models.Reports
{
    /// <summary>
    /// Represents filters for sales report data
    /// </summary>
    public class SalesReportFilters
    {
        public HashSet<string> PaymentMethods { get; set; } = new();
        public HashSet<string> Categories { get; set; } = new();
        public HashSet<string> Sizes { get; set; } = new();
        
        public bool HasAnyFilter => PaymentMethods.Count > 0 || Categories.Count > 0 || Sizes.Count > 0;
        
        public void Clear()
        {
            PaymentMethods.Clear();
            Categories.Clear();
            Sizes.Clear();
        }
    }
    
    /// <summary>
    /// Represents comparison settings for sales reports
    /// </summary>
    public class SalesReportComparison
    {
        public bool IsEnabled { get; set; }
        public ComparisonType Type { get; set; }
        public decimal CurrentPeriodTotal { get; set; }
        public decimal ComparisonPeriodTotal { get; set; }
        
        public decimal Difference => CurrentPeriodTotal - ComparisonPeriodTotal;
        
        public decimal PercentageChange
        {
            get
            {
                if (ComparisonPeriodTotal == 0) return CurrentPeriodTotal > 0 ? 100 : 0;
                return ((CurrentPeriodTotal - ComparisonPeriodTotal) / ComparisonPeriodTotal) * 100;
            }
        }
        
        public bool IsPositive => Difference >= 0;
    }
    
    public enum ComparisonType
    {
        PeriodOverPeriod,  // This week vs last week
        YearOverYear,      // This month vs same month last year
        Custom             // Custom date range comparison
    }
}

