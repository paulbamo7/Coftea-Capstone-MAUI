using CommunityToolkit.Mvvm.ComponentModel;

namespace Coftea_Capstone.Models
{
    public class TrendItem : ObservableObject
    {
        public string Name { get; set; }
        public int Count { get; set; }
        public string ColorCode { get; set; }
        
        // Maximum count in the collection for relative scaling
        public int MaxCount { get; set; } = 1;
        
        // Scaled count for bar chart visualization (scaled to fit within 150px height)
        public double ScaledCount 
        { 
            get 
            {
                if (Count <= 0) return 10; // Minimum height for visibility
                if (MaxCount <= 0) return 10;
                
                // Scale relative to the maximum count, with the highest item reaching 140px
                double ratio = (double)Count / MaxCount;
                return Math.Max(10, ratio * 140); // Minimum 10px, maximum 140px
            } 
        }
    }
}
