using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Coftea_Capstone.Models;
using Coftea_Capstone.C_;

namespace Coftea_Capstone.ViewModel.Controls
{
    public class IngredientDisplayModel
    {
        public string IngredientName { get; set; }
        public double Amount { get; set; }
        public string Unit { get; set; }
    }

    public class AddonDisplayModel
    {
        public string AddonName { get; set; }
        public int Quantity { get; set; } // Addon quantity per drink
        public double Amount { get; set; } // Amount per serving (from product_addons)
        public string Unit { get; set; } // UoM per serving (from product_addons)
        public int ProductQuantity { get; set; } // Number of drinks (not used in display, only for calculations if needed)
    }

    public class ProcessingItemModel
    {
        public int? Id { get; set; } // For database persistence
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public string Size { get; set; } // Small/Medium/Large/None
        public int Quantity { get; set; }
        public List<AddonDisplayModel> Addons { get; set; } = new();
        public decimal UnitPrice { get; set; }
        public decimal AddonPrice { get; set; }
        public POSPageModel Source { get; set; }
        public List<IngredientDisplayModel> Ingredients { get; set; } = new();
        public string SizeDisplay { get; set; } // e.g., "16oz" or "18oz"
    }

    public partial class ProcessingQueuePopupViewModel : ObservableObject
    {
        private readonly Database _database = new Database();

        [ObservableProperty]
        private bool isVisible;

        [ObservableProperty]
        private ObservableCollection<ProcessingItemModel> pendingItems = new();

        public int QueueCount => PendingItems?.Count ?? 0;
        
        public bool HasItems => QueueCount > 0;

        partial void OnPendingItemsChanged(ObservableCollection<ProcessingItemModel> value)
        {
            OnPropertyChanged(nameof(QueueCount));
            OnPropertyChanged(nameof(HasItems));
        }

        [RelayCommand]
        public void Show()
        {
            IsVisible = true;
        }

        [RelayCommand]
        public void Close()
        {
            IsVisible = false;
        }

        public async Task EnqueueFromCartItem(POSPageModel cartItem)
        {
            if (cartItem == null) return;

            // Fetch addon details from database
            var addonsList = await _database.GetProductAddonsAsync(cartItem.ProductID);
            var addonsDict = addonsList.ToDictionary(a => a.itemID, a => a);

            // Fetch ingredients for this product
            var ingredientsList = await _database.GetProductIngredientsAsync(cartItem.ProductID);
            var mainIngredients = ingredientsList.Where(i => i.role == "ingredient").ToList();

            void enqueue(string size, int qty, decimal unitPrice, string sizeDisplay, ObservableCollection<InventoryPageModel> sizeAddons)
            {
                if (qty <= 0) return;

                // Get ingredients for this size
                var sizeIngredients = new List<IngredientDisplayModel>();
                foreach (var (item, _, _, role) in mainIngredients)
                {
                    double amount = 0;
                    string unit = "";
                    
                    if (size == "Small" && item.InputAmountSmall > 0)
                    {
                        amount = item.InputAmountSmall;
                        unit = item.InputUnitSmall ?? item.unitOfMeasurement ?? "pcs";
                    }
                    else if (size == "Medium" && item.InputAmountMedium > 0)
                    {
                        amount = item.InputAmountMedium;
                        unit = item.InputUnitMedium ?? item.unitOfMeasurement ?? "pcs";
                    }
                    else if (size == "Large" && item.InputAmountLarge > 0)
                    {
                        amount = item.InputAmountLarge;
                        unit = item.InputUnitLarge ?? item.unitOfMeasurement ?? "pcs";
                    }

                    if (amount > 0)
                    {
                        sizeIngredients.Add(new IngredientDisplayModel
                        {
                            IngredientName = item.itemName,
                            Amount = amount,
                            Unit = unit
                        });
                    }
                }

                // Get addons for this specific size only
                var selectedSizeAddons = sizeAddons?.Where(a => a != null && a.IsSelected && a.AddonQuantity > 0).ToList() ?? new();
                var addOnTotal = selectedSizeAddons.Sum(a => (decimal)(a.AddonPrice * a.AddonQuantity * qty));

                // Build addons list with per-size amount and UoM
                var addonsListForItem = new List<AddonDisplayModel>();
                foreach (var addon in selectedSizeAddons)
                {
                    double amountPerServing = 0;
                    string unit = "pcs";
                    
                    if (addonsDict.ContainsKey(addon.itemID))
                    {
                        var addonInfo = addonsDict[addon.itemID];
                        // Use per-size addon amounts
                        if (size == "Small")
                        {
                            amountPerServing = addonInfo.InputAmountSmall > 0 
                                ? addonInfo.InputAmountSmall 
                                : (addonInfo.InputAmount > 0 ? addonInfo.InputAmount : 0);
                            unit = !string.IsNullOrWhiteSpace(addonInfo.InputUnitSmall) 
                                ? addonInfo.InputUnitSmall 
                                : (!string.IsNullOrWhiteSpace(addonInfo.InputUnit) 
                                    ? addonInfo.InputUnit 
                                    : (addonInfo.unitOfMeasurement ?? "pcs"));
                        }
                        else if (size == "Medium")
                        {
                            amountPerServing = addonInfo.InputAmountMedium > 0 
                                ? addonInfo.InputAmountMedium 
                                : (addonInfo.InputAmount > 0 ? addonInfo.InputAmount : 0);
                            unit = !string.IsNullOrWhiteSpace(addonInfo.InputUnitMedium) 
                                ? addonInfo.InputUnitMedium 
                                : (!string.IsNullOrWhiteSpace(addonInfo.InputUnit) 
                                    ? addonInfo.InputUnit 
                                    : (addonInfo.unitOfMeasurement ?? "pcs"));
                        }
                        else if (size == "Large")
                        {
                            amountPerServing = addonInfo.InputAmountLarge > 0 
                                ? addonInfo.InputAmountLarge 
                                : (addonInfo.InputAmount > 0 ? addonInfo.InputAmount : 0);
                            unit = !string.IsNullOrWhiteSpace(addonInfo.InputUnitLarge) 
                                ? addonInfo.InputUnitLarge 
                                : (!string.IsNullOrWhiteSpace(addonInfo.InputUnit) 
                                    ? addonInfo.InputUnit 
                                    : (addonInfo.unitOfMeasurement ?? "pcs"));
                        }
                    }
                    else
                    {
                        // Fallback if addon not found in database
                        amountPerServing = addon.InputAmount > 0 ? addon.InputAmount : 0;
                        unit = !string.IsNullOrWhiteSpace(addon.InputUnit) 
                            ? addon.InputUnit 
                            : (addon.unitOfMeasurement ?? "pcs");
                    }
                    
                    addonsListForItem.Add(new AddonDisplayModel
                    {
                        AddonName = addon.itemName,
                        Quantity = addon.AddonQuantity, // Number of addons per drink
                        Amount = amountPerServing, // Amount per serving (from product_addons)
                        Unit = unit, // UoM per serving (from product_addons)
                        ProductQuantity = qty // Number of drinks for this size
                    });
                }

                var processingItem = new ProcessingItemModel
                {
                    ProductID = cartItem.ProductID,
                    ProductName = cartItem.ProductName,
                    Size = size,
                    SizeDisplay = sizeDisplay,
                    Quantity = qty,
                    Addons = addonsListForItem,
                    AddonPrice = addOnTotal,
                    UnitPrice = unitPrice,
                    Source = cartItem,
                    Ingredients = sizeIngredients
                };

                PendingItems.Add(processingItem);
                
                // Notify property changes for queue count
                OnPropertyChanged(nameof(QueueCount));
                OnPropertyChanged(nameof(HasItems));
                
                // Save to database for persistence
                _ = SaveProcessingItemAsync(processingItem);
            }

            enqueue("Small", cartItem.SmallQuantity, cartItem.SmallPrice ?? 0, "16oz", cartItem.SmallAddons);
            enqueue("Medium", cartItem.MediumQuantity, cartItem.MediumPrice, "18oz", cartItem.MediumAddons);
            enqueue("Large", cartItem.LargeQuantity, cartItem.LargePrice, "22oz", cartItem.LargeAddons);
        }

