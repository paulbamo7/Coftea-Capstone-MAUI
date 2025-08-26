using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace Coftea_Capstone.C_
{
    class Database
    {
        private readonly SQLiteAsyncConnection db;

        public Database(string dbPath)
        {
            db = new SQLiteAsyncConnection(dbPath);
            db.CreateTableAsync<UserInfo>().Wait();
        }
        public Task<UserInfo> GetUserAsync(string email, string password)
        {
            return db.Table<UserInfo>()
                      .Where(u => u.Email == email && u.Password == password)
                      .FirstOrDefaultAsync();
        }
        public Task<int> SaveUserAsync(UserInfo user)
        {
            return db.InsertAsync(user);
        }
    }
}
