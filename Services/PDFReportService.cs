using System;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Coftea_Capstone.Models;
using Coftea_Capstone.Services.Pdf;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using PdfSharpCore.Drawing;
using PdfSharpCore.Drawing.Layout;
using PdfSharpCore.Fonts;
using PdfSharpCore.Pdf;

namespace Coftea_Capstone.Services
{
    public interface IPDFReportService
    {
        Task<string> GenerateWeeklyReportPDFAsync(DateTime startDate, DateTime endDate, List<TransactionHistoryModel> transactions, List<TrendItem> topItems);
        Task<string> GenerateMonthlyReportPDFAsync(DateTime startDate, DateTime endDate, List<TransactionHistoryModel> transactions, List<TrendItem> topItems);
        Task<string> GeneratePurchaseOrderPDFAsync(int purchaseOrderId, PurchaseOrderModel order, List<PurchaseOrderItemModel> items);
        Task<string> GenerateActivityLogPDFAsync(DateTime startDate, DateTime endDate, List<InventoryActivityLog> entries);
    }

    public class PDFReportService : IPDFReportService
    {
        private const double Margin = 40;

        private static readonly Lazy<XFont> TitleFontLazy = new(() => new XFont("OpenSans", 24, XFontStyle.Bold));
        private static readonly Lazy<XFont> SubTitleFontLazy = new(() => new XFont("OpenSans", 14, XFontStyle.Regular));
        private static readonly Lazy<XFont> SectionTitleFontLazy = new(() => new XFont("OpenSans", 12, XFontStyle.Bold));
        private static readonly Lazy<XFont> LabelFontLazy = new(() => new XFont("OpenSans", 10, XFontStyle.Regular));
        private static readonly Lazy<XFont> ValueFontLazy = new(() => new XFont("OpenSans", 14, XFontStyle.Bold));
        private static readonly Lazy<XFont> TableHeaderFontLazy = new(() => new XFont("OpenSans", 11, XFontStyle.Bold));
        private static readonly Lazy<XFont> TableRowFontLazy = new(() => new XFont("OpenSans", 10, XFontStyle.Regular));
        private static readonly Lazy<XFont> SmallFontLazy = new(() => new XFont("OpenSans", 9, XFontStyle.Regular));

        private static readonly XBrush TitleBrush = new XSolidBrush(XColor.FromArgb(0x3E, 0x27, 0x23));
        private static readonly XBrush TextBrush = new XSolidBrush(XColor.FromArgb(0x4B, 0x3A, 0x2F));
        private static readonly XBrush MutedBrush = new XSolidBrush(XColor.FromArgb(0x80, 0x80, 0x80));
        private static readonly XBrush TableHeaderBrush = new XSolidBrush(XColor.FromArgb(0xDA, 0xAB, 0x97));
        private static readonly XBrush TableRowBrush = new XSolidBrush(XColor.FromArgb(0xFF, 0xFF, 0xFF));
        private static readonly XPen TableBorderPen = new XPen(XColor.FromArgb(0xE5, 0xD2, 0xC1), 0.75);

        private static XFont TitleFont => TitleFontLazy.Value;
        private static XFont SubTitleFont => SubTitleFontLazy.Value;
        private static XFont SectionTitleFont => SectionTitleFontLazy.Value;
        private static XFont LabelFont => LabelFontLazy.Value;
        private static XFont ValueFont => ValueFontLazy.Value;
        private static XFont TableHeaderFont => TableHeaderFontLazy.Value;
        private static XFont TableRowFont => TableRowFontLazy.Value;
        private static XFont SmallFont => SmallFontLazy.Value;

        private readonly Database _database;

        public PDFReportService()
        {
            _database = new Database();
        }

        public async Task<string> GenerateMonthlyReportPDFAsync(DateTime startDate, DateTime endDate, List<TransactionHistoryModel> transactions, List<TrendItem> topItems)
        {
            try
            {
                transactions ??= new List<TransactionHistoryModel>();
                topItems ??= new List<TrendItem>();

                var deductionsWithUnits = await _database.GetInventoryDeductionsForPeriodAsync(startDate, endDate);
                var inventoryDeductions = deductionsWithUnits.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.totalDeducted);
                var unitMap = deductionsWithUnits.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.unitOfMeasurement ?? string.Empty, StringComparer.OrdinalIgnoreCase);

