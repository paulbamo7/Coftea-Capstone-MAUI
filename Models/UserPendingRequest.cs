using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Coftea_Capstone.Models
{
    public class UserPendingRequest : ObservableObject
    {
        public int ID { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public string Status { get; set; } = "pending"; // pending, approved, denied
        public DateTime RegistrationDate => RequestDate;
    }
}
