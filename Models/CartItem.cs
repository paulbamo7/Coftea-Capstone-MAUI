using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using Coftea_Capstone.C_;

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

        [ObservableProperty]
        private string paymentMethod = ""; // Track which payment method was used for this item

        public string SizeDisplay => GetSizeDisplay(); // Displays quantities for each size

        public string AddOnsDisplay => AddOns != null && AddOns.Count > 0 ? string.Join(", ", AddOns) : "No add-ons"; // Displays selected add-ons or "No add-ons"

        public int TotalQuantity => SmallQuantity + MediumQuantity + LargeQuantity;
        
        public decimal TotalPrice => Price; // Price already includes addon costs from CartPopupViewModel

        // Carry selected addon items with their quantities to checkout
        public ObservableCollection<InventoryPageModel> InventoryItems { get; set; } = new();

        private string GetSizeDisplay() // Displays quantities for each size
        {
            var sizes = new List<string>();
            if (SmallQuantity > 0) sizes.Add($"Small: {SmallQuantity}");
            if (MediumQuantity > 0) sizes.Add($"Medium: {MediumQuantity}");
            if (LargeQuantity > 0) sizes.Add($"Large: {LargeQuantity}");
            
            return sizes.Count > 0 ? string.Join("\n", sizes) : "No sizes";
        }
    }
}
