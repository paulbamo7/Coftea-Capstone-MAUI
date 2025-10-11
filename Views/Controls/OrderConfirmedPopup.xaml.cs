using Coftea_Capstone.ViewModel.Controls;

namespace Coftea_Capstone.Views.Controls
{
    public partial class OrderConfirmedPopup : ContentView
    {
        public OrderConfirmedPopup()
        {
            InitializeComponent();
            BindingContext = new OrderConfirmedPopupViewModel();
        }

        public OrderConfirmedPopupViewModel ViewModel => BindingContext as OrderConfirmedPopupViewModel;

        public async Task ShowOrderConfirmationAsync(string orderId, decimal totalAmount, string paymentMethod)
        {
            await ViewModel.ShowOrderConfirmationAsync(orderId, totalAmount, paymentMethod);
        }
    }
}