                var inventoryUnitMap = await GetInventoryUnitsMapAsync();
                foreach (var kvp in inventoryUnitMap)
                {
                    if (!unitMap.ContainsKey(kvp.Key))
                    {
                        unitMap[kvp.Key] = kvp.Value;
                    }
                }

                var totalSales = transactions.Sum(t => t?.Total ?? 0);
                var totalOrders = transactions.Count;
                var dayCount = Math.Max(1, Math.Abs((endDate.Date - startDate.Date).Days));
                var cashSales = transactions.Where(t => t?.PaymentMethod?.Equals("Cash", StringComparison.OrdinalIgnoreCase) == true).Sum(t => t?.Total ?? 0);
                var gcashSales = transactions.Where(t => t?.PaymentMethod?.Equals("GCash", StringComparison.OrdinalIgnoreCase) == true).Sum(t => t?.Total ?? 0);
                var bankSales = transactions.Where(t => t?.PaymentMethod?.Equals("Bank", StringComparison.OrdinalIgnoreCase) == true).Sum(t => t?.Total ?? 0);

                using var document = new PdfDocument();
                using (var context = new PdfPageContext(document, Margin))
                {
                    DrawReportHeader(context, "Coftea Monthly Sales Report", startDate.ToString("MMMM yyyy"));

                    DrawSectionTitle(context, "Sales Summary");
                    DrawStatBoxRow(context, new[]
                    {
                        ("Total Sales", $"₱{totalSales:N2}", XColor.FromArgb(200, 230, 255)),
                        ("Total Orders", $"{totalOrders}", XColor.FromArgb(200, 230, 255)),
                        ("Average Order", $"₱{(totalOrders > 0 ? totalSales / totalOrders : 0):N2}", XColor.FromArgb(200, 230, 255)),
                        ("Daily Average", $"₱{(totalSales / dayCount):N2}", XColor.FromArgb(200, 230, 255))
                    });

                    DrawSectionTitle(context, "Payment Methods");
                    DrawStatBoxRow(context, new[]
                    {
                        ("Cash", $"₱{cashSales:N2}", XColor.FromArgb(200, 255, 230)),
                        ("GCash", $"₱{gcashSales:N2}", XColor.FromArgb(200, 230, 255)),
                        ("Bank", $"₱{bankSales:N2}", XColor.FromArgb(230, 200, 255))
                    });

                    DrawSectionTitle(context, "Top Selling Items");
                    var topRows = topItems
                        .Take(15)
                        .Select((item, index) => new[]
                        {
                            $"#{index + 1} {item?.Name ?? "Unknown Item"}",
                            $"{item?.Count ?? 0} orders"
                        })
                        .ToList();

                    if (topRows.Count == 0)
                    {
                        topRows.Add(new[] { "No items to display", string.Empty });
                    }

                    DrawTable(context, new[] { "Item", "Orders" }, topRows);

                    // Add new section: All Products by Category
                    DrawSectionTitle(context, "All Products - Order Summary by Category");
                    var allProducts = await _database.GetProductsAsync();
                    var productOrderCounts = transactions
                        .Where(t => !string.IsNullOrWhiteSpace(t.DrinkName))
                        .GroupBy(t => t.DrinkName.Trim(), StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity > 0 ? x.Quantity : 1), StringComparer.OrdinalIgnoreCase);

                    // Group products by category
                    var productsByCategory = allProducts
                        .Where(p => !string.IsNullOrWhiteSpace(p.ProductName))
                        .GroupBy(p => string.IsNullOrWhiteSpace(p.Category) ? "Other" : p.Category.Trim())
                        .OrderBy(g => g.Key)
                        .ToList();

