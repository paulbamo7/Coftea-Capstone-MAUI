using System.Collections.Generic;
using System.Threading.Tasks;

namespace Coftea_Capstone.Services
{
    public class NetworkConfigurationService
    {
        private static readonly Dictionary<string, object> _settings = new Dictionary<string, object>();

        public static async Task<string> GetDatabaseHostAsync()
        {
            System.Diagnostics.Debug.WriteLine($"🔍 NetworkConfigurationService.GetDatabaseHostAsync called");
            
            // Check if user has manually set a host
            if (_settings.ContainsKey("DatabaseHost"))
            {
                var manualHost = _settings["DatabaseHost"].ToString();
                System.Diagnostics.Debug.WriteLine($"📝 Using manual database host: {manualHost}");
                return manualHost;
            }

            // Otherwise use auto-detection
            System.Diagnostics.Debug.WriteLine($"🔍 No manual host set, using auto-detection");
            var detectedHost = await NetworkDetectionService.GetDatabaseHostAsync();
            System.Diagnostics.Debug.WriteLine($"🔍 Auto-detected database host: {detectedHost}");
            return detectedHost;
        }

        public static async Task<string> GetEmailHostAsync()
        {
            // Check if user has manually set a host
            if (_settings.ContainsKey("EmailHost"))
            {
                return _settings["EmailHost"].ToString();
            }

            // Otherwise use auto-detection
            return await NetworkDetectionService.GetEmailHostAsync();
        }

        public static void SetDatabaseHost(string host)
        {
            _settings["DatabaseHost"] = host;
        }

        public static void SetEmailHost(string host)
        {
            _settings["EmailHost"] = host;
        }

        public static void ClearCustomSettings()
        {
            _settings.Clear();
        }

        public static bool HasCustomSettings()
        {
            return _settings.Count > 0;
        }

        public static Dictionary<string, string> GetCurrentSettings()
        {
            var result = new Dictionary<string, string>();
            
            if (_settings.ContainsKey("DatabaseHost"))
                result["Database Host"] = _settings["DatabaseHost"].ToString();
            
            if (_settings.ContainsKey("EmailHost"))
                result["Email Host"] = _settings["EmailHost"].ToString();

            return result;
        }

        public static async Task<Dictionary<string, string>> GetDetectedHostsAsync()
        {
            var result = new Dictionary<string, string>();
            
            try
            {
                result["Detected Database Host"] = await NetworkDetectionService.GetDatabaseHostAsync();
                result["Detected Email Host"] = await NetworkDetectionService.GetEmailHostAsync();
            }
            catch (Exception ex)
            {
                result["Error"] = ex.Message;
            }

            return result;
        }

        public static async Task<string> GetNetworkDiagnosticsAsync()
        {
            return await NetworkDetectionService.GetCurrentNetworkInfoAsync();
        }

        // Helper method to set your PC's IP address manually for testing
        public static void SetManualDatabaseHost(string ipAddress)
        {
            System.Diagnostics.Debug.WriteLine($"🔧 Manually setting database host to: {ipAddress}");
            SetDatabaseHost(ipAddress);
        }

        // Clear manual database host to enable automatic detection
        public static void ClearManualDatabaseHost()
        {
            if (_settings.ContainsKey("DatabaseHost"))
            {
                _settings.Remove("DatabaseHost");
                System.Diagnostics.Debug.WriteLine($"🧹 Cleared manual database host, enabling automatic detection");
            }
        }

        // Helper method to get all possible IPs for debugging
        public static async Task<List<string>> GetAllPossibleHostsAsync()
        {
            return await NetworkDetectionService.GetPossibleHostsAsync();
        }

        // Get the current PC's primary IP address
        public static string GetCurrentPCIPAddress()
        {
            return NetworkDetectionService.GetCurrentPCIPAddress();
        }

        // Set the database host to the current PC's IP address
        public static void SetDatabaseHostToCurrentPC()
        {
            var currentIP = GetCurrentPCIPAddress();
            if (!string.IsNullOrEmpty(currentIP))
            {
                System.Diagnostics.Debug.WriteLine($"🔧 Setting database host to current PC IP: {currentIP}");
                SetDatabaseHost(currentIP);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("❌ Could not detect current PC IP address");
            }
        }
    }
}
