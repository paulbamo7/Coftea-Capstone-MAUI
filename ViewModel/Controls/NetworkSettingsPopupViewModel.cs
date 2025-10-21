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
        [ObservableProperty] private bool isAutoDetectionEnabled = true;
        [ObservableProperty] private string autoDetectedIP = "";
        [ObservableProperty] private bool isRefreshing = false;

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
        private async Task RefreshAutoDetectionAsync() // Refresh auto-detected IP
        {
            try
            {
                IsRefreshing = true;
                NetworkDiagnostics = "Refreshing auto-detected IP address...";
                
                await NetworkConfigurationService.RefreshAutoDetectedIPAsync();
                await LoadCurrentSettingsAsync();
                
                NetworkDiagnostics = "Auto-detection refreshed successfully!";
            }
            catch (Exception ex)
            {
                NetworkDiagnostics = $"Auto-detection refresh failed: {ex.Message}";
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        [RelayCommand]
        private async Task ToggleAutoDetectionAsync() // Toggle automatic IP detection
        {
            try
            {
                if (IsAutoDetectionEnabled)
                {
                    NetworkConfigurationService.DisableAutomaticIPDetection();
                    IsAutoDetectionEnabled = false;
                    NetworkDiagnostics = "Automatic IP detection disabled";
                }
                else
                {
                    await NetworkConfigurationService.EnableAutomaticIPDetectionAsync();
                    IsAutoDetectionEnabled = true;
                    NetworkDiagnostics = "Automatic IP detection enabled";
                }
                
                await LoadCurrentSettingsAsync();
            }
            catch (Exception ex)
            {
                NetworkDiagnostics = $"Failed to toggle auto-detection: {ex.Message}";
            }
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
                
                // Load auto-detection status
                IsAutoDetectionEnabled = NetworkConfigurationService.IsAutomaticDetectionEnabled();
                AutoDetectedIP = NetworkConfigurationService.GetAutoDetectedIP() ?? "Not detected";

                // Load current settings
                var settings = NetworkConfigurationService.GetCurrentSettings();
                var autoStatus = IsAutoDetectionEnabled ? "Enabled" : "Disabled";
                NetworkDiagnostics = $"Database Host: {CurrentDatabaseHost}\nEmail Host: {CurrentEmailHost}\nAuto-Detection: {autoStatus}\nAuto-Detected IP: {AutoDetectedIP}";

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
