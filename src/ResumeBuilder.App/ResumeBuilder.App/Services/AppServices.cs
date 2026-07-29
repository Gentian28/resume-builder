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
    public IResumeRepository Repository { get; }
    public ICoverLetterRepository CoverLetterRepository { get; }
    public IJobApplicationRepository JobApplicationRepository { get; }
    public TemplateRegistry TemplateRegistry { get; }
    public ExportService ExportService { get; }
    public CoverLetterExportService CoverLetterExportService { get; }
    public ThemeService ThemeService { get; }
    public ISpellChecker SpellChecker { get; }
    public UndoRedoManager UndoRedoManager { get; }
    public IAiService AiService { get; }
    public ISyncService SyncService { get; }
    public JobTailoringService TailoringService { get; }
    public CoverLetterService CoverLetterService { get; }
    public UpdateService UpdateService { get; }
    public TemplateThumbnailService TemplateThumbnailService { get; }

    public AppServices(
        IResumeRepository repository,
        ICoverLetterRepository coverLetterRepository,
        IJobApplicationRepository jobApplicationRepository,
        TemplateRegistry templateRegistry,
        ExportService exportService,
        CoverLetterExportService coverLetterExportService,
        ThemeService themeService,
        ISpellChecker spellChecker,
        UndoRedoManager undoRedoManager,
        IAiService aiService,
        ISyncService syncService,
        JobTailoringService tailoringService,
        CoverLetterService coverLetterService,
        UpdateService updateService,
        TemplateThumbnailService templateThumbnailService)
    {
        Repository = repository;
        CoverLetterRepository = coverLetterRepository;
        JobApplicationRepository = jobApplicationRepository;
        TemplateRegistry = templateRegistry;
        ExportService = exportService;
        CoverLetterExportService = coverLetterExportService;
        ThemeService = themeService;
        SpellChecker = spellChecker;
        UndoRedoManager = undoRedoManager;
        AiService = aiService;
        SyncService = syncService;
        TailoringService = tailoringService;
        CoverLetterService = coverLetterService;
        UpdateService = updateService;
        TemplateThumbnailService = templateThumbnailService;
    }
}
