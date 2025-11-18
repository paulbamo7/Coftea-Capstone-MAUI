using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Globalization;
using Coftea_Capstone.Models;
using Coftea_Capstone.Models.Reports;
using Coftea_Capstone.C_;
using System;
using System.Threading;

namespace Coftea_Capstone.Services
{
    public class SalesReportSummary
    {
        public int ActiveDays { get; set; }
        public string MostBoughtToday { get; set; }
        public string TrendingToday { get; set; }
        public double TrendingPercentage { get; set; }
        public decimal TotalSalesToday { get; set; }
        public int TotalOrdersToday { get; set; }
        public int TotalOrdersThisWeek { get; set; }
        public IEnumerable<TrendItem> TopCoffeeToday { get; set; }
        public IEnumerable<TrendItem> TopMilkteaToday { get; set; }
        public IEnumerable<TrendItem> TopFrappeToday { get; set; }
        public IEnumerable<TrendItem> TopFruitSodaToday { get; set; }
        public IEnumerable<TrendItem> TopCoffeeWeekly { get; set; }
        public IEnumerable<TrendItem> TopMilkteaWeekly { get; set; }
        public IEnumerable<TrendItem> TopFrappeWeekly { get; set; }
        public IEnumerable<TrendItem> TopFruitSodaWeekly { get; set; }
        public IEnumerable<SalesReportPageModel> Reports { get; set; }
    }

    public enum SalesAggregateGrouping
    {
        Product,
        Day,
        Week,
        Month,
        Category
    }

    public interface ISalesReportService
    {
        Task<SalesReportSummary> GetSummaryAsync(DateTime startDate, DateTime endDate);
        Task<List<SalesAggregateRow>> GetAggregatesAsync(DateTime startDate, DateTime endDate, SalesAggregateGrouping grouping, CancellationToken cancellationToken = default);
    }

    // Real implementation using database
    public class DatabaseSalesReportService : ISalesReportService
    {
        private readonly Database _database;

        public DatabaseSalesReportService()
        {
            _database = new Database(); // Will use auto-detected host
        }

        private static List<KeyValuePair<string, int>> AggregateProductCounts(IEnumerable<TransactionHistoryModel> transactions)
        {
            return transactions
                .Where(t => !string.IsNullOrWhiteSpace(t.DrinkName))
                .GroupBy(t => t.DrinkName.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => new KeyValuePair<string, int>(g.Key, g.Sum(x => x.Quantity > 0 ? x.Quantity : 1)))
                .OrderByDescending(kvp => kvp.Value)
                .ToList();
        }

        private static string InferCategory(string? categoryFromDb, string productName)
        {
            if (!string.IsNullOrWhiteSpace(categoryFromDb))
            {
                return categoryFromDb.Trim();
            }

            var name = productName?.ToLowerInvariant() ?? string.Empty;

            if (name.Contains("frappe") || name.Contains("frap"))
                return "Frappe";

            if (name.Contains("fruit") || name.Contains("soda") || name.Contains("juice"))
                return "Fruit/Soda";

            if (name.Contains("tea"))
                return "Milktea";

            return "Coffee";
        }

        private static string GetDefaultColor(string category)
        {
            return category switch
            {
                "Frappe" => "#ac94f4",
                "Fruit/Soda" => "#F0E0C1",
                "Milktea" => "#f5dde0",
                _ => "#99E599",
            };
        }

