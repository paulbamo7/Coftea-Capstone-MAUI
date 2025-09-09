using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace Coftea_Capstone.Models
{
    public class UserInfoModel : ObservableObject
    {
      
        public int ID { get; set; }

        public string Email { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; } = string.Empty;
        public string Password { get; set; }

        public bool IsAdmin { get; set; }

        public DateTime Birthday { get; set; }

        public string PhoneNumber { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;
    }
}
