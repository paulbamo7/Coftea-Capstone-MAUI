using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Coftea_Capstone.Models;
using Microsoft.Maui.Storage;
using System.Text;
<<<<<<< Updated upstream
=======
using System.IO;
>>>>>>> Stashed changes

namespace Coftea_Capstone.Services
{
    public interface IPDFReportService
    {
        Task<string> GenerateWeeklyReportAsync(DateTime startDate, DateTime endDate, List<TransactionHistoryModel> transactions, List<TrendItem> topItems);
        Task<string> GenerateMonthlyReportAsync(DateTime startDate, DateTime endDate, List<TransactionHistoryModel> transactions, List<TrendItem> topItems);
<<<<<<< Updated upstream
=======
        // Generates HTML report files (can be opened and printed to PDF by browser)
>>>>>>> Stashed changes
    }

    public class PDFReportService : IPDFReportService
    {
        private readonly Database _database;

        public PDFReportService()
        {
            _database = new Database();
        }

        public async Task<string> GenerateWeeklyReportAsync(DateTime startDate, DateTime endDate, List<TransactionHistoryModel> transactions, List<TrendItem> topItems)
        {
            try
            {
                // Get inventory deduction data for the week
                var inventoryDeductions = await GetInventoryDeductionsForPeriodAsync(startDate, endDate);
                
                // Generate HTML content for the report
                var htmlContent = GenerateWeeklyReportHTML(startDate, endDate, transactions, topItems, inventoryDeductions);
                
                // Save as HTML file (browser can print/save as PDF)
                var fileName = $"Weekly_Report_{startDate:yyyy_MM_dd}_to_{endDate:yyyy_MM_dd}.html";
                
                // Try to save to Download folder first (most accessible in emulator)
                string filePath;
                try
                {
                    // Try Download folder first - most accessible in emulator
                    var downloadPath = "/storage/emulated/0/Download";
                    if (!Directory.Exists(downloadPath))
                    {
                        Directory.CreateDirectory(downloadPath);
                    }
                    filePath = Path.Combine(downloadPath, fileName);
                }
                catch
                {
                    // Fallback to app data directory
                    filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);
                }
                
                // Save HTML content to file
                await File.WriteAllTextAsync(filePath, htmlContent, Encoding.UTF8);
                
                return filePath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating weekly report: {ex.Message}");
                throw;
            }
        }

        public async Task<string> GenerateMonthlyReportAsync(DateTime startDate, DateTime endDate, List<TransactionHistoryModel> transactions, List<TrendItem> topItems)
        {
            try
            {
                // Get inventory deduction data for the month
                var inventoryDeductions = await GetInventoryDeductionsForPeriodAsync(startDate, endDate);
                
                // Generate HTML content for the report
                var htmlContent = GenerateMonthlyReportHTML(startDate, endDate, transactions, topItems, inventoryDeductions);
                
                // Save as HTML file (browser can print/save as PDF)
                var fileName = $"Monthly_Report_{startDate:yyyy_MM}_to_{endDate:yyyy_MM}.html";
                
                // Try to save to Download folder first (most accessible in emulator)
                string filePath;
                try
                {
                    // Try Download folder first - most accessible in emulator
                    var downloadPath = "/storage/emulated/0/Download";
                    if (!Directory.Exists(downloadPath))
                    {
                        Directory.CreateDirectory(downloadPath);
                    }
                    filePath = Path.Combine(downloadPath, fileName);
                }
                catch
                {
                    // Fallback to app data directory
                    filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);
                }
                
                // Save HTML content to file
                await File.WriteAllTextAsync(filePath, htmlContent, Encoding.UTF8);
                
                return filePath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating monthly report: {ex.Message}");
                throw;
            }
        }

<<<<<<< Updated upstream
=======
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
                        var qty = item.ApprovedQuantity > 0 ? item.ApprovedQuantity : item.RequestedQuantity;
                        PdfGridRow row = itemsGrid.Rows.Add();
                        row.Cells[0].Value = item.ItemName ?? "";
                        row.Cells[1].Value = qty.ToString();
                        row.Cells[2].Value = item.UnitOfMeasurement ?? "pcs";
                        row.Cells[3].Value = qty.ToString(); // Quantity (same as amount - represents total needed)
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

>>>>>>> Stashed changes
        private async Task<Dictionary<string, double>> GetInventoryDeductionsForPeriodAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                // Simplified approach to avoid timeout - use transaction data only
                var transactions = await _database.GetTransactionsByDateRangeAsync(startDate, endDate);
                
                // Group transactions by product and calculate basic deductions
                var deductions = new Dictionary<string, double>();
                
                // Group transactions by product name to reduce database calls
                var productGroups = transactions
                    .Where(t => !string.IsNullOrEmpty(t.DrinkName))
                    .GroupBy(t => t.DrinkName)
                    .ToList();
                
                foreach (var productGroup in productGroups)
                {
                    var productName = productGroup.Key;
                    var totalQuantity = productGroup.Sum(t => t.Quantity);
                    
                    // Add basic deductions based on product type and quantity
                    AddBasicDeductionsForProduct(deductions, productName, totalQuantity);
                    
                    // Add cup and straw deductions for each transaction
                    foreach (var transaction in productGroup)
                    {
                        AddCupAndStrawDeductions(deductions, transaction.Size, transaction.Quantity);
                    }
                }
                
                return deductions;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting inventory deductions: {ex.Message}");
                return new Dictionary<string, double>();
            }
        }

        private void AddBasicDeductionsForProduct(Dictionary<string, double> deductions, string productName, int totalQuantity)
        {
            try
            {
                // Add basic ingredient deductions based on product type
                // This is a simplified approach to avoid database timeouts
                var productNameLower = productName.ToLowerInvariant();
                
                // Common ingredients based on product type
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
                    // Generic deductions for unknown products
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
                _ => 1.5 // Default to medium
            };
        }

        private void AddCupAndStrawDeductions(Dictionary<string, double> deductions, string size, int quantity)
        {
            var sizeMultiplier = GetSizeMultiplier(size);
            var totalQuantity = quantity * sizeMultiplier;
            
            // Add cup deduction
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
            
            // Add straw deduction
            if (deductions.ContainsKey("Straw"))
            {
                deductions["Straw"] += quantity; // One straw per drink regardless of size
            }
            else
            {
                deductions["Straw"] = quantity;
            }
        }

        private string GenerateWeeklyReportHTML(DateTime startDate, DateTime endDate, List<TransactionHistoryModel> transactions, List<TrendItem> topItems, Dictionary<string, double> inventoryDeductions)
        {
            var totalSales = transactions.Sum(t => t.Total);
            var totalOrders = transactions.Count;
            var cashSales = transactions.Where(t => t.PaymentMethod?.Equals("Cash", StringComparison.OrdinalIgnoreCase) == true).Sum(t => t.Total);
            var gcashSales = transactions.Where(t => t.PaymentMethod?.Equals("GCash", StringComparison.OrdinalIgnoreCase) == true).Sum(t => t.Total);
            var bankSales = transactions.Where(t => t.PaymentMethod?.Equals("Bank", StringComparison.OrdinalIgnoreCase) == true).Sum(t => t.Total);

            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>Weekly Sales Report - {startDate:MMM dd} to {endDate:MMM dd}, {startDate:yyyy}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; background-color: #f5f5f5; }}
        .container {{ max-width: 800px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; margin-bottom: 30px; border-bottom: 3px solid #D4A574; padding-bottom: 20px; }}
        .header h1 {{ color: #5B4F45; margin: 0; font-size: 28px; }}
        .header h2 {{ color: #8B7355; margin: 10px 0 0 0; font-size: 18px; font-weight: normal; }}
        .section {{ margin-bottom: 25px; }}
        .section h3 {{ color: #5B4F45; border-left: 4px solid #D4A574; padding-left: 15px; margin-bottom: 15px; }}
        .summary-grid {{ display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 15px; margin-bottom: 20px; }}
        .summary-card {{ background: linear-gradient(135deg, #F5E6D8, #E8D5C4); padding: 15px; border-radius: 8px; text-align: center; }}
        .summary-card h4 {{ margin: 0 0 5px 0; color: #5B4F45; font-size: 14px; }}
        .summary-card .value {{ font-size: 20px; font-weight: bold; color: #2E7D32; }}
        .payment-methods {{ display: grid; grid-template-columns: repeat(3, 1fr); gap: 15px; margin-bottom: 20px; }}
        .payment-card {{ padding: 15px; border-radius: 8px; text-align: center; }}
        .cash {{ background: linear-gradient(135deg, #E8F5E8, #C8E6C9); }}
        .gcash {{ background: linear-gradient(135deg, #E3F2FD, #BBDEFB); }}
        .bank {{ background: linear-gradient(135deg, #F3E5F5, #E1BEE7); }}
        .top-items {{ background: #f9f9f9; padding: 15px; border-radius: 8px; }}
        .top-item {{ display: flex; justify-content: space-between; align-items: center; padding: 8px 0; border-bottom: 1px solid #eee; }}
        .top-item:last-child {{ border-bottom: none; }}
        .inventory-section {{ background: #fff3e0; padding: 15px; border-radius: 8px; border-left: 4px solid #FF9800; }}
        .inventory-item {{ display: flex; justify-content: space-between; padding: 5px 0; }}
        .footer {{ text-align: center; margin-top: 30px; padding-top: 20px; border-top: 2px solid #eee; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Coftea Weekly Sales Report</h1>
            <h2>{startDate:MMMM dd, yyyy} - {endDate:MMMM dd, yyyy}</h2>
        </div>

        <div class='section'>
            <h3>📊 Sales Summary</h3>
            <div class='summary-grid'>
                <div class='summary-card'>
                    <h4>Total Sales</h4>
                    <div class='value'>₱{totalSales:N2}</div>
                </div>
                <div class='summary-card'>
                    <h4>Total Orders</h4>
                    <div class='value'>{totalOrders:N0}</div>
                </div>
                <div class='summary-card'>
                    <h4>Average Order Value</h4>
                    <div class='value'>₱{(totalOrders > 0 ? totalSales / totalOrders : 0):N2}</div>
                </div>
            </div>
        </div>

        <div class='section'>
            <h3>💳 Payment Methods</h3>
            <div class='payment-methods'>
                <div class='payment-card cash'>
                    <h4>Cash</h4>
                    <div class='value'>₱{cashSales:N2}</div>
                </div>
                <div class='payment-card gcash'>
                    <h4>GCash</h4>
                    <div class='value'>₱{gcashSales:N2}</div>
                </div>
                <div class='payment-card bank'>
                    <h4>Bank Transfer</h4>
                    <div class='value'>₱{bankSales:N2}</div>
                </div>
            </div>
        </div>

        <div class='section'>
            <h3>🏆 Top Selling Items</h3>
            <div class='top-items'>
                {string.Join("", topItems.Take(10).Select((item, index) => $@"
                <div class='top-item'>
                    <span><strong>#{index + 1}</strong> {item.Name}</span>
                    <span>{item.Count} orders</span>
                </div>"))}
            </div>
        </div>

        <div class='section'>
            <h3>📦 Inventory Deductions</h3>
            <div class='inventory-section'>
                <p><strong>Total ingredients and supplies deducted this week:</strong></p>
                {string.Join("", inventoryDeductions.OrderByDescending(x => x.Value).Select(kvp => $@"
                <div class='inventory-item'>
                    <span>{kvp.Key}</span>
                    <span>{kvp.Value:N1} units</span>
                </div>"))}
            </div>
        </div>

        <div class='footer'>
            <p>Generated on {DateTime.Now:MMMM dd, yyyy 'at' HH:mm} | Coftea Sales Management System</p>
        </div>
    </div>
</body>
</html>";

            return html;
        }

        private string GenerateMonthlyReportHTML(DateTime startDate, DateTime endDate, List<TransactionHistoryModel> transactions, List<TrendItem> topItems, Dictionary<string, double> inventoryDeductions)
        {
            var totalSales = transactions.Sum(t => t.Total);
            var totalOrders = transactions.Count;
            var cashSales = transactions.Where(t => t.PaymentMethod?.Equals("Cash", StringComparison.OrdinalIgnoreCase) == true).Sum(t => t.Total);
            var gcashSales = transactions.Where(t => t.PaymentMethod?.Equals("GCash", StringComparison.OrdinalIgnoreCase) == true).Sum(t => t.Total);
            var bankSales = transactions.Where(t => t.PaymentMethod?.Equals("Bank", StringComparison.OrdinalIgnoreCase) == true).Sum(t => t.Total);

            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>Monthly Sales Report - {startDate:MMMM yyyy}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; background-color: #f5f5f5; }}
        .container {{ max-width: 800px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; margin-bottom: 30px; border-bottom: 3px solid #D4A574; padding-bottom: 20px; }}
        .header h1 {{ color: #5B4F45; margin: 0; font-size: 28px; }}
        .header h2 {{ color: #8B7355; margin: 10px 0 0 0; font-size: 18px; font-weight: normal; }}
        .section {{ margin-bottom: 25px; }}
        .section h3 {{ color: #5B4F45; border-left: 4px solid #D4A574; padding-left: 15px; margin-bottom: 15px; }}
        .summary-grid {{ display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 15px; margin-bottom: 20px; }}
        .summary-card {{ background: linear-gradient(135deg, #F5E6D8, #E8D5C4); padding: 15px; border-radius: 8px; text-align: center; }}
        .summary-card h4 {{ margin: 0 0 5px 0; color: #5B4F45; font-size: 14px; }}
        .summary-card .value {{ font-size: 20px; font-weight: bold; color: #2E7D32; }}
        .payment-methods {{ display: grid; grid-template-columns: repeat(3, 1fr); gap: 15px; margin-bottom: 20px; }}
        .payment-card {{ padding: 15px; border-radius: 8px; text-align: center; }}
        .cash {{ background: linear-gradient(135deg, #E8F5E8, #C8E6C9); }}
        .gcash {{ background: linear-gradient(135deg, #E3F2FD, #BBDEFB); }}
        .bank {{ background: linear-gradient(135deg, #F3E5F5, #E1BEE7); }}
        .top-items {{ background: #f9f9f9; padding: 15px; border-radius: 8px; }}
        .top-item {{ display: flex; justify-content: space-between; align-items: center; padding: 8px 0; border-bottom: 1px solid #eee; }}
        .top-item:last-child {{ border-bottom: none; }}
        .inventory-section {{ background: #fff3e0; padding: 15px; border-radius: 8px; border-left: 4px solid #FF9800; }}
        .inventory-item {{ display: flex; justify-content: space-between; padding: 5px 0; }}
        .footer {{ text-align: center; margin-top: 30px; padding-top: 20px; border-top: 2px solid #eee; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Coftea Monthly Sales Report</h1>
            <h2>{startDate:MMMM yyyy}</h2>
        </div>

        <div class='section'>
            <h3>📊 Sales Summary</h3>
            <div class='summary-grid'>
                <div class='summary-card'>
                    <h4>Total Sales</h4>
                    <div class='value'>₱{totalSales:N2}</div>
                </div>
                <div class='summary-card'>
                    <h4>Total Orders</h4>
                    <div class='value'>{totalOrders:N0}</div>
                </div>
                <div class='summary-card'>
                    <h4>Average Order Value</h4>
                    <div class='value'>₱{(totalOrders > 0 ? totalSales / totalOrders : 0):N2}</div>
                </div>
                <div class='summary-card'>
                    <h4>Daily Average</h4>
                    <div class='value'>₱{(totalSales / Math.Max(1, (endDate - startDate).Days)):N2}</div>
                </div>
            </div>
        </div>

        <div class='section'>
            <h3>💳 Payment Methods</h3>
            <div class='payment-methods'>
                <div class='payment-card cash'>
                    <h4>Cash</h4>
                    <div class='value'>₱{cashSales:N2}</div>
                </div>
                <div class='payment-card gcash'>
                    <h4>GCash</h4>
                    <div class='value'>₱{gcashSales:N2}</div>
                </div>
                <div class='payment-card bank'>
                    <h4>Bank Transfer</h4>
                    <div class='value'>₱{bankSales:N2}</div>
                </div>
            </div>
        </div>

        <div class='section'>
            <h3>🏆 Top Selling Items</h3>
            <div class='top-items'>
                {string.Join("", topItems.Take(15).Select((item, index) => $@"
                <div class='top-item'>
                    <span><strong>#{index + 1}</strong> {item.Name}</span>
                    <span>{item.Count} orders</span>
                </div>"))}
            </div>
        </div>

        <div class='section'>
            <h3>📦 Inventory Deductions</h3>
            <div class='inventory-section'>
                <p><strong>Total ingredients and supplies deducted this month:</strong></p>
                {string.Join("", inventoryDeductions.OrderByDescending(x => x.Value).Select(kvp => $@"
                <div class='inventory-item'>
                    <span>{kvp.Key}</span>
                    <span>{kvp.Value:N1} units</span>
                </div>"))}
            </div>
        </div>

        <div class='footer'>
            <p>Generated on {DateTime.Now:MMMM dd, yyyy 'at' HH:mm} | Coftea Sales Management System</p>
        </div>
    </div>
</body>
</html>";

            return html;
        }
    }
}
