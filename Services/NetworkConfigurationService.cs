using System.Collections.Generic;
using System.Threading.Tasks;

namespace Coftea_Capstone.Services
{
    public class NetworkConfigurationService
    {
        private static readonly Dictionary<string, object> _settings = new Dictionary<string, object>();

        public static async Task<string> GetDatabaseHostAsync()
        {
            // Check if user has manually set a host
            if (_settings.ContainsKey("DatabaseHost"))
            {
                return _settings["DatabaseHost"].ToString();
            }

            // Otherwise use auto-detection
            return await NetworkDetectionService.GetDatabaseHostAsync();
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
    }
}
