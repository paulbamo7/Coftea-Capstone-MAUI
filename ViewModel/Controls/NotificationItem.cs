using System;

namespace Coftea_Capstone.ViewModel.Controls
{
    public class NotificationItem
    {
        public string Title { get; set; } = string.Empty; // e.g., "Point-Of-Sale"
        public string Message { get; set; } = string.Empty; // e.g., "Successfully Listed Product: Java Chip"
        public string IdText { get; set; } = string.Empty; // e.g., "ID: JC1234"
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}


