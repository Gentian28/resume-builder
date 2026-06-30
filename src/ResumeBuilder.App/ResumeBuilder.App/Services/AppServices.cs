using ResumeBuilder.Core.SmartContent;
using ResumeBuilder.Core.SpellCheck;
using ResumeBuilder.Core.Sync;
using ResumeBuilder.Core.UndoRedo;
using ResumeBuilder.Data;
using ResumeBuilder.Templates;
using ResumeBuilder.Export;

namespace ResumeBuilder.App.Services;

public class AppServices
{
    public ResumeDbContext DbContext { get; }
    public IResumeRepository Repository { get; }
    public TemplateRegistry TemplateRegistry { get; }
    public ExportService ExportService { get; }
    public ThemeService ThemeService { get; }
    public ISpellChecker SpellChecker { get; }
    public UndoRedoManager UndoRedoManager { get; }
    public IAiService AiService { get; }
    public ISyncService SyncService { get; }

    public AppServices(
        ResumeDbContext dbContext,
        IResumeRepository repository,
        TemplateRegistry templateRegistry,
        ExportService exportService,
        ThemeService themeService,
        ISpellChecker spellChecker,
        UndoRedoManager undoRedoManager,
        IAiService aiService,
        ISyncService syncService)
    {
        DbContext = dbContext;
        Repository = repository;
        TemplateRegistry = templateRegistry;
        ExportService = exportService;
        ThemeService = themeService;
        SpellChecker = spellChecker;
        UndoRedoManager = undoRedoManager;
        AiService = aiService;
        SyncService = syncService;
    }
}
