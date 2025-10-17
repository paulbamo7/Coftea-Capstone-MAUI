using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using Coftea_Capstone.Models;
using System;
using System.IO;
using Microsoft.Maui.Storage;

namespace Coftea_Capstone.C_
{
    public partial class POSPageModel : ObservableObject
    {
        public POSPageModel()
        {
            InventoryItems.CollectionChanged += (_, __) =>
            {
                UnhookAddonItemHandlers();
                HookAddonItemHandlers();
                OnPropertyChanged(nameof(TotalPrice));
            };
            UnhookAddonItemHandlers();
            HookAddonItemHandlers();
        }
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public decimal SmallPrice { get; set; }
        public decimal MediumPrice { get; set; }
        public decimal LargePrice { get; set; }
        public string ImageSet { get; set; }
        public ImageSource ImageSource // Identifies the source of the image
        {
            get
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(ImageSet))
                        return ImageSource.FromFile("coftea_logo.png"); 
                    
                    if (ImageSet.StartsWith("http"))
                        return ImageSource.FromUri(new Uri(ImageSet));

                    // Normalize to just the filename
                    var fileName = Path.GetFileName(ImageSet);

                    // 1) Absolute/local path
                    if (Path.IsPathRooted(ImageSet) && File.Exists(ImageSet))
                        return ImageSource.FromFile(ImageSet);

                    // 2) App data location (e.g., previously downloaded images)
                    var appDataFolder = Path.Combine(FileSystem.AppDataDirectory, "ProductImages");
                    var appDataPath = Path.Combine(appDataFolder, fileName);
                    if (File.Exists(appDataPath))
                        return ImageSource.FromFile(appDataPath);

                    // 3) Bundled resource in Resources/Images by filename
                    return ImageSource.FromFile(fileName);
                }
                catch
                {
                    return ImageSource.FromFile("coftea_logo.png");
                }
            }
        }
        public string Category { get; set; }
        public string Subcategory { get; set; }
        public string ProductDescription { get; set; }
        public string ColorCode { get; set; }

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
            set { SetProperty(ref smallQuantity, value); OnPropertyChanged(nameof(TotalPrice)); } // Update total price when quantity changes   
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
        public string SelectedSize // Tracks the currently selected product size.
        {
            get => selectedSize;
            set => SetProperty(ref selectedSize, value);
        }

        public decimal TotalPrice // Calculates the total price based on sizes and addons
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

        // List of selected ingredients/addons for the product selected from the inventory
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

        private void UnhookAddonItemHandlers() 
        {
            foreach (var item in InventoryItems)
            {
                try { item.PropertyChanged -= OnAddonPropertyChanged; } catch { }
            }
        }

        private void OnAddonPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) // Update total price when addon properties change
        {
            if (e.PropertyName == nameof(InventoryPageModel.AddonPrice) || e.PropertyName == nameof(InventoryPageModel.AddonQuantity))
            {
                OnPropertyChanged(nameof(TotalPrice));
            }
        }
    }
}
