using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace Coftea_Capstone.C_
{
    class UserInfo
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Unique]
        public string Email { get; set; }

        public string Password { get; set; }

        public string Role { get; set; } // "Admin" or "Employee"
    }
}
