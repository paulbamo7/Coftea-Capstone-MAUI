using Coftea_Capstone.C_; // Start
using Coftea_Capstone.Models;
using Microsoft.Maui.Networking;
using Coftea_Capstone.Views.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using System;
using Coftea_Capstone.ViewModel.Controls;
using Coftea_Capstone.Models.Service;
using Coftea_Capstone.Services;

namespace Coftea_Capstone.ViewModel
{
    public partial class POSPageViewModel : BaseViewModel
    {
        // Dependencies & Services which references other popups, databases 
        private readonly CartStorageService _cartStorage = new CartStorageService();
        public SettingsPopUpViewModel SettingsPopup { get; set; }
        public AddItemToPOSViewModel AddItemToPOSViewModel { get; set; }
        public RetryConnectionPopupViewModel RetryConnectionPopup { get; set; }
        public NotificationPopupViewModel NotificationPopup { get; set; }
        public CartPopupViewModel CartPopup { get; set; }
        public HistoryPopupViewModel HistoryPopup { get; set; }
        public ProcessingQueuePopupViewModel ProcessingQueuePopup { get; set; }
        public PaymentPopupViewModel PaymentPopup { get; set; }
        public OrderCompletePopupViewModel OrderCompletePopup { get; set; }
        public OrderConfirmedPopupViewModel OrderConfirmedPopup { get; set; }
        public SuccessCardPopupViewModel SuccessCardPopup { get; set; }
        public AddonsSelectionPopupViewModel AddonsPopup { get; set; }


        // ===================== State & Models holding product list, filter list, current cart items. =====================
        private readonly Database _database;

        [ObservableProperty]
        private ObservableCollection<POSPageModel> products = new();

        [ObservableProperty]
        private ObservableCollection<POSPageModel> cartItems = new();

        [ObservableProperty]
        private ObservableCollection<POSPageModel> filteredProducts = new();

        [ObservableProperty]
        private string selectedMainCategory;

        [ObservableProperty]
        private string selectedSubcategory;

        public bool IsFruitSodaSubcategoryVisible => !string.IsNullOrWhiteSpace(SelectedMainCategory) && string.Equals(SelectedMainCategory, "Fruit/Soda", StringComparison.OrdinalIgnoreCase);
        
        public bool IsCoffeeSubcategoryVisible => !string.IsNullOrWhiteSpace(SelectedMainCategory) && string.Equals(SelectedMainCategory, "Coffee", StringComparison.OrdinalIgnoreCase);

        partial void OnSelectedMainCategoryChanged(string value)
        {
            System.Diagnostics.Debug.WriteLine($"OnSelectedMainCategoryChanged: value = '{value}'");
            OnPropertyChanged(nameof(IsFruitSodaSubcategoryVisible));
            OnPropertyChanged(nameof(IsCoffeeSubcategoryVisible));
        }

        public bool IsCartVisible => CartItems?.Any() ?? false;

        public int CartCount => CartItems?.Where(item => item != null && (item.SmallQuantity > 0 || item.MediumQuantity > 0 || item.LargeQuantity > 0)).Count() ?? 0;
        
        public bool CartHasItems => CartCount > 0;
        
        public int ProcessingQueueCount => ProcessingQueuePopup?.QueueCount ?? 0;
        
        public bool ProcessingQueueHasItems => ProcessingQueuePopup?.HasItems ?? false;

        partial void OnCartItemsChanged(ObservableCollection<POSPageModel> value)
        {
            OnPropertyChanged(nameof(IsCartVisible));
            OnPropertyChanged(nameof(CartCount));
            OnPropertyChanged(nameof(CartHasItems));
        }
        
        private void OnProcessingQueueItemsChanged()
        {
            OnPropertyChanged(nameof(ProcessingQueueCount));
            OnPropertyChanged(nameof(ProcessingQueueHasItems));
        }

        [ObservableProperty]
        private bool isAdmin;

        [ObservableProperty]
        private bool isCategoryLoading;

        [ObservableProperty]
        private POSPageModel selectedProduct;

        [ObservableProperty]
        private ObservableCollection<string> availableSizes = new();

        public bool IsSmallSizeVisibleInCart => string.Equals(SelectedProduct?.Category, "Coffee", StringComparison.OrdinalIgnoreCase);
        public bool IsMediumSizeVisibleInCart => true; 
        public bool IsLargeSizeVisibleInCart => true;

        // Addon summary properties for display
        public string SmallAddonsDisplay => GetAddonsDisplay("Small");
        public string MediumAddonsDisplay => GetAddonsDisplay("Medium");
        public string LargeAddonsDisplay => GetAddonsDisplay("Large");

        private string GetAddonsDisplay(string size)
        {
            if (SelectedProduct == null) return "";
            
            ObservableCollection<InventoryPageModel> addons = null;
            int quantity = 0;
            
            switch (size)
            {
                case "Small":
                    addons = SelectedProduct.SmallAddons;
                    quantity = SelectedProduct.SmallQuantity;
                    break;
                case "Medium":
                    addons = SelectedProduct.MediumAddons;
                    quantity = SelectedProduct.MediumQuantity;
                    break;
                case "Large":
                    addons = SelectedProduct.LargeAddons;
                    quantity = SelectedProduct.LargeQuantity;
                    break;
            }
            
            if (addons == null || quantity <= 0) return "";
            
            var selectedAddons = addons
                .Where(a => a != null && a.IsSelected && a.AddonQuantity > 0)
                .Select(a => $"{a.itemName} ({a.AddonQuantity} pcs)")
                .ToList();
            
            if (!selectedAddons.Any()) return "";
            
            return $"{size} addons: {string.Join(", ", selectedAddons)}";
        }

        partial void OnSelectedProductChanged(POSPageModel value)
        {
            OnPropertyChanged(nameof(SmallAddonsDisplay));
            OnPropertyChanged(nameof(MediumAddonsDisplay));
            OnPropertyChanged(nameof(LargeAddonsDisplay));
            
            // Unsubscribe from previous product
            if (value == null)
            {
                return;
            }
            
            // Subscribe to product quantity changes
            value.PropertyChanged -= OnProductQuantityChanged;
            value.PropertyChanged += OnProductQuantityChanged;
            
            // Subscribe to addon collection changes
            if (value.SmallAddons != null)
            {
                value.SmallAddons.CollectionChanged -= OnAddonsCollectionChanged;
                value.SmallAddons.CollectionChanged += OnAddonsCollectionChanged;
                
                foreach (var addon in value.SmallAddons)
                {
                    if (addon != null)
                    {
                        addon.PropertyChanged -= OnAddonPropertyChanged;
                        addon.PropertyChanged += OnAddonPropertyChanged;
                    }
                }
            }
            if (value.MediumAddons != null)
            {
                value.MediumAddons.CollectionChanged -= OnAddonsCollectionChanged;
                value.MediumAddons.CollectionChanged += OnAddonsCollectionChanged;
                
                foreach (var addon in value.MediumAddons)
                {
                    if (addon != null)
                    {
                        addon.PropertyChanged -= OnAddonPropertyChanged;
                        addon.PropertyChanged += OnAddonPropertyChanged;
                    }
                }
            }
            if (value.LargeAddons != null)
            {
                value.LargeAddons.CollectionChanged -= OnAddonsCollectionChanged;
                value.LargeAddons.CollectionChanged += OnAddonsCollectionChanged;
                
                foreach (var addon in value.LargeAddons)
                {
                    if (addon != null)
                    {
                        addon.PropertyChanged -= OnAddonPropertyChanged;
                        addon.PropertyChanged += OnAddonPropertyChanged;
                    }
                }
            }
        }

