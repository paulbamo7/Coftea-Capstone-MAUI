using Coftea_Capstone.Models;
using MySqlConnector;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Linq;
using Coftea_Capstone.Services;

using Coftea_Capstone.C_;

namespace Coftea_Capstone.Models
{
    public class Database
    {
        private readonly string _db;
        // In-memory caches to reduce repeated DB calls during UI updates
        private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);
        private static (DateTime ts, List<POSPageModel> items)? _productsCache;
        private static (DateTime ts, List<InventoryPageModel> items)? _inventoryCache;
        private static readonly Dictionary<int, (DateTime ts, List<(InventoryPageModel item, double amount, string unit, string role)> items)> _productIngredientsCache = new();
        private static readonly Dictionary<int, (DateTime ts, List<InventoryPageModel> items)> _productAddonsCache = new();

        public Database(string host = null,
                        string database = "coftea_db",
                        string user = "root",
                        string password = "")
        {
            // Use provided host or auto-detect
            var server = host ?? GetDefaultHostForPlatform();
            
            _db = new MySqlConnectionStringBuilder
            {
                Server = server,
                Port = 3306,
                Database = "coftea_db",
                UserID = "root",
                Password = "",
                SslMode = MySqlSslMode.None
            }.ConnectionString;

        }

        private static string GetDefaultHostForPlatform()
        {
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                return "192.168.1.6";  
            }

            if (DeviceInfo.Platform == DevicePlatform.iOS)
            {
                return "192.168.1.6";
            }
            
