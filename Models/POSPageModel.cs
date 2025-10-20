using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using Coftea_Capstone.Models;
using System;
using System.IO;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Graphics;
using Coftea_Capstone.Services;

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
        private ImageSource _cachedImageSource;
        private bool _imageSourceInitialized = false;

        public ImageSource ImageSource // Identifies the source of the image
        {
            get
            {
                if (!_imageSourceInitialized)
                {
                    _cachedImageSource = LoadImageSource();
                    _imageSourceInitialized = true;
                }
                return _cachedImageSource;
            }
        }

        private ImageSource LoadImageSource()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🖼️ Loading image for product: {ProductName}, ImageSet: '{ImageSet}'");
                
                if (string.IsNullOrWhiteSpace(ImageSet))
                {
                    System.Diagnostics.Debug.WriteLine("🖼️ ImageSet is empty, using default logo");
                    return ImageLoadingService.GetFallbackImage();
                }
                
                if (ImageSet.StartsWith("http"))
                {
                    System.Diagnostics.Debug.WriteLine($"🖼️ Loading HTTP image: {ImageSet}");
                    return ImageSource.FromUri(new Uri(ImageSet));
                }

                // Normalize to just the filename
                var fileName = Path.GetFileName(ImageSet);
                System.Diagnostics.Debug.WriteLine($"🖼️ Normalized filename: {fileName}");

                // 1) Absolute/local path
                if (Path.IsPathRooted(ImageSet) && File.Exists(ImageSet))
                {
                    System.Diagnostics.Debug.WriteLine($"🖼️ Loading from absolute path: {ImageSet}");
                    return CreateOptimizedImageSource(ImageSet);
                }

                // 2) App data location (e.g., previously downloaded images)
                var appDataFolder = Path.Combine(FileSystem.AppDataDirectory, "ProductImages");
                var appDataPath = Path.Combine(appDataFolder, fileName);
                if (File.Exists(appDataPath))
                {
                    System.Diagnostics.Debug.WriteLine($"🖼️ Loading from app data: {appDataPath}");
                    return CreateOptimizedImageSource(appDataPath);
                }

                // 3) Check if it's a bundled resource in Resources/Images
                System.Diagnostics.Debug.WriteLine($"🖼️ Attempting to load bundled resource: {fileName}");
                
                // Try different approaches for bundled resources
                try
                {
                    // First try with the filename as-is
                    var resourceImage = CreateOptimizedImageSource(fileName);
                    System.Diagnostics.Debug.WriteLine($"🖼️ Successfully loaded bundled resource: {fileName}");
                    return resourceImage;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"🖼️ Failed to load {fileName} as bundled resource: {ex.Message}");
                    
                    // Try with .png extension if not present
                    if (!fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) && 
                        !fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) && 
                        !fileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                    {
                        var fileNameWithExt = fileName + ".png";
                        try
                        {
                            System.Diagnostics.Debug.WriteLine($"🖼️ Trying with .png extension: {fileNameWithExt}");
                            return CreateOptimizedImageSource(fileNameWithExt);
                        }
                        catch (Exception ex2)
                        {
                            System.Diagnostics.Debug.WriteLine($"🖼️ Failed to load {fileNameWithExt}: {ex2.Message}");
                        }
                    }
                }

                // 4) Fallback to default logo
                System.Diagnostics.Debug.WriteLine("🖼️ All image loading attempts failed, using default logo");
                return ImageLoadingService.GetFallbackImage();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"🖼️ Error loading image for {ProductName}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"🖼️ Stack trace: {ex.StackTrace}");
                return ImageLoadingService.GetFallbackImage();
            }
        }

        private ImageSource CreateOptimizedImageSource(string imagePath)
        {
            return ImageLoadingService.CreateOptimizedImageSource(imagePath);
        }

        public void RefreshImageSource()
        {
            _imageSourceInitialized = false;
            _cachedImageSource = null;
            OnPropertyChanged(nameof(ImageSource));
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
