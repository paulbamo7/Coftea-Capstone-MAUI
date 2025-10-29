using System.Diagnostics;
using Coftea_Capstone.Services;

namespace Coftea_Capstone.Views.Controls;

public partial class SyncStatusIndicator : ContentView
{
    private ConnectivityService? _connectivity;
    private DatabaseSyncService? _syncService;
    private LocalDatabaseService? _localDb;
    private System.Threading.Timer? _updateTimer;
    
    public SyncStatusIndicator()
    {
        InitializeComponent();
        
        // Initialize after app services are ready
        Loaded += OnLoaded;
    }
    
    private void OnLoaded(object? sender, EventArgs e)
    {
        try
        {
            _connectivity = App.ConnectivityService;
            _syncService = App.SyncService;
            _localDb = App.LocalDb;
            
            if (_connectivity != null)
            {
                _connectivity.ConnectivityChanged += OnConnectivityChanged;
                UpdateStatus(_connectivity.IsConnected);
            }
            
            if (_syncService != null)
            {
                _syncService.SyncStatusChanged += OnSyncStatusChanged;
            }
            
            // Update pending count every 5 seconds
            _updateTimer = new System.Threading.Timer(async _ =>
            {
                await UpdatePendingCount();
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SyncStatusIndicator] Error in OnLoaded: {ex.Message}");
        }
    }
    
    private void OnConnectivityChanged(object? sender, bool isConnected)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateStatus(isConnected);
        });
    }
    
    private void OnSyncStatusChanged(object? sender, SyncStatus status)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            SyncIndicator.IsRunning = status.IsSyncing;
            SyncIndicator.IsVisible = status.IsSyncing;
            
            if (status.IsSyncing && !string.IsNullOrEmpty(status.Message))
            {
                StatusText.Text = status.Message;
            }
            else if (!status.IsSyncing)
            {
                UpdateStatus(_connectivity?.IsConnected ?? false);
            }
        });
    }
    
    private void UpdateStatus(bool isOnline)
    {
        if (isOnline)
        {
            ConnectionIcon.Text = "🌐";
            StatusText.Text = "Online";
            StatusText.TextColor = Colors.LightGreen;
        }
        else
        {
            ConnectionIcon.Text = "📱";
            StatusText.Text = "Offline";
            StatusText.TextColor = Colors.Orange;
        }
        
        _ = UpdatePendingCount();
    }
    
    private async Task UpdatePendingCount()
    {
        try
        {
            if (_localDb == null) return;
            
            var count = await _localDb.GetPendingOperationsCountAsync();
            
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (count > 0)
                {
                    PendingBadge.IsVisible = true;
                    PendingCount.Text = count.ToString();
                }
                else
                {
                    PendingBadge.IsVisible = false;
                }
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SyncStatusIndicator] Error updating pending count: {ex.Message}");
        }
    }
    
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        
        if (Handler == null)
        {
            // Cleanup
            if (_connectivity != null)
            {
                _connectivity.ConnectivityChanged -= OnConnectivityChanged;
            }
            
            if (_syncService != null)
            {
                _syncService.SyncStatusChanged -= OnSyncStatusChanged;
            }
            
            _updateTimer?.Dispose();
        }
    }
}

