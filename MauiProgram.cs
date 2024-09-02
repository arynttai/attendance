using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace AC
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkitMediaElement()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("gilroy-medium.ttf", "Gilroy");
                    fonts.AddFont("gilroy-regular.ttf", "GilroyThin");

                })
                .UseBarcodeReader();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}

