using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Coftea_Capstone.Models;
using Coftea_Capstone.Services;
using Coftea_Capstone.C_;

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

    public interface ISalesReportService
    {
        Task<SalesReportSummary> GetSummaryAsync(DateTime startDate, DateTime endDate);
    }

    // Real implementation using database
    public class DatabaseSalesReportService : ISalesReportService
    {
        private readonly Database _database;

        public DatabaseSalesReportService()
        {
            _database = new Database(); // Will use auto-detected host
        }

        public async Task<SalesReportSummary> GetSummaryAsync(DateTime startDate, DateTime endDate)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔍 SalesReportService: Getting summary from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
                
                // Get all transactions for the date range
                var transactions = await _database.GetTransactionsByDateRangeAsync(startDate, endDate);
                System.Diagnostics.Debug.WriteLine($"📊 Found {transactions.Count} transactions");
                
                // Calculate basic metrics
                var totalSales = transactions.Sum(t => t.Total);
                var totalOrders = transactions.Count;
                var activeDays = transactions.Select(t => t.TransactionDate.Date).Distinct().Count();

                // Get top products (increase limit to ensure we get comprehensive data)
                var topProducts = await _database.GetTopProductsByDateRangeAsync(startDate, endDate, 50);
                System.Diagnostics.Debug.WriteLine($"🏆 Found {topProducts.Count} top products");
                
                // Separate by category
                var coffeeProducts = new List<TrendItem>();
                var milkTeaProducts = new List<TrendItem>();
                var frappeProducts = new List<TrendItem>();
                var fruitSodaProducts = new List<TrendItem>();

                // Get product details to include color codes
                var allProducts = await _database.GetProductsAsyncCached();
                var productLookup = allProducts.ToDictionary(p => p.ProductName, p => p.ColorCode ?? "");
                
                foreach (var product in topProducts)
                {
                    var trendItem = new TrendItem 
                    { 
                        Name = product.Key, 
                        Count = product.Value,
                        ColorCode = productLookup.GetValueOrDefault(product.Key, "")
                    };
                    
                    // Categorize based on product name patterns
                    if (product.Key.Contains("Frappe", StringComparison.OrdinalIgnoreCase))
                    {
                        frappeProducts.Add(trendItem);
                    }
                    else if (product.Key.Contains("Soda", StringComparison.OrdinalIgnoreCase) || 
                             product.Key.Contains("Orange", StringComparison.OrdinalIgnoreCase) ||
                             product.Key.Contains("Lemon", StringComparison.OrdinalIgnoreCase) ||
                             product.Key.Contains("Grape", StringComparison.OrdinalIgnoreCase))
                    {
                        fruitSodaProducts.Add(trendItem);
                    }
                    else if (product.Key.Contains("Matcha", StringComparison.OrdinalIgnoreCase) ||
                             product.Key.Contains("Brown Sugar", StringComparison.OrdinalIgnoreCase) ||
                             product.Key.Contains("Hokkaido", StringComparison.OrdinalIgnoreCase) ||
                             product.Key.Contains("Taro", StringComparison.OrdinalIgnoreCase) ||
                             product.Key.Contains("Wintermelon", StringComparison.OrdinalIgnoreCase))
                    {
                        milkTeaProducts.Add(trendItem);
                    }
                    else
                    {
                        coffeeProducts.Add(trendItem);
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
                var mostBought = topProducts.OrderByDescending(p => p.Value).FirstOrDefault();
                
                // Calculate trending item based on recent demand patterns
                var trending = await GetTrendingItemAsync(startDate, endDate);

                System.Diagnostics.Debug.WriteLine($"✅ SalesReportService: Successfully processed data - Most bought: {mostBought.Key}, Total sales: {totalSales:C}");

                return new SalesReportSummary
                {
                    ActiveDays = activeDays,
                    MostBoughtToday = mostBought.Key ?? "No data",
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
                    TopCoffeeWeekly = new List<TrendItem>(),
                    TopMilkteaWeekly = new List<TrendItem>(),
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
                    TopCoffeeWeekly = new List<TrendItem>(),
                    TopMilkteaWeekly = new List<TrendItem>(),
                    Reports = new List<SalesReportPageModel>()
                };
            }
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

                // Get recent sales
                var recentProducts = await _database.GetTopProductsByDateRangeAsync(recentStart, endDate, 20);
                System.Diagnostics.Debug.WriteLine($"Recent products count: {recentProducts.Count}");
                
                // Get historical sales for comparison
                var historicalProducts = await _database.GetTopProductsByDateRangeAsync(historicalStart, historicalEnd, 20);
                System.Diagnostics.Debug.WriteLine($"Historical products count: {historicalProducts.Count}");

                // Calculate trending score (recent sales vs historical average)
                var trendingScores = new Dictionary<string, double>();
                
                foreach (var recent in recentProducts)
                {
                    var historicalCount = historicalProducts.ContainsKey(recent.Key) ? historicalProducts[recent.Key] : 0;
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
                var fallback = recentProducts.Skip(1).FirstOrDefault();
                if (fallback.Key != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Fallback trending item: {fallback.Key}");
                    return new KeyValuePair<string, double>(fallback.Key, 0.0); // No growth data
                }
                
                // Final fallback: use second most bought from main period
                var mainProducts = await _database.GetTopProductsByDateRangeAsync(startDate, endDate, 10);
                var finalFallback = mainProducts.Skip(1).FirstOrDefault();
                System.Diagnostics.Debug.WriteLine($"Final fallback trending item: {finalFallback.Key}");
                return finalFallback.Key != null ? new KeyValuePair<string, double>(finalFallback.Key, 0.0) : (KeyValuePair<string, double>?)null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error calculating trending item: {ex.Message}");
                // Fallback to second most bought item
                var topProducts = await _database.GetTopProductsByDateRangeAsync(startDate, endDate, 10);
                var fallback = topProducts.Skip(1).FirstOrDefault();
                return fallback.Key != null ? new KeyValuePair<string, double>(fallback.Key, 0.0) : (KeyValuePair<string, double>?)null;
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
                TotalSalesToday = 0,
                TotalOrdersToday = 0,
                TotalOrdersThisWeek = 0,
                TopCoffeeToday = new List<TrendItem>(),
                TopMilkteaToday = new List<TrendItem>(),
                TopCoffeeWeekly = new List<TrendItem>(),
                TopMilkteaWeekly = new List<TrendItem>(),
                Reports = new List<SalesReportPageModel>()
            };
            return Task.FromResult(summary);
        }

    }
}

