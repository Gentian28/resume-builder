using Avalonia;
using System;
using Velopack;

namespace ResumeBuilder.App;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // Must be the first thing that runs, before Avalonia and before any window exists.
        // Velopack re-launches the app with hook arguments during install, update and uninstall;
        // Run() handles those and exits the process. If this is called late — or after a window
        // opens — the installer flashes a UI at the user and first-run/update hooks never fire.
        VelopackApp.Build().Run();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
