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
        private string addOns = "No add-ons";

        [ObservableProperty]
        private decimal price;

        [ObservableProperty]
        private decimal smallPrice;

        [ObservableProperty]
        private decimal mediumPrice;

        [ObservableProperty]
        private decimal largePrice;

        [ObservableProperty]
        private decimal addonPrice;

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
        
        public string FormattedSmallPrice => SmallPrice > 0 ? $"P{SmallPrice:F2}" : "-";
        
        public string FormattedMediumPrice => MediumPrice > 0 ? $"P{MediumPrice:F2}" : "-";
        
        public string FormattedLargePrice => LargePrice > 0 ? $"P{LargePrice:F2}" : "-";
        
        public string FormattedAddonPrice => AddonPrice > 0 ? $"P{AddonPrice:F2}" : "-";
        
        public string FormattedVAT => $"P{Vat:F2}";
        
        public string FormattedTotal => $"P{Total:F2}";
    }
}
