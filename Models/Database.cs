using Coftea_Capstone.Models;
using MySqlConnector;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Maui.Devices;

using Coftea_Capstone.C_;
using Coftea_Capstone.Services;

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
            var server = host ?? GetDefaultHostForPlatform();
            
            _db = new MySqlConnectionStringBuilder // Configuration for connecting to MySQL XAMPP server database
            {
                Server = server,
                Port = 3306,
                Database = "coftea_db",
                UserID = "root",
                Password = "",
                SslMode = MySqlSslMode.None
            }.ConnectionString;
        }

        // Connectivity test to detect if DB server is reachable
        public async Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await using var conn = new MySqlConnection(_db);
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(3));
                await conn.OpenAsync(cts.Token);
                await conn.CloseAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Ensure server is reachable and the database exists; create DB if missing
        public async Task EnsureServerAndDatabaseAsync(CancellationToken cancellationToken = default)
        {
            // Use the configured connection string
            await using var conn = new MySqlConnection(_db);
            await conn.OpenAsync(cancellationToken);
            var createDbSql = "CREATE DATABASE IF NOT EXISTS coftea_db;";
            await using var cmd = new MySqlCommand(createDbSql, conn);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        private async Task<MySqlConnection> GetOpenConnectionAsync(CancellationToken cancellationToken = default)
        {
            // Use the configured connection string directly
            var conn = new MySqlConnection(_db);
            await conn.OpenAsync(cancellationToken);
            return conn;
        }

        private static string GetDefaultHostForPlatform() // Detects which platform the app is running on
        {
            try
            {
                // Use automatic IP detection if available and enabled
                if (NetworkConfigurationService.IsAutomaticDetectionEnabled())
                {
                    var autoDetectedIP = NetworkConfigurationService.GetAutoDetectedIP();
                    if (!string.IsNullOrEmpty(autoDetectedIP) && autoDetectedIP != "localhost")
                    {
                        System.Diagnostics.Debug.WriteLine($"🤖 Using auto-detected IP for platform: {autoDetectedIP}");
                        return autoDetectedIP;
                    }
                }

                // Fallback to platform-specific hardcoded IPs
                if (DeviceInfo.Platform == DevicePlatform.Android)
                {
                    return "192.168.1.8";
                }

                if (DeviceInfo.Platform == DevicePlatform.iOS)
                {
                    return "192.168.1.6";
                }

                if (DeviceInfo.Platform == DevicePlatform.WinUI || DeviceInfo.Platform == DevicePlatform.macOS || DeviceInfo.Platform == DevicePlatform.MacCatalyst)
                {
                    return "192.168.1.6";
                }
            }
            catch
            {
                // If DeviceInfo is unavailable (e.g., during tests), fall back to localhost
            }

            // Fallback for other/unknown platforms
            return "localhost";
        }

		// Cache helpers
		private static bool IsFresh(DateTime ts) => (DateTime.UtcNow - ts) < CacheDuration;
		private static void InvalidateProductsCache() => _productsCache = null;
		public void InvalidateInventoryCache() => _inventoryCache = null;
		private static void InvalidateProductLinksCache(int productId)
		{
			_productIngredientsCache.Remove(productId);
			_productAddonsCache.Remove(productId);
		}

        // Database initialization
        public async Task InitializeDatabaseAsync(CancellationToken cancellationToken = default)
        {
            await using var conn = await GetOpenConnectionAsync(cancellationToken);

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
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    username VARCHAR(100),
                    fullName VARCHAR(200),
                    profileImage VARCHAR(255) DEFAULT 'usericon.png'
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
                    colorCode VARCHAR(20) DEFAULT NULL,
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
                    maximumQuantity DECIMAL(10,2) DEFAULT 0,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                );
                
                CREATE TABLE IF NOT EXISTS product_ingredients (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    productID INT NOT NULL,
                    itemID INT NOT NULL,
                    amount DECIMAL(10,4) NOT NULL,
                    unit VARCHAR(50),
                    -- Per-size amounts/units (nullable; fallback to shared amount/unit when NULL)
                    amount_small  DECIMAL(10,4) NULL,
                    unit_small    VARCHAR(50)   NULL,
                    amount_medium DECIMAL(10,4) NULL,
                    unit_medium   VARCHAR(50)   NULL,
                    amount_large  DECIMAL(10,4) NULL,
                    unit_large    VARCHAR(50)   NULL,
                    role VARCHAR(50) DEFAULT 'ingredient',
                    FOREIGN KEY (productID) REFERENCES products(productID) ON DELETE CASCADE,
                    FOREIGN KEY (itemID) REFERENCES inventory(itemID) ON DELETE CASCADE
                );
    
                CREATE TABLE IF NOT EXISTS product_addons (
                  id INT AUTO_INCREMENT PRIMARY KEY,
                  productID INT NOT NULL,
                  itemID INT NOT NULL,
                  amount DECIMAL(10,4) NOT NULL,
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
                    paymentMethod VARCHAR(50) DEFAULT 'Cash',
                    FOREIGN KEY (userID) REFERENCES users(id) ON DELETE SET NULL ON UPDATE CASCADE
                );
                
                CREATE TABLE IF NOT EXISTS transaction_items (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    transactionID INT NOT NULL,
                    productID INT,
                    productName VARCHAR(255) NOT NULL,
                    quantity INT NOT NULL,
                    price DECIMAL(10,2) NOT NULL,
                    smallPrice DECIMAL(10,2) DEFAULT 0.00,
                    mediumPrice DECIMAL(10,2) DEFAULT 0.00,
                    largePrice DECIMAL(10,2) DEFAULT 0.00,
                    addonPrice DECIMAL(10,2) DEFAULT 0.00,
                    addOns TEXT,
                    size VARCHAR(20),
                    CONSTRAINT fk_tx_items_tx FOREIGN KEY (transactionID) REFERENCES transactions(transactionID) ON DELETE CASCADE,
                    CONSTRAINT fk_tx_items_product FOREIGN KEY (productID) REFERENCES products(productID) ON DELETE SET NULL ON UPDATE CASCADE
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
                
                CREATE TABLE IF NOT EXISTS purchase_orders (
                    purchaseOrderId INT AUTO_INCREMENT PRIMARY KEY,
                    orderDate DATETIME NOT NULL,
                    supplierName VARCHAR(255) NOT NULL,
                    status VARCHAR(50) DEFAULT 'Pending',
                    requestedBy VARCHAR(255) NOT NULL,
                    approvedBy VARCHAR(255) DEFAULT NULL,
                    approvedDate DATETIME DEFAULT NULL,
                    notes TEXT,
                    totalAmount DECIMAL(10,2) DEFAULT 0.00,
                    createdAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    updatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
                );
                
                CREATE TABLE IF NOT EXISTS purchase_order_items (
                    purchaseOrderItemId INT AUTO_INCREMENT PRIMARY KEY,
                    purchaseOrderId INT NOT NULL,
                    inventoryItemId INT NOT NULL,
                    itemName VARCHAR(255) NOT NULL,
                    itemCategory VARCHAR(100),
                    requestedQuantity INT NOT NULL,
                    approvedQuantity INT DEFAULT 0,
                    unitPrice DECIMAL(10,2) NOT NULL,
                    totalPrice DECIMAL(10,2) NOT NULL,
                    unitOfMeasurement VARCHAR(50),
                    notes TEXT,
                    FOREIGN KEY (purchaseOrderId) REFERENCES purchase_orders(purchaseOrderId) ON DELETE CASCADE,
                    FOREIGN KEY (inventoryItemId) REFERENCES inventory(itemID) ON DELETE CASCADE
                );
            ";

            await using var cmd = new MySqlCommand(createTablesSql, conn);
            await cmd.ExecuteNonQueryAsync();


            // Ensure at least one user exists to prevent foreign key constraint errors
            await EnsureDefaultUserExistsAsync(conn);

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

        private static object DbValue(object? value) // Handles null values for database parameters
        {
			return value ?? DBNull.Value;
		}

		private static void AddParameters(MySqlCommand cmd, IDictionary<string, object?>? parameters) // Adds parameters to a MySQL command
        {
			if (parameters == null) return;
			foreach (var kvp in parameters)
			{
				cmd.Parameters.AddWithValue(kvp.Key, DbValue(kvp.Value));
			}
		}

		private async Task<int> ExecuteNonQueryAsync(string sql, IDictionary<string, 
                                                     object?>? parameters = null, CancellationToken cancellationToken = default) // Executes a non-query SQL command
        {
			await using var conn = await GetOpenConnectionAsync(cancellationToken);
			await using var cmd = new MySqlCommand(sql, conn);
			AddParameters(cmd, parameters);
			return await cmd.ExecuteNonQueryAsync(cancellationToken);
		}

		private async Task<List<T>> QueryAsync<T>(string sql, Func<MySqlDataReader, T> map, 
                                                  IDictionary<string, object?>? parameters = null, 
                                                  CancellationToken cancellationToken = default) // Executes a query and maps the results to a list of type T
        {
			await using var conn = await GetOpenConnectionAsync(cancellationToken);
			await using var cmd = new MySqlCommand(sql, conn);
			AddParameters(cmd, parameters);
			var list = new List<T>();
			await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
			while (await reader.ReadAsync(cancellationToken))
			{
				list.Add(map(reader));
			}
			return list;
		}

        // ===================== POS =====================        
        // POS Database
		public async Task<List<POSPageModel>> GetProductsAsync() // Gets all products from the database
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
				ProductDescription = reader.IsDBNull(reader.GetOrdinal("description")) ? "" : reader.GetString("description"),
				ColorCode = reader.IsDBNull(reader.GetOrdinal("colorCode")) ? "" : reader.GetString("colorCode")
			});
		}
		public async Task<List<POSPageModel>> GetProductsAsyncCached() // Gets all products with caching
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
                                        pi.amount_small, pi.unit_small,
                                        pi.amount_medium, pi.unit_medium,
                                        pi.amount_large, pi.unit_large,
                                        i.itemID, i.itemName, i.itemQuantity, i.itemCategory, i.imageSet,
                                        i.itemDescription, i.unitOfMeasurement, i.minimumQuantity, i.maximumQuantity
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
                    minimumQuantity = reader.IsDBNull(reader.GetOrdinal("minimumQuantity")) ? 0 : reader.GetDouble("minimumQuantity"),
                    maximumQuantity = HasColumn(reader, "maximumQuantity") ? (reader.IsDBNull(reader.GetOrdinal("maximumQuantity")) ? 0 : reader.GetDouble("maximumQuantity")) : 0
                };

                // Shared amount/unit
                double sharedAmt = reader.IsDBNull(reader.GetOrdinal("amount")) ? 0d : reader.GetDouble("amount");
                string sharedUnit = reader.IsDBNull(reader.GetOrdinal("unit")) ? item.unitOfMeasurement : reader.GetString("unit");

                // Per-size amounts/units: DO NOT fall back here; leave 0/empty when NULL so callers can decide
                item.InputAmountSmall = (HasColumn(reader, "amount_small") && !reader.IsDBNull(reader.GetOrdinal("amount_small")))
                    ? reader.GetDouble("amount_small") : 0d;
                item.InputAmountMedium = (HasColumn(reader, "amount_medium") && !reader.IsDBNull(reader.GetOrdinal("amount_medium")))
                    ? reader.GetDouble("amount_medium") : 0d;
                item.InputAmountLarge = (HasColumn(reader, "amount_large") && !reader.IsDBNull(reader.GetOrdinal("amount_large")))
                    ? reader.GetDouble("amount_large") : 0d;

                item.InputUnitSmall = (HasColumn(reader, "unit_small") && !reader.IsDBNull(reader.GetOrdinal("unit_small")))
                    ? reader.GetString("unit_small") : string.Empty;
                item.InputUnitMedium = (HasColumn(reader, "unit_medium") && !reader.IsDBNull(reader.GetOrdinal("unit_medium")))
                    ? reader.GetString("unit_medium") : string.Empty;
                item.InputUnitLarge = (HasColumn(reader, "unit_large") && !reader.IsDBNull(reader.GetOrdinal("unit_large")))
                    ? reader.GetString("unit_large") : string.Empty;

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

        // Get add-ons (linked inventory items) for a product from product_addons
        public async Task<List<InventoryPageModel>> GetProductAddonsAsync(int productId)
        {
            await using var conn = await GetOpenConnectionAsync();

            const string sql = @"SELECT pa.amount, pa.unit, pa.role, pa.addon_price,
                                        i.itemID, i.itemName, i.itemQuantity, i.itemCategory, i.imageSet,
                                        i.itemDescription, i.unitOfMeasurement, i.minimumQuantity, i.maximumQuantity
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
                    maximumQuantity = HasColumn(reader, "maximumQuantity") ? (reader.IsDBNull(reader.GetOrdinal("maximumQuantity")) ? 0 : reader.GetDouble("maximumQuantity")) : 0,
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

        public async Task<int> SaveProductAsync(POSPageModel product) // Saves a new product to the database
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = "INSERT INTO products (productName, smallPrice, mediumPrice, largePrice, category, subcategory, imageSet, description, colorCode) " +
                      "VALUES (@ProductName, @SmallPrice, @MediumPrice, @LargePrice, @Category, @Subcategory, @Image, @Description, @ColorCode);";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ProductName", product.ProductName);
            cmd.Parameters.AddWithValue("@SmallPrice", product.SmallPrice);
            cmd.Parameters.AddWithValue("@MediumPrice", product.MediumPrice);
            cmd.Parameters.AddWithValue("@LargePrice", product.LargePrice);
            cmd.Parameters.AddWithValue("@Category", product.Category);
            cmd.Parameters.AddWithValue("@Subcategory", (object?)product.Subcategory ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Image", product.ImageSet);
            cmd.Parameters.AddWithValue("@Description", (object?)product.ProductDescription ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ColorCode", (object?)product.ColorCode ?? DBNull.Value);

            var rows = await cmd.ExecuteNonQueryAsync();
            InvalidateProductsCache();
            return rows;
        }

        // Save product and return its new ID
        public async Task<int> SaveProductReturningIdAsync(POSPageModel product)
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = "INSERT INTO products (productName, smallPrice, mediumPrice, largePrice, category, subcategory, imageSet, description, colorCode) " +
                      "VALUES (@ProductName, @SmallPrice, @MediumPrice, @LargePrice, @Category, @Subcategory, @Image, @Description, @ColorCode);";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ProductName", product.ProductName);
            cmd.Parameters.AddWithValue("@SmallPrice", product.SmallPrice);
            cmd.Parameters.AddWithValue("@MediumPrice", product.MediumPrice);
            cmd.Parameters.AddWithValue("@LargePrice", product.LargePrice);
            cmd.Parameters.AddWithValue("@Category", product.Category);
            cmd.Parameters.AddWithValue("@Subcategory", (object?)product.Subcategory ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Image", product.ImageSet);
            cmd.Parameters.AddWithValue("@Description", (object?)product.ProductDescription ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ColorCode", (object?)product.ColorCode ?? DBNull.Value);

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

            // Clear existing links first to avoid duplicates/stale rows
            const string sqlClearIngredients = "DELETE FROM product_ingredients WHERE productID = @ProductID";
            const string sqlClearAddons = "DELETE FROM product_addons WHERE productID = @ProductID";
            const string sqlIngredients = @"INSERT INTO product_ingredients 
                (productID, itemID, amount, unit, role, amount_small, unit_small, amount_medium, unit_medium, amount_large, unit_large) 
                VALUES (@ProductID, @ItemID, @Amount, @Unit, 'ingredient', @AmtS, @UnitS, @AmtM, @UnitM, @AmtL, @UnitL);";
            const string sqlAddons = "INSERT INTO product_addons (productID, itemID, amount, unit, role, addon_price) VALUES (@ProductID, @ItemID, @Amount, @Unit, 'addon', @AddonPrice) ON DUPLICATE KEY UPDATE amount = VALUES(amount), unit = VALUES(unit), addon_price = VALUES(addon_price);";
            int total = 0;
            try
            {
                var ingredientList = ingredients?.ToList() ?? new List<(int inventoryItemId, double amount, string? unit)>();
                var addonList = addons?.ToList() ?? new List<(int inventoryItemId, double amount, string? unit, decimal addonPrice)>();

                // Clear previous links
                await using (var clearIngCmd = new MySqlCommand(sqlClearIngredients, conn, (MySqlTransaction)tx))
                {
                    clearIngCmd.Parameters.AddWithValue("@ProductID", productId);
                    await clearIngCmd.ExecuteNonQueryAsync();
                }
                await using (var clearAddCmd = new MySqlCommand(sqlClearAddons, conn, (MySqlTransaction)tx))
                {
                    clearAddCmd.Parameters.AddWithValue("@ProductID", productId);
                    await clearAddCmd.ExecuteNonQueryAsync();
                }

                foreach (var link in ingredientList)
                {
                    await using var cmd = new MySqlCommand(sqlIngredients, conn, (MySqlTransaction)tx);
                    cmd.Parameters.AddWithValue("@ProductID", productId);
                    cmd.Parameters.AddWithValue("@ItemID", link.inventoryItemId);
                    cmd.Parameters.AddWithValue("@Amount", link.amount);
                    cmd.Parameters.AddWithValue("@Unit", (object?)link.unit ?? DBNull.Value);
                    // Default per-size to shared value unless caller added explicit params
                    cmd.Parameters.AddWithValue("@AmtS", link.amount);
                    cmd.Parameters.AddWithValue("@UnitS", (object?)link.unit ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@AmtM", link.amount);
                    cmd.Parameters.AddWithValue("@UnitM", (object?)link.unit ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@AmtL", link.amount);
                    cmd.Parameters.AddWithValue("@UnitL", (object?)link.unit ?? DBNull.Value);
                    var affected = await cmd.ExecuteNonQueryAsync();
                    total += affected;
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
                }

                await tx.CommitAsync();
                InvalidateProductLinksCache(productId);
                return total;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // Overload that accepts per-size values for ingredients
        public async Task<int> SaveProductLinksSplitAsync(
            int productId,
            IEnumerable<(int inventoryItemId, double amount, string? unit, double amtS, string? unitS, double amtM, string? unitM, double amtL, string? unitL)> ingredients,
            IEnumerable<(int inventoryItemId, double amount, string? unit, decimal addonPrice)> addons)
        {
            await using var conn = await GetOpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            const string sqlClearIngredients = "DELETE FROM product_ingredients WHERE productID = @ProductID";
            const string sqlClearAddons = "DELETE FROM product_addons WHERE productID = @ProductID";
            const string sqlIngredients = @"INSERT INTO product_ingredients 
                (productID, itemID, amount, unit, role, amount_small, unit_small, amount_medium, unit_medium, amount_large, unit_large) 
                VALUES (@ProductID, @ItemID, @Amount, @Unit, 'ingredient', @AmtS, @UnitS, @AmtM, @UnitM, @AmtL, @UnitL);";
            const string sqlAddons = "INSERT INTO product_addons (productID, itemID, amount, unit, role, addon_price) VALUES (@ProductID, @ItemID, @Amount, @Unit, 'addon', @AddonPrice) ON DUPLICATE KEY UPDATE amount = VALUES(amount), unit = VALUES(unit), addon_price = VALUES(addon_price);";
            int total = 0;
            try
            {
                await using (var clearIngCmd = new MySqlCommand(sqlClearIngredients, conn, (MySqlTransaction)tx))
                {
                    clearIngCmd.Parameters.AddWithValue("@ProductID", productId);
                    await clearIngCmd.ExecuteNonQueryAsync();
                }
                await using (var clearAddCmd = new MySqlCommand(sqlClearAddons, conn, (MySqlTransaction)tx))
                {
                    clearAddCmd.Parameters.AddWithValue("@ProductID", productId);
                    await clearAddCmd.ExecuteNonQueryAsync();
                }

                foreach (var link in ingredients)
                {
                    await using var cmd = new MySqlCommand(sqlIngredients, conn, (MySqlTransaction)tx);
                    cmd.Parameters.AddWithValue("@ProductID", productId);
                    cmd.Parameters.AddWithValue("@ItemID", link.inventoryItemId);
                    cmd.Parameters.AddWithValue("@Amount", link.amount);
                    cmd.Parameters.AddWithValue("@Unit", (object?)link.unit ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@AmtS", link.amtS);
                    cmd.Parameters.AddWithValue("@UnitS", (object?)link.unitS ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@AmtM", link.amtM);
                    cmd.Parameters.AddWithValue("@UnitM", (object?)link.unitM ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@AmtL", link.amtL);
                    cmd.Parameters.AddWithValue("@UnitL", (object?)link.unitL ?? DBNull.Value);
                    total += await cmd.ExecuteNonQueryAsync();
                }

                foreach (var link in addons)
                {
                    await using var cmd = new MySqlCommand(sqlAddons, conn, (MySqlTransaction)tx);
                    cmd.Parameters.AddWithValue("@ProductID", productId);
                    cmd.Parameters.AddWithValue("@ItemID", link.inventoryItemId);
                    cmd.Parameters.AddWithValue("@Amount", link.amount);
                    cmd.Parameters.AddWithValue("@Unit", (object?)link.unit ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@AddonPrice", link.addonPrice);
                    total += await cmd.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                InvalidateProductLinksCache(productId);
                return total;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
        public async Task<POSPageModel?> GetProductByNameAsync(string name) // Gets a product by name from the database
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

		public async Task<POSPageModel?> GetProductByNameAsyncCached(string name) // Gets a product by name with caching
        {
			var list = await GetProductsAsyncCached();
			return list.FirstOrDefault(p => string.Equals(p.ProductName?.Trim(), name?.Trim(), StringComparison.OrdinalIgnoreCase));
		}

        public async Task<POSPageModel?> GetProductByIdAsync(int productId) // Gets a product by ID from the database
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

        public async Task<int> UpdateProductAsync(POSPageModel product) // Updates the product details 
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
            cmd.Parameters.AddWithValue("@ColorCode", (object?)product.ColorCode ?? DBNull.Value);

            var rows = await cmd.ExecuteNonQueryAsync();
            InvalidateProductsCache();
            InvalidateProductLinksCache(product.ProductID);
            return rows;
        }

        public async Task<int> DeleteProductAsync(int productId) // Deletes a product from the database
        {
            await using var conn = await GetOpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // Ensure product exists
                var checkSql = "SELECT COUNT(*) FROM products WHERE productID = @ProductID;";
                await using var checkCmd = new MySqlCommand(checkSql, conn, tx);
                checkCmd.Parameters.AddWithValue("@ProductID", productId);
                var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                if (exists == 0)
                {
                    await tx.RollbackAsync();
                    return 0;
                }

                // Delete the product; FK cascades will remove product_ingredients / product_addons automatically
                var sql = "DELETE FROM products WHERE productID = @ProductID;";
                await using var cmd = new MySqlCommand(sql, conn, tx);
                cmd.Parameters.AddWithValue("@ProductID", productId);
                var rows = await cmd.ExecuteNonQueryAsync();

                await tx.CommitAsync();
                InvalidateProductsCache();
                InvalidateProductLinksCache(productId);
                return rows;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // ===================== Inventory =====================
        // Inventory Database Methods
		public async Task<List<InventoryPageModel>> GetInventoryItemsAsync() // Gets all inventory items from the database
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
				minimumQuantity = reader.IsDBNull(reader.GetOrdinal("minimumQuantity")) ? 0 : reader.GetDouble("minimumQuantity"),
				maximumQuantity = HasColumn(reader, "maximumQuantity") ? (reader.IsDBNull(reader.GetOrdinal("maximumQuantity")) ? 0 : reader.GetDouble("maximumQuantity")) : 0
			});
		}
		private static bool HasColumn(System.Data.Common.DbDataReader reader, string columnName) // Checks if a column exists in the data reader
        {
			try
			{
				return reader.GetOrdinal(columnName) >= 0;
			}
			catch
			{
				return false;
			}
		}

		public async Task<List<InventoryPageModel>> GetInventoryItemsAsyncCached() // Gets all inventory items with caching
        {
			if (_inventoryCache.HasValue && IsFresh(_inventoryCache.Value.ts))
				return _inventoryCache.Value.items;

			var items = await GetInventoryItemsAsync();
			_inventoryCache = (DateTime.UtcNow, items);
			return items;
		}

        public async Task<InventoryPageModel?> GetInventoryItemByIdAsync(int itemId) // Gets an inventory item by ID from the database
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
                    minimumQuantity = reader.IsDBNull(reader.GetOrdinal("minimumQuantity")) ? 0 : reader.GetDouble("minimumQuantity"),
                    maximumQuantity = HasColumn(reader, "maximumQuantity") ? (reader.IsDBNull(reader.GetOrdinal("maximumQuantity")) ? 0 : reader.GetDouble("maximumQuantity")) : 0
                };
            }
            return null;
        }

        public async Task<InventoryPageModel?> GetInventoryItemByNameAsync(string itemName) // Gets an inventory item by name from the database
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
                    minimumQuantity = reader.IsDBNull(reader.GetOrdinal("minimumQuantity")) ? 0 : reader.GetDouble("minimumQuantity"),
                    maximumQuantity = HasColumn(reader, "maximumQuantity") ? (reader.IsDBNull(reader.GetOrdinal("maximumQuantity")) ? 0 : reader.GetDouble("maximumQuantity")) : 0
                };
            }
            return null;
        }
		public async Task<InventoryPageModel?> GetInventoryItemByNameCachedAsync(string itemName) // Gets an inventory item by name with caching
        {
			var list = await GetInventoryItemsAsyncCached();
			return list.FirstOrDefault(i => string.Equals(i.itemName?.Trim(), itemName?.Trim(), StringComparison.OrdinalIgnoreCase));
		}

        // Deduct inventory quantities by item name and amount
        public async Task<int> DeductInventoryAsync(IEnumerable<(string name, double amount)> deductions) // Deducts inventory quantities
        {
            System.Diagnostics.Debug.WriteLine($"🔧 DeductInventoryAsync: Starting with {deductions?.Count() ?? 0} deductions");
            
            await using var conn = await GetOpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            int totalAffected = 0;
            try
            {
                foreach (var (name, amount) in deductions)
                {
                    System.Diagnostics.Debug.WriteLine($"🔧 DeductInventoryAsync: Processing {name} - {amount}");
                    var updateSql = "UPDATE inventory SET itemQuantity = GREATEST(itemQuantity - @Amount, 0) WHERE itemName = @Name;";
                    await using var updateCmd = new MySqlCommand(updateSql, conn, (MySqlTransaction)tx);
                    updateCmd.Parameters.AddWithValue("@Amount", amount);
                    updateCmd.Parameters.AddWithValue("@Name", name);
                    var rowsAffected = await updateCmd.ExecuteNonQueryAsync();
                    System.Diagnostics.Debug.WriteLine($"🔧 DeductInventoryAsync: {name} - {rowsAffected} rows affected");
                    totalAffected += rowsAffected;
                }

                await tx.CommitAsync();
                InvalidateInventoryCache();
                
                System.Diagnostics.Debug.WriteLine($"🔧 DeductInventoryAsync: Transaction committed, total affected rows: {totalAffected}");
                
                // Check for minimum stock levels after deduction
                await CheckMinimumStockLevelsAsync();
                
                System.Diagnostics.Debug.WriteLine($"🔧 DeductInventoryAsync: Completed successfully, returning {totalAffected}");
                return totalAffected;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
        public async Task<int> SaveInventoryItemAsync(InventoryPageModel inventory) // Saves a new inventory item to the database
        {
            await using var conn = await GetOpenConnectionAsync();

            // Set default UoM based on category if not specified
            var unitOfMeasurement = inventory.unitOfMeasurement;
            if (string.IsNullOrWhiteSpace(unitOfMeasurement))
            {
                unitOfMeasurement = GetDefaultUnitForCategory(inventory.itemCategory);
            }

            var sql = "INSERT INTO inventory (itemName, itemQuantity, itemCategory, imageSet, itemDescription, unitOfMeasurement, minimumQuantity, maximumQuantity) " +
                      "VALUES (@ItemName, @ItemQuantity, @ItemCategory, @ImageSet, @ItemDescription, @UnitOfMeasurement, @MinimumQuantity, @MaximumQuantity);";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ItemName", inventory.itemName);
            cmd.Parameters.AddWithValue("@ItemQuantity", inventory.itemQuantity);
            cmd.Parameters.AddWithValue("@ItemCategory", inventory.itemCategory);
            cmd.Parameters.AddWithValue("@ImageSet", inventory.ImageSet);
            cmd.Parameters.AddWithValue("@ItemDescription", (object?)inventory.itemDescription ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UnitOfMeasurement", unitOfMeasurement);
            cmd.Parameters.AddWithValue("@MinimumQuantity", inventory.minimumQuantity);
            cmd.Parameters.AddWithValue("@MaximumQuantity", inventory.maximumQuantity);

            var rows = await cmd.ExecuteNonQueryAsync();
            InvalidateInventoryCache();
            return rows;
        }

        public async Task<int> UpdateInventoryItemAsync(InventoryPageModel inventory) // Updates an existing inventory item in the database
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = "UPDATE inventory SET itemName = @ItemName, itemQuantity = @ItemQuantity, itemCategory = @ItemCategory, " +
                      "imageSet = @ImageSet, itemDescription = @ItemDescription, unitOfMeasurement = @UnitOfMeasurement, " +
                      "minimumQuantity = @MinimumQuantity, maximumQuantity = @MaximumQuantity WHERE itemID = @ItemID;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ItemID", inventory.itemID);
            cmd.Parameters.AddWithValue("@ItemName", inventory.itemName);
            cmd.Parameters.AddWithValue("@ItemQuantity", inventory.itemQuantity);
            cmd.Parameters.AddWithValue("@ItemCategory", inventory.itemCategory);
            cmd.Parameters.AddWithValue("@ImageSet", inventory.ImageSet);
            cmd.Parameters.AddWithValue("@ItemDescription", (object?)inventory.itemDescription ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UnitOfMeasurement", (object?)inventory.unitOfMeasurement ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@MinimumQuantity", inventory.minimumQuantity);
            cmd.Parameters.AddWithValue("@MaximumQuantity", inventory.maximumQuantity);

            var rows = await cmd.ExecuteNonQueryAsync();
            InvalidateInventoryCache();
            return rows;
        }

        // Check for minimum stock levels and send notifications
        public async Task CheckMinimumStockLevelsAsync()
        {
            try
            {
                var inventoryItems = await GetInventoryItemsAsync();
                var lowStockItems = inventoryItems.Where(item => 
                    item.minimumQuantity > 0 && 
                    item.itemQuantity <= item.minimumQuantity).ToList();

                if (lowStockItems.Any())
                {
                    // Send notification for each low stock item
                    foreach (var item in lowStockItems)
                    {
                        await SendLowStockNotificationAsync(item);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking minimum stock levels: {ex.Message}");
            }
        }

        // Manual check for minimum stock levels (can be called on app startup or manually)
        public async Task CheckAllMinimumStockLevelsAsync()
        {
            try
            {
                var inventoryItems = await GetInventoryItemsAsync();
                var lowStockItems = inventoryItems.Where(item => 
                    item.minimumQuantity > 0 && 
                    item.itemQuantity <= item.minimumQuantity).ToList();

                if (lowStockItems.Any())
                {
                    // Send a single notification with all low stock items
                    var app = (App)Application.Current;
                    if (app?.NotificationPopup != null)
                    {
                        var itemList = string.Join(", ", lowStockItems.Select(item => $"{item.itemName} ({item.itemQuantity:F1}/{item.minimumQuantity:F1})"));
                        var message = $"Low Stock Alert: {lowStockItems.Count} item(s) below minimum level - {itemList}";
                        await app.NotificationPopup.AddNotification(
                            "Inventory Alert", 
                            message, 
                            $"Items: {lowStockItems.Count}", 
                            "Warning"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking all minimum stock levels: {ex.Message}");
            }
        }

        // Send low stock notification
        private async Task SendLowStockNotificationAsync(InventoryPageModel item)
        {
            try
            {
                var app = (App)Application.Current;
                if (app?.NotificationPopup != null)
                {
                    var message = $"Low Stock Alert: {item.itemName} is at {item.itemQuantity:F1} {item.unitOfMeasurement} (minimum: {item.minimumQuantity:F1} {item.unitOfMeasurement})";
                    await app.NotificationPopup.AddNotification(
                        "Low Stock Alert", 
                        message, 
                        $"ID: {item.itemID}", 
                        "Warning"
                    );
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending low stock notification: {ex.Message}");
            }
        }

        public async Task<int> DeleteInventoryItemAsync(int itemId) // Deletes an inventory item from the database
        {
            await using var conn = await GetOpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // Ensure the item exists
                var checkSql = "SELECT COUNT(*) FROM inventory WHERE itemID = @ItemID;";
                await using var checkCmd = new MySqlCommand(checkSql, conn, tx);
                checkCmd.Parameters.AddWithValue("@ItemID", itemId);
                var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                if (exists == 0)
                {
                    await tx.RollbackAsync();
                    return 0;
                }

                // Let FK cascades handle link cleanup; just delete the inventory row
                var sql = "DELETE FROM inventory WHERE itemID = @ItemID;";
                await using var cmd = new MySqlCommand(sql, conn, tx);
                cmd.Parameters.AddWithValue("@ItemID", itemId);
                var rows = await cmd.ExecuteNonQueryAsync();

                await tx.CommitAsync();
                InvalidateInventoryCache();
                return rows;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        private string GetDefaultUnitForCategory(string category) // Determines default unit of measurement based on category
        {
            return category?.ToLowerInvariant() switch
            {
                "fruit/soda" => "kg",
                "coffee" => "kg",
                "milktea" => "kg",
                "frappe" => "kg",
                "liquid" => "L",
                "supplies" => "pcs",
                _ => "pcs"
            };
        }

        // ===================== Sales Report =====================
        public async Task<int> SaveTransactionAsync(TransactionHistoryModel transaction) // Saves a transaction to the database
        {
            await using var conn = await GetOpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // Get current user ID with proper validation
                int userId = await GetValidUserIdAsync(conn, tx);
                
                // Insert into transactions table
                var transactionSql = "INSERT INTO transactions (userID, total, transactionDate, status, paymentMethod) VALUES (@UserID, @Total, @TransactionDate, @Status, @PaymentMethod);";
                await using var transactionCmd = new MySqlCommand(transactionSql, conn, (MySqlTransaction)tx);
                transactionCmd.Parameters.AddWithValue("@UserID", userId);
                transactionCmd.Parameters.AddWithValue("@Total", transaction.Total);
                transactionCmd.Parameters.AddWithValue("@TransactionDate", transaction.TransactionDate);
                transactionCmd.Parameters.AddWithValue("@Status", transaction.Status);
                transactionCmd.Parameters.AddWithValue("@PaymentMethod", transaction.PaymentMethod ?? "Cash");

                await transactionCmd.ExecuteNonQueryAsync();
                int transactionId = (int)transactionCmd.LastInsertedId;

                // Find product ID by name (safer than using hardcoded ID)
                int productId = await GetProductIdByNameAsync(transaction.DrinkName, conn, (MySqlTransaction)tx);
                
                // Insert into transaction_items table
                var itemSql = "INSERT INTO transaction_items (transactionID, productID, productName, quantity, price, smallPrice, mediumPrice, largePrice, addonPrice, addOns, size) VALUES (@TransactionID, @ProductID, @ProductName, @Quantity, @Price, @SmallPrice, @MediumPrice, @LargePrice, @AddonPrice, @AddOns, @Size);";
                await using var itemCmd = new MySqlCommand(itemSql, conn, (MySqlTransaction)tx);
                itemCmd.Parameters.AddWithValue("@TransactionID", transactionId);
                itemCmd.Parameters.AddWithValue("@ProductID", productId > 0 ? productId : (object)DBNull.Value);
                itemCmd.Parameters.AddWithValue("@ProductName", transaction.DrinkName);
                itemCmd.Parameters.AddWithValue("@Quantity", transaction.Quantity);
                itemCmd.Parameters.AddWithValue("@Price", transaction.Price);
                itemCmd.Parameters.AddWithValue("@SmallPrice", transaction.SmallPrice);
                itemCmd.Parameters.AddWithValue("@MediumPrice", transaction.MediumPrice);
                itemCmd.Parameters.AddWithValue("@LargePrice", transaction.LargePrice);
                itemCmd.Parameters.AddWithValue("@AddonPrice", transaction.AddonPrice);
                itemCmd.Parameters.AddWithValue("@AddOns", transaction.AddOns ?? "");
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

        private async Task<int> GetValidUserIdAsync(MySqlConnection conn, MySqlTransaction tx) // Gets a valid user ID for transactions
        {
            try
            {
                if (App.CurrentUser?.ID > 0)
                {
                    var checkSql = "SELECT id FROM users WHERE id = @UserId LIMIT 1;";
                    await using var checkCmd = new MySqlCommand(checkSql, conn, tx);
                    checkCmd.Parameters.AddWithValue("@UserId", App.CurrentUser.ID);
                    
                    var result = await checkCmd.ExecuteScalarAsync();
                    if (result != null)
                    {
                        return App.CurrentUser.ID;
                    }
                }
                var fallbackSql = "SELECT id FROM users WHERE status = 'approved' ORDER BY id ASC LIMIT 1;";
                await using var fallbackCmd = new MySqlCommand(fallbackSql, conn, tx);
                var fallbackResult = await fallbackCmd.ExecuteScalarAsync();
                
                if (fallbackResult != null)
                {
                    return Convert.ToInt32(fallbackResult);
                }

                return 0;
            }
            catch
            {
                return 1;
            }
        }

        private async Task EnsureDefaultUserExistsAsync(MySqlConnection conn) // Ensures at least one user exists in the database
        {
            try
            {
                // Check if any users exist
                var checkSql = "SELECT COUNT(*) FROM users;";
                await using var checkCmd = new MySqlCommand(checkSql, conn);
                var userCount = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                
            }
            catch
            {
                // Silently handle errors
            }
        }

        private async Task<int> GetProductIdByNameAsync(string productName, MySqlConnection conn, MySqlTransaction tx)
        {
            try
            {
                var sql = "SELECT productID FROM products WHERE productName = @ProductName LIMIT 1;";
                await using var cmd = new MySqlCommand(sql, conn, tx);
                cmd.Parameters.AddWithValue("@ProductName", productName);
                
                var result = await cmd.ExecuteScalarAsync();
                return result != null ? Convert.ToInt32(result) : 0;
            }
            catch
            {
                return 0;
            }
        }

        public async Task<List<TransactionHistoryModel>> GetTransactionsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = @"SELECT t.transactionID, t.total, t.transactionDate, t.status, t.paymentMethod,
                        ti.productName, ti.quantity, ti.price, ti.smallPrice, ti.mediumPrice, ti.largePrice, ti.addonPrice, ti.addOns, ti.size
                        FROM transactions t
                        LEFT JOIN transaction_items ti ON t.transactionID = ti.transactionID
                        WHERE t.transactionDate >= @StartDate AND t.transactionDate < @EndDate
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
                    Status = reader.GetString("status"),
                    PaymentMethod = reader.IsDBNull(reader.GetOrdinal("paymentMethod")) ? "Cash" : reader.GetString("paymentMethod")
                };

                if (!reader.IsDBNull(reader.GetOrdinal("productName")))
                {
                    transaction.DrinkName = reader.GetString("productName");
                    transaction.Quantity = reader.GetInt32("quantity");
                    transaction.Price = reader.GetDecimal("price");
                    
                    // Handle new price columns with fallback for existing data
                    try
                    {
                        transaction.SmallPrice = reader.IsDBNull(reader.GetOrdinal("smallPrice")) ? 0 : reader.GetDecimal("smallPrice");
                        transaction.MediumPrice = reader.IsDBNull(reader.GetOrdinal("mediumPrice")) ? 0 : reader.GetDecimal("mediumPrice");
                        transaction.LargePrice = reader.IsDBNull(reader.GetOrdinal("largePrice")) ? 0 : reader.GetDecimal("largePrice");
                        transaction.AddonPrice = reader.IsDBNull(reader.GetOrdinal("addonPrice")) ? 0 : reader.GetDecimal("addonPrice");
                        
                        // If all size prices are 0, try to distribute the total price based on size
                        if (transaction.SmallPrice == 0 && transaction.MediumPrice == 0 && transaction.LargePrice == 0)
                        {
                            var totalPrice = reader.GetDecimal("price");
                            var size = reader.IsDBNull(reader.GetOrdinal("size")) ? "" : reader.GetString("size");
                            var quantity = reader.GetInt32("quantity");
                            
                            // Calculate unit price first
                            var unitPrice = quantity > 0 ? totalPrice / quantity : totalPrice;
                            
                            if (size.Contains("Small") || size.Contains("S"))
                            {
                                transaction.SmallPrice = unitPrice;
                                transaction.MediumPrice = 0;
                                transaction.LargePrice = 0;
                            }
                            else if (size.Contains("Medium") || size.Contains("M"))
                            {
                                transaction.SmallPrice = 0;
                                transaction.MediumPrice = unitPrice;
                                transaction.LargePrice = 0;
                            }
                            else if (size.Contains("Large") || size.Contains("L"))
                            {
                                transaction.SmallPrice = 0;
                                transaction.MediumPrice = 0;
                                transaction.LargePrice = unitPrice;
                            }
                            else
                            {
                                // Default fallback - put all price in medium
                                transaction.SmallPrice = 0;
                                transaction.MediumPrice = unitPrice;
                                transaction.LargePrice = 0;
                            }
                        }
                    }
                    catch
                    {
                        // Fallback for old data - distribute the total price based on size
                        var totalPrice = reader.GetDecimal("price");
                        var size = reader.IsDBNull(reader.GetOrdinal("size")) ? "" : reader.GetString("size");
                        var quantity = reader.GetInt32("quantity");
                        
                        // For existing data, we'll distribute the price based on the size string
                        // Calculate unit price first
                        var unitPrice = quantity > 0 ? totalPrice / quantity : totalPrice;
                        
                        if (size.Contains("Small") || size.Contains("S"))
                        {
                            transaction.SmallPrice = unitPrice;
                            transaction.MediumPrice = 0;
                            transaction.LargePrice = 0;
                        }
                        else if (size.Contains("Medium") || size.Contains("M"))
                        {
                            transaction.SmallPrice = 0;
                            transaction.MediumPrice = unitPrice;
                            transaction.LargePrice = 0;
                        }
                        else if (size.Contains("Large") || size.Contains("L"))
                        {
                            transaction.SmallPrice = 0;
                            transaction.MediumPrice = 0;
                            transaction.LargePrice = unitPrice;
                        }
                        else
                        {
                            // Default fallback - put all price in medium
                            transaction.SmallPrice = 0;
                            transaction.MediumPrice = unitPrice;
                            transaction.LargePrice = 0;
                        }
                    }
                    
                    transaction.Size = reader.IsDBNull(reader.GetOrdinal("size")) ? "" : reader.GetString("size");
                    transaction.AddOns = reader.IsDBNull(reader.GetOrdinal("addOns")) ? "No add-ons" : reader.GetString("addOns");
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
                        WHERE t.transactionDate >= @StartDate AND t.transactionDate < @EndDate
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

        public async Task<int> GetNextTransactionIdAsync()
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = "SELECT COALESCE(MAX(transactionID), 0) + 1 as nextId FROM transactions";

            await using var cmd = new MySqlCommand(sql, conn);
            var result = await cmd.ExecuteScalarAsync();

            return result != null ? Convert.ToInt32(result) : 1;
        }

        // ===================== User Management =====================
        public async Task<UserInfoModel> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default) // Gets a user by email from the database
        {
            await using var conn = await GetOpenConnectionAsync(cancellationToken);

            var sql = "SELECT * FROM users WHERE email = @Email LIMIT 1;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Email", email);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
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
            
            var sql = "INSERT INTO users (firstName, lastName, email, password, phoneNumber, birthday, address, status, isAdmin, can_access_inventory, can_access_sales_report) " +
                      "VALUES (@FirstName,@LastName, @Email, @Password, @PhoneNumber, @Birthday, @Address, 'approved', @IsAdmin, @CanAccessInventory, @CanAccessSalesReport);";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@FirstName", user.FirstName);
            cmd.Parameters.AddWithValue("@LastName", user.LastName);
            cmd.Parameters.AddWithValue("@Email", user.Email);
            cmd.Parameters.AddWithValue("@Password", user.Password);
            cmd.Parameters.AddWithValue("@Birthday", user.Birthday.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@PhoneNumber", user.PhoneNumber);
            cmd.Parameters.AddWithValue("@Address", user.Address);
            cmd.Parameters.AddWithValue("@IsAdmin", isFirstUser);
            cmd.Parameters.AddWithValue("@CanAccessInventory", isFirstUser); // First user gets all permissions
            cmd.Parameters.AddWithValue("@CanAccessSalesReport", isFirstUser); // First user gets all permissions

            return await cmd.ExecuteNonQueryAsync();
        }
        public async Task<UserInfoModel> GetUserByIdAsync(int userId)
        {
            try
            {
                await using var conn = await GetOpenConnectionAsync();
                var sql = "SELECT * FROM users WHERE id = @UserId LIMIT 1;";
                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@UserId", userId);

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new UserInfoModel
                    {
                        ID = reader.GetInt32("id"),
                        Email = reader.GetString("email"),
                        FirstName = reader.GetString("firstName"),
                        LastName = reader.GetString("lastName"),
                        Password = reader.GetString("password"),
                        IsAdmin = reader.GetBoolean("isAdmin"),
                        Birthday = reader.GetDateTime("birthday"),
                        PhoneNumber = reader.GetString("phoneNumber"),
                        Address = reader.GetString("address"),
                        Status = reader.GetString("status"),
                        CanAccessInventory = reader.IsDBNull(reader.GetOrdinal("can_access_inventory")) ? false : reader.GetBoolean("can_access_inventory"),
                        CanAccessSalesReport = reader.IsDBNull(reader.GetOrdinal("can_access_sales_report")) ? false : reader.GetBoolean("can_access_sales_report"),
                        Username = reader.GetString("username") ?? string.Empty,
                        FullName = reader.GetString("fullName") ?? string.Empty,
                        ProfileImage = reader.GetString("profileImage") ?? "usericon.png"
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting user by ID: {ex.Message}");
                return null;
            }
        }

        public async Task<int> UpdateUserProfileAsync(int userId, string username, string email, string fullName, string phoneNumber, string profileImage, bool canAccessInventory, bool canAccessSalesReport)
        {
            try
            {
                await using var conn = await GetOpenConnectionAsync();
                
                // Parse fullName into firstName and lastName
                var nameParts = fullName?.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? new string[0];
                var firstName = nameParts.Length > 0 ? nameParts[0] : string.Empty;
                var lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : string.Empty;
                
                var sql = @"UPDATE users SET 
                           username = @Username, 
                           email = @Email, 
                           firstName = @FirstName,
                           lastName = @LastName,
                           phoneNumber = @PhoneNumber, 
                           profileImage = @ProfileImage,
                           can_access_inventory = @CanAccessInventory,
                           can_access_sales_report = @CanAccessSalesReport
                           WHERE id = @UserId;";
                
                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@FirstName", firstName);
                cmd.Parameters.AddWithValue("@LastName", lastName);
                cmd.Parameters.AddWithValue("@PhoneNumber", phoneNumber);
                cmd.Parameters.AddWithValue("@ProfileImage", profileImage);
                cmd.Parameters.AddWithValue("@CanAccessInventory", canAccessInventory);
                cmd.Parameters.AddWithValue("@CanAccessSalesReport", canAccessSalesReport);

                return await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating user profile: {ex.Message}");
                throw;
            }
        }

        public async Task<int> UpdateUserProfileImageAsync(int userId, string profileImage)
        {
            try
            {
                await using var conn = await GetOpenConnectionAsync();
                var sql = "UPDATE users SET profileImage = @ProfileImage WHERE id = @UserId;";
                
                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@ProfileImage", profileImage);

                return await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating user profile image: {ex.Message}");
                throw;
            }
        }

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

            // Generate 6-digit numeric reset code (store in reset_token)
            var rng = new Random();
            var resetToken = rng.Next(100000, 999999).ToString();
            var resetExpiry = DateTime.UtcNow.AddHours(1); // Token expires in 1 hour (using UTC)

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

            // Verify token and expiry (using UTC time)
            var sql = "SELECT * FROM users WHERE email = @Email AND reset_token = @ResetToken AND reset_expiry > UTC_TIMESTAMP() LIMIT 1;";
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

        // Helper method to handle foreign key constraint violations
        private bool IsForeignKeyConstraintViolation(Exception ex)
        {
            return ex.Message.Contains("foreign key constraint") || 
                   ex.Message.Contains("Cannot delete or update") ||
                   ex.Message.Contains("a foreign key constraint fails");
        }

        // Check if a product has transaction dependencies
        public async Task<bool> HasProductTransactionDependenciesAsync(int productId)
        {
            try
            {
                await using var conn = await GetOpenConnectionAsync();
                var sql = "SELECT COUNT(*) FROM transaction_items WHERE productID = @ProductID;";
                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ProductID", productId);
                
                var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                return count > 0;
            }
            catch
            {
                return false;
            }
        }

        // Check if an inventory item has product dependencies
        public async Task<bool> HasInventoryProductDependenciesAsync(int itemId)
        {
            try
            {
                await using var conn = await GetOpenConnectionAsync();
                var sql = @"
                    SELECT COUNT(*) FROM (
                        SELECT 1 FROM product_ingredients WHERE itemID = @ItemID
                        UNION ALL
                        SELECT 1 FROM product_addons WHERE itemID = @ItemID
                    ) as dependencies;";
                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ItemID", itemId);

                var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                return count > 0;
            }
            catch
            {
                return false;
            }
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
            // Placeholder for future user management features
            await Task.CompletedTask;
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

        /// <summary>
        /// Creates a purchase order for low stock items
        /// </summary>
        public async Task<int> CreatePurchaseOrderAsync(List<InventoryPageModel> lowStockItems)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🛒 Creating purchase order with {lowStockItems.Count} items");
                
                await using var conn = await GetOpenConnectionAsync();
                
                // First, ensure the purchase order tables exist
                await EnsurePurchaseOrderTablesExistAsync(conn);
                
                // Create purchase order
                var insertOrderSql = @"
                    INSERT INTO purchase_orders (orderDate, supplierName, status, requestedBy, totalAmount, createdAt)
                    VALUES (@orderDate, @supplierName, @status, @requestedBy, @totalAmount, @createdAt);
                    SELECT LAST_INSERT_ID();";
                
                var currentUser = App.CurrentUser?.Email ?? "Unknown";
                var totalAmount = lowStockItems.Sum(item => (decimal)(item.minimumQuantity - item.itemQuantity) * 10.0m); // Assuming $10 per unit
                
                System.Diagnostics.Debug.WriteLine($"💰 Total amount: {totalAmount:C}");
                System.Diagnostics.Debug.WriteLine($"👤 Current user: {currentUser}");
                
                await using var orderCmd = new MySqlCommand(insertOrderSql, conn);
                AddParameters(orderCmd, new Dictionary<string, object?>
                {
                    ["@orderDate"] = DateTime.Now,
                    ["@supplierName"] = "Coftea Supplier",
                    ["@status"] = "Pending",
                    ["@requestedBy"] = currentUser,
                    ["@totalAmount"] = totalAmount,
                    ["@createdAt"] = DateTime.Now
                });
                
                var orderId = Convert.ToInt32(await orderCmd.ExecuteScalarAsync());
                System.Diagnostics.Debug.WriteLine($"✅ Purchase order created with ID: {orderId}");
                
                // Create purchase order items
                foreach (var item in lowStockItems)
                {
                    var requestedQuantity = (int)(item.minimumQuantity - item.itemQuantity);
                    var unitPrice = 10.0m; // Default unit price
                    var totalPrice = (decimal)requestedQuantity * unitPrice;
                    
                    System.Diagnostics.Debug.WriteLine($"📦 Adding item: {item.itemName} - Qty: {requestedQuantity}, Price: {totalPrice:C}");
                    
                    var insertItemSql = @"
                        INSERT INTO purchase_order_items (purchaseOrderId, inventoryItemId, itemName, itemCategory, 
                                                        requestedQuantity, unitPrice, totalPrice, unitOfMeasurement)
                        VALUES (@purchaseOrderId, @inventoryItemId, @itemName, @itemCategory, 
                                @requestedQuantity, @unitPrice, @totalPrice, @unitOfMeasurement);";
                    
                    await using var itemCmd = new MySqlCommand(insertItemSql, conn);
                    AddParameters(itemCmd, new Dictionary<string, object?>
                    {
                        ["@purchaseOrderId"] = orderId,
                        ["@inventoryItemId"] = item.itemID,
                        ["@itemName"] = item.itemName,
                        ["@itemCategory"] = item.itemCategory,
                        ["@requestedQuantity"] = requestedQuantity,
                        ["@unitPrice"] = unitPrice,
                        ["@totalPrice"] = totalPrice,
                        ["@unitOfMeasurement"] = item.unitOfMeasurement
                    });
                    
                    await itemCmd.ExecuteNonQueryAsync();
                    System.Diagnostics.Debug.WriteLine($"✅ Item added to purchase order: {item.itemName}");
                }
                
                System.Diagnostics.Debug.WriteLine($"✅ Purchase order {orderId} created with {lowStockItems.Count} items");
                return orderId;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error creating purchase order: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Inner exception: {ex.InnerException.Message}");
                }
                return 0;
            }
        }

        /// <summary>
        /// Gets all pending purchase orders for admin approval
        /// </summary>
        public async Task<List<PurchaseOrderModel>> GetPendingPurchaseOrdersAsync()
        {
            try
            {
                await using var conn = await GetOpenConnectionAsync();
                
                var sql = @"
                    SELECT po.*, poi.*
                    FROM purchase_orders po
                    LEFT JOIN purchase_order_items poi ON po.purchaseOrderId = poi.purchaseOrderId
                    WHERE po.status = 'Pending'
                    ORDER BY po.createdAt DESC;";
                
                await using var cmd = new MySqlCommand(sql, conn);
                await using var reader = await cmd.ExecuteReaderAsync();
                
                var orders = new List<PurchaseOrderModel>();
                var currentOrder = (PurchaseOrderModel)null;
                
                while (await reader.ReadAsync())
                {
                    if (currentOrder == null || currentOrder.PurchaseOrderId != reader.GetInt32("purchaseOrderId"))
                    {
                        if (currentOrder != null)
                            orders.Add(currentOrder);
                            
                        currentOrder = new PurchaseOrderModel
                        {
                            PurchaseOrderId = reader.GetInt32("purchaseOrderId"),
                            OrderDate = reader.GetDateTime("orderDate"),
                            SupplierName = reader.GetString("supplierName"),
                            Status = reader.GetString("status"),
                            RequestedBy = reader.GetString("requestedBy"),
                            TotalAmount = reader.GetDecimal("totalAmount"),
                            CreatedAt = reader.GetDateTime("createdAt")
                        };
                    }
                }
                
                if (currentOrder != null)
                    orders.Add(currentOrder);
                
                return orders;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error getting pending purchase orders: {ex.Message}");
                return new List<PurchaseOrderModel>();
            }
        }

        /// <summary>
        /// Approves or rejects a purchase order
        /// </summary>
        public async Task<bool> UpdatePurchaseOrderStatusAsync(int purchaseOrderId, string status, string approvedBy)
        {
            try
            {
                await using var conn = await GetOpenConnectionAsync();
                
                var sql = @"
                    UPDATE purchase_orders 
                    SET status = @status, approvedBy = @approvedBy, approvedDate = @approvedDate, updatedAt = @updatedAt
                    WHERE purchaseOrderId = @purchaseOrderId;";
                
                await using var cmd = new MySqlCommand(sql, conn);
                AddParameters(cmd, new Dictionary<string, object?>
                {
                    ["@status"] = status,
                    ["@approvedBy"] = approvedBy,
                    ["@approvedDate"] = DateTime.Now,
                    ["@updatedAt"] = DateTime.Now,
                    ["@purchaseOrderId"] = purchaseOrderId
                });
                
                var rowsAffected = await cmd.ExecuteNonQueryAsync();
                
                if (status == "Approved")
                {
                    // Update inventory quantities
                    await UpdateInventoryFromPurchaseOrderAsync(purchaseOrderId);
                }
                
                System.Diagnostics.Debug.WriteLine($"✅ Purchase order {purchaseOrderId} status updated to {status}");
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error updating purchase order status: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Updates inventory quantities when purchase order is approved
        /// </summary>
        private async Task UpdateInventoryFromPurchaseOrderAsync(int purchaseOrderId)
        {
            try
            {
                await using var conn = await GetOpenConnectionAsync();
                
                var sql = @"
                    UPDATE inventory i
                    INNER JOIN purchase_order_items poi ON i.itemID = poi.inventoryItemId
                    SET i.itemQuantity = i.itemQuantity + poi.requestedQuantity,
                        i.updatedAt = @updatedAt
                    WHERE poi.purchaseOrderId = @purchaseOrderId;";
                
                await using var cmd = new MySqlCommand(sql, conn);
                AddParameters(cmd, new Dictionary<string, object?>
                {
                    ["@updatedAt"] = DateTime.Now,
                    ["@purchaseOrderId"] = purchaseOrderId
                });
                
                await cmd.ExecuteNonQueryAsync();
                System.Diagnostics.Debug.WriteLine($"✅ Inventory updated for purchase order {purchaseOrderId}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error updating inventory from purchase order: {ex.Message}");
            }
        }

        /// <summary>
        /// Ensures purchase order tables exist in the database
        /// </summary>
        private async Task EnsurePurchaseOrderTablesExistAsync(MySqlConnection conn)
        {
            try
            {
                var createTablesSql = @"
                    CREATE TABLE IF NOT EXISTS purchase_orders (
                        purchaseOrderId INT AUTO_INCREMENT PRIMARY KEY,
                        orderDate DATETIME NOT NULL,
                        supplierName VARCHAR(255) NOT NULL,
                        status VARCHAR(50) DEFAULT 'Pending',
                        requestedBy VARCHAR(255) NOT NULL,
                        approvedBy VARCHAR(255) DEFAULT NULL,
                        approvedDate DATETIME DEFAULT NULL,
                        notes TEXT,
                        totalAmount DECIMAL(10,2) DEFAULT 0.00,
                        createdAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        updatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
                    );
                    
                    CREATE TABLE IF NOT EXISTS purchase_order_items (
                        purchaseOrderItemId INT AUTO_INCREMENT PRIMARY KEY,
                        purchaseOrderId INT NOT NULL,
                        inventoryItemId INT NOT NULL,
                        itemName VARCHAR(255) NOT NULL,
                        itemCategory VARCHAR(100),
                        requestedQuantity INT NOT NULL,
                        approvedQuantity INT DEFAULT 0,
                        unitPrice DECIMAL(10,2) NOT NULL,
                        totalPrice DECIMAL(10,2) NOT NULL,
                        unitOfMeasurement VARCHAR(50),
                        notes TEXT,
                        FOREIGN KEY (purchaseOrderId) REFERENCES purchase_orders(purchaseOrderId) ON DELETE CASCADE,
                        FOREIGN KEY (inventoryItemId) REFERENCES inventory(itemID) ON DELETE CASCADE
                    );";
                
                await using var cmd = new MySqlCommand(createTablesSql, conn);
                await cmd.ExecuteNonQueryAsync();
                
                System.Diagnostics.Debug.WriteLine("✅ Purchase order tables ensured to exist");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error ensuring purchase order tables exist: {ex.Message}");
                throw;
            }
        }
    }
}
