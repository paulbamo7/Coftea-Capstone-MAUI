using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Coftea_Capstone.Services;

namespace Coftea_Capstone.ViewModel.Controls
{
    public partial class RetryConnectionPopupViewModel : ObservableObject
    {
        [ObservableProperty] private bool isRetryPopupVisible = false;
        [ObservableProperty] private string errorMessage = "Connection failed. Please check your internet connection and try again.";
        [ObservableProperty] private bool isRetrying = false;

        private Func<Task> _retryAction;
        private Action _onRetrySuccess;
        private Action _onRetryCancel;

        public RetryConnectionPopupViewModel()
        {
        }

        public void ShowRetryPopup(Func<Task> retryAction, string customMessage = null, Action onRetrySuccess = null, Action onRetryCancel = null)
        {
            _retryAction = retryAction;
            _onRetrySuccess = onRetrySuccess;
            _onRetryCancel = onRetryCancel;
            
            if (!string.IsNullOrEmpty(customMessage))
                ErrorMessage = customMessage;
            else
                ErrorMessage = "Connection failed. Please check your internet connection and try again.";

            IsRetryPopupVisible = true;
        }

        [RelayCommand]
        private async Task RetryConnection()
        {
            if (_retryAction == null) return;

            try
            {
                IsRetrying = true;
                
                // Check internet connection first
                if (!NetworkService.HasInternetConnection())
                {
                    ErrorMessage = "No internet connection detected. Please check your network settings.";
                    return;
                }

                // Execute the retry action
                await _retryAction.Invoke();
                
                // If we get here, retry was successful
                IsRetryPopupVisible = false;
                _onRetrySuccess?.Invoke();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Retry failed: {ex.Message}";
            }
            finally
            {
                IsRetrying = false;
            }
        }

        [RelayCommand]
        private void CancelRetry()
        {
            IsRetryPopupVisible = false;
            _onRetryCancel?.Invoke();
        }

        [RelayCommand]
        private void CloseRetryPopup()
        {
            IsRetryPopupVisible = false;
        }
    }
}
