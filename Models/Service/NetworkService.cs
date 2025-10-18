using Microsoft.Maui.Networking;

namespace Coftea_Capstone.Services
{
    public static class NetworkService
    {
        public static bool HasInternetConnection()
        {
            var access = Connectivity.Current.NetworkAccess;
            return access == NetworkAccess.Internet;
        }
    }
}