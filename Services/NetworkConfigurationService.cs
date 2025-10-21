using System.Collections.Generic;

namespace Coftea_Capstone.Services
{
    public class NetworkConfigurationService
    {
        private static readonly Dictionary<string, object> _settings = new Dictionary<string, object>();
        private static string _autoDetectedIP = null;
        private static bool _autoDetectionEnabled = true;

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

            // Try automatic IP detection if enabled
            if (_autoDetectionEnabled && !string.IsNullOrEmpty(_autoDetectedIP))
            {
                System.Diagnostics.Debug.WriteLine($"🤖 Using auto-detected IP: {_autoDetectedIP}");
                return _autoDetectedIP;
            }

            // Default to localhost if no manual host is set and auto-detection hasn't run
            System.Diagnostics.Debug.WriteLine($"🔍 No manual host set, using localhost");
            return "localhost";
        }

        /// <summary>
        /// Enables automatic IP detection and attempts to detect the best IP address
        /// </summary>
        public static async Task EnableAutomaticIPDetectionAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🤖 Starting automatic IP detection...");
                _autoDetectedIP = await AutomaticIPDetectionService.DetectDatabaseIPAsync();
                _autoDetectionEnabled = true;
                System.Diagnostics.Debug.WriteLine($"✅ Automatic IP detection completed: {_autoDetectedIP}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Automatic IP detection failed: {ex.Message}");
                _autoDetectedIP = "localhost";
                _autoDetectionEnabled = false;
            }
        }

        /// <summary>
        /// Disables automatic IP detection
        /// </summary>
        public static void DisableAutomaticIPDetection()
        {
            _autoDetectionEnabled = false;
            _autoDetectedIP = null;
            System.Diagnostics.Debug.WriteLine("🚫 Automatic IP detection disabled");
        }

        /// <summary>
        /// Gets the currently auto-detected IP address
        /// </summary>
        public static string GetAutoDetectedIP()
        {
            return _autoDetectedIP;
        }

        /// <summary>
        /// Checks if automatic IP detection is enabled
        /// </summary>
        public static bool IsAutomaticDetectionEnabled()
        {
            return _autoDetectionEnabled;
        }

        /// <summary>
        /// Forces a refresh of the auto-detected IP address
        /// </summary>
        public static async Task RefreshAutoDetectedIPAsync()
        {
            AutomaticIPDetectionService.ClearCache();
            await EnableAutomaticIPDetectionAsync();
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
            else if (_autoDetectionEnabled && !string.IsNullOrEmpty(_autoDetectedIP))
                result["Database Host"] = $"Auto-detected: {_autoDetectedIP}";
            else
                result["Database Host"] = "localhost (default)";
            
            if (_settings.ContainsKey("EmailHost"))
                result["Email Host"] = _settings["EmailHost"].ToString();
            else
                result["Email Host"] = "localhost (default)";

            return result;
        }
    }
}
