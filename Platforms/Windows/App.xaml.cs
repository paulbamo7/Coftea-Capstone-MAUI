using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;

namespace Coftea_Capstone.Platforms.Windows
{
    public partial class App : Microsoft.UI.Xaml.Application
    {
        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            // Handle deep link if app was launched via protocol
            if (args?.Arguments != null && args.Arguments.StartsWith("coftea://"))
            {
                System.Diagnostics.Debug.WriteLine($"🔗 Windows deep link: {args.Arguments}");
                
                // Call the app's deep link handler
                if (Microsoft.Maui.Controls.Application.Current is Coftea_Capstone.App app)
                {
                    app.HandleDeepLink(args.Arguments);
                }
            }
        }
    }
}