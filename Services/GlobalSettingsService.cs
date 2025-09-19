using Coftea_Capstone.ViewModel.Controls;
using Coftea_Capstone.ViewModel;

namespace Coftea_Capstone.Services
{
    public static class GlobalSettingsService
    {
        private static SettingsPopUpViewModel _settingsPopup;
        
        public static SettingsPopUpViewModel SettingsPopup
        {
            get
            {
                if (_settingsPopup == null)
                {
                    // Get from App instance
                    if (Application.Current is App app)
                    {
                        _settingsPopup = app.SettingsPopup;
                    }
                }
                return _settingsPopup;
            }
        }
        
        public static void ShowSettings()
        {
            SettingsPopup?.ShowSettingsPopupCommand.Execute(null);
        }
    }
}