        public async Task<SalesReportSummary> GetSummaryAsync(DateTime startDate, DateTime endDate)
        {
            // Use cache for frequently accessed date ranges (today, this week, this month)
            var cacheKey = $"sales_summary_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}";
            var isToday = startDate.Date == DateTime.Today && endDate.Date >= DateTime.Today;
            var cacheDuration = isToday ? TimeSpan.FromMinutes(1) : TimeSpan.FromMinutes(5);
            
            return await CacheService.GetOrSetAsync(cacheKey, async () =>
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                
                try
                {
                    System.Diagnostics.Debug.WriteLine($"🔍 SalesReportService: Getting summary from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
                    
                    // Get all transactions for the date range
                    var transactions = await _database.GetTransactionsByDateRangeAsync(startDate, endDate);
                System.Diagnostics.Debug.WriteLine($"📊 Found {transactions.Count} transaction rows");
                
                // Group by TransactionId to get distinct transactions (since GetTransactionsByDateRangeAsync returns one row per transaction_item)
                var distinctTransactions = transactions.GroupBy(t => t.TransactionId).Select(g => g.First()).ToList();
                
                // Calculate basic metrics
                var totalSales = distinctTransactions.Sum(t => t.Total);
                var totalOrders = distinctTransactions.Count;
                var activeDays = distinctTransactions.Select(t => t.TransactionDate.Date).Distinct().Count();
                // Aggregate product counts directly from transaction history
                var aggregatedProducts = AggregateProductCounts(transactions);

                if (!aggregatedProducts.Any())
                {
                    var fallbackProducts = await _database.GetTopProductsByDateRangeAsync(startDate, endDate, 50);
                    aggregatedProducts = fallbackProducts
                        .OrderByDescending(p => p.Value)
                        .Select(p => new KeyValuePair<string, int>(p.Key, p.Value))
                        .ToList();
                }

                // Separate by category
                var coffeeProducts = new List<TrendItem>();
                var milkTeaProducts = new List<TrendItem>();
                var frappeProducts = new List<TrendItem>();
                var fruitSodaProducts = new List<TrendItem>();

                // Get product details to include color codes and categories (case-insensitive lookup)
                var allProducts = await _database.GetProductsAsyncCached();
                var productLookup = allProducts
                    .Where(p => !string.IsNullOrWhiteSpace(p.ProductName))
                    .GroupBy(p => p.ProductName, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        g => g.Key,
                        g => g.First(),
                        StringComparer.OrdinalIgnoreCase);

                foreach (var product in aggregatedProducts)
                {
                    if (string.IsNullOrWhiteSpace(product.Key))
                    {
                        continue;
                    }

                    productLookup.TryGetValue(product.Key, out var productInfo);
                    var category = InferCategory(productInfo?.Category, product.Key);
                    var trendItem = new TrendItem
                    {
                        Name = product.Key,
                        Count = product.Value,
                        Category = category,
                        ColorCode = string.IsNullOrWhiteSpace(productInfo?.ColorCode) ? GetDefaultColor(category) : productInfo!.ColorCode!
                    };

                    switch (category.Trim().ToLowerInvariant())
                    {
                        case "frappe":
                            frappeProducts.Add(trendItem);
                            break;
                        case "fruit/soda":
                        case "fruitsoda":
                            fruitSodaProducts.Add(trendItem);
                            break;
                        case "milktea":
                        case "milk tea":
                        case "milk_tea":
                            milkTeaProducts.Add(trendItem);
                            break;
                        case "coffee":
                        default:
                            coffeeProducts.Add(trendItem);
                            break;
                    }
                }

                // Sort categories by count desc and take top 5 for each
                coffeeProducts = coffeeProducts
                    .OrderByDescending(i => i.Count)
                    .Take(5)
                    .ToList();

                milkTeaProducts = milkTeaProducts
                    .OrderByDescending(i => i.Count)
                    .Take(5)
                    .ToList();

                frappeProducts = frappeProducts
                    .OrderByDescending(i => i.Count)
                    .Take(5)
                    .ToList();

                fruitSodaProducts = fruitSodaProducts
                    .OrderByDescending(i => i.Count)
                    .Take(5)
                    .ToList();

                // Get most bought item across ALL categories (single item)
                var mostBought = aggregatedProducts.FirstOrDefault();
                
                // Calculate trending item based on recent demand patterns
                var trending = await GetTrendingItemAsync(startDate, endDate);

                System.Diagnostics.Debug.WriteLine($"✅ SalesReportService: Successfully processed data - Most bought: {mostBought.Key}, Total sales: {totalSales:C}");

                return new SalesReportSummary
                {
                    ActiveDays = activeDays,
                    MostBoughtToday = string.IsNullOrEmpty(mostBought.Key) ? "No data" : mostBought.Key,
                    TrendingToday = trending?.Key ?? "No data",
                    TrendingPercentage = trending?.Value ?? 0.0,
                    TotalSalesToday = totalSales,
                    TotalOrdersToday = totalOrders,
                    TotalOrdersThisWeek = totalOrders, // Same as today for now
                    TopCoffeeToday = coffeeProducts.Take(5),
                    TopMilkteaToday = milkTeaProducts.Take(5),
                    TopFrappeToday = frappeProducts.Take(5),
                    TopFruitSodaToday = fruitSodaProducts.Take(5),
                    TopCoffeeWeekly = coffeeProducts.Take(5),
                    TopMilkteaWeekly = milkTeaProducts.Take(5),
                    TopFrappeWeekly = frappeProducts.Take(5),
                    TopFruitSodaWeekly = fruitSodaProducts.Take(5),
                    Reports = new List<SalesReportPageModel>()
                };
                }
                catch (OperationCanceledException)
                {
                    System.Diagnostics.Debug.WriteLine("⏰ SalesReportService: Operation canceled/timeout");
                    return new SalesReportSummary
                    {
                        ActiveDays = 0,
                        MostBoughtToday = "No data available",
                        TrendingToday = "No data available",
                        TrendingPercentage = 0,
                        TotalSalesToday = 0,
                        TotalOrdersToday = 0,
                        TotalOrdersThisWeek = 0,
                        TopCoffeeToday = new List<TrendItem>(),
                        TopMilkteaToday = new List<TrendItem>(),
                        TopFrappeToday = new List<TrendItem>(),
                        TopFruitSodaToday = new List<TrendItem>(),
                        TopCoffeeWeekly = new List<TrendItem>(),
                        TopMilkteaWeekly = new List<TrendItem>(),
                        TopFrappeWeekly = new List<TrendItem>(),
                        TopFruitSodaWeekly = new List<TrendItem>(),
                        Reports = new List<SalesReportPageModel>()
                    };
                }
                catch (Exception ex)
                {
                    // Return empty data on error
                    System.Diagnostics.Debug.WriteLine($"❌ SalesReportService error: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                    return new SalesReportSummary
                    {
                        ActiveDays = 0,
                        MostBoughtToday = "No data available",
                        TrendingToday = "No data available",
                        TrendingPercentage = 0,
                        TotalSalesToday = 0,
                        TotalOrdersToday = 0,
                        TotalOrdersThisWeek = 0,
                        TopCoffeeToday = new List<TrendItem>(),
                        TopMilkteaToday = new List<TrendItem>(),
                        TopFrappeToday = new List<TrendItem>(),
                        TopFruitSodaToday = new List<TrendItem>(),
                        TopCoffeeWeekly = new List<TrendItem>(),
                        TopMilkteaWeekly = new List<TrendItem>(),
                        TopFrappeWeekly = new List<TrendItem>(),
                        TopFruitSodaWeekly = new List<TrendItem>(),
                        Reports = new List<SalesReportPageModel>()
                    };
                }
            }, cacheDuration);
        }

        private static (string Key, string Label, DateTime SortDate) ResolveGrouping(TransactionHistoryModel transaction, SalesAggregateGrouping grouping)
        {
            var date = transaction.TransactionDate;

            switch (grouping)
            {
                case SalesAggregateGrouping.Day:
                    var day = date.Date;
                    return (day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), day.ToString("MMM dd, yyyy", CultureInfo.InvariantCulture), day);

                case SalesAggregateGrouping.Week:
                    var dateOnly = date.Date;
                    var diffToMonday = (7 + ((int)dateOnly.DayOfWeek - (int)DayOfWeek.Monday)) % 7;
                    var startOfWeek = dateOnly.AddDays(-diffToMonday);
                    var endOfWeek = startOfWeek.AddDays(6);
                    var weekLabel = FormattableString.Invariant($"Week of {startOfWeek:MMM dd} - {endOfWeek:MMM dd}");
                    return ($"{startOfWeek:yyyy-MM-dd}", weekLabel, startOfWeek);

                case SalesAggregateGrouping.Month:
                    var monthStart = new DateTime(date.Year, date.Month, 1);
                    return ($"{monthStart:yyyy-MM}", monthStart.ToString("MMMM yyyy", CultureInfo.InvariantCulture), monthStart);

                case SalesAggregateGrouping.Category:
                    var category = InferCategory(null, transaction.DrinkName ?? "");
                    return (category.ToUpperInvariant(), category, DateTime.MinValue);

                case SalesAggregateGrouping.Product:
                default:
                    var productName = (transaction.DrinkName ?? "Unknown").Trim();
                    if (string.IsNullOrWhiteSpace(productName))
                    {
                        productName = "Uncategorised";
                    }
                    return (productName.ToUpperInvariant(), productName, DateTime.MinValue);
            }
        }

        private static SalesAggregateBreakdown ResolveBreakdown(SalesAggregateRow row, string paymentMethod)
        {
            if (string.IsNullOrWhiteSpace(paymentMethod))
            {
                return row.Cash;
            }

            paymentMethod = paymentMethod.Trim();

            if (paymentMethod.Equals("gcash", StringComparison.OrdinalIgnoreCase))
            {
                return row.GCash;
            }

            if (paymentMethod.Equals("bank", StringComparison.OrdinalIgnoreCase) || paymentMethod.Equals("card", StringComparison.OrdinalIgnoreCase))
            {
                return row.Bank;
            }

            if (paymentMethod.Equals("cash", StringComparison.OrdinalIgnoreCase))
            {
                return row.Cash;
            }

            // Default fallback to cash grouping for other methods
            return row.Cash;
        }

        private static decimal CalculateLineRevenue(TransactionHistoryModel transaction)
        {
            if (transaction == null)
            {
                return 0m;
            }

            if (transaction.Price > 0m)
            {
                return transaction.Price;
            }

            var breakdownTotal = transaction.SmallPrice + transaction.MediumPrice + transaction.LargePrice + transaction.AddonPrice;
            if (breakdownTotal > 0m)
            {
                return breakdownTotal;
            }

            return transaction.Total > 0m ? transaction.Total : 0m;
        }

        public async Task<List<SalesAggregateRow>> GetAggregatesAsync(DateTime startDate, DateTime endDate, SalesAggregateGrouping grouping, CancellationToken cancellationToken = default)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(20));

            var effectiveEnd = endDate <= startDate ? startDate.AddDays(1) : endDate;

            var transactions = await _database.GetTransactionsByDateRangeAsync(startDate, effectiveEnd);

            var aggregates = new Dictionary<string, (SalesAggregateRow Row, DateTime SortDate)>(StringComparer.OrdinalIgnoreCase);

            foreach (var transaction in transactions)
            {
                if (transaction == null)
                {
                    continue;
                }

                var quantity = transaction.Quantity > 0 ? transaction.Quantity : 1;
                if (quantity <= 0)
                {
                    quantity = 1;
                }

                if (string.IsNullOrWhiteSpace(transaction.DrinkName) && grouping == SalesAggregateGrouping.Product)
                {
                    continue;
                }

                var revenue = CalculateLineRevenue(transaction);
                var (key, label, sortDate) = ResolveGrouping(transaction, grouping);

                if (!aggregates.TryGetValue(key, out var aggregateEntry))
                {
                    var row = new SalesAggregateRow
                    {
                        GroupKey = key,
                        GroupLabel = label,
                        GroupingDescription = grouping.ToString()
                    };
                    aggregates[key] = (row, sortDate);
                    aggregateEntry = aggregates[key];
                }

                aggregateEntry.Row.AddOverall(quantity, revenue);

                var paymentBreakdown = ResolveBreakdown(aggregateEntry.Row, transaction.PaymentMethod ?? "Cash");
                paymentBreakdown.Add(quantity, revenue);

                aggregates[key] = (aggregateEntry.Row, aggregateEntry.SortDate == DateTime.MinValue ? sortDate : aggregateEntry.SortDate);
            }

            var rows = aggregates
                .Select(pair => new
                {
                    pair.Value.SortDate,
                    pair.Value.Row
                })
                .ToList();

            if (grouping == SalesAggregateGrouping.Product || grouping == SalesAggregateGrouping.Category)
            {
                rows = rows
                    .OrderByDescending(r => r.Row.TotalQuantity)
                    .ThenBy(r => r.Row.GroupLabel, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            else
            {
                rows = rows
                    .OrderBy(r => r.SortDate)
                    .ToList();
            }

            return rows.Select(r => r.Row).ToList();
        }

        private async Task<KeyValuePair<string, double>?> GetTrendingItemAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"GetTrendingItemAsync called with startDate: {startDate}, endDate: {endDate}");
                
                // Get recent sales (last 3 days) vs historical average (last 7 days before that)
                var recentStart = endDate.AddDays(-3);
                var historicalStart = recentStart.AddDays(-7);
                var historicalEnd = recentStart;

                System.Diagnostics.Debug.WriteLine($"Recent period: {recentStart} to {endDate}");
                System.Diagnostics.Debug.WriteLine($"Historical period: {historicalStart} to {historicalEnd}");

                var recentTransactions = await _database.GetTransactionsByDateRangeAsync(recentStart, endDate);
                var historicalTransactions = await _database.GetTransactionsByDateRangeAsync(historicalStart, historicalEnd);

