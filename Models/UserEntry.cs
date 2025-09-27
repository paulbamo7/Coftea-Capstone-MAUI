using CommunityToolkit.Mvvm.ComponentModel;

namespace Coftea_Capstone.Models
{
    public class UserEntry : ObservableObject
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Status { get; set; }
        public string LastActive { get; set; }
        public string DateAdded { get; set; }
        public bool CanEditInventory { get; set; }
        public bool CanEditPOS { get; set; }
        public bool CanEditBalanceSheet { get; set; }
    }
}


