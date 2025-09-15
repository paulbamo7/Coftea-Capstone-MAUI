using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Coftea_Capstone.C_
{
    public class POSPageModel : ObservableObject
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public decimal SmallPrice { get; set; }
        public decimal LargePrice { get; set; }
        public string ImageSet { get; set; }
        public string Category { get; set; }

        public bool HasSmall { get; set; } = true;
        public bool HasLarge { get; set; } = true;

        private int smallQuantity;
        public int SmallQuantity
        {
            get => smallQuantity;
            set { SetProperty(ref smallQuantity, value); OnPropertyChanged(nameof(TotalPrice)); }
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

        public decimal TotalPrice => (SmallPrice * SmallQuantity) + (LargePrice * LargeQuantity);

        // Addons
        public ObservableCollection<string> Addons { get; set; } = new();
        public string SelectedAddon { get; set; }
    }
}
