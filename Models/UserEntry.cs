using CommunityToolkit.Mvvm.ComponentModel;

namespace Coftea_Capstone.Models
{
    public class UserEntry : ObservableObject
    {
        public string Username { get; set; }
        public string LastActive { get; set; }
        public string DateAdded { get; set; }
        public bool CanEditInventory { get; set; }
        public bool CanEditPOS { get; set; }
        public bool CanEditBalanceSheet { get; set; }
    }
}


