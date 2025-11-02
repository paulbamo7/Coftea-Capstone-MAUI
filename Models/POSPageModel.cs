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
            
            SmallAddons.CollectionChanged += (_, __) =>
            {
                UnhookSmallAddonHandlers();
                HookSmallAddonHandlers();
                OnPropertyChanged(nameof(TotalPrice));
            };
            
            MediumAddons.CollectionChanged += (_, __) =>
            {
                UnhookMediumAddonHandlers();
                HookMediumAddonHandlers();
                OnPropertyChanged(nameof(TotalPrice));
            };
            
            LargeAddons.CollectionChanged += (_, __) =>
            {
                UnhookLargeAddonHandlers();
                HookLargeAddonHandlers();
                OnPropertyChanged(nameof(TotalPrice));
            };
            
            UnhookAddonItemHandlers();
            HookAddonItemHandlers();
            UnhookSmallAddonHandlers();
            HookSmallAddonHandlers();
            UnhookMediumAddonHandlers();
            HookMediumAddonHandlers();
            UnhookLargeAddonHandlers();
            HookLargeAddonHandlers();
        }
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public decimal? SmallPrice { get; set; } // Nullable for non-Coffee categories
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
                    
                    // Handle HTTP URLs
                    if (ImageSet.StartsWith("http"))
                        return ImageSource.FromUri(new Uri(ImageSet));

                    // Handle local file paths (legacy support)
                    if (Path.IsPathRooted(ImageSet) && File.Exists(ImageSet))
                        return ImageSource.FromFile(ImageSet);

                    // Handle app data directory images (new system)
                    var appDataPath = Services.ImagePersistenceService.GetImagePath(ImageSet);
                    if (File.Exists(appDataPath))
                        return ImageSource.FromFile(appDataPath);

                    // Fallback to bundled resource (restoration from database happens when items are loaded)
                    return ImageSource.FromFile(ImageSet);
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
                decimal baseSizes = ((SmallPrice ?? 0) * SmallQuantity) + (MediumPrice * MediumQuantity) + (LargePrice * LargeQuantity);
                decimal addons = 0m;
                
                // Legacy: Include old InventoryItems (if any)
                foreach (var addon in InventoryItems)
                {
                    if (addon == null || !addon.IsSelected) continue;
                    addons += addon.AddonPrice * addon.AddonQuantity;
                }
                
                // Small size addons
                foreach (var addon in SmallAddons)
                {
                    if (addon == null || !addon.IsSelected) continue;
                    addons += addon.AddonPrice * addon.AddonQuantity * SmallQuantity;
                }
                
                // Medium size addons
                foreach (var addon in MediumAddons)
                {
                    if (addon == null || !addon.IsSelected) continue;
                    addons += addon.AddonPrice * addon.AddonQuantity * MediumQuantity;
                }
                
                // Large size addons
                foreach (var addon in LargeAddons)
                {
                    if (addon == null || !addon.IsSelected) continue;
                    addons += addon.AddonPrice * addon.AddonQuantity * LargeQuantity;
                }
                
                return baseSizes + addons;
            }
        }

        // List of selected ingredients/addons for the product selected from the inventory (legacy - kept for backward compatibility)
        public ObservableCollection<InventoryPageModel> InventoryItems { get; set; } = new();
        
        // Separate addon collections for Small, Medium, and Large sizes
        public ObservableCollection<InventoryPageModel> SmallAddons { get; set; } = new();
        public ObservableCollection<InventoryPageModel> MediumAddons { get; set; } = new();
        public ObservableCollection<InventoryPageModel> LargeAddons { get; set; } = new();

        // Dropdown visibility for addon selection
        [ObservableProperty]
        private bool isSmallAddonsDropdownVisible = false;
        
        [ObservableProperty]
        private bool isMediumAddonsDropdownVisible = false;
        
        [ObservableProperty]
        private bool isLargeAddonsDropdownVisible = false;

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
        
        private void HookSmallAddonHandlers()
        {
            foreach (var item in SmallAddons)
            {
                if (item == null) continue;
                item.PropertyChanged -= OnSmallAddonPropertyChanged;
                item.PropertyChanged += OnSmallAddonPropertyChanged;
            }
        }

        private void UnhookSmallAddonHandlers() 
        {
            foreach (var item in SmallAddons)
            {
                try { item.PropertyChanged -= OnSmallAddonPropertyChanged; } catch { }
            }
        }
        
        private void HookMediumAddonHandlers()
        {
            foreach (var item in MediumAddons)
            {
                if (item == null) continue;
                item.PropertyChanged -= OnMediumAddonPropertyChanged;
                item.PropertyChanged += OnMediumAddonPropertyChanged;
            }
        }

        private void UnhookMediumAddonHandlers() 
        {
            foreach (var item in MediumAddons)
            {
                try { item.PropertyChanged -= OnMediumAddonPropertyChanged; } catch { }
            }
        }
        
        private void HookLargeAddonHandlers()
        {
            foreach (var item in LargeAddons)
            {
                if (item == null) continue;
                item.PropertyChanged -= OnLargeAddonPropertyChanged;
                item.PropertyChanged += OnLargeAddonPropertyChanged;
            }
        }

        private void UnhookLargeAddonHandlers() 
        {
            foreach (var item in LargeAddons)
            {
                try { item.PropertyChanged -= OnLargeAddonPropertyChanged; } catch { }
            }
        }

        private void OnAddonPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) // Update total price when addon properties change
        {
            if (e.PropertyName == nameof(InventoryPageModel.AddonPrice) || e.PropertyName == nameof(InventoryPageModel.AddonQuantity))
            {
                OnPropertyChanged(nameof(TotalPrice));
            }
        }
        
        private void OnMediumAddonPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(InventoryPageModel.AddonPrice) || e.PropertyName == nameof(InventoryPageModel.AddonQuantity))
            {
                OnPropertyChanged(nameof(TotalPrice));
            }
        }
        
        private void OnLargeAddonPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(InventoryPageModel.AddonPrice) || e.PropertyName == nameof(InventoryPageModel.AddonQuantity))
            {
                OnPropertyChanged(nameof(TotalPrice));
            }
        }
        
        private void OnSmallAddonPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(InventoryPageModel.AddonPrice) || e.PropertyName == nameof(InventoryPageModel.AddonQuantity))
            {
                OnPropertyChanged(nameof(TotalPrice));
            }
        }
    }
}
