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
    public class ProcessingItemModel
    {
        public string ProductName { get; set; }
        public string Size { get; set; } // Small/Medium/Large/None
        public int Quantity { get; set; }
        public string AddOnsDisplay { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal AddonPrice { get; set; }
        public POSPageModel Source { get; set; }
    }

    public partial class ProcessingQueuePopupViewModel : ObservableObject
    {
        private readonly Database _database = new Database();

        [ObservableProperty]
        private bool isVisible;

        [ObservableProperty]
        private ObservableCollection<ProcessingItemModel> pendingItems = new();

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

        public void EnqueueFromCartItem(POSPageModel cartItem)
        {
            if (cartItem == null) return;

            // Build add-ons text and total addon price
            var selectedAddons = cartItem.InventoryItems?.Where(a => a.IsSelected && a.AddonQuantity > 0).ToList() ?? new();
            var addOnText = selectedAddons.Any()
                ? string.Join(", ", selectedAddons.Select(a => $"{a.itemName} x{a.AddonQuantity}"))
                : "No add-ons";
            var addOnTotal = selectedAddons.Sum(a => (decimal)(a.AddonPrice * a.AddonQuantity));

            void enqueue(string size, int qty, decimal unitPrice)
            {
                if (qty <= 0) return;
                PendingItems.Add(new ProcessingItemModel
                {
                    ProductName = cartItem.ProductName,
                    Size = size,
                    Quantity = qty,
                    AddOnsDisplay = addOnText,
                    AddonPrice = addOnTotal,
                    UnitPrice = unitPrice,
                    Source = cartItem
                });
            }

            enqueue("Small", cartItem.SmallQuantity, cartItem.SmallPrice ?? 0);
            enqueue("Medium", cartItem.MediumQuantity, cartItem.MediumPrice);
            enqueue("Large", cartItem.LargeQuantity, cartItem.LargePrice);
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
                    AddOns = item.AddOnsDisplay,
                    PaymentMethod = "Cash",
                    Status = "Completed",
                    TransactionDate = DateTime.Now
                };

                await _database.SaveTransactionAsync(tx);

                PendingItems.Remove(item);
                if (!PendingItems.Any())
                    IsVisible = false;
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to save completed item: {ex.Message}", "OK");
            }
        }
    }
}