        private void OnProductQuantityChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(POSPageModel.SmallQuantity) ||
                e.PropertyName == nameof(POSPageModel.MediumQuantity) ||
                e.PropertyName == nameof(POSPageModel.LargeQuantity))
            {
                OnPropertyChanged(nameof(SmallAddonsDisplay));
                OnPropertyChanged(nameof(MediumAddonsDisplay));
                OnPropertyChanged(nameof(LargeAddonsDisplay));
            }
        }

        private void OnAddonsCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Subscribe to new addons
            if (e.NewItems != null)
            {
                foreach (InventoryPageModel addon in e.NewItems)
                {
                    if (addon != null)
                    {
                        addon.PropertyChanged -= OnAddonPropertyChanged;
                        addon.PropertyChanged += OnAddonPropertyChanged;
                    }
                }
            }
            
            // Update display
            OnPropertyChanged(nameof(SmallAddonsDisplay));
            OnPropertyChanged(nameof(MediumAddonsDisplay));
            OnPropertyChanged(nameof(LargeAddonsDisplay));
        }

        private void OnAddonPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(InventoryPageModel.IsSelected) || 
                e.PropertyName == nameof(InventoryPageModel.AddonQuantity))
            {
                OnPropertyChanged(nameof(SmallAddonsDisplay));
                OnPropertyChanged(nameof(MediumAddonsDisplay));
                OnPropertyChanged(nameof(LargeAddonsDisplay));
            }
        } 

        // ===================== Initialization =====================
        public POSPageViewModel(AddItemToPOSViewModel addItemToPOSViewModel, SettingsPopUpViewModel settingsPopupViewModel)
        {
            _database = new Database(); 
            SettingsPopup = settingsPopupViewModel;
            AddItemToPOSViewModel = addItemToPOSViewModel;
            NotificationPopup = ((App)Application.Current).NotificationPopup;
            RetryConnectionPopup = ((App)Application.Current).RetryConnectionPopup;
            CartPopup = new CartPopupViewModel();
            HistoryPopup = new HistoryPopupViewModel();
            ProcessingQueuePopup = new ProcessingQueuePopupViewModel();
            // Subscribe to ProcessingQueuePopup property changes
            ProcessingQueuePopup.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ProcessingQueuePopup.QueueCount) || 
                    e.PropertyName == nameof(ProcessingQueuePopup.HasItems))
                {
                    OnProcessingQueueItemsChanged();
                }
            };
            // Load pending items on initialization
            _ = ProcessingQueuePopup.LoadPendingItemsAsync();
            PaymentPopup = ((App)Application.Current).PaymentPopup;
            OrderCompletePopup = ((App)Application.Current).OrderCompletePopup;
            OrderConfirmedPopup = ((App)Application.Current).OrderConfirmedPopup;
            SuccessCardPopup = ((App)Application.Current).SuccessCardPopup;
            AddonsPopup = new AddonsSelectionPopupViewModel();

            AddonsPopup.AddonsSelected += (selectedAddons) =>
            {
                try
                {
                    if (SelectedProduct == null || selectedAddons == null)
                        return;


                    SelectedProduct.InventoryItems.Clear();
                    foreach (var addon in selectedAddons)
                    {
                        // Ensures addon has proper quantity and selection state
                        if (addon.IsSelected && addon.AddonQuantity <= 0)
                        {
                            addon.AddonQuantity = 1;
                        }
                        System.Diagnostics.Debug.WriteLine($"🔧 Adding addon to product: {addon.itemName}, IsSelected: {addon.IsSelected}, AddonQuantity: {addon.AddonQuantity}");
                        SelectedProduct.InventoryItems.Add(addon);
                    }
                    // Notify price/summary recalculation on UI if bound
                    OnPropertyChanged(nameof(SelectedProduct));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error applying selected addons: {ex.Message}");
                }
            };

            AddItemToPOSViewModel.ProductAdded += OnProductAdded;
            AddItemToPOSViewModel.ProductUpdated += OnProductUpdated;
            AddItemToPOSViewModel.ConnectPOSToInventoryVM.ReturnRequested += () =>
            {
                AddItemToPOSViewModel.IsAddItemToPOSVisible = true;
            };
        }

        [RelayCommand]
        private async void AddToCart(POSPageModel product) // Adds products to cart with their quantities and addons
        {
            try
            {
                if (product == null || CartItems == null)
                {
                    System.Diagnostics.Debug.WriteLine("AddToCart: product or CartItems is null");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"🛒 AddToCart called for: {product.ProductName}");
                System.Diagnostics.Debug.WriteLine($"   SmallPrice: {product.SmallPrice}, MediumPrice: {product.MediumPrice}, LargePrice: {product.LargePrice}");
                
                // Check if there are any items to add (at least one quantity > 0 for available sizes)
                bool hasAnyQuantity = false;
                
                // Only check sizes that the product actually supports
                if (product.HasSmall && product.SmallQuantity > 0) hasAnyQuantity = true;
                if (product.HasMedium && product.MediumQuantity > 0) hasAnyQuantity = true;
                if (product.HasLarge && product.LargeQuantity > 0) hasAnyQuantity = true;
                
                // If product doesn't support any sizes, allow adding without size selection
                if (!product.HasSmall && !product.HasMedium && !product.HasLarge)
                {
                    hasAnyQuantity = true; // Allow products without size support
                }
                
                if (!hasAnyQuantity)
                {
                    // Show notification that no items were selected
                    if (NotificationPopup != null)
                    {
                        NotificationPopup.ShowNotification("Please select at least one item to add to cart.", "No Items Selected");
                    }
                    return;
                }
                // Check if cups and straws are available before adding to cart
                bool cupsAndStrawsAvailable = await CheckCupsAndStrawsAvailability();
                if (cupsAndStrawsAvailable)
                {
                    if (NotificationPopup != null)
                    {
                        NotificationPopup.ShowNotification("Cannot add items to cart: No cups or straws available in inventory.", "Out of Stock");
                    }
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"🛒 Creating cart item copy...");
                
                // Always create a new cart line entry even if the same product already exists
                var copy = new POSPageModel
                {
                    ProductID = product.ProductID,
                    ProductName = product.ProductName,
                    SmallPrice = product.SmallPrice,
                    MediumPrice = product.MediumPrice,
                    LargePrice = product.LargePrice,
                    ImageSet = product.ImageSet,
                    SmallQuantity = product.SmallQuantity,
                    MediumQuantity = product.MediumQuantity,
                    LargeQuantity = product.LargeQuantity,
                    InventoryItems = new ObservableCollection<InventoryPageModel>(), // Create new collection
                    SmallAddons = new ObservableCollection<InventoryPageModel>(),
                    MediumAddons = new ObservableCollection<InventoryPageModel>(),
                    LargeAddons = new ObservableCollection<InventoryPageModel>()
                };

                // Copy Small size addons (only selected ones)
                if (product.SmallAddons != null && product.SmallQuantity > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"🛒 Copying {product.SmallAddons.Count} small addons...");
                    foreach (var addon in product.SmallAddons)
                    {
                        if (addon == null || !addon.IsSelected) continue;
                        var addonCopy = new InventoryPageModel
                        {
                            itemID = addon.itemID,
                            itemName = addon.itemName,
                            itemCategory = addon.itemCategory,
                            itemDescription = addon.itemDescription,
                            itemQuantity = addon.itemQuantity,
                            unitOfMeasurement = addon.unitOfMeasurement,
                            minimumQuantity = addon.minimumQuantity,
                            ImageSet = addon.ImageSet,
                            IsSelected = addon.IsSelected,
                            AddonQuantity = addon.AddonQuantity,
                            AddonPrice = addon.AddonPrice,
                            AddonUnit = addon.AddonUnit
                        };
                        copy.SmallAddons.Add(addonCopy);
                        System.Diagnostics.Debug.WriteLine($"🔧 Copied small addon to cart: {addonCopy.itemName}, IsSelected: {addonCopy.IsSelected}, AddonQuantity: {addonCopy.AddonQuantity}");
                    }
                }

                // Copy Medium size addons (only selected ones)
                if (product.MediumAddons != null && product.MediumQuantity > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"🛒 Copying {product.MediumAddons.Count} medium addons...");
                    foreach (var addon in product.MediumAddons)
                    {
                        if (addon == null || !addon.IsSelected) continue;
                        var addonCopy = new InventoryPageModel
                        {
                            itemID = addon.itemID,
                            itemName = addon.itemName,
                            itemCategory = addon.itemCategory,
                            itemDescription = addon.itemDescription,
                            itemQuantity = addon.itemQuantity,
                            unitOfMeasurement = addon.unitOfMeasurement,
                            minimumQuantity = addon.minimumQuantity,
                            ImageSet = addon.ImageSet,
                            IsSelected = addon.IsSelected,
                            AddonQuantity = addon.AddonQuantity,
                            AddonPrice = addon.AddonPrice,
                            AddonUnit = addon.AddonUnit
                        };
                        copy.MediumAddons.Add(addonCopy);
                        System.Diagnostics.Debug.WriteLine($"🔧 Copied medium addon to cart: {addonCopy.itemName}, IsSelected: {addonCopy.IsSelected}, AddonQuantity: {addonCopy.AddonQuantity}");
                    }
                }
                
                // Copy Large size addons (only selected ones)
                if (product.LargeAddons != null && product.LargeQuantity > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"🛒 Copying {product.LargeAddons.Count} large addons...");
                    foreach (var addon in product.LargeAddons)
                    {
                        if (addon == null || !addon.IsSelected) continue;
                        var addonCopy = new InventoryPageModel
                        {
                            itemID = addon.itemID,
                            itemName = addon.itemName,
                            itemCategory = addon.itemCategory,
                            itemDescription = addon.itemDescription,
                            itemQuantity = addon.itemQuantity,
                            unitOfMeasurement = addon.unitOfMeasurement,
                            minimumQuantity = addon.minimumQuantity,
                            ImageSet = addon.ImageSet,
                            IsSelected = addon.IsSelected,
                            AddonQuantity = addon.AddonQuantity,
                            AddonPrice = addon.AddonPrice,
                            AddonUnit = addon.AddonUnit
                        };
                        copy.LargeAddons.Add(addonCopy);
                        System.Diagnostics.Debug.WriteLine($"🔧 Copied large addon to cart: {addonCopy.itemName}, IsSelected: {addonCopy.IsSelected}, AddonQuantity: {addonCopy.AddonQuantity}");
                    }
                }

                // Legacy: Copy old InventoryItems if any (for backward compatibility)
                if (product.InventoryItems != null)
                {
                    System.Diagnostics.Debug.WriteLine($"🛒 Copying {product.InventoryItems.Count} legacy addons...");
                    foreach (var addon in product.InventoryItems)
                    {
                        if (addon == null || !addon.IsSelected) continue;
                        var addonCopy = new InventoryPageModel
                        {
                            itemID = addon.itemID,
                            itemName = addon.itemName,
                            itemCategory = addon.itemCategory,
                            itemDescription = addon.itemDescription,
                            itemQuantity = addon.itemQuantity,
                            unitOfMeasurement = addon.unitOfMeasurement,
                            minimumQuantity = addon.minimumQuantity,
                            ImageSet = addon.ImageSet,
                            IsSelected = addon.IsSelected,
                            AddonQuantity = addon.AddonQuantity,
                            AddonPrice = addon.AddonPrice,
                            AddonUnit = addon.AddonUnit,
                            InputAmount = addon.InputAmount,
                            InputUnit = addon.InputUnit
                        };
                        copy.InventoryItems.Add(addonCopy);
                        System.Diagnostics.Debug.WriteLine($"🔧 Copied legacy addon to cart: {addonCopy.itemName}, IsSelected: {addonCopy.IsSelected}, AddonQuantity: {addonCopy.AddonQuantity}");
                    }
                }
                
                // Check if an identical item already exists in cart (same product, quantities, and addons)
                var existingItem = CartItems.FirstOrDefault(item => 
                    item.ProductID == copy.ProductID &&
                    item.SmallQuantity == copy.SmallQuantity &&
                    item.MediumQuantity == copy.MediumQuantity &&
                    item.LargeQuantity == copy.LargeQuantity &&
                    AreAddonsIdentical(item.SmallAddons, copy.SmallAddons) &&
                    AreAddonsIdentical(item.MediumAddons, copy.MediumAddons) &&
                    AreAddonsIdentical(item.LargeAddons, copy.LargeAddons) &&
                    AreAddonsIdentical(item.InventoryItems, copy.InventoryItems)
                );
                
                if (existingItem != null)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Identical item already exists in cart, skipping duplicate");
                    if (NotificationPopup != null)
                    {
                        NotificationPopup.ShowNotification("This item is already in your cart with the same configuration.", "Duplicate Item");
                    }
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"🛒 Adding to CartItems collection...");
                CartItems.Add(copy);
                System.Diagnostics.Debug.WriteLine($"✅ Successfully added to cart! Cart now has {CartItems.Count} items");

                // Note: Items are enqueued to processing queue only after payment is confirmed in PaymentPopupViewModel

                // Reset selection quantities
                product.SmallQuantity = 0;
                product.MediumQuantity = 0;
                product.LargeQuantity = 0;

                // Close addon dropdowns
                product.IsSmallAddonsDropdownVisible = false;
                product.IsMediumAddonsDropdownVisible = false;
                product.IsLargeAddonsDropdownVisible = false;
                
                // Reset addons to 0
                if (product.SmallAddons != null)
                {
                    foreach (var addon in product.SmallAddons)
                    {
                        addon.AddonQuantity = 0;
                        addon.IsSelected = false;
                    }
                }
                
                if (product.MediumAddons != null)
                {
                    foreach (var addon in product.MediumAddons)
                    {
                        addon.AddonQuantity = 0;
                        addon.IsSelected = false;
                    }
                }
                
                if (product.LargeAddons != null)
                {
                    foreach (var addon in product.LargeAddons)
                    {
                        addon.AddonQuantity = 0;
                        addon.IsSelected = false;
                    }
                }
                
                // Legacy: Reset old InventoryItems
                if (product.InventoryItems != null)
                {
                    foreach (var addon in product.InventoryItems)
                    {
                        addon.AddonQuantity = 0;
                        addon.IsSelected = false;
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"🛒 Saving cart to storage...");
                _ = _cartStorage.SaveCartAsync(CartItems);
                
                // Clear the right frame by deselecting the product
                SelectedProduct = null;
                System.Diagnostics.Debug.WriteLine($"✅ Cleared SelectedProduct after adding to cart");
            }
            catch (InvalidCastException castEx)
            {
                System.Diagnostics.Debug.WriteLine($"❌ CAST ERROR in AddToCart: {castEx.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {castEx.StackTrace}");
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to add product: Specified cast is not valid.\n\nDetails: {castEx.Message}\n\nPlease check that all prices are set correctly.", "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ERROR in AddToCart: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to add product: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private void ShowNotificationBell() // Opens the notification popup
        {
            NotificationPopup?.ToggleCommand.Execute(null);
        }

        [RelayCommand]
        private void ShowProcessingQueue()
        {
            ProcessingQueuePopup?.Show();
        }

        private RetryConnectionPopupViewModel GetRetryConnectionPopup() => ((App)Application.Current).RetryConnectionPopup;

        private async void OnProductAdded(POSPageModel newProduct) // Handles product addition from AddItemToPOSViewModel
        {
            if (newProduct != null)
            {
                Products?.Add(newProduct); 
                FilteredProducts?.Add(newProduct); 
                await LoadDataAsync();  
            }
        }

        private async void OnProductUpdated(POSPageModel updatedProduct) // Handles product updates from AddItemToPOSViewModel
        {
            if (updatedProduct == null || Products == null || FilteredProducts == null)
                return;

            // Check if the updated product is currently selected
            bool wasSelected = SelectedProduct != null && SelectedProduct.ProductID == updatedProduct.ProductID;

            // Find and update the existing product in the collections
            var existingProduct = Products.FirstOrDefault(p => p.ProductID == updatedProduct.ProductID);
            if (existingProduct != null)
            {
                var index = Products.IndexOf(existingProduct);
                Products[index] = updatedProduct;
            }

            var existingFilteredProduct = FilteredProducts.FirstOrDefault(p => p.ProductID == updatedProduct.ProductID);
            if (existingFilteredProduct != null)
            {
                var index = FilteredProducts.IndexOf(existingFilteredProduct);
                FilteredProducts[index] = updatedProduct;
            }

            await LoadDataAsync();
            
            // If the updated product was selected, reload its addons
            if (wasSelected)
            {
                var refreshedProduct = Products.FirstOrDefault(p => p.ProductID == updatedProduct.ProductID);
                if (refreshedProduct != null)
                {
                    SelectedProduct = refreshedProduct;
                    await LoadAddonsForSelectedAsync();
                }
            }
        }

        [RelayCommand]
        private async Task FilterByCategory(object category) // Filter products by main category
        {
            try
            {
                SelectedMainCategory = category?.ToString() ?? string.Empty;
                System.Diagnostics.Debug.WriteLine($"FilterByCategory: SelectedMainCategory = '{SelectedMainCategory}'");

                // Reset subcategory when switching main category (unless still Fruit/Soda)
                if (!string.Equals(SelectedMainCategory, "Fruit/Soda", StringComparison.OrdinalIgnoreCase))
                    SelectedSubcategory = null;

                ApplyFilters();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FilterByCategory error: {ex.Message}");
                throw;
            }
        }

        [RelayCommand]
        private async Task FilterBySubcategory(object subcategory) // Filter products by subcategory
        {
            SelectedSubcategory = subcategory?.ToString() ?? string.Empty;
            ApplyFilters();
        }

        private void ApplyFilters() // Applies the selected filters to the product list
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"ApplyFilters: Products count = {Products?.Count ?? -1}");

                if (Products == null || !Products.Any())
                {
                    FilteredProducts?.Clear();
                    return;
                }

                var filteredSequence = ProductFilterService.Apply(Products, SelectedMainCategory, SelectedSubcategory);

                FilteredProducts?.Clear();
                if (FilteredProducts != null)
                {
                    foreach (var product in filteredSequence)
                        FilteredProducts.Add(product);
                }

                System.Diagnostics.Debug.WriteLine($"ApplyFilters: Final filtered count = {FilteredProducts?.Count ?? -1}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApplyFilters error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"ApplyFilters stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task LoadDataAsync() // Load Data
        {
            await RunWithLoading(async () =>
            {
                StatusMessage = "Loading products...";

                if (!EnsureInternetOrShowRetry(LoadDataAsync, "No internet connection detected. Please check your network settings and try again."))
                    return;

                try
                {
                var productList = await _database.GetProductsAsyncCached();
                    Products = new ObservableCollection<POSPageModel>(productList ?? new List<POSPageModel>());
                    FilteredProducts = new ObservableCollection<POSPageModel>(productList ?? new List<POSPageModel>());

                    // Check stock levels for all products
                    await CheckStockLevelsForAllProducts();

                    System.Diagnostics.Debug.WriteLine($"LoadDataAsync: Loaded {Products.Count} products");
                    foreach (var product in Products.Take(5))
                    {
                        System.Diagnostics.Debug.WriteLine($"Product: {product.ProductName}, Category: '{product.Category}', Subcategory: '{product.Subcategory}'");
                    }

                    StatusMessage = Products.Any()
                        ? "Products loaded successfully!"
                        : "No products found. Please add some products to the database.";
                }
                catch (Exception ex)
                {
                    HasError = true;
                    StatusMessage = $"Failed to load products: {ex.Message}";
                    GetRetryConnectionPopup()?.ShowRetryPopup(LoadDataAsync, $"Failed to load products: {ex.Message}");
                }
            });
        }
       

        [RelayCommand]
        private void SelectProduct(POSPageModel product) // Selects a product and loads its details
        {
            System.Diagnostics.Debug.WriteLine($"SelectProduct called with product: {product?.ProductName ?? "null"}");
            
            // Don't allow selection if product has low stock
            if (product?.IsLowStock == true)
            {
                System.Diagnostics.Debug.WriteLine($"Product {product.ProductName} has low stock, selection blocked");
                return;
            }
            
            SelectedProduct = product;
            System.Diagnostics.Debug.WriteLine($"🔧 SelectProduct: Set SelectedProduct to {product.ProductName} (ID: {product.ProductID})");

            AvailableSizes.Clear();
            if (product.HasSmall) AvailableSizes.Add("Small");
            if (product.HasMedium) AvailableSizes.Add("Medium");
            if (product.HasLarge) AvailableSizes.Add("Large");

            // Notify visibility change for cart size options
            OnPropertyChanged(nameof(IsSmallSizeVisibleInCart));
            OnPropertyChanged(nameof(IsMediumSizeVisibleInCart));
            OnPropertyChanged(nameof(IsLargeSizeVisibleInCart));

            // Force UI update for SelectedProduct
            OnPropertyChanged(nameof(SelectedProduct));

            // Load linked addons from DB for this product
            _ = LoadAddonsForSelectedAsync();
        }


        private async Task LoadAddonsForSelectedAsync() // Loads addons for the selected product from the database
        {
            if (SelectedProduct == null) 
            {
                System.Diagnostics.Debug.WriteLine($"🔧 LoadAddonsForSelectedAsync: SelectedProduct is null");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"🔧 LoadAddonsForSelectedAsync: Loading addons for product ID: {SelectedProduct.ProductID}, Name: {SelectedProduct.ProductName}");
            
            try
            {
                var addons = await _database.GetProductAddonsAsync(SelectedProduct.ProductID);
                System.Diagnostics.Debug.WriteLine($"🔧 Database returned {addons?.Count ?? 0} addons");
                
                SelectedProduct.InventoryItems.Clear();
                SelectedProduct.SmallAddons.Clear();
                SelectedProduct.MediumAddons.Clear();
                SelectedProduct.LargeAddons.Clear();
                
                if (addons != null && addons.Any())
                {
                    foreach (var a in addons)
                    {
                        // Default: addons unchecked and hidden until user selects
                        a.IsSelected = false;
                        a.AddonQuantity = 0;
                        // Keep the AddonPrice from database
                        a.AddonUnit = a.DefaultUnit;
                        System.Diagnostics.Debug.WriteLine($"🔧 Loaded addon: {a.itemName}, IsSelected: {a.IsSelected}, AddonQuantity: {a.AddonQuantity}, AddonPrice: {a.AddonPrice}");
                        
                        // Add to legacy InventoryItems for backward compatibility
                        SelectedProduct.InventoryItems.Add(a);
                        
                        // Also add to Small, Medium, and Large addon collections (for size-specific selection)
                        var smallAddonCopy = new InventoryPageModel
                        {
                            itemID = a.itemID,
                            itemName = a.itemName,
                            itemCategory = a.itemCategory,
                            itemDescription = a.itemDescription,
                            itemQuantity = a.itemQuantity,
                            unitOfMeasurement = a.unitOfMeasurement,
                            minimumQuantity = a.minimumQuantity,
                            ImageSet = a.ImageSet,
                            IsSelected = false,
                            AddonQuantity = 0,
                            AddonPrice = a.AddonPrice,
                            AddonUnit = a.AddonUnit
                        };
                        
                        var mediumAddonCopy = new InventoryPageModel
                        {
                            itemID = a.itemID,
                            itemName = a.itemName,
                            itemCategory = a.itemCategory,
                            itemDescription = a.itemDescription,
                            itemQuantity = a.itemQuantity,
                            unitOfMeasurement = a.unitOfMeasurement,
                            minimumQuantity = a.minimumQuantity,
                            ImageSet = a.ImageSet,
                            IsSelected = false,
                            AddonQuantity = 0,
                            AddonPrice = a.AddonPrice,
                            AddonUnit = a.AddonUnit
                        };
                        
                        var largeAddonCopy = new InventoryPageModel
                        {
                            itemID = a.itemID,
                            itemName = a.itemName,
                            itemCategory = a.itemCategory,
                            itemDescription = a.itemDescription,
                            itemQuantity = a.itemQuantity,
                            unitOfMeasurement = a.unitOfMeasurement,
                            minimumQuantity = a.minimumQuantity,
                            ImageSet = a.ImageSet,
                            IsSelected = false,
                            AddonQuantity = 0,
                            AddonPrice = a.AddonPrice,
                            AddonUnit = a.AddonUnit
                        };
                        
                        SelectedProduct.SmallAddons.Add(smallAddonCopy);
                        SelectedProduct.MediumAddons.Add(mediumAddonCopy);
                        SelectedProduct.LargeAddons.Add(largeAddonCopy);
                    }
                    System.Diagnostics.Debug.WriteLine($"🔧 Successfully loaded {addons.Count} addons for product: {SelectedProduct.ProductName}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"🔧 No addons found for product: {SelectedProduct.ProductName}");
                }
                
                // Force UI update
                OnPropertyChanged(nameof(SelectedProduct));
                OnPropertyChanged(nameof(SelectedProduct.InventoryItems));
                OnPropertyChanged(nameof(SelectedProduct.SmallAddons));
                OnPropertyChanged(nameof(SelectedProduct.MediumAddons));
                OnPropertyChanged(nameof(SelectedProduct.LargeAddons));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to load addons: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
            }
        }
        
        [RelayCommand]
        private void ToggleSmallAddonsDropdown()
        {
            if (SelectedProduct == null) return;
            SelectedProduct.IsSmallAddonsDropdownVisible = !SelectedProduct.IsSmallAddonsDropdownVisible;
            System.Diagnostics.Debug.WriteLine($"🔧 Small addons dropdown toggled: {SelectedProduct.IsSmallAddonsDropdownVisible}");
        }
        
        [RelayCommand]
        private void ToggleMediumAddonsDropdown()
        {
            if (SelectedProduct == null) return;
            SelectedProduct.IsMediumAddonsDropdownVisible = !SelectedProduct.IsMediumAddonsDropdownVisible;
            System.Diagnostics.Debug.WriteLine($"🔧 Medium addons dropdown toggled: {SelectedProduct.IsMediumAddonsDropdownVisible}");
        }
        
        [RelayCommand]
        private void ToggleLargeAddonsDropdown()
        {
            if (SelectedProduct == null) return;
            SelectedProduct.IsLargeAddonsDropdownVisible = !SelectedProduct.IsLargeAddonsDropdownVisible;
            System.Diagnostics.Debug.WriteLine($"🔧 Large addons dropdown toggled: {SelectedProduct.IsLargeAddonsDropdownVisible}");
        }

        
        [RelayCommand]
        private async Task EditProduct(POSPageModel product) // Opens the Edit Product panel
        {
            if (product == null) return;

            await AddItemToPOSViewModel.SetEditMode(product);
            SettingsPopup.OpenAddItemToPOSCommand.Execute(null);
        }

        
        [RelayCommand]
        private void RemoveFromCart(POSPageModel product) // Removes a product from the cart
        {
            if (product == null || CartItems == null) return;

            var itemInCart = CartItems.FirstOrDefault(p => p.ProductID == product.ProductID);
            if (itemInCart != null) CartItems.Remove(itemInCart);

            if (SelectedProduct?.ProductID == product.ProductID)
                SelectedProduct = null;

        // Persist cart update
        _ = _cartStorage.SaveCartAsync(CartItems);
        }


        public async Task CheckStockLevelsForAllProducts() // Checks stock levels for all products and marks low stock
        {
            try
            {
                // Clear any cached inventory data to ensure fresh reads
                _database.InvalidateInventoryCache();
                
                foreach (var product in Products)
                {
                    // Load core ingredients for the product (not addons) to validate sufficiency
                    var ingredients = await _database.GetProductIngredientsAsync(product.ProductID);

                    // If product has no ingredients, it's always available (no inventory requirements)
                    if (ingredients == null || !ingredients.Any())
                    {
                        System.Diagnostics.Debug.WriteLine($"Product {product.ProductName}: No ingredients, marking as available");
                        product.IsLowStock = false;
                        continue;
                    }

                    bool insufficientForRecipe = false;
                    foreach (var tuple in ingredients)
                    {
                        var item = tuple.item;
                        
                        // Get the MAXIMUM amount needed across all sizes (Small, Medium, Large)
                        // This ensures we have enough inventory for ANY size order
                        var maxAmount = Math.Max(item.InputAmountSmall, Math.Max(item.InputAmountMedium, item.InputAmountLarge));
                        var maxUnit = item.InputAmountLarge > 0 && item.InputAmountLarge >= maxAmount ? item.InputUnitLarge :
                                     item.InputAmountMedium > 0 && item.InputAmountMedium >= maxAmount ? item.InputUnitMedium :
                                     item.InputAmountSmall > 0 ? item.InputUnitSmall :
                                     item.InputUnitMedium; // If small is 0 (product without small size), prefer medium
                        
                        // If maxAmount is still 0, try to find any non-zero size
                        if (maxAmount <= 0)
                        {
                            if (item.InputAmountLarge > 0)
                            {
                                maxAmount = item.InputAmountLarge;
                                maxUnit = item.InputUnitLarge;
                            }
                            else if (item.InputAmountMedium > 0)
                            {
                                maxAmount = item.InputAmountMedium;
                                maxUnit = item.InputUnitMedium;
                            }
                            else
                            {
                                // All amounts are 0, fall back to database tuple
                                maxAmount = tuple.amount;
                                maxUnit = tuple.unit;
                                
                                // If tuple.amount is also 0, but we have a unit from medium/large, use that
                                if (maxAmount <= 0 && !string.IsNullOrWhiteSpace(item.InputUnitMedium) && item.InputUnitMedium != "pcs")
                                {
                                    maxUnit = item.InputUnitMedium;
                                }
                                else if (maxAmount <= 0 && !string.IsNullOrWhiteSpace(item.InputUnitLarge) && item.InputUnitLarge != "pcs")
                                {
                                    maxUnit = item.InputUnitLarge;
                                }
                            }
                        }
                        
                        // If amount is still 0 or negative, skip this ingredient (no requirement = sufficient)
                        if (maxAmount <= 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"Product {product.ProductName}, Ingredient {item.itemName}: No amount requirement (0 or negative), skipping check");
                            continue;
                        }
                        
                        // If unit is empty or default "pcs" but doesn't match inventory unit, try to use inventory unit
                        var inventoryUnit = UnitConversionService.Normalize(item.unitOfMeasurement);
                        var recipeUnit = UnitConversionService.Normalize(maxUnit);
                        
                        // If recipe unit is "pcs" but inventory is not, and we have compatible units, try to infer the correct unit
                        if (recipeUnit == "pcs" && inventoryUnit != "pcs" && !string.IsNullOrWhiteSpace(inventoryUnit))
                        {
                            // Check if there's a per-size unit that matches inventory better
                            if (!string.IsNullOrWhiteSpace(item.InputUnitLarge) && UnitConversionService.AreCompatibleUnits(item.InputUnitLarge, inventoryUnit))
                            {
                                maxUnit = item.InputUnitLarge;
                                recipeUnit = UnitConversionService.Normalize(maxUnit);
                            }
                            else if (!string.IsNullOrWhiteSpace(item.InputUnitMedium) && UnitConversionService.AreCompatibleUnits(item.InputUnitMedium, inventoryUnit))
                            {
                                maxUnit = item.InputUnitMedium;
                                recipeUnit = UnitConversionService.Normalize(maxUnit);
                            }
                            else if (!string.IsNullOrWhiteSpace(item.InputUnitSmall) && UnitConversionService.AreCompatibleUnits(item.InputUnitSmall, inventoryUnit))
                            {
                                maxUnit = item.InputUnitSmall;
                                recipeUnit = UnitConversionService.Normalize(maxUnit);
                            }
                            // If still incompatible, try using inventory unit directly (assume 1:1 conversion for now)
                            else if (UnitConversionService.AreCompatibleUnits("pcs", inventoryUnit))
                            {
                                // This shouldn't happen, but if it does, just proceed
                            }
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"Product {product.ProductName}, Ingredient {item.itemName}: Quantity={item.itemQuantity}, Max amount needed (across all sizes)={maxAmount} {maxUnit}");

                        double requiredInInventoryUnit;
                        if (string.IsNullOrWhiteSpace(inventoryUnit) || string.IsNullOrWhiteSpace(recipeUnit))
                        {
                            requiredInInventoryUnit = maxAmount;
                        }
                        else if (!UnitConversionService.AreCompatibleUnits(recipeUnit, inventoryUnit))
                        {
                            System.Diagnostics.Debug.WriteLine($"Product {product.ProductName}: Incompatible units - recipe: {recipeUnit} ({maxUnit}), inventory: {inventoryUnit} ({item.unitOfMeasurement})");
                            System.Diagnostics.Debug.WriteLine($"Product {product.ProductName}: Per-size units - Small: {item.InputUnitSmall}, Medium: {item.InputUnitMedium}, Large: {item.InputUnitLarge}");
                            insufficientForRecipe = true;
                            break;
                        }
                        else
                        {
                            // Convert both the required amount and available stock to a common unit for comparison
                            var commonUnit = UnitConversionService.GetCommonUnit(recipeUnit, inventoryUnit);
                            var requiredInCommonUnit = UnitConversionService.Convert(maxAmount, recipeUnit, commonUnit);
                            var availableInCommonUnit = UnitConversionService.Convert(item.itemQuantity, inventoryUnit, commonUnit);
                            
                            System.Diagnostics.Debug.WriteLine($"Product {product.ProductName}, Ingredient {item.itemName}: " +
                                $"Need {requiredInCommonUnit} {commonUnit} (max across all sizes: {maxAmount} {maxUnit}), " +
                                $"Have {availableInCommonUnit} {commonUnit} (from {item.itemQuantity} {inventoryUnit})");
                            
                            if (requiredInCommonUnit > availableInCommonUnit)
                            {
                                System.Diagnostics.Debug.WriteLine($"Product {product.ProductName}: Insufficient {item.itemName} - need {requiredInCommonUnit} {commonUnit}, have {availableInCommonUnit} {commonUnit}");
                                insufficientForRecipe = true;
                                break;
                            }
                            
                            // If we get here, we have enough of this ingredient for any size
                            continue;
                        }
                    }

                    // Check if cups and straws are available for at least one serving
                    // Only check if ingredients are sufficient
                    if (!insufficientForRecipe)
                    {
                        bool cupsStrawsInsufficient = await CheckCupsAndStrawsAvailability();
                        System.Diagnostics.Debug.WriteLine($"Product {product.ProductName}: Cups/Straws insufficient = {cupsStrawsInsufficient}");
                        insufficientForRecipe = cupsStrawsInsufficient;
                    }

                    // Rule: If inventory for each required ingredient is enough for ONE serving, product is available.
                    // Ignore minimum thresholds for availability; they can be surfaced elsewhere as warnings.
                    System.Diagnostics.Debug.WriteLine($"Product {product.ProductName}: Final IsLowStock = {insufficientForRecipe}");
                    product.IsLowStock = insufficientForRecipe;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking stock levels: {ex.Message}");
            }
        }

        private async Task<bool> CheckCupsAndStrawsAvailability() // Checks if cups and straws are available in inventory - returns TRUE if INSUFFICIENT
        {
            try
            {
                // Check if we have at least one of each cup size and straw
                var smallCup = await _database.GetInventoryItemByNameCachedAsync("Small Cup");
                var mediumCup = await _database.GetInventoryItemByNameCachedAsync("Medium Cup");
                var largeCup = await _database.GetInventoryItemByNameCachedAsync("Large Cup");
                var straw = await _database.GetInventoryItemByNameCachedAsync("Straw");

                System.Diagnostics.Debug.WriteLine($"Cup/Straw Check - Small: {smallCup?.itemQuantity ?? 0}, Medium: {mediumCup?.itemQuantity ?? 0}, Large: {largeCup?.itemQuantity ?? 0}, Straw: {straw?.itemQuantity ?? 0}");

                // Check if we have at least one cup (any size) AND at least one straw
                bool hasCupsAndStraws = (smallCup?.itemQuantity > 0 || mediumCup?.itemQuantity > 0 || largeCup?.itemQuantity > 0) && 
                                       (straw?.itemQuantity > 0);

                System.Diagnostics.Debug.WriteLine($"Has cups and straws: {hasCupsAndStraws}, Returning insufficient: {!hasCupsAndStraws}");

                // Return TRUE if insufficient (i.e., if we DON'T have cups and straws)
                return !hasCupsAndStraws;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking cups and straws availability: {ex.Message}");
                return true; // Conservative approach - mark as unavailable if we can't check
            }
        }


        [RelayCommand]
        private void IncreaseSmallQty() 
        { 
            if (SelectedProduct != null) 
                SelectedProduct.SmallQuantity++; 
        }

        [RelayCommand]
        private void DecreaseSmallQty() 
        { 
            if (SelectedProduct != null && SelectedProduct.SmallQuantity > 0) 
                SelectedProduct.SmallQuantity--; 
        }

        [RelayCommand]
        private void IncreaseMediumQty() 
        { 
            if (SelectedProduct != null) 
                SelectedProduct.MediumQuantity++; 
        }

        [RelayCommand]
        private void DecreaseMediumQty() 
        { 
            if (SelectedProduct != null && SelectedProduct.MediumQuantity > 0) 
                SelectedProduct.MediumQuantity--; 
        }

        [RelayCommand]
        private void IncreaseLargeQty() 
        { 
            if (SelectedProduct != null) 
                SelectedProduct.LargeQuantity++; 
        }

        [RelayCommand]
        private void DecreaseLargeQty() 
        { 
            if (SelectedProduct != null && SelectedProduct.LargeQuantity > 0) 
                SelectedProduct.LargeQuantity--; 
        }


        [RelayCommand]
        private void ShowCart() // Opens the cart popup with current cart items
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🛒 ShowCart called with {CartItems?.Count ?? 0} items");
                
                if (CartItems != null && CartItems.Any())
                {
                    foreach (var item in CartItems)
                    {
                        System.Diagnostics.Debug.WriteLine($"🛒 Cart item: {item.ProductName}, Small: {item.SmallQuantity}, Medium: {item.MediumQuantity}, Large: {item.LargeQuantity}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("🛒 Cart is empty or null");
                }
                
                if (CartPopup == null)
                {
                    System.Diagnostics.Debug.WriteLine("❌ CartPopup is null!");
                    if (NotificationPopup != null)
                    {
                        NotificationPopup.ShowNotification("Cart is not available", "Error");
                    }
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine("🛒 Calling CartPopup.ShowCart");
                CartPopup.ShowCart(CartItems);
                System.Diagnostics.Debug.WriteLine("✅ CartPopup.ShowCart completed successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error in ShowCart: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                
                if (NotificationPopup != null)
                {
                    NotificationPopup.ShowNotification($"Failed to open cart: {ex.Message}", "Error");
                }
            }
        }

    public Task SaveCartToStorageAsync() => _cartStorage.SaveCartAsync(CartItems); // Saves the current cart to persistent storage
    public async Task LoadCartFromStorageAsync() // Loads the cart from persistent storage
        {
        try
        {
            var loaded = await _cartStorage.LoadCartAsync();
            CartItems = loaded ?? new ObservableCollection<POSPageModel>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ LoadCartFromStorageAsync failed: {ex.Message}");
        }
    }

    public async Task ClearCartAsync() // Clears the cart both in memory and persistent storage
        {
        try
        {
            System.Diagnostics.Debug.WriteLine("🧹 Clearing cart in POSPageViewModel");
            CartItems.Clear();
            await _cartStorage.SaveCartAsync(new ObservableCollection<POSPageModel>());
            
            // Also clear and close the cart popup if it's open
            if (CartPopup != null)
            {
                CartPopup.ClearCart(); // Clear the cart popup items
                CartPopup.IsCartVisible = false; // Close the cart popup
                System.Diagnostics.Debug.WriteLine("✅ Cart popup cleared and closed");
            }
            
            System.Diagnostics.Debug.WriteLine("✅ Cart cleared successfully in POSPageViewModel");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error clearing cart: {ex.Message}");
        }
    }


        [RelayCommand] 
        private async Task ShowHistory() // Opens the transaction history popup
        {
            System.Diagnostics.Debug.WriteLine("ShowHistory command called");

            // Use shared transactions populated from checkout
            var app = (App)Application.Current;
            var transactions = app?.Transactions ?? new ObservableCollection<TransactionHistoryModel>();
            await HistoryPopup.ShowHistory(transactions);
            System.Diagnostics.Debug.WriteLine($"HistoryPopup.IsHistoryVisible: {HistoryPopup.IsHistoryVisible}");
        }

        [RelayCommand]
        private void Cart() // Opens the cart popup
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Cart command executed");
                ShowCart();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Cart command: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Show error notification if available
                if (NotificationPopup != null)
                {
                    NotificationPopup.ShowNotification($"Cart error: {ex.Message}", "Error");
                }
            }
        }

        [RelayCommand]
        private void ShowProfile() // Open the profile popup
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("ShowProfile command executed");
                var app = (App)Application.Current;
                if (app?.ProfilePopup != null)
                {
                    app.ProfilePopup.ShowProfile();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ProfilePopup is null!");
                    if (NotificationPopup != null)
                    {
                        NotificationPopup.ShowNotification("Profile is not available", "Error");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ShowProfile command: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                if (NotificationPopup != null)
                {
                    NotificationPopup.ShowNotification($"Profile error: {ex.Message}", "Error");
                }
            }
        }

        [RelayCommand]
        private void ForceCloseUpdateInputPopup()
        {
            System.Diagnostics.Debug.WriteLine("🔧 ForceCloseUpdateInputPopup called from POSPageViewModel");
            if (AddItemToPOSViewModel?.ConnectPOSToInventoryVM != null)
            {
                AddItemToPOSViewModel.ConnectPOSToInventoryVM.IsInputIngredientsVisible = false;
                AddItemToPOSViewModel.ConnectPOSToInventoryVM.IsEditMode = false;
                AddItemToPOSViewModel.ConnectPOSToInventoryVM.IsUpdateAmountsMode = false;
                AddItemToPOSViewModel.ConnectPOSToInventoryVM.IsPreviewVisible = false;
                AddItemToPOSViewModel.ConnectPOSToInventoryVM.IsConnectPOSToInventoryVisible = false;
            }
        }

        [RelayCommand]
        private void ManagePOS() // Open the Manage POS Options panel
        {
            System.Diagnostics.Debug.WriteLine("ManagePOS called");
            
            // Check if user is admin OR has been granted POS access
            var currentUser = App.CurrentUser;
            bool hasAccess = (currentUser?.IsAdmin ?? false) || (currentUser?.CanAccessPOS ?? false);
            
            if (!hasAccess)
            {
                Application.Current?.MainPage?.DisplayAlert("Access Denied", 
                    "You don't have permission to manage POS menu. Please contact an administrator.", "OK");
                return;
            }
            
            // Get the ManagePOSOptionsViewModel from the app
            var managePOSPopup = ((App)Application.Current).ManagePOSPopup;
            if (managePOSPopup != null)
            {
                System.Diagnostics.Debug.WriteLine($"Setting ManagePOSPopup visibility to true. Current value: {managePOSPopup.IsPOSManagementPopupVisible}");
                managePOSPopup.IsPOSManagementPopupVisible = true;
                System.Diagnostics.Debug.WriteLine($"ManagePOSPopup visibility set to: {managePOSPopup.IsPOSManagementPopupVisible}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ManagePOSPopup is null");
            }
        }
        
        public async Task InitializeAsync() // Initializes the ViewModel, loading user info and persisted cart
        {
            if (App.CurrentUser != null)
                IsAdmin = App.CurrentUser.IsAdmin;

            await LoadDataAsync();

            // Load persisted cart
            var loadedCart = await _cartStorage.LoadCartAsync();
            if (loadedCart != null && loadedCart.Any())
            {
                CartItems = loadedCart;
            }
        }

        // Helper method to check if two addon collections are identical
        private bool AreAddonsIdentical(ObservableCollection<InventoryPageModel> addons1, ObservableCollection<InventoryPageModel> addons2)
        {
            if (addons1 == null && addons2 == null) return true;
            if (addons1 == null || addons2 == null) return false;
            if (addons1.Count != addons2.Count) return false;
            
            // Get selected addons with quantities from both collections
            var selected1 = addons1.Where(a => a != null && a.IsSelected && a.AddonQuantity > 0)
                .OrderBy(a => a.itemID)
                .Select(a => new { a.itemID, a.AddonQuantity })
                .ToList();
            var selected2 = addons2.Where(a => a != null && a.IsSelected && a.AddonQuantity > 0)
                .OrderBy(a => a.itemID)
                .Select(a => new { a.itemID, a.AddonQuantity })
                .ToList();
            
            if (selected1.Count != selected2.Count) return false;
            
            for (int i = 0; i < selected1.Count; i++)
            {
                if (selected1[i].itemID != selected2[i].itemID || selected1[i].AddonQuantity != selected2[i].AddonQuantity)
                    return false;
            }
            
            return true;
        }
    }
}