                    foreach (var categoryGroup in productsByCategory)
                    {
                        context.EnsureSpace(30);
                        WriteTextLine(context, $"{categoryGroup.Key}:", SectionTitleFont, TitleBrush, 16);
                        context.CurrentY += 4;

                        var categoryRows = categoryGroup
                            .OrderBy(p => p.ProductName)
                            .Select(p =>
                            {
                                var orderCount = productOrderCounts.TryGetValue(p.ProductName, out var count) ? count : 0;
                                return new[] { p.ProductName ?? "Unknown", orderCount.ToString() };
                            })
                            .ToList();

                        if (categoryRows.Count == 0)
                        {
                            categoryRows.Add(new[] { "No products in this category", "0" });
                        }

                        DrawTable(context, new[] { "Product Name", "Orders" }, categoryRows, new[] { 0.7, 0.3 });
                        context.CurrentY += 8;
                    }

                    DrawSectionTitle(context, "Inventory Deductions");
                    var deductionRows = inventoryDeductions
                        .OrderByDescending(x => x.Value)
                        .Take(20)
                        .Select(d =>
                        {
                            var unit = unitMap.TryGetValue(d.Key, out var value) ? value : string.Empty;
                            var amountText = string.IsNullOrWhiteSpace(unit)
                                ? $"{d.Value:N1}"
                                : $"{d.Value:N1} {unit}";
                            return new[] { d.Key, amountText };
                        })
                        .ToList();

                    if (deductionRows.Count == 0)
                    {
                        deductionRows.Add(new[] { "No inventory deductions to display", string.Empty });
                    }

                    DrawTable(context, new[] { "Item", "Amount" }, deductionRows);
                }

                DrawFooter(document, $"Generated on {DateTime.Now:MMMM dd, yyyy 'at' HH:mm} | Coftea Sales Management System");

                var monthLabel = startDate.ToString("yyyyMM");
                var fileName = MakeSafeFileName($"Coftea_MonthlyReports_{monthLabel}.pdf");
                var filePath = GetDownloadPath(fileName);
                document.Save(filePath);
                
                return filePath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating monthly PDF report: {ex.Message}");
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task<string> GeneratePurchaseOrderPDFAsync(int purchaseOrderId, PurchaseOrderModel order, List<PurchaseOrderItemModel> items)
        {
            try
            {
                if (order == null)
                {
                    throw new ArgumentNullException(nameof(order), "Purchase order cannot be null");
                }

                items ??= new List<PurchaseOrderItemModel>();

                using var document = new PdfDocument();
                using (var context = new PdfPageContext(document, Margin))
                {
                    DrawReportHeader(context, "Purchase Order", $"Order #: {purchaseOrderId}");

                    WriteTextLine(context, $"Date: {order.OrderDate:MMMM dd, yyyy}", SectionTitleFont, TextBrush, 18);
                    WriteTextLine(context, $"Status: {order.Status}", SectionTitleFont, TextBrush, 18);

                    WriteSpacing(context, 10);
                    DrawSectionTitle(context, "Order Information");
                    WriteTextLine(context, $"Supplier: {order.SupplierName}", LabelFont, TextBrush);
                    WriteTextLine(context, $"Requested By: {order.RequestedBy}", LabelFont, TextBrush);

                    if (!string.IsNullOrWhiteSpace(order.ApprovedBy))
                    {
                        WriteTextLine(context, $"Approved By: {order.ApprovedBy}", LabelFont, TextBrush);

                        if (order.ApprovedDate.HasValue)
                        {
                            WriteTextLine(context, $"Approved Date: {order.ApprovedDate.Value:MMMM dd, yyyy HH:mm}", LabelFont, TextBrush);
                        }
                    }

                    WriteSpacing(context, 12);
                    DrawSectionTitle(context, "Items Needed to Restock");

                    var itemRows = items.Select(item =>
                    {
                        var amount = item.ApprovedQuantity > 0 ? item.ApprovedQuantity : item.RequestedQuantity;
                        var quantity = item.Quantity > 0 ? item.Quantity : 1;
                        return new[]
                        {
                            item.ItemName ?? string.Empty,
                            amount.ToString(),
                            item.UnitOfMeasurement ?? "pcs",
                            $"x{quantity}"
                        };
                    }).ToList();

                    if (itemRows.Count == 0)
                    {
                        itemRows.Add(new[] { "No items in this order", string.Empty, string.Empty, string.Empty });
                    }

                    DrawTable(context, new[] { "Item Name", "Amount", "Unit", "Quantity" }, itemRows, new[] { 0.45, 0.15, 0.2, 0.2 });

                    if (!string.IsNullOrWhiteSpace(order.Notes))
                    {
                        WriteSpacing(context, 8);
                        DrawSectionTitle(context, "Notes");
                        WriteParagraph(context, order.Notes, LabelFont, TextBrush, 80);
                    }
                }

                DrawFooter(document, $"Generated on {DateTime.Now:MMMM dd, yyyy 'at' HH:mm} | Coftea Management System");

                var supplierLabel = string.IsNullOrWhiteSpace(order.SupplierName)
                    ? "Supplier"
                    : order.SupplierName.Replace(' ', '_');
                var poDate = order.OrderDate.ToString("yyyyMMdd");
                var fileName = MakeSafeFileName($"Coftea_{supplierLabel}_{poDate}_{purchaseOrderId}.pdf");
                var filePath = GetDownloadPath(fileName);
                document.Save(filePath);
                
                return filePath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating purchase order PDF: {ex.Message}");
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                throw new Exception($"Failed to generate PDF: {ex.Message}", ex);
            }
        }

