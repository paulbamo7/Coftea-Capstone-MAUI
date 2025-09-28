using Coftea_Capstone.ViewModel.Controls;

namespace Coftea_Capstone.Views.Controls;

public partial class PaymentPopup : ContentView
{
    public PaymentPopup()
    {
        InitializeComponent();
    }

    private void OnAmountPaidChanged(object sender, TextChangedEventArgs e)
    {
        if (BindingContext is PaymentPopupViewModel viewModel)
        {
            viewModel.UpdateAmountPaid(e.NewTextValue);
        }
    }

    private void OnQuickAmountClicked(object sender, EventArgs e)
    {
        if (sender is Button button && BindingContext is PaymentPopupViewModel viewModel)
        {
            // Extract amount from button text (remove ₱ symbol)
            string amountText = button.Text.Replace("₱", "");
            viewModel.UpdateAmountPaid(amountText);
        }
    }
}
