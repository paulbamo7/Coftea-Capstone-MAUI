using Coftea_Capstone.ViewModel.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Networking;
using System;
using System.Threading.Tasks;

namespace Coftea_Capstone.ViewModel
{
	public partial class BaseViewModel : ObservableObject
	{
		[ObservableProperty]
		private bool isLoading;

		[ObservableProperty]
		private string statusMessage;

		[ObservableProperty]
		private bool hasError;

		protected RetryConnectionPopupViewModel GetRetryConnectionPopup()
		{
			return ((App)Application.Current).RetryConnectionPopup;
		}

		protected async Task RunWithLoading(Func<Task> work, string startMessage = null)
		{
			try
			{
				IsLoading = true;
				HasError = false;
				if (!string.IsNullOrWhiteSpace(startMessage)) StatusMessage = startMessage;
				await work();
			}
			catch (Exception ex)
			{
				HasError = true;
				StatusMessage = ex.Message;
				throw;
			}
			finally
			{
				IsLoading = false;
			}
		}

		protected bool EnsureInternetOrShowRetry(Func<Task> retryAction, string message)
		{
			if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
			{
				HasError = true;
				StatusMessage = "No internet connection. Please check your network.";
				GetRetryConnectionPopup().ShowRetryPopup(retryAction, message);
				return false;
			}
			return true;
		}
	}
}