        public async Task<string> GenerateActivityLogPDFAsync(DateTime startDate, DateTime endDate, List<InventoryActivityLog> entries)
        {
            try
            {
                entries ??= new List<InventoryActivityLog>();

                var safeFileName = MakeSafeFileName($"Coftea_ActivityLog_{startDate:yyyy_MM_dd}_to_{endDate:yyyy_MM_dd}.pdf");
                var filePath = GetDownloadPath(safeFileName);

                using var document = new PdfDocument();
                using (var context = new PdfPageContext(document, Margin))
                {
                    DrawReportHeader(context, "Inventory Activity Log", $"{startDate:MMMM dd, yyyy} - {endDate:MMMM dd, yyyy}");

                    var summaryText = entries.Count == 1
                        ? "1 inventory activity entry exported."
                        : $"{entries.Count} inventory activity entries exported.";
                    WriteTextLine(context, summaryText, LabelFont, TextBrush, 18);
                    WriteSpacing(context, 10);

                    var headers = new[]
                    {
                        "#",
                        "Date & Time",
                        "User",
                        "Action",
                        "Item",
                        "Qty Δ",
                        "Prev Qty",
                        "New Qty",
                        "Used For",
                        "Remarks"
                    };

                    var weights = new[]
                    {
                        0.45,
                        1.25,
                        1.15,
                        0.9,
                        1.35,
                        0.75,
                        0.95,
                        0.95,
                        1.1,
                        1.55
                    };

                    var rows = entries
                        .Select((entry, index) =>
                        {
                            var unit = entry?.UnitOfMeasurement ?? string.Empty;
                            var previousQty = SanitizeCell(CombineQuantity(entry?.PreviousQuantityText, unit));
                            var newQty = SanitizeCell(CombineQuantity(entry?.NewQuantityText, unit));

                            return new[]
                            {
                                (index + 1).ToString(),
                                SanitizeCell(entry?.FormattedTimestampShort),
                                SanitizeCell(entry?.UserDisplay ?? entry?.UserFullName ?? entry?.UserEmail),
                                SanitizeCell(entry?.ActionDisplay ?? entry?.ActionText ?? entry?.Action),
                                SanitizeCell(entry?.ItemName),
                                SanitizeCell(entry?.QuantityChangeDisplay ?? entry?.QuantityChangeText),
                                previousQty,
                                newQty,
                                SanitizeCell(entry?.UsedForProductText),
                                SanitizeCell(entry?.RemarksText)
                            };
                        })
                        .ToList();

                    DrawTable(context, headers, rows, weights);
                }

                DrawFooter(document, $"Generated on {DateTime.Now:MMMM dd, yyyy 'at' HH:mm} | Coftea Inventory Activity Log");
                document.Save(filePath);
                return filePath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating activity log PDF: {ex.Message}");
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                throw;
            }
        }

