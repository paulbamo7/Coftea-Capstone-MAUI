using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Coftea_Capstone.Services;

namespace Coftea_Capstone.ViewModel.Controls
{
    public partial class NetworkSettingsPopupViewModel : ObservableObject
    {
        [ObservableProperty] private bool isVisible = false;
        [ObservableProperty] private string currentDatabaseHost = "localhost";
        [ObservableProperty] private string currentEmailHost = "localhost";
        [ObservableProperty] private string databaseHost = "";
        [ObservableProperty] private string emailHost = "";
        [ObservableProperty] private string networkDiagnostics = "Network settings configured";

        public NetworkSettingsPopupViewModel()
        {
            _ = LoadCurrentSettingsAsync();
        }

        public async Task ShowAsync() // Open the popup
        {
            IsVisible = true;
            await LoadCurrentSettingsAsync();
        }

        [RelayCommand]
        private async Task CloseAsync() // Close the popup
        {
            IsVisible = false;
        }

        [RelayCommand]
        private async Task RefreshAsync() // Refresh current settings
        {
            await LoadCurrentSettingsAsync();
        }

        [RelayCommand]
        private async Task SaveAsync() // Save custom settings
        {
            try
            {
                // Save custom settings
                if (!string.IsNullOrWhiteSpace(DatabaseHost))
                {
                    NetworkConfigurationService.SetDatabaseHost(DatabaseHost.Trim());
                }

                if (!string.IsNullOrWhiteSpace(EmailHost))
                {
                    NetworkConfigurationService.SetEmailHost(EmailHost.Trim());
                }

                // Reload settings
                await LoadCurrentSettingsAsync();

                // Show success message
                await Application.Current.MainPage.DisplayAlert(
                    "Success", 
                    "Network settings saved successfully!", 
                    "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Error", 
                    $"Failed to save settings: {ex.Message}", 
                    "OK");
            }
        }

        private async Task LoadCurrentSettingsAsync() // Load current settings
        {
            try
            {
                // Load current settings
                CurrentDatabaseHost = NetworkConfigurationService.GetDatabaseHost();
                CurrentEmailHost = NetworkConfigurationService.GetEmailHost();

                // Load current settings
                var settings = NetworkConfigurationService.GetCurrentSettings();
                NetworkDiagnostics = $"Database Host: {CurrentDatabaseHost}\nEmail Host: {CurrentEmailHost}";

                // Clear input fields
                DatabaseHost = "";
                EmailHost = "";
            }
            catch (Exception ex)
            {
                NetworkDiagnostics = $"Error loading settings: {ex.Message}";
            }
        }
    }
}
