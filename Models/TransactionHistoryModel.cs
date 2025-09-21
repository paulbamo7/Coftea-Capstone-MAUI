using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Coftea_Capstone.Models
{
    public partial class TransactionHistoryModel : ObservableObject
    {
        [ObservableProperty]
        private int transactionId;

        [ObservableProperty]
        private string drinkName;

        [ObservableProperty]
        private int quantity;

        [ObservableProperty]
        private string size;

        [ObservableProperty]
        private string addOns;

        [ObservableProperty]
        private decimal price;

        [ObservableProperty]
        private decimal vat;

        [ObservableProperty]
        private decimal total;

        [ObservableProperty]
        private DateTime transactionDate;

        [ObservableProperty]
        private string customerName;

        [ObservableProperty]
        private string paymentMethod;

        [ObservableProperty]
        private string status;

        public string FormattedDate => TransactionDate.ToString("dd/MM");
        
        public string FormattedPrice => $"P{Price:F2}";
       
        
        public string FormattedTotal => $"P{Total:F2}";
    }
}
