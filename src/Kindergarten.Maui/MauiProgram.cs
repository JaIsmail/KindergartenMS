using Microsoft.Extensions.Logging;

namespace Kindergarten.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("Cairo-Regular.ttf",  "CairoRegular");
                fonts.AddFont("Cairo-Bold.ttf",     "CairoBold");
                fonts.AddFont("Outfit-Regular.ttf", "OutfitRegular");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
