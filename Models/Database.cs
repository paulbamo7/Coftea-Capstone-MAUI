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
            _db.CreateTableAsync<LoginPageModel>().Wait();
        }

        public Task<LoginPageModel> GetUserByEmailAsync(string email)
        {
            return _db.Table<LoginPageModel>()
                      .Where(u => u.Email == email)
                      .FirstOrDefaultAsync();
        }

        public Task<int> AddUserAsync(LoginPageModel user)
        {
            return _db.InsertAsync(user);
        }

        public Task<List<LoginPageModel>> GetAllUsersAsync()
        {
            return _db.Table<LoginPageModel>().ToListAsync();
        }
    }

}
