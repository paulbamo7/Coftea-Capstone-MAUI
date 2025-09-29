using CommunityToolkit.Mvvm.ComponentModel;

namespace Coftea_Capstone.Models
{
    public class UserEntry : ObservableObject
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string LastActive { get; set; }
        public string DateAdded { get; set; }
        public bool CanAccessInventory { get; set; }
        public bool CanAccessSalesReport { get; set; }
    }
}


