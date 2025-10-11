using System.Collections.Generic;

namespace Coftea_Capstone.Services
{
    public class NetworkConfigurationService
    {
        private static readonly Dictionary<string, object> _settings = new Dictionary<string, object>();

        public static string GetDatabaseHost()
        {
            System.Diagnostics.Debug.WriteLine($"🔍 NetworkConfigurationService.GetDatabaseHost called");
            
            // Check if user has manually set a host
            if (_settings.ContainsKey("DatabaseHost"))
            {
                var manualHost = _settings["DatabaseHost"].ToString();
                System.Diagnostics.Debug.WriteLine($"📝 Using manual database host: {manualHost}");
                return manualHost;
            }

            // Default to localhost if no manual host is set
            System.Diagnostics.Debug.WriteLine($"🔍 No manual host set, using localhost");
            return "localhost";
        }

        public static string GetEmailHost()
        {
            // Check if user has manually set a host
            if (_settings.ContainsKey("EmailHost"))
            {
                return _settings["EmailHost"].ToString();
            }

            // Default to localhost if no manual host is set
            return "localhost";
        }

        public static void SetDatabaseHost(string host)
        {
            _settings["DatabaseHost"] = host;
        }

        public static void SetEmailHost(string host)
        {
            _settings["EmailHost"] = host;
        }

        public static void SetManualDatabaseHost(string host)
        {
            System.Diagnostics.Debug.WriteLine($"🔧 Manually setting database host to: {host}");
            _settings["DatabaseHost"] = host;
        }

        public static void ClearManualDatabaseHost()
        {
            if (_settings.ContainsKey("DatabaseHost"))
            {
                _settings.Remove("DatabaseHost");
                System.Diagnostics.Debug.WriteLine($"🧹 Cleared manual database host");
            }
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
    }
}
