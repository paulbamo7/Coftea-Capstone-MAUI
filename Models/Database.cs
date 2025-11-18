using Coftea_Capstone.Models;
using MySqlConnector;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using System.IO;
using System.Threading;

using Coftea_Capstone.C_;
using Coftea_Capstone.Services;
using Coftea_Capstone.Models.Service;

namespace Coftea_Capstone.Models
{
    public class Database
    {
        private readonly string _db;
        // In-memory caches to reduce repeated DB calls during UI updates
        // Thread-safe caches for concurrent access
        private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);
        private static readonly object _productsCacheLock = new object();
        private static (DateTime ts, List<POSPageModel> items)? _productsCache;
        private static readonly object _inventoryCacheLock = new object();
        private static (DateTime ts, List<InventoryPageModel> items)? _inventoryCache;
        private static readonly ConcurrentDictionary<int, (DateTime ts, List<(InventoryPageModel item, double amount, string unit, string role)> items)> _productIngredientsCache = new();
        private static readonly ConcurrentDictionary<int, (DateTime ts, List<InventoryPageModel> items)> _productAddonsCache = new();

        // Public properties to expose connection details
        public string Host { get; private set; }
        public int Port { get; private set; }
        public string ConnectionString => _db;

        public Database(string host = null,
                        string database = "coftea_db",
                        string user = "root",
                        string password = "")
        {
            // Allow manual override for testing (useful for emulator debugging)
            var server = host ?? GetDefaultHostForPlatform();
            Host = server;
            Port = 3306;
            System.Diagnostics.Debug.WriteLine($"🔗 Database connecting to server: {server}");
            
            _db = new MySqlConnectionStringBuilder // Configuration for connecting to MySQL XAMPP server database
            {
                Server = server,
                Port = 3306,
                Database = "coftea_db",
                UserID = "root",
                Password = "",
                SslMode = MySqlSslMode.None,
                // Connection pool settings for stress testing
                MaximumPoolSize = 100, // Maximum connections in pool
                MinimumPoolSize = 5,  // Minimum connections to maintain
                ConnectionTimeout = 30, // Connection timeout in seconds
                DefaultCommandTimeout = 30, // Command timeout in seconds
                ConnectionReset = true, // Reset connection state when retrieved from pool
                Pooling = true // Enable connection pooling
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
            try
            {
                // Use the configured connection string directly
                var conn = new MySqlConnection(_db);
                await conn.OpenAsync(cancellationToken);
                return conn;
            }
            catch (MySqlException ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ MySQL connection error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Error code: {ex.Number}, SQL state: {ex.SqlState}");
                throw new Exception($"Database connection failed: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Database connection error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private static string GetDefaultHostForPlatform() // Detects which platform the app is running on
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔍 GetDefaultHostForPlatform called for platform: {DeviceInfo.Platform}");
                
                // Use automatic IP detection if available and enabled
                if (NetworkConfigurationService.IsAutomaticDetectionEnabled())
                {
                    var autoDetectedIP = NetworkConfigurationService.GetAutoDetectedIP();
                    if (!string.IsNullOrEmpty(autoDetectedIP) && autoDetectedIP != "localhost")
                    {
                        System.Diagnostics.Debug.WriteLine($"🤖 Using cached auto-detected IP: {autoDetectedIP}");
                        return autoDetectedIP;
                    }
                }

                // Check if we're likely in an emulator environment
                bool isLikelyEmulator = IsLikelyEmulatorEnvironment();
                System.Diagnostics.Debug.WriteLine($"🔍 Is likely emulator: {isLikelyEmulator}");

                // Try automatic IP detection with timeout
                string detectedIP = null;
                if (!isLikelyEmulator)
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine("🔍 Attempting automatic IP detection...");
                        var task = AutomaticIPDetectionService.DetectDatabaseIPAsync();
                        if (task.Wait(TimeSpan.FromSeconds(5))) // 5 second timeout for real devices
                        {
                            detectedIP = task.Result;
                            System.Diagnostics.Debug.WriteLine($"✅ Auto-detected IP: {detectedIP}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("⏰ IP detection timed out");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ IP detection failed: {ex.Message}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("🔍 Skipping automatic IP detection for emulator environment");
                }

                // Use detected IP if available and not localhost
                if (!string.IsNullOrEmpty(detectedIP) && detectedIP != "localhost")
                {
                    return detectedIP;
                }

                // Platform-specific fallbacks
                if (DeviceInfo.Platform == DevicePlatform.Android)
                {
                    // Android emulator typically uses 10.0.2.2 to access host machine
                    var androidIPs = isLikelyEmulator ? 
                        new[] { "10.0.2.2", "192.168.1.6", "localhost" } : 
                        new[] { "192.168.1.2", "10.0.2.2", "localhost" };
                    
                    // For Android emulator, use 10.0.2.2 which maps to host PC's localhost
                    // This is the standard Android emulator IP for accessing host machine
                    var selectedIP = isLikelyEmulator ? "10.0.2.2" : androidIPs[0];
                    System.Diagnostics.Debug.WriteLine($"🤖 Android emulator detected - using IP: {selectedIP}");
                    System.Diagnostics.Debug.WriteLine($"💡 If connection fails, check:");
                    System.Diagnostics.Debug.WriteLine($"   1. MySQL bind-address in my.ini (should be 0.0.0.0 or commented out)");
                    System.Diagnostics.Debug.WriteLine($"   2. Windows Firewall allows port 3306");
                    System.Diagnostics.Debug.WriteLine($"   3. MySQL user has network permissions (GRANT ALL ON *.* TO 'root'@'%')");
                    return selectedIP;
                }

                if (DeviceInfo.Platform == DevicePlatform.iOS)
                {
                    // iOS simulator typically uses localhost or host machine IP
                    var iosIPs = isLikelyEmulator ? 
                        new[] { "localhost", "192.168.1.6" } : 
                        new[] { "192.168.1.2", "localhost" };
                    
                    foreach (var ip in iosIPs)
                    {
                        System.Diagnostics.Debug.WriteLine($"🤖 iOS trying IP: {ip}");
                        return ip;
                    }
                }

                if (DeviceInfo.Platform == DevicePlatform.WinUI || DeviceInfo.Platform == DevicePlatform.macOS || DeviceInfo.Platform == DevicePlatform.MacCatalyst)
                {
                    System.Diagnostics.Debug.WriteLine($"🤖 Desktop platform using localhost");
                    return "localhost";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error in GetDefaultHostForPlatform: {ex.Message}");
            }

            // Final fallback
            System.Diagnostics.Debug.WriteLine($"🤖 Final fallback: localhost");
            return "localhost";
        }

        private static bool IsLikelyEmulatorEnvironment()
        {
            try
            {
                // Check for common emulator indicators
                if (DeviceInfo.Platform == DevicePlatform.Android)
                {
                    // Android emulator indicators
                    var model = DeviceInfo.Model?.ToLower() ?? "";
                    var manufacturer = DeviceInfo.Manufacturer?.ToLower() ?? "";
                    
                    return model.Contains("emulator") || 
                           model.Contains("google_sdk") || 
                           model.Contains("sdk") ||
                           manufacturer.Contains("genymotion") ||
                           model.Contains("android sdk");
                }
                
                if (DeviceInfo.Platform == DevicePlatform.iOS)
                {
                    // iOS simulator indicators
                    var model = DeviceInfo.Model?.ToLower() ?? "";
                    return model.Contains("simulator") || 
                           model.Contains("iphone simulator") ||
                           model.Contains("ipad simulator");
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

		// Cache helpers
		private static bool IsFresh(DateTime ts) => (DateTime.UtcNow - ts) < CacheDuration;
		private static void InvalidateProductsCache()
		{
			lock (_productsCacheLock)
			{
				_productsCache = null;
			}
		}
		public void InvalidateInventoryCache()
		{
			lock (_inventoryCacheLock)
			{
				_inventoryCache = null;
			}
		}
		private static void InvalidateProductLinksCache(int productId)
		{
			_productIngredientsCache.TryRemove(productId, out _);
			_productAddonsCache.TryRemove(productId, out _);
		}

        // Database initialization
        public async Task InitializeDatabaseAsync(CancellationToken cancellationToken = default)
        {
            await using var conn = await GetOpenConnectionAsync(cancellationToken);

            // Add imageData column to existing tables if it doesn't exist (for migration)
            try
            {
                // Check if column exists first (MySQL doesn't support IF NOT EXISTS for ALTER TABLE)
                var checkProductsSql = @"SELECT COUNT(*) FROM information_schema.COLUMNS 
                                         WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'products' AND COLUMN_NAME = 'imageData';";
                await using var checkProductsCmd = new MySqlCommand(checkProductsSql, conn);
                var productsHasColumn = Convert.ToInt32(await checkProductsCmd.ExecuteScalarAsync()) > 0;
                
                if (!productsHasColumn)
                {
                    var alterProductsSql = "ALTER TABLE products ADD COLUMN imageData LONGBLOB;";
                    await using var alterProductsCmd = new MySqlCommand(alterProductsSql, conn);
                    await alterProductsCmd.ExecuteNonQueryAsync();
                }

                var checkInventorySql = @"SELECT COUNT(*) FROM information_schema.COLUMNS 
                                          WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'inventory' AND COLUMN_NAME = 'imageData';";
                await using var checkInventoryCmd = new MySqlCommand(checkInventorySql, conn);
                var inventoryHasColumn = Convert.ToInt32(await checkInventoryCmd.ExecuteScalarAsync()) > 0;
                
                if (!inventoryHasColumn)
                {
                    var alterInventorySql = "ALTER TABLE inventory ADD COLUMN imageData LONGBLOB;";
                    await using var alterInventoryCmd = new MySqlCommand(alterInventorySql, conn);
                    await alterInventoryCmd.ExecuteNonQueryAsync();
                }

                // Add imageData column to users table if it doesn't exist (for migration)
                var checkUsersSql = @"SELECT COUNT(*) FROM information_schema.COLUMNS 
                                      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'users' AND COLUMN_NAME = 'imageData';";
                await using var checkUsersCmd = new MySqlCommand(checkUsersSql, conn);
                var usersHasColumn = Convert.ToInt32(await checkUsersCmd.ExecuteScalarAsync()) > 0;
                
                if (!usersHasColumn)
                {
                    var alterUsersSql = "ALTER TABLE users ADD COLUMN imageData LONGBLOB;";
                    await using var alterUsersCmd = new MySqlCommand(alterUsersSql, conn);
                    await alterUsersCmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error checking/adding imageData columns: {ex.Message}");
            }

            // Add last_login column to users table if it doesn't exist
            try
            {
                var checkLastLoginColumnSql = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'users' AND COLUMN_NAME = 'last_login';";
                await using var checkLastLoginColumnCmd = new MySqlCommand(checkLastLoginColumnSql, conn);
                var lastLoginColumnExists = await checkLastLoginColumnCmd.ExecuteScalarAsync() != null;
                
                if (!lastLoginColumnExists)
                {
                    var addLastLoginColumnSql = "ALTER TABLE users ADD COLUMN last_login DATETIME NULL;";
                    await using var addLastLoginColumnCmd = new MySqlCommand(addLastLoginColumnSql, conn);
                    await addLastLoginColumnCmd.ExecuteNonQueryAsync();
                    System.Diagnostics.Debug.WriteLine("✅ Added last_login column to users table");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error checking/adding last_login column: {ex.Message}");
            }

            // Create tables if they don't exist
            var createTablesSql = @"
                CREATE TABLE IF NOT EXISTS users (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    firstName VARCHAR(100),
                    lastName VARCHAR(100),
                    email VARCHAR(255) UNIQUE NOT NULL,
                    password VARCHAR(255) NOT NULL,
                    status VARCHAR(50) DEFAULT 'approved',
                    isAdmin BOOLEAN DEFAULT FALSE,
                    can_access_inventory BOOLEAN DEFAULT TRUE,
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
                    smallPrice DECIMAL(10,2) DEFAULT NULL,
                    mediumPrice DECIMAL(10,2) NOT NULL DEFAULT 0,
                    largePrice DECIMAL(10,2) NOT NULL,
                    category VARCHAR(100),
                    subcategory VARCHAR(100),
                    imageSet VARCHAR(255),
                    imageData LONGBLOB,
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
                    imageData LONGBLOB,
                    itemDescription TEXT,
                    unitOfMeasurement VARCHAR(50),
                    minimumQuantity DECIMAL(10,2) DEFAULT 0,
                    maximumQuantity DECIMAL(10,2) DEFAULT 0,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                );
                
                CREATE TABLE IF NOT EXISTS inventory_activity_log (
                    logId INT AUTO_INCREMENT PRIMARY KEY,
                    itemId INT NOT NULL,
                    itemName VARCHAR(255) NOT NULL,
                    itemCategory VARCHAR(100),
                    action VARCHAR(50) NOT NULL,
                    quantityChanged DECIMAL(10,2) NOT NULL,
                    previousQuantity DECIMAL(10,2) NOT NULL,
                    newQuantity DECIMAL(10,2) NOT NULL,
                    unitOfMeasurement VARCHAR(50),
                    reason VARCHAR(100),
                    userEmail VARCHAR(255),
                    userFullName VARCHAR(255),
                    userId INT,
                    changedBy VARCHAR(50) DEFAULT 'USER',
                    cost DECIMAL(10,2) DEFAULT NULL,
                    orderId VARCHAR(100),
                    productName VARCHAR(255) DEFAULT NULL COMMENT 'POS product name that used this ingredient',
                    productSize VARCHAR(50) DEFAULT NULL COMMENT 'Product size (Small, Medium, Large)',
                    timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                    notes TEXT,
                    FOREIGN KEY (itemId) REFERENCES inventory(itemID) ON DELETE CASCADE,
                    FOREIGN KEY (userId) REFERENCES users(id) ON DELETE SET NULL,
                    INDEX idx_itemId (itemId),
                    INDEX idx_timestamp (timestamp),
                    INDEX idx_action (action),
                    INDEX idx_userEmail (userEmail),
                    INDEX idx_orderId (orderId),
                    INDEX idx_productName (productName),
                    INDEX idx_productSize (productSize)
                );
                
                -- Add productSize column if it doesn't exist (for existing databases)
                ALTER TABLE inventory_activity_log 
                    ADD COLUMN IF NOT EXISTS productSize VARCHAR(50) DEFAULT NULL COMMENT 'Product size (Small, Medium, Large)';
                
                CREATE TABLE IF NOT EXISTS product_ingredients (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    productID INT NOT NULL,
                    itemID INT NOT NULL,
                    amount_small  DECIMAL(10,4) NOT NULL DEFAULT 0,
                    unit_small    VARCHAR(50) NOT NULL DEFAULT 'pcs',
                    amount_medium DECIMAL(10,4) NOT NULL DEFAULT 0,
                    unit_medium   VARCHAR(50) NOT NULL DEFAULT 'pcs',
                    amount_large  DECIMAL(10,4) NOT NULL DEFAULT 0,
                    unit_large    VARCHAR(50) NOT NULL DEFAULT 'pcs',
                    role VARCHAR(50) DEFAULT 'ingredient',
                    FOREIGN KEY (productID) REFERENCES products(productID) ON DELETE CASCADE,
                    FOREIGN KEY (itemID) REFERENCES inventory(itemID) ON DELETE CASCADE,
                    INDEX idx_productID (productID),
                    INDEX idx_itemID (itemID)
                );
                
                -- Fix existing NULL or empty values in unit columns
                UPDATE product_ingredients SET unit_small = 'pcs' WHERE unit_small IS NULL OR unit_small = '';
                UPDATE product_ingredients SET unit_medium = 'pcs' WHERE unit_medium IS NULL OR unit_medium = '';
                UPDATE product_ingredients SET unit_large = 'pcs' WHERE unit_large IS NULL OR unit_large = '';
    
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
                    size VARCHAR(100),
                    CONSTRAINT fk_tx_items_tx FOREIGN KEY (transactionID) REFERENCES transactions(transactionID) ON DELETE CASCADE,
                    CONSTRAINT fk_tx_items_product FOREIGN KEY (productID) REFERENCES products(productID) ON DELETE SET NULL ON UPDATE CASCADE
                );
                
                CREATE TABLE IF NOT EXISTS processing_queue (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    productID INT NOT NULL,
                    productName VARCHAR(255) NOT NULL,
                    size VARCHAR(50) NOT NULL,
                    sizeDisplay VARCHAR(50),
                    orderGroupId VARCHAR(64),
                    quantity INT NOT NULL,
                    unitPrice DECIMAL(10,2) NOT NULL,
                    addonPrice DECIMAL(10,2) DEFAULT 0.00,
                    ingredients JSON,
                    addons JSON,
                    createdAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (productID) REFERENCES products(productID) ON DELETE CASCADE,
                    INDEX idx_productID (productID),
                    INDEX idx_createdAt (createdAt)
                );

                ALTER TABLE processing_queue 
                    ADD COLUMN IF NOT EXISTS orderGroupId VARCHAR(64);
                
                CREATE TABLE IF NOT EXISTS pending_registrations (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    email VARCHAR(255) NOT NULL,
                    password VARCHAR(255) NOT NULL,
                    firstName VARCHAR(100),
                    lastName VARCHAR(100),
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
            
            // Add quantity column to purchase_order_items if it doesn't exist (migration)
            try
            {
                var checkQuantityColumnSql = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'purchase_order_items' AND COLUMN_NAME = 'quantity';";
                await using var checkQuantityColumnCmd = new MySqlCommand(checkQuantityColumnSql, conn);
                var quantityColumnExists = await checkQuantityColumnCmd.ExecuteScalarAsync() != null;
                
                if (!quantityColumnExists)
                {
                    var addQuantityColumnSql = "ALTER TABLE purchase_order_items ADD COLUMN quantity INT DEFAULT 1;";
                    await using var addQuantityColumnCmd = new MySqlCommand(addQuantityColumnSql, conn);
                    await addQuantityColumnCmd.ExecuteNonQueryAsync();
                    System.Diagnostics.Debug.WriteLine("✅ Added quantity column to purchase_order_items table");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error checking/adding quantity column: {ex.Message}");
            }
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
			var products = await QueryAsync(sql, reader => new POSPageModel
			{
				ProductID = reader.GetInt32("productID"),
				ProductName = reader.GetString("productName"),
				SmallPrice = reader.IsDBNull(reader.GetOrdinal("smallPrice")) ? null : reader.GetDecimal("smallPrice"),
				MediumPrice = reader.IsDBNull(reader.GetOrdinal("mediumPrice")) ? 0 : reader.GetDecimal("mediumPrice"),
				LargePrice = reader.GetDecimal("largePrice"),
				ImageSet = reader.IsDBNull(reader.GetOrdinal("imageSet")) ? "" : reader.GetString("imageSet"),
				Category = reader.IsDBNull(reader.GetOrdinal("category")) ? null : reader.GetString("category"),
				Subcategory = reader.IsDBNull(reader.GetOrdinal("subcategory")) ? null : reader.GetString("subcategory"),
				ProductDescription = reader.IsDBNull(reader.GetOrdinal("description")) ? "" : reader.GetString("description"),
				ColorCode = reader.IsDBNull(reader.GetOrdinal("colorCode")) ? "" : reader.GetString("colorCode")
			});

			// Restore images from database for products where app data file is missing (background task)
			_ = Task.Run(async () =>
			{
				try
				{
					foreach (var product in products)
					{
						if (!string.IsNullOrWhiteSpace(product.ImageSet))
						{
							var imagePath = Services.ImagePersistenceService.GetImagePath(product.ImageSet);
							if (!File.Exists(imagePath))
							{
								await GetProductImageAsync(product.ProductID);
							}
						}
					}
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"⚠️ Error in background image restoration task: {ex.Message}");
				}
			});

			return products;
		}
		public async Task<List<POSPageModel>> GetProductsAsyncCached() // Gets all products with caching
        {
			lock (_productsCacheLock)
			{
				if (_productsCache.HasValue && IsFresh(_productsCache.Value.ts))
					return _productsCache.Value.items;
			}

			var items = await GetProductsAsync();
			lock (_productsCacheLock)
			{
				_productsCache = (DateTime.UtcNow, items);
			}
			return items;
		}

        // Get all ingredients and addons (linked inventory items) for a product
        public async Task<List<(InventoryPageModel item, double amount, string unit, string role)>> GetProductIngredientsAsync(int productId)
        {
            await using var conn = await GetOpenConnectionAsync();

            const string sql = @"SELECT pi.amount_small, pi.unit_small, pi.role,
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

                // Per-size amounts/units
                item.InputAmountSmall = reader.IsDBNull(reader.GetOrdinal("amount_small")) ? 0d : reader.GetDouble("amount_small");
                item.InputAmountMedium = reader.IsDBNull(reader.GetOrdinal("amount_medium")) ? 0d : reader.GetDouble("amount_medium");
                item.InputAmountLarge = reader.IsDBNull(reader.GetOrdinal("amount_large")) ? 0d : reader.GetDouble("amount_large");

                // Handle NULL values and ensure defaults - use 'pcs' if NULL or empty
                var unitSmall = reader.IsDBNull(reader.GetOrdinal("unit_small")) ? null : reader.GetString("unit_small");
                var unitMedium = reader.IsDBNull(reader.GetOrdinal("unit_medium")) ? null : reader.GetString("unit_medium");
                var unitLarge = reader.IsDBNull(reader.GetOrdinal("unit_large")) ? null : reader.GetString("unit_large");
                
                item.InputUnitSmall = string.IsNullOrWhiteSpace(unitSmall) ? (item.unitOfMeasurement ?? "pcs") : unitSmall;
                item.InputUnitMedium = string.IsNullOrWhiteSpace(unitMedium) ? (item.unitOfMeasurement ?? "pcs") : unitMedium;
                item.InputUnitLarge = string.IsNullOrWhiteSpace(unitLarge) ? (item.unitOfMeasurement ?? "pcs") : unitLarge;

                // Initialize the InputUnit based on the current selected size
                // Pass true for edit mode since we're loading saved data from database
                item.InitializeInputUnit(isEditMode: true);

                // If you later add cost computation, populate PriceUsed* here
                item.PriceUsedSmall = 0;
                item.PriceUsedMedium = 0;
                item.PriceUsedLarge = 0;

                // Use small size as the default amount/unit for the return tuple
                var amount = item.InputAmountSmall;
                var unit = item.InputUnitSmall;
                var role = reader.IsDBNull(reader.GetOrdinal("role")) ? "ingredient" : reader.GetString("role");

                results.Add((item, amount, unit, role));
            }

            return results;
        }

        // Validate and fix product-ingredient connections to ensure consistent deduction behavior
        public async Task<bool> ValidateAndFixProductIngredientsAsync(int productId)
        {
            try
            {
                await using var conn = await GetOpenConnectionAsync();
                
                // Get current ingredient connections
                var ingredients = await GetProductIngredientsAsync(productId);
                
                if (!ingredients.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Product {productId} has no ingredients - this is normal for products without inventory requirements");
                    return true;
                }
                
                bool needsUpdate = false;
                var updateCommands = new List<string>();
                
                foreach (var (ingredient, amount, unit, role) in ingredients)
                {
                    // Check if per-size amounts are missing (0 or NULL) but shared amount exists
                    if (amount > 0 && ingredient.InputAmountSmall == 0 && ingredient.InputAmountMedium == 0 && ingredient.InputAmountLarge == 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"🔧 Product {productId}: Ingredient {ingredient.itemName} has shared amount {amount} but no per-size amounts - fixing");
                        
                        // Update per-size amounts to match shared amount
                        var updateSql = @"
                            UPDATE product_ingredients 
                            SET amount_small = @Amount, 
                                amount_medium = @Amount, 
                                amount_large = @Amount,
                                unit_small = @Unit,
                                unit_medium = @Unit,
                                unit_large = @Unit
                            WHERE productID = @ProductID AND itemID = @ItemID";
                        
                        await using var updateCmd = new MySqlCommand(updateSql, conn);
                        updateCmd.Parameters.AddWithValue("@Amount", amount);
                        // Ensure unit is never null or empty - use 'pcs' as fallback for NOT NULL columns
                        var unitValue = string.IsNullOrWhiteSpace(unit) ? "pcs" : unit;
                        updateCmd.Parameters.AddWithValue("@Unit", unitValue);
                        updateCmd.Parameters.AddWithValue("@ProductID", productId);
                        updateCmd.Parameters.AddWithValue("@ItemID", ingredient.itemID);
                        
                        await updateCmd.ExecuteNonQueryAsync();
                        needsUpdate = true;
                        
                        System.Diagnostics.Debug.WriteLine($"✅ Fixed per-size amounts for {ingredient.itemName}: {amount} {unit}");
                    }
                }
                
                if (needsUpdate)
                {
                    InvalidateProductLinksCache(productId);
                    System.Diagnostics.Debug.WriteLine($"✅ Product {productId} ingredient connections validated and fixed");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error validating product ingredients for {productId}: {ex.Message}");
                return false;
            }
        }

        // Validate and fix all product-ingredient connections
        public async Task<bool> ValidateAllProductIngredientsAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔧 Starting validation of all product-ingredient connections...");
                
                // Get all products
                var products = await GetProductsAsyncCached();
                int fixedCount = 0;
                
                foreach (var product in products)
                {
                    var result = await ValidateAndFixProductIngredientsAsync(product.ProductID);
                    if (result) fixedCount++;
                }
                
                System.Diagnostics.Debug.WriteLine($"✅ Validated {products.Count} products, fixed {fixedCount} products with ingredient issues");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error validating all product ingredients: {ex.Message}");
                return false;
            }
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
                
                // Store addon amount and unit
                item.InputAmountMedium = reader.IsDBNull(reader.GetOrdinal("amount")) ? 0 : reader.GetDouble("amount");
                item.InputUnitMedium = reader.IsDBNull(reader.GetOrdinal("unit")) ? item.unitOfMeasurement : reader.GetString("unit");

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

            // Get image bytes if imageSet is provided
            byte[]? imageBytes = null;
            if (!string.IsNullOrWhiteSpace(product.ImageSet))
            {
                var imagePath = Services.ImagePersistenceService.GetImagePath(product.ImageSet);
                if (File.Exists(imagePath))
                {
                    imageBytes = await File.ReadAllBytesAsync(imagePath);
                }
            }

            var sql = "INSERT INTO products (productName, smallPrice, mediumPrice, largePrice, category, subcategory, imageSet, imageData, description, colorCode) " +
                      "VALUES (@ProductName, @SmallPrice, @MediumPrice, @LargePrice, @Category, @Subcategory, @Image, @ImageData, @Description, @ColorCode);";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ProductName", product.ProductName);
            // Save NULL for smallPrice for all categories except Coffee (only Coffee category needs small size)
            bool isCoffeeCategory = string.Equals(product.Category, "Coffee", StringComparison.OrdinalIgnoreCase);
            cmd.Parameters.AddWithValue("@SmallPrice", isCoffeeCategory && product.SmallPrice.HasValue && product.SmallPrice.Value > 0 ? (object)product.SmallPrice.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@MediumPrice", product.MediumPrice);
            cmd.Parameters.AddWithValue("@LargePrice", product.LargePrice);
            cmd.Parameters.AddWithValue("@Category", product.Category);
            cmd.Parameters.AddWithValue("@Subcategory", (object?)product.Subcategory ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Image", product.ImageSet);
            cmd.Parameters.AddWithValue("@ImageData", imageBytes != null ? (object)imageBytes : DBNull.Value);
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

            // Get image bytes if imageSet is provided
            byte[]? imageBytes = null;
            if (!string.IsNullOrWhiteSpace(product.ImageSet))
            {
                var imagePath = Services.ImagePersistenceService.GetImagePath(product.ImageSet);
                if (File.Exists(imagePath))
                {
                    imageBytes = await File.ReadAllBytesAsync(imagePath);
                }
            }

            var sql = "INSERT INTO products (productName, smallPrice, mediumPrice, largePrice, category, subcategory, imageSet, imageData, description, colorCode) " +
                      "VALUES (@ProductName, @SmallPrice, @MediumPrice, @LargePrice, @Category, @Subcategory, @Image, @ImageData, @Description, @ColorCode);";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ProductName", product.ProductName);
            // Save NULL for smallPrice for all categories except Coffee (only Coffee category needs small size)
            bool isCoffeeCategory = string.Equals(product.Category, "Coffee", StringComparison.OrdinalIgnoreCase);
            cmd.Parameters.AddWithValue("@SmallPrice", isCoffeeCategory && product.SmallPrice.HasValue && product.SmallPrice.Value > 0 ? (object)product.SmallPrice.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@MediumPrice", product.MediumPrice);
            cmd.Parameters.AddWithValue("@LargePrice", product.LargePrice);
            cmd.Parameters.AddWithValue("@Category", product.Category);
            cmd.Parameters.AddWithValue("@Subcategory", (object?)product.Subcategory ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Image", product.ImageSet);
            cmd.Parameters.AddWithValue("@ImageData", imageBytes != null ? (object)imageBytes : DBNull.Value);
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
                (productID, itemID, role, amount_small, unit_small, amount_medium, unit_medium, amount_large, unit_large) 
                VALUES (@ProductID, @ItemID, 'ingredient', @AmtS, @UnitS, @AmtM, @UnitM, @AmtL, @UnitL);";
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
                    // Ensure unit is never null or empty - use 'pcs' as fallback for NOT NULL columns
                    var unitValue = string.IsNullOrWhiteSpace(link.unit) ? "pcs" : link.unit;
                    cmd.Parameters.AddWithValue("@Unit", unitValue);
                    // Default per-size to shared value unless caller added explicit params
                    cmd.Parameters.AddWithValue("@AmtS", link.amount);
                    cmd.Parameters.AddWithValue("@UnitS", unitValue);
                    cmd.Parameters.AddWithValue("@AmtM", link.amount);
                    cmd.Parameters.AddWithValue("@UnitM", unitValue);
                    cmd.Parameters.AddWithValue("@AmtL", link.amount);
                    cmd.Parameters.AddWithValue("@UnitL", unitValue);
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
                (productID, itemID, role, amount_small, unit_small, amount_medium, unit_medium, amount_large, unit_large) 
                VALUES (@ProductID, @ItemID, 'ingredient', @AmtS, @UnitS, @AmtM, @UnitM, @AmtL, @UnitL);";
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
                    cmd.Parameters.AddWithValue("@AmtS", link.amtS);
                    // Ensure units are never null or empty - use 'pcs' as fallback for NOT NULL columns
                    var unitS = string.IsNullOrWhiteSpace(link.unitS) ? "pcs" : link.unitS;
                    var unitM = string.IsNullOrWhiteSpace(link.unitM) ? "pcs" : link.unitM;
                    var unitL = string.IsNullOrWhiteSpace(link.unitL) ? "pcs" : link.unitL;
                    cmd.Parameters.AddWithValue("@UnitS", unitS);
                    cmd.Parameters.AddWithValue("@AmtM", link.amtM);
                    cmd.Parameters.AddWithValue("@UnitM", unitM);
                    cmd.Parameters.AddWithValue("@AmtL", link.amtL);
                    cmd.Parameters.AddWithValue("@UnitL", unitL);
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
                    SmallPrice = reader.IsDBNull(reader.GetOrdinal("smallPrice")) ? null : reader.GetDecimal("smallPrice"),
                    MediumPrice = reader.IsDBNull(reader.GetOrdinal("mediumPrice")) ? 0 : reader.GetDecimal("mediumPrice"),
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
                    SmallPrice = reader.IsDBNull(reader.GetOrdinal("smallPrice")) ? null : reader.GetDecimal("smallPrice"),
                    MediumPrice = reader.IsDBNull(reader.GetOrdinal("mediumPrice")) ? 0 : reader.GetDecimal("mediumPrice"),
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

            // Get image bytes if imageSet is provided
            byte[]? imageBytes = null;
            if (!string.IsNullOrWhiteSpace(product.ImageSet))
            {
                var imagePath = Services.ImagePersistenceService.GetImagePath(product.ImageSet);
                if (File.Exists(imagePath))
                {
                    imageBytes = await File.ReadAllBytesAsync(imagePath);
                }
            }

            var sql = "UPDATE products SET productName = @ProductName, smallPrice = @SmallPrice, mediumPrice = @MediumPrice, largePrice = @LargePrice, " +
                      "category = @Category, subcategory = @Subcategory, imageSet = @Image, imageData = @ImageData, description = @Description, colorCode = @ColorCode WHERE productID = @ProductID;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ProductID", product.ProductID);
            cmd.Parameters.AddWithValue("@ProductName", product.ProductName);
            // Save NULL for smallPrice for all categories except Coffee (only Coffee category needs small size)
            bool isCoffeeCategory = string.Equals(product.Category, "Coffee", StringComparison.OrdinalIgnoreCase);
            cmd.Parameters.AddWithValue("@SmallPrice", isCoffeeCategory && product.SmallPrice.HasValue && product.SmallPrice.Value > 0 ? (object)product.SmallPrice.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@MediumPrice", product.MediumPrice);
            cmd.Parameters.AddWithValue("@LargePrice", product.LargePrice);
            cmd.Parameters.AddWithValue("@Category", product.Category);
            cmd.Parameters.AddWithValue("@Subcategory", (object?)product.Subcategory ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Image", product.ImageSet);
            cmd.Parameters.AddWithValue("@ImageData", imageBytes != null ? (object)imageBytes : DBNull.Value);
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
			var items = await QueryAsync(sql, reader => new InventoryPageModel
			{
				itemID = reader.GetInt32("itemID"),
				itemName = reader.GetString("itemName"),
				itemQuantity = reader.GetDouble("itemQuantity"),
				itemCategory = reader.IsDBNull(reader.GetOrdinal("itemCategory")) ? "" : reader.GetString("itemCategory"),
				ImageSet = reader.IsDBNull(reader.GetOrdinal("imageSet")) ? "" : reader.GetString("imageSet"),
				itemDescription = reader.IsDBNull(reader.GetOrdinal("itemDescription")) ? "" : reader.GetString("itemDescription"),
				unitOfMeasurement = reader.IsDBNull(reader.GetOrdinal("unitOfMeasurement")) ? "" : reader.GetString("unitOfMeasurement"),
				minimumQuantity = reader.IsDBNull(reader.GetOrdinal("minimumQuantity")) ? 0 : reader.GetDouble("minimumQuantity"),
				maximumQuantity = HasColumn(reader, "maximumQuantity") ? (reader.IsDBNull(reader.GetOrdinal("maximumQuantity")) ? 0 : reader.GetDouble("maximumQuantity")) : 0,
				CreatedAt = HasColumn(reader, "created_at") && !reader.IsDBNull(reader.GetOrdinal("created_at")) ? reader.GetDateTime("created_at") : DateTime.Now
			});

			// Restore images from database for inventory items where app data file is missing (background task)
			_ = Task.Run(async () =>
			{
				try
				{
					foreach (var item in items)
					{
						if (!string.IsNullOrWhiteSpace(item.ImageSet))
						{
							var imagePath = Services.ImagePersistenceService.GetImagePath(item.ImageSet);
							if (!File.Exists(imagePath))
							{
								await GetInventoryImageAsync(item.itemID);
							}
						}
					}
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"⚠️ Error in background inventory image restoration task: {ex.Message}");
				}
			});

			return items;
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
			lock (_inventoryCacheLock)
			{
				if (_inventoryCache.HasValue && IsFresh(_inventoryCache.Value.ts))
					return _inventoryCache.Value.items;
			}

			var items = await GetInventoryItemsAsync();
			lock (_inventoryCacheLock)
			{
				_inventoryCache = (DateTime.UtcNow, items);
			}
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
                    maximumQuantity = HasColumn(reader, "maximumQuantity") ? (reader.IsDBNull(reader.GetOrdinal("maximumQuantity")) ? 0 : reader.GetDouble("maximumQuantity")) : 0,
                    CreatedAt = HasColumn(reader, "created_at") && !reader.IsDBNull(reader.GetOrdinal("created_at")) ? reader.GetDateTime("created_at") : DateTime.Now
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
        public async Task<int> DeductInventoryAsync(IEnumerable<(string name, double amount, string originalUnit, double originalAmount, string size)> deductions, string productName = null) // Deducts inventory quantities
        {
            System.Diagnostics.Debug.WriteLine($"🔧 DeductInventoryAsync: Starting with {deductions?.Count() ?? 0} deductions for product: {productName ?? "Unknown"}");
            
            await using var conn = await GetOpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            int totalAffected = 0;
            try
            {
                foreach (var (name, amount, originalUnit, originalAmount, size) in deductions)
                {
                    System.Diagnostics.Debug.WriteLine($"🔧 DeductInventoryAsync: Processing {name} - {amount}");
                    
                    // Get current quantity before deduction
                    var getCurrentSql = "SELECT itemID, itemName, itemCategory, itemQuantity, unitOfMeasurement FROM inventory WHERE itemName = @Name;";
                    await using var getCurrentCmd = new MySqlCommand(getCurrentSql, conn, (MySqlTransaction)tx);
                    getCurrentCmd.Parameters.AddWithValue("@Name", name);
                    
                    int itemId = 0;
                    string itemName = name;
                    string itemCategory = "";
                    double previousQuantity = 0;
                    string unitOfMeasurement = "";
                    
                    await using var reader = await getCurrentCmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        itemId = reader.GetInt32("itemID");
                        itemName = reader.GetString("itemName");
                        itemCategory = reader.IsDBNull(reader.GetOrdinal("itemCategory")) ? "" : reader.GetString("itemCategory");
                        previousQuantity = reader.GetDouble("itemQuantity");
                        unitOfMeasurement = reader.IsDBNull(reader.GetOrdinal("unitOfMeasurement")) ? "" : reader.GetString("unitOfMeasurement");
                    }
                    await reader.CloseAsync();
                    
                    // Perform the deduction
                    var updateSql = "UPDATE inventory SET itemQuantity = GREATEST(itemQuantity - @Amount, 0) WHERE itemName = @Name;";
                    await using var updateCmd = new MySqlCommand(updateSql, conn, (MySqlTransaction)tx);
                    updateCmd.Parameters.AddWithValue("@Amount", amount);
                    updateCmd.Parameters.AddWithValue("@Name", name);
                    var rowsAffected = await updateCmd.ExecuteNonQueryAsync();
                    
                    if (rowsAffected > 0)
                    {
                        // Get new quantity after deduction
                        var getNewSql = "SELECT itemQuantity FROM inventory WHERE itemName = @Name;";
                        await using var getNewCmd = new MySqlCommand(getNewSql, conn, (MySqlTransaction)tx);
                        getNewCmd.Parameters.AddWithValue("@Name", name);
                        var newQuantity = Convert.ToDouble(await getNewCmd.ExecuteScalarAsync());
                        
                        // Use original amount and unit for logging if provided, otherwise use converted
                        var logAmount = originalAmount > 0 ? originalAmount : amount;
                        var logUnit = !string.IsNullOrWhiteSpace(originalUnit) ? originalUnit : unitOfMeasurement;
                        
                        // Log the activity
                        var logEntry = new InventoryActivityLog
                        {
                            ItemId = itemId,
                            ItemName = itemName,
                            ItemCategory = itemCategory,
                            Action = "DEDUCTED",
                            QuantityChanged = -logAmount, // Negative for deduction - use original amount
                            PreviousQuantity = previousQuantity,
                            NewQuantity = newQuantity,
                            UnitOfMeasurement = logUnit, // Use original unit for display
                            Reason = "POS_ORDER",
                            UserEmail = App.CurrentUser?.Email ?? "System",
                            UserFullName = !string.IsNullOrWhiteSpace(App.CurrentUser?.FullName) 
                                ? App.CurrentUser.FullName 
                                : $"{App.CurrentUser?.FirstName} {App.CurrentUser?.LastName}".Trim(),
                            UserId = App.CurrentUser?.ID,
                            ChangedBy = "POS",
                            OrderId = null, // Will be set by calling code if available
                            ProductName = productName, // Store the POS product name
                            ProductSize = size, // Store the product size
                            Notes = !string.IsNullOrWhiteSpace(productName) 
                                ? $"Deducted {logAmount} {logUnit} (converted: {amount} {unitOfMeasurement}) for {productName}" + (!string.IsNullOrWhiteSpace(size) ? $" - {size}" : "")
                                : $"Deducted {logAmount} {logUnit} (converted: {amount} {unitOfMeasurement}) for POS order"
                        };
                        
                        await LogInventoryActivityAsync(logEntry, conn, tx);
                        System.Diagnostics.Debug.WriteLine($"📝 Logged deduction activity for {name}: {previousQuantity} → {newQuantity}");
                        
                        await SendInventoryUpdateNotificationAsync(itemName, previousQuantity, newQuantity, -logAmount, logUnit, "DEDUCTED", $"POS order: {productName ?? "N/A"}");
                    }
                    
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
            try
            {
                await using var conn = await GetOpenConnectionAsync();

                // Set default UoM based on category if not specified
                var unitOfMeasurement = inventory.unitOfMeasurement;
                if (string.IsNullOrWhiteSpace(unitOfMeasurement))
                {
                    unitOfMeasurement = GetDefaultUnitForCategory(inventory.itemCategory);
                }

                // Get image bytes if imageSet is provided
                byte[]? imageBytes = null;
                if (!string.IsNullOrWhiteSpace(inventory.ImageSet))
                {
                    var imagePath = Services.ImagePersistenceService.GetImagePath(inventory.ImageSet);
                    if (File.Exists(imagePath))
                    {
                        imageBytes = await File.ReadAllBytesAsync(imagePath);
                    }
                }

                var sql = "INSERT INTO inventory (itemName, itemQuantity, itemCategory, imageSet, imageData, itemDescription, unitOfMeasurement, minimumQuantity, maximumQuantity) " +
                          "VALUES (@ItemName, @ItemQuantity, @ItemCategory, @ImageSet, @ImageData, @ItemDescription, @UnitOfMeasurement, @MinimumQuantity, @MaximumQuantity);";
                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ItemName", inventory.itemName);
                cmd.Parameters.AddWithValue("@ItemQuantity", inventory.itemQuantity);
                cmd.Parameters.AddWithValue("@ItemCategory", inventory.itemCategory);
                cmd.Parameters.AddWithValue("@ImageSet", inventory.ImageSet);
                cmd.Parameters.AddWithValue("@ImageData", imageBytes != null ? (object)imageBytes : DBNull.Value);
                cmd.Parameters.AddWithValue("@ItemDescription", (object?)inventory.itemDescription ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@UnitOfMeasurement", unitOfMeasurement);
                cmd.Parameters.AddWithValue("@MinimumQuantity", inventory.minimumQuantity);
                cmd.Parameters.AddWithValue("@MaximumQuantity", inventory.maximumQuantity);

                var rows = await cmd.ExecuteNonQueryAsync();
                InvalidateInventoryCache();
                
                // Send notification for new inventory item
                await SendInventoryUpdateNotificationAsync(inventory.itemName, 0, inventory.itemQuantity, inventory.itemQuantity, unitOfMeasurement, "ADDED", "New item created");
                
                return rows;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error saving inventory item '{inventory.itemName}': {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<int> UpdateInventoryItemAsync(InventoryPageModel inventory) // Updates an existing inventory item in the database
        {
            await using var conn = await GetOpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // Get previous quantity before update
                var getPreviousSql = "SELECT itemQuantity, itemName, itemCategory, unitOfMeasurement FROM inventory WHERE itemID = @ItemID;";
                await using var getPreviousCmd = new MySqlCommand(getPreviousSql, conn, (MySqlTransaction)tx);
                getPreviousCmd.Parameters.AddWithValue("@ItemID", inventory.itemID);
                
                double previousQuantity = 0;
                string itemName = inventory.itemName;
                string itemCategory = inventory.itemCategory ?? "";
                string unitOfMeasurement = inventory.unitOfMeasurement ?? "";
                
                await using var reader = await getPreviousCmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    previousQuantity = reader.GetDouble("itemQuantity");
                    itemName = reader.IsDBNull(reader.GetOrdinal("itemName")) ? inventory.itemName : reader.GetString("itemName");
                    itemCategory = reader.IsDBNull(reader.GetOrdinal("itemCategory")) ? (inventory.itemCategory ?? "") : reader.GetString("itemCategory");
                    unitOfMeasurement = reader.IsDBNull(reader.GetOrdinal("unitOfMeasurement")) ? (inventory.unitOfMeasurement ?? "") : reader.GetString("unitOfMeasurement");
                }
                await reader.CloseAsync();

                // Get image bytes if imageSet is provided
                byte[]? imageBytes = null;
                if (!string.IsNullOrWhiteSpace(inventory.ImageSet))
                {
                    var imagePath = Services.ImagePersistenceService.GetImagePath(inventory.ImageSet);
                    if (File.Exists(imagePath))
                    {
                        imageBytes = await File.ReadAllBytesAsync(imagePath);
                    }
                }

                var sql = "UPDATE inventory SET itemName = @ItemName, itemQuantity = @ItemQuantity, itemCategory = @ItemCategory, " +
                          "imageSet = @ImageSet, imageData = @ImageData, itemDescription = @ItemDescription, unitOfMeasurement = @UnitOfMeasurement, " +
                          "minimumQuantity = @MinimumQuantity, maximumQuantity = @MaximumQuantity WHERE itemID = @ItemID;";
                await using var cmd = new MySqlCommand(sql, conn, (MySqlTransaction)tx);
                cmd.Parameters.AddWithValue("@ItemID", inventory.itemID);
                cmd.Parameters.AddWithValue("@ItemName", inventory.itemName);
                cmd.Parameters.AddWithValue("@ItemQuantity", inventory.itemQuantity);
                cmd.Parameters.AddWithValue("@ItemCategory", inventory.itemCategory);
                cmd.Parameters.AddWithValue("@ImageSet", inventory.ImageSet);
                cmd.Parameters.AddWithValue("@ImageData", imageBytes != null ? (object)imageBytes : DBNull.Value);
                cmd.Parameters.AddWithValue("@ItemDescription", (object?)inventory.itemDescription ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@UnitOfMeasurement", (object?)inventory.unitOfMeasurement ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@MinimumQuantity", inventory.minimumQuantity);
                cmd.Parameters.AddWithValue("@MaximumQuantity", inventory.maximumQuantity);

                var rows = await cmd.ExecuteNonQueryAsync();
                
                // Calculate quantity change
                var quantityChanged = inventory.itemQuantity - previousQuantity;
                
                // Log the activity only if quantity actually changed
                if (Math.Abs(quantityChanged) > 0.0001) // Use small epsilon to handle floating point
                {
                    var logEntry = new InventoryActivityLog
                    {
                        ItemId = inventory.itemID,
                        ItemName = itemName,
                        ItemCategory = itemCategory,
                        Action = "UPDATED",
                        QuantityChanged = quantityChanged,
                        PreviousQuantity = previousQuantity,
                        NewQuantity = inventory.itemQuantity,
                        UnitOfMeasurement = unitOfMeasurement,
                        Reason = "MANUAL_ADJUSTMENT",
                        UserEmail = App.CurrentUser?.Email ?? "Unknown",
                        UserFullName = !string.IsNullOrWhiteSpace(App.CurrentUser?.FullName) 
                            ? App.CurrentUser.FullName 
                            : $"{App.CurrentUser?.FirstName} {App.CurrentUser?.LastName}".Trim(),
                        UserId = App.CurrentUser?.ID,
                        ChangedBy = "USER",
                        Notes = $"Manual inventory update: {itemName}"
                    };
                    
                    await LogInventoryActivityAsync(logEntry, conn, tx);
                    System.Diagnostics.Debug.WriteLine($"📝 Logged inventory update: {itemName} - {previousQuantity} → {inventory.itemQuantity} ({quantityChanged:+0.##;-0.##} {unitOfMeasurement})");
                    
                    // Send notification for inventory update
                    await SendInventoryUpdateNotificationAsync(itemName, previousQuantity, inventory.itemQuantity, quantityChanged, unitOfMeasurement, "UPDATED", "Manual adjustment");
                }
                
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

        // Send inventory update notification
        // DISABLED: Inventory/stock notifications have been disabled per user request
        private async Task SendInventoryUpdateNotificationAsync(string itemName, double previousQuantity, double newQuantity, double quantityChanged, string unitOfMeasurement, string action, string reason = "")
        {
            // Inventory notifications are disabled - only log to debug
            System.Diagnostics.Debug.WriteLine($"📝 Inventory update (notification disabled): {itemName} - {previousQuantity:F1} → {newQuantity:F1} {unitOfMeasurement} ({quantityChanged:+0.##;-0.##}) - {action}");
            await Task.CompletedTask;
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

        // ===================== INVENTORY ACTIVITY LOG METHODS =====================
        
        public async Task<int> LogInventoryActivityAsync(InventoryActivityLog logEntry) // Logs an inventory activity
        {
            await using var conn = await GetOpenConnectionAsync();
            
            var sql = @"INSERT INTO inventory_activity_log 
                        (itemId, itemName, itemCategory, action, quantityChanged, previousQuantity, 
                         newQuantity, unitOfMeasurement, reason, userEmail, userFullName, userId, 
                         changedBy, cost, orderId, productName, notes) 
                        VALUES (@ItemId, @ItemName, @ItemCategory, @Action, @QuantityChanged, 
                                @PreviousQuantity, @NewQuantity, @UnitOfMeasurement, @Reason, 
                                @UserEmail, @UserFullName, @UserId, @ChangedBy, @Cost, @OrderId, @ProductName, @Notes);";
            
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ItemId", logEntry.ItemId);
            cmd.Parameters.AddWithValue("@ItemName", logEntry.ItemName);
            cmd.Parameters.AddWithValue("@ItemCategory", (object?)logEntry.ItemCategory ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Action", logEntry.Action);
            cmd.Parameters.AddWithValue("@QuantityChanged", logEntry.QuantityChanged);
            cmd.Parameters.AddWithValue("@PreviousQuantity", logEntry.PreviousQuantity);
            cmd.Parameters.AddWithValue("@NewQuantity", logEntry.NewQuantity);
            cmd.Parameters.AddWithValue("@UnitOfMeasurement", (object?)logEntry.UnitOfMeasurement ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Reason", (object?)logEntry.Reason ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UserEmail", (object?)logEntry.UserEmail ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UserFullName", (object?)logEntry.UserFullName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UserId", (object?)logEntry.UserId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ChangedBy", (object?)logEntry.ChangedBy ?? "USER");
            cmd.Parameters.AddWithValue("@Cost", (object?)logEntry.Cost ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@OrderId", (object?)logEntry.OrderId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ProductName", (object?)logEntry.ProductName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Notes", (object?)logEntry.Notes ?? DBNull.Value);
            
            return await cmd.ExecuteNonQueryAsync();
        }

        private async Task<int> LogInventoryActivityAsync(InventoryActivityLog logEntry, MySqlConnection conn, MySqlTransaction tx) // Logs an inventory activity within a transaction
        {
            var sql = @"INSERT INTO inventory_activity_log 
                        (itemId, itemName, itemCategory, action, quantityChanged, previousQuantity, 
                         newQuantity, unitOfMeasurement, reason, userEmail, userFullName, userId, 
                         changedBy, cost, orderId, productName, productSize, notes) 
                        VALUES (@ItemId, @ItemName, @ItemCategory, @Action, @QuantityChanged, 
                                @PreviousQuantity, @NewQuantity, @UnitOfMeasurement, @Reason, 
                                @UserEmail, @UserFullName, @UserId, @ChangedBy, @Cost, @OrderId, @ProductName, @ProductSize, @Notes);";
            
            await using var cmd = new MySqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@ItemId", logEntry.ItemId);
            cmd.Parameters.AddWithValue("@ItemName", logEntry.ItemName);
            cmd.Parameters.AddWithValue("@ItemCategory", (object?)logEntry.ItemCategory ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Action", logEntry.Action);
            cmd.Parameters.AddWithValue("@QuantityChanged", logEntry.QuantityChanged);
            cmd.Parameters.AddWithValue("@PreviousQuantity", logEntry.PreviousQuantity);
            cmd.Parameters.AddWithValue("@NewQuantity", logEntry.NewQuantity);
            cmd.Parameters.AddWithValue("@UnitOfMeasurement", (object?)logEntry.UnitOfMeasurement ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Reason", (object?)logEntry.Reason ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UserEmail", (object?)logEntry.UserEmail ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UserFullName", (object?)logEntry.UserFullName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UserId", (object?)logEntry.UserId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ChangedBy", (object?)logEntry.ChangedBy ?? "USER");
            cmd.Parameters.AddWithValue("@Cost", (object?)logEntry.Cost ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@OrderId", (object?)logEntry.OrderId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ProductName", (object?)logEntry.ProductName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ProductSize", (object?)logEntry.ProductSize ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Notes", (object?)logEntry.Notes ?? DBNull.Value);
            
            return await cmd.ExecuteNonQueryAsync();
        }
        
        public async Task<List<InventoryActivityLog>> GetInventoryActivityLogAsync(int? itemId = null, int limit = 100) // Gets inventory activity log
        {
            await using var conn = await GetOpenConnectionAsync();
            
            var sql = @"SELECT * FROM inventory_activity_log";
            var parameters = new List<MySqlParameter>();
            
            if (itemId.HasValue)
            {
                sql += " WHERE itemId = @ItemId";
                parameters.Add(new MySqlParameter("@ItemId", MySqlDbType.Int32) { Value = itemId.Value });
            }
            
            sql += " ORDER BY timestamp DESC LIMIT @Limit";
            parameters.Add(new MySqlParameter("@Limit", MySqlDbType.Int32) { Value = limit });
            
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddRange(parameters.ToArray());
            
            var logs = new List<InventoryActivityLog>();
            await using var reader = await cmd.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                logs.Add(new InventoryActivityLog
                {
                    LogId = reader.GetInt32("logId"),
                    ItemId = reader.GetInt32("itemId"),
                    ItemName = reader.GetString("itemName"),
                    ItemCategory = reader.IsDBNull(reader.GetOrdinal("itemCategory")) ? null : reader.GetString("itemCategory"),
                    Action = reader.GetString("action"),
                    QuantityChanged = reader.GetDouble("quantityChanged"),
                    PreviousQuantity = reader.GetDouble("previousQuantity"),
                    NewQuantity = reader.GetDouble("newQuantity"),
                    UnitOfMeasurement = reader.IsDBNull(reader.GetOrdinal("unitOfMeasurement")) ? null : reader.GetString("unitOfMeasurement"),
                    Reason = reader.IsDBNull(reader.GetOrdinal("reason")) ? null : reader.GetString("reason"),
                    UserEmail = reader.IsDBNull(reader.GetOrdinal("userEmail")) ? null : reader.GetString("userEmail"),
                    UserFullName = reader.IsDBNull(reader.GetOrdinal("userFullName")) ? null : reader.GetString("userFullName"),
                    UserId = reader.IsDBNull(reader.GetOrdinal("userId")) ? (int?)null : reader.GetInt32("userId"),
                    ChangedBy = reader.IsDBNull(reader.GetOrdinal("changedBy")) ? "USER" : reader.GetString("changedBy"),
                    Cost = reader.IsDBNull(reader.GetOrdinal("cost")) ? (double?)null : reader.GetDouble("cost"),
                    OrderId = reader.IsDBNull(reader.GetOrdinal("orderId")) ? null : reader.GetString("orderId"),
                    ProductName = reader.IsDBNull(reader.GetOrdinal("productName")) ? null : reader.GetString("productName"),
                    ProductSize = reader.IsDBNull(reader.GetOrdinal("productSize")) ? null : reader.GetString("productSize"),
                    Timestamp = reader.GetDateTime("timestamp"),
                    Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes")
                });
            }
            
            return logs;
        }
        
        public async Task<List<InventoryActivityLog>> GetInventoryActivityLogByItemAsync(int itemId, int limit = 50) // Gets activity log for a specific item
        {
            return await GetInventoryActivityLogAsync(itemId, limit);
        }
        
        public async Task<List<InventoryActivityLog>> GetRecentInventoryActivityAsync(int limit = 50) // Gets recent inventory activity
        {
            return await GetInventoryActivityLogAsync(null, limit);
        }
        
        public async Task<Dictionary<string, (double totalDeducted, string unitOfMeasurement)>> GetInventoryDeductionsForPeriodAsync(DateTime startDate, DateTime endDate) // Gets inventory deductions from activity logs for a date range
        {
            await using var conn = await GetOpenConnectionAsync();
            var sql = @"SELECT itemName, SUM(ABS(quantityChanged)) as totalDeducted, unitOfMeasurement 
                       FROM inventory_activity_log 
                       WHERE action = 'DEDUCTED' 
                       AND reason = 'POS_ORDER' 
                       AND timestamp >= @StartDate 
                       AND timestamp < @EndDate 
                       GROUP BY itemName, unitOfMeasurement";
            
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@StartDate", startDate);
            cmd.Parameters.AddWithValue("@EndDate", endDate.AddDays(1)); // Add 1 day to include end date
            
            var deductions = new Dictionary<string, (double totalDeducted, string unitOfMeasurement)>();
            await using var reader = await cmd.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var itemName = reader.GetString("itemName");
                var totalDeducted = reader.GetDouble("totalDeducted");
                var unitOfMeasurement = reader.IsDBNull(reader.GetOrdinal("unitOfMeasurement")) 
                    ? null 
                    : reader.GetString("unitOfMeasurement");
                
                if (deductions.ContainsKey(itemName))
                {
                    // Aggregate if same item name appears with different units (shouldn't happen, but safe)
                    var existing = deductions[itemName];
                    deductions[itemName] = (existing.totalDeducted + totalDeducted, existing.unitOfMeasurement ?? unitOfMeasurement);
                }
                else
                {
                    deductions[itemName] = (totalDeducted, unitOfMeasurement);
                }
            }
            
            await reader.CloseAsync();
            
            return deductions;
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
        public async Task<int> SaveTransactionAsync(TransactionHistoryModel transaction, IEnumerable<CartItem>? cartItems = null) // Saves a transaction (and optional cart items) to the database
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

                var hasCartItems = cartItems != null && cartItems.Any();

                // Insert into transaction_items table
                var itemSql = "INSERT INTO transaction_items (transactionID, productID, productName, quantity, price, smallPrice, mediumPrice, largePrice, addonPrice, addOns, size) VALUES (@TransactionID, @ProductID, @ProductName, @Quantity, @Price, @SmallPrice, @MediumPrice, @LargePrice, @AddonPrice, @AddOns, @Size);";

                if (hasCartItems)
                {
                    foreach (var cartItem in cartItems!)
                    {
                        if (cartItem == null) continue;

                        var totalQuantity = cartItem.TotalQuantity > 0 ? cartItem.TotalQuantity : Math.Max(1, cartItem.Quantity);
                        if (totalQuantity <= 0) totalQuantity = 1;

                        var smallTotal = (cartItem.SmallPrice ?? 0m) * cartItem.SmallQuantity;
                        var mediumTotal = cartItem.MediumPrice * cartItem.MediumQuantity;
                        var largeTotal = cartItem.LargePrice * cartItem.LargeQuantity;
                        var calculatedAddon = cartItem.TotalPrice - (smallTotal + mediumTotal + largeTotal);
                        if (calculatedAddon < 0) calculatedAddon = 0;

                        var sizeLabel = string.IsNullOrWhiteSpace(cartItem.SelectedSize) || totalQuantity > 1
                            ? (totalQuantity > 1 ? "Multiple" : (cartItem.SelectedSize ?? ""))
                            : cartItem.SelectedSize;

                        int productId = cartItem.ProductId;
                        if (productId <= 0 && !string.IsNullOrWhiteSpace(cartItem.ProductName))
                        {
                            productId = await GetProductIdByNameAsync(cartItem.ProductName, conn, (MySqlTransaction)tx);
                        }

                        await using var itemCmd = new MySqlCommand(itemSql, conn, (MySqlTransaction)tx);
                        itemCmd.Parameters.AddWithValue("@TransactionID", transactionId);
                        itemCmd.Parameters.AddWithValue("@ProductID", productId > 0 ? productId : (object)DBNull.Value);
                        itemCmd.Parameters.AddWithValue("@ProductName", cartItem.ProductName ?? transaction.DrinkName ?? "");
                        itemCmd.Parameters.AddWithValue("@Quantity", totalQuantity);
                        itemCmd.Parameters.AddWithValue("@Price", cartItem.TotalPrice);
                        itemCmd.Parameters.AddWithValue("@SmallPrice", smallTotal);
                        itemCmd.Parameters.AddWithValue("@MediumPrice", mediumTotal);
                        itemCmd.Parameters.AddWithValue("@LargePrice", largeTotal);
                        itemCmd.Parameters.AddWithValue("@AddonPrice", calculatedAddon);
                        itemCmd.Parameters.AddWithValue("@AddOns", cartItem.AddOnsDisplay);
                        itemCmd.Parameters.AddWithValue("@Size", sizeLabel);

                        await itemCmd.ExecuteNonQueryAsync();
                    }
                }
                else
                {
                    // Preserve legacy behavior when no cart items are provided
                    int productId = await GetProductIdByNameAsync(transaction.DrinkName, conn, (MySqlTransaction)tx);

                    await using var itemCmd = new MySqlCommand(itemSql, conn, (MySqlTransaction)tx);
                    itemCmd.Parameters.AddWithValue("@TransactionID", transactionId);
                    itemCmd.Parameters.AddWithValue("@ProductID", productId > 0 ? productId : (object)DBNull.Value);
                    itemCmd.Parameters.AddWithValue("@ProductName", transaction.DrinkName ?? "");
                    itemCmd.Parameters.AddWithValue("@Quantity", transaction.Quantity <= 0 ? 1 : transaction.Quantity);
                    itemCmd.Parameters.AddWithValue("@Price", transaction.Price);
                    itemCmd.Parameters.AddWithValue("@SmallPrice", transaction.SmallPrice);
                    itemCmd.Parameters.AddWithValue("@MediumPrice", transaction.MediumPrice);
                    itemCmd.Parameters.AddWithValue("@LargePrice", transaction.LargePrice);
                    itemCmd.Parameters.AddWithValue("@AddonPrice", transaction.AddonPrice);
                    itemCmd.Parameters.AddWithValue("@AddOns", transaction.AddOns ?? "");
                    itemCmd.Parameters.AddWithValue("@Size", transaction.Size ?? "");

                    await itemCmd.ExecuteNonQueryAsync();
                }

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

        /// <summary>
        /// Gets transactions with advanced filtering and pagination support
        /// </summary>
        public async Task<(List<TransactionHistoryModel> Transactions, int TotalCount)> GetTransactionsFilteredAsync(
            DateTime startDate, 
            DateTime endDate,
            List<string> paymentMethods = null,
            List<string> categories = null,
            List<string> sizes = null,
            int page = 1,
            int pageSize = 50)
        {
            await using var conn = await GetOpenConnectionAsync();

            // Build WHERE clause with filters
            var whereConditions = new List<string> { "t.transactionDate >= @StartDate AND t.transactionDate < @EndDate" };
            var parameters = new Dictionary<string, object>
            {
                { "@StartDate", startDate },
                { "@EndDate", endDate }
            };

            if (paymentMethods != null && paymentMethods.Count > 0)
            {
                var paymentPlaceholders = string.Join(",", paymentMethods.Select((_, i) => $"@PaymentMethod{i}"));
                whereConditions.Add($"t.paymentMethod IN ({paymentPlaceholders})");
                for (int i = 0; i < paymentMethods.Count; i++)
                {
                    parameters.Add($"@PaymentMethod{i}", paymentMethods[i]);
                }
            }

            if (sizes != null && sizes.Count > 0)
            {
                var sizePlaceholders = string.Join(",", sizes.Select((_, i) => $"@Size{i}"));
                whereConditions.Add($"ti.size IN ({sizePlaceholders})");
                for (int i = 0; i < sizes.Count; i++)
                {
                    parameters.Add($"@Size{i}", sizes[i]);
                }
            }

            var whereClause = string.Join(" AND ", whereConditions);

            // Get total count
            var countSql = $@"SELECT COUNT(DISTINCT t.transactionID) as totalCount
                            FROM transactions t
                            LEFT JOIN transaction_items ti ON t.transactionID = ti.transactionID
                            WHERE {whereClause}";

            await using var countCmd = new MySqlCommand(countSql, conn);
            AddParameters(countCmd, parameters);
            var totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

            // Get paginated transactions
            var offset = (page - 1) * pageSize;
            var sql = $@"SELECT t.transactionID, t.total, t.transactionDate, t.status, t.paymentMethod,
                        ti.productName, ti.quantity, ti.price, ti.smallPrice, ti.mediumPrice, ti.largePrice, ti.addonPrice, ti.addOns, ti.size
                        FROM transactions t
                        LEFT JOIN transaction_items ti ON t.transactionID = ti.transactionID
                        WHERE {whereClause}
                        ORDER BY t.transactionDate DESC
                        LIMIT @PageSize OFFSET @Offset";

            await using var cmd = new MySqlCommand(sql, conn);
            AddParameters(cmd, parameters);
            cmd.Parameters.AddWithValue("@PageSize", pageSize);
            cmd.Parameters.AddWithValue("@Offset", offset);

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
                    
                    try
                    {
                        transaction.SmallPrice = reader.IsDBNull(reader.GetOrdinal("smallPrice")) ? 0 : reader.GetDecimal("smallPrice");
                        transaction.MediumPrice = reader.IsDBNull(reader.GetOrdinal("mediumPrice")) ? 0 : reader.GetDecimal("mediumPrice");
                        transaction.LargePrice = reader.IsDBNull(reader.GetOrdinal("largePrice")) ? 0 : reader.GetDecimal("largePrice");
                        transaction.AddonPrice = reader.IsDBNull(reader.GetOrdinal("addonPrice")) ? 0 : reader.GetDecimal("addonPrice");
                    }
                    catch
                    {
                        // Fallback for old schema
                        transaction.SmallPrice = 0;
                        transaction.MediumPrice = 0;
                        transaction.LargePrice = 0;
                        transaction.AddonPrice = 0;
                    }
                    
                    transaction.Size = reader.IsDBNull(reader.GetOrdinal("size")) ? "" : reader.GetString("size");
                    transaction.AddOns = reader.IsDBNull(reader.GetOrdinal("addOns")) ? "No add-ons" : reader.GetString("addOns");
                }

                transactions.Add(transaction);
            }

            // Apply category filter in memory (since categories are in products table)
            if (categories != null && categories.Count > 0)
            {
                var allProducts = await GetProductsAsyncCached();
                var categoryProducts = allProducts
                    .Where(p => categories.Contains(p.Category, StringComparer.OrdinalIgnoreCase))
                    .Select(p => p.ProductName)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                transactions = transactions
                    .Where(t => !string.IsNullOrWhiteSpace(t.DrinkName) && categoryProducts.Contains(t.DrinkName))
                    .ToList();
            }

            return (transactions, totalCount);
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

        public async Task<List<PaymentMethodProductData>> GetProductsByPaymentMethodAsync(string paymentMethod, DateTime startDate, DateTime endDate)
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = @"SELECT ti.productName, ti.size, SUM(ti.quantity) as totalQuantity, SUM(ti.price * ti.quantity) as totalRevenue
                        FROM transactions t
                        JOIN transaction_items ti ON t.transactionID = ti.transactionID
                        WHERE t.paymentMethod = @PaymentMethod 
                        AND t.transactionDate >= @StartDate 
                        AND t.transactionDate < @EndDate
                        GROUP BY ti.productName, ti.size
                        ORDER BY totalRevenue DESC";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@PaymentMethod", paymentMethod);
            cmd.Parameters.AddWithValue("@StartDate", startDate);
            cmd.Parameters.AddWithValue("@EndDate", endDate);

            var results = new List<PaymentMethodProductData>();
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                results.Add(new PaymentMethodProductData
                {
                    ProductName = reader.GetString("productName"),
                    Size = reader.IsDBNull(reader.GetOrdinal("size")) ? null : reader.GetString("size"),
                    Quantity = reader.GetInt32("totalQuantity"),
                    Revenue = reader.GetDecimal("totalRevenue")
                });
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
                var user = new UserInfoModel
                {
                    ID = reader.GetInt32("id"),
                    Email = reader.GetString("email"),
                    Password = reader.GetString("password"),
                    FirstName = reader.IsDBNull(reader.GetOrdinal("firstName")) ? string.Empty : reader.GetString("firstName"),
                    LastName = reader.IsDBNull(reader.GetOrdinal("lastName")) ? string.Empty : reader.GetString("lastName"),
                    IsAdmin = reader.IsDBNull(reader.GetOrdinal("isAdmin")) ? false : reader.GetBoolean("isAdmin"),
                    Status = reader.IsDBNull(reader.GetOrdinal("status")) ? "approved" : reader.GetString("status"),
                    CanAccessInventory = reader.IsDBNull(reader.GetOrdinal("can_access_inventory")) ? false : reader.GetBoolean("can_access_inventory"),
                    CanAccessSalesReport = reader.IsDBNull(reader.GetOrdinal("can_access_sales_report")) ? false : reader.GetBoolean("can_access_sales_report")
                };
                
                // Handle potentially NULL fields for username, fullName, and profileImage
                user.Username = reader.IsDBNull(reader.GetOrdinal("username")) ? string.Empty : reader.GetString("username");
                user.FullName = reader.IsDBNull(reader.GetOrdinal("fullName")) ? string.Empty : reader.GetString("fullName");
                user.ProfileImage = reader.IsDBNull(reader.GetOrdinal("profileImage")) ? "usericon.png" : reader.GetString("profileImage");
                
                // Handle last_login field (may be NULL for users who haven't logged in yet)
                try
                {
                    var lastLoginOrdinal = reader.GetOrdinal("last_login");
                    user.LastLogin = reader.IsDBNull(lastLoginOrdinal) ? null : (DateTime?)reader.GetDateTime(lastLoginOrdinal);
                }
                catch
                {
                    // Column doesn't exist yet (old database), set to null
                    user.LastLogin = null;
                }
                
                // Handle created_at field
                try
                {
                    var createdAtOrdinal = reader.GetOrdinal("created_at");
                    user.CreatedAt = reader.IsDBNull(createdAtOrdinal) ? DateTime.Now : reader.GetDateTime(createdAtOrdinal);
                }
                catch
                {
                    user.CreatedAt = DateTime.Now;
                }
                
                return user;
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

        public async Task<int> UpdateUserLastLoginAsync(int userId) // Updates the last_login timestamp for a user
        {
            var sql = "UPDATE users SET last_login = @LastLogin WHERE id = @Id;";
            return await ExecuteNonQueryAsync(sql, new Dictionary<string, object?>
            {
                {"@LastLogin", DateTime.Now},
                {"@Id", userId}
            });
        }

        public async Task<List<UserInfoModel>> GetAllUsersAsync() 
        {
            // Check if can_access_pos column exists
            bool hasPOSColumn = false;
            try
            {
                await using var conn = await GetOpenConnectionAsync();
                var checkSql = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'users' AND COLUMN_NAME = 'can_access_pos';";
                await using var checkCmd = new MySqlCommand(checkSql, conn);
                hasPOSColumn = await checkCmd.ExecuteScalarAsync() != null;
            }
            catch
            {
                // If check fails, assume column doesn't exist
                hasPOSColumn = false;
            }
            
            var sql = hasPOSColumn
                ? "SELECT id, email, password, firstName, lastName, isAdmin, status, profileImage, IFNULL(can_access_inventory, 0) AS can_access_inventory, IFNULL(can_access_pos, 0) AS can_access_pos, IFNULL(can_access_sales_report, 0) AS can_access_sales_report, created_at, last_login FROM users ORDER BY id ASC;"
                : "SELECT id, email, password, firstName, lastName, isAdmin, status, profileImage, IFNULL(can_access_inventory, 0) AS can_access_inventory, IFNULL(can_access_sales_report, 0) AS can_access_sales_report, created_at, last_login FROM users ORDER BY id ASC;";
            
            return await QueryAsync(sql, reader => new UserInfoModel
            {
                ID = reader.GetInt32("id"),
                Email = reader.IsDBNull(reader.GetOrdinal("email")) ? string.Empty : reader.GetString("email"),
                Password = reader.IsDBNull(reader.GetOrdinal("password")) ? string.Empty : reader.GetString("password"),
                FirstName = reader.IsDBNull(reader.GetOrdinal("firstName")) ? string.Empty : reader.GetString("firstName"),
                LastName = reader.IsDBNull(reader.GetOrdinal("lastName")) ? string.Empty : reader.GetString("lastName"),
                IsAdmin = reader.IsDBNull(reader.GetOrdinal("isAdmin")) ? false : reader.GetBoolean("isAdmin"),
                Status = reader.IsDBNull(reader.GetOrdinal("status")) ? "approved" : reader.GetString("status"),
                ProfileImage = reader.IsDBNull(reader.GetOrdinal("profileImage")) ? "usericon.png" : reader.GetString("profileImage"),
                CanAccessInventory = !reader.IsDBNull(reader.GetOrdinal("can_access_inventory")) && reader.GetBoolean("can_access_inventory"),
                CanAccessPOS = hasPOSColumn && !reader.IsDBNull(reader.GetOrdinal("can_access_pos")) && reader.GetBoolean("can_access_pos"),
                CanAccessSalesReport = !reader.IsDBNull(reader.GetOrdinal("can_access_sales_report")) && reader.GetBoolean("can_access_sales_report"),
                CreatedAt = HasColumn(reader, "created_at") && !reader.IsDBNull(reader.GetOrdinal("created_at")) ? reader.GetDateTime("created_at") : DateTime.Now,
                LastLogin = HasColumn(reader, "last_login") && !reader.IsDBNull(reader.GetOrdinal("last_login")) ? (DateTime?)reader.GetDateTime("last_login") : null
            });
        }

        /// <summary>
        /// Returns a page of users and the total count for pagination.
        /// </summary>
        public async Task<(List<UserInfoModel> Users, int TotalCount)> GetUsersPagedAsync(int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            await using var conn = await GetOpenConnectionAsync();

            // Check optional can_access_pos column once per call
            bool hasPOSColumn = false;
            try
            {
                var checkSql = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'users' AND COLUMN_NAME = 'can_access_pos';";
                await using var checkCmd = new MySqlCommand(checkSql, conn);
                hasPOSColumn = await checkCmd.ExecuteScalarAsync() != null;
            }
            catch
            {
                hasPOSColumn = false;
            }

            // Get total count
            var countSql = "SELECT COUNT(*) FROM users;";
            await using var countCmd = new MySqlCommand(countSql, conn);
            var totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

            // Get page
            var offset = (page - 1) * pageSize;
            var pageSql = hasPOSColumn
                ? "SELECT id, email, password, firstName, lastName, isAdmin, status, profileImage, IFNULL(can_access_inventory, 0) AS can_access_inventory, IFNULL(can_access_pos, 0) AS can_access_pos, IFNULL(can_access_sales_report, 0) AS can_access_sales_report, created_at, last_login FROM users ORDER BY id ASC LIMIT @PageSize OFFSET @Offset;"
                : "SELECT id, email, password, firstName, lastName, isAdmin, status, profileImage, IFNULL(can_access_inventory, 0) AS can_access_inventory, IFNULL(can_access_sales_report, 0) AS can_access_sales_report, created_at, last_login FROM users ORDER BY id ASC LIMIT @PageSize OFFSET @Offset;";

            await using var pageCmd = new MySqlCommand(pageSql, conn);
            pageCmd.Parameters.AddWithValue("@PageSize", pageSize);
            pageCmd.Parameters.AddWithValue("@Offset", offset);

            var users = new List<UserInfoModel>();
            await using var reader = await pageCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(new UserInfoModel
                {
                    ID = reader.GetInt32("id"),
                    Email = reader.IsDBNull(reader.GetOrdinal("email")) ? string.Empty : reader.GetString("email"),
                    Password = reader.IsDBNull(reader.GetOrdinal("password")) ? string.Empty : reader.GetString("password"),
                    FirstName = reader.IsDBNull(reader.GetOrdinal("firstName")) ? string.Empty : reader.GetString("firstName"),
                    LastName = reader.IsDBNull(reader.GetOrdinal("lastName")) ? string.Empty : reader.GetString("lastName"),
                    IsAdmin = reader.IsDBNull(reader.GetOrdinal("isAdmin")) ? false : reader.GetBoolean("isAdmin"),
                    Status = reader.IsDBNull(reader.GetOrdinal("status")) ? "approved" : reader.GetString("status"),
                    ProfileImage = reader.IsDBNull(reader.GetOrdinal("profileImage")) ? "usericon.png" : reader.GetString("profileImage"),
                    CanAccessInventory = !reader.IsDBNull(reader.GetOrdinal("can_access_inventory")) && reader.GetBoolean("can_access_inventory"),
                    CanAccessPOS = hasPOSColumn && !reader.IsDBNull(reader.GetOrdinal("can_access_pos")) && reader.GetBoolean("can_access_pos"),
                    CanAccessSalesReport = !reader.IsDBNull(reader.GetOrdinal("can_access_sales_report")) && reader.GetBoolean("can_access_sales_report"),
                    CreatedAt = HasColumn(reader, "created_at") && !reader.IsDBNull(reader.GetOrdinal("created_at")) ? reader.GetDateTime("created_at") : DateTime.Now,
                    LastLogin = HasColumn(reader, "last_login") && !reader.IsDBNull(reader.GetOrdinal("last_login")) ? (DateTime?)reader.GetDateTime("last_login") : null
                });
            }

            return (users, totalCount);
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
            
            var sql = "INSERT INTO users (firstName, lastName, email, password, status, isAdmin, can_access_inventory, can_access_sales_report) " +
                      "VALUES (@FirstName,@LastName, @Email, @Password, 'approved', @IsAdmin, @CanAccessInventory, @CanAccessSalesReport);";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@FirstName", user.FirstName);
            cmd.Parameters.AddWithValue("@LastName", user.LastName);
            cmd.Parameters.AddWithValue("@Email", user.Email);
            cmd.Parameters.AddWithValue("@Password", user.Password);
            cmd.Parameters.AddWithValue("@IsAdmin", isFirstUser);
            cmd.Parameters.AddWithValue("@CanAccessInventory", true); // All users can view inventory by default
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
                    var user = new UserInfoModel
                    {
                        ID = reader.GetInt32("id"),
                        Email = reader.GetString("email"),
                        FirstName = reader.GetString("firstName"),
                        LastName = reader.GetString("lastName"),
                        Password = reader.GetString("password"),
                        IsAdmin = reader.GetBoolean("isAdmin"),
                        Status = reader.GetString("status"),
                        CanAccessInventory = reader.IsDBNull(reader.GetOrdinal("can_access_inventory")) ? false : reader.GetBoolean("can_access_inventory"),
                        CanAccessSalesReport = reader.IsDBNull(reader.GetOrdinal("can_access_sales_report")) ? false : reader.GetBoolean("can_access_sales_report")
                    };
                    
                    // Handle potentially NULL fields with proper null checking
                    user.Username = reader.IsDBNull(reader.GetOrdinal("username")) ? string.Empty : reader.GetString("username");
                    user.FullName = reader.IsDBNull(reader.GetOrdinal("fullName")) ? string.Empty : reader.GetString("fullName");
                    user.ProfileImage = reader.IsDBNull(reader.GetOrdinal("profileImage")) ? "usericon.png" : reader.GetString("profileImage");
                    
                    System.Diagnostics.Debug.WriteLine($"Loaded user from DB - ID: {user.ID}, Username: '{user.Username}', ProfileImage: '{user.ProfileImage}', FullName: '{user.FullName}'");
                    
                    // Restore profile image from database if file is missing
                    if (!string.IsNullOrWhiteSpace(user.ProfileImage) && user.ProfileImage != "usericon.png")
                    {
                        var profileImageName = user.ProfileImage;
                        var imagePath = Services.ImagePersistenceService.GetImagePath(profileImageName);
                        if (!File.Exists(imagePath))
                        {
                            // Try to restore from database
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    await GetUserProfileImageAsync(user.ID);
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Error restoring profile image for user {user.ID}: {ex.Message}");
                                }
                            });
                        }
                    }
                    
                    return user;
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting user by ID: {ex.Message}");
                return null;
            }
        }

        public async Task<int> UpdateUserProfileAsync(int userId, string username, string email, string fullName, string profileImage, bool canAccessInventory, bool canAccessSalesReport)
        {
            try
            {
                await using var conn = await GetOpenConnectionAsync();
                
                // Parse fullName into firstName and lastName
                var nameParts = fullName?.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? new string[0];
                var firstName = nameParts.Length > 0 ? nameParts[0] : string.Empty;
                var lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : string.Empty;
                
                // Read image bytes from file system if it exists
                byte[]? imageBytes = null;
                if (!string.IsNullOrWhiteSpace(profileImage) && profileImage != "usericon.png")
                {
                    // Profile images are stored directly in AppDataDirectory, not in ProductImages subdirectory
                    var imagePath = Path.Combine(FileSystem.AppDataDirectory, profileImage);
                    
                    if (File.Exists(imagePath))
                    {
                        imageBytes = await File.ReadAllBytesAsync(imagePath);
                        System.Diagnostics.Debug.WriteLine($"✅ Read profile image from: {imagePath}, Size: {imageBytes.Length} bytes");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Profile image file not found at: {imagePath}");
                    }
                }
                
                var sql = @"UPDATE users SET 
                           username = @Username, 
                           email = @Email, 
                           firstName = @FirstName,
                           lastName = @LastName,
                           fullName = @FullName,
                           profileImage = @ProfileImage,
                           imageData = @ImageData,
                           can_access_inventory = @CanAccessInventory,
                           can_access_sales_report = @CanAccessSalesReport
                           WHERE id = @UserId;";
                
                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@Username", username ?? string.Empty);
                cmd.Parameters.AddWithValue("@Email", email ?? string.Empty);
                cmd.Parameters.AddWithValue("@FirstName", firstName ?? string.Empty);
                cmd.Parameters.AddWithValue("@LastName", lastName ?? string.Empty);
                cmd.Parameters.AddWithValue("@FullName", fullName ?? string.Empty);
                cmd.Parameters.AddWithValue("@ProfileImage", profileImage ?? "usericon.png");
                cmd.Parameters.AddWithValue("@ImageData", imageBytes != null ? (object)imageBytes : DBNull.Value);
                cmd.Parameters.AddWithValue("@CanAccessInventory", canAccessInventory);
                cmd.Parameters.AddWithValue("@CanAccessSalesReport", canAccessSalesReport);

                System.Diagnostics.Debug.WriteLine($"Executing UPDATE for userId={userId}, username='{username}', profileImage='{profileImage}', fullName='{fullName}'");
                var rowsAffected = await cmd.ExecuteNonQueryAsync();
                System.Diagnostics.Debug.WriteLine($"UPDATE completed. Rows affected: {rowsAffected}");
                return rowsAffected;
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
                
                // Read image bytes from file system if it exists
                byte[]? imageBytes = null;
                if (!string.IsNullOrWhiteSpace(profileImage) && profileImage != "usericon.png")
                {
                    // Profile images are stored directly in AppDataDirectory, not in ProductImages subdirectory
                    var imagePath = Path.Combine(FileSystem.AppDataDirectory, profileImage);
                    
                    if (File.Exists(imagePath))
                    {
                        imageBytes = await File.ReadAllBytesAsync(imagePath);
                        System.Diagnostics.Debug.WriteLine($"✅ Read profile image from: {imagePath}, Size: {imageBytes.Length} bytes");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Profile image file not found at: {imagePath}");
                    }
                }
                
                var sql = "UPDATE users SET profileImage = @ProfileImage, imageData = @ImageData WHERE id = @UserId;";
                
                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@ProfileImage", profileImage);
                cmd.Parameters.AddWithValue("@ImageData", imageBytes != null ? (object)imageBytes : DBNull.Value);

                var rowsAffected = await cmd.ExecuteNonQueryAsync();
                System.Diagnostics.Debug.WriteLine($"✅ Updated profile image in database for userId={userId}, imageBytes={(imageBytes != null ? imageBytes.Length : 0)} bytes, rowsAffected={rowsAffected}");
                
                return rowsAffected;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error updating user profile image: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
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
        public async Task<int> UpdateUserAccessAsync(int userId, bool canAccessInventory, bool canAccessPOS, bool canAccessSalesReport)
        {
            await using var conn = await GetOpenConnectionAsync();
            
            try
            {
                // Check if can_access_pos column exists, if not, add it
                var checkSql = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'users' AND COLUMN_NAME = 'can_access_pos';";
                await using var checkCmd = new MySqlCommand(checkSql, conn);
                var posColumnExists = await checkCmd.ExecuteScalarAsync() != null;
                
                if (!posColumnExists)
                {
                    // Add the can_access_pos column if it doesn't exist
                    var addColumnSql = "ALTER TABLE users ADD COLUMN can_access_pos BOOLEAN DEFAULT FALSE;";
                    await using var addColumnCmd = new MySqlCommand(addColumnSql, conn);
                    await addColumnCmd.ExecuteNonQueryAsync();
                    System.Diagnostics.Debug.WriteLine("✅ Added can_access_pos column to users table");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error checking/adding can_access_pos column: {ex.Message}");
                // Continue with update even if column check fails
            }
            
            // Update with all three access flags
            var sql = "UPDATE users SET can_access_inventory = @Inv, can_access_pos = @POS, can_access_sales_report = @Sales WHERE id = @Id;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Inv", canAccessInventory);
            cmd.Parameters.AddWithValue("@POS", canAccessPOS);
            cmd.Parameters.AddWithValue("@Sales", canAccessSalesReport);
            cmd.Parameters.AddWithValue("@Id", userId);
            return await cmd.ExecuteNonQueryAsync();
        }

        // Delete user
        public async Task<int> DeleteUserAsync(int userId)
        {
            // Protect the first user (primary admin) from deletion
            if (userId == 1)
            {
                System.Diagnostics.Debug.WriteLine("[Database] ⚠️ Attempted to delete protected primary admin account (ID: 1)");
                throw new InvalidOperationException("Cannot delete the primary admin account. This account is protected.");
            }
            
            await using var conn = await GetOpenConnectionAsync();
            
            // Check if user is admin before deleting
            var checkSql = "SELECT isAdmin FROM users WHERE id = @Id;";
            await using var checkCmd = new MySqlCommand(checkSql, conn);
            checkCmd.Parameters.AddWithValue("@Id", userId);
            var result = await checkCmd.ExecuteScalarAsync();
            
            if (result != null && Convert.ToBoolean(result))
            {
                System.Diagnostics.Debug.WriteLine($"[Database] ⚠️ Attempted to delete admin user (ID: {userId})");
                throw new InvalidOperationException("Cannot delete admin users. Please revoke admin privileges first.");
            }
            
            var sql = "DELETE FROM users WHERE id = @Id;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", userId);
            var deleted = await cmd.ExecuteNonQueryAsync();
            
            if (deleted > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[Database] ✅ User deleted (ID: {userId})");
            }
            
            return deleted;
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

            var sql = "INSERT INTO pending_registrations (email, password, firstName, lastName, registrationDate) " +
                      "VALUES (@Email, @Password, @FirstName, @LastName, @RegistrationDate);";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Email", request.Email);
            cmd.Parameters.AddWithValue("@Password", request.Password);
            cmd.Parameters.AddWithValue("@FirstName", request.FirstName);
            cmd.Parameters.AddWithValue("@LastName", request.LastName);
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
                    RequestDate = reader.IsDBNull(reader.GetOrdinal("registrationDate")) ? DateTime.MinValue : reader.GetDateTime("registrationDate"),
                    Status = "pending" // Default status since it's not stored in database
                });
            }
            return requests;
        }

        public async Task<UserPendingRequest> GetPendingUserRequestByEmailAsync(string email) // Check if email exists in pending requests
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = "SELECT * FROM pending_registrations WHERE email = @Email LIMIT 1;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Email", email);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new UserPendingRequest
                {
                    ID = reader.GetInt32("id"),
                    FirstName = reader.IsDBNull(reader.GetOrdinal("firstName")) ? string.Empty : reader.GetString("firstName"),
                    LastName = reader.IsDBNull(reader.GetOrdinal("lastName")) ? string.Empty : reader.GetString("lastName"),
                    Email = reader.GetString("email"),
                    Password = reader.IsDBNull(reader.GetOrdinal("password")) ? string.Empty : reader.GetString("password"),
                    RequestDate = reader.IsDBNull(reader.GetOrdinal("registrationDate")) ? DateTime.MinValue : reader.GetDateTime("registrationDate"),
                    Status = "pending"
                };
            }
            return null;
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
                var addUserSql = "INSERT INTO users (firstName, lastName, email, password, status, isAdmin, can_access_inventory, can_access_sales_report) " +
                                "VALUES (@FirstName, @LastName, @Email, @Password, @Status, @IsAdmin, @CanAccessInventory, @CanAccessSalesReport);";
                await using var addUserCmd = new MySqlCommand(addUserSql, conn, (MySqlTransaction)tx);
                addUserCmd.Parameters.AddWithValue("@FirstName", registration.FirstName);
                addUserCmd.Parameters.AddWithValue("@LastName", registration.LastName);
                addUserCmd.Parameters.AddWithValue("@Email", registration.Email);
                addUserCmd.Parameters.AddWithValue("@Password", registration.Password);
                addUserCmd.Parameters.AddWithValue("@Status", "approved");
                addUserCmd.Parameters.AddWithValue("@IsAdmin", isFirstUser);
                addUserCmd.Parameters.AddWithValue("@CanAccessInventory", true);
                addUserCmd.Parameters.AddWithValue("@CanAccessSalesReport", isFirstUser);

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
            try
            {
                await using var conn = await GetOpenConnectionAsync();

                var sql = "DELETE FROM pending_registrations WHERE id = @RequestId;";
                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@RequestId", requestId);

                return await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error rejecting pending registration (ID: {requestId}): {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                throw;
            }
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
                                                        requestedQuantity, unitPrice, totalPrice, unitOfMeasurement, quantity)
                        VALUES (@purchaseOrderId, @inventoryItemId, @itemName, @itemCategory, 
                                @requestedQuantity, @unitPrice, @totalPrice, @unitOfMeasurement, @quantity);";
                    
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
                        ["@unitOfMeasurement"] = item.unitOfMeasurement,
                        ["@quantity"] = 1 // Default quantity multiplier
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
        /// Creates a purchase order with custom quantities and UoMs
        /// </summary>
        public async Task<int> CreatePurchaseOrderWithCustomQuantitiesAsync(List<InventoryPageModel> lowStockItems, object editableItems)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🛒 Creating purchase order with custom quantities for {lowStockItems.Count} items");
                
                await using var conn = await GetOpenConnectionAsync();
                
                // First, ensure the purchase order tables exist
                await EnsurePurchaseOrderTablesExistAsync(conn);
                
                // Extract custom quantities from editableItems
                var customQuantities = new Dictionary<int, (double quantity, string uom)>();
                if (editableItems != null)
                {
                    var itemsProperty = editableItems.GetType().GetProperty("Items");
                    if (itemsProperty != null)
                    {
                        var itemsList = itemsProperty.GetValue(editableItems) as System.Collections.IEnumerable;
                        if (itemsList != null)
                        {
                            foreach (var editableItem in itemsList)
                            {
                                var inventoryItemIdProp = editableItem.GetType().GetProperty("InventoryItemId");
                                var approvedQuantityProp = editableItem.GetType().GetProperty("ApprovedQuantity");
                                var approvedUoMProp = editableItem.GetType().GetProperty("ApprovedUoM");

                                if (inventoryItemIdProp != null && approvedQuantityProp != null && approvedUoMProp != null)
                                {
                                    var itemId = Convert.ToInt32(inventoryItemIdProp.GetValue(editableItem));
                                    var quantity = Convert.ToDouble(approvedQuantityProp.GetValue(editableItem));
                                    var uom = approvedUoMProp.GetValue(editableItem)?.ToString() ?? "";
                                    customQuantities[itemId] = (quantity, uom);
                                }
                            }
                        }
                    }
                }

                // Create purchase order
                var insertOrderSql = @"
                    INSERT INTO purchase_orders (orderDate, supplierName, status, requestedBy, totalAmount, createdAt)
                    VALUES (@orderDate, @supplierName, @status, @requestedBy, @totalAmount, @createdAt);
                    SELECT LAST_INSERT_ID();";
                
                var currentUser = App.CurrentUser?.Email ?? "Unknown";
                
                // Calculate total amount using custom quantities
                decimal totalAmount = 0;
                foreach (var item in lowStockItems)
                {
                    var itemId = item.itemID;
                    if (customQuantities.ContainsKey(itemId))
                    {
                        var (quantity, uom) = customQuantities[itemId];
                        totalAmount += (decimal)quantity * 10.0m; // Default unit price
                    }
                    else
                    {
                        var neededQuantity = item.minimumQuantity - item.itemQuantity;
                        totalAmount += (decimal)neededQuantity * 10.0m;
                    }
                }
                
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
                
                // Create purchase order items with custom quantities
                foreach (var item in lowStockItems)
                {
                    var itemId = item.itemID;
                    double quantity;
                    string uom;

                    if (customQuantities.ContainsKey(itemId))
                    {
                        var (customQty, customUom) = customQuantities[itemId];
                        quantity = customQty;
                        uom = customUom;
                    }
                    else
                    {
                        quantity = item.minimumQuantity - item.itemQuantity;
                        uom = item.unitOfMeasurement ?? "pcs";
                    }

                    var requestedQuantity = (int)Math.Round(quantity);
                    var unitPrice = 10.0m; // Default unit price
                    var totalPrice = (decimal)quantity * unitPrice;
                    
                    System.Diagnostics.Debug.WriteLine($"📦 Adding item: {item.itemName} - Qty: {quantity} {uom}, Price: {totalPrice:C}");
                    
                    // Get quantity multiplier from editableItems if available, default to 1
                    int quantityMultiplier = 1;
                    if (customQuantities.ContainsKey(itemId) && editableItems != null)
                    {
                        var itemsProperty = editableItems.GetType().GetProperty("Items");
                        if (itemsProperty != null)
                        {
                            var itemsList = itemsProperty.GetValue(editableItems) as System.Collections.IEnumerable;
                            if (itemsList != null)
                            {
                                foreach (var editableItem in itemsList)
                                {
                                    var inventoryItemIdProp = editableItem.GetType().GetProperty("InventoryItemId");
                                    var quantityProp = editableItem.GetType().GetProperty("Quantity");
                                    
                                    if (inventoryItemIdProp != null && quantityProp != null)
                                    {
                                        var edItemId = Convert.ToInt32(inventoryItemIdProp.GetValue(editableItem));
                                        if (edItemId == itemId)
                                        {
                                            quantityMultiplier = Convert.ToInt32(quantityProp.GetValue(editableItem));
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    
                    var insertItemSql = @"
                        INSERT INTO purchase_order_items (purchaseOrderId, inventoryItemId, itemName, itemCategory, 
                                                        requestedQuantity, unitPrice, totalPrice, unitOfMeasurement, quantity)
                        VALUES (@purchaseOrderId, @inventoryItemId, @itemName, @itemCategory, 
                                @requestedQuantity, @unitPrice, @totalPrice, @unitOfMeasurement, @quantity);";
                    
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
                        ["@unitOfMeasurement"] = uom,
                        ["@quantity"] = quantityMultiplier
                    });
                    
                    await itemCmd.ExecuteNonQueryAsync();
                    System.Diagnostics.Debug.WriteLine($"✅ Item added to purchase order: {item.itemName}");
                }
                
                System.Diagnostics.Debug.WriteLine($"✅ Purchase order {orderId} created with custom quantities for {lowStockItems.Count} items");
                return orderId;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error creating purchase order with custom quantities: {ex.Message}");
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
                
                System.Diagnostics.Debug.WriteLine("📦 [Database] Fetching pending purchase orders...");
                
                // Get all pending orders (case-insensitive and whitespace-insensitive)
                // Include "Pending", "Partially Approved", and "Approved" orders
                // - Partially approved orders still have items that need to be processed
                // - Approved orders are included so users can retract items if they made a mistake
                // No limit - pagination is handled in ViewModel
                var sql = @"
                    SELECT * FROM purchase_orders
                    WHERE LOWER(TRIM(status)) IN ('pending', 'partially approved', 'approved')
                    ORDER BY 
                        CASE 
                            WHEN LOWER(TRIM(status)) = 'pending' THEN 1
                            WHEN LOWER(TRIM(status)) = 'partially approved' THEN 2
                            WHEN LOWER(TRIM(status)) = 'approved' THEN 3
                            ELSE 4
                        END,
                        createdAt DESC;";
                
                await using var cmd = new MySqlCommand(sql, conn);
                await using var reader = await cmd.ExecuteReaderAsync();
                
                var orders = new List<PurchaseOrderModel>();
                
                while (await reader.ReadAsync())
                {
                    var order = new PurchaseOrderModel
                    {
                        PurchaseOrderId = reader.GetInt32("purchaseOrderId"),
                        OrderDate = reader.GetDateTime("orderDate"),
                        SupplierName = reader.GetString("supplierName"),
                        Status = reader.GetString("status"),
                        RequestedBy = reader.GetString("requestedBy"),
                        ApprovedBy = reader.IsDBNull(reader.GetOrdinal("approvedBy")) ? string.Empty : reader.GetString("approvedBy"),
                        TotalAmount = reader.GetDecimal("totalAmount"),
                        CreatedAt = reader.GetDateTime("createdAt"),
                        Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? string.Empty : reader.GetString("notes")
                    };
                    orders.Add(order);
                    System.Diagnostics.Debug.WriteLine($"📦 [Database] Found pending order #{order.PurchaseOrderId} by {order.RequestedBy}");
                }
                
                System.Diagnostics.Debug.WriteLine($"📦 [Database] Total pending orders found: {orders.Count}");
                return orders;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error getting pending purchase orders: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                return new List<PurchaseOrderModel>();
            }
        }

        /// <summary>
        /// Gets recent purchase orders (all orders, ordered by date descending)
        /// </summary>
        public async Task<List<PurchaseOrderModel>> GetAllPurchaseOrdersAsync(int limit = 50)
        {
            try
            {
                await using var conn = await GetOpenConnectionAsync();
                
                System.Diagnostics.Debug.WriteLine("📦 [Database] Fetching all purchase orders...");
                
                var sql = @"
                    SELECT * FROM purchase_orders
                    ORDER BY createdAt DESC
                    LIMIT @Limit;";
                
                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Limit", limit);
                await using var reader = await cmd.ExecuteReaderAsync();
                
                var orders = new List<PurchaseOrderModel>();
                
                while (await reader.ReadAsync())
                {
                    var order = new PurchaseOrderModel
                    {
                        PurchaseOrderId = reader.GetInt32("purchaseOrderId"),
                        OrderDate = reader.GetDateTime("orderDate"),
                        SupplierName = reader.GetString("supplierName"),
                        Status = reader.GetString("status"),
                        RequestedBy = reader.GetString("requestedBy"),
                        ApprovedBy = reader.IsDBNull(reader.GetOrdinal("approvedBy")) ? string.Empty : reader.GetString("approvedBy"),
                        ApprovedDate = reader.IsDBNull(reader.GetOrdinal("approvedDate")) ? null : reader.GetDateTime("approvedDate"),
                        TotalAmount = reader.GetDecimal("totalAmount"),
                        CreatedAt = reader.GetDateTime("createdAt"),
                        Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? string.Empty : reader.GetString("notes")
                    };
                    orders.Add(order);
                }
                
                System.Diagnostics.Debug.WriteLine($"📦 [Database] Total purchase orders found: {orders.Count}");
                return orders;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error getting all purchase orders: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                return new List<PurchaseOrderModel>();
            }
        }

        /// <summary>
        /// Gets all items for a specific purchase order
        /// </summary>
        public async Task<List<PurchaseOrderItemModel>> GetPurchaseOrderItemsAsync(int purchaseOrderId)
        {
            try
            {
                await using var conn = await GetOpenConnectionAsync();
                
                var sql = @"
                    SELECT * FROM purchase_order_items
                    WHERE purchaseOrderId = @purchaseOrderId
                    ORDER BY itemName;";
                
                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@purchaseOrderId", purchaseOrderId);
                await using var reader = await cmd.ExecuteReaderAsync();
                
                var items = new List<PurchaseOrderItemModel>();
                while (await reader.ReadAsync())
                {
                    var requestedQty = reader.GetInt32("requestedQuantity");
                    // approvedQuantity = -1 means canceled, > 0 means accepted, 0 or null means pending
                    var approvedQty = reader.IsDBNull(reader.GetOrdinal("approvedQuantity")) 
                        ? 0 
                        : reader.GetInt32("approvedQuantity");
                    // If canceled (-1), set to 0 for display purposes
                    if (approvedQty < 0) approvedQty = 0;
                    
                    var quantity = reader.IsDBNull(reader.GetOrdinal("quantity")) ? 1 : reader.GetInt32("quantity");
                    
                    items.Add(new PurchaseOrderItemModel
                    {
                        PurchaseOrderItemId = reader.GetInt32("purchaseOrderItemId"),
                        PurchaseOrderId = reader.GetInt32("purchaseOrderId"),
                        InventoryItemId = reader.GetInt32("inventoryItemId"),
                        ItemName = reader.GetString("itemName"),
                        ItemCategory = reader.IsDBNull(reader.GetOrdinal("itemCategory")) ? "" : reader.GetString("itemCategory"),
                        RequestedQuantity = requestedQty,
                        ApprovedQuantity = approvedQty,
                        UnitPrice = reader.GetDecimal("unitPrice"),
                        TotalPrice = reader.GetDecimal("totalPrice"),
                        UnitOfMeasurement = reader.IsDBNull(reader.GetOrdinal("unitOfMeasurement")) ? "" : reader.GetString("unitOfMeasurement"),
                        Quantity = quantity
                    });
                }
                
                return items;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error getting purchase order items: {ex.Message}");
                return new List<PurchaseOrderItemModel>();
            }
        }

        /// <summary>
        /// Approves or rejects a purchase order
        /// </summary>
        public async Task<bool> UpdatePurchaseOrderStatusAsync(int purchaseOrderId, string status, string approvedBy, object? customItems = null)
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
                
                if (status == "Approved" && rowsAffected > 0)
                {
                    try
                    {
                        // Update inventory quantities with custom items if provided
                        await UpdateInventoryFromPurchaseOrderAsync(purchaseOrderId, customItems);
                        System.Diagnostics.Debug.WriteLine($"✅ Inventory updated for purchase order {purchaseOrderId}");
                    }
                    catch (Exception invEx)
                    {
                        // If inventory update fails, log it but don't fail the status update
                        System.Diagnostics.Debug.WriteLine($"⚠️ Warning: Purchase order {purchaseOrderId} status updated to Approved, but inventory update failed: {invEx.Message}");
                        System.Diagnostics.Debug.WriteLine($"⚠️ Stack trace: {invEx.StackTrace}");
                        // Status is already "Approved" in database, but inventory wasn't updated
                        // This is logged but doesn't fail the operation
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"✅ Purchase order {purchaseOrderId} status updated to {status}");
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error updating purchase order status (ID: {purchaseOrderId}): {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Inner exception: {ex.InnerException.Message}");
                }
                return false;
            }
        }

        /// <summary>
        /// Updates inventory quantities when purchase order is approved
        /// </summary>
        private async Task UpdateInventoryFromPurchaseOrderAsync(int purchaseOrderId, object? customItems = null)
        {
            await using var conn = await GetOpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();
            
            try
            {
                
                // If custom items are provided, use them; otherwise use requested quantities
                if (customItems != null)
                {
                    // Use reflection to access the custom items list
                    var itemsProperty = customItems.GetType().GetProperty("Items");
                    if (itemsProperty != null)
                    {
                        var itemsList = itemsProperty.GetValue(customItems) as System.Collections.IEnumerable;
                        if (itemsList != null)
                        {
                            foreach (var customItem in itemsList)
                            {
                                var inventoryItemIdProp = customItem.GetType().GetProperty("InventoryItemId");
                                var itemNameProp = customItem.GetType().GetProperty("ItemName");
                                var approvedQuantityProp = customItem.GetType().GetProperty("ApprovedQuantity");
                                var approvedUoMProp = customItem.GetType().GetProperty("ApprovedUoM");

                                if (inventoryItemIdProp == null || itemNameProp == null || approvedQuantityProp == null || approvedUoMProp == null)
                                    continue;

                                var inventoryItemId = Convert.ToInt32(inventoryItemIdProp.GetValue(customItem));
                                var itemName = itemNameProp.GetValue(customItem)?.ToString() ?? "";
                                var approvedQuantity = Convert.ToDouble(approvedQuantityProp.GetValue(customItem));
                                var approvedUoM = approvedUoMProp.GetValue(customItem)?.ToString() ?? "";

                                // Get current inventory item info (including maximumQuantity)
                                var getItemSqlCustom = "SELECT itemID, itemName, itemCategory, itemQuantity, unitOfMeasurement, maximumQuantity FROM inventory WHERE itemID = @ItemID;";
                                await using var getItemCmdCustom = new MySqlCommand(getItemSqlCustom, conn, (MySqlTransaction)tx);
                                getItemCmdCustom.Parameters.AddWithValue("@ItemID", inventoryItemId);

                                double currentQuantity = 0;
                                double maximumQuantity = 0;
                                string itemCategory = "";
                                string inventoryUoM = "";

                                await using var readerCustom = await getItemCmdCustom.ExecuteReaderAsync();
                                if (await readerCustom.ReadAsync())
                                {
                                    currentQuantity = readerCustom.GetDouble("itemQuantity");
                                    itemCategory = readerCustom.IsDBNull(readerCustom.GetOrdinal("itemCategory")) ? "" : readerCustom.GetString("itemCategory");
                                    inventoryUoM = readerCustom.IsDBNull(readerCustom.GetOrdinal("unitOfMeasurement")) ? "" : readerCustom.GetString("unitOfMeasurement");
                                    
                                    // Get maximumQuantity (may be NULL or 0)
                                    var maxQtyOrdinal = readerCustom.GetOrdinal("maximumQuantity");
                                    maximumQuantity = readerCustom.IsDBNull(maxQtyOrdinal) ? 0 : readerCustom.GetDouble(maxQtyOrdinal);
                                }
                                await readerCustom.CloseAsync();

                                // Convert approved quantity to inventory UoM if needed
                                double quantityToAdd = approvedQuantity;
                                if (approvedUoM != inventoryUoM)
                                {
                                    quantityToAdd = UnitConversionService.Convert(approvedQuantity, approvedUoM, inventoryUoM);
                                }

                                // Calculate new quantity after adding
                                double calculatedNewQuantity = currentQuantity + quantityToAdd;
                                
                                // Check if new quantity exceeds maximum stock, and adjust maximum if needed
                                if (maximumQuantity > 0 && calculatedNewQuantity > maximumQuantity)
                                {
                                    // Automatically adjust maximum stock to accommodate the new quantity
                                    System.Diagnostics.Debug.WriteLine($"📊 New quantity ({calculatedNewQuantity}) exceeds maximum stock ({maximumQuantity}). Adjusting maximum stock to {calculatedNewQuantity}.");
                                    maximumQuantity = calculatedNewQuantity;
                                }

                                // Update inventory (quantity and maximumQuantity if adjusted)
                                var updateSqlCustom = "UPDATE inventory SET itemQuantity = itemQuantity + @Quantity, maximumQuantity = @MaximumQuantity, updatedAt = @updatedAt WHERE itemID = @ItemID;";
                                await using var updateCmdCustom = new MySqlCommand(updateSqlCustom, conn, (MySqlTransaction)tx);
                                updateCmdCustom.Parameters.AddWithValue("@Quantity", quantityToAdd);
                                updateCmdCustom.Parameters.AddWithValue("@MaximumQuantity", maximumQuantity);
                                updateCmdCustom.Parameters.AddWithValue("@updatedAt", DateTime.Now);
                                updateCmdCustom.Parameters.AddWithValue("@ItemID", inventoryItemId);
                                await updateCmdCustom.ExecuteNonQueryAsync();

                                // Update the purchase_order_items table with approved quantity and UoM
                                var updatePOItemSql = @"UPDATE purchase_order_items 
                                                       SET approvedQuantity = @ApprovedQuantity, 
                                                           unitOfMeasurement = @ApprovedUoM
                                                       WHERE purchaseOrderId = @PurchaseOrderId 
                                                         AND inventoryItemId = @InventoryItemId;";
                                await using var updatePOItemCmd = new MySqlCommand(updatePOItemSql, conn, (MySqlTransaction)tx);
                                updatePOItemCmd.Parameters.AddWithValue("@ApprovedQuantity", (int)Math.Round(approvedQuantity));
                                updatePOItemCmd.Parameters.AddWithValue("@ApprovedUoM", approvedUoM);
                                updatePOItemCmd.Parameters.AddWithValue("@PurchaseOrderId", purchaseOrderId);
                                updatePOItemCmd.Parameters.AddWithValue("@InventoryItemId", inventoryItemId);
                                await updatePOItemCmd.ExecuteNonQueryAsync();

                                // Get new quantity after update
                                var getNewSqlCustom = "SELECT itemQuantity FROM inventory WHERE itemID = @ItemID;";
                                await using var getNewCmdCustom = new MySqlCommand(getNewSqlCustom, conn, (MySqlTransaction)tx);
                                getNewCmdCustom.Parameters.AddWithValue("@ItemID", inventoryItemId);
                                var newQuantity = Convert.ToDouble(await getNewCmdCustom.ExecuteScalarAsync());

                                // Log the addition
                                var logEntry = new InventoryActivityLog
                                {
                                    ItemId = inventoryItemId,
                                    ItemName = itemName,
                                    ItemCategory = itemCategory,
                                    Action = "ADDED",
                                    QuantityChanged = quantityToAdd,
                                    PreviousQuantity = currentQuantity,
                                    NewQuantity = newQuantity,
                                    UnitOfMeasurement = inventoryUoM,
                                    Reason = "PURCHASE_ORDER",
                                    UserEmail = App.CurrentUser?.Email ?? "System",
                                    OrderId = purchaseOrderId.ToString(),
                                    Notes = $"Added {approvedQuantity} {approvedUoM} (converted to {quantityToAdd} {inventoryUoM}) from purchase order #{purchaseOrderId}"
                                };

                                await LogInventoryActivityAsync(logEntry, conn, tx);
                                System.Diagnostics.Debug.WriteLine($"📝 Logged addition: {itemName} - {currentQuantity} → {newQuantity} ({quantityToAdd} {inventoryUoM})");
                            }

                            await tx.CommitAsync();
                            System.Diagnostics.Debug.WriteLine($"✅ Inventory updated for purchase order {purchaseOrderId} with custom amounts");
                            return;
                        }
                    }
                }

                // Fallback to original logic if no custom items provided
                var getItemsSql = @"
                    SELECT i.itemID, i.itemName, i.itemCategory, i.itemQuantity, i.unitOfMeasurement, poi.requestedQuantity
                    FROM inventory i
                    INNER JOIN purchase_order_items poi ON i.itemID = poi.inventoryItemId
                    WHERE poi.purchaseOrderId = @purchaseOrderId;";
                
                await using var getItemsCmd = new MySqlCommand(getItemsSql, conn, (MySqlTransaction)tx);
                getItemsCmd.Parameters.AddWithValue("@purchaseOrderId", purchaseOrderId);
                
                var itemsToUpdate = new List<(int itemId, string itemName, string itemCategory, double currentQuantity, string unitOfMeasurement, double requestedQuantity)>();
                
                await using var reader = await getItemsCmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    itemsToUpdate.Add((
                        reader.GetInt32("itemID"),
                        reader.GetString("itemName"),
                        reader.IsDBNull(reader.GetOrdinal("itemCategory")) ? "" : reader.GetString("itemCategory"),
                        reader.GetDouble("itemQuantity"),
                        reader.IsDBNull(reader.GetOrdinal("unitOfMeasurement")) ? "" : reader.GetString("unitOfMeasurement"),
                        reader.GetDouble("requestedQuantity")
                    ));
                }
                await reader.CloseAsync();
                
                // Update inventory quantities
                var updateSql = @"
                    UPDATE inventory i
                    INNER JOIN purchase_order_items poi ON i.itemID = poi.inventoryItemId
                    SET i.itemQuantity = i.itemQuantity + poi.requestedQuantity,
                        i.updatedAt = @updatedAt
                    WHERE poi.purchaseOrderId = @purchaseOrderId;";
                
                await using var updateCmd = new MySqlCommand(updateSql, conn, (MySqlTransaction)tx);
                updateCmd.Parameters.AddWithValue("@updatedAt", DateTime.Now);
                updateCmd.Parameters.AddWithValue("@purchaseOrderId", purchaseOrderId);
                
                await updateCmd.ExecuteNonQueryAsync();
                
                // Log each inventory addition
                foreach (var item in itemsToUpdate)
                {
                    var newQuantity = item.currentQuantity + item.requestedQuantity;
                    
                    var logEntry = new InventoryActivityLog
                    {
                        ItemId = item.itemId,
                        ItemName = item.itemName,
                        ItemCategory = item.itemCategory,
                        Action = "ADDED",
                        QuantityChanged = item.requestedQuantity,
                        PreviousQuantity = item.currentQuantity,
                        NewQuantity = newQuantity,
                        UnitOfMeasurement = item.unitOfMeasurement,
                        Reason = "PURCHASE_ORDER",
                        UserEmail = App.CurrentUser?.Email ?? "System",
                        OrderId = purchaseOrderId.ToString(),
                        Notes = $"Added {item.requestedQuantity} {item.unitOfMeasurement} from purchase order #{purchaseOrderId}"
                    };
                    
                    await LogInventoryActivityAsync(logEntry, conn, tx);
                    System.Diagnostics.Debug.WriteLine($"📝 Logged addition activity for {item.itemName}: {item.currentQuantity} → {newQuantity}");
                }
                
                await tx.CommitAsync();
                System.Diagnostics.Debug.WriteLine($"✅ Inventory updated for purchase order {purchaseOrderId}");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                System.Diagnostics.Debug.WriteLine($"❌ Error updating inventory from purchase order {purchaseOrderId}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Gets the status of a purchase order item (approvedQuantity value)
        /// </summary>
        public async Task<int?> GetPurchaseOrderItemStatusAsync(int purchaseOrderId, int inventoryItemId)
        {
            try
            {
                await using var conn = await GetOpenConnectionAsync();
                var sql = @"SELECT approvedQuantity FROM purchase_order_items 
                           WHERE purchaseOrderId = @PurchaseOrderId AND inventoryItemId = @InventoryItemId;";
                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@PurchaseOrderId", purchaseOrderId);
                cmd.Parameters.AddWithValue("@InventoryItemId", inventoryItemId);
                var result = await cmd.ExecuteScalarAsync();
                if (result == null || result == DBNull.Value)
                    return null;
                return Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error getting purchase order item status: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Retracts an accepted purchase order item - removes the quantity that was added to inventory
        /// </summary>
        public async Task<bool> RetractPurchaseOrderItemAsync(int purchaseOrderId, int inventoryItemId, string retractedBy)
        {
            MySqlConnection? conn = null;
            MySqlTransaction? tx = null;
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔍 RetractPurchaseOrderItemAsync START: PO={purchaseOrderId}, ItemID={inventoryItemId}, User='{retractedBy}'");
                
                conn = await GetOpenConnectionAsync();
                tx = await conn.BeginTransactionAsync();
                
                System.Diagnostics.Debug.WriteLine($"✅ Connection opened and transaction started");

                // Get the approved quantity and quantity multiplier from purchase_order_items to know how much to subtract
                // Also get the original requested UoM from the inventory table (since unitOfMeasurement might have been overwritten when accepting)
                var getPOItemSql = @"SELECT poi.approvedQuantity, poi.unitOfMeasurement, poi.itemName, poi.quantity, i.unitOfMeasurement as inventoryUoM
                                    FROM purchase_order_items poi
                                    INNER JOIN inventory i ON poi.inventoryItemId = i.itemID
                                    WHERE poi.purchaseOrderId = @PurchaseOrderId 
                                      AND poi.inventoryItemId = @InventoryItemId;";
                await using var getPOItemCmd = new MySqlCommand(getPOItemSql, conn, tx);
                getPOItemCmd.Parameters.AddWithValue("@PurchaseOrderId", purchaseOrderId);
                getPOItemCmd.Parameters.AddWithValue("@InventoryItemId", inventoryItemId);
                
                await using var poReader = await getPOItemCmd.ExecuteReaderAsync();
                if (!await poReader.ReadAsync())
                {
                    await poReader.CloseAsync();
                    await tx.RollbackAsync();
                    System.Diagnostics.Debug.WriteLine($"❌ Purchase order item not found for PO={purchaseOrderId}, ItemID={inventoryItemId}");
                    return false;
                }
                
                int approvedQuantityDb = poReader.GetInt32("approvedQuantity");
                string approvedUoM = poReader.IsDBNull(poReader.GetOrdinal("unitOfMeasurement")) ? "" : poReader.GetString("unitOfMeasurement");
                string itemName = poReader.GetString("itemName");
                // Get quantity multiplier - check both quantity column and use 1 as fallback if null or 0
                int quantityOrdinal = poReader.GetOrdinal("quantity");
                int quantityMultiplier = poReader.IsDBNull(quantityOrdinal) ? 1 : Math.Max(1, poReader.GetInt32(quantityOrdinal));
                System.Diagnostics.Debug.WriteLine($"🔍 Retract: Got quantity={quantityMultiplier} from database for item {itemName}");
                // Get the original requested UoM from inventory (this is what was used when creating the purchase order)
                string originalRequestedUoM = poReader.IsDBNull(poReader.GetOrdinal("inventoryUoM")) ? "" : poReader.GetString("inventoryUoM");
                await poReader.CloseAsync();
                
                if (approvedQuantityDb <= 0)
                {
                    await tx.RollbackAsync();
                    System.Diagnostics.Debug.WriteLine($"❌ Item {itemName} was not accepted (approvedQuantity={approvedQuantityDb}), cannot retract");
                    return false;
                }
                
                double approvedQuantity = approvedQuantityDb;
                // Calculate total quantity to subtract: approvedQuantity (units per package) * quantity (number of packages)
                double totalApprovedQuantity = approvedQuantity * quantityMultiplier;

                // Get current inventory item info
                var getItemSql = "SELECT itemID, itemName, itemCategory, itemQuantity, unitOfMeasurement FROM inventory WHERE itemID = @ItemID;";
                await using var getItemCmd = new MySqlCommand(getItemSql, conn, tx);
                getItemCmd.Parameters.AddWithValue("@ItemID", inventoryItemId);

                double currentQuantity = 0;
                string itemCategory = "";
                string inventoryUoM = "";

                await using var reader = await getItemCmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    await reader.CloseAsync();
                    await tx.RollbackAsync();
                    System.Diagnostics.Debug.WriteLine($"❌ Inventory item {inventoryItemId} not found");
                    return false;
                }
                
                currentQuantity = reader.GetDouble("itemQuantity");
                itemCategory = reader.IsDBNull(reader.GetOrdinal("itemCategory")) ? "" : reader.GetString("itemCategory");
                inventoryUoM = reader.IsDBNull(reader.GetOrdinal("unitOfMeasurement")) ? "" : reader.GetString("unitOfMeasurement");
                await reader.CloseAsync();
                
                System.Diagnostics.Debug.WriteLine($"🔍 Retracting item: {itemName} (ID: {inventoryItemId}), Current Qty: {currentQuantity} {inventoryUoM}, Removing: {totalApprovedQuantity} {approvedUoM} (Approved: {approvedQuantity} x Quantity: {quantityMultiplier})");

                // Convert approved quantity to inventory UoM if needed (same conversion as accept)
                double quantityToSubtract = totalApprovedQuantity;
                if (!string.IsNullOrWhiteSpace(approvedUoM) && !string.IsNullOrWhiteSpace(inventoryUoM) && approvedUoM != inventoryUoM)
                {
                    var normalizedApproved = UnitConversionService.Normalize(approvedUoM);
                    var normalizedInventory = UnitConversionService.Normalize(inventoryUoM);
                    
                    if (UnitConversionService.AreCompatibleUnits(normalizedApproved, normalizedInventory))
                    {
                        quantityToSubtract = UnitConversionService.Convert(totalApprovedQuantity, approvedUoM, inventoryUoM);
                        System.Diagnostics.Debug.WriteLine($"✅ Converted {totalApprovedQuantity} {approvedUoM} to {quantityToSubtract} {inventoryUoM} for retraction");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Warning: Incompatible units for item {itemName}: approved {approvedUoM} vs inventory {inventoryUoM}. Using approved quantity as-is.");
                        quantityToSubtract = totalApprovedQuantity;
                    }
                }

                // Check if inventory has enough quantity to subtract
                if (currentQuantity < quantityToSubtract)
                {
                    await tx.RollbackAsync();
                    System.Diagnostics.Debug.WriteLine($"❌ Cannot retract: Current quantity ({currentQuantity}) is less than quantity to subtract ({quantityToSubtract})");
                    return false;
                }

                // Update inventory - subtract the quantity
                System.Diagnostics.Debug.WriteLine($"🔍 Step 1: About to update inventory - ItemID={inventoryItemId}, QuantityToSubtract={quantityToSubtract}");
                var updateSql = "UPDATE inventory SET itemQuantity = itemQuantity - @Quantity WHERE itemID = @ItemID;";
                await using var updateCmd = new MySqlCommand(updateSql, conn, tx);
                updateCmd.Parameters.AddWithValue("@Quantity", quantityToSubtract);
                updateCmd.Parameters.AddWithValue("@ItemID", inventoryItemId);
                System.Diagnostics.Debug.WriteLine($"🔍 Step 2: Executing UPDATE command...");
                var rowsAffected = await updateCmd.ExecuteNonQueryAsync();
                System.Diagnostics.Debug.WriteLine($"🔍 Step 3: UPDATE executed - rowsAffected={rowsAffected}");
                
                if (rowsAffected == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"❌❌❌ CRITICAL: Failed to update inventory - no rows affected for itemID={inventoryItemId}");
                    await tx.RollbackAsync();
                    System.Diagnostics.Debug.WriteLine($"❌ Transaction rolled back due to inventory update failure");
                    return false;
                }
                
                System.Diagnostics.Debug.WriteLine($"✅ Updated inventory: Removed {quantityToSubtract} {inventoryUoM} from {itemName}");

                // Reset the purchase_order_items table - set approvedQuantity to 0 and restore original requested UoM
                System.Diagnostics.Debug.WriteLine($"🔍 Step 4: Resetting purchase_order_items table...");
                var updatePOItemSql = @"UPDATE purchase_order_items 
                                       SET approvedQuantity = 0, 
                                           unitOfMeasurement = @OriginalRequestedUoM
                                       WHERE purchaseOrderId = @PurchaseOrderId 
                                         AND inventoryItemId = @InventoryItemId;";
                await using var updatePOItemCmd = new MySqlCommand(updatePOItemSql, conn, tx);
                // Restore to the original requested UoM (from inventory item's UoM, which was used when creating the purchase order)
                // Use originalRequestedUoM if available, otherwise fall back to inventoryUoM
                string uomToRestore = !string.IsNullOrWhiteSpace(originalRequestedUoM) ? originalRequestedUoM : (inventoryUoM ?? "");
                updatePOItemCmd.Parameters.AddWithValue("@OriginalRequestedUoM", uomToRestore);
                updatePOItemCmd.Parameters.AddWithValue("@PurchaseOrderId", purchaseOrderId);
                updatePOItemCmd.Parameters.AddWithValue("@InventoryItemId", inventoryItemId);
                System.Diagnostics.Debug.WriteLine($"🔍 Restoring unitOfMeasurement to original requested UoM: {uomToRestore}");
                System.Diagnostics.Debug.WriteLine($"🔍 Step 5: Executing purchase_order_items UPDATE...");
                var poRowsAffected = await updatePOItemCmd.ExecuteNonQueryAsync();
                System.Diagnostics.Debug.WriteLine($"🔍 Step 6: purchase_order_items UPDATE executed - rowsAffected={poRowsAffected}");

                // Get new quantity after update
                double newQuantity = currentQuantity - quantityToSubtract;
                try
                {
                    var getNewSql = "SELECT itemQuantity FROM inventory WHERE itemID = @ItemID;";
                    await using var getNewCmd = new MySqlCommand(getNewSql, conn, tx);
                    getNewCmd.Parameters.AddWithValue("@ItemID", inventoryItemId);
                    var result = await getNewCmd.ExecuteScalarAsync();
                    if (result != null && result != DBNull.Value)
                    {
                        newQuantity = Convert.ToDouble(result);
                    }
                }
                catch (Exception qtyEx)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Warning: Could not get new quantity: {qtyEx.Message}. Using calculated value: {newQuantity}");
                }

                // Log the retraction
                var logEntry = new InventoryActivityLog
                {
                    ItemId = inventoryItemId,
                    ItemName = itemName,
                    ItemCategory = itemCategory,
                    Action = "RETRACTED",
                    QuantityChanged = -quantityToSubtract,
                    PreviousQuantity = currentQuantity,
                    NewQuantity = newQuantity,
                    UnitOfMeasurement = inventoryUoM,
                    Reason = "PURCHASE_ORDER_RETRACT",
                    UserEmail = retractedBy,
                    OrderId = purchaseOrderId.ToString(),
                    Notes = $"Retracted accepted item from purchase order #{purchaseOrderId}: {approvedQuantity} {approvedUoM} (converted to {quantityToSubtract} {inventoryUoM})"
                };

                // Log the retraction (non-critical, continue even if logging fails)
                try
                {
                    await LogInventoryActivityAsync(logEntry, conn, tx);
                }
                catch (Exception logEx)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Warning: Failed to log inventory activity: {logEx.Message}");
                }

                // Check the status of all items in the purchase order after retraction
                var checkAllItemsSql = @"SELECT 
                    COUNT(*) as totalItems,
                    SUM(CASE WHEN approvedQuantity > 0 THEN 1 ELSE 0 END) as acceptedItems,
                    SUM(CASE WHEN approvedQuantity = -1 THEN 1 ELSE 0 END) as canceledItems,
                    SUM(CASE WHEN approvedQuantity > 0 OR approvedQuantity = -1 THEN 1 ELSE 0 END) as processedItems
                    FROM purchase_order_items 
                    WHERE purchaseOrderId = @PurchaseOrderId;";
                await using var checkAllItemsCmd = new MySqlCommand(checkAllItemsSql, conn, tx);
                checkAllItemsCmd.Parameters.AddWithValue("@PurchaseOrderId", purchaseOrderId);
                await using var checkReader = await checkAllItemsCmd.ExecuteReaderAsync();
                int totalItems = 0;
                int acceptedItems = 0;
                int canceledItems = 0;
                int processedItems = 0;
                if (await checkReader.ReadAsync())
                {
                    totalItems = checkReader.GetInt32("totalItems");
                    acceptedItems = checkReader.GetInt32("acceptedItems");
                    canceledItems = checkReader.GetInt32("canceledItems");
                    processedItems = checkReader.GetInt32("processedItems");
                }
                await checkReader.CloseAsync();

                // Update purchase order status based on item processing state after retraction
                if (totalItems > 0)
                {
                    string newStatus;
                    if (processedItems >= totalItems)
                    {
                        // All items are processed
                        if (acceptedItems == totalItems && canceledItems == 0)
                        {
                            // All items are accepted, none canceled
                            newStatus = "Approved";
                        }
                        else if (acceptedItems > 0 && canceledItems > 0)
                        {
                            // Some items accepted, some canceled
                            newStatus = "Partially Approved";
                        }
                        else if (canceledItems == totalItems)
                        {
                            // All items are canceled
                            newStatus = "Rejected";
                        }
                        else
                        {
                            // Fallback (shouldn't happen, but just in case)
                            newStatus = "Partially Approved";
                        }
                    }
                    else if (acceptedItems > 0 || canceledItems > 0)
                    {
                        // Some items are processed but not all
                        newStatus = "Partially Approved";
                    }
                    else
                    {
                        // No items processed yet (all retracted back to pending)
                        newStatus = "Pending";
                    }
                    
                    // Only update status if it's different from current
                    var updateOrderStatusSql = @"UPDATE purchase_orders 
                                                SET status = @Status, 
                                                    updatedAt = @UpdatedAt
                                                WHERE purchaseOrderId = @PurchaseOrderId
                                                  AND (status != @Status OR status IS NULL);";
                    await using var updateOrderStatusCmd = new MySqlCommand(updateOrderStatusSql, conn, tx);
                    updateOrderStatusCmd.Parameters.AddWithValue("@Status", newStatus);
                    updateOrderStatusCmd.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
                    updateOrderStatusCmd.Parameters.AddWithValue("@PurchaseOrderId", purchaseOrderId);
                    var statusRowsAffected = await updateOrderStatusCmd.ExecuteNonQueryAsync();
                    System.Diagnostics.Debug.WriteLine($"📊 Updated purchase order status to '{newStatus}' after retraction: {statusRowsAffected} row(s) affected (Total: {totalItems}, Accepted: {acceptedItems}, Canceled: {canceledItems})");
                }

                await tx.CommitAsync();
                System.Diagnostics.Debug.WriteLine($"✅ Item {itemName} retracted from purchase order {purchaseOrderId} - Removed {quantityToSubtract} {inventoryUoM} from inventory");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌");
                System.Diagnostics.Debug.WriteLine($"❌ EXCEPTION CAUGHT in RetractPurchaseOrderItemAsync");
                System.Diagnostics.Debug.WriteLine($"❌ Error message: {ex.Message ?? "(null)"}");
                System.Diagnostics.Debug.WriteLine($"❌ Error type: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"❌ Error source: {ex.Source ?? "(null)"}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace ?? "(null)"}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Inner exception: {ex.InnerException.Message ?? "(null)"}");
                }
                System.Diagnostics.Debug.WriteLine($"❌ Parameters: PO={purchaseOrderId}, ItemID={inventoryItemId}, User='{retractedBy}'");
                System.Diagnostics.Debug.WriteLine($"❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌");
                System.Diagnostics.Debug.WriteLine($"");
                
                try
                {
                    if (tx != null)
                    {
                        await tx.RollbackAsync();
                        System.Diagnostics.Debug.WriteLine($"❌ Transaction rolled back successfully");
                    }
                }
                catch (Exception rollbackEx)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error during rollback: {rollbackEx.Message}");
                }
                
                return false;
            }
        }

        /// <summary>
        /// Accepts a single purchase order item and adds it to inventory
        /// </summary>
        public async Task<bool> AcceptPurchaseOrderItemAsync(int purchaseOrderId, int inventoryItemId, double approvedQuantity, string approvedUoM, string approvedBy, int quantity = 1)
        {
            MySqlConnection? conn = null;
            MySqlTransaction? tx = null;
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔍 AcceptPurchaseOrderItemAsync START: PO={purchaseOrderId}, ItemID={inventoryItemId}, Qty={approvedQuantity}, UoM='{approvedUoM}', User='{approvedBy}'");
                
                conn = await GetOpenConnectionAsync();
                tx = await conn.BeginTransactionAsync();
                
                System.Diagnostics.Debug.WriteLine($"✅ Connection opened and transaction started");

                // Get current inventory item info (including maximumQuantity)
                var getItemSql = "SELECT itemID, itemName, itemCategory, itemQuantity, unitOfMeasurement, maximumQuantity FROM inventory WHERE itemID = @ItemID;";
                await using var getItemCmd = new MySqlCommand(getItemSql, conn, tx);
                getItemCmd.Parameters.AddWithValue("@ItemID", inventoryItemId);

                double currentQuantity = 0;
                double maximumQuantity = 0;
                string itemName = "";
                string itemCategory = "";
                string inventoryUoM = "";

                await using var reader = await getItemCmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    await reader.CloseAsync();
                    await tx.RollbackAsync();
                    System.Diagnostics.Debug.WriteLine($"❌ Inventory item {inventoryItemId} not found");
                    return false;
                }
                
                currentQuantity = reader.GetDouble("itemQuantity");
                itemName = reader.GetString("itemName");
                itemCategory = reader.IsDBNull(reader.GetOrdinal("itemCategory")) ? "" : reader.GetString("itemCategory");
                inventoryUoM = reader.IsDBNull(reader.GetOrdinal("unitOfMeasurement")) ? "" : reader.GetString("unitOfMeasurement");
                
                // Get maximumQuantity (may be NULL or 0)
                var maxQtyOrdinal = reader.GetOrdinal("maximumQuantity");
                maximumQuantity = reader.IsDBNull(maxQtyOrdinal) ? 0 : reader.GetDouble(maxQtyOrdinal);
                await reader.CloseAsync();
                
                // Calculate total quantity to add: approvedQuantity (units per package) * quantity (number of packages)
                double totalApprovedQuantity = approvedQuantity * quantity;
                System.Diagnostics.Debug.WriteLine($"🔍 Accepting item: {itemName} (ID: {inventoryItemId}), Current Qty: {currentQuantity} {inventoryUoM}, Adding: {totalApprovedQuantity} {approvedUoM} (Approved: {approvedQuantity} x Quantity: {quantity})");

                // Convert approved quantity to inventory UoM if needed
                double quantityToAdd = totalApprovedQuantity;
                if (!string.IsNullOrWhiteSpace(approvedUoM) && !string.IsNullOrWhiteSpace(inventoryUoM) && approvedUoM != inventoryUoM)
                {
                    // Check if units are compatible before converting
                    var normalizedApproved = UnitConversionService.Normalize(approvedUoM);
                    var normalizedInventory = UnitConversionService.Normalize(inventoryUoM);
                    
                    if (UnitConversionService.AreCompatibleUnits(normalizedApproved, normalizedInventory))
                    {
                        quantityToAdd = UnitConversionService.Convert(totalApprovedQuantity, approvedUoM, inventoryUoM);
                        System.Diagnostics.Debug.WriteLine($"✅ Converted {totalApprovedQuantity} {approvedUoM} to {quantityToAdd} {inventoryUoM}");
                    }
                    else
                    {
                        // Units are incompatible, but for purchase orders, we might allow direct addition
                        // Log a warning but proceed with the approved quantity (assuming 1:1 for incompatible units)
                        System.Diagnostics.Debug.WriteLine($"⚠️ Warning: Incompatible units for item {itemName}: approved {approvedUoM} vs inventory {inventoryUoM}. Using approved quantity as-is.");
                        quantityToAdd = totalApprovedQuantity;
                    }
                }

                // Calculate new quantity after adding
                double newQuantity = currentQuantity + quantityToAdd;
                
                // Check if new quantity exceeds maximum stock, and adjust maximum if needed
                if (maximumQuantity > 0 && newQuantity > maximumQuantity)
                {
                    // Automatically adjust maximum stock to accommodate the new quantity
                    System.Diagnostics.Debug.WriteLine($"📊 New quantity ({newQuantity}) exceeds maximum stock ({maximumQuantity}). Adjusting maximum stock to {newQuantity}.");
                    maximumQuantity = newQuantity;
                }
                
                // Update inventory (quantity and maximumQuantity if adjusted)
                System.Diagnostics.Debug.WriteLine($"🔍 Step 1: About to update inventory - ItemID={inventoryItemId}, QuantityToAdd={quantityToAdd}, NewMax={maximumQuantity}");
                var updateSql = "UPDATE inventory SET itemQuantity = itemQuantity + @Quantity, maximumQuantity = @MaximumQuantity WHERE itemID = @ItemID;";
                await using var updateCmd = new MySqlCommand(updateSql, conn, tx);
                updateCmd.Parameters.AddWithValue("@Quantity", quantityToAdd);
                updateCmd.Parameters.AddWithValue("@MaximumQuantity", maximumQuantity);
                updateCmd.Parameters.AddWithValue("@ItemID", inventoryItemId);
                System.Diagnostics.Debug.WriteLine($"🔍 Step 2: Executing UPDATE command...");
                var rowsAffected = await updateCmd.ExecuteNonQueryAsync();
                System.Diagnostics.Debug.WriteLine($"🔍 Step 3: UPDATE executed - rowsAffected={rowsAffected}");
                
                if (rowsAffected == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"❌❌❌ CRITICAL: Failed to update inventory - no rows affected for itemID={inventoryItemId}");
                    System.Diagnostics.Debug.WriteLine($"❌ Attempted to add {quantityToAdd} {inventoryUoM} to item: {itemName}");
                    await tx.RollbackAsync();
                    System.Diagnostics.Debug.WriteLine($"❌ Transaction rolled back due to inventory update failure");
                    return false;
                }
                
                System.Diagnostics.Debug.WriteLine($"✅ Updated inventory: Added {quantityToAdd} {inventoryUoM} to {itemName}");

                // Update the purchase_order_items table with approved quantity and UoM
                System.Diagnostics.Debug.WriteLine($"🔍 Step 4: Updating purchase_order_items table...");
                var updatePOItemSql = @"UPDATE purchase_order_items 
                                       SET approvedQuantity = @ApprovedQuantity, 
                                           unitOfMeasurement = @ApprovedUoM,
                                           quantity = @Quantity
                                       WHERE purchaseOrderId = @PurchaseOrderId 
                                         AND inventoryItemId = @InventoryItemId;";
                await using var updatePOItemCmd = new MySqlCommand(updatePOItemSql, conn, tx);
                updatePOItemCmd.Parameters.AddWithValue("@ApprovedQuantity", (int)Math.Round(approvedQuantity));
                updatePOItemCmd.Parameters.AddWithValue("@ApprovedUoM", approvedUoM ?? "");
                updatePOItemCmd.Parameters.AddWithValue("@Quantity", quantity);
                updatePOItemCmd.Parameters.AddWithValue("@PurchaseOrderId", purchaseOrderId);
                updatePOItemCmd.Parameters.AddWithValue("@InventoryItemId", inventoryItemId);
                System.Diagnostics.Debug.WriteLine($"🔍 Step 5: Executing purchase_order_items UPDATE...");
                var poRowsAffected = await updatePOItemCmd.ExecuteNonQueryAsync();
                System.Diagnostics.Debug.WriteLine($"🔍 Step 6: purchase_order_items UPDATE executed - rowsAffected={poRowsAffected}");
                
                if (poRowsAffected == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Warning: No purchase_order_items row found for PO={purchaseOrderId}, ItemID={inventoryItemId}. Continuing anyway.");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"✅ Updated purchase_order_items: {poRowsAffected} row(s) affected");
                }

                // Get new quantity after update (verify it matches our calculation)
                try
                {
                    var getNewSql = "SELECT itemQuantity FROM inventory WHERE itemID = @ItemID;";
                    await using var getNewCmd = new MySqlCommand(getNewSql, conn, tx);
                    getNewCmd.Parameters.AddWithValue("@ItemID", inventoryItemId);
                    var result = await getNewCmd.ExecuteScalarAsync();
                    if (result != null && result != DBNull.Value)
                    {
                        var dbNewQuantity = Convert.ToDouble(result);
                        if (Math.Abs(dbNewQuantity - newQuantity) > 0.01) // Allow small floating point differences
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ Warning: Calculated new quantity ({newQuantity}) differs from database value ({dbNewQuantity}). Using database value.");
                            newQuantity = dbNewQuantity;
                        }
                    }
                }
                catch (Exception qtyEx)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Warning: Could not get new quantity: {qtyEx.Message}. Using calculated value: {newQuantity}");
                }

                // Send notification for inventory addition from purchase order
                await SendInventoryUpdateNotificationAsync(itemName, currentQuantity, newQuantity, quantityToAdd, inventoryUoM, "ADDED", $"Purchase Order #{purchaseOrderId}");

                // Log the addition
                var logEntry = new InventoryActivityLog
                {
                    ItemId = inventoryItemId,
                    ItemName = itemName,
                    ItemCategory = itemCategory,
                    Action = "ADDED",
                    QuantityChanged = quantityToAdd,
                    PreviousQuantity = currentQuantity,
                    NewQuantity = newQuantity,
                    UnitOfMeasurement = inventoryUoM,
                    Reason = "PURCHASE_ORDER",
                    UserEmail = approvedBy,
                    OrderId = purchaseOrderId.ToString(),
                    Notes = $"Accepted item from purchase order #{purchaseOrderId}: {approvedQuantity} {approvedUoM} (converted to {quantityToAdd} {inventoryUoM})"
                };

                // Log the addition (non-critical, continue even if logging fails)
                try
                {
                    await LogInventoryActivityAsync(logEntry, conn, tx);
                }
                catch (Exception logEx)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Warning: Failed to log inventory activity: {logEx.Message}");
                    // Continue anyway - the inventory update is more important than logging
                }

                // Check the status of all items in the purchase order
                var checkAllItemsSql = @"SELECT 
                    COUNT(*) as totalItems,
                    SUM(CASE WHEN approvedQuantity > 0 THEN 1 ELSE 0 END) as acceptedItems,
                    SUM(CASE WHEN approvedQuantity = -1 THEN 1 ELSE 0 END) as canceledItems,
                    SUM(CASE WHEN approvedQuantity > 0 OR approvedQuantity = -1 THEN 1 ELSE 0 END) as processedItems
                    FROM purchase_order_items 
                    WHERE purchaseOrderId = @PurchaseOrderId;";
                await using var checkAllItemsCmd = new MySqlCommand(checkAllItemsSql, conn, tx);
                checkAllItemsCmd.Parameters.AddWithValue("@PurchaseOrderId", purchaseOrderId);
                await using var checkReader = await checkAllItemsCmd.ExecuteReaderAsync();
                int totalItems = 0;
                int acceptedItems = 0;
                int canceledItems = 0;
                int processedItems = 0;
                if (await checkReader.ReadAsync())
                {
                    totalItems = checkReader.GetInt32("totalItems");
                    acceptedItems = checkReader.GetInt32("acceptedItems");
                    canceledItems = checkReader.GetInt32("canceledItems");
                    processedItems = checkReader.GetInt32("processedItems");
                }
                await checkReader.CloseAsync();

                // Update purchase order status based on item processing state
                if (totalItems > 0)
                {
                    string newStatus;
                    if (processedItems >= totalItems)
                    {
                        // All items are processed
                        if (acceptedItems == totalItems && canceledItems == 0)
                        {
                            // All items are accepted, none canceled
                            newStatus = "Approved";
                        }
                        else if (acceptedItems > 0 && canceledItems > 0)
                        {
                            // Some items accepted, some canceled
                            newStatus = "Partially Approved";
                        }
                        else if (canceledItems == totalItems)
                        {
                            // All items are canceled
                            newStatus = "Rejected";
                        }
                        else
                        {
                            // Fallback (shouldn't happen, but just in case)
                            newStatus = "Partially Approved";
                        }
                    }
                    else if (acceptedItems > 0 || canceledItems > 0)
                    {
                        // Some items are processed but not all
                        newStatus = "Partially Approved";
                    }
                    else
                    {
                        // No items processed yet
                        newStatus = "Pending";
                    }
                    
                    // Only update status if it's different from current
                    var updateOrderStatusSql = @"UPDATE purchase_orders 
                                                SET status = @Status, 
                                                    approvedBy = @ApprovedBy, 
                                                    approvedDate = @ApprovedDate,
                                                    updatedAt = @UpdatedAt
                                                WHERE purchaseOrderId = @PurchaseOrderId
                                                  AND (status != @Status OR status IS NULL);";
                    await using var updateOrderStatusCmd = new MySqlCommand(updateOrderStatusSql, conn, tx);
                    updateOrderStatusCmd.Parameters.AddWithValue("@Status", newStatus);
                    updateOrderStatusCmd.Parameters.AddWithValue("@ApprovedBy", approvedBy);
                    updateOrderStatusCmd.Parameters.AddWithValue("@ApprovedDate", DateTime.Now);
                    updateOrderStatusCmd.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
                    updateOrderStatusCmd.Parameters.AddWithValue("@PurchaseOrderId", purchaseOrderId);
                    var statusRowsAffected = await updateOrderStatusCmd.ExecuteNonQueryAsync();
                    System.Diagnostics.Debug.WriteLine($"📊 Updated purchase order status to '{newStatus}': {statusRowsAffected} row(s) affected (Total: {totalItems}, Accepted: {acceptedItems}, Canceled: {canceledItems})");
                }

                await tx.CommitAsync();
                System.Diagnostics.Debug.WriteLine($"✅ Item {itemName} accepted from purchase order {purchaseOrderId} - Added {quantityToAdd} {inventoryUoM} to inventory");
                return true;
            }
            catch (Exception ex)
            {
                // Log error FIRST before any rollback attempts
                System.Diagnostics.Debug.WriteLine($"");
                System.Diagnostics.Debug.WriteLine($"❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌");
                System.Diagnostics.Debug.WriteLine($"❌ EXCEPTION CAUGHT in AcceptPurchaseOrderItemAsync");
                System.Diagnostics.Debug.WriteLine($"❌ Error message: {ex.Message ?? "(null)"}");
                System.Diagnostics.Debug.WriteLine($"❌ Error type: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"❌ Error source: {ex.Source ?? "(null)"}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace ?? "(null)"}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Inner exception: {ex.InnerException.Message ?? "(null)"}");
                    System.Diagnostics.Debug.WriteLine($"❌ Inner exception type: {ex.InnerException.GetType().Name}");
                    System.Diagnostics.Debug.WriteLine($"❌ Inner exception source: {ex.InnerException.Source ?? "(null)"}");
                    System.Diagnostics.Debug.WriteLine($"❌ Inner stack trace: {ex.InnerException.StackTrace ?? "(null)"}");
                }
                System.Diagnostics.Debug.WriteLine($"❌ Parameters: PO={purchaseOrderId}, ItemID={inventoryItemId}, Qty={approvedQuantity}, UoM='{approvedUoM}', User='{approvedBy}'");
                System.Diagnostics.Debug.WriteLine($"❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌");
                System.Diagnostics.Debug.WriteLine($"");
                
                // Try to rollback transaction if it's still open
                try
                {
                    if (tx != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"🔍 Attempting to rollback transaction...");
                        await tx.RollbackAsync();
                        System.Diagnostics.Debug.WriteLine($"❌ Transaction rolled back successfully");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Transaction was null, cannot rollback");
                    }
                }
                catch (Exception rollbackEx)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error during rollback: {rollbackEx.Message}");
                    System.Diagnostics.Debug.WriteLine($"❌ Rollback stack trace: {rollbackEx.StackTrace}");
                }
                
                return false;
            }
        }

        /// <summary>
        /// Cancels a single purchase order item (does not add to inventory)
        /// </summary>
        public async Task<bool> CancelPurchaseOrderItemAsync(int purchaseOrderId, int inventoryItemId, string canceledBy)
        {
            try
            {
                await using var conn = await GetOpenConnectionAsync();
                await using var tx = await conn.BeginTransactionAsync();

                // Mark the item as canceled by setting approvedQuantity to -1
                var updatePOItemSql = @"UPDATE purchase_order_items 
                                       SET approvedQuantity = -1,
                                           unitOfMeasurement = COALESCE(unitOfMeasurement, '')
                                       WHERE purchaseOrderId = @PurchaseOrderId 
                                         AND inventoryItemId = @InventoryItemId;";
                await using var updatePOItemCmd = new MySqlCommand(updatePOItemSql, conn, (MySqlTransaction)tx);
                updatePOItemCmd.Parameters.AddWithValue("@PurchaseOrderId", purchaseOrderId);
                updatePOItemCmd.Parameters.AddWithValue("@InventoryItemId", inventoryItemId);
                var rowsAffected = await updatePOItemCmd.ExecuteNonQueryAsync();

                // Check the status of all items in the purchase order
                var checkAllItemsSql = @"SELECT 
                    COUNT(*) as totalItems,
                    SUM(CASE WHEN approvedQuantity > 0 THEN 1 ELSE 0 END) as acceptedItems,
                    SUM(CASE WHEN approvedQuantity = -1 THEN 1 ELSE 0 END) as canceledItems,
                    SUM(CASE WHEN approvedQuantity > 0 OR approvedQuantity = -1 THEN 1 ELSE 0 END) as processedItems
                    FROM purchase_order_items 
                    WHERE purchaseOrderId = @PurchaseOrderId;";
                await using var checkAllItemsCmd = new MySqlCommand(checkAllItemsSql, conn, (MySqlTransaction)tx);
                checkAllItemsCmd.Parameters.AddWithValue("@PurchaseOrderId", purchaseOrderId);
                await using var checkReader = await checkAllItemsCmd.ExecuteReaderAsync();
                int totalItems = 0;
                int acceptedItems = 0;
                int canceledItems = 0;
                int processedItems = 0;
                if (await checkReader.ReadAsync())
                {
                    totalItems = checkReader.GetInt32("totalItems");
                    acceptedItems = checkReader.GetInt32("acceptedItems");
                    canceledItems = checkReader.GetInt32("canceledItems");
                    processedItems = checkReader.GetInt32("processedItems");
                }
                await checkReader.CloseAsync();

                // Update purchase order status based on item processing state
                if (totalItems > 0)
                {
                    string newStatus;
                    if (processedItems >= totalItems)
                    {
                        // All items are processed
                        if (acceptedItems == totalItems && canceledItems == 0)
                        {
                            // All items are accepted, none canceled
                            newStatus = "Approved";
                        }
                        else if (acceptedItems > 0 && canceledItems > 0)
                        {
                            // Some items accepted, some canceled
                            newStatus = "Partially Approved";
                        }
                        else if (canceledItems == totalItems)
                        {
                            // All items are canceled
                            newStatus = "Rejected";
                        }
                        else
                        {
                            // Fallback (shouldn't happen, but just in case)
                            newStatus = "Partially Approved";
                        }
                    }
                    else if (acceptedItems > 0 || canceledItems > 0)
                    {
                        // Some items are processed but not all
                        newStatus = "Partially Approved";
                    }
                    else
                    {
                        // No items processed yet
                        newStatus = "Pending";
                    }
                    
                    // Only update status if it's different from current
                    var updateOrderStatusSql = @"UPDATE purchase_orders 
                                                SET status = @Status, 
                                                    approvedBy = @CanceledBy, 
                                                    approvedDate = @ApprovedDate,
                                                    updatedAt = @UpdatedAt
                                                WHERE purchaseOrderId = @PurchaseOrderId
                                                  AND (status != @Status OR status IS NULL);";
                    await using var updateOrderStatusCmd = new MySqlCommand(updateOrderStatusSql, conn, (MySqlTransaction)tx);
                    updateOrderStatusCmd.Parameters.AddWithValue("@Status", newStatus);
                    updateOrderStatusCmd.Parameters.AddWithValue("@CanceledBy", canceledBy);
                    updateOrderStatusCmd.Parameters.AddWithValue("@ApprovedDate", DateTime.Now);
                    updateOrderStatusCmd.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
                    updateOrderStatusCmd.Parameters.AddWithValue("@PurchaseOrderId", purchaseOrderId);
                    var statusRowsAffected = await updateOrderStatusCmd.ExecuteNonQueryAsync();
                    System.Diagnostics.Debug.WriteLine($"📊 Updated purchase order status to '{newStatus}': {statusRowsAffected} row(s) affected (Total: {totalItems}, Accepted: {acceptedItems}, Canceled: {canceledItems})");
                }

                await tx.CommitAsync();
                System.Diagnostics.Debug.WriteLine($"✅ Item canceled from purchase order {purchaseOrderId}");
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error canceling purchase order item: {ex.Message}");
                return false;
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

        // ===================== Processing Queue =====================
        public async Task<int> SaveProcessingItemAsync(ViewModel.Controls.ProcessingItemModel item)
        {
            await using var conn = await GetOpenConnectionAsync();
            
            try
            {
                var ingredientsJson = System.Text.Json.JsonSerializer.Serialize(item.Ingredients);
                var addonsJson = System.Text.Json.JsonSerializer.Serialize(item.Addons);
                
                var sql = @"INSERT INTO processing_queue 
                    (productID, productName, size, sizeDisplay, orderGroupId, quantity, unitPrice, addonPrice, ingredients, addons) 
                    VALUES (@ProductID, @ProductName, @Size, @SizeDisplay, @OrderGroupId, @Quantity, @UnitPrice, @AddonPrice, @Ingredients, @Addons);";
                
                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ProductID", item.ProductID);
                cmd.Parameters.AddWithValue("@ProductName", item.ProductName);
                cmd.Parameters.AddWithValue("@Size", item.Size);
                cmd.Parameters.AddWithValue("@SizeDisplay", item.SizeDisplay ?? "");
                cmd.Parameters.AddWithValue("@OrderGroupId", item.OrderGroupId ?? Guid.NewGuid().ToString());
                cmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                cmd.Parameters.AddWithValue("@UnitPrice", item.UnitPrice);
                cmd.Parameters.AddWithValue("@AddonPrice", item.AddonPrice);
                cmd.Parameters.AddWithValue("@Ingredients", ingredientsJson);
                cmd.Parameters.AddWithValue("@Addons", addonsJson);
                
                await cmd.ExecuteNonQueryAsync();
                return (int)cmd.LastInsertedId;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error saving processing item: {ex.Message}");
                throw;
            }
        }

        public async Task<List<ViewModel.Controls.ProcessingItemModel>> GetPendingProcessingItemsAsync()
        {
            await using var conn = await GetOpenConnectionAsync();
            var items = new List<ViewModel.Controls.ProcessingItemModel>();
            
            try
            {
                var sql = "SELECT * FROM processing_queue ORDER BY createdAt ASC;";
                await using var cmd = new MySqlCommand(sql, conn);
                await using var reader = await cmd.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    var ingredientsJson = reader.IsDBNull(reader.GetOrdinal("ingredients")) ? "[]" : reader.GetString("ingredients");
                    var addonsJson = reader.IsDBNull(reader.GetOrdinal("addons")) ? "[]" : reader.GetString("addons");
                    
                    var ingredients = System.Text.Json.JsonSerializer.Deserialize<List<ViewModel.Controls.IngredientDisplayModel>>(ingredientsJson) ?? new();
                    var addons = System.Text.Json.JsonSerializer.Deserialize<List<ViewModel.Controls.AddonDisplayModel>>(addonsJson) ?? new();
                    
                    items.Add(new ViewModel.Controls.ProcessingItemModel
                    {
                        Id = reader.GetInt32("id"),
                        ProductID = reader.GetInt32("productID"),
                        ProductName = reader.GetString("productName"),
                        Size = reader.GetString("size"),
                        SizeDisplay = reader.IsDBNull(reader.GetOrdinal("sizeDisplay")) ? "" : reader.GetString("sizeDisplay"),
                        OrderGroupId = reader.IsDBNull(reader.GetOrdinal("orderGroupId")) ? null : reader.GetString("orderGroupId"),
                        Quantity = reader.GetInt32("quantity"),
                        UnitPrice = reader.GetDecimal("unitPrice"),
                        AddonPrice = reader.GetDecimal("addonPrice"),
                        Ingredients = ingredients,
                        Addons = addons
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading pending items: {ex.Message}");
            }
            
            return items;
        }

        public async Task<bool> DeleteProcessingItemAsync(int id)
        {
            await using var conn = await GetOpenConnectionAsync();
            
            try
            {
                var sql = "DELETE FROM processing_queue WHERE id = @Id;";
                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                
                var rows = await cmd.ExecuteNonQueryAsync();
                return rows > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error deleting processing item: {ex.Message}");
                return false;
            }
        }

        // ===================== Image Storage =====================
        /// <summary>
        /// Gets product image bytes from database and restores to app data directory if missing
        /// </summary>
        public async Task<byte[]?> GetProductImageAsync(int productId)
        {
            try
            {
                await using var conn = await GetOpenConnectionAsync();
                var sql = "SELECT imageData, imageSet FROM products WHERE productID = @ProductID;";
                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ProductID", productId);

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var imageSet = reader.IsDBNull(reader.GetOrdinal("imageSet")) ? null : reader.GetString("imageSet");
                    
                    // If imageData exists in database, return it and restore to app data directory
                    if (!reader.IsDBNull(reader.GetOrdinal("imageData")))
                    {
                        var imageBytes = (byte[])reader["imageData"];
                        
                        // Restore to app data directory if file doesn't exist
                        if (!string.IsNullOrWhiteSpace(imageSet))
                        {
                            var imagePath = Services.ImagePersistenceService.GetImagePath(imageSet);
                            if (!File.Exists(imagePath) && imageBytes != null && imageBytes.Length > 0)
                            {
                                await Services.ImagePersistenceService.EnsureImagesDirectoryExistsAsync();
                                await File.WriteAllBytesAsync(imagePath, imageBytes);
                                System.Diagnostics.Debug.WriteLine($"✅ Restored product image to app data: {imageSet}");
                            }
                        }
                        
                        return imageBytes;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error getting product image: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets inventory item image bytes from database and restores to app data directory if missing
        /// </summary>
        public async Task<byte[]?> GetInventoryImageAsync(int itemId)
        {
            try
            {
                await using var conn = await GetOpenConnectionAsync();
                var sql = "SELECT imageData, imageSet FROM inventory WHERE itemID = @ItemID;";
                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ItemID", itemId);

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var imageSet = reader.IsDBNull(reader.GetOrdinal("imageSet")) ? null : reader.GetString("imageSet");
                    
                    // If imageData exists in database, return it and restore to app data directory
                    if (!reader.IsDBNull(reader.GetOrdinal("imageData")))
                    {
                        var imageBytes = (byte[])reader["imageData"];
                        
                        // Restore to app data directory if file doesn't exist
                        if (!string.IsNullOrWhiteSpace(imageSet))
                        {
                            var imagePath = Services.ImagePersistenceService.GetImagePath(imageSet);
                            if (!File.Exists(imagePath) && imageBytes != null && imageBytes.Length > 0)
                            {
                                await Services.ImagePersistenceService.EnsureImagesDirectoryExistsAsync();
                                await File.WriteAllBytesAsync(imagePath, imageBytes);
                                System.Diagnostics.Debug.WriteLine($"✅ Restored inventory image to app data: {imageSet}");
                            }
                        }
                        
                        return imageBytes;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error getting inventory image: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets user profile image bytes from database and restores to app data directory if missing
        /// </summary>
        public async Task<byte[]?> GetUserProfileImageAsync(int userId)
        {
            try
            {
                await using var conn = await GetOpenConnectionAsync();
                var sql = "SELECT imageData, profileImage FROM users WHERE id = @UserId;";
                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@UserId", userId);

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var profileImage = reader.IsDBNull(reader.GetOrdinal("profileImage")) ? null : reader.GetString("profileImage");
                    
                    // If imageData exists in database, return it and restore to app data directory
                    if (!reader.IsDBNull(reader.GetOrdinal("imageData")))
                    {
                        var imageBytes = (byte[])reader["imageData"];
                        
                        // Restore to app data directory if file doesn't exist
                        if (!string.IsNullOrWhiteSpace(profileImage) && profileImage != "usericon.png")
                        {
                            var imagePath = Services.ImagePersistenceService.GetImagePath(profileImage);
                            if (!File.Exists(imagePath) && imageBytes != null && imageBytes.Length > 0)
                            {
                                await Services.ImagePersistenceService.EnsureImagesDirectoryExistsAsync();
                                await File.WriteAllBytesAsync(imagePath, imageBytes);
                                System.Diagnostics.Debug.WriteLine($"✅ Restored user profile image to app data: {profileImage}");
                            }
                        }
                        
                        return imageBytes;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error getting user profile image: {ex.Message}");
            }
            return null;
        }
    }

    public class PaymentMethodProductData
    {
        public string ProductName { get; set; } = string.Empty;
        public string? Size { get; set; }
        public int Quantity { get; set; }
        public decimal Revenue { get; set; }
    }
}
