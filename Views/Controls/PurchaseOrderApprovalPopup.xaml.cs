using Coftea_Capstone.ViewModel.Controls;

namespace Coftea_Capstone.Views.Controls;

public partial class PurchaseOrderApprovalPopup : ContentView
{
	public PurchaseOrderApprovalPopup()
	{
		InitializeComponent();
	}

	private async void OnApproveClicked(object sender, EventArgs e)
	{
		try
		{
			var button = sender as Button;
			var order = button?.BindingContext as PurchaseOrderDisplayModel;
			
			if (order != null && BindingContext is PurchaseOrderApprovalPopupViewModel viewModel)
			{
				System.Diagnostics.Debug.WriteLine($"🔵 Approve button clicked for order #{order.PurchaseOrderId}");
				await viewModel.ApproveOrderCommand.ExecuteAsync(order);
			}
			else
			{
				System.Diagnostics.Debug.WriteLine("⚠️ Could not get order or viewModel for approve");
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"❌ Error in OnApproveClicked: {ex.Message}");
		}
	}

	private async void OnRejectClicked(object sender, EventArgs e)
	{
		try
		{
			var button = sender as Button;
			var order = button?.BindingContext as PurchaseOrderDisplayModel;
			
			if (order != null && BindingContext is PurchaseOrderApprovalPopupViewModel viewModel)
			{
				System.Diagnostics.Debug.WriteLine($"🔵 Reject button clicked for order #{order.PurchaseOrderId}");
				await viewModel.RejectOrderCommand.ExecuteAsync(order);
			}
			else
			{
				System.Diagnostics.Debug.WriteLine("⚠️ Could not get order or viewModel for reject");
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"❌ Error in OnRejectClicked: {ex.Message}");
		}
	}
}

