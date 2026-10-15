using System;
using Avalonia;
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
        // Run() handles those and exits the process. If this is called late, or after a window
        // opens, the installer flashes a UI at the user and first-run/update hooks never fire.
        var updater = VelopackApp.Build();
        if (OperatingSystem.IsWindows()) updater.OnAfterInstallFastCallback(_ => PreserveLegacyInstallerData());
        updater.Run();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static void PreserveLegacyInstallerData()
    {
        if (!OperatingSystem.IsWindows() || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RESUMEBUILDER_DATA_DIRECTORY"))) return;
        var localData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData,
            System.Environment.SpecialFolderOption.DoNotVerify);
        ResumeBuilder.Data.LegacyWorkspaceMigration.PreserveInstallerRollback(
            System.IO.Path.Combine(localData, "ResumeBuilder"), System.IO.Path.Combine(localData, "ResumeBuilderLegacyRecovery"));
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
