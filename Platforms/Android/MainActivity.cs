using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Microsoft.Maui;

namespace Coftea_Capstone.Platforms.Android
{
    [Activity(Label = "Coftea_Capstone", Theme = "@style/Maui.SplashTheme", MainLauncher = true, 
              ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density,
              LaunchMode = LaunchMode.SingleTop)]
    [IntentFilter(new[] { Intent.ActionView },
                  Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
                  DataScheme = "coftea")]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            // Handle deep link if app was launched via intent
            HandleIntent(Intent);
        }

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            
            // Handle deep link if app was already running
            HandleIntent(intent);
        }

        private void HandleIntent(Intent intent)
        {
            if (intent?.Data != null)
            {
                var url = intent.Data.ToString();
                System.Diagnostics.Debug.WriteLine($"🔗 Android deep link: {url}");
                
                // Call the app's deep link handler
                if (Microsoft.Maui.Controls.Application.Current is Coftea_Capstone.App app)
                {
                    app.HandleDeepLink(url);
                }
            }
        }
    }
}