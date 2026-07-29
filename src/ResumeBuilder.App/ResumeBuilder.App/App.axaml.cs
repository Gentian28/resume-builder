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
            var contextFactory = ResumeDbContextFactory.CreateInitialized();

            var repository = new ResumeRepository(contextFactory);
            var coverLetterRepository = new CoverLetterRepository(contextFactory);
            var jobApplicationRepository = new JobApplicationRepository(contextFactory);
            var templateRegistry = new TemplateRegistry();
            var exportService = new ExportService(templateRegistry);
            var coverLetterExportService = new CoverLetterExportService(templateRegistry);
            var themeService = new ThemeService();
            var spellChecker = new HunspellService();
            var undoRedoManager = new UndoRedoManager();
            // Routes to whichever provider the user picked; both stay configured independently.
            var aiService = new AiProviderRouter();
            var syncService = new LocalFolderSyncService(repository, new SyncStateStore());
            var tailoringService = new JobTailoringService(aiService);
            var coverLetterService = new CoverLetterService(aiService);
            var updateService = new UpdateService();
            // Its own PngExporter: ExportService keeps its instances private, and this one is
            // stateless, so sharing would only add coupling.
            var thumbnailService = new TemplateThumbnailService(new PngExporter(templateRegistry));

            // Apply saved theme preference
            RequestedThemeVariant = themeService.CurrentTheme;

            Services = new AppServices(
                repository,
                coverLetterRepository,
                jobApplicationRepository,
                templateRegistry,
                exportService,
                coverLetterExportService,
                themeService,
                spellChecker,
                undoRedoManager,
                aiService,
                syncService,
                tailoringService,
                coverLetterService,
                updateService,
                thumbnailService);

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