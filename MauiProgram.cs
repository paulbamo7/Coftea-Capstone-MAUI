using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using Coftea_Capstone.Services;
using Coftea_Capstone.Models;

namespace Coftea_Capstone
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit();

                builder.ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("Montserrat-Black.ttf", "MontserratBlack");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            // Register offline-first services
            builder.Services.AddSingleton<LocalDatabaseService>();
            builder.Services.AddSingleton<ConnectivityService>();
            builder.Services.AddSingleton<Database>();
            builder.Services.AddSingleton<DatabaseSyncService>();

            return builder.Build();
        }
    }
}
