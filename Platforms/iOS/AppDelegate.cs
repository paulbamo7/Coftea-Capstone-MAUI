using Foundation;
using UIKit;

namespace Coftea_Capstone
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
        
        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            // Set the background color for the entire app
            if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
            {
                // iOS 13+ - use the window background color
                var window = UIApplication.SharedApplication.KeyWindow;
                if (window != null)
                {
                    window.BackgroundColor = UIColor.FromRGB(0.757f, 0.659f, 0.573f); // #C1A892
                }
            }
            
            return base.FinishedLaunching(application, launchOptions);
        }
    }
}
