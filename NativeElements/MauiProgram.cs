using Microsoft.Extensions.Logging;
using NativeElements.Data;
using NativeElements.ViewModels;

namespace NativeElements;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Register ViewModels
        builder.Services.AddSingleton<PetalViewModel>();
        builder.Services.AddSingleton<SegmentedRingViewModel>();
        builder.Services.AddSingleton<HistoryViewModel>();
        builder.Services.AddSingleton<CushionViewModel>();

        // Register Views
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddSingleton<PetalPage>();
        builder.Services.AddSingleton<SegmentedRingPage>();
        builder.Services.AddSingleton<CushionCalculatorPage>();
        builder.Services.AddSingleton<HistoryPage>();
        builder.Services.AddSingleton<DeveloperPage>();
        builder.Services.AddSingleton<SettingsPage>();

        // Register Services
        builder.Services.AddSingleton<DatabaseService>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}

