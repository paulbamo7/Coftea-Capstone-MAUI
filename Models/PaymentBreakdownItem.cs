using CommunityToolkit.Mvvm.ComponentModel;

namespace Coftea_Capstone.Models
{
    public partial class PaymentBreakdownItem : ObservableObject
    {
        [ObservableProperty]
        private string productName;

        [ObservableProperty]
        private string paymentMethod;

        [ObservableProperty]
        private decimal amount;

        [ObservableProperty]
        private int quantity;

        public string FormattedAmount => $"₱{Amount:F2}";
        
        public string DisplayText => $"{ProductName} x{Quantity}";
    }
}

