using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Styling;
using System.Linq;
using Avalonia.Markup.Xaml;
using ResumeBuilder.App.ViewModels;
using ResumeBuilder.App.Views;
using ResumeBuilder.App.Services;
using ResumeBuilder.Core.SmartContent;
using ResumeBuilder.Core.SpellCheck;
using ResumeBuilder.Core.Sync;
using ResumeBuilder.Core.UndoRedo;
using ResumeBuilder.Data;
using ResumeBuilder.Templates;
using ResumeBuilder.Export;
using Microsoft.EntityFrameworkCore;

namespace ResumeBuilder.App;

public partial class App : Application
{
    public static AppServices Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();

            // Initialize services
            var dbContext = new ResumeDbContext();
            dbContext.Database.EnsureCreated();

            var repository = new ResumeRepository(dbContext);
            var templateRegistry = new TemplateRegistry();
            var exportService = new ExportService(templateRegistry);
            var themeService = new ThemeService();
            var spellChecker = new HunspellService();
            var undoRedoManager = new UndoRedoManager();
            var aiService = new LocalAiService();
            var syncService = new LocalFolderSyncService();

            // Apply saved theme preference
            RequestedThemeVariant = themeService.CurrentTheme;

            Services = new AppServices(dbContext, repository, templateRegistry, exportService, themeService, spellChecker, undoRedoManager, aiService, syncService);

            // Initialize spell checker in background
            _ = spellChecker.InitializeAsync();

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(Services),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}