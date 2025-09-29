using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Coftea_Capstone.Models;
using Coftea_Capstone.C_;

namespace Coftea_Capstone.Services
{
    public class SalesReportSummary
    {
        public int ActiveDays { get; set; }
        public string MostBoughtToday { get; set; }
        public string TrendingToday { get; set; }
        public decimal TotalSalesToday { get; set; }
        public int TotalOrdersToday { get; set; }
        public int TotalOrdersThisWeek { get; set; }
        public IEnumerable<TrendItem> TopCoffeeToday { get; set; }
        public IEnumerable<TrendItem> TopMilkteaToday { get; set; }
        public IEnumerable<TrendItem> TopCoffeeWeekly { get; set; }
        public IEnumerable<TrendItem> TopMilkteaWeekly { get; set; }
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
            _database = new Database(
                host: "192.168.1.4",
                database: "coftea_db",
                user: "maui",
                password: "password123"
            );
        }

        public async Task<SalesReportSummary> GetSummaryAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                // Get all transactions for the date range
                var transactions = await _database.GetTransactionsByDateRangeAsync(startDate, endDate);
                
                // Calculate basic metrics
                var totalSales = transactions.Sum(t => t.Total);
                var totalOrders = transactions.Count;
                var activeDays = transactions.Select(t => t.TransactionDate.Date).Distinct().Count();

                // Get top products
                var topProducts = await _database.GetTopProductsByDateRangeAsync(startDate, endDate, 20);
                
                // Separate by category
                var coffeeProducts = new List<TrendItem>();
                var milkTeaProducts = new List<TrendItem>();
                var frappeProducts = new List<TrendItem>();
                var fruitSodaProducts = new List<TrendItem>();

                foreach (var product in topProducts)
                {
                    var trendItem = new TrendItem { Name = product.Key, Count = product.Value };
                    
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

                // Get most bought and trending items
                var mostBought = topProducts.OrderByDescending(p => p.Value).FirstOrDefault();
                var trending = topProducts.OrderByDescending(p => p.Value).Skip(1).FirstOrDefault();

                return new SalesReportSummary
                {
                    ActiveDays = activeDays,
                    MostBoughtToday = mostBought.Key ?? "No data",
                    TrendingToday = trending.Key ?? "No data",
                    TotalSalesToday = totalSales,
                    TotalOrdersToday = totalOrders,
                    TotalOrdersThisWeek = totalOrders, // Same as today for now
                    TopCoffeeToday = coffeeProducts.Take(5),
                    TopMilkteaToday = milkTeaProducts.Take(5),
                    TopCoffeeWeekly = coffeeProducts.Take(5),
                    TopMilkteaWeekly = milkTeaProducts.Take(5),
                    Reports = new List<SalesReportPageModel>()
                };
            }
            catch (Exception ex)
            {
                // Return empty data on error
                return new SalesReportSummary
                {
                    ActiveDays = 0,
                    MostBoughtToday = "Error loading data",
                    TrendingToday = "Error loading data",
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

