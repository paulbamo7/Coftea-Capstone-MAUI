using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using Coftea_Capstone.Models;

namespace Coftea_Capstone.C_
{
    public class POSPageModel : ObservableObject
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public decimal SmallPrice { get; set; }
        public decimal MediumPrice { get; set; }
        public decimal LargePrice { get; set; }
        public string ImageSet { get; set; }
        public ImageSource ImageSource
        {
            get
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(ImageSet))
                        return ImageSource.FromFile("placeholder.png");
                    
                    if (ImageSet.StartsWith("http"))
                        return ImageSource.FromUri(new Uri(ImageSet));
                    
                    return ImageSource.FromFile(ImageSet);
                }
                catch
                {
                    return ImageSource.FromFile("placeholder.png");
                }
            }
        }
        public string Category { get; set; }
        public string Subcategory { get; set; }
        public string ProductDescription { get; set; }

        public bool HasSmall { get; set; } = true;
        public bool HasMedium { get; set; } = true;
        public bool HasLarge { get; set; } = true;

        private int smallQuantity;
        public int SmallQuantity
        {
            get => smallQuantity;
            set { SetProperty(ref smallQuantity, value); OnPropertyChanged(nameof(TotalPrice)); }
        }

        private int mediumQuantity;
        public int MediumQuantity
        {
            get => mediumQuantity;
            set { SetProperty(ref mediumQuantity, value); OnPropertyChanged(nameof(TotalPrice)); }
        }

        private int largeQuantity;
        public int LargeQuantity
        {
            get => largeQuantity;
            set { SetProperty(ref largeQuantity, value); OnPropertyChanged(nameof(TotalPrice)); }
        }

        private string selectedSize;
        public string SelectedSize
        {
            get => selectedSize;
            set => SetProperty(ref selectedSize, value);
        }

        public decimal TotalPrice => (SmallPrice * SmallQuantity) + (MediumPrice * MediumQuantity) + (LargePrice * LargeQuantity);

        // Linked inventory items (addons/ingredients)
        public ObservableCollection<InventoryPageModel> InventoryItems { get; set; } = new();
    }
}
