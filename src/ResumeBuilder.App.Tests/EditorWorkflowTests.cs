using System.Reflection;
using System.Text.Json;
using System.Xml.Linq;
using AwesomeAssertions;
using Microsoft.Data.Sqlite;
using Moq;
using ResumeBuilder.App.Services;
using ResumeBuilder.App.ViewModels;
using ResumeBuilder.Core.Models;
using ResumeBuilder.Core.SmartContent;
using ResumeBuilder.Core.Sync;
using ResumeBuilder.Core.UndoRedo;
using ResumeBuilder.Data;
using ResumeBuilder.Export;
using ResumeBuilder.Templates;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace ResumeBuilder.App.Tests;

public partial class EditorWorkflowTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "EditorTests_" + Guid.NewGuid().ToString("N"));
    private readonly ResumeDbContextFactory _factory;
    private readonly ResumeRepository _resumes;
    private readonly CoverLetterRepository _letters;
    private readonly JobApplicationRepository _applications;
    private readonly TestDialogs _dialogs = new();
    private readonly List<MainWindowViewModel> _viewModels = new();

    public EditorWorkflowTests()
    {
        Directory.CreateDirectory(_folder);
        _factory = new ResumeDbContextFactory(Path.Combine(_folder, "resumes.db"));
        using var context = _factory.CreateDbContext();
        DatabaseInitializer.Initialize(context);
        _resumes = new(_factory);
        _letters = new(_factory);
        _applications = new(_factory);
    }

    private MainWindowViewModel Editor(IResumeRepository? repository = null, IJobApplicationRepository? applications = null,
        IAiService? ai = null)
    {
        var registry = new TemplateRegistry();
        ai ??= new AiProviderRouter();
        var services = new AppServices(repository ?? _resumes, _letters, applications ?? _applications,
            registry, new ExportService(registry), new CoverLetterExportService(registry), null!, null!, new UndoRedoManager(),
            ai, null!, new JobTailoringService(ai), new CoverLetterService(ai), new UpdateService(), null!, _dialogs, new WorkspaceBackupService(_factory));
        var vm = new MainWindowViewModel(services, initialize: false);
        _viewModels.Add(vm);
        return vm;
    }

    private static object? Invoke(MainWindowViewModel vm, string name, params object[] args) =>
        typeof(MainWindowViewModel).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(vm, args);

    [Fact]
    public async Task VariantCommand_PreservesBaseAndOpensTheNewVariant()
    {
        var original = await _resumes.CreateAsync(new Resume { Name = "Base", Summary = "Original" });
        var vm = Editor();
        await vm.LoadResumeCommand.ExecuteAsync(original);
        vm.JobDescriptionText = "Job posting";
        vm.TargetRoleText = "Engineer";
        vm.Summary = "Tailored content";
        await vm.SaveAsVariantCommand.ExecuteAsync(null);
        (await _resumes.GetByIdAsync(original.Id))!.Summary.Should().Be("Original");
        vm.CurrentResume.Id.Should().NotBe(original.Id);
        vm.CurrentResume.BaseResumeId.Should().Be(original.Id);
        vm.Summary.Should().Be("Tailored content");
        vm.IsDirty.Should().BeFalse();
    }

    [Fact]
    public async Task AutosaveDuringTailoring_BranchesWithoutOverwritingBase()
    {
        var original = await _resumes.CreateAsync(new Resume { Summary = "Original" });
        var vm = Editor();
        await vm.LoadResumeCommand.ExecuteAsync(original);
        vm.JobDescriptionText = "Posting";
        vm.Summary = "Tailored";
        await (Task)Invoke(vm, "AutoSaveAsync")!;
        (await _resumes.GetByIdAsync(original.Id))!.Summary.Should().Be("Original");
        vm.CurrentResume.BaseResumeId.Should().Be(original.Id);
        (await _resumes.GetByIdAsync(vm.CurrentResume.Id))!.Summary.Should().Be("Tailored");
    }

    [Fact]
    public async Task DeleteOpenResume_DoesNotReappearOnAutosave()
    {
        var resume = await _resumes.CreateAsync(new Resume { Summary = "To delete" });
        var vm = Editor();
        await vm.LoadResumeCommand.ExecuteAsync(resume);
        await vm.DeleteResumeCommand.ExecuteAsync(resume);
        await (Task)Invoke(vm, "AutoSaveAsync")!;
        (await _resumes.GetAllAsync()).Should().BeEmpty();
        vm.IsDirty.Should().BeFalse();
    }

    [Fact]
    public async Task AutosavedVariant_IsNotDuplicatedByTheVariantButton()
    {
        var original = await _resumes.CreateAsync(new Resume { Summary = "Original" });
        var vm = Editor();
        await vm.LoadResumeCommand.ExecuteAsync(original);
        vm.JobDescriptionText = "First posting";
        await (Task)Invoke(vm, "AutoSaveAsync")!;
        var variantId = vm.CurrentResume.Id;
        await vm.SaveAsVariantCommand.ExecuteAsync(null);
        vm.CurrentResume.Id.Should().Be(variantId);
        (await _resumes.GetAllAsync()).Should().HaveCount(2);

        vm.JobDescriptionText = "First posting with a corrected requirement";
        vm.Summary = "Further tailoring";
        await (Task)Invoke(vm, "AutoSaveAsync")!;
        vm.CurrentResume.Id.Should().Be(variantId);
        vm.CurrentResume.BaseResumeId.Should().Be(original.Id);
        (await _resumes.GetAllAsync()).Should().HaveCount(2);
        (await _resumes.GetByIdAsync(original.Id))!.Summary.Should().Be("Original");
    }

    [Fact]
    public async Task DuplicateShortcut_CopiesTheActiveLetter()
    {
        var letter = await _letters.CreateAsync(new CoverLetter { Subject = "Original" });
        var vm = Editor();
        await vm.LoadCoverLetterCommand.ExecuteAsync(letter);
        vm.LetterSubject = "Copy content";
        await vm.DuplicateActiveDocumentCommand.ExecuteAsync(null);
        (await _letters.GetByIdAsync(letter.Id))!.Subject.Should().Be("Original");
        vm.CurrentLetter.Id.Should().NotBe(letter.Id);
        vm.LetterSubject.Should().Be("Copy content");
        (await _resumes.GetAllAsync()).Should().BeEmpty();
        (await _letters.GetAllAsync()).Should().HaveCount(2);
    }

    [Fact]
    public async Task SentSnapshot_OpensEvenWhenItsLinkIsMissing()
    {
        await _applications.CreateAsync(new JobApplication
        {
            ResumeId = null,
            SentResumeJson = JsonSerializer.Serialize(new Resume { Summary = "Archived sent content" })
        });
        // Repository creation intentionally owns snapshots. Emulate a restored archive instead.
        using (var context = _factory.CreateDbContext())
        {
            var row = context.JobApplications.Single();
            row.SentResumeJson = JsonSerializer.Serialize(new Resume { Summary = "Archived sent content" });
            await context.SaveChangesAsync();
        }
        var vm = Editor();
        await vm.OpenApplicationsCommand.ExecuteAsync(null);
        await vm.OpenApplicationResumeCommand.ExecuteAsync(vm.Applications.Single());
        vm.Summary.Should().Be("Archived sent content");
    }

    [Fact]
    public async Task DeleteOpenLetter_DoesNotReappearOnAutosave()
    {
        var letter = await _letters.CreateAsync(new CoverLetter { Paragraphs = ["To delete"] });
        var vm = Editor();
        await vm.LoadCoverLetterCommand.ExecuteAsync(letter);
        await vm.DeleteCoverLetterCommand.ExecuteAsync(letter);
        await (Task)Invoke(vm, "AutoSaveCoverLetterAsync")!;
        (await _letters.GetAllAsync()).Should().BeEmpty();
        vm.IsLetterDirty.Should().BeFalse();
        vm.ShowCoverLetterEditor.Should().BeFalse();
    }

    [Fact]
    public void ImportAdoption_DismissesOnboarding()
    {
        var vm = Editor();
        vm.ShowFirstRun = true;
        Invoke(vm, "AdoptImportedResume", new Resume { PersonalInfo = new() { FirstName = "Jane" } }, false);
        vm.ShowFirstRun.Should().BeFalse();
        vm.FirstName.Should().Be("Jane");
        vm.IsDirty.Should().BeTrue();
    }

    [Fact]
    public async Task LoadingAnotherResume_DiscardsPriorProposals()
    {
        var vm = Editor();
        vm.TailoredEdits.Add(new(new TailoredEdit { Target = TailoredEditTarget.Summary, Original = "", Proposed = "Wrong", Accepted = true }, "Summary"));
        var next = await _resumes.CreateAsync(new Resume { Summary = "Next" });
        await vm.LoadResumeCommand.ExecuteAsync(next);
        vm.ApplyAcceptedEditsCommand.Execute(null);
        vm.Summary.Should().Be("Next");
        vm.TailoredEdits.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveWaitsForEditsTypedDuringTheWrite()
    {
        var repository = new Mock<IResumeRepository>();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var written = new List<string>();
        repository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Resume>());
        repository.Setup(r => r.UpdateAsync(It.IsAny<Resume>())).Returns(async (Resume snapshot) =>
        {
            if (written.Count == 0) { entered.TrySetResult(); await release.Task; }
            written.Add(snapshot.Summary);
            snapshot.RowVersion = Guid.NewGuid();
            return snapshot;
        });
        var vm = Editor(repository.Object);
        vm.CurrentResume = new Resume { Id = 1 };
        vm.Summary = "First edit";
        var save = vm.SaveActiveDocumentCommand.ExecuteAsync(null);
        await entered.Task;
        vm.Summary = "Latest edit";
        release.SetResult();
        await save;
        written.Should().Equal("First edit", "Latest edit");
        vm.IsDirty.Should().BeFalse();
    }

    [Fact]
    public async Task LateAiResult_CannotAppearInAnotherDocument()
    {
        var ai = new Mock<IAiService>();
        ai.SetupGet(a => a.IsConfigured).Returns(true);
        var response = new TaskCompletionSource<AiResult<string>>(TaskCreationOptions.RunContinuationsAsynchronously);
        ai.Setup(a => a.GenerateSummaryAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).Returns(response.Task);
        var vm = Editor(ai: ai.Object);
        var request = vm.GenerateAiSummaryCommand.ExecuteAsync(null);
        await vm.LoadResumeCommand.ExecuteAsync(await _resumes.CreateAsync(new Resume { Summary = "Next document" }));
        response.SetResult(AiResult<string>.Succeeded("Old document summary"));
        await request;
        vm.AiGeneratedSummary.Should().BeNull();
        vm.Summary.Should().Be("Next document");
    }

    [Fact]
    public async Task LetterDraft_UsesItsOwnSourceAfterOpeningAnotherResume()
    {
        var source = await _resumes.CreateAsync(new Resume { Summary = "LETTER SOURCE" });
        var letter = await _letters.CreateAsync(CoverLetter.FromResume(source, "Example", "Engineer"));
        var vm = Editor();
        await vm.LoadResumeCommand.ExecuteAsync(await _resumes.CreateAsync(new Resume { Summary = "UNRELATED SOURCE" }));
        await vm.LoadCoverLetterCommand.ExecuteAsync(letter);
        await vm.DraftCoverLetterWithAiCommand.ExecuteAsync(null);
        string.Join(" ", vm.LetterParagraphs.Select(p => p.Text)).Should().Contain("LETTER SOURCE").And.NotContain("UNRELATED SOURCE");
    }

    [Fact]
    public async Task SaveAndUndo_TargetTheActiveLetter()
    {
        var vm = Editor();
        await vm.NewCoverLetterCommand.ExecuteAsync(null);
        vm.LetterSubject = "First subject";
        vm.LetterSubject = "Latest subject";
        vm.UndoCommand.Execute(null);
        vm.LetterSubject.Should().Be("");
        await vm.SaveActiveDocumentCommand.ExecuteAsync(null);
        (await _resumes.GetAllAsync()).Should().BeEmpty();
        (await _letters.GetAllAsync()).Should().ContainSingle();
    }

    [Fact]
    public async Task Tracker_KeepsFailedFinalEditPendingAndRetries()
    {
        var application = await _applications.CreateAsync(new JobApplication { Company = "Original" });
        var repository = new Mock<IJobApplicationRepository>();
        repository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<JobApplication> { application });
        repository.Setup(r => r.UpdateAsync(It.IsAny<JobApplication>())).ThrowsAsync(new IOException("Disk unavailable"));
        var vm = Editor(applications: repository.Object);
        await vm.OpenApplicationsCommand.ExecuteAsync(null);
        vm.Applications[0].Company = "Latest";
        await vm.SaveActiveDocumentCommand.ExecuteAsync(null);
        vm.HasUnsavedWork.Should().BeTrue();
        vm.ApplicationSummary.Should().Contain("not saved");
        repository.Setup(r => r.UpdateAsync(It.IsAny<JobApplication>())).Returns((JobApplication row) => _applications.UpdateAsync(row));
        await vm.SaveActiveDocumentCommand.ExecuteAsync(null);
        vm.HasUnsavedWork.Should().BeFalse();
        (await _applications.GetByIdAsync(application.Id))!.Company.Should().Be("Latest");
    }

    [Fact]
    public async Task TrackingStopsWhenTheResumeSaveFails()
    {
        var repository = new Mock<IResumeRepository>();
        repository.Setup(r => r.CreateAsync(It.IsAny<Resume>())).ThrowsAsync(new IOException("Disk unavailable"));
        var vm = Editor(repository.Object);
        vm.FirstName = "Jane";
        await vm.TrackCurrentResumeCommand.ExecuteAsync(null);
        (await _applications.GetAllAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task FailedTrackerSave_CanBeExplicitlyDiscardedOnClose()
    {
        var application = await _applications.CreateAsync(new JobApplication { Company = "Saved" });
        var repository = new Mock<IJobApplicationRepository>();
        repository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<JobApplication> { application });
        repository.Setup(r => r.UpdateAsync(It.IsAny<JobApplication>())).ThrowsAsync(new IOException("Disk unavailable"));
        var vm = Editor(applications: repository.Object);
        await vm.OpenApplicationsCommand.ExecuteAsync(null);
        vm.Applications[0].Company = "Unsaved";
        _dialogs.UnsavedChoice = UnsavedChangesChoice.Discard;
        (await vm.ConfirmDiscardAllChangesAsync()).Should().BeTrue();
        (await _applications.GetByIdAsync(application.Id))!.Company.Should().Be("Saved");
    }

    [Fact]
    public async Task ApplicationOpensSentContentAfterSourceDeletion()
    {
        var original = await _resumes.CreateAsync(new Resume { Summary = "Sent content" });
        var application = await _applications.CreateAsync(new JobApplication { ResumeId = original.Id });
        await _resumes.DeleteAsync(original.Id);
        var vm = Editor();
        await vm.OpenApplicationsCommand.ExecuteAsync(null);
        await vm.OpenApplicationResumeCommand.ExecuteAsync(vm.Applications.Single());
        vm.Summary.Should().Be("Sent content");
        vm.CurrentResume.Id.Should().Be(0);
        vm.CurrentResume.SyncId.Should().NotBe(original.SyncId);
        await vm.SaveActiveDocumentCommand.ExecuteAsync(null);
        JsonSerializer.Deserialize<Resume>((await _applications.GetByIdAsync(application.Id))!.SentResumeJson!)!.Summary.Should().Be("Sent content");
    }

    [Fact]
    public void EveryVisibleExport_ResolvesToARegisteredFormat()
    {
        using var stream = typeof(EditorWorkflowTests).Assembly.GetManifestResourceStream("ResumeBuilder.App.Tests.MainWindow.axaml")!;
        var document = XDocument.Load(stream);
        var export = new ExportService(new TemplateRegistry());
        foreach (var element in document.Descendants().Where(e => ((string?)e.Attribute("Command"))?.Contains("ExportActiveDocumentCommand") == true))
            export.GetExporter((string)element.Attribute("CommandParameter")!).Should().NotBeNull();
    }

    [Fact]
    public async Task WorkspaceRestore_ReplacesDocumentsAndClearsEditorsAndPausesSync()
    {
        await _resumes.CreateAsync(new Resume { Summary = "Old workspace" });
        var vm = Editor();
        vm.IsSyncConfigured = true;
        await (Task)Invoke(vm, "RestoreWorkspaceAsync", new WorkspaceArchive
        {
            Resumes = [new Resume { Id = 42, Summary = "Restored" }]
        })!;
        (await _resumes.GetAllAsync()).Should().ContainSingle(r => r.Id == 42 && r.Summary == "Restored");
        vm.IsSyncConfigured.Should().BeFalse();
        vm.IsDirty.Should().BeFalse();
        vm.CurrentResume.Id.Should().Be(0);
        vm.SavedResumes.Should().ContainSingle(r => r.Id == 42);
    }

    public void Dispose()
    {
        foreach (var vm in _viewModels)
            foreach (var row in vm.Applications)
                row.StopSavingAsync().GetAwaiter().GetResult();
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
    }

    private sealed class TestDialogs : IUserDialogs
    {
        public UnsavedChangesChoice UnsavedChoice { get; set; } = UnsavedChangesChoice.Save;
        public List<string> Errors { get; } = new();
        public Task ShowErrorAsync(string title, string message) { Errors.Add(message); return Task.CompletedTask; }
        public Task<bool> ConfirmAsync(string title, string message, string confirmText = "OK", string cancelText = "Cancel") => Task.FromResult(true);
        public Task<UnsavedChangesChoice> ConfirmUnsavedChangesAsync(string name) => Task.FromResult(UnsavedChoice);
        public Task<string?> PromptAsync(string title, string message, string initialValue = "") => Task.FromResult<string?>(initialValue);
    }
}
