using System.Diagnostics;
using Coftea_Capstone.Models;

namespace Coftea_Capstone.Services;

/// <summary>
/// Service to monitor internet connectivity and notify when online/offline status changes
/// </summary>
public class ConnectivityService
{
    private bool _wasConnected = true;
    
    public event EventHandler<bool>? ConnectivityChanged;
    
    public bool IsConnected => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
    
    public ConnectivityService()
    {
        // Subscribe to connectivity changes
        Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
        _wasConnected = IsConnected;
        
        Debug.WriteLine($"[ConnectivityService] Initial connection status: {(IsConnected ? "ONLINE" : "OFFLINE")}");
    }
    
    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        var isNowConnected = e.NetworkAccess == NetworkAccess.Internet;
        
        Debug.WriteLine($"[ConnectivityService] Connectivity changed: {(isNowConnected ? "ONLINE" : "OFFLINE")}");
        
        // Only notify if status actually changed
        if (_wasConnected != isNowConnected)
        {
            _wasConnected = isNowConnected;
            ConnectivityChanged?.Invoke(this, isNowConnected);
            
            if (isNowConnected)
            {
                Debug.WriteLine("[ConnectivityService] ✅ Internet connection restored - ready to sync");
            }
            else
            {
                Debug.WriteLine("[ConnectivityService] ⚠️ Internet connection lost - switching to offline mode");
            }
        }
    }
    
    /// <summary>
    /// Test MySQL database connectivity
    /// </summary>
    public async Task<bool> CanReachDatabaseAsync()
    {
        if (!IsConnected)
        {
            return false;
        }
        
        try
        {
            // Try to query the database to verify connectivity
            var db = new Database();
            var users = await db.GetAllUsersAsync();
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ConnectivityService] Cannot reach database: {ex.Message}");
            return false;
        }
    }
}

