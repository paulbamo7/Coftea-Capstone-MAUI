using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Coftea_Capstone.ViewModel.Controls
{
    public partial class ConnectivityStatusIndicatorViewModel : ObservableObject
    {
        [ObservableProperty]
        private string statusIcon = "🌐";

        [ObservableProperty]
        private string statusText = "Online";

        [ObservableProperty]
        private string statusBackgroundColor = "#4CAF50"; // Green for online

        [ObservableProperty]
        private bool hasPendingOperations = false;

        [ObservableProperty]
        private string pendingCountText = "0";

        private System.Timers.Timer _updateTimer;

        public ConnectivityStatusIndicatorViewModel()
        {
            // Subscribe to connectivity changes
            if (App.ConnectivityService != null)
            {
                App.ConnectivityService.ConnectivityChanged += OnConnectivityChanged;
                UpdateStatus(App.ConnectivityService.IsConnected);
            }

            // Start periodic updates for pending operations count
            _updateTimer = new System.Timers.Timer(2000); // Update every 2 seconds
            _updateTimer.Elapsed += async (s, e) => await UpdatePendingCountAsync();
            _updateTimer.Start();

            // Initial update
            Task.Run(async () => await UpdatePendingCountAsync());
        }

        private void OnConnectivityChanged(object? sender, bool isConnected)
        {
            UpdateStatus(isConnected);
        }

        private void UpdateStatus(bool isConnected)
        {
            if (isConnected)
            {
                StatusIcon = "🌐";
                StatusText = "Online";
                StatusBackgroundColor = "#4CAF50"; // Green
                System.Diagnostics.Debug.WriteLine("🌐 Status Indicator: ONLINE");
            }
            else
            {
                StatusIcon = "📱";
                StatusText = "Offline";
                StatusBackgroundColor = "#FF9800"; // Orange
                System.Diagnostics.Debug.WriteLine("📱 Status Indicator: OFFLINE");
            }
        }

        private async Task UpdatePendingCountAsync()
        {
            try
            {
                if (App.LocalDb != null)
                {
                    var pendingCount = await App.LocalDb.GetPendingOperationsCountAsync();
                    HasPendingOperations = pendingCount > 0;
                    PendingCountText = pendingCount.ToString();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error updating pending count: {ex.Message}");
            }
        }

        ~ConnectivityStatusIndicatorViewModel()
        {
            // Cleanup
            _updateTimer?.Stop();
            _updateTimer?.Dispose();
            if (App.ConnectivityService != null)
            {
                App.ConnectivityService.ConnectivityChanged -= OnConnectivityChanged;
            }
        }
    }
}

