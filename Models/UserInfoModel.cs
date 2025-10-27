using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Coftea_Capstone.Models
{
    public partial class UserInfoModel : ObservableObject
    {
        public int ID { get; set; }

        public string Email { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; } = string.Empty;
        public string Password { get; set; }

        public bool IsAdmin { get; set; }

        public string PhoneNumber { get; set; } = string.Empty;

        // Profile fields with property change notifications
        [ObservableProperty]
        private string username = string.Empty;
        
        [ObservableProperty]
        private string fullName = string.Empty;
        
        [ObservableProperty]
        private string profileImage = "usericon.png";

        public string Status { get; set; } = "approved";

        // Access flags managed by admin
        public bool CanAccessInventory { get; set; }
        public bool CanAccessSalesReport { get; set; }
        public bool CanAccessPOS { get; set; }
    }
}
