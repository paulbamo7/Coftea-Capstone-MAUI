using CommunityToolkit.Mvvm.ComponentModel;

namespace Coftea_Capstone.Models
{
    public partial class TrendItem : ObservableObject
    {
        public string Name { get; set; }
        
        [ObservableProperty]
        private string colorCode;
        public string Category { get; set; }
        
        // Maximum count in the collection for relative scaling
        public int MaxCount { get; set; } = 1;
        
        // Maximum Y-axis value for proper scaling (e.g., 100, 120, 140, etc.)
        private int _yAxisMax = 100;
        public int YAxisMax 
        { 
            get => _yAxisMax;
            set 
            {
                if (SetProperty(ref _yAxisMax, value))
                {
                    // Notify that ScaledCount has changed when YAxisMax changes
                    OnPropertyChanged(nameof(ScaledCount));
                }
            }
        }
        
        private int _count;
        public int Count
        {
            get => _count;
            set
            {
                if (SetProperty(ref _count, value))
                {
                    // Notify that ScaledCount has changed when Count changes
                    OnPropertyChanged(nameof(ScaledCount));
                }
            }
        }
        
        // Scaled count for bar chart visualization (scaled to fit within 150px height)
        public double ScaledCount 
        { 
            get 
            {
                if (Count <= 0) return 10; // Minimum 10px height for visibility
                if (YAxisMax <= 0) return 10;
                
                // Scale relative to the Y-axis maximum value, with the highest item reaching 150px
                double ratio = (double)Count / YAxisMax;
                double scaled = ratio * 150; // Scale from 0 to 150px based on ratio
                return Math.Max(scaled, 10); // Ensure minimum 10px height
            } 
        }
    }
}
