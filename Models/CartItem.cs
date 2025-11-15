using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
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
        private string imageSet; // Store as string (filename)

        public ImageSource ImageSource // Convert to ImageSource for binding
        {
            get
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(ImageSet))
                        return ImageSource.FromFile("coftea_logo.png");
                    
                    // Handle HTTP URLs
                    if (ImageSet.StartsWith("http"))
                        return ImageSource.FromUri(new Uri(ImageSet));

                    // Handle local file paths (legacy support)
                    if (System.IO.Path.IsPathRooted(ImageSet) && System.IO.File.Exists(ImageSet))
                        return ImageSource.FromFile(ImageSet);

                    // Handle app data directory images (new system)
                    var appDataPath = Services.ImagePersistenceService.GetImagePath(ImageSet);
                    if (System.IO.File.Exists(appDataPath))
                        return ImageSource.FromFile(appDataPath);

                    // Fallback to bundled resource
                    return ImageSource.FromFile(ImageSet);
                }
                catch
                {
                    return ImageSource.FromFile("coftea_logo.png");
                }
            }
        }

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
        private decimal? smallPrice; // Nullable for non-Coffee categories

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

        // Carry selected addon items with their quantities to checkout (legacy - kept for backward compatibility)
        public ObservableCollection<InventoryPageModel> InventoryItems { get; set; } = new();
        
        // Separate addon collections for Small, Medium, and Large sizes
        public ObservableCollection<InventoryPageModel> SmallAddons { get; set; } = new();
        public ObservableCollection<InventoryPageModel> MediumAddons { get; set; } = new();
        public ObservableCollection<InventoryPageModel> LargeAddons { get; set; } = new();

        // Size breakdown properties for display
        public decimal SmallSubtotal => SmallQuantity > 0 && SmallPrice.HasValue ? SmallQuantity * SmallPrice.Value : 0;
        public decimal MediumSubtotal => MediumQuantity > 0 ? MediumQuantity * MediumPrice : 0;
        public decimal LargeSubtotal => LargeQuantity > 0 ? LargeQuantity * LargePrice : 0;
        
        public bool HasSmall => SmallQuantity > 0 && SmallPrice.HasValue;
        public bool HasMedium => MediumQuantity > 0;
        public bool HasLarge => LargeQuantity > 0;
        
        // Display strings for size breakdown
        public string SmallDisplay => HasSmall ? $"₱{SmallPrice.Value:F2} × {SmallQuantity}" : "";
        public string MediumDisplay => HasMedium ? $"₱{MediumPrice:F2} × {MediumQuantity}" : "";
        public string LargeDisplay => HasLarge ? $"₱{LargePrice:F2} × {LargeQuantity}" : "";
        
        // Compact display strings with size prefix
        public string SmallDisplayCompact => HasSmall ? $"S: {SmallDisplay}" : "";
        public string MediumDisplayCompact => HasMedium ? $"M: {MediumDisplay}" : "";
        public string LargeDisplayCompact => HasLarge ? $"L: {LargeDisplay}" : "";

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