        private void DrawReportHeader(PdfPageContext context, string title, string? subtitle)
        {
            context.EnsureSpace(60);
            context.Graphics.DrawString(title, TitleFont, TitleBrush, new XRect(context.Left, context.CurrentY, context.ContentWidth, 32), XStringFormats.TopLeft);
            context.CurrentY += 34;

            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                context.Graphics.DrawString(subtitle, SubTitleFont, TextBrush, new XRect(context.Left, context.CurrentY, context.ContentWidth, 24), XStringFormats.TopLeft);
                context.CurrentY += 26;
            }

            context.CurrentY += 6;
        }

        private void DrawSectionTitle(PdfPageContext context, string title)
        {
            context.EnsureSpace(24);
            context.Graphics.DrawString(title, SectionTitleFont, TitleBrush, new XRect(context.Left, context.CurrentY, context.ContentWidth, 20), XStringFormats.TopLeft);
            context.CurrentY += 22;
        }

        private void DrawStatBoxRow(PdfPageContext context, IEnumerable<(string Label, string Value, XColor Color)> boxes)
        {
            var items = boxes?.ToList() ?? new List<(string Label, string Value, XColor Color)>();
            if (items.Count == 0)
            {
                return;
            }

            var spacing = 18d;
            var width = (context.ContentWidth - spacing * (items.Count - 1)) / items.Count;
            var height = 60d;

            context.EnsureSpace(height + 18);

            var x = context.Left;
            foreach (var (label, value, color) in items)
            {
                DrawStatBox(context, x, context.CurrentY, width, height, label, value, color);
                x += width + spacing;
            }

            context.CurrentY += height + 18;
        }

        private void DrawStatBox(PdfPageContext context, double x, double y, double width, double height, string label, string value, XColor color)
        {
            var rect = new XRect(x, y, width, height);
            var pen = new XPen(XColor.FromArgb(210, 210, 210), 0.75);
            var brush = new XSolidBrush(color);

            context.Graphics.DrawRectangle(pen, brush, rect);

            var labelRect = new XRect(rect.X + 8, rect.Y + 6, rect.Width - 16, 18);
            var valueRect = new XRect(rect.X + 8, rect.Y + 26, rect.Width - 16, rect.Height - 32);

            context.Graphics.DrawString(label, LabelFont, TextBrush, labelRect, XStringFormats.TopLeft);
            context.Graphics.DrawString(value, ValueFont, TitleBrush, valueRect, XStringFormats.TopLeft);
        }

        private void DrawTable(PdfPageContext context, string[] headers, IReadOnlyList<string[]> rows, double[]? columnWeights = null)
        {
            if (headers == null || headers.Length == 0)
            {
                return;
            }

            var columnCount = headers.Length;
            var weights = columnWeights != null && columnWeights.Length == columnCount
                ? columnWeights
                : Enumerable.Repeat(1d, columnCount).ToArray();
            var weightTotal = weights.Sum();

            var columnWidths = weights
                .Select(w => context.ContentWidth * (w / weightTotal))
                .ToArray();

            var headerHeight = 24d;
            var rowHeight = 20d;
            var rowIndex = 0;
            var hasRows = rows != null && rows.Count > 0;

            while (true)
            {
                if (context.EnsureSpace(headerHeight))
                {
                    continue;
                }

                DrawTableHeader(context, headers, columnWidths, headerHeight);
                context.CurrentY += headerHeight;

                if (!hasRows)
                {
                    if (context.EnsureSpace(rowHeight))
                    {
                        continue;
                    }

                    DrawTableRow(context, new[] { "No data available" }, columnWidths, rowHeight);
                    context.CurrentY += rowHeight;
                    break;
                }

                while (rowIndex < rows.Count)
                {
                    if (context.EnsureSpace(rowHeight))
                    {
                        break;
                    }

                    DrawTableRow(context, rows[rowIndex] ?? Array.Empty<string>(), columnWidths, rowHeight);
                    context.CurrentY += rowHeight;
                    rowIndex++;
                }

                if (rowIndex >= rows.Count)
                {
                    break;
                }
            }

            context.CurrentY += 20;
        }

        private void DrawTableHeader(PdfPageContext context, string[] headers, double[] widths, double height)
        {
            var x = context.Left;
            for (var i = 0; i < headers.Length; i++)
            {
                var rect = new XRect(x, context.CurrentY, widths[i], height);
                context.Graphics.DrawRectangle(TableBorderPen, TableHeaderBrush, rect);
                context.Graphics.DrawString(headers[i], TableHeaderFont, XBrushes.White,
                    new XRect(rect.X + 6, rect.Y, rect.Width - 12, rect.Height), XStringFormats.CenterLeft);
                x += widths[i];
            }
        }

        private void DrawTableRow(PdfPageContext context, string[] values, double[] widths, double height)
        {
            var x = context.Left;
            for (var i = 0; i < widths.Length; i++)
            {
                var value = i < values.Length ? values[i] ?? string.Empty : string.Empty;
                var rect = new XRect(x, context.CurrentY, widths[i], height);
                context.Graphics.DrawRectangle(TableBorderPen, TableRowBrush, rect);
                context.Graphics.DrawString(value, TableRowFont, TextBrush,
                    new XRect(rect.X + 6, rect.Y, rect.Width - 12, rect.Height), XStringFormats.CenterLeft);
                x += widths[i];
            }
        }

        private void DrawFooter(PdfDocument document, string footerText)
        {
            foreach (var page in document.Pages.Cast<PdfPage>())
            {
                using var gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
                var rect = new XRect(Margin, page.Height - Margin + 5, page.Width - Margin * 2, 20);
                gfx.DrawString(footerText, SmallFont, MutedBrush, rect, XStringFormats.Center);
            }
        }

        private void WriteTextLine(PdfPageContext context, string text, XFont font, XBrush brush, double lineHeight = 16)
        {
            context.EnsureSpace(lineHeight);
            context.Graphics.DrawString(text, font, brush, new XRect(context.Left, context.CurrentY, context.ContentWidth, lineHeight), XStringFormats.TopLeft);
            context.CurrentY += lineHeight;
        }

        private void WriteParagraph(PdfPageContext context, string text, XFont font, XBrush brush, double blockHeight)
        {
            context.EnsureSpace(blockHeight);
            var formatter = new XTextFormatter(context.Graphics);
            formatter.DrawString(text, font, brush, new XRect(context.Left, context.CurrentY, context.ContentWidth, blockHeight));
            context.CurrentY += blockHeight;
        }

        private void WriteSpacing(PdfPageContext context, double spacing)
        {
            context.EnsureSpace(spacing);
            context.CurrentY += spacing;
        }

        private static string CombineQuantity(string? value, string? unit)
        {
            var quantity = string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();
            if (string.IsNullOrWhiteSpace(unit))
            {
                return quantity;
            }

            return $"{quantity} {unit}".Trim();
        }

        private static string SanitizeCell(string? value, int maxLength = 80)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "—";
            }

            var sanitized = value.Replace("\r", " ").Replace("\n", " ").Trim();
            if (sanitized.Length > maxLength)
            {
                sanitized = sanitized[..(maxLength - 1)] + "…";
            }

            return sanitized;
        }

        private static string MakeSafeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return "Coftea_Report.pdf";
            }

            var invalid = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(fileName.Length);

            foreach (var ch in fileName)
            {
                if (char.IsWhiteSpace(ch))
                {
                    builder.Append('_');
                }
                else
                {
                    builder.Append(invalid.Contains(ch) ? '_' : ch);
                }
            }

            return builder.ToString();
        }

        private async Task<Dictionary<string, string>> GetInventoryUnitsMapAsync()
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var items = await _database.GetInventoryItemsAsyncCached();
                if (items == null)
                {
                    return map;
                }

                foreach (var item in items)
                {
                    if (!string.IsNullOrWhiteSpace(item?.itemName) && !string.IsNullOrWhiteSpace(item.unitOfMeasurement))
                    {
                        map[item.itemName] = item.unitOfMeasurement;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error building inventory unit map: {ex.Message}");
            }

            return map;
        }

        private string GetDownloadPath(string fileName)
        {
            try
            {
                string downloadPath;
                var platform = DeviceInfo.Platform;

                if (platform == DevicePlatform.Android)
                {
                    var possiblePaths = new[]
                    {
                        "/storage/emulated/0/Download",
                        "/storage/emulated/0/Downloads",
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/Download",
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/Downloads"
                    };

                    downloadPath = possiblePaths.FirstOrDefault(Directory.Exists);

                    if (string.IsNullOrEmpty(downloadPath))
                    {
                        downloadPath = Path.Combine(FileSystem.AppDataDirectory, "Downloads");
                    }

                    if (!Directory.Exists(downloadPath))
                    {
                        Directory.CreateDirectory(downloadPath);
                    }
                }
                else if (platform == DevicePlatform.iOS || platform == DevicePlatform.MacCatalyst)
                {
                    downloadPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Downloads");
                }
                else if (platform == DevicePlatform.WinUI || platform == DevicePlatform.macOS)
                {
                    downloadPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            }
            else
            {
                    downloadPath = Path.Combine(FileSystem.AppDataDirectory, "Downloads");
                }

                if (!Directory.Exists(downloadPath))
                {
                    Directory.CreateDirectory(downloadPath);
                }

                return Path.Combine(downloadPath, fileName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Warning: Falling back to app data directory for PDF export: {ex.Message}");
                return Path.Combine(FileSystem.AppDataDirectory, fileName);
            }
        }

        private sealed class PdfPageContext : IDisposable
        {
            private readonly PdfDocument _document;

            public PdfPageContext(PdfDocument document, double margin)
            {
                _document = document;
                Margin = margin;
                AddPage();
            }

            public PdfPage Page { get; private set; } = default!;
            public XGraphics Graphics { get; private set; } = default!;
            public double Margin { get; }
            public double CurrentY { get; set; }
            public double Left => Margin;
            public double ContentWidth => Page.Width - Margin * 2;

            public bool EnsureSpace(double requiredHeight)
            {
                if (CurrentY + requiredHeight <= Page.Height - Margin)
                {
                    return false;
                }

                AddPage();
                return true;
            }

            private void AddPage()
            {
                Graphics?.Dispose();
                Page = _document.AddPage();
                Graphics = XGraphics.FromPdfPage(Page);
                CurrentY = Margin;
            }

            public void Dispose()
            {
                Graphics?.Dispose();
            }
        }

        public async Task<string> GenerateWeeklyReportPDFAsync(DateTime startDate, DateTime endDate, List<TransactionHistoryModel> transactions, List<TrendItem> topItems)
        {
            try
            {
                transactions ??= new List<TransactionHistoryModel>();
                topItems ??= new List<TrendItem>();

                var deductionsWithUnits = await _database.GetInventoryDeductionsForPeriodAsync(startDate, endDate);
                var inventoryDeductions = deductionsWithUnits.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.totalDeducted);
                var unitMap = deductionsWithUnits.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.unitOfMeasurement ?? string.Empty, StringComparer.OrdinalIgnoreCase);

                var inventoryUnitMap = await GetInventoryUnitsMapAsync();
                foreach (var kvp in inventoryUnitMap)
                {
                    if (!unitMap.ContainsKey(kvp.Key))
                    {
                        unitMap[kvp.Key] = kvp.Value;
                    }
                }

                var totalSales = transactions.Sum(t => t?.Total ?? 0);
                var totalOrders = transactions.Count;
                var cashSales = transactions.Where(t => t?.PaymentMethod?.Equals("Cash", StringComparison.OrdinalIgnoreCase) == true).Sum(t => t?.Total ?? 0);
                var gcashSales = transactions.Where(t => t?.PaymentMethod?.Equals("GCash", StringComparison.OrdinalIgnoreCase) == true).Sum(t => t?.Total ?? 0);
                var bankSales = transactions.Where(t => t?.PaymentMethod?.Equals("Bank", StringComparison.OrdinalIgnoreCase) == true).Sum(t => t?.Total ?? 0);

                using var document = new PdfDocument();
                using (var context = new PdfPageContext(document, Margin))
                {
                    DrawReportHeader(context, "Coftea Weekly Sales Report", $"{startDate:MMMM dd, yyyy} - {endDate:MMMM dd, yyyy}");

                    DrawSectionTitle(context, "Sales Summary");
                    DrawStatBoxRow(context, new[]
                    {
                        ("Total Sales", $"₱{totalSales:N2}", XColor.FromArgb(200, 230, 255)),
                        ("Total Orders", $"{totalOrders}", XColor.FromArgb(200, 230, 255)),
                        ("Average Order", $"₱{(totalOrders > 0 ? totalSales / totalOrders : 0):N2}", XColor.FromArgb(200, 230, 255))
                    });

                    DrawSectionTitle(context, "Payment Methods");
                    DrawStatBoxRow(context, new[]
                    {
                        ("Cash", $"₱{cashSales:N2}", XColor.FromArgb(200, 255, 230)),
                        ("GCash", $"₱{gcashSales:N2}", XColor.FromArgb(200, 230, 255)),
                        ("Bank", $"₱{bankSales:N2}", XColor.FromArgb(230, 200, 255))
                    });

                    DrawSectionTitle(context, "Top Selling Items");
                    var topRows = topItems
                        .Take(10)
                        .Select((item, index) => new[]
                        {
                            $"#{index + 1} {item?.Name ?? "Unknown Item"}",
                            $"{item?.Count ?? 0} orders"
                        })
                        .ToList();

                    if (topRows.Count == 0)
                    {
                        topRows.Add(new[] { "No items to display", string.Empty });
                    }

                    DrawTable(context, new[] { "Item", "Orders" }, topRows);

                    // Add new section: All Products by Category
                    DrawSectionTitle(context, "All Products - Order Summary by Category");
                    var allProducts = await _database.GetProductsAsync();
                    var productOrderCounts = transactions
                        .Where(t => !string.IsNullOrWhiteSpace(t.DrinkName))
                        .GroupBy(t => t.DrinkName.Trim(), StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity > 0 ? x.Quantity : 1), StringComparer.OrdinalIgnoreCase);

                    // Group products by category
                    var productsByCategory = allProducts
                        .Where(p => !string.IsNullOrWhiteSpace(p.ProductName))
                        .GroupBy(p => string.IsNullOrWhiteSpace(p.Category) ? "Other" : p.Category.Trim())
                        .OrderBy(g => g.Key)
                        .ToList();

                    foreach (var categoryGroup in productsByCategory)
                    {
                        context.EnsureSpace(30);
                        WriteTextLine(context, $"{categoryGroup.Key}:", SectionTitleFont, TitleBrush, 16);
                        context.CurrentY += 4;

                        var categoryRows = categoryGroup
                            .OrderBy(p => p.ProductName)
                            .Select(p =>
                            {
                                var orderCount = productOrderCounts.TryGetValue(p.ProductName, out var count) ? count : 0;
                                return new[] { p.ProductName ?? "Unknown", orderCount.ToString() };
                            })
                            .ToList();

                        if (categoryRows.Count == 0)
                        {
                            categoryRows.Add(new[] { "No products in this category", "0" });
                        }

                        DrawTable(context, new[] { "Product Name", "Orders" }, categoryRows, new[] { 0.7, 0.3 });
                        context.CurrentY += 8;
                    }

                    DrawSectionTitle(context, "Inventory Deductions");
                    var deductionRows = inventoryDeductions
                        .OrderByDescending(x => x.Value)
                        .Take(15)
                        .Select(d =>
                        {
                            var unit = unitMap.TryGetValue(d.Key, out var value) ? value : string.Empty;
                            var amountText = string.IsNullOrWhiteSpace(unit)
                                ? $"{d.Value:N1}"
                                : $"{d.Value:N1} {unit}";
                            return new[] { d.Key, amountText };
                        })
                        .ToList();

                    if (deductionRows.Count == 0)
                    {
                        deductionRows.Add(new[] { "No inventory deductions to display", string.Empty });
                    }

                    DrawTable(context, new[] { "Item", "Amount" }, deductionRows);
                }

                DrawFooter(document, $"Generated on {DateTime.Now:MMMM dd, yyyy 'at' HH:mm} | Coftea Sales Management System");

                var endInclusive = endDate > startDate ? endDate.AddDays(-1) : endDate;
                var dateLabel = $"{startDate:yyyyMMdd}-{endInclusive:yyyyMMdd}";
                var fileName = MakeSafeFileName($"Coftea_WeeklyReports_{dateLabel}.pdf");
                var filePath = GetDownloadPath(fileName);
                document.Save(filePath);

                return filePath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating weekly PDF report: {ex.Message}");
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                throw;
            }
        }

    }
}

