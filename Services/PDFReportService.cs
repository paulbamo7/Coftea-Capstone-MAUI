using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Coftea_Capstone.Models;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Devices;
using System.Text;
using System.IO;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Drawing;
using Syncfusion.Pdf.Grid;
using PointF = Syncfusion.Drawing.PointF;
using SizeF = Syncfusion.Drawing.SizeF;
using RectangleF = Syncfusion.Drawing.RectangleF;
using PdfColor = Syncfusion.Drawing.Color;

namespace Coftea_Capstone.Services
{
    public interface IPDFReportService
    {
        Task<string> GenerateWeeklyReportPDFAsync(DateTime startDate, DateTime endDate, List<TransactionHistoryModel> transactions, List<TrendItem> topItems);
        Task<string> GenerateMonthlyReportPDFAsync(DateTime startDate, DateTime endDate, List<TransactionHistoryModel> transactions, List<TrendItem> topItems);
        Task<string> GeneratePurchaseOrderPDFAsync(int purchaseOrderId, PurchaseOrderModel order, List<PurchaseOrderItemModel> items);
    }

    public class PDFReportService : IPDFReportService
    {
        private readonly Database _database;

        public PDFReportService()
        {
            _database = new Database();
        }

        public async Task<string> GenerateWeeklyReportPDFAsync(DateTime startDate, DateTime endDate, List<TransactionHistoryModel> transactions, List<TrendItem> topItems)
        {
            try
            {
                // Ensure collections are not null
                transactions = transactions ?? new List<TransactionHistoryModel>();
                topItems = topItems ?? new List<TrendItem>();

                // Get inventory deductions
                var inventoryDeductions = await GetInventoryDeductionsForPeriodAsync(startDate, endDate);
                var unitMap = await GetInventoryUnitsMapAsync();

                // Calculate totals
                var totalSales = transactions?.Sum(t => t?.Total ?? 0) ?? 0;
                var totalOrders = transactions?.Count ?? 0;
                var cashSales = transactions?.Where(t => t?.PaymentMethod?.Equals("Cash", StringComparison.OrdinalIgnoreCase) == true).Sum(t => t?.Total ?? 0) ?? 0;
                var gcashSales = transactions?.Where(t => t?.PaymentMethod?.Equals("GCash", StringComparison.OrdinalIgnoreCase) == true).Sum(t => t?.Total ?? 0) ?? 0;
                var bankSales = transactions?.Where(t => t?.PaymentMethod?.Equals("Bank", StringComparison.OrdinalIgnoreCase) == true).Sum(t => t?.Total ?? 0) ?? 0;

                // Create PDF document
                using (PdfDocument document = new PdfDocument())
                {
                    // Add page
                    PdfPage page = document.Pages.Add();
                    PdfGraphics graphics = page.Graphics;

                    float yPosition = 40;
                    float pageWidth = page.GetClientSize().Width;
                    float margin = 40;

                    // Header
                    PdfFont titleFont = new PdfStandardFont(PdfFontFamily.Helvetica, 24, PdfFontStyle.Bold);
                    PdfFont subTitleFont = new PdfStandardFont(PdfFontFamily.Helvetica, 14);
                    PdfFont normalFont = new PdfStandardFont(PdfFontFamily.Helvetica, 10);
                    PdfFont boldFont = new PdfStandardFont(PdfFontFamily.Helvetica, 10, PdfFontStyle.Bold);

                    PdfBrush titleBrush = new PdfSolidBrush(PdfColor.FromArgb(68, 68, 68));
                    PdfBrush textBrush = new PdfSolidBrush(PdfColor.FromArgb(100, 100, 100));

                    graphics.DrawString("Coftea Weekly Sales Report", titleFont, titleBrush, new PointF(margin, yPosition));
                    yPosition += 30;

                    graphics.DrawString($"{startDate:MMMM dd, yyyy} - {endDate:MMMM dd, yyyy}", subTitleFont, textBrush, new PointF(margin, yPosition));
                    yPosition += 40;

                    // Sales Summary
                    graphics.DrawString("Sales Summary", boldFont, titleBrush, new PointF(margin, yPosition));
                    yPosition += 20;

                    float boxWidth = (pageWidth - margin * 2 - 40) / 3;
                    float boxHeight = 60;
                    float boxSpacing = 20;

                    DrawStatBox(graphics, margin, yPosition, boxWidth, boxHeight, "Total Sales", $"₱{totalSales:N2}", PdfColor.FromArgb(200, 230, 255));
                    DrawStatBox(graphics, margin + boxWidth + boxSpacing, yPosition, boxWidth, boxHeight, "Total Orders", totalOrders.ToString(), PdfColor.FromArgb(200, 230, 255));
                    DrawStatBox(graphics, margin + (boxWidth + boxSpacing) * 2, yPosition, boxWidth, boxHeight, "Avg Order", $"₱{(totalOrders > 0 ? totalSales / totalOrders : 0):N2}", PdfColor.FromArgb(200, 230, 255));
                    yPosition += boxHeight + 30;

                    // Payment Methods
                    graphics.DrawString("Payment Methods", boldFont, titleBrush, new PointF(margin, yPosition));
                    yPosition += 20;

                    DrawStatBox(graphics, margin, yPosition, boxWidth, boxHeight, "Cash", $"₱{cashSales:N2}", PdfColor.FromArgb(200, 255, 230));
                    DrawStatBox(graphics, margin + boxWidth + boxSpacing, yPosition, boxWidth, boxHeight, "GCash", $"₱{gcashSales:N2}", PdfColor.FromArgb(200, 230, 255));
                    DrawStatBox(graphics, margin + (boxWidth + boxSpacing) * 2, yPosition, boxWidth, boxHeight, "Bank", $"₱{bankSales:N2}", PdfColor.FromArgb(230, 200, 255));
                    yPosition += boxHeight + 30;

                    // Top Selling Items Table
                    graphics.DrawString("Top Selling Items", boldFont, titleBrush, new PointF(margin, yPosition));
                    yPosition += 20;

                    PdfGrid topItemsGrid = new PdfGrid();
                    topItemsGrid.Columns.Add(2);
                    topItemsGrid.Headers.Add(1);
                    PdfGridRow headerRow = topItemsGrid.Headers[0];
                    headerRow.Cells[0].Value = "Item";
                    headerRow.Cells[1].Value = "Orders";
                    headerRow.Cells[0].Style.Font = boldFont;
                    headerRow.Cells[1].Style.Font = boldFont;

                    int itemCount = 0;
                    foreach (var item in topItems.Take(10))
                    {
                        if (item == null) continue;
                        PdfGridRow row = topItemsGrid.Rows.Add();
                        row.Cells[0].Value = $"#{++itemCount} {item.Name ?? "Unknown Item"}";
                        row.Cells[1].Value = $"{item.Count} orders";
                    }

                    if (topItems.Count == 0)
                    {
                        PdfGridRow row = topItemsGrid.Rows.Add();
                        row.Cells[0].Value = "No items to display";
                        topItemsGrid.Columns[0].Width = pageWidth - margin * 2;
                    }

                    topItemsGrid.Draw(graphics, new RectangleF(margin, yPosition, pageWidth - margin * 2, topItemsGrid.Rows.Count * 25 + 30));
                    yPosition += topItemsGrid.Rows.Count * 25 + 50;

                    // Check if we need a new page for inventory deductions
                    if (yPosition > page.GetClientSize().Height - 200)
                    {
                        page = document.Pages.Add();
                        graphics = page.Graphics;
                        yPosition = 40;
                    }

                    // Inventory Deductions Table
                    graphics.DrawString("Inventory Deductions", boldFont, titleBrush, new PointF(margin, yPosition));
                    yPosition += 20;

                    PdfGrid deductionsGrid = new PdfGrid();
                    deductionsGrid.Columns.Add(2);
                    deductionsGrid.Headers.Add(1);
                    PdfGridRow deductionsHeaderRow = deductionsGrid.Headers[0];
                    deductionsHeaderRow.Cells[0].Value = "Item";
                    deductionsHeaderRow.Cells[1].Value = "Amount";
                    deductionsHeaderRow.Cells[0].Style.Font = boldFont;
                    deductionsHeaderRow.Cells[1].Style.Font = boldFont;

                    foreach (var deduction in inventoryDeductions.OrderByDescending(x => x.Value).Take(15))
                    {
                        if (deduction.Key == null) continue;
                        var unit = unitMap.TryGetValue(deduction.Key, out var u) && !string.IsNullOrWhiteSpace(u) ? u : "units";
                        PdfGridRow row = deductionsGrid.Rows.Add();
                        row.Cells[0].Value = deduction.Key;
                        row.Cells[1].Value = $"{deduction.Value:N1} {unit}";
                    }

                    if (inventoryDeductions.Count == 0)
                    {
                        PdfGridRow row = deductionsGrid.Rows.Add();
                        row.Cells[0].Value = "No inventory deductions to display";
                        deductionsGrid.Columns[0].Width = pageWidth - margin * 2;
                    }

                    deductionsGrid.Draw(graphics, new RectangleF(margin, yPosition, pageWidth - margin * 2, deductionsGrid.Rows.Count * 25 + 30));

                    // Footer
                    PdfPage lastPage = document.Pages[document.Pages.Count - 1];
                    PdfGraphics footerGraphics = lastPage.Graphics;
                    PdfFont footerFont = new PdfStandardFont(PdfFontFamily.Helvetica, 9);
                    PdfBrush footerBrush = new PdfSolidBrush(PdfColor.FromArgb(128, 128, 128));
                    string footerText = $"Generated on {DateTime.Now:MMMM dd, yyyy 'at' HH:mm} | Coftea Sales Management System";
                    SizeF footerSize = footerFont.MeasureString(footerText);
                    footerGraphics.DrawString(footerText, footerFont, footerBrush, new PointF((pageWidth - footerSize.Width) / 2, lastPage.GetClientSize().Height - 30));

                    // Save PDF
                    var fileName = $"Weekly_Report_{startDate:yyyy_MM_dd}_to_{endDate:yyyy_MM_dd}.pdf";
                    string filePath = GetDownloadPath(fileName);
                    FileStream fileStream = new FileStream(filePath, FileMode.Create);
                    document.Save(fileStream);
                    fileStream.Dispose();

                    return filePath;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating weekly PDF report: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<string> GenerateMonthlyReportPDFAsync(DateTime startDate, DateTime endDate, List<TransactionHistoryModel> transactions, List<TrendItem> topItems)
        {
            try
            {
                // Ensure collections are not null
                transactions = transactions ?? new List<TransactionHistoryModel>();
                topItems = topItems ?? new List<TrendItem>();

                // Get inventory deductions
                var inventoryDeductions = await GetInventoryDeductionsForPeriodAsync(startDate, endDate);
                var unitMap = await GetInventoryUnitsMapAsync();

                // Calculate totals
                var totalSales = transactions?.Sum(t => t?.Total ?? 0) ?? 0;
                var totalOrders = transactions?.Count ?? 0;
                var days = Math.Max(1, (endDate - startDate).Days);
                var cashSales = transactions?.Where(t => t?.PaymentMethod?.Equals("Cash", StringComparison.OrdinalIgnoreCase) == true).Sum(t => t?.Total ?? 0) ?? 0;
                var gcashSales = transactions?.Where(t => t?.PaymentMethod?.Equals("GCash", StringComparison.OrdinalIgnoreCase) == true).Sum(t => t?.Total ?? 0) ?? 0;
                var bankSales = transactions?.Where(t => t?.PaymentMethod?.Equals("Bank", StringComparison.OrdinalIgnoreCase) == true).Sum(t => t?.Total ?? 0) ?? 0;

                // Create PDF document
                using (PdfDocument document = new PdfDocument())
                {
                    PdfPage page = document.Pages.Add();
                    PdfGraphics graphics = page.Graphics;

                    float yPosition = 40;
                    float pageWidth = page.GetClientSize().Width;
                    float margin = 40;

                    // Fonts
                    PdfFont titleFont = new PdfStandardFont(PdfFontFamily.Helvetica, 24, PdfFontStyle.Bold);
                    PdfFont subTitleFont = new PdfStandardFont(PdfFontFamily.Helvetica, 14);
                    PdfFont normalFont = new PdfStandardFont(PdfFontFamily.Helvetica, 10);
                    PdfFont boldFont = new PdfStandardFont(PdfFontFamily.Helvetica, 10, PdfFontStyle.Bold);

                    PdfBrush titleBrush = new PdfSolidBrush(PdfColor.FromArgb(68, 68, 68));
                    PdfBrush textBrush = new PdfSolidBrush(PdfColor.FromArgb(100, 100, 100));

                    // Header
                    graphics.DrawString("Coftea Monthly Sales Report", titleFont, titleBrush, new PointF(margin, yPosition));
                    yPosition += 30;

                    graphics.DrawString($"{startDate:MMMM yyyy}", subTitleFont, textBrush, new PointF(margin, yPosition));
                    yPosition += 40;

                    // Sales Summary (4 boxes)
                    graphics.DrawString("Sales Summary", boldFont, titleBrush, new PointF(margin, yPosition));
                    yPosition += 20;

                    float boxWidth = (pageWidth - margin * 2 - 60) / 4;
                    float boxHeight = 60;
                    float boxSpacing = 15;

                    DrawStatBox(graphics, margin, yPosition, boxWidth, boxHeight, "Total Sales", $"₱{totalSales:N2}", PdfColor.FromArgb(200, 230, 255));
                    DrawStatBox(graphics, margin + boxWidth + boxSpacing, yPosition, boxWidth, boxHeight, "Total Orders", totalOrders.ToString(), PdfColor.FromArgb(200, 230, 255));
                    DrawStatBox(graphics, margin + (boxWidth + boxSpacing) * 2, yPosition, boxWidth, boxHeight, "Avg Order", $"₱{(totalOrders > 0 ? totalSales / totalOrders : 0):N2}", PdfColor.FromArgb(200, 230, 255));
                    DrawStatBox(graphics, margin + (boxWidth + boxSpacing) * 3, yPosition, boxWidth, boxHeight, "Daily Avg", $"₱{(totalSales / days):N2}", PdfColor.FromArgb(200, 230, 255));
                    yPosition += boxHeight + 30;

                    // Payment Methods
                    graphics.DrawString("Payment Methods", boldFont, titleBrush, new PointF(margin, yPosition));
                    yPosition += 20;

                    boxWidth = (pageWidth - margin * 2 - 40) / 3;
                    boxSpacing = 20;

                    DrawStatBox(graphics, margin, yPosition, boxWidth, boxHeight, "Cash", $"₱{cashSales:N2}", PdfColor.FromArgb(200, 255, 230));
                    DrawStatBox(graphics, margin + boxWidth + boxSpacing, yPosition, boxWidth, boxHeight, "GCash", $"₱{gcashSales:N2}", PdfColor.FromArgb(200, 230, 255));
                    DrawStatBox(graphics, margin + (boxWidth + boxSpacing) * 2, yPosition, boxWidth, boxHeight, "Bank", $"₱{bankSales:N2}", PdfColor.FromArgb(230, 200, 255));
                    yPosition += boxHeight + 30;

                    // Top Selling Items Table
                    graphics.DrawString("Top Selling Items", boldFont, titleBrush, new PointF(margin, yPosition));
                    yPosition += 20;

                    PdfGrid topItemsGrid = new PdfGrid();
                    topItemsGrid.Columns.Add(2);
                    topItemsGrid.Headers.Add(1);
                    PdfGridRow headerRow = topItemsGrid.Headers[0];
                    headerRow.Cells[0].Value = "Item";
                    headerRow.Cells[1].Value = "Orders";
                    headerRow.Cells[0].Style.Font = boldFont;
                    headerRow.Cells[1].Style.Font = boldFont;

                    int itemCount = 0;
                    foreach (var item in topItems.Take(15))
                    {
                        if (item == null) continue;
                        PdfGridRow row = topItemsGrid.Rows.Add();
                        row.Cells[0].Value = $"#{++itemCount} {item.Name ?? "Unknown Item"}";
                        row.Cells[1].Value = $"{item.Count} orders";
                    }

                    if (topItems.Count == 0)
                    {
                        PdfGridRow row = topItemsGrid.Rows.Add();
                        row.Cells[0].Value = "No items to display";
                        topItemsGrid.Columns[0].Width = pageWidth - margin * 2;
                    }

                    topItemsGrid.Draw(graphics, new RectangleF(margin, yPosition, pageWidth - margin * 2, topItemsGrid.Rows.Count * 25 + 30));
                    yPosition += topItemsGrid.Rows.Count * 25 + 50;

                    // Check if we need a new page
                    if (yPosition > page.GetClientSize().Height - 200)
                    {
                        page = document.Pages.Add();
                        graphics = page.Graphics;
                        yPosition = 40;
                    }

                    // Inventory Deductions Table
                    graphics.DrawString("Inventory Deductions", boldFont, titleBrush, new PointF(margin, yPosition));
                    yPosition += 20;

                    PdfGrid deductionsGrid = new PdfGrid();
                    deductionsGrid.Columns.Add(2);
                    deductionsGrid.Headers.Add(1);
                    PdfGridRow deductionsHeaderRow = deductionsGrid.Headers[0];
                    deductionsHeaderRow.Cells[0].Value = "Item";
                    deductionsHeaderRow.Cells[1].Value = "Amount";
                    deductionsHeaderRow.Cells[0].Style.Font = boldFont;
                    deductionsHeaderRow.Cells[1].Style.Font = boldFont;

                    foreach (var deduction in inventoryDeductions.OrderByDescending(x => x.Value).Take(20))
                    {
                        if (deduction.Key == null) continue;
                        var unit = unitMap.TryGetValue(deduction.Key, out var u) && !string.IsNullOrWhiteSpace(u) ? u : "units";
                        PdfGridRow row = deductionsGrid.Rows.Add();
                        row.Cells[0].Value = deduction.Key;
                        row.Cells[1].Value = $"{deduction.Value:N1} {unit}";
                    }

                    if (inventoryDeductions.Count == 0)
                    {
                        PdfGridRow row = deductionsGrid.Rows.Add();
                        row.Cells[0].Value = "No inventory deductions to display";
                        deductionsGrid.Columns[0].Width = pageWidth - margin * 2;
                    }

                    deductionsGrid.Draw(graphics, new RectangleF(margin, yPosition, pageWidth - margin * 2, deductionsGrid.Rows.Count * 25 + 30));

                    // Footer
                    PdfPage lastPage = document.Pages[document.Pages.Count - 1];
                    PdfGraphics footerGraphics = lastPage.Graphics;
                    PdfFont footerFont = new PdfStandardFont(PdfFontFamily.Helvetica, 9);
                    PdfBrush footerBrush = new PdfSolidBrush(PdfColor.FromArgb(128, 128, 128));
                    string footerText = $"Generated on {DateTime.Now:MMMM dd, yyyy 'at' HH:mm} | Coftea Sales Management System";
                    SizeF footerSize = footerFont.MeasureString(footerText);
                    footerGraphics.DrawString(footerText, footerFont, footerBrush, new PointF((pageWidth - footerSize.Width) / 2, lastPage.GetClientSize().Height - 30));

                    // Save PDF
                    var fileName = $"Monthly_Report_{startDate:yyyy_MM}_to_{endDate:yyyy_MM}.pdf";
                    string filePath = GetDownloadPath(fileName);
                    FileStream fileStream = new FileStream(filePath, FileMode.Create);
                    document.Save(fileStream);
                    fileStream.Dispose();

                    return filePath;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating monthly PDF report: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<string> GeneratePurchaseOrderPDFAsync(int purchaseOrderId, PurchaseOrderModel order, List<PurchaseOrderItemModel> items)
        {
            try
            {
                // Validate inputs
                if (order == null)
                {
                    throw new ArgumentNullException(nameof(order), "Purchase order cannot be null");
                }
                items = items ?? new List<PurchaseOrderItemModel>();

                // Create PDF document
                using (PdfDocument document = new PdfDocument())
                {
                    PdfPage page = document.Pages.Add();
                    PdfGraphics graphics = page.Graphics;

                    float yPosition = 40;
                    float pageWidth = page.GetClientSize().Width;
                    float margin = 40;

                    // Fonts
                    PdfFont titleFont = new PdfStandardFont(PdfFontFamily.Helvetica, 24, PdfFontStyle.Bold);
                    PdfFont subTitleFont = new PdfStandardFont(PdfFontFamily.Helvetica, 12, PdfFontStyle.Bold);
                    PdfFont normalFont = new PdfStandardFont(PdfFontFamily.Helvetica, 10);
                    PdfFont boldFont = new PdfStandardFont(PdfFontFamily.Helvetica, 10, PdfFontStyle.Bold);

                    PdfBrush titleBrush = new PdfSolidBrush(PdfColor.FromArgb(68, 68, 68));
                    PdfBrush textBrush = new PdfSolidBrush(PdfColor.FromArgb(100, 100, 100));

                    // Header
                    graphics.DrawString("Purchase Order", titleFont, titleBrush, new PointF(margin, yPosition));
                    yPosition += 30;

                    graphics.DrawString($"Order #: {purchaseOrderId}", subTitleFont, textBrush, new PointF(margin, yPosition));
                    yPosition += 20;

                    graphics.DrawString($"Date: {order.OrderDate:MMMM dd, yyyy}", normalFont, textBrush, new PointF(margin, yPosition));
                    yPosition += 15;

                    graphics.DrawString($"Status: {order.Status}", normalFont, textBrush, new PointF(margin, yPosition));
                    yPosition += 40;

                    // Order Information
                    graphics.DrawString("Order Information", boldFont, titleBrush, new PointF(margin, yPosition));
                    yPosition += 20;

                    graphics.DrawString($"Supplier: {order.SupplierName}", normalFont, titleBrush, new PointF(margin, yPosition));
                    yPosition += 15;

                    graphics.DrawString($"Requested By: {order.RequestedBy}", normalFont, titleBrush, new PointF(margin, yPosition));
                    yPosition += 15;

                    if (!string.IsNullOrEmpty(order.ApprovedBy))
                    {
                        graphics.DrawString($"Approved By: {order.ApprovedBy}", normalFont, titleBrush, new PointF(margin, yPosition));
                        yPosition += 15;

                        if (order.ApprovedDate.HasValue)
                        {
                            graphics.DrawString($"Approved Date: {order.ApprovedDate.Value:MMMM dd, yyyy HH:mm}", normalFont, titleBrush, new PointF(margin, yPosition));
                            yPosition += 15;
                        }
                    }

                    yPosition += 20;

                    // Items Table
                    graphics.DrawString("Items Needed to Restock", boldFont, titleBrush, new PointF(margin, yPosition));
                    yPosition += 20;

                    PdfGrid itemsGrid = new PdfGrid();
                    itemsGrid.Columns.Add(4);
                    itemsGrid.Headers.Add(1);
                    PdfGridRow itemsHeaderRow = itemsGrid.Headers[0];
                    itemsHeaderRow.Cells[0].Value = "Item Name";
                    itemsHeaderRow.Cells[1].Value = "Amount";
                    itemsHeaderRow.Cells[2].Value = "UoM";
                    itemsHeaderRow.Cells[3].Value = "Quantity";
                    itemsHeaderRow.Cells[0].Style.Font = boldFont;
                    itemsHeaderRow.Cells[1].Style.Font = boldFont;
                    itemsHeaderRow.Cells[2].Style.Font = boldFont;
                    itemsHeaderRow.Cells[3].Style.Font = boldFont;

                    foreach (var item in items)
                    {
                        var quantity = item.ApprovedQuantity > 0 ? item.ApprovedQuantity : item.RequestedQuantity;
                        PdfGridRow row = itemsGrid.Rows.Add();
                        row.Cells[0].Value = item.ItemName ?? "";
                        row.Cells[1].Value = "1"; // Amount: 1 unit per piece
                        row.Cells[2].Value = item.UnitOfMeasurement ?? "pcs";
                        row.Cells[3].Value = quantity.ToString(); // Quantity: how many units are being purchased
                    }

                    if (items.Count == 0)
                    {
                        PdfGridRow row = itemsGrid.Rows.Add();
                        row.Cells[0].Value = "No items in this order";
                        itemsGrid.Columns[0].Width = pageWidth - margin * 2;
                    }

                    itemsGrid.Draw(graphics, new RectangleF(margin, yPosition, pageWidth - margin * 2, itemsGrid.Rows.Count * 25 + 30));
                    yPosition += itemsGrid.Rows.Count * 25 + 50;

                    // Notes
                    if (!string.IsNullOrEmpty(order.Notes))
                    {
                        yPosition += 10;
                        graphics.DrawString("Notes:", boldFont, titleBrush, new PointF(margin, yPosition));
                        yPosition += 20;
                        graphics.DrawString(order.Notes, normalFont, titleBrush, new RectangleF(margin, yPosition, pageWidth - margin * 2, 100));
                    }

                    // Footer
                    PdfPage lastPage = document.Pages[document.Pages.Count - 1];
                    PdfGraphics footerGraphics = lastPage.Graphics;
                    PdfFont footerFont = new PdfStandardFont(PdfFontFamily.Helvetica, 9);
                    PdfBrush footerBrush = new PdfSolidBrush(PdfColor.FromArgb(128, 128, 128));
                    string footerText = $"Generated on {DateTime.Now:MMMM dd, yyyy 'at' HH:mm} | Coftea Management System";
                    SizeF footerSize = footerFont.MeasureString(footerText);
                    footerGraphics.DrawString(footerText, footerFont, footerBrush, new PointF((pageWidth - footerSize.Width) / 2, lastPage.GetClientSize().Height - 30));

                    // Save PDF
                    var fileName = $"Purchase_Order_{purchaseOrderId}_{order.OrderDate:yyyy_MM_dd}.pdf";
                    string filePath = GetDownloadPath(fileName);
                    FileStream fileStream = new FileStream(filePath, FileMode.Create);
                    document.Save(fileStream);
                    fileStream.Dispose();

                    return filePath;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error generating purchase order PDF: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Inner exception: {ex.InnerException.Message}");
                    System.Diagnostics.Debug.WriteLine($"❌ Inner stack trace: {ex.InnerException.StackTrace}");
                }
                throw new Exception($"Failed to generate PDF: {ex.Message}", ex);
            }
        }

        private void DrawStatBox(PdfGraphics graphics, float x, float y, float width, float height, string label, string value, PdfColor backgroundColor)
        {
            PdfBrush bgBrush = new PdfSolidBrush(backgroundColor);
            PdfBrush borderBrush = new PdfSolidBrush(PdfColor.FromArgb(200, 200, 200));
            PdfPen borderPen = new PdfPen(borderBrush, 1);

            graphics.DrawRectangle(borderPen, new RectangleF(x, y, width, height));
            graphics.DrawRectangle(bgBrush, new RectangleF(x, y, width, height));

            PdfFont labelFont = new PdfStandardFont(PdfFontFamily.Helvetica, 9);
            PdfFont valueFont = new PdfStandardFont(PdfFontFamily.Helvetica, 14, PdfFontStyle.Bold);
            PdfBrush textBrush = new PdfSolidBrush(PdfColor.FromArgb(68, 68, 68));

            graphics.DrawString(label, labelFont, textBrush, new PointF(x + 10, y + 10));
            graphics.DrawString(value, valueFont, textBrush, new PointF(x + 10, y + 30));
        }

        private async Task<Dictionary<string, double>> GetInventoryDeductionsForPeriodAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var transactions = await _database.GetTransactionsByDateRangeAsync(startDate, endDate);
                var deductions = new Dictionary<string, double>();

                if (transactions == null || transactions.Count == 0)
                {
                    return deductions;
                }

                var productGroups = transactions
                    .Where(t => t != null && !string.IsNullOrEmpty(t.DrinkName))
                    .GroupBy(t => t.DrinkName)
                    .ToList();

                foreach (var productGroup in productGroups)
                {
                    if (productGroup == null || string.IsNullOrEmpty(productGroup.Key)) continue;

                    var productName = productGroup.Key;
                    var totalQuantity = productGroup.Sum(t => t?.Quantity ?? 0);
                    AddBasicDeductionsForProduct(deductions, productName, totalQuantity);

                    foreach (var transaction in productGroup)
                    {
                        if (transaction == null) continue;
                        AddCupAndStrawDeductions(deductions, transaction.Size, transaction.Quantity);
                    }
                }

                return deductions;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting inventory deductions: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return new Dictionary<string, double>();
            }
        }

        private async Task<Dictionary<string, string>> GetInventoryUnitsMapAsync()
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var items = await _database.GetInventoryItemsAsyncCached();
                if (items != null)
                {
                    foreach (var it in items)
                    {
                        if (it != null && !string.IsNullOrWhiteSpace(it.itemName) && !string.IsNullOrWhiteSpace(it.unitOfMeasurement))
                        {
                            map[it.itemName] = it.unitOfMeasurement;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting inventory units map: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            return map;
        }

        private void AddBasicDeductionsForProduct(Dictionary<string, double> deductions, string productName, int totalQuantity)
        {
            try
            {
                if (deductions == null || string.IsNullOrWhiteSpace(productName)) return;

                var productNameLower = productName.ToLowerInvariant();

                if (productNameLower.Contains("coffee"))
                {
                    AddDeduction(deductions, "Coffee Beans", totalQuantity * 0.1);
                    AddDeduction(deductions, "Sugar", totalQuantity * 0.05);
                    AddDeduction(deductions, "Milk", totalQuantity * 0.2);
                }
                else if (productNameLower.Contains("milktea"))
                {
                    AddDeduction(deductions, "Tea Leaves", totalQuantity * 0.05);
                    AddDeduction(deductions, "Milk", totalQuantity * 0.3);
                    AddDeduction(deductions, "Sugar", totalQuantity * 0.1);
                    AddDeduction(deductions, "Tapioca Pearls", totalQuantity * 0.1);
                }
                else if (productNameLower.Contains("frappe"))
                {
                    AddDeduction(deductions, "Ice", totalQuantity * 0.5);
                    AddDeduction(deductions, "Milk", totalQuantity * 0.2);
                    AddDeduction(deductions, "Sugar", totalQuantity * 0.08);
                    AddDeduction(deductions, "Coffee Beans", totalQuantity * 0.08);
                }
                else if (productNameLower.Contains("fruit") || productNameLower.Contains("soda"))
                {
                    AddDeduction(deductions, "Fruit Syrup", totalQuantity * 0.1);
                    AddDeduction(deductions, "Soda Water", totalQuantity * 0.3);
                    AddDeduction(deductions, "Ice", totalQuantity * 0.3);
                }
                else
                {
                    AddDeduction(deductions, "Base Ingredient", totalQuantity * 0.1);
                    AddDeduction(deductions, "Sugar", totalQuantity * 0.05);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding basic deductions for {productName}: {ex.Message}");
            }
        }

        private void AddDeduction(Dictionary<string, double> deductions, string itemName, double amount)
        {
            if (deductions.ContainsKey(itemName))
            {
                deductions[itemName] += amount;
            }
            else
            {
                deductions[itemName] = amount;
            }
        }

        private double GetSizeMultiplier(string size)
        {
            return size?.ToLowerInvariant() switch
            {
                "small" => 1.0,
                "medium" => 1.5,
                "large" => 2.0,
                _ => 1.5
            };
        }

        /// <summary>
        /// Gets the appropriate download path for PDF files based on the platform
        /// </summary>
        private string GetDownloadPath(string fileName)
        {
            try
            {
                string downloadPath;
                var platform = DeviceInfo.Platform;

                if (platform == DevicePlatform.Android)
                {
                    // Android: Use external storage Downloads folder
                    downloadPath = "/storage/emulated/0/Download";
                    if (!Directory.Exists(downloadPath))
                    {
                        // Fallback to app's external files directory
                        downloadPath = Path.Combine(FileSystem.AppDataDirectory, "Downloads");
                    }
                }
                else if (platform == DevicePlatform.iOS || platform == DevicePlatform.MacCatalyst)
                {
                    // iOS/MacCatalyst: Use Documents folder (accessible via Files app)
                    downloadPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        "Downloads");
                }
                else if (platform == DevicePlatform.WinUI)
                {
                    // Windows: Use user's Downloads folder
                    downloadPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        "Downloads");
                }
                else if (platform == DevicePlatform.macOS)
                {
                    // macOS: Use user's Downloads folder
                    downloadPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        "Downloads");
                }
                else
                {
                    // Fallback: Use app data directory
                    downloadPath = Path.Combine(FileSystem.AppDataDirectory, "Downloads");
                }

                // Ensure directory exists
                if (!Directory.Exists(downloadPath))
                {
                    Directory.CreateDirectory(downloadPath);
                }

                return Path.Combine(downloadPath, fileName);
            }
            catch (Exception ex)
            {
                // If all else fails, save to app data directory
                System.Diagnostics.Debug.WriteLine($"Warning: Could not access Downloads folder, using app data directory: {ex.Message}");
                return Path.Combine(FileSystem.AppDataDirectory, fileName);
            }
        }

        private void AddCupAndStrawDeductions(Dictionary<string, double> deductions, string size, int quantity)
        {
            if (deductions == null) return;

            try
            {
                var sizeMultiplier = GetSizeMultiplier(size);
                var totalQuantity = quantity * sizeMultiplier;

                var cupName = size?.ToLowerInvariant() switch
                {
                    "small" => "Small Cup",
                    "medium" => "Medium Cup",
                    "large" => "Large Cup",
                    _ => "Medium Cup"
                };

                if (deductions.ContainsKey(cupName))
                {
                    deductions[cupName] += totalQuantity;
                }
                else
                {
                    deductions[cupName] = totalQuantity;
                }

                if (deductions.ContainsKey("Straw"))
                {
                    deductions["Straw"] += quantity;
                }
                else
                {
                    deductions["Straw"] = quantity;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding cup and straw deductions: {ex.Message}");
            }
        }
    }
}
