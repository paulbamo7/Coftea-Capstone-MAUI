using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Coftea_Capstone.Models
{
    public class RegisterPageModel
    {
        [PrimaryKey, AutoIncrement]
        public int ID { get; set; }



        [Unique, NotNull]
        public string Email { get; set; } = string.Empty;

        [NotNull]
        public string FirstName { get; set; } = string.Empty;

        [NotNull]
        public string LastName { get; set; } = string.Empty;

        [NotNull]
        public string Password { get; set; } = string.Empty;

        [NotNull]
        public string Role { get; set; } = string.Empty;

        [NotNull]
        public DateTime Birthday { get; set; }

        [NotNull]
        public string PhoneNumber { get; set; } = string.Empty;

        [NotNull]
        public string Address { get; set; } = string.Empty;
    }
}
