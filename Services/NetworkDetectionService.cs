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

        public static async Task<string> DetectDatabaseHostAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔍 Detecting database host for {DeviceInfo.Platform} ({DeviceInfo.DeviceType})");
                
                // Platform-specific detection
                if (DeviceInfo.Platform == DevicePlatform.Android)
                {
                    // Check if running on emulator or physical device
                    if (DeviceInfo.DeviceType == DeviceType.Virtual)
                    {
                        System.Diagnostics.Debug.WriteLine("📱 Android emulator detected, using 10.0.2.2");
                        return "10.0.2.2"; // Android emulator default
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("📱 Physical Android device detected, scanning for host IP");
                        // Physical Android device - try to find the host machine's IP
                        var possibleHosts = await GetPossibleHostsAsync();
                        System.Diagnostics.Debug.WriteLine($"🔍 Testing {possibleHosts.Count} possible hosts: {string.Join(", ", possibleHosts)}");
                        
                        foreach (var host in possibleHosts)
                        {
                            if (await TestDatabaseConnectionAsync(host))
                            {
                                System.Diagnostics.Debug.WriteLine($"✅ Found working database host: {host}");
                                return host;
                            }
                        }
                        
                        // Fallback to localhost for physical devices
                        System.Diagnostics.Debug.WriteLine("❌ No working database host found, falling back to localhost");
                        return "localhost";
                    }
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
                    // Check if running on emulator or physical device
                    if (DeviceInfo.DeviceType == DeviceType.Virtual)
                    {
                        return "10.0.2.2"; // Android emulator default
                    }
                    else
                    {
                        // Physical Android device - try to find the host machine's IP
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

        public static async Task<List<string>> GetPossibleHostsAsync()
        {
            var hosts = new List<string>();

            try
            {
                // Platform-specific prioritization
                if (DeviceInfo.Platform == DevicePlatform.Android)
                {
                    // For Android emulators, use 10.0.2.2 which maps to host localhost
                    hosts.Add("10.0.2.2"); // Android emulator default - maps to host localhost
                    hosts.Add("10.0.3.2"); // Genymotion
                    System.Diagnostics.Debug.WriteLine("✅ Android emulator detected - using 10.0.2.2");
                    
                    // Add host machine IPs as fallbacks
                    var primaryIP = GetPrimaryIPAddress();
                    if (!string.IsNullOrEmpty(primaryIP))
                    {
                        hosts.Add(primaryIP);
                        System.Diagnostics.Debug.WriteLine($"✅ Added primary IP: {primaryIP}");
                    }
                }
                else if (DeviceInfo.Platform == DevicePlatform.iOS)
                {
                    // For iOS simulator, use localhost
                    hosts.Add("127.0.0.1");
                    hosts.Add("localhost");
                    System.Diagnostics.Debug.WriteLine("✅ iOS simulator detected - using localhost");
                }
                else
                {
                    // For Windows/macOS, use host machine IPs
                    var primaryIP = GetPrimaryIPAddress();
                    if (!string.IsNullOrEmpty(primaryIP))
                    {
                        hosts.Add(primaryIP);
                        System.Diagnostics.Debug.WriteLine($"✅ Added primary IP: {primaryIP}");
                    }

                    // Get all local IP addresses
                    var localIPs = GetLocalIPAddresses();
                    hosts.AddRange(localIPs);
                }

                // Always add localhost as final fallback
                hosts.Add("localhost");
                hosts.Add("127.0.0.1");

                // Remove duplicates and return
                var uniqueHosts = hosts.Distinct().ToList();
                System.Diagnostics.Debug.WriteLine($"🔍 All possible hosts: {string.Join(", ", uniqueHosts)}");
                return uniqueHosts;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting possible hosts: {ex.Message}");
                return new List<string> { "10.0.2.2", "localhost", "127.0.0.1" };
            }
        }

        public static string GetPrimaryIPAddress()
        {
            try
            {
                var localIPs = GetLocalIPAddresses();
                
                if (!localIPs.Any())
                {
                    System.Diagnostics.Debug.WriteLine("❌ No local IP addresses found");
                    return string.Empty;
                }

                // Find the most likely primary IP (first private IP that's not link-local)
                var primaryIP = localIPs.FirstOrDefault(ip => 
                {
                    var address = IPAddress.Parse(ip);
                    return IsPrivateIP(address) && !IsLinkLocal(address);
                });

                if (!string.IsNullOrEmpty(primaryIP))
                {
                    System.Diagnostics.Debug.WriteLine($"✅ Primary IP selected: {primaryIP}");
                    return primaryIP;
                }

                // Fallback to first available IP
                var fallbackIP = localIPs.First();
                System.Diagnostics.Debug.WriteLine($"⚠️ Using fallback IP: {fallbackIP}");
                return fallbackIP;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting primary IP: {ex.Message}");
                return string.Empty;
            }
        }

        public static List<string> GetLocalIPAddresses()
        {
            var ipAddresses = new List<string>();

            try
            {
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                                ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                                ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                    .OrderBy(ni => GetInterfacePriority(ni))
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"Found {networkInterfaces.Count} active network interfaces");

                foreach (var ni in networkInterfaces)
                {
                    var ipProps = ni.GetIPProperties();
                    var addresses = ipProps.UnicastAddresses
                        .Where(addr => addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                                      !IPAddress.IsLoopback(addr.Address))
                        .Select(addr => new { 
                            Address = addr.Address.ToString(), 
                            IsPrivate = IsPrivateIP(addr.Address),
                            IsLinkLocal = IsLinkLocal(addr.Address)
                        })
                        .OrderBy(addr => addr.IsLinkLocal) // Prioritize non-link-local addresses
                        .ThenBy(addr => addr.IsPrivate) // Then prioritize private addresses
                        .Select(addr => addr.Address)
                        .ToList();

                    if (addresses.Any())
                    {
                        System.Diagnostics.Debug.WriteLine($"Interface {ni.Name} ({ni.NetworkInterfaceType}): {string.Join(", ", addresses)}");
                        ipAddresses.AddRange(addresses);
                    }
                }

                // Remove duplicates and return in priority order
                var uniqueIPs = ipAddresses.Distinct().ToList();
                System.Diagnostics.Debug.WriteLine($"All detected IPs: {string.Join(", ", uniqueIPs)}");
                return uniqueIPs;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting local IP addresses: {ex.Message}");
            }

            return ipAddresses;
        }

        private static int GetInterfacePriority(NetworkInterface ni)
        {
            // Priority order for network interfaces
            return ni.NetworkInterfaceType switch
            {
                NetworkInterfaceType.Ethernet => 1,      // Wired Ethernet first
                NetworkInterfaceType.Wireless80211 => 2,  // WiFi second
                NetworkInterfaceType.Ppp => 3,           // PPP connections third
                _ => 4                                   // Others last
            };
        }

        private static bool IsPrivateIP(IPAddress ip)
        {
            var bytes = ip.GetAddressBytes();
            
            // Check for private IP ranges
            return (bytes[0] == 10) ||                                    // 10.0.0.0/8
                   (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) || // 172.16.0.0/12
                   (bytes[0] == 192 && bytes[1] == 168);                    // 192.168.0.0/16
        }

        private static bool IsLinkLocal(IPAddress ip)
        {
            var bytes = ip.GetAddressBytes();
            // Check for link-local addresses (169.254.0.0/16)
            return bytes[0] == 169 && bytes[1] == 254;
        }

        private static async Task<bool> TestDatabaseConnectionAsync(string host)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Testing database connection to {host}:3306");
                using var tcpClient = new TcpClient();
                var connectTask = tcpClient.ConnectAsync(host, 3306);
                var timeoutTask = Task.Delay(2000); // 2 second timeout

                var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                
                if (completedTask == connectTask && tcpClient.Connected)
                {
                    tcpClient.Close();
                    System.Diagnostics.Debug.WriteLine($"✅ Database connection successful to {host}:3306");
                    return true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Database connection timeout to {host}:3306");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Database connection test failed for {host}: {ex.Message}");
            }

            return false;
        }

        private static async Task<bool> TestEmailConnectionAsync(string host)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Testing email connection to {host}:1025");
                using var tcpClient = new TcpClient();
                var connectTask = tcpClient.ConnectAsync(host, 1025); // MailHog port
                var timeoutTask = Task.Delay(2000); // 2 second timeout

                var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                
                if (completedTask == connectTask && tcpClient.Connected)
                {
                    tcpClient.Close();
                    System.Diagnostics.Debug.WriteLine($"✅ Email connection successful to {host}:1025");
                    return true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Email connection timeout to {host}:1025");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Email connection test failed for {host}: {ex.Message}");
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

                // Primary IP address
                var primaryIP = GetPrimaryIPAddress();
                info.Add($"Primary IP: {primaryIP}");

                // Local IP addresses
                var localIPs = GetLocalIPAddresses();
                info.Add($"All Local IPs: {string.Join(", ", localIPs)}");

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

        public static string GetCurrentPCIPAddress()
        {
            return GetPrimaryIPAddress();
        }
    }
}