        public async Task LoadPendingItemsAsync()
        {
            try
            {
                var items = await _database.GetPendingProcessingItemsAsync();
                PendingItems.Clear();
                foreach (var item in items)
                {
                    PendingItems.Add(item);
                }
                // Notify property changes for queue count
                OnPropertyChanged(nameof(QueueCount));
                OnPropertyChanged(nameof(HasItems));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading pending items: {ex.Message}");
            }
        }

        private async Task SaveProcessingItemAsync(ProcessingItemModel item)
        {
            try
            {
                item.Id = await _database.SaveProcessingItemAsync(item);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error saving processing item: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task CompleteItem(ProcessingItemModel item)
        {
            if (item == null) return;

            try
            {
                var total = (item.UnitPrice * item.Quantity) + item.AddonPrice;
                var tx = new TransactionHistoryModel
                {
                    DrinkName = item.ProductName,
                    Quantity = item.Quantity,
                    Price = item.UnitPrice,
                    SmallPrice = item.Size == "Small" ? item.UnitPrice : 0,
                    MediumPrice = item.Size == "Medium" ? item.UnitPrice : 0,
                    LargePrice = item.Size == "Large" ? item.UnitPrice : 0,
                    AddonPrice = item.AddonPrice,
                    Total = total,
                    Size = item.Size,
                    AddOns = string.Join(", ", item.Addons.Select(a => $"{a.AddonName} x{a.Quantity}")),
                    PaymentMethod = "Cash",
                    Status = "Completed",
                    TransactionDate = DateTime.Now
                };

                await _database.SaveTransactionAsync(tx);

                // Delete from processing queue
                if (item.Id.HasValue)
                {
                    await _database.DeleteProcessingItemAsync(item.Id.Value);
                }

                PendingItems.Remove(item);
                if (!PendingItems.Any())
                    IsVisible = false;
                // Notify property changes for queue count
                OnPropertyChanged(nameof(QueueCount));
                OnPropertyChanged(nameof(HasItems));
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to save completed item: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        public async Task AcceptAll()
        {
            if (PendingItems == null || !PendingItems.Any())
            {
                return;
            }

            try
            {
                // Get a copy of all items to avoid collection modification during iteration
                var itemsToComplete = PendingItems.ToList();

                foreach (var item in itemsToComplete)
                {
                    await CompleteItem(item);
                }

                // Close the popup if all items are completed
                if (!PendingItems.Any())
                {
                    IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to complete all items: {ex.Message}", "OK");
            }
        }
    }
}


