using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Diagnostics;

namespace Coftea_Capstone.Services
{
    public static class AutomaticIPDetectionService
    {
        private static string _cachedIP = null;
        private static DateTime _cacheTime = DateTime.MinValue;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Automatically detects the best IP address for database connection
        /// </summary>
        /// <returns>The detected IP address or localhost as fallback</returns>
        public static async Task<string> DetectDatabaseIPAsync()
        {
            // Return cached result if still fresh
            if (_cachedIP != null && DateTime.UtcNow - _cacheTime < CacheDuration)
            {
                Debug.WriteLine($"🔄 Using cached IP: {_cachedIP}");
                return _cachedIP;
            }

            try
            {
                Debug.WriteLine("🔍 Starting automatic IP detection...");
                
                // Add timeout to prevent hanging
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                
                // Method 1: Try to get local IP from network interfaces
                var localIP = GetLocalNetworkIP();
                if (!string.IsNullOrEmpty(localIP) && await TestDatabaseConnectionAsync(localIP, cts.Token))
                {
                    _cachedIP = localIP;
                    _cacheTime = DateTime.UtcNow;
                    Debug.WriteLine($"✅ Auto-detected IP via network interfaces: {localIP}");
                    return localIP;
                }

                // Method 2: Try common local network ranges (limited to first 5 to avoid timeout)
                var commonIPs = GetCommonLocalNetworkIPs().Take(5);
                foreach (var ip in commonIPs)
                {
                    if (cts.Token.IsCancellationRequested)
                        break;
                        
                    if (await TestDatabaseConnectionAsync(ip, cts.Token))
                    {
                        _cachedIP = ip;
                        _cacheTime = DateTime.UtcNow;
                        Debug.WriteLine($"✅ Auto-detected IP via common ranges: {ip}");
                        return ip;
                    }
                }

                Debug.WriteLine("⚠️ No suitable IP found, falling back to localhost");
                _cachedIP = "localhost";
                _cacheTime = DateTime.UtcNow;
                return "localhost";
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("⏰ IP detection timed out, using localhost");
                _cachedIP = "localhost";
                _cacheTime = DateTime.UtcNow;
                return "localhost";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ IP detection failed: {ex.Message}");
                _cachedIP = "localhost";
                _cacheTime = DateTime.UtcNow;
                return "localhost";
            }
        }

        /// <summary>
        /// Gets the local network IP address from network interfaces
        /// </summary>
        private static string GetLocalNetworkIP()
        {
            try
            {
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                                ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                                ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                    .OrderBy(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ? 0 : 1);

                foreach (var ni in networkInterfaces)
                {
                    var ipProps = ni.GetIPProperties();
                    var addresses = ipProps.UnicastAddresses
                        .Where(addr => addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                                      !IPAddress.IsLoopback(addr.Address))
                        .OrderBy(addr => addr.PrefixLength);

                    foreach (var addr in addresses)
                    {
                        var ip = addr.Address.ToString();
                        if (IsPrivateIP(ip))
                        {
                            Debug.WriteLine($"🔍 Found local network IP: {ip} on interface {ni.Name}");
                            return ip;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error getting local network IP: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Gets common local network IP addresses to test
        /// </summary>
        private static List<string> GetCommonLocalNetworkIPs()
        {
            var commonIPs = new List<string>();
            
            try
            {
                // Get current machine's IP and try variations
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork && IsPrivateIP(ip.ToString()))
                    {
                        commonIPs.Add(ip.ToString());
                    }
                }

                // Add common local network ranges
                var commonRanges = new[]
                {
                    "192.168.1.", "192.168.0.", "192.168.2.", "192.168.3.",
                    "10.0.0.", "10.0.1.", "172.16.0.", "172.16.1."
                };

                foreach (var range in commonRanges)
                {
                    for (int i = 1; i <= 10; i++) // Test first 10 IPs in each range
                    {
                        commonIPs.Add($"{range}{i}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error generating common IPs: {ex.Message}");
            }

            return commonIPs.Distinct().ToList();
        }

        /// <summary>
        /// Gets external IP address (as fallback)
        /// </summary>
        private static async Task<string> GetExternalIPAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                
                var response = await client.GetStringAsync("https://api.ipify.org");
                var ip = response.Trim();
                
                if (IPAddress.TryParse(ip, out var parsedIP) && parsedIP.AddressFamily == AddressFamily.InterNetwork)
                {
                    Debug.WriteLine($"🌐 External IP detected: {ip}");
                    return ip;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error getting external IP: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Tests if a database connection can be established with the given IP
        /// </summary>
        private static async Task<bool> TestDatabaseConnectionAsync(string ip, CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(ip, 3306); // MySQL default port
                var timeoutTask = Task.Delay(1000, cancellationToken); // 1 second timeout
                
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                
                if (completedTask == connectTask && client.Connected)
                {
                    Debug.WriteLine($"✅ Database connection test successful for {ip}");
                    return true;
                }
                else
                {
                    Debug.WriteLine($"❌ Database connection test failed for {ip}");
                    return false;
                }
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine($"⏰ Database connection test cancelled for {ip}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Database connection test error for {ip}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if an IP address is in a private network range
        /// </summary>
        private static bool IsPrivateIP(string ip)
        {
            if (!IPAddress.TryParse(ip, out var address))
                return false;

            var bytes = address.GetAddressBytes();
            
            // Check for private IP ranges
            return (bytes[0] == 10) ||
                   (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                   (bytes[0] == 192 && bytes[1] == 168);
        }

        /// <summary>
        /// Clears the IP cache to force re-detection
        /// </summary>
        public static void ClearCache()
        {
            _cachedIP = null;
            _cacheTime = DateTime.MinValue;
            Debug.WriteLine("🧹 IP detection cache cleared");
        }

        /// <summary>
        /// Gets the currently cached IP address
        /// </summary>
        public static string GetCachedIP()
        {
            return _cachedIP;
        }

        /// <summary>
        /// Checks if the cached IP is still valid
        /// </summary>
        public static bool IsCacheValid()
        {
            return _cachedIP != null && DateTime.UtcNow - _cacheTime < CacheDuration;
        }
    }
}
