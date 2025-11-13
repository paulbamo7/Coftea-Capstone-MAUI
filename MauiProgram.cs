using System.Text;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using Coftea_Capstone.Services;
using Coftea_Capstone.Models;
using Maui.PDFView;
using PdfSharpCore.Fonts;
using Coftea_Capstone.Services.Pdf;

namespace Coftea_Capstone
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseMauiPdfView();

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            try
            {
                GlobalFontSettings.FontResolver = CofteaFontResolver.Instance;
            }
            catch (NotImplementedException)
            {
                // Some platforms throw while probing the default resolver; suppress because
                // PdfSharpCore will fall back to our resolver once fonts are requested.
            }

                builder.ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("Montserrat-Black.ttf", "MontserratBlack");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            // Register core database service
            builder.Services.AddSingleton<Database>();

            return builder.Build();
        }
    }
}
