using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Microsoft.Maui.Networking;

namespace Coftea_Capstone.Services
{
    public class NetworkDetectionService
    {
        private static string _cachedDatabaseHost;
        private static string _cachedEmailHost;
        private static DateTime _lastCacheTime = DateTime.MinValue;
        private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(5); // Cache for 5 minutes

        public static async Task<string> GetDatabaseHostAsync()
        {
            if (ShouldRefreshCache())
            {
                _cachedDatabaseHost = await DetectDatabaseHostAsync();
                _lastCacheTime = DateTime.Now;
            }
            return _cachedDatabaseHost ?? "localhost";
        }

        public static async Task<string> GetEmailHostAsync()
        {
            if (ShouldRefreshCache())
            {
                _cachedEmailHost = await DetectEmailHostAsync();
                _lastCacheTime = DateTime.Now;
            }
            return _cachedEmailHost ?? "localhost";
        }

        private static bool ShouldRefreshCache()
        {
            return _lastCacheTime == DateTime.MinValue || 
                   DateTime.Now - _lastCacheTime > CacheExpiry;
        }

        private static async Task<string> DetectDatabaseHostAsync()
        {
            try
            {
                // Platform-specific detection
                if (DeviceInfo.Platform == DevicePlatform.Android)
                {
                    return "10.0.2.2"; // Android emulator default
                }

                if (DeviceInfo.Platform == DevicePlatform.iOS)
                {
                    // For iOS Simulator, use localhost
                    // For physical iOS devices, try to detect the host machine's IP
                    if (DeviceInfo.DeviceType == DeviceType.Virtual)
                    {
                        return "localhost"; // iOS Simulator
                    }
                    else
                    {
                        // Physical iOS device - try to find the host machine's IP
                        var possibleHosts = await GetPossibleHostsAsync();
                        
                        foreach (var host in possibleHosts)
                        {
                            if (await TestDatabaseConnectionAsync(host))
                            {
                                return host;
                            }
                        }
                        
                        // Fallback to localhost for physical devices
                        return "localhost";
                    }
                }

                // For Windows, Mac, and other platforms, try to detect the actual server
                var allPossibleHosts = await GetPossibleHostsAsync();
                
                foreach (var host in allPossibleHosts)
                {
                    if (await TestDatabaseConnectionAsync(host))
                    {
                        return host;
                    }
                }

                // Fallback to localhost
                return "localhost";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error detecting database host: {ex.Message}");
                return "localhost";
            }
        }

        private static async Task<string> DetectEmailHostAsync()
        {
            try
            {
                // Platform-specific detection for email host
                if (DeviceInfo.Platform == DevicePlatform.Android)
                {
                    return "10.0.2.2"; // Android emulator default
                }

                if (DeviceInfo.Platform == DevicePlatform.iOS)
                {
                    // For iOS Simulator, use localhost
                    // For physical iOS devices, try to detect the host machine's IP
                    if (DeviceInfo.DeviceType == DeviceType.Virtual)
                    {
                        return "localhost"; // iOS Simulator
                    }
                    else
                    {
                        // Physical iOS device - try to find the host machine's IP
                        var possibleHosts = await GetPossibleHostsAsync();
                        
                        foreach (var host in possibleHosts)
                        {
                            if (await TestEmailConnectionAsync(host))
                            {
                                return host;
                            }
                        }
                        
                        // Fallback to localhost for physical devices
                        return "localhost";
                    }
                }

                // For Windows, Mac, and other platforms, try to detect the actual server
                var allPossibleHosts = await GetPossibleHostsAsync();
                
                foreach (var host in allPossibleHosts)
                {
                    if (await TestEmailConnectionAsync(host))
                    {
                        return host;
                    }
                }

                // Fallback to localhost
                return "localhost";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error detecting email host: {ex.Message}");
                return "localhost";
            }
        }

        private static async Task<List<string>> GetPossibleHostsAsync()
        {
            var hosts = new List<string> { "localhost", "127.0.0.1" };

            try
            {
                // Get local IP addresses
                var localIPs = GetLocalIPAddresses();
                hosts.AddRange(localIPs);

                // Try common development server IPs
                var commonIPs = new[]
                {
                    "192.168.1.4",    // Your current hardcoded IP
                    "192.168.254.104", // Your email service IP
                    "192.168.0.1",
                    "192.168.1.1",
                    "10.0.0.1",
                    "172.16.0.1"
                };

                hosts.AddRange(commonIPs);

                // Remove duplicates and return
                return hosts.Distinct().ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting possible hosts: {ex.Message}");
                return hosts;
            }
        }

        private static List<string> GetLocalIPAddresses()
        {
            var ipAddresses = new List<string>();

            try
            {
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                                ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .ToList();

                foreach (var ni in networkInterfaces)
                {
                    var ipProps = ni.GetIPProperties();
                    var addresses = ipProps.UnicastAddresses
                        .Where(addr => addr.Address.AddressFamily == AddressFamily.InterNetwork)
                        .Select(addr => addr.Address.ToString())
                        .ToList();

                    ipAddresses.AddRange(addresses);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting local IP addresses: {ex.Message}");
            }

            return ipAddresses;
        }

        private static async Task<bool> TestDatabaseConnectionAsync(string host)
        {
            try
            {
                using var tcpClient = new TcpClient();
                var connectTask = tcpClient.ConnectAsync(host, 3306);
                var timeoutTask = Task.Delay(2000); // 2 second timeout

                var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                
                if (completedTask == connectTask && tcpClient.Connected)
                {
                    tcpClient.Close();
                    System.Diagnostics.Debug.WriteLine($"Database connection successful to {host}:3306");
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database connection test failed for {host}: {ex.Message}");
            }

            return false;
        }

        private static async Task<bool> TestEmailConnectionAsync(string host)
        {
            try
            {
                using var tcpClient = new TcpClient();
                var connectTask = tcpClient.ConnectAsync(host, 1025); // MailHog port
                var timeoutTask = Task.Delay(2000); // 2 second timeout

                var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                
                if (completedTask == connectTask && tcpClient.Connected)
                {
                    tcpClient.Close();
                    System.Diagnostics.Debug.WriteLine($"Email connection successful to {host}:1025");
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Email connection test failed for {host}: {ex.Message}");
            }

            return false;
        }

        public static void ClearCache()
        {
            _cachedDatabaseHost = null;
            _cachedEmailHost = null;
            _lastCacheTime = DateTime.MinValue;
        }

        public static async Task<string> GetCurrentNetworkInfoAsync()
        {
            try
            {
                var info = new List<string>();
                
                // Network connectivity
                var access = Connectivity.Current.NetworkAccess;
                info.Add($"Network Access: {access}");

                // Local IP addresses
                var localIPs = GetLocalIPAddresses();
                info.Add($"Local IPs: {string.Join(", ", localIPs)}");

                // Detected hosts
                var dbHost = await GetDatabaseHostAsync();
                var emailHost = await GetEmailHostAsync();
                info.Add($"Database Host: {dbHost}");
                info.Add($"Email Host: {emailHost}");

                return string.Join("\n", info);
            }
            catch (Exception ex)
            {
                return $"Error getting network info: {ex.Message}";
            }
        }
    }
}
