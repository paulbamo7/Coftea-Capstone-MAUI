using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Coftea_Capstone.Models;
using System.Collections.ObjectModel;

namespace Coftea_Capstone.ViewModel.Controls
{
    public partial class PaymentMethodProductsPopupViewModel : ObservableObject
    {
        private readonly Database _database = new Database();

        [ObservableProperty]
        private bool isVisible;

        [ObservableProperty]
        private string paymentMethod = string.Empty;

        [ObservableProperty]
        private string periodText = string.Empty;

        [ObservableProperty]
        private ObservableCollection<PaymentMethodProductItem> products = new();

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private decimal totalRevenue;

        public event Func<string, Task>? OnViewProduct;

        [RelayCommand]
        private async Task ViewProduct(string productName)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔵 ViewProduct command called with product: {productName}");
                if (OnViewProduct != null)
                {
                    System.Diagnostics.Debug.WriteLine($"🔵 OnViewProduct event handler is not null, invoking...");
                    await OnViewProduct(productName);
                    System.Diagnostics.Debug.WriteLine($"✅ OnViewProduct event handler completed");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ OnViewProduct event handler is null!");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error in ViewProduct command: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
            }
        }

        [RelayCommand]
        private void Close()
        {
            IsVisible = false;
            Products.Clear();
        }

        public async Task LoadProductsAsync(string paymentMethod, DateTime startDate, DateTime endDate, string periodText)
        {
            try
            {
                IsLoading = true;
                PaymentMethod = paymentMethod;
                PeriodText = periodText;
                Products.Clear();
                TotalRevenue = 0;

                var productData = await _database.GetProductsByPaymentMethodAsync(paymentMethod, startDate, endDate);

                var groupedProducts = productData
                    .GroupBy(p => new { p.ProductName, p.Size })
                    .Select(g => new PaymentMethodProductItem
                    {
                        ProductName = g.Key.ProductName,
                        Size = g.Key.Size ?? "N/A",
                        TotalQuantity = g.Sum(p => p.Quantity),
                        TotalRevenue = g.Sum(p => p.Revenue)
                    })
                    .OrderByDescending(p => p.TotalRevenue)
                    .ToList();

                foreach (var product in groupedProducts)
                {
                    Products.Add(product);
                    TotalRevenue += product.TotalRevenue;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading payment method products: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    public class PaymentMethodProductItem : ObservableObject
    {
        public string ProductName { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public decimal TotalRevenue { get; set; }
        public string RevenueDisplay => $"₱{TotalRevenue:N2}";
    }
}

