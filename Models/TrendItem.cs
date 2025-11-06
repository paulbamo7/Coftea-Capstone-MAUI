using CommunityToolkit.Mvvm.ComponentModel;

namespace Coftea_Capstone.Models
{
    public class TrendItem : ObservableObject
    {
        public string Name { get; set; }
        public int Count { get; set; }
        public string ColorCode { get; set; }
        public string Category { get; set; }
        
        // Maximum count in the collection for relative scaling
        public int MaxCount { get; set; } = 1;
        
        // Maximum Y-axis value for proper scaling (e.g., 100, 120, 140, etc.)
        public int YAxisMax { get; set; } = 100;
        
        // Scaled count for bar chart visualization (scaled to fit within 150px height)
        public double ScaledCount 
        { 
            get 
            {
                if (Count <= 0) return 10; // Minimum height for visibility
                if (YAxisMax <= 0) return 10;
                
                // Scale relative to the Y-axis maximum value, with the highest item reaching 140px
                double ratio = (double)Count / YAxisMax;
                return Math.Max(10, ratio * 140); // Minimum 10px, maximum 140px
            } 
        }
    }
}
