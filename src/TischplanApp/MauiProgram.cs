using Microsoft.Extensions.Logging;
using TischplanApp.Controls;

namespace TischplanApp;

/// <summary>
/// MAUI Application configuration and initialization.
/// </summary>
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                // Optional: Add custom fonts if available
                // fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                // fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .ConfigureMauiHandlers(handlers =>
            {
#if ANDROID
                handlers.AddHandler<ZoomPanCanvas, Platforms.Android.Handlers.ZoomPanCanvasHandler>();
#elif IOS
                handlers.AddHandler<ZoomPanCanvas, Platforms.iOS.Handlers.ZoomPanCanvasHandler>();
#endif
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
