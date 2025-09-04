using Coftea_Capstone.Models;
using SQLite;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Coftea_Capstone.C_
{
    public class Database
    {
        private readonly SQLiteAsyncConnection _db;

        public Database(string dbPath = null)
        {
            if (string.IsNullOrEmpty(dbPath))
                dbPath = Path.Combine(FileSystem.AppDataDirectory, "coftea.db3");

            _db = new SQLiteAsyncConnection(dbPath);
            _db.CreateTableAsync<UserInfoModel>().Wait();
            _db.CreateTableAsync<POSPageModel>().Wait();
            _db.CreateTableAsync<InventoryPageModel>().Wait();
            _db.CreateTableAsync<SalesReportPageModel>().Wait();
        }

        // User Database
        public Task<UserInfoModel> GetUserByEmailAsync(string email)
        {
            return _db.Table<UserInfoModel>()
                      .Where(u => u.Email == email)
                      .FirstOrDefaultAsync();
        }
        public Task<int> AddUserAsync(UserInfoModel user)
        {
            return _db.InsertAsync(user);
        }

        public Task<List<UserInfoModel>> GetAllUsersAsync()
        {
            return _db.Table<UserInfoModel>().ToListAsync();
        }
        // POS Database
        public Task<List<POSPageModel>> GetProductsAsync()
        {
            return _db.Table<POSPageModel>().ToListAsync();
        }

        public Task<int> SaveProductAsync(POSPageModel product)
        {
            if (product.ProductID == 0)
                return _db.InsertAsync(product);
            else
                return _db.UpdateAsync(product); 
        }

        public Task<int> DeleteProductAsync(POSPageModel product)
        {
            return _db.DeleteAsync(product);
        }

        // Inventory Database
        public Task<List<InventoryPageModel>> GetInventoryItemsAsync()
        {
            return _db.Table<InventoryPageModel>().ToListAsync();
        }
        public Task<int> SaveInventoryItemsAsync(InventoryPageModel inventory)
        {
            return _db.InsertOrReplaceAsync(inventory);
        }
        public Task<int> DeleteInventoryItemAsync(InventoryPageModel inventory)
        {
            return _db.DeleteAsync(inventory);
        }

        // Sales Report
        public Task<List<SalesReportPageModel>> RetrieveSalesData()
        {
            return _db.Table<SalesReportPageModel>().ToListAsync();
        }

        // User Management

        // Notification

        // Cart

        //
    }
}
