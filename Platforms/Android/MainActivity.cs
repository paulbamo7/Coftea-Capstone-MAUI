using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace Coftea_Capstone
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            ApplyImmersiveMode();
        }

        protected override void OnResume()
        {
            base.OnResume();
            ApplyImmersiveMode();
        }

        public override void OnWindowFocusChanged(bool hasFocus)
        {
            base.OnWindowFocusChanged(hasFocus);
            if (hasFocus)
            {
                ApplyImmersiveMode();
            }
        }

        private void ApplyImmersiveMode()
        {
            try
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
                {
                    // Android 11+ (API 30): use WindowInsetsController
                    Window.SetDecorFitsSystemWindows(false);
                    var controller = Window.InsetsController;
                    if (controller != null)
                    {
                        controller.Hide(WindowInsets.Type.StatusBars() | WindowInsets.Type.NavigationBars());
                        controller.SystemBarsBehavior = (int)WindowInsetsControllerBehavior.ShowTransientBarsBySwipe;
                    }
                }
                else
                {
                    // Older Android versions: use System UI flags
                    var uiOptions = (StatusBarVisibility)(SystemUiFlags.LayoutStable
                                                          | SystemUiFlags.LayoutFullscreen
                                                          | SystemUiFlags.LayoutHideNavigation
                                                          | SystemUiFlags.Fullscreen
                                                          | SystemUiFlags.HideNavigation
                                                          | SystemUiFlags.ImmersiveSticky);
                    Window.DecorView.SystemUiVisibility = uiOptions;
                }
            }
            catch
            {
                // Ignore failures; UI will remain standard if immersive cannot be applied
            }
        }
    }
}
