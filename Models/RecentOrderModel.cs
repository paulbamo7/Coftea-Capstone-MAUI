using CommunityToolkit.Mvvm.ComponentModel;

namespace Coftea_Capstone.Models
{
    public partial class RecentOrderModel : ObservableObject
    {
        [ObservableProperty]
        private int orderNumber;

        [ObservableProperty]
        private string status = "Completed";

        [ObservableProperty]
        private string productImage;

        [ObservableProperty]
        private string productName;

        [ObservableProperty]
        private decimal totalAmount;

        [ObservableProperty]
        private DateTime orderTime;

        public string OrderDisplay => $"Order #{OrderNumber}";
        public string StatusColor => Status == "Completed" ? "#4CAF50" : "#FF9800";
    }
}
