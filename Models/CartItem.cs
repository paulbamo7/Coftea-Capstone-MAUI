using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Coftea_Capstone.Models
{
    public partial class CartItem : ObservableObject
    {
        [ObservableProperty]
        private int productId;

        [ObservableProperty]
        private string productName;

        [ObservableProperty]
        private string imageSource;

        [ObservableProperty]
        private string customerName;

        [ObservableProperty]
        private string sugarLevel;

        [ObservableProperty]
        private ObservableCollection<string> addOns = new();

        [ObservableProperty]
        private int smallQuantity;

        [ObservableProperty]
        private int mediumQuantity;

        [ObservableProperty]
        private int largeQuantity;

        [ObservableProperty]
        private decimal smallPrice;

        [ObservableProperty]
        private decimal mediumPrice;

        [ObservableProperty]
        private decimal largePrice;

        [ObservableProperty]
        private string selectedSize;

        [ObservableProperty]
        private int quantity;

        [ObservableProperty]
        private decimal price;

        public string SizeDisplay => GetSizeDisplay();
        
        public string AddOnsDisplay => AddOns != null && AddOns.Count > 0 ? string.Join(", ", AddOns) : "No add-ons";
        
        public int TotalQuantity => SmallQuantity + MediumQuantity + LargeQuantity;
        
        public decimal TotalPrice => Price; // Price already includes addon costs from CartPopupViewModel

        private string GetSizeDisplay()
        {
            var sizes = new List<string>();
            if (SmallQuantity > 0) sizes.Add($"Small: {SmallQuantity}");
            if (MediumQuantity > 0) sizes.Add($"Medium: {MediumQuantity}");
            if (LargeQuantity > 0) sizes.Add($"Large: {LargeQuantity}");
            
            return sizes.Count > 0 ? string.Join(", ", sizes) : "No sizes";
        }
    }
}
