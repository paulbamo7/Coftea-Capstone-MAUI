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
        public static async Task<bool> EnsureInternetAsync()
        {
            var access = Connectivity.Current.NetworkAccess;

            if (access != NetworkAccess.Internet)
            {
                // Show popup with Retry/Cancel
                bool retry = await Application.Current.MainPage.DisplayAlert(
                    "No Internet",
                    "Internet connection is required. Retry?",
                    "Retry",
                    "Cancel"
                );

                if (retry)
                {
                    // Check again recursively
                    return await EnsureInternetAsync();
                }

                return false;
            }

            return true;
        }
    }
}