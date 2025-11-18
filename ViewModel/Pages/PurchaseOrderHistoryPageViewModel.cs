using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Coftea_Capstone.Models;

namespace Coftea_Capstone.ViewModel.Pages
{
    public partial class PurchaseOrderHistoryPageViewModel : ObservableObject
    {
        private readonly Database _database = new();

        [ObservableProperty]
        private ObservableCollection<PurchaseOrderHistoryItem> purchaseOrders = new();

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        private string selectedStatusFilter = "All";

        public List<string> StatusFilters { get; } = new() { "All", "Pending", "Approved", "Rejected", "Completed" };

        [ObservableProperty]
        private int currentPage = 1;

        [ObservableProperty]
        private int pageSize = 15;

        [ObservableProperty]
        private int totalPages = 1;

        [ObservableProperty]
        private int totalOrders = 0;

        public bool HasNextPage => CurrentPage < TotalPages;
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasMultiplePages => TotalPages > 1;

        private List<PurchaseOrderHistoryItem> _allOrders = new();

        public PurchaseOrderHistoryPageViewModel()
        {
        }

        [RelayCommand]
        public async Task LoadPurchaseOrdersAsync()
        {
            try
            {
                IsLoading = true;
                
                // Get all purchase orders (increase limit for history)
                var orders = await _database.GetAllPurchaseOrdersAsync(500);
                
                // Convert to history items with items preview
                var historyItems = new List<PurchaseOrderHistoryItem>();
                foreach (var order in orders)
                {
                    var items = await _database.GetPurchaseOrderItemsAsync(order.PurchaseOrderId);
                    var itemsPreview = items.Count > 0
                        ? string.Join(", ", items.Take(3).Select(i => $"{i.ItemName} ({i.RequestedQuantity} {i.UnitOfMeasurement})"))
                        : "No items";
                    
                    if (items.Count > 3)
                        itemsPreview += $" and {items.Count - 3} more...";

                    historyItems.Add(new PurchaseOrderHistoryItem
                    {
                        PurchaseOrderId = order.PurchaseOrderId,
                        OrderDate = order.OrderDate,
                        SupplierName = order.SupplierName,
                        Status = order.Status,
                        RequestedBy = order.RequestedBy,
                        ApprovedBy = order.ApprovedBy,
                        ApprovedDate = order.ApprovedDate,
                        TotalAmount = order.TotalAmount,
                        CreatedAt = order.CreatedAt,
                        Notes = order.Notes,
                        ItemsPreview = itemsPreview,
                        ItemCount = items.Count
                    });
                }

                _allOrders = historyItems;
                ApplyFilters();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading purchase orders: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to load purchase orders: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            ApplyFilters();
        }

        partial void OnSelectedStatusFilterChanged(string value)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            var filtered = _allOrders.AsEnumerable();

            // Apply status filter
            if (SelectedStatusFilter != "All")
            {
                filtered = filtered.Where(o => o.Status == SelectedStatusFilter);
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLowerInvariant();
                filtered = filtered.Where(o =>
                    o.PurchaseOrderId.ToString().Contains(searchLower) ||
                    o.RequestedBy.ToLowerInvariant().Contains(searchLower) ||
                    o.SupplierName.ToLowerInvariant().Contains(searchLower) ||
                    o.ItemsPreview.ToLowerInvariant().Contains(searchLower));
            }

            // Apply pagination
            TotalOrders = filtered.Count();
            TotalPages = Math.Max(1, (int)Math.Ceiling(TotalOrders / (double)PageSize));
            
            if (CurrentPage > TotalPages)
                CurrentPage = 1;

            var startIndex = (CurrentPage - 1) * PageSize;
            var pagedOrders = filtered.Skip(startIndex).Take(PageSize).ToList();

            PurchaseOrders.Clear();
            foreach (var order in pagedOrders)
            {
                PurchaseOrders.Add(order);
            }

            OnPropertyChanged(nameof(HasNextPage));
            OnPropertyChanged(nameof(HasPreviousPage));
            OnPropertyChanged(nameof(HasMultiplePages));
        }

