using Coftea_Capstone.Views.Controls;
using Microsoft.Maui.Controls;
using System;

namespace Coftea_Capstone.Services
{
    public static class UiOverlayService
    {
        public static void CloseGlobalOverlays()
        {
            try
            {
                var app = Application.Current as App;
                try { app?.SettingsPopup?.CloseSettingsPopupCommand?.Execute(null); } catch { }
                try { app?.ManagePOSPopup?.ClosePOSManagementPopupCommand?.Execute(null); } catch { }
                try { if (app?.POSVM?.AddItemToPOSViewModel != null) app.POSVM.AddItemToPOSViewModel.IsAddItemToPOSVisible = false; } catch { }
                try { var vm = app?.POSVM?.AddItemToPOSViewModel?.ConnectPOSToInventoryVM; vm?.CloseAddonPopupCommand?.Execute(null); } catch { }
            }
            catch { }
        }
    }
}


