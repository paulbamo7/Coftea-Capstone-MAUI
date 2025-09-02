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

        public Database()
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "coftea.db3");
            _db = new SQLiteAsyncConnection(dbPath);
            _db.CreateTableAsync<UserInfoModel>().Wait();
            _db.CreateTableAsync<POSPageModel>().Wait();
            _db.CreateTableAsync<InventoryPageModel>().Wait();
            _db.CreateTableAsync<SalesReportPageModel>().Wait();
        }

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

        public Task<int> AddProductAsync(POSPageModel product)
        {
            return _db.InsertAsync(product);
        }

        public Task<List<POSPageModel>> GetAllProductsAsync()
        {
            return _db.Table<POSPageModel>().ToListAsync();
        }

        public Task<int> UpdateProductAsync(POSPageModel product) {
            return _db.UpdateAsync(product);
        }

        public Task<int> DeleteProductAsync(POSPageModel product)
        {
            return _db.DeleteAsync(product);
        }
    }

}