        [RelayCommand]
        private void GoToNextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                ApplyFilters();
            }
        }

        [RelayCommand]
        private void GoToPreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                ApplyFilters();
            }
        }

        [RelayCommand]
        private void GoToFirstPage()
        {
            CurrentPage = 1;
            ApplyFilters();
        }

        [RelayCommand]
        private async Task ViewOrderDetails(PurchaseOrderHistoryItem order)
        {
            try
            {
                var items = await _database.GetPurchaseOrderItemsAsync(order.PurchaseOrderId);
                var itemsList = string.Join("\n", items.Select(i => 
                    $"• {i.ItemName}: {i.RequestedQuantity} {i.UnitOfMeasurement} (Requested) / {i.ApprovedQuantity} {i.UnitOfMeasurement} (Approved)"));

                var statusColor = order.Status switch
                {
                    "Approved" => "✅",
                    "Rejected" => "❌",
                    "Pending" => "⏳",
                    "Completed" => "✅",
                    _ => "📋"
                };

                await Application.Current.MainPage.DisplayAlert(
                    $"{statusColor} Purchase Order #{order.PurchaseOrderId}",
                    $"Status: {order.Status}\n" +
                    $"Date: {order.OrderDate:MMM dd, yyyy}\n" +
                    $"Requested By: {order.RequestedBy}\n" +
                    $"Supplier: {order.SupplierName}\n" +
                    $"Total Amount: ₱{order.TotalAmount:F2}\n" +
                    $"Items ({order.ItemCount}):\n{itemsList}",
                    "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to load order details: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task ExportPDF(PurchaseOrderHistoryItem order)
        {
            try
            {
                var fullOrder = _allOrders.FirstOrDefault(o => o.PurchaseOrderId == order.PurchaseOrderId);
                if (fullOrder == null)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Order not found.", "OK");
                    return;
                }

                var purchaseOrderModel = new PurchaseOrderModel
                {
                    PurchaseOrderId = order.PurchaseOrderId,
                    OrderDate = order.OrderDate,
                    SupplierName = order.SupplierName,
                    Status = order.Status,
                    RequestedBy = order.RequestedBy,
                    ApprovedBy = order.ApprovedBy,
                    ApprovedDate = order.ApprovedDate,
                    TotalAmount = order.TotalAmount,
                    CreatedAt = order.CreatedAt,
                    Notes = order.Notes
                };

                var items = await _database.GetPurchaseOrderItemsAsync(order.PurchaseOrderId);
                
                var pdfService = new Services.PDFReportService();
                var filePath = await pdfService.GeneratePurchaseOrderPDFAsync(
                    order.PurchaseOrderId,
                    purchaseOrderModel,
                    items);

                await Application.Current.MainPage.DisplayAlert(
                    "PDF Generated",
                    $"Purchase Order PDF has been saved successfully!\n\nFile location: {filePath}\n\nTo find the file:\n1. Open File Manager\n2. Go to Download folder\n3. Look for Purchase_Order_{order.PurchaseOrderId}_*.pdf",
                    "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error exporting PDF: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    $"Failed to export PDF:\n\n{ex.Message}",
                    "OK");
            }
        }
    }

    public class PurchaseOrderHistoryItem : ObservableObject
    {
        public int PurchaseOrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string RequestedBy { get; set; } = string.Empty;
        public string ApprovedBy { get; set; } = string.Empty;
        public DateTime? ApprovedDate { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Notes { get; set; } = string.Empty;
        public string ItemsPreview { get; set; } = string.Empty;
        public int ItemCount { get; set; }

        public string FormattedDate => OrderDate.ToString("MMM dd, yyyy");
        public string FormattedAmount => $"₱{TotalAmount:F2}";
        public string StatusColor => Status switch
        {
            "Approved" => "#28A745",
            "Rejected" => "#DC3545",
            "Pending" => "#FFC107",
            "Completed" => "#28A745",
            _ => "#6C757D"
        };
    }
}

