using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using Coftea_Capstone.Models;
using System;

namespace Coftea_Capstone.C_
{
    public partial class POSPageModel : ObservableObject
    {
        public POSPageModel()
        {
            InventoryItems.CollectionChanged += (_, __) =>
            {
                HookAddonItemHandlers();
                OnPropertyChanged(nameof(TotalPrice));
            };
            HookAddonItemHandlers();
        }
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

        // Stock availability
        [ObservableProperty]
        private bool isLowStock = false;

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

        public decimal TotalPrice
        {
            get
            {
                decimal baseSizes = (SmallPrice * SmallQuantity) + (MediumPrice * MediumQuantity) + (LargePrice * LargeQuantity);
                decimal addons = 0m;
                foreach (var addon in InventoryItems)
                {
                    if (addon == null) continue;
                    addons += addon.AddonPrice * addon.AddonQuantity;
                }
                return baseSizes + addons;
            }
        }

        // Linked inventory items (addons/ingredients)
        public ObservableCollection<InventoryPageModel> InventoryItems { get; set; } = new();

        private void HookAddonItemHandlers()
        {
            foreach (var item in InventoryItems)
            {
                if (item == null) continue;
                item.PropertyChanged -= OnAddonPropertyChanged;
                item.PropertyChanged += OnAddonPropertyChanged;
            }
        }

        private void OnAddonPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(InventoryPageModel.AddonPrice) || e.PropertyName == nameof(InventoryPageModel.AddonQuantity))
            {
                OnPropertyChanged(nameof(TotalPrice));
            }
        }
    }
}
