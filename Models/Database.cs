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
                    Password = reader.GetString("password")
                    /*FirstName = reader.GetString("name"),*/
                    /*IsAdmin = reader.GetString("role") == "admin"*/
                };
            }
            return null;
        } 
        public async Task<int> AddUserAsync(UserInfoModel user)
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = "INSERT INTO users (firstName, lastName, email, password, phoneNumber, birthday, address) " +
                      "VALUES (@FirstName,@LastName, @Email, @Password, @PhoneNumber, @Birthday, @Address);";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@FirstName", user.FirstName);
            cmd.Parameters.AddWithValue("@LastName", user.LastName);
            cmd.Parameters.AddWithValue("@Email", user.Email);
            cmd.Parameters.AddWithValue("@Password", user.Password);
            cmd.Parameters.AddWithValue("@Birthday", user.Birthday.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@PhoneNumber", user.PhoneNumber);
            cmd.Parameters.AddWithValue("@Address", user.Address);
            /*cmd.Parameters.AddWithValue("@Role", user.IsAdmin ? "admin" : "employee");*/

            return await cmd.ExecuteNonQueryAsync();
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
                    LargePrice = reader.GetDecimal("largePrice"),
                    ImageSet = reader.IsDBNull(reader.GetOrdinal("imageSet")) ? "" : reader.GetString("imageSet"),
                    Category = reader.IsDBNull(reader.GetOrdinal("category")) ? null : reader.GetString("category"),
                    Subcategory = reader.IsDBNull(reader.GetOrdinal("subcategory")) ? null : reader.GetString("subcategory"),
                    ProductDescription = reader.IsDBNull(reader.GetOrdinal("description")) ? "" : reader.GetString("description")
                });
            }
            return products;
        }

        public async Task<int> SaveProductAsync(POSPageModel product)
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = "INSERT INTO products (productName, smallPrice, largePrice, category, subcategory, imageSet, description) " +
                      "VALUES (@ProductName, @SmallPrice, @LargePrice, @Category, @Subcategory, @Image, @Description);";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ProductName", product.ProductName);
            cmd.Parameters.AddWithValue("@SmallPrice", product.SmallPrice);
            cmd.Parameters.AddWithValue("@LargePrice", product.LargePrice);
            cmd.Parameters.AddWithValue("@Category", product.Category);
            cmd.Parameters.AddWithValue("@Subcategory", (object?)product.Subcategory ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Image", product.ImageSet);
            cmd.Parameters.AddWithValue("@Description", (object?)product.ProductDescription ?? DBNull.Value);

            return await cmd.ExecuteNonQueryAsync();
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

        public async Task<int> UpdateProductAsync(POSPageModel product)
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = "UPDATE products SET productName = @ProductName, smallPrice = @SmallPrice, largePrice = @LargePrice, " +
                      "category = @Category, subcategory = @Subcategory, imageSet = @Image, description = @Description WHERE productID = @ProductID;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ProductID", product.ProductID);
            cmd.Parameters.AddWithValue("@ProductName", product.ProductName);
            cmd.Parameters.AddWithValue("@SmallPrice", product.SmallPrice);
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
                    minimumQuantity = reader.IsDBNull(reader.GetOrdinal("minimumQuantity")) ? 0 : reader.GetDouble("minimumQuantity")
                });
            }
            return inventoryItems;
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

            return await cmd.ExecuteNonQueryAsync();
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

        /*public async Task<int> DeleteProductAsync(InventoryPageModel inventory)
        {
            *//*await using var conn = await GetOpenConnectionAsync();
            var sql = "DELETE * FROM inventory WHERE itemName = @Name LIMIT 1;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Name", inventory);

            *//*await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new InventoryPageModel
                {
                    ProductID = reader.GetInt32("productID"),
                    ProductName = reader.GetString("productName"),
                    SmallPrice = Convert.ToDouble(reader["smallPrice"]),
                    LargePrice = Convert.ToDouble(reader["largePrice"]),
                    ImageSet = reader.GetString("imageSet")
                };
            }*//*
            return null;*//*
        }*/

        // Inventory Database
       /* public async Task<int>  GetInventoryItemsAsync(InventoryPageModel inventory)
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = "INSERT INTO products (productName, smallPrice, largePrice, imageSet) " +
                      "VALUES (@ProductName, @SmallPrice, @LargePrice, @Image);";
            await using var cmd = new MySqlCommand(sql, conn);
            *//*cmd.Parameters.AddWithValue("@ProductName", inventory.ProductName);
            cmd.Parameters.AddWithValue("@SmallPrice", inventory.SmallPrice);
            cmd.Parameters.AddWithValue("@LargePrice", inventory.LargePrice);
            cmd.Parameters.AddWithValue("@Image", inventory.ImageSet);*//*

            return await cmd.ExecuteNonQueryAsync();
        }*/
       /* public Task<int> SaveInventoryItemsAsync(InventoryPageModel inventory)
        {
            return _db.InsertOrReplaceAsync(inventory);
        }
        public Task<int> DeleteInventoryItemAsync(InventoryPageModel inventory)
        {
            return _db.DeleteAsync(inventory);
        }
*/
        // Sales Report
        /*public Task<List<SalesReportPageModel>> RetrieveSalesData()
        {
            return _db.Table<SalesReportPageModel>().ToListAsync();
        }**/

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
            updateCmd.Parameters.AddWithValue("@NewPassword", newPassword);
            updateCmd.Parameters.AddWithValue("@Email", email);

            int rowsAffected = await updateCmd.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        // Notification

        // Cart

        //
    }
}
