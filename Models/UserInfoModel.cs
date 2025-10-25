using System;
using CommunityToolkit.Mvvm.ComponentModel;

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

        // Profile fields
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string ProfileImage { get; set; } = "usericon.png";

        public string Status { get; set; } = "approved";

        // Access flags managed by admin
        public bool CanAccessInventory { get; set; }
        public bool CanAccessSalesReport { get; set; }
        public bool CanAccessPOS { get; set; }
    }
}
