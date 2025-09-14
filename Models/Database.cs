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
            _db = new MySqlConnectionStringBuilder
            {
                Server = "10.0.2.2",
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
                    ImageSet = reader.GetString("imageSet"),
                    Category = reader.GetString("category")
                });
            }
            return products;
        }

        public async Task<int> SaveProductAsync(POSPageModel product)
        {
            await using var conn = await GetOpenConnectionAsync();

            var sql = "INSERT INTO products (productName, smallPrice, largePrice, category, imageSet) " +
                      "VALUES (@ProductName, @SmallPrice, @LargePrice, @Category ,@Image);";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ProductName", product.ProductName);
            cmd.Parameters.AddWithValue("@SmallPrice", product.SmallPrice);
            cmd.Parameters.AddWithValue("@LargePrice", product.LargePrice);
            cmd.Parameters.AddWithValue("@Category", product.Category);
            cmd.Parameters.AddWithValue("@Image", product.ImageSet);

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
                    ImageSet = reader.GetString("imageSet")
                };
            }
            return null;
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

        // Notification

        // Cart

        //
    }
}
