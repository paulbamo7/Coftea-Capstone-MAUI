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
        [ObservableProperty] private string detectedDatabaseHost = "Detecting...";
        [ObservableProperty] private string detectedEmailHost = "Detecting...";
        [ObservableProperty] private string databaseHost = "";
        [ObservableProperty] private string emailHost = "";
        [ObservableProperty] private string networkDiagnostics = "Loading...";

        public NetworkSettingsPopupViewModel()
        {
            _ = LoadCurrentSettingsAsync();
        }

        public async Task ShowAsync()
        {
            IsVisible = true;
            await LoadCurrentSettingsAsync();
        }

        [RelayCommand]
        private async Task CloseAsync()
        {
            IsVisible = false;
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadCurrentSettingsAsync();
        }

        [RelayCommand]
        private async Task SaveAsync()
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

                // Clear cache to force re-detection
                NetworkDetectionService.ClearCache();

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

        private async Task LoadCurrentSettingsAsync()
        {
            try
            {
                // Load current settings
                CurrentDatabaseHost = await NetworkConfigurationService.GetDatabaseHostAsync();
                CurrentEmailHost = await NetworkConfigurationService.GetEmailHostAsync();

                // Load detected settings
                var detectedHosts = await NetworkConfigurationService.GetDetectedHostsAsync();
                DetectedDatabaseHost = detectedHosts.ContainsKey("Detected Database Host") 
                    ? detectedHosts["Detected Database Host"] 
                    : "Not detected";
                DetectedEmailHost = detectedHosts.ContainsKey("Detected Email Host") 
                    ? detectedHosts["Detected Email Host"] 
                    : "Not detected";

                // Load network diagnostics
                NetworkDiagnostics = await NetworkConfigurationService.GetNetworkDiagnosticsAsync();

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