            // For Windows, Mac, and other platforms
            return "localhost";
        }

        // Ensure server is reachable and the database exists; create DB if missing
        public async Task EnsureServerAndDatabaseAsync()
        {
            // Get the database host from NetworkConfigurationService
            var server = await NetworkConfigurationService.GetDatabaseHostAsync();
            var builder = new MySqlConnectionStringBuilder
            {
                Server = server,
                Port = 3306,
                UserID = "root",
                Password = "",
                SslMode = MySqlSslMode.None
            };

            await using var conn = new MySqlConnection(builder.ConnectionString);
            await conn.OpenAsync();
            var createDbSql = "CREATE DATABASE IF NOT EXISTS coftea_db;";
            await using var cmd = new MySqlCommand(createDbSql, conn);
            await cmd.ExecuteNonQueryAsync();
        }
        private async Task<MySqlConnection> GetOpenConnectionAsync()
        {
            var conn = new MySqlConnection(_db);
            await conn.OpenAsync();
            return conn;
        }

		// Cache helpers
		private static bool IsFresh(DateTime ts) => (DateTime.UtcNow - ts) < CacheDuration;
		private static void InvalidateProductsCache() => _productsCache = null;
		private static void InvalidateInventoryCache() => _inventoryCache = null;
		private static void InvalidateProductLinksCache(int productId)
		{
			_productIngredientsCache.Remove(productId);
			_productAddonsCache.Remove(productId);
		}

		// Shared DB helpers to reduce redundancy
		private static object DbValue(object? value)
		{
			return value ?? DBNull.Value;
		}

		private static void AddParameters(MySqlCommand cmd, IDictionary<string, object?>? parameters)
		{
			if (parameters == null) return;
			foreach (var kvp in parameters)
			{
				cmd.Parameters.AddWithValue(kvp.Key, DbValue(kvp.Value));
			}
		}

		private async Task<int> ExecuteNonQueryAsync(string sql, IDictionary<string, object?>? parameters = null)
		{
			await using var conn = await GetOpenConnectionAsync();
			await using var cmd = new MySqlCommand(sql, conn);
			AddParameters(cmd, parameters);
			return await cmd.ExecuteNonQueryAsync();
		}

		private async Task<T?> ExecuteScalarAsync<T>(string sql, IDictionary<string, object?>? parameters = null)
		{
			await using var conn = await GetOpenConnectionAsync();
			await using var cmd = new MySqlCommand(sql, conn);
			AddParameters(cmd, parameters);
			var result = await cmd.ExecuteScalarAsync();
			if (result == null || result is DBNull) return default;
			return (T)Convert.ChangeType(result, typeof(T));
		}

		private async Task<List<T>> QueryAsync<T>(string sql, Func<MySqlDataReader, T> map, IDictionary<string, object?>? parameters = null)
		{
			await using var conn = await GetOpenConnectionAsync();
			await using var cmd = new MySqlCommand(sql, conn);
			AddParameters(cmd, parameters);
			var list = new List<T>();
			await using var reader = await cmd.ExecuteReaderAsync();
			while (await reader.ReadAsync())
			{
				list.Add(map(reader));
			}
			return list;
		}
        public async Task<UserInfoModel> GetUserByEmailAsync(string email)
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = "SELECT * FROM users WHERE email = @Email LIMIT 1;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Email", email);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new UserInfoModel
                {
                    ID = reader.GetInt32("id"),
                    Email = reader.GetString("email"),
                    Password = reader.GetString("password"),
                    FirstName = reader.IsDBNull(reader.GetOrdinal("firstName")) ? string.Empty : reader.GetString("firstName"),
                    LastName = reader.IsDBNull(reader.GetOrdinal("lastName")) ? string.Empty : reader.GetString("lastName"),
                    Birthday = reader.IsDBNull(reader.GetOrdinal("birthday")) ? DateTime.MinValue : reader.GetDateTime("birthday"),
                    PhoneNumber = reader.IsDBNull(reader.GetOrdinal("phoneNumber")) ? string.Empty : reader.GetString("phoneNumber"),
                    Address = reader.IsDBNull(reader.GetOrdinal("address")) ? string.Empty : reader.GetString("address"),
                    IsAdmin = reader.IsDBNull(reader.GetOrdinal("isAdmin")) ? false : reader.GetBoolean("isAdmin"),
                    Status = reader.IsDBNull(reader.GetOrdinal("status")) ? "approved" : reader.GetString("status"),
                    CanAccessInventory = reader.IsDBNull(reader.GetOrdinal("can_access_inventory")) ? false : reader.GetBoolean("can_access_inventory"),
                    CanAccessSalesReport = reader.IsDBNull(reader.GetOrdinal("can_access_sales_report")) ? false : reader.GetBoolean("can_access_sales_report")
                };
            }
            return null;
        } 

		public async Task<int> UpdateUserPasswordAsync(int userId, string newHashedPassword)
		{
			var sql = "UPDATE users SET password = @Password WHERE id = @Id;";
			return await ExecuteNonQueryAsync(sql, new Dictionary<string, object?>
			{
				{"@Password", newHashedPassword},
				{"@Id", userId}
			});
		}
        public async Task<List<UserInfoModel>> GetAllUsersAsync()
        {
			var sql = "SELECT id, email, password, firstName, lastName, birthday, phoneNumber, address, isAdmin, status, IFNULL(can_access_inventory, 0) AS can_access_inventory, IFNULL(can_access_sales_report, 0) AS can_access_sales_report FROM users ORDER BY id ASC;";
			return await QueryAsync(sql, reader => new UserInfoModel
			{
				ID = reader.GetInt32("id"),
				Email = reader.IsDBNull(reader.GetOrdinal("email")) ? string.Empty : reader.GetString("email"),
				Password = reader.IsDBNull(reader.GetOrdinal("password")) ? string.Empty : reader.GetString("password"),
				FirstName = reader.IsDBNull(reader.GetOrdinal("firstName")) ? string.Empty : reader.GetString("firstName"),
				LastName = reader.IsDBNull(reader.GetOrdinal("lastName")) ? string.Empty : reader.GetString("lastName"),
				Birthday = reader.IsDBNull(reader.GetOrdinal("birthday")) ? DateTime.MinValue : reader.GetDateTime("birthday"),
				PhoneNumber = reader.IsDBNull(reader.GetOrdinal("phoneNumber")) ? string.Empty : reader.GetString("phoneNumber"),
				Address = reader.IsDBNull(reader.GetOrdinal("address")) ? string.Empty : reader.GetString("address"),
				IsAdmin = reader.IsDBNull(reader.GetOrdinal("isAdmin")) ? false : reader.GetBoolean("isAdmin"),
				Status = reader.IsDBNull(reader.GetOrdinal("status")) ? "approved" : reader.GetString("status"),
				CanAccessInventory = !reader.IsDBNull(reader.GetOrdinal("can_access_inventory")) && reader.GetBoolean("can_access_inventory"),
				CanAccessSalesReport = !reader.IsDBNull(reader.GetOrdinal("can_access_sales_report")) && reader.GetBoolean("can_access_sales_report")
			});
        }
        public async Task<bool> IsFirstUserAsync()
        {
            await using var conn = await GetOpenConnectionAsync();
            
            var sql = "SELECT COUNT(*) FROM users;";
            await using var cmd = new MySqlCommand(sql, conn);
            var userCount = await cmd.ExecuteScalarAsync();
            
            return Convert.ToInt32(userCount) == 0;
        }

        public async Task<int> AddUserAsync(UserInfoModel user)
        {
            await using var conn = await GetOpenConnectionAsync();

            // Check if this is the first user
            bool isFirstUser = await IsFirstUserAsync();
            
            var sql = "INSERT INTO users (firstName, lastName, email, password, phoneNumber, birthday, address, status, isAdmin) " +
                      "VALUES (@FirstName,@LastName, @Email, @Password, @PhoneNumber, @Birthday, @Address, 'approved', @IsAdmin);";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@FirstName", user.FirstName);
            cmd.Parameters.AddWithValue("@LastName", user.LastName);
            cmd.Parameters.AddWithValue("@Email", user.Email);
            cmd.Parameters.AddWithValue("@Password", user.Password);
            cmd.Parameters.AddWithValue("@Birthday", user.Birthday.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@PhoneNumber", user.PhoneNumber);
            cmd.Parameters.AddWithValue("@Address", user.Address);
            cmd.Parameters.AddWithValue("@IsAdmin", isFirstUser);

            return await cmd.ExecuteNonQueryAsync();
        }

        // POS Database
		public async Task<List<POSPageModel>> GetProductsAsync()
		{
			var sql = "SELECT * FROM products;";
			return await QueryAsync(sql, reader => new POSPageModel
			{
				ProductID = reader.GetInt32("productID"),
				ProductName = reader.GetString("productName"),
				SmallPrice = reader.GetDecimal("smallPrice"),
				MediumPrice = reader.IsDBNull(reader.GetOrdinal("mediumPrice")) ? 0 : reader.GetDecimal("mediumPrice"),
				LargePrice = reader.GetDecimal("largePrice"),
				ImageSet = reader.IsDBNull(reader.GetOrdinal("imageSet")) ? "" : reader.GetString("imageSet"),
				Category = reader.IsDBNull(reader.GetOrdinal("category")) ? null : reader.GetString("category"),
				Subcategory = reader.IsDBNull(reader.GetOrdinal("subcategory")) ? null : reader.GetString("subcategory"),
				ProductDescription = reader.IsDBNull(reader.GetOrdinal("description")) ? "" : reader.GetString("description")
			});
		}

		public async Task<List<POSPageModel>> GetProductsAsyncCached()
		{
			if (_productsCache.HasValue && IsFresh(_productsCache.Value.ts))
				return _productsCache.Value.items;

			var items = await GetProductsAsync();
			_productsCache = (DateTime.UtcNow, items);
			return items;
		}

        // Get all ingredients and addons (linked inventory items) for a product
        public async Task<List<(InventoryPageModel item, double amount, string unit, string role)>> GetProductIngredientsAsync(int productId)
        {
            await using var conn = await GetOpenConnectionAsync();

            const string sql = @"SELECT pi.amount, pi.unit, pi.role,
                                        i.itemID, i.itemName, i.itemQuantity, i.itemCategory, i.imageSet,
                                        i.itemDescription, i.unitOfMeasurement, i.minimumQuantity
                                   FROM product_ingredients pi
                                   JOIN inventory i ON i.itemID = pi.itemID
                                  WHERE pi.productID = @ProductID";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ProductID", productId);

            var results = new List<(InventoryPageModel item, double amount, string unit, string role)>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var item = new InventoryPageModel
                {
                    itemID = reader.GetInt32("itemID"),
                    itemName = reader.GetString("itemName"),
                    itemQuantity = reader.GetDouble("itemQuantity"),
                    itemCategory = reader.IsDBNull(reader.GetOrdinal("itemCategory")) ? string.Empty : reader.GetString("itemCategory"),
                    ImageSet = reader.IsDBNull(reader.GetOrdinal("imageSet")) ? string.Empty : reader.GetString("imageSet"),
                    itemDescription = reader.IsDBNull(reader.GetOrdinal("itemDescription")) ? string.Empty : reader.GetString("itemDescription"),
                    unitOfMeasurement = reader.IsDBNull(reader.GetOrdinal("unitOfMeasurement")) ? string.Empty : reader.GetString("unitOfMeasurement"),
                    minimumQuantity = reader.IsDBNull(reader.GetOrdinal("minimumQuantity")) ? 0 : reader.GetDouble("minimumQuantity")
                };

                // Map linked amount/unit into size-specific fields (default to same across sizes)
                var linkAmount = reader.IsDBNull(reader.GetOrdinal("amount")) ? 0d : reader.GetDouble("amount");
                var linkUnit = reader.IsDBNull(reader.GetOrdinal("unit")) ? item.unitOfMeasurement : reader.GetString("unit");

                item.InputAmountSmall = linkAmount;
                item.InputAmountMedium = linkAmount;
                item.InputAmountLarge = linkAmount;
                item.InputUnitSmall = string.IsNullOrWhiteSpace(linkUnit) ? item.DefaultUnit : linkUnit;
                item.InputUnitMedium = item.InputUnitSmall;
                item.InputUnitLarge = item.InputUnitSmall;

                // If you later add cost computation, populate PriceUsed* here
                item.PriceUsedSmall = 0;
                item.PriceUsedMedium = 0;
                item.PriceUsedLarge = 0;

                var amount = reader.IsDBNull(reader.GetOrdinal("amount")) ? 0d : reader.GetDouble("amount");
                var unit = reader.IsDBNull(reader.GetOrdinal("unit")) ? item.unitOfMeasurement : reader.GetString("unit");
                var role = reader.IsDBNull(reader.GetOrdinal("role")) ? "ingredient" : reader.GetString("role");

                results.Add((item, amount, unit, role));
            }

            return results;
        }

		public async Task<List<(InventoryPageModel item, double amount, string unit, string role)>> GetProductIngredientsAsyncCached(int productId)
		{
			if (_productIngredientsCache.TryGetValue(productId, out var cache) && IsFresh(cache.ts))
				return cache.items;

			var items = await GetProductIngredientsAsync(productId);
			_productIngredientsCache[productId] = (DateTime.UtcNow, items);
			return items;
		}

        // Get add-ons (linked inventory items) for a product from product_addons
        public async Task<List<InventoryPageModel>> GetProductAddonsAsync(int productId)
        {
            await using var conn = await GetOpenConnectionAsync();

            const string sql = @"SELECT pa.amount, pa.unit, pa.role, pa.addon_price,
                                        i.itemID, i.itemName, i.itemQuantity, i.itemCategory, i.imageSet,
                                        i.itemDescription, i.unitOfMeasurement, i.minimumQuantity
                                   FROM product_addons pa
                                   JOIN inventory i ON i.itemID = pa.itemID
                                  WHERE pa.productID = @ProductID";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ProductID", productId);

            var results = new List<InventoryPageModel>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var item = new InventoryPageModel
                {
                    itemID = reader.GetInt32("itemID"),
                    itemName = reader.GetString("itemName"),
                    itemQuantity = reader.GetDouble("itemQuantity"),
                    itemCategory = reader.IsDBNull(reader.GetOrdinal("itemCategory")) ? string.Empty : reader.GetString("itemCategory"),
                    ImageSet = reader.IsDBNull(reader.GetOrdinal("imageSet")) ? string.Empty : reader.GetString("imageSet"),
                    itemDescription = reader.IsDBNull(reader.GetOrdinal("itemDescription")) ? string.Empty : reader.GetString("itemDescription"),
                    unitOfMeasurement = reader.IsDBNull(reader.GetOrdinal("unitOfMeasurement")) ? string.Empty : reader.GetString("unitOfMeasurement"),
                    minimumQuantity = reader.IsDBNull(reader.GetOrdinal("minimumQuantity")) ? 0 : reader.GetDouble("minimumQuantity"),
                    IsSelected = false
                };

                // Map linked amount/unit
                var linkAmount = reader.IsDBNull(reader.GetOrdinal("amount")) ? 0d : reader.GetDouble("amount");
                var linkUnit = reader.IsDBNull(reader.GetOrdinal("unit")) ? item.unitOfMeasurement : reader.GetString("unit");
                var addonPrice = reader.IsDBNull(reader.GetOrdinal("addon_price")) ? 0m : reader.GetDecimal("addon_price");

                item.InputAmountSmall = linkAmount;
                item.InputAmountMedium = linkAmount;
                item.InputAmountLarge = linkAmount;
                item.InputUnitSmall = string.IsNullOrWhiteSpace(linkUnit) ? item.DefaultUnit : linkUnit;
                item.InputUnitMedium = item.InputUnitSmall;
                item.InputUnitLarge = item.InputUnitSmall;
                item.AddonPrice = addonPrice;

                // If you later add cost computation, populate PriceUsed* here
                item.PriceUsedSmall = 0;
                item.PriceUsedMedium = 0;
                item.PriceUsedLarge = 0;

                results.Add(item);
            }

            return results;
        }

		public async Task<List<InventoryPageModel>> GetProductAddonsAsyncCached(int productId)
		{
			if (_productAddonsCache.TryGetValue(productId, out var cache) && IsFresh(cache.ts))
				return cache.items;

			var items = await GetProductAddonsAsync(productId);
			_productAddonsCache[productId] = (DateTime.UtcNow, items);
			return items;
		}

        public async Task<int> SaveProductAsync(POSPageModel product)
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = "INSERT INTO products (productName, smallPrice, mediumPrice, largePrice, category, subcategory, imageSet, description) " +
                      "VALUES (@ProductName, @SmallPrice, @MediumPrice, @LargePrice, @Category, @Subcategory, @Image, @Description);";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ProductName", product.ProductName);
            cmd.Parameters.AddWithValue("@SmallPrice", product.SmallPrice);
            cmd.Parameters.AddWithValue("@MediumPrice", product.MediumPrice);
            cmd.Parameters.AddWithValue("@LargePrice", product.LargePrice);
            cmd.Parameters.AddWithValue("@Category", product.Category);
            cmd.Parameters.AddWithValue("@Subcategory", (object?)product.Subcategory ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Image", product.ImageSet);
            cmd.Parameters.AddWithValue("@Description", (object?)product.ProductDescription ?? DBNull.Value);

            var rows = await cmd.ExecuteNonQueryAsync();
            InvalidateProductsCache();
            return rows;
        }

        // Save product and return its new ID
        public async Task<int> SaveProductReturningIdAsync(POSPageModel product)
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = "INSERT INTO products (productName, smallPrice, mediumPrice, largePrice, category, subcategory, imageSet, description) " +
                      "VALUES (@ProductName, @SmallPrice, @MediumPrice, @LargePrice, @Category, @Subcategory, @Image, @Description);";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ProductName", product.ProductName);
            cmd.Parameters.AddWithValue("@SmallPrice", product.SmallPrice);
            cmd.Parameters.AddWithValue("@MediumPrice", product.MediumPrice);
            cmd.Parameters.AddWithValue("@LargePrice", product.LargePrice);
            cmd.Parameters.AddWithValue("@Category", product.Category);
            cmd.Parameters.AddWithValue("@Subcategory", (object?)product.Subcategory ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Image", product.ImageSet);
            cmd.Parameters.AddWithValue("@Description", (object?)product.ProductDescription ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
            InvalidateProductsCache();
            return (int)cmd.LastInsertedId;
        }

        // Link product to inventory items, splitting ingredients and addons into separate tables
        public async Task<int> SaveProductLinksSplitAsync(
            int productId,
            IEnumerable<(int inventoryItemId, double amount, string? unit)> ingredients,
            IEnumerable<(int inventoryItemId, double amount, string? unit, decimal addonPrice)> addons)
        {
            await using var conn = await GetOpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            const string sqlIngredients = "INSERT INTO product_ingredients (productID, itemID, amount, unit, role) VALUES (@ProductID, @ItemID, @Amount, @Unit, 'ingredient');";
            const string sqlAddons = "INSERT INTO product_addons (productID, itemID, amount, unit, role, addon_price) VALUES (@ProductID, @ItemID, @Amount, @Unit, 'addon', @AddonPrice) ON DUPLICATE KEY UPDATE amount = VALUES(amount), unit = VALUES(unit), addon_price = VALUES(addon_price);";
            int total = 0;
            try
            {
                System.Diagnostics.Debug.WriteLine($"Saving product links for productId={productId}");
                var ingredientList = ingredients?.ToList() ?? new List<(int inventoryItemId, double amount, string? unit)>();
                var addonList = addons?.ToList() ?? new List<(int inventoryItemId, double amount, string? unit, decimal addonPrice)>();
                System.Diagnostics.Debug.WriteLine($"Ingredients to save: {ingredientList.Count}, Addons to save: {addonList.Count}");

                foreach (var link in ingredientList)
                {
                    await using var cmd = new MySqlCommand(sqlIngredients, conn, (MySqlTransaction)tx);
                    cmd.Parameters.AddWithValue("@ProductID", productId);
                    cmd.Parameters.AddWithValue("@ItemID", link.inventoryItemId);
                    cmd.Parameters.AddWithValue("@Amount", link.amount);
                    cmd.Parameters.AddWithValue("@Unit", (object?)link.unit ?? DBNull.Value);
                    var affected = await cmd.ExecuteNonQueryAsync();
                    total += affected;
                    System.Diagnostics.Debug.WriteLine($"Inserted ingredient link itemId={link.inventoryItemId}, amount={link.amount}, unit={link.unit} → rows={affected}");
                }

                foreach (var link in addonList)
                {
                    await using var cmd = new MySqlCommand(sqlAddons, conn, (MySqlTransaction)tx);
                    cmd.Parameters.AddWithValue("@ProductID", productId);
                    cmd.Parameters.AddWithValue("@ItemID", link.inventoryItemId);
                    cmd.Parameters.AddWithValue("@Amount", link.amount);
                    cmd.Parameters.AddWithValue("@Unit", (object?)link.unit ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@AddonPrice", link.addonPrice);
                    var affected = await cmd.ExecuteNonQueryAsync();
                    total += affected;
                    System.Diagnostics.Debug.WriteLine($"Upserted addon link itemId={link.inventoryItemId}, amount={link.amount}, unit={link.unit}, price={link.addonPrice} → rows={affected}");
                }

                await tx.CommitAsync();
                InvalidateProductLinksCache(productId);
                System.Diagnostics.Debug.WriteLine($"Finished saving links. Total affected rows={total}");
                return total;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
        public async Task<POSPageModel?> GetProductByNameAsync(string name)
        {
            await using var conn = await GetOpenConnectionAsync();
            var sql = "SELECT * FROM products WHERE productName = @Name LIMIT 1;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Name", name);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new POSPageModel
                {
                    ProductID = reader.GetInt32("productID"),
                    ProductName = reader.GetString("productName"),
                    SmallPrice = reader.GetDecimal("smallPrice"),
                    LargePrice = reader.GetDecimal("largePrice"),
                    ImageSet = reader.IsDBNull(reader.GetOrdinal("imageSet")) ? "" : reader.GetString("imageSet"),
                    Category = reader.IsDBNull(reader.GetOrdinal("category")) ? null : reader.GetString("category"),
                    Subcategory = reader.IsDBNull(reader.GetOrdinal("subcategory")) ? null : reader.GetString("subcategory"),
                    ProductDescription = reader.IsDBNull(reader.GetOrdinal("description")) ? "" : reader.GetString("description")
                };
            }
            return null;
        }

		public async Task<POSPageModel?> GetProductByNameAsyncCached(string name)
		{
			var list = await GetProductsAsyncCached();
			return list.FirstOrDefault(p => string.Equals(p.ProductName?.Trim(), name?.Trim(), StringComparison.OrdinalIgnoreCase));
		}

        public async Task<POSPageModel?> GetProductByIdAsync(int productId)
        {
            await using var conn = await GetOpenConnectionAsync();
            var sql = "SELECT * FROM products WHERE productID = @Id LIMIT 1;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", productId);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new POSPageModel
                {
                    ProductID = reader.GetInt32("productID"),
                    ProductName = reader.GetString("productName"),
                    SmallPrice = reader.GetDecimal("smallPrice"),
                    LargePrice = reader.GetDecimal("largePrice"),
                    ImageSet = reader.IsDBNull(reader.GetOrdinal("imageSet")) ? "" : reader.GetString("imageSet"),
                    Category = reader.IsDBNull(reader.GetOrdinal("category")) ? null : reader.GetString("category"),
                    Subcategory = reader.IsDBNull(reader.GetOrdinal("subcategory")) ? null : reader.GetString("subcategory"),
                    ProductDescription = reader.IsDBNull(reader.GetOrdinal("description")) ? "" : reader.GetString("description")
                };
            }
            return null;
        }

        public async Task<int> UpdateProductAsync(POSPageModel product)
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = "UPDATE products SET productName = @ProductName, smallPrice = @SmallPrice, mediumPrice = @MediumPrice, largePrice = @LargePrice, " +
                      "category = @Category, subcategory = @Subcategory, imageSet = @Image, description = @Description WHERE productID = @ProductID;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ProductID", product.ProductID);
            cmd.Parameters.AddWithValue("@ProductName", product.ProductName);
            cmd.Parameters.AddWithValue("@SmallPrice", product.SmallPrice);
            cmd.Parameters.AddWithValue("@MediumPrice", product.MediumPrice);
            cmd.Parameters.AddWithValue("@LargePrice", product.LargePrice);
            cmd.Parameters.AddWithValue("@Category", product.Category);
            cmd.Parameters.AddWithValue("@Subcategory", (object?)product.Subcategory ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Image", product.ImageSet);
            cmd.Parameters.AddWithValue("@Description", (object?)product.ProductDescription ?? DBNull.Value);

            var rows = await cmd.ExecuteNonQueryAsync();
            InvalidateProductsCache();
            InvalidateProductLinksCache(product.ProductID);
            return rows;
        }

        public async Task<int> DeleteProductAsync(int productId)
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = "DELETE FROM products WHERE productID = @ProductID;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ProductID", productId);

            var rows = await cmd.ExecuteNonQueryAsync();
            InvalidateProductsCache();
            InvalidateProductLinksCache(productId);
            return rows;
        }

        // Inventory Database Methods
		public async Task<List<InventoryPageModel>> GetInventoryItemsAsync()
		{
			var sql = "SELECT * FROM inventory;";
			return await QueryAsync(sql, reader => new InventoryPageModel
			{
				itemID = reader.GetInt32("itemID"),
				itemName = reader.GetString("itemName"),
				itemQuantity = reader.GetDouble("itemQuantity"),
				itemCategory = reader.IsDBNull(reader.GetOrdinal("itemCategory")) ? "" : reader.GetString("itemCategory"),
				ImageSet = reader.IsDBNull(reader.GetOrdinal("imageSet")) ? "" : reader.GetString("imageSet"),
				itemDescription = reader.IsDBNull(reader.GetOrdinal("itemDescription")) ? "" : reader.GetString("itemDescription"),
				unitOfMeasurement = reader.IsDBNull(reader.GetOrdinal("unitOfMeasurement")) ? "" : reader.GetString("unitOfMeasurement"),
				minimumQuantity = reader.IsDBNull(reader.GetOrdinal("minimumQuantity")) ? 0 : reader.GetDouble("minimumQuantity")
			});
		}

		public async Task<List<InventoryPageModel>> GetInventoryItemsAsyncCached()
		{
			if (_inventoryCache.HasValue && IsFresh(_inventoryCache.Value.ts))
				return _inventoryCache.Value.items;

			var items = await GetInventoryItemsAsync();
			_inventoryCache = (DateTime.UtcNow, items);
			return items;
		}

        public async Task<InventoryPageModel?> GetInventoryItemByIdAsync(int itemId)
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = "SELECT * FROM inventory WHERE itemID = @ItemID LIMIT 1;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ItemID", itemId);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new InventoryPageModel
                {
                    itemID = reader.GetInt32("itemID"),
                    itemName = reader.GetString("itemName"),
                    itemQuantity = reader.GetDouble("itemQuantity"),
                    itemCategory = reader.IsDBNull(reader.GetOrdinal("itemCategory")) ? "" : reader.GetString("itemCategory"),
                    ImageSet = reader.IsDBNull(reader.GetOrdinal("imageSet")) ? "" : reader.GetString("imageSet"),
                    itemDescription = reader.IsDBNull(reader.GetOrdinal("itemDescription")) ? "" : reader.GetString("itemDescription"),
                    unitOfMeasurement = reader.IsDBNull(reader.GetOrdinal("unitOfMeasurement")) ? "" : reader.GetString("unitOfMeasurement"),
                    minimumQuantity = reader.IsDBNull(reader.GetOrdinal("minimumQuantity")) ? 0 : reader.GetDouble("minimumQuantity")
                };
            }
            return null;
        }

        public async Task<InventoryPageModel?> GetInventoryItemByNameAsync(string itemName)
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = "SELECT * FROM inventory WHERE itemName = @ItemName LIMIT 1;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ItemName", itemName);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new InventoryPageModel
                {
                    itemID = reader.GetInt32("itemID"),
                    itemName = reader.GetString("itemName"),
                    itemQuantity = reader.GetDouble("itemQuantity"),
                    itemCategory = reader.IsDBNull(reader.GetOrdinal("itemCategory")) ? "" : reader.GetString("itemCategory"),
                    ImageSet = reader.IsDBNull(reader.GetOrdinal("imageSet")) ? "" : reader.GetString("imageSet"),
                    itemDescription = reader.IsDBNull(reader.GetOrdinal("itemDescription")) ? "" : reader.GetString("itemDescription"),
                    unitOfMeasurement = reader.IsDBNull(reader.GetOrdinal("unitOfMeasurement")) ? "" : reader.GetString("unitOfMeasurement"),
                    minimumQuantity = reader.IsDBNull(reader.GetOrdinal("minimumQuantity")) ? 0 : reader.GetDouble("minimumQuantity")
                };
            }
            return null;
        }

		public async Task<InventoryPageModel?> GetInventoryItemByNameCachedAsync(string itemName)
		{
			var list = await GetInventoryItemsAsyncCached();
			return list.FirstOrDefault(i => string.Equals(i.itemName?.Trim(), itemName?.Trim(), StringComparison.OrdinalIgnoreCase));
		}

        // Deduct inventory quantities by item name and amount
        public async Task<int> DeductInventoryAsync(IEnumerable<(string name, double amount)> deductions)
        {
            await using var conn = await GetOpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            int totalAffected = 0;
            try
            {
                foreach (var (name, amount) in deductions)
                {
                    var updateSql = "UPDATE inventory SET itemQuantity = GREATEST(itemQuantity - @Amount, 0) WHERE itemName = @Name;";
                    await using var updateCmd = new MySqlCommand(updateSql, conn, (MySqlTransaction)tx);
                    updateCmd.Parameters.AddWithValue("@Amount", amount);
                    updateCmd.Parameters.AddWithValue("@Name", name);
                    totalAffected += await updateCmd.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                InvalidateInventoryCache();
                return totalAffected;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<int> SaveInventoryItemAsync(InventoryPageModel inventory)
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = "INSERT INTO inventory (itemName, itemQuantity, itemCategory, imageSet, itemDescription, unitOfMeasurement, minimumQuantity) " +
                      "VALUES (@ItemName, @ItemQuantity, @ItemCategory, @ImageSet, @ItemDescription, @UnitOfMeasurement, @MinimumQuantity);";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ItemName", inventory.itemName);
            cmd.Parameters.AddWithValue("@ItemQuantity", inventory.itemQuantity);
            cmd.Parameters.AddWithValue("@ItemCategory", inventory.itemCategory);
            cmd.Parameters.AddWithValue("@ImageSet", inventory.ImageSet);
            cmd.Parameters.AddWithValue("@ItemDescription", (object?)inventory.itemDescription ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UnitOfMeasurement", (object?)inventory.unitOfMeasurement ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@MinimumQuantity", inventory.minimumQuantity);

            var rows = await cmd.ExecuteNonQueryAsync();
            InvalidateInventoryCache();
            return rows;
        }

        public async Task<int> SaveTransactionAsync(TransactionHistoryModel transaction)
        {
            await using var conn = await GetOpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // Insert into transactions table
                var transactionSql = "INSERT INTO transactions (userID, total, transactionDate, status) VALUES (@UserID, @Total, @TransactionDate, @Status);";
                await using var transactionCmd = new MySqlCommand(transactionSql, conn, (MySqlTransaction)tx);
                transactionCmd.Parameters.AddWithValue("@UserID", 1); // Default user ID for now
                transactionCmd.Parameters.AddWithValue("@Total", transaction.Total);
                transactionCmd.Parameters.AddWithValue("@TransactionDate", transaction.TransactionDate);
                transactionCmd.Parameters.AddWithValue("@Status", transaction.Status);

                await transactionCmd.ExecuteNonQueryAsync();
                int transactionId = (int)transactionCmd.LastInsertedId;

                // Insert into transaction_items table
                var itemSql = "INSERT INTO transaction_items (transactionID, productID, productName, quantity, price, size) VALUES (@TransactionID, @ProductID, @ProductName, @Quantity, @Price, @Size);";
                await using var itemCmd = new MySqlCommand(itemSql, conn, (MySqlTransaction)tx);
                itemCmd.Parameters.AddWithValue("@TransactionID", transactionId);
                itemCmd.Parameters.AddWithValue("@ProductID", 1); // Default product ID for now
                itemCmd.Parameters.AddWithValue("@ProductName", transaction.DrinkName);
                itemCmd.Parameters.AddWithValue("@Quantity", transaction.Quantity);
                itemCmd.Parameters.AddWithValue("@Price", transaction.Price);
                itemCmd.Parameters.AddWithValue("@Size", transaction.Size ?? "");

                await itemCmd.ExecuteNonQueryAsync();

                await tx.CommitAsync();
                return transactionId;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<List<TransactionHistoryModel>> GetTransactionsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = @"SELECT t.transactionID, t.total, t.transactionDate, t.status,
                        ti.productName, ti.quantity, ti.price, ti.size
                        FROM transactions t
                        LEFT JOIN transaction_items ti ON t.transactionID = ti.transactionID
                        WHERE t.transactionDate >= @StartDate AND t.transactionDate <= @EndDate
                        ORDER BY t.transactionDate DESC";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@StartDate", startDate);
            cmd.Parameters.AddWithValue("@EndDate", endDate);

            var transactions = new List<TransactionHistoryModel>();
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var transaction = new TransactionHistoryModel
                {
                    TransactionId = reader.GetInt32("transactionID"),
                    Total = reader.GetDecimal("total"),
                    TransactionDate = reader.GetDateTime("transactionDate"),
                    Status = reader.GetString("status")
                };

                if (!reader.IsDBNull(reader.GetOrdinal("productName")))
                {
                    transaction.DrinkName = reader.GetString("productName");
                    transaction.Quantity = reader.GetInt32("quantity");
                    transaction.Price = reader.GetDecimal("price");
                    transaction.Size = reader.IsDBNull(reader.GetOrdinal("size")) ? "" : reader.GetString("size");
                }

                transactions.Add(transaction);
            }

            return transactions;
        }

        public async Task<Dictionary<string, int>> GetTopProductsByDateRangeAsync(DateTime startDate, DateTime endDate, int limit = 10)
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = @"SELECT ti.productName, SUM(ti.quantity) as totalQuantity
                        FROM transactions t
                        JOIN transaction_items ti ON t.transactionID = ti.transactionID
                        WHERE t.transactionDate >= @StartDate AND t.transactionDate <= @EndDate
                        GROUP BY ti.productName
                        ORDER BY totalQuantity DESC
                        LIMIT @Limit";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@StartDate", startDate);
            cmd.Parameters.AddWithValue("@EndDate", endDate);
            cmd.Parameters.AddWithValue("@Limit", limit);

            var results = new Dictionary<string, int>();
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var productName = reader.GetString("productName");
                var quantity = reader.GetInt32("totalQuantity");
                results[productName] = quantity;
            }

            return results;
        }

        public async Task<int> UpdateInventoryItemAsync(InventoryPageModel inventory)
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = "UPDATE inventory SET itemName = @ItemName, itemQuantity = @ItemQuantity, itemCategory = @ItemCategory, " +
                      "imageSet = @ImageSet, itemDescription = @ItemDescription, unitOfMeasurement = @UnitOfMeasurement, " +
                      "minimumQuantity = @MinimumQuantity WHERE itemID = @ItemID;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ItemID", inventory.itemID);
            cmd.Parameters.AddWithValue("@ItemName", inventory.itemName);
            cmd.Parameters.AddWithValue("@ItemQuantity", inventory.itemQuantity);
            cmd.Parameters.AddWithValue("@ItemCategory", inventory.itemCategory);
            cmd.Parameters.AddWithValue("@ImageSet", inventory.ImageSet);
            cmd.Parameters.AddWithValue("@ItemDescription", (object?)inventory.itemDescription ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UnitOfMeasurement", (object?)inventory.unitOfMeasurement ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@MinimumQuantity", inventory.minimumQuantity);

            var rows = await cmd.ExecuteNonQueryAsync();
            InvalidateInventoryCache();
            return rows;
        }

        public async Task<int> DeleteInventoryItemAsync(int itemId)
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = "DELETE FROM inventory WHERE itemID = @ItemID;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ItemID", itemId);

            var rows = await cmd.ExecuteNonQueryAsync();
            InvalidateInventoryCache();
            return rows;
        }

        // User Management
        public async Task<string> RequestPasswordResetAsync(string email)
        {
            await using var conn = await GetOpenConnectionAsync();

            // Check if user exists
            var sql = "SELECT * FROM users WHERE email = @Email LIMIT 1;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Email", email);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null; // User not found
            }

            // Generate reset token
            var resetToken = Guid.NewGuid().ToString();
            var resetExpiry = DateTime.Now.AddHours(1); // Token expires in 1 hour

            reader.Close();


            // Update user with reset token
            var updateSql = "UPDATE users SET reset_token = @ResetToken, reset_expiry = @ResetExpiry WHERE email = @Email;";
            await using var updateCmd = new MySqlCommand(updateSql, conn);
            updateCmd.Parameters.AddWithValue("@ResetToken", resetToken);
            updateCmd.Parameters.AddWithValue("@ResetExpiry", resetExpiry);
            updateCmd.Parameters.AddWithValue("@Email", email);

            int rowsAffected = await updateCmd.ExecuteNonQueryAsync();
            return rowsAffected > 0 ? resetToken : null;
        }
    

        public async Task<bool> ResetPasswordAsync(string email, string newPassword, string resetToken)
        {
            await using var conn = await GetOpenConnectionAsync();

            // Verify token and expiry
            var sql = "SELECT * FROM users WHERE email = @Email AND reset_token = @ResetToken AND reset_expiry > NOW() LIMIT 1;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@ResetToken", resetToken);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return false; // Invalid or expired token
            }

            reader.Close();

            // Update password and clear reset token
            var updateSql = "UPDATE users SET password = @NewPassword, reset_token = NULL, reset_expiry = NULL WHERE email = @Email;";
            await using var updateCmd = new MySqlCommand(updateSql, conn);
            updateCmd.Parameters.AddWithValue("@NewPassword", BCrypt.Net.BCrypt.HashPassword(newPassword));
            updateCmd.Parameters.AddWithValue("@Email", email);

            int rowsAffected = await updateCmd.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        // Database initialization
        public async Task InitializeDatabaseAsync()
        {
            await using var conn = await GetOpenConnectionAsync();
            
            // Create tables if they don't exist
            var createTablesSql = @"
                CREATE TABLE IF NOT EXISTS users (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    firstName VARCHAR(100),
                    lastName VARCHAR(100),
                    email VARCHAR(255) UNIQUE NOT NULL,
                    password VARCHAR(255) NOT NULL,
                    phoneNumber VARCHAR(20),
                    birthday DATE,
                    address TEXT,
                    status VARCHAR(50) DEFAULT 'approved',
                    isAdmin BOOLEAN DEFAULT FALSE,
                    can_access_inventory BOOLEAN DEFAULT FALSE,
                    can_access_sales_report BOOLEAN DEFAULT FALSE,
                    reset_token VARCHAR(255),
                    reset_expiry DATETIME,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                );
                
                CREATE TABLE IF NOT EXISTS products (
                    productID INT AUTO_INCREMENT PRIMARY KEY,
                    productName VARCHAR(255) NOT NULL,
                    smallPrice DECIMAL(10,2) NOT NULL,
                    mediumPrice DECIMAL(10,2) NOT NULL DEFAULT 0,
                    largePrice DECIMAL(10,2) NOT NULL,
                    category VARCHAR(100),
                    subcategory VARCHAR(100),
                    imageSet VARCHAR(255),
                    description TEXT,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                );
                
                CREATE TABLE IF NOT EXISTS inventory (
                    itemID INT AUTO_INCREMENT PRIMARY KEY,
                    itemName VARCHAR(255) NOT NULL,
                    itemQuantity DECIMAL(10,2) NOT NULL DEFAULT 0,
                    itemCategory VARCHAR(100),
                    imageSet VARCHAR(255),
                    itemDescription TEXT,
                    unitOfMeasurement VARCHAR(50),
                    minimumQuantity DECIMAL(10,2) DEFAULT 0,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                );
                
                CREATE TABLE IF NOT EXISTS product_ingredients (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    productID INT NOT NULL,
                    itemID INT NOT NULL,
                    amount DECIMAL(10,2) NOT NULL,
                    unit VARCHAR(50),
                    role VARCHAR(50) DEFAULT 'ingredient',
                    FOREIGN KEY (productID) REFERENCES products(productID) ON DELETE CASCADE,
                    FOREIGN KEY (itemID) REFERENCES inventory(itemID) ON DELETE CASCADE
                );
    
                CREATE TABLE IF NOT EXISTS product_addons (
                  id INT AUTO_INCREMENT PRIMARY KEY,
                  productID INT NOT NULL,
                  itemID INT NOT NULL,
                  amount DECIMAL(10,2) NOT NULL,
                  unit VARCHAR(50),
                  role VARCHAR(50) DEFAULT 'addon',
                  addon_price DECIMAL(10,2) DEFAULT 0.00,
                  UNIQUE KEY uq_product_item (productID, itemID),
                  INDEX idx_product (productID),
                  INDEX idx_item (itemID),
                  FOREIGN KEY (productID) REFERENCES products(productID) ON DELETE CASCADE,
                  FOREIGN KEY (itemID) REFERENCES inventory(itemID) ON DELETE CASCADE
                );
                
                CREATE TABLE IF NOT EXISTS transactions (
                    transactionID INT AUTO_INCREMENT PRIMARY KEY,
                    userID INT,
                    total DECIMAL(10,2) NOT NULL,
                    transactionDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                    status VARCHAR(50) DEFAULT 'completed',
                    FOREIGN KEY (userID) REFERENCES users(id)
                );
                
                CREATE TABLE IF NOT EXISTS transaction_items (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    transactionID INT NOT NULL,
                    productID INT NOT NULL,
                    productName VARCHAR(255) NOT NULL,
                    quantity INT NOT NULL,
                    price DECIMAL(10,2) NOT NULL,
                    size VARCHAR(20),
                    FOREIGN KEY (transactionID) REFERENCES transactions(transactionID) ON DELETE CASCADE,
                    FOREIGN KEY (productID) REFERENCES products(productID)
                );
                
                CREATE TABLE IF NOT EXISTS pending_registrations (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    email VARCHAR(255) NOT NULL,
                    password VARCHAR(255) NOT NULL,
                    firstName VARCHAR(100),
                    lastName VARCHAR(100),
                    phoneNumber VARCHAR(20),
                    address TEXT,
                    birthday DATE,
                    registrationDate DATETIME DEFAULT CURRENT_TIMESTAMP
                );
            ";
            
            await using var cmd = new MySqlCommand(createTablesSql, conn);
            await cmd.ExecuteNonQueryAsync();

            // Add status column to existing users table if it doesn't exist
            var alterTableSql = @"
                ALTER TABLE users 
                ADD COLUMN IF NOT EXISTS status VARCHAR(50) DEFAULT 'approved',
                ADD COLUMN IF NOT EXISTS can_access_inventory BOOLEAN DEFAULT FALSE,
                ADD COLUMN IF NOT EXISTS can_access_sales_report BOOLEAN DEFAULT FALSE;
            ";
            await using var alterCmd = new MySqlCommand(alterTableSql, conn);
            await alterCmd.ExecuteNonQueryAsync();

            // Seed default cups and straws if not present
            var seedSql = @"
                INSERT INTO inventory (itemName, itemQuantity, itemCategory, unitOfMeasurement, minimumQuantity)
                SELECT * FROM (SELECT 'Small Cup' AS itemName, 100 AS itemQuantity, 'Supplies' AS itemCategory, 'pcs' AS unitOfMeasurement, 20 AS minimumQuantity) AS tmp
                WHERE NOT EXISTS (SELECT 1 FROM inventory WHERE itemName = 'Small Cup') LIMIT 1;
                INSERT INTO inventory (itemName, itemQuantity, itemCategory, unitOfMeasurement, minimumQuantity)
                SELECT * FROM (SELECT 'Medium Cup' AS itemName, 100 AS itemQuantity, 'Supplies' AS itemCategory, 'pcs' AS unitOfMeasurement, 20 AS minimumQuantity) AS tmp
                WHERE NOT EXISTS (SELECT 1 FROM inventory WHERE itemName = 'Medium Cup') LIMIT 1;
                INSERT INTO inventory (itemName, itemQuantity, itemCategory, unitOfMeasurement, minimumQuantity)
                SELECT * FROM (SELECT 'Large Cup' AS itemName, 100 AS itemQuantity, 'Supplies' AS itemCategory, 'pcs' AS unitOfMeasurement, 20 AS minimumQuantity) AS tmp
                WHERE NOT EXISTS (SELECT 1 FROM inventory WHERE itemName = 'Large Cup') LIMIT 1;
                INSERT INTO inventory (itemName, itemQuantity, itemCategory, unitOfMeasurement, minimumQuantity)
                SELECT * FROM (SELECT 'Straw' AS itemName, 200 AS itemQuantity, 'Supplies' AS itemCategory, 'pcs' AS unitOfMeasurement, 50 AS minimumQuantity) AS tmp
                WHERE NOT EXISTS (SELECT 1 FROM inventory WHERE itemName = 'Straw') LIMIT 1;
            ";
            await using var seedCmd = new MySqlCommand(seedSql, conn);
            await seedCmd.ExecuteNonQueryAsync();
        }

        // Update user access flags
        public async Task<int> UpdateUserAccessAsync(int userId, bool canAccessInventory, bool canAccessSalesReport)
        {
            await using var conn = await GetOpenConnectionAsync();
            var sql = "UPDATE users SET can_access_inventory = @Inv, can_access_sales_report = @Sales WHERE id = @Id;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Inv", canAccessInventory);
            cmd.Parameters.AddWithValue("@Sales", canAccessSalesReport);
            cmd.Parameters.AddWithValue("@Id", userId);
            return await cmd.ExecuteNonQueryAsync();
        }

        // Delete user
        public async Task<int> DeleteUserAsync(int userId)
        {
            await using var conn = await GetOpenConnectionAsync();
            var sql = "DELETE FROM users WHERE id = @Id;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", userId);
            return await cmd.ExecuteNonQueryAsync();
        }
        
        // Update existing users to approved status
        public async Task UpdateExistingUsersToApprovedAsync()
        {
            await using var conn = await GetOpenConnectionAsync();
            
            // This method can be used to update user statuses or other initialization tasks
            // For now, it's a placeholder for future user management features
            var sql = "SELECT COUNT(*) FROM users;";
            await using var cmd = new MySqlCommand(sql, conn);
            var userCount = await cmd.ExecuteScalarAsync();
            
            System.Diagnostics.Debug.WriteLine($"Database initialized with {userCount} users.");
        }
        

        // User Pending Requests
        public async Task<int> AddPendingUserRequestAsync(UserPendingRequest request)
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = "INSERT INTO pending_registrations (email, password, firstName, lastName, phoneNumber, address, birthday, registrationDate) " +
                      "VALUES (@Email, @Password, @FirstName, @LastName, @PhoneNumber, @Address, @Birthday, @RegistrationDate);";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Email", request.Email);
            cmd.Parameters.AddWithValue("@Password", request.Password);
            cmd.Parameters.AddWithValue("@FirstName", request.FirstName);
            cmd.Parameters.AddWithValue("@LastName", request.LastName);
            cmd.Parameters.AddWithValue("@PhoneNumber", request.PhoneNumber);
            cmd.Parameters.AddWithValue("@Address", request.Address);
            cmd.Parameters.AddWithValue("@Birthday", request.Birthday.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@RegistrationDate", request.RequestDate);

            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<UserPendingRequest>> GetPendingUserRequestsAsync()
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = "SELECT * FROM pending_registrations ORDER BY registrationDate ASC;";
            await using var cmd = new MySqlCommand(sql, conn);

            var requests = new List<UserPendingRequest>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                requests.Add(new UserPendingRequest
                {
                    ID = reader.GetInt32("id"),
                    FirstName = reader.IsDBNull(reader.GetOrdinal("firstName")) ? string.Empty : reader.GetString("firstName"),
                    LastName = reader.IsDBNull(reader.GetOrdinal("lastName")) ? string.Empty : reader.GetString("lastName"),
                    Email = reader.IsDBNull(reader.GetOrdinal("email")) ? string.Empty : reader.GetString("email"),
                    Password = reader.IsDBNull(reader.GetOrdinal("password")) ? string.Empty : reader.GetString("password"),
                    PhoneNumber = reader.IsDBNull(reader.GetOrdinal("phoneNumber")) ? string.Empty : reader.GetString("phoneNumber"),
                    Address = reader.IsDBNull(reader.GetOrdinal("address")) ? string.Empty : reader.GetString("address"),
                    Birthday = reader.IsDBNull(reader.GetOrdinal("birthday")) ? DateTime.MinValue : reader.GetDateTime("birthday"),
                    RequestDate = reader.IsDBNull(reader.GetOrdinal("registrationDate")) ? DateTime.MinValue : reader.GetDateTime("registrationDate"),
                    Status = "pending" // Default status since it's not stored in database
                });
            }
            return requests;
        }

        public async Task<int> ApprovePendingRegistrationAsync(int requestId)
        {
            await using var conn = await GetOpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // Get the pending registration
                var getSql = "SELECT * FROM pending_registrations WHERE id = @Id;";
                await using var getCmd = new MySqlCommand(getSql, conn, (MySqlTransaction)tx);
                getCmd.Parameters.AddWithValue("@Id", requestId);

                UserPendingRequest registration = null;
                await using var reader = await getCmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    registration = new UserPendingRequest
                    {
                        ID = reader.GetInt32("id"),
                        Email = reader.GetString("email"),
                        Password = reader.GetString("password"),
                        FirstName = reader.GetString("firstName"),
                        LastName = reader.GetString("lastName"),
                        PhoneNumber = reader.GetString("phoneNumber"),
                        Address = reader.GetString("address"),
                        Birthday = reader.GetDateTime("birthday"),
                        RequestDate = reader.GetDateTime("registrationDate")
                    };
                }
                await reader.CloseAsync();

                if (registration == null)
                {
                    await tx.RollbackAsync();
                    return 0;
                }

                // Check if this will be the first user (admin)
                var checkUsersSql = "SELECT COUNT(*) FROM users;";
                await using var checkUsersCmd = new MySqlCommand(checkUsersSql, conn, (MySqlTransaction)tx);
                var userCount = Convert.ToInt32(await checkUsersCmd.ExecuteScalarAsync());
                bool isFirstUser = userCount == 0;

                // Add to users table
                var addUserSql = "INSERT INTO users (firstName, lastName, email, password, phoneNumber, birthday, address, status, isAdmin) " +
                                "VALUES (@FirstName, @LastName, @Email, @Password, @PhoneNumber, @Birthday, @Address, @Status, @IsAdmin);";
                await using var addUserCmd = new MySqlCommand(addUserSql, conn, (MySqlTransaction)tx);
                addUserCmd.Parameters.AddWithValue("@FirstName", registration.FirstName);
                addUserCmd.Parameters.AddWithValue("@LastName", registration.LastName);
                addUserCmd.Parameters.AddWithValue("@Email", registration.Email);
                addUserCmd.Parameters.AddWithValue("@Password", registration.Password);
                addUserCmd.Parameters.AddWithValue("@PhoneNumber", registration.PhoneNumber);
                addUserCmd.Parameters.AddWithValue("@Birthday", registration.Birthday.ToString("yyyy-MM-dd"));
                addUserCmd.Parameters.AddWithValue("@Address", registration.Address);
                addUserCmd.Parameters.AddWithValue("@Status", "approved");
                addUserCmd.Parameters.AddWithValue("@IsAdmin", isFirstUser);

                await addUserCmd.ExecuteNonQueryAsync();

                // Remove from pending registrations
                var deleteSql = "DELETE FROM pending_registrations WHERE id = @Id;";
                await using var deleteCmd = new MySqlCommand(deleteSql, conn, (MySqlTransaction)tx);
                deleteCmd.Parameters.AddWithValue("@Id", requestId);
                await deleteCmd.ExecuteNonQueryAsync();

                await tx.CommitAsync();
                return 1;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<int> RejectPendingRegistrationAsync(int requestId)
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = "DELETE FROM pending_registrations WHERE id = @RequestId;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@RequestId", requestId);

            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<UserPendingRequest>> GetPendingRegistrationsAsync()
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = "SELECT * FROM pending_registrations ORDER BY registrationDate ASC;";
            await using var cmd = new MySqlCommand(sql, conn);

            var requests = new List<UserPendingRequest>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                requests.Add(new UserPendingRequest
                {
                    ID = reader.GetInt32("id"),
                    FirstName = reader.IsDBNull(reader.GetOrdinal("firstName")) ? string.Empty : reader.GetString("firstName"),
                    LastName = reader.IsDBNull(reader.GetOrdinal("lastName")) ? string.Empty : reader.GetString("lastName"),
                    Email = reader.IsDBNull(reader.GetOrdinal("email")) ? string.Empty : reader.GetString("email"),
                    Password = reader.IsDBNull(reader.GetOrdinal("password")) ? string.Empty : reader.GetString("password"),
                    PhoneNumber = reader.IsDBNull(reader.GetOrdinal("phoneNumber")) ? string.Empty : reader.GetString("phoneNumber"),
                    Address = reader.IsDBNull(reader.GetOrdinal("address")) ? string.Empty : reader.GetString("address"),
                    Birthday = reader.IsDBNull(reader.GetOrdinal("birthday")) ? DateTime.MinValue : reader.GetDateTime("birthday"),
                    RequestDate = reader.IsDBNull(reader.GetOrdinal("registrationDate")) ? DateTime.MinValue : reader.GetDateTime("registrationDate"),
                    Status = "pending" // Default status since it's not stored in database
                });
            }
            return requests;
        }

        // Notification

        // Cart

        //
    }
}
