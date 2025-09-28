using Coftea_Capstone.ViewModel;

namespace Coftea_Capstone.Services
{
    public static class GlobalSettingsService
    {
        public static void ShowSettings()
        {
            try
            {
                var app = (App)Application.Current;
                if (app?.SettingsPopup != null)
                {
                    app.SettingsPopup.ShowSettingsPopup();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to show settings: {ex.Message}");
            }
        }
    }
}
