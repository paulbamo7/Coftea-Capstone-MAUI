using Coftea_Capstone.Models;
using MySqlConnector;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Coftea_Capstone.C_
{
    public class Database
    {
        private readonly string _db;

        public Database(string host = "localhost",
                        string database = "coftea_db",
                        string user = "root",
                        string password = "")
        {
            // Use localhost for Windows, 10.0.2.2 for Android emulator
            var server = DeviceInfo.Platform == DevicePlatform.Android ? "10.0.2.2" : "localhost";
            
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
        private async Task<MySqlConnection> GetOpenConnectionAsync()
        {
            var conn = new MySqlConnection(_db);
            await conn.OpenAsync();
            return conn;
        }

        public async Task InitializeDatabaseAsync()
        {
            await using var conn = await GetOpenConnectionAsync();

            // Create users table
            var createUsersTable = @"
                CREATE TABLE IF NOT EXISTS users (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    firstName VARCHAR(255) NOT NULL,
                    lastName VARCHAR(255) NOT NULL,
                    email VARCHAR(255) UNIQUE NOT NULL,
                    password VARCHAR(255) NOT NULL,
                    phoneNumber VARCHAR(20),
                    birthday DATE,
                    address TEXT,
                    status VARCHAR(20) DEFAULT 'pending',
                    isAdmin BOOLEAN DEFAULT FALSE,
                    reset_token VARCHAR(255),
                    reset_expiry DATETIME,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
                );";

            // Create pending_registrations table
            var createPendingRegistrationsTable = @"
                CREATE TABLE IF NOT EXISTS pending_registrations (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    email VARCHAR(255) NOT NULL,
                    password VARCHAR(255) NOT NULL,
                    firstName VARCHAR(255) NOT NULL,
                    lastName VARCHAR(255) NOT NULL,
                    phoneNumber VARCHAR(20),
                    address TEXT,
                    birthday DATE,
                    registrationDate DATETIME DEFAULT CURRENT_TIMESTAMP
                );";

            // Create products table
            var createProductsTable = @"
                CREATE TABLE IF NOT EXISTS products (
                    productID INT AUTO_INCREMENT PRIMARY KEY,
                    productName VARCHAR(255) NOT NULL,
                    smallPrice DECIMAL(10,2) NOT NULL,
                    mediumPrice DECIMAL(10,2) NOT NULL,
                    largePrice DECIMAL(10,2) NOT NULL,
                    category VARCHAR(100),
                    subcategory VARCHAR(100),
                    imageSet VARCHAR(255),
                    description TEXT,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
                );";

            // Create inventory table
            var createInventoryTable = @"
                CREATE TABLE IF NOT EXISTS inventory (
                    itemID INT AUTO_INCREMENT PRIMARY KEY,
                    itemName VARCHAR(255) NOT NULL,
                    itemQuantity DECIMAL(10,2) DEFAULT 0,
                    itemCategory VARCHAR(100),
                    imageSet VARCHAR(255),
                    itemDescription TEXT,
                    unitOfMeasurement VARCHAR(50),
                    minimumQuantity DECIMAL(10,2) DEFAULT 0,
                    maximumStockLevel DECIMAL(10,2) DEFAULT 0,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
                );";

            // Create transaction_history table
            var createTransactionHistoryTable = @"
                CREATE TABLE IF NOT EXISTS transaction_history (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    transactionId INT,
                    drinkName VARCHAR(255) NOT NULL,
                    quantity INT NOT NULL,
                    size VARCHAR(50),
                    addOns TEXT,
                    price DECIMAL(10,2) NOT NULL,
                    vat DECIMAL(10,2) DEFAULT 0,
                    total DECIMAL(10,2) NOT NULL,
                    transactionDate DATETIME NOT NULL,
                    customerName VARCHAR(255),
                    paymentMethod VARCHAR(50),
                    status VARCHAR(50) DEFAULT 'completed',
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                );";

            // Create product_ingredients table
            var createProductIngredientsTable = @"
                CREATE TABLE IF NOT EXISTS product_ingredients (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    productID INT NOT NULL,
                    itemID INT NOT NULL,
                    amount DECIMAL(10,2) NOT NULL,
                    unit VARCHAR(50),
                    role VARCHAR(50) DEFAULT 'ingredient',
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (productID) REFERENCES products(productID) ON DELETE CASCADE,
                    FOREIGN KEY (itemID) REFERENCES inventory(itemID) ON DELETE CASCADE
                );";

            // Execute all table creation commands
            await using var cmd1 = new MySqlCommand(createUsersTable, conn);
            await cmd1.ExecuteNonQueryAsync();

            await using var cmd2 = new MySqlCommand(createPendingRegistrationsTable, conn);
            await cmd2.ExecuteNonQueryAsync();

            await using var cmd3 = new MySqlCommand(createProductsTable, conn);
            await cmd3.ExecuteNonQueryAsync();

            await using var cmd4 = new MySqlCommand(createInventoryTable, conn);
            await cmd4.ExecuteNonQueryAsync();

            await using var cmd5 = new MySqlCommand(createTransactionHistoryTable, conn);
            await cmd5.ExecuteNonQueryAsync();

            await using var cmd6 = new MySqlCommand(createProductIngredientsTable, conn);
            await cmd6.ExecuteNonQueryAsync();

            // Add maximumStockLevel column to existing inventory table if it doesn't exist
            await AddMaximumStockLevelColumnAsync(conn);
            
            // Fix any items where maximumStockLevel is 0 or less than current quantity
            await FixMaximumStockLevelsAsync(conn);

            // Add default cup sizes to inventory
            await AddDefaultCupSizesAsync(conn);
        }

        private async Task AddMaximumStockLevelColumnAsync(MySqlConnection conn)
        {
            try
            {
                // Check if maximumStockLevel column exists
                var checkColumnSql = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'inventory' 
                    AND COLUMN_NAME = 'maximumStockLevel'";
                
                await using var checkCmd = new MySqlCommand(checkColumnSql, conn);
                var columnExists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;

                if (!columnExists)
                {
                    // Add the column
                    var addColumnSql = "ALTER TABLE inventory ADD COLUMN maximumStockLevel DECIMAL(10,2) DEFAULT 0;";
                    await using var addCmd = new MySqlCommand(addColumnSql, conn);
                    await addCmd.ExecuteNonQueryAsync();

                    // Initialize maximumStockLevel with current itemQuantity for existing items
                    var updateSql = "UPDATE inventory SET maximumStockLevel = itemQuantity WHERE maximumStockLevel = 0;";
                    await using var updateCmd = new MySqlCommand(updateSql, conn);
                    await updateCmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the initialization
                System.Diagnostics.Debug.WriteLine($"Error adding maximumStockLevel column: {ex.Message}");
            }
        }

        private async Task FixMaximumStockLevelsAsync(MySqlConnection conn)
        {
            try
            {
                // Fix items where maximumStockLevel is 0 or less than current quantity
                var fixSql = "UPDATE inventory SET maximumStockLevel = itemQuantity WHERE maximumStockLevel = 0 OR maximumStockLevel < itemQuantity;";
                await using var fixCmd = new MySqlCommand(fixSql, conn);
                await fixCmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                // Log error but don't fail the initialization
                System.Diagnostics.Debug.WriteLine($"Error fixing maximumStockLevels: {ex.Message}");
            }
        }

        private async Task AddDefaultCupSizesAsync(MySqlConnection conn)
        {
            // Check if cup sizes already exist
            var checkSql = "SELECT COUNT(*) FROM inventory WHERE itemName IN ('Small Cup', 'Medium Cup', 'Large Cup');";
            await using var checkCmd = new MySqlCommand(checkSql, conn);
            var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

            if (count == 0)
            {
                // Insert Small Cup
                var smallCupSql = "INSERT INTO inventory (itemName, itemQuantity, itemCategory, imageSet, itemDescription, unitOfMeasurement, minimumQuantity) " +
                                 "VALUES ('Small Cup', 1, 'Supplies', 'cup_small.png', 'Small size cup for beverages', 'pcs', 1);";
                await using var smallCupCmd = new MySqlCommand(smallCupSql, conn);
                await smallCupCmd.ExecuteNonQueryAsync();

                // Insert Medium Cup
                var mediumCupSql = "INSERT INTO inventory (itemName, itemQuantity, itemCategory, imageSet, itemDescription, unitOfMeasurement, minimumQuantity) " +
                                  "VALUES ('Medium Cup', 1, 'Supplies', 'cup_medium.png', 'Medium size cup for beverages', 'pcs', 1);";
                await using var mediumCupCmd = new MySqlCommand(mediumCupSql, conn);
                await mediumCupCmd.ExecuteNonQueryAsync();

                // Insert Large Cup
                var largeCupSql = "INSERT INTO inventory (itemName, itemQuantity, itemCategory, imageSet, itemDescription, unitOfMeasurement, minimumQuantity) " +
                                 "VALUES ('Large Cup', 1, 'Supplies', 'cup_large.png', 'Large size cup for beverages', 'pcs', 1);";
                await using var largeCupCmd = new MySqlCommand(largeCupSql, conn);
                await largeCupCmd.ExecuteNonQueryAsync();
            }
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
                    Status = reader.IsDBNull(reader.GetOrdinal("status")) ? "pending" : reader.GetString("status"),
                    IsAdmin = reader.IsDBNull(reader.GetOrdinal("isAdmin")) ? false : reader.GetBoolean("isAdmin")
                };
            }
            return null;
        } 

        public async Task<int> UpdateUserPasswordAsync(int userId, string newHashedPassword)
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = "UPDATE users SET password = @Password WHERE id = @Id;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Password", newHashedPassword);
            cmd.Parameters.AddWithValue("@Id", userId);
            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<int> UpdateUserStatusAsync(int userId, string status)
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = "UPDATE users SET status = @Status WHERE id = @Id;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Status", status);
            cmd.Parameters.AddWithValue("@Id", userId);
            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<UserInfoModel>> GetPendingUsersAsync()
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = "SELECT id, email, password, firstName, lastName, birthday, phoneNumber, address, status, isAdmin FROM users WHERE status = 'pending' ORDER BY id ASC;";
            await using var cmd = new MySqlCommand(sql, conn);

            var users = new List<UserInfoModel>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var user = new UserInfoModel
                {
                    ID = reader.GetInt32("id"),
                    Email = reader.IsDBNull(reader.GetOrdinal("email")) ? string.Empty : reader.GetString("email"),
                    Password = reader.IsDBNull(reader.GetOrdinal("password")) ? string.Empty : reader.GetString("password"),
                    FirstName = reader.IsDBNull(reader.GetOrdinal("firstName")) ? string.Empty : reader.GetString("firstName"),
                    LastName = reader.IsDBNull(reader.GetOrdinal("lastName")) ? string.Empty : reader.GetString("lastName"),
                    Birthday = reader.IsDBNull(reader.GetOrdinal("birthday")) ? DateTime.MinValue : reader.GetDateTime("birthday"),
                    PhoneNumber = reader.IsDBNull(reader.GetOrdinal("phoneNumber")) ? string.Empty : reader.GetString("phoneNumber"),
                    Address = reader.IsDBNull(reader.GetOrdinal("address")) ? string.Empty : reader.GetString("address"),
                    Status = reader.IsDBNull(reader.GetOrdinal("status")) ? "pending" : reader.GetString("status"),
                    IsAdmin = reader.IsDBNull(reader.GetOrdinal("isAdmin")) ? false : reader.GetBoolean("isAdmin")
                };
                users.Add(user);
            }
            return users;
        }

        public async Task<int> UpdateExistingUsersToApprovedAsync()
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = "UPDATE users SET status = 'approved' WHERE status IS NULL OR status = '';";
            await using var cmd = new MySqlCommand(sql, conn);

            return await cmd.ExecuteNonQueryAsync();
        }

        // Pending Registrations Methods
        public async Task<int> AddPendingRegistrationAsync(PendingRegistrationModel registration)
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = "INSERT INTO pending_registrations (email, password, firstName, lastName, phoneNumber, address, birthday, registrationDate) " +
                      "VALUES (@Email, @Password, @FirstName, @LastName, @PhoneNumber, @Address, @Birthday, @RegistrationDate);";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Email", registration.Email);
            cmd.Parameters.AddWithValue("@Password", registration.Password);
            cmd.Parameters.AddWithValue("@FirstName", registration.FirstName);
            cmd.Parameters.AddWithValue("@LastName", registration.LastName);
            cmd.Parameters.AddWithValue("@PhoneNumber", registration.PhoneNumber);
            cmd.Parameters.AddWithValue("@Address", registration.Address);
            cmd.Parameters.AddWithValue("@Birthday", registration.Birthday.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@RegistrationDate", registration.RegistrationDate.ToString("yyyy-MM-dd HH:mm:ss"));

            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<PendingRegistrationModel>> GetPendingRegistrationsAsync()
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = "SELECT * FROM pending_registrations ORDER BY registrationDate ASC;";
            await using var cmd = new MySqlCommand(sql, conn);

            var registrations = new List<PendingRegistrationModel>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var registration = new PendingRegistrationModel
                {
                    ID = reader.GetInt32("id"),
                    Email = reader.IsDBNull(reader.GetOrdinal("email")) ? string.Empty : reader.GetString("email"),
                    Password = reader.IsDBNull(reader.GetOrdinal("password")) ? string.Empty : reader.GetString("password"),
                    FirstName = reader.IsDBNull(reader.GetOrdinal("firstName")) ? string.Empty : reader.GetString("firstName"),
                    LastName = reader.IsDBNull(reader.GetOrdinal("lastName")) ? string.Empty : reader.GetString("lastName"),
                    PhoneNumber = reader.IsDBNull(reader.GetOrdinal("phoneNumber")) ? string.Empty : reader.GetString("phoneNumber"),
                    Address = reader.IsDBNull(reader.GetOrdinal("address")) ? string.Empty : reader.GetString("address"),
                    Birthday = reader.IsDBNull(reader.GetOrdinal("birthday")) ? DateTime.MinValue : reader.GetDateTime("birthday"),
                    RegistrationDate = reader.IsDBNull(reader.GetOrdinal("registrationDate")) ? DateTime.Now : reader.GetDateTime("registrationDate")
                };
                registrations.Add(registration);
            }
            return registrations;
        }

        public async Task<int> ApprovePendingRegistrationAsync(int registrationId)
        {
            await using var conn = await GetOpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // Get the pending registration
                var getSql = "SELECT * FROM pending_registrations WHERE id = @Id;";
                await using var getCmd = new MySqlCommand(getSql, conn, (MySqlTransaction)tx);
                getCmd.Parameters.AddWithValue("@Id", registrationId);

                PendingRegistrationModel registration = null;
                await using var reader = await getCmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    registration = new PendingRegistrationModel
                    {
                        ID = reader.GetInt32("id"),
                        Email = reader.GetString("email"),
                        Password = reader.GetString("password"),
                        FirstName = reader.GetString("firstName"),
                        LastName = reader.GetString("lastName"),
                        PhoneNumber = reader.GetString("phoneNumber"),
                        Address = reader.GetString("address"),
                        Birthday = reader.GetDateTime("birthday"),
                        RegistrationDate = reader.GetDateTime("registrationDate")
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
                deleteCmd.Parameters.AddWithValue("@Id", registrationId);
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

        public async Task<int> RejectPendingRegistrationAsync(int registrationId)
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = "DELETE FROM pending_registrations WHERE id = @Id;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", registrationId);

            return await cmd.ExecuteNonQueryAsync();
        }
        public async Task<List<UserInfoModel>> GetAllUsersAsync()
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = "SELECT id, email, password, firstName, lastName, birthday, phoneNumber, address, status, isAdmin FROM users ORDER BY id ASC;";
            await using var cmd = new MySqlCommand(sql, conn);

            var users = new List<UserInfoModel>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var user = new UserInfoModel
                {
                    ID = reader.GetInt32("id"),
                    Email = reader.IsDBNull(reader.GetOrdinal("email")) ? string.Empty : reader.GetString("email"),
                    Password = reader.IsDBNull(reader.GetOrdinal("password")) ? string.Empty : reader.GetString("password"),
                    FirstName = reader.IsDBNull(reader.GetOrdinal("firstName")) ? string.Empty : reader.GetString("firstName"),
                    LastName = reader.IsDBNull(reader.GetOrdinal("lastName")) ? string.Empty : reader.GetString("lastName"),
                    Birthday = reader.IsDBNull(reader.GetOrdinal("birthday")) ? DateTime.MinValue : reader.GetDateTime("birthday"),
                    PhoneNumber = reader.IsDBNull(reader.GetOrdinal("phoneNumber")) ? string.Empty : reader.GetString("phoneNumber"),
                    Address = reader.IsDBNull(reader.GetOrdinal("address")) ? string.Empty : reader.GetString("address"),
                    Status = reader.IsDBNull(reader.GetOrdinal("status")) ? "pending" : reader.GetString("status"),
                    IsAdmin = reader.IsDBNull(reader.GetOrdinal("isAdmin")) ? false : reader.GetBoolean("isAdmin")
                };
                users.Add(user);
            }
            return users;
        }
        public async Task<int> AddUserAsync(UserInfoModel user)
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = "INSERT INTO users (firstName, lastName, email, password, phoneNumber, birthday, address, status, isAdmin) " +
                      "VALUES (@FirstName,@LastName, @Email, @Password, @PhoneNumber, @Birthday, @Address, @Status, @IsAdmin);";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@FirstName", user.FirstName);
            cmd.Parameters.AddWithValue("@LastName", user.LastName);
            cmd.Parameters.AddWithValue("@Email", user.Email);
            cmd.Parameters.AddWithValue("@Password", user.Password);
            cmd.Parameters.AddWithValue("@Birthday", user.Birthday.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@PhoneNumber", user.PhoneNumber);
            cmd.Parameters.AddWithValue("@Address", user.Address);
            cmd.Parameters.AddWithValue("@Status", user.Status ?? "pending");
            cmd.Parameters.AddWithValue("@IsAdmin", user.IsAdmin);

            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<bool> HasAnyUsersAsync()
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = "SELECT COUNT(*) FROM users;";
            await using var cmd = new MySqlCommand(sql, conn);

            var result = await cmd.ExecuteScalarAsync();
            return result != null && Convert.ToInt32(result) > 0;
        }

        // POS Database
        public async Task<List<POSPageModel>> GetProductsAsync()
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = "SELECT * FROM products;"; 
            await using var cmd = new MySqlCommand(sql, conn);

            var products = new List<POSPageModel>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                products.Add(new POSPageModel
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
            return products;
        }

        // Get add-ons (linked inventory items) for a product
        public async Task<List<InventoryPageModel>> GetProductAddonsAsync(int productId)
        {
            await using var conn = await GetOpenConnectionAsync();

            const string sql = @"SELECT pi.amount, pi.unit, pi.role,
                                        i.itemID, i.itemName, i.itemQuantity, i.itemCategory, i.imageSet,
                                        i.itemDescription, i.unitOfMeasurement, i.minimumQuantity
                                   FROM product_ingredients pi
                                   JOIN inventory i ON i.itemID = pi.itemID
                                  WHERE pi.productID = @ProductID AND pi.role = 'addon'";

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
                    IsSelected = true
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

                results.Add(item);
            }

            return results;
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

            return await cmd.ExecuteNonQueryAsync();
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
            return (int)cmd.LastInsertedId;
        }

        // Link product to inventory items (addons/ingredients)
        public async Task<int> SaveProductIngredientLinksAsync(int productId, IEnumerable<(int inventoryItemId, double amount, string? unit, string role)> links)
        {
            await using var conn = await GetOpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            const string sql = "INSERT INTO product_ingredients (productID, itemID, amount, unit, role) VALUES (@ProductID, @ItemID, @Amount, @Unit, @Role);";
            int total = 0;
            try
            {
                foreach (var link in links)
                {
                    await using var cmd = new MySqlCommand(sql, conn, (MySqlTransaction)tx);
                    cmd.Parameters.AddWithValue("@ProductID", productId);
                    cmd.Parameters.AddWithValue("@ItemID", link.inventoryItemId);
                    cmd.Parameters.AddWithValue("@Amount", link.amount);
                    cmd.Parameters.AddWithValue("@Unit", (object?)link.unit ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Role", link.role);
                    total += await cmd.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
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

            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<int> DeleteProductAsync(int productId)
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = "DELETE FROM products WHERE productID = @ProductID;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ProductID", productId);

            return await cmd.ExecuteNonQueryAsync();
        }

        // Inventory Database Methods
        public async Task<List<InventoryPageModel>> GetInventoryItemsAsync()
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = "SELECT * FROM inventory;";
            await using var cmd = new MySqlCommand(sql, conn);

            var inventoryItems = new List<InventoryPageModel>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                inventoryItems.Add(new InventoryPageModel
                {
                    itemID = reader.GetInt32("itemID"),
                    itemName = reader.GetString("itemName"),
                    itemQuantity = reader.GetDouble("itemQuantity"),
                    itemCategory = reader.IsDBNull(reader.GetOrdinal("itemCategory")) ? "" : reader.GetString("itemCategory"),
                    ImageSet = reader.IsDBNull(reader.GetOrdinal("imageSet")) ? "" : reader.GetString("imageSet"),
                    itemDescription = reader.IsDBNull(reader.GetOrdinal("itemDescription")) ? "" : reader.GetString("itemDescription"),
                    unitOfMeasurement = reader.IsDBNull(reader.GetOrdinal("unitOfMeasurement")) ? "" : reader.GetString("unitOfMeasurement"),
                    minimumQuantity = reader.IsDBNull(reader.GetOrdinal("minimumQuantity")) ? 0 : reader.GetDouble("minimumQuantity"),
                    maximumStockLevel = reader.IsDBNull(reader.GetOrdinal("maximumStockLevel")) ? 0 : reader.GetDouble("maximumStockLevel")
                });
            }
            return inventoryItems;
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
                    minimumQuantity = reader.IsDBNull(reader.GetOrdinal("minimumQuantity")) ? 0 : reader.GetDouble("minimumQuantity"),
                    maximumStockLevel = reader.IsDBNull(reader.GetOrdinal("maximumStockLevel")) ? 0 : reader.GetDouble("maximumStockLevel")
                };
            }
            return null;
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
                return totalAffected;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // Transaction History Methods
        public async Task<int> SaveTransactionAsync(TransactionHistoryModel transaction)
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = "INSERT INTO transaction_history (drinkName, quantity, size, addOns, price, vat, total, transactionDate, customerName, paymentMethod, status) " +
                      "VALUES (@DrinkName, @Quantity, @Size, @AddOns, @Price, @Vat, @Total, @TransactionDate, @CustomerName, @PaymentMethod, @Status);";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@DrinkName", transaction.DrinkName);
            cmd.Parameters.AddWithValue("@Quantity", transaction.Quantity);
            cmd.Parameters.AddWithValue("@Size", transaction.Size);
            cmd.Parameters.AddWithValue("@AddOns", transaction.AddOns);
            cmd.Parameters.AddWithValue("@Price", transaction.Price);
            cmd.Parameters.AddWithValue("@Vat", transaction.Vat);
            cmd.Parameters.AddWithValue("@Total", transaction.Total);
            cmd.Parameters.AddWithValue("@TransactionDate", transaction.TransactionDate);
            cmd.Parameters.AddWithValue("@CustomerName", transaction.CustomerName);
            cmd.Parameters.AddWithValue("@PaymentMethod", transaction.PaymentMethod);
            cmd.Parameters.AddWithValue("@Status", transaction.Status);

            await cmd.ExecuteNonQueryAsync();
            return (int)cmd.LastInsertedId;
        }

        public async Task<List<TransactionHistoryModel>> GetTransactionsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = "SELECT * FROM transaction_history WHERE transactionDate >= @StartDate AND transactionDate <= @EndDate ORDER BY transactionDate DESC;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@StartDate", startDate);
            cmd.Parameters.AddWithValue("@EndDate", endDate);

            var transactions = new List<TransactionHistoryModel>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                transactions.Add(new TransactionHistoryModel
                {
                    TransactionId = reader.GetInt32("id"),
                    DrinkName = reader.GetString("drinkName"),
                    Quantity = reader.GetInt32("quantity"),
                    Size = reader.GetString("size"),
                    AddOns = reader.GetString("addOns"),
                    Price = reader.GetDecimal("price"),
                    Vat = reader.GetDecimal("vat"),
                    Total = reader.GetDecimal("total"),
                    TransactionDate = reader.GetDateTime("transactionDate"),
                    CustomerName = reader.GetString("customerName"),
                    PaymentMethod = reader.GetString("paymentMethod"),
                    Status = reader.GetString("status")
                });
            }

            return transactions;
        }

        public async Task<List<TransactionHistoryModel>> GetTransactionsByCategoryAsync(string category, DateTime startDate, DateTime endDate)
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = @"SELECT th.* FROM transaction_history th 
                       JOIN products p ON th.drinkName = p.productName 
                       WHERE p.category = @Category 
                       AND th.transactionDate >= @StartDate 
                       AND th.transactionDate <= @EndDate 
                       ORDER BY th.transactionDate DESC;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Category", category);
            cmd.Parameters.AddWithValue("@StartDate", startDate);
            cmd.Parameters.AddWithValue("@EndDate", endDate);

            var transactions = new List<TransactionHistoryModel>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                transactions.Add(new TransactionHistoryModel
                {
                    TransactionId = reader.GetInt32("id"),
                    DrinkName = reader.GetString("drinkName"),
                    Quantity = reader.GetInt32("quantity"),
                    Size = reader.GetString("size"),
                    AddOns = reader.GetString("addOns"),
                    Price = reader.GetDecimal("price"),
                    Vat = reader.GetDecimal("vat"),
                    Total = reader.GetDecimal("total"),
                    TransactionDate = reader.GetDateTime("transactionDate"),
                    CustomerName = reader.GetString("customerName"),
                    PaymentMethod = reader.GetString("paymentMethod"),
                    Status = reader.GetString("status")
                });
            }

            return transactions;
        }

        public async Task<decimal> GetTotalSalesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = "SELECT COALESCE(SUM(total), 0) FROM transaction_history WHERE transactionDate >= @StartDate AND transactionDate <= @EndDate;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@StartDate", startDate);
            cmd.Parameters.AddWithValue("@EndDate", endDate);

            var result = await cmd.ExecuteScalarAsync();
            return result != null ? Convert.ToDecimal(result) : 0;
        }

        public async Task<int> GetTotalOrdersByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = "SELECT COUNT(DISTINCT transactionId) FROM transaction_history WHERE transactionDate >= @StartDate AND transactionDate <= @EndDate;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@StartDate", startDate);
            cmd.Parameters.AddWithValue("@EndDate", endDate);

            var result = await cmd.ExecuteScalarAsync();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        public async Task<Dictionary<string, int>> GetTopProductsByDateRangeAsync(DateTime startDate, DateTime endDate, int limit = 10)
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = @"SELECT drinkName, SUM(quantity) as totalQuantity 
                       FROM transaction_history 
                       WHERE transactionDate >= @StartDate AND transactionDate <= @EndDate 
                       GROUP BY drinkName 
                       ORDER BY totalQuantity DESC 
                       LIMIT @Limit;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@StartDate", startDate);
            cmd.Parameters.AddWithValue("@EndDate", endDate);
            cmd.Parameters.AddWithValue("@Limit", limit);

            var topProducts = new Dictionary<string, int>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                topProducts[reader.GetString("drinkName")] = reader.GetInt32("totalQuantity");
            }

            return topProducts;
        }

        public async Task<int> SaveInventoryItemAsync(InventoryPageModel inventory)
        {
            await using var conn = await GetOpenConnectionAsync();

            // Set maximumStockLevel to itemQuantity for new items
            var maxStockLevel = Math.Max(inventory.maximumStockLevel, inventory.itemQuantity);

            var sql = "INSERT INTO inventory (itemName, itemQuantity, itemCategory, imageSet, itemDescription, unitOfMeasurement, minimumQuantity, maximumStockLevel) " +
                      "VALUES (@ItemName, @ItemQuantity, @ItemCategory, @ImageSet, @ItemDescription, @UnitOfMeasurement, @MinimumQuantity, @MaximumStockLevel);";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ItemName", inventory.itemName);
            cmd.Parameters.AddWithValue("@ItemQuantity", inventory.itemQuantity);
            cmd.Parameters.AddWithValue("@ItemCategory", inventory.itemCategory);
            cmd.Parameters.AddWithValue("@ImageSet", inventory.ImageSet);
            cmd.Parameters.AddWithValue("@ItemDescription", (object?)inventory.itemDescription ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UnitOfMeasurement", (object?)inventory.unitOfMeasurement ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@MinimumQuantity", inventory.minimumQuantity);
            cmd.Parameters.AddWithValue("@MaximumStockLevel", maxStockLevel);

            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<int> UpdateInventoryItemAsync(InventoryPageModel inventory)
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = "UPDATE inventory SET itemName = @ItemName, itemQuantity = @ItemQuantity, itemCategory = @ItemCategory, " +
                      "imageSet = @ImageSet, itemDescription = @ItemDescription, unitOfMeasurement = @UnitOfMeasurement, " +
                      "minimumQuantity = @MinimumQuantity, maximumStockLevel = @MaximumStockLevel WHERE itemID = @ItemID;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ItemID", inventory.itemID);
            cmd.Parameters.AddWithValue("@ItemName", inventory.itemName);
            cmd.Parameters.AddWithValue("@ItemQuantity", inventory.itemQuantity);
            cmd.Parameters.AddWithValue("@ItemCategory", inventory.itemCategory);
            cmd.Parameters.AddWithValue("@ImageSet", inventory.ImageSet);
            cmd.Parameters.AddWithValue("@ItemDescription", (object?)inventory.itemDescription ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UnitOfMeasurement", (object?)inventory.unitOfMeasurement ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@MinimumQuantity", inventory.minimumQuantity);
            cmd.Parameters.AddWithValue("@MaximumStockLevel", inventory.maximumStockLevel);

            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<int> DeleteInventoryItemAsync(int itemId)
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = "DELETE FROM inventory WHERE itemID = @ItemID;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ItemID", itemId);

            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<int> UpdateInventoryQuantityAsync(int itemId, double newQuantity)
        {
            await using var conn = await GetOpenConnectionAsync();

            // Update quantity and maximum stock level if new quantity is higher
            var sql = @"UPDATE inventory 
                       SET itemQuantity = @NewQuantity, 
                           maximumStockLevel = GREATEST(maximumStockLevel, @NewQuantity)
                       WHERE itemID = @ItemID;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ItemID", itemId);
            cmd.Parameters.AddWithValue("@NewQuantity", newQuantity);

            return await cmd.ExecuteNonQueryAsync();
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

        // Notification

        // Cart

        //
    }
}
