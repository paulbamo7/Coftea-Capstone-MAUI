using CommunityToolkit.Mvvm.ComponentModel;

namespace Coftea_Capstone.Models
{
    public class PendingRegistrationModel : ObservableObject
    {
        public int ID { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public DateTime Birthday { get; set; }
        public DateTime RegistrationDate { get; set; } = DateTime.Now;
    }
}