                var recentProducts = AggregateProductCounts(recentTransactions)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);
                System.Diagnostics.Debug.WriteLine($"Recent products count: {recentProducts.Count}");

                var historicalProducts = AggregateProductCounts(historicalTransactions)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);
                System.Diagnostics.Debug.WriteLine($"Historical products count: {historicalProducts.Count}");

                // Calculate trending score (recent sales vs historical average)
                var trendingScores = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

                foreach (var recent in recentProducts)
                {
                    var historicalCount = historicalProducts.TryGetValue(recent.Key, out var count) ? count : 0;
                    var historicalAverage = historicalCount / 7.0; // Average per day over 7 days
                    var recentAverage = recent.Value / 3.0; // Average per day over 3 days

                    if (historicalAverage > 0)
                    {
                        // Calculate growth rate
                        var growthRate = (recentAverage - historicalAverage) / historicalAverage;
                        trendingScores[recent.Key] = growthRate;
                    }
                    else if (recentAverage > 0)
                    {
                        // New product or no historical data, but has recent sales
                        trendingScores[recent.Key] = 1.0; // 100% growth
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Trending scores calculated: {trendingScores.Count} items");
                foreach (var score in trendingScores.Take(5))
                {
                    System.Diagnostics.Debug.WriteLine($"  {score.Key}: {score.Value:P2} growth");
                }

                // Find the item with highest growth rate (most trending)
                var mostTrending = trendingScores
                    .Where(kvp => kvp.Value > 0) // Only positive growth
                    .OrderByDescending(kvp => kvp.Value)
                    .FirstOrDefault();

                if (!string.IsNullOrEmpty(mostTrending.Key))
                {
                    System.Diagnostics.Debug.WriteLine($"Most trending item: {mostTrending.Key} with {mostTrending.Value:P2} growth");
                    return new KeyValuePair<string, double>(mostTrending.Key, mostTrending.Value);
                }

                System.Diagnostics.Debug.WriteLine("No positive trending items found, using fallback logic");
                
                // Fallback: if no trending items, return second most bought from current period
                var fallback = recentProducts.OrderByDescending(kvp => kvp.Value).Skip(1).FirstOrDefault();
                if (!string.IsNullOrEmpty(fallback.Key))
                {
                    System.Diagnostics.Debug.WriteLine($"Fallback trending item: {fallback.Key}");
                    return new KeyValuePair<string, double>(fallback.Key, 0.0); // No growth data
                }
                
                // Final fallback: use second most bought from main period
                var mainTransactions = await _database.GetTransactionsByDateRangeAsync(startDate, endDate);
                var mainProducts = AggregateProductCounts(mainTransactions);
                var finalFallback = mainProducts.Skip(1).FirstOrDefault();
                System.Diagnostics.Debug.WriteLine($"Final fallback trending item: {finalFallback.Key}");
                return !string.IsNullOrEmpty(finalFallback.Key) ? new KeyValuePair<string, double>(finalFallback.Key, 0.0) : (KeyValuePair<string, double>?)null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error calculating trending item: {ex.Message}");
                // Fallback to second most bought item using aggregated data
                var mainTransactions = await _database.GetTransactionsByDateRangeAsync(startDate, endDate);
                var topProducts = AggregateProductCounts(mainTransactions);
                var fallback = topProducts.Skip(1).FirstOrDefault();
                return !string.IsNullOrEmpty(fallback.Key) ? new KeyValuePair<string, double>(fallback.Key, 0.0) : (KeyValuePair<string, double>?)null;
            }
        }
    }

    // Fallback implementation for when database is not available
    public class NullSalesReportService : ISalesReportService
    {
        public Task<SalesReportSummary> GetSummaryAsync(DateTime startDate, DateTime endDate)
        {
            var summary = new SalesReportSummary
            {
                ActiveDays = 0,
                MostBoughtToday = "No data available",
                TrendingToday = "No data available",
                TrendingPercentage = 0,
                TotalSalesToday = 0,
                TotalOrdersToday = 0,
                TotalOrdersThisWeek = 0,
                TopCoffeeToday = new List<TrendItem>(),
                TopMilkteaToday = new List<TrendItem>(),
                TopFrappeToday = new List<TrendItem>(),
                TopFruitSodaToday = new List<TrendItem>(),
                TopCoffeeWeekly = new List<TrendItem>(),
                TopMilkteaWeekly = new List<TrendItem>(),
                TopFrappeWeekly = new List<TrendItem>(),
                TopFruitSodaWeekly = new List<TrendItem>(),
                Reports = new List<SalesReportPageModel>()
            };
            return Task.FromResult(summary);
        }

        public Task<List<SalesAggregateRow>> GetAggregatesAsync(DateTime startDate, DateTime endDate, SalesAggregateGrouping grouping, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<SalesAggregateRow>());
        }

    }
}

