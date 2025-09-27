using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Coftea_Capstone.Models;
using Coftea_Capstone.C_;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace Coftea_Capstone.ViewModel.Controls
{
    public partial class CartPopupViewModel : ObservableObject
    {
        [ObservableProperty] 
        private bool isCartVisible = false;

        [ObservableProperty] 
        private ObservableCollection<POSPageModel> cartItems = new();

        public CartPopupViewModel()
        {
        }

        public void ShowCart(ObservableCollection<POSPageModel> items)
        {
            CartItems = items ?? new ObservableCollection<POSPageModel>();
            IsCartVisible = true;
        }

        [RelayCommand]
        private void CloseCart()
        {
            IsCartVisible = false;
        }

        [RelayCommand]
        private void EditCartItem(POSPageModel item)
        {
            if (item == null) return;
            
            // TODO: Implement edit functionality
            // This could open another popup or navigate to edit page
        }

        [RelayCommand]
        private async Task Checkout()
        {
            if (CartItems == null || !CartItems.Any())
                return;

            // Compute total (Small and Large quantities supported in POSPageModel)
            decimal total = 0m;
            foreach (var item in CartItems)
            {
                var smallSubtotal = item.SmallPrice * item.SmallQuantity;
                var largeSubtotal = item.LargePrice * item.LargeQuantity;
                total += smallSubtotal + largeSubtotal;
            }

            // Confirm checkout
            bool confirm = await Application.Current.MainPage.DisplayAlert(
                "Checkout",
                $"Proceed to simulate payment for total ₱{total:F2}?",
                "Pay",
                "Cancel");

            if (!confirm)
                return;

            // Simulate processing delay
            await Task.Delay(800);

            // Save transactions to database and shared store
            var app = (App)Application.Current;
            var transactions = app?.Transactions;
            var database = new Coftea_Capstone.C_.Database(
                host: "192.168.1.4",
                database: "coftea_db",
                user: "maui",
                password: "password123"
            );

            if (transactions != null)
            {
                int nextId = (transactions.Count > 0 ? transactions.Max(t => t.TransactionId) : 0) + 1;

                foreach (var item in CartItems.ToList())
                {
                    if (item.SmallQuantity > 0)
                    {
                        var transaction = new TransactionHistoryModel
                        {
                            TransactionId = nextId++,
                            DrinkName = item.ProductName,
                            Size = "Small",
                            Quantity = item.SmallQuantity,
                            Price = item.SmallPrice,
                            Vat = 0m,
                            Total = item.SmallPrice * item.SmallQuantity,
                            AddOns = string.Empty,
                            CustomerName = string.Empty,
                            PaymentMethod = "Simulated",
                            Status = "Completed",
                            TransactionDate = DateTime.Now
                        };

                        // Save to database
                        try
                        {
                            await database.SaveTransactionAsync(transaction);
                        }
                        catch (Exception ex)
                        {
                            // Log error but continue with in-memory storage
                            System.Diagnostics.Debug.WriteLine($"Failed to save transaction to database: {ex.Message}");
                        }

                        // Also add to in-memory store for immediate UI updates
                        transactions.Add(transaction);
                    }
                    if (item.LargeQuantity > 0)
                    {
                        var transaction = new TransactionHistoryModel
                        {
                            TransactionId = nextId++,
                            DrinkName = item.ProductName,
                            Size = "Large",
                            Quantity = item.LargeQuantity,
                            Price = item.LargePrice,
                            Vat = 0m,
                            Total = item.LargePrice * item.LargeQuantity,
                            AddOns = string.Empty,
                            CustomerName = string.Empty,
                            PaymentMethod = "Simulated",
                            Status = "Completed",
                            TransactionDate = DateTime.Now
                        };

                        // Save to database
                        try
                        {
                            await database.SaveTransactionAsync(transaction);
                        }
                        catch (Exception ex)
                        {
                            // Log error but continue with in-memory storage
                            System.Diagnostics.Debug.WriteLine($"Failed to save transaction to database: {ex.Message}");
                        }

                        // Also add to in-memory store for immediate UI updates
                        transactions.Add(transaction);
                    }
                }
            }

            // Success: clear cart and hide popup
            CartItems.Clear();
            IsCartVisible = false;

            // Notify success if NotificationPopup is available via App POSVM
            var notification = app?.NotificationPopup;
            notification?.ShowNotification("Payment successful. Receipt generated.", "Checkout Complete");
        }
    }
}
