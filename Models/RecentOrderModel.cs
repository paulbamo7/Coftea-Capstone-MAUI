using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Controls;
using System.IO;

namespace Coftea_Capstone.Models
{
    public partial class RecentOrderModel : ObservableObject
    {
        [ObservableProperty]
        private int orderNumber;

        [ObservableProperty]
        private string status = "Completed";

        [ObservableProperty]
        private string productImage;

        [ObservableProperty]
        private string productName;

        [ObservableProperty]
        private decimal totalAmount;

        [ObservableProperty]
        private DateTime orderTime;

        public string OrderDisplay => $"Order #{OrderNumber}";
        public string StatusColor => Status == "Completed" ? "#4CAF50" : "#FF9800";

        public ImageSource ImageSource // Converts ProductImage string to ImageSource
        {
            get
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(ProductImage))
                        return ImageSource.FromFile("drink.png"); // default fallback
                    
                    // Handle HTTP URLs
                    if (ProductImage.StartsWith("http"))
                        return ImageSource.FromUri(new Uri(ProductImage));

                    // Handle local file paths (legacy support)
                    if (Path.IsPathRooted(ProductImage) && File.Exists(ProductImage))
                        return ImageSource.FromFile(ProductImage);

                    // Handle app data directory images (new system)
                    var appDataPath = Services.ImagePersistenceService.GetImagePath(ProductImage);
                    if (File.Exists(appDataPath))
                        return ImageSource.FromFile(appDataPath);

                    // Fallback to bundled resource
                    return ImageSource.FromFile(ProductImage);
                }
                catch
                {
                    return ImageSource.FromFile("drink.png");
                }
            }
        }
    }
}
