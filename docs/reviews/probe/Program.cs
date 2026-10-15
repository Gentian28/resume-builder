using System.Reflection;
using System.Text.Json;
using ResumeBuilder.App.Services;
using ResumeBuilder.App.ViewModels;
using ResumeBuilder.Core.Models;
using ResumeBuilder.Core.SmartContent;
using ResumeBuilder.Core.Sync;
using ResumeBuilder.Core.UndoRedo;
using ResumeBuilder.Data;
using ResumeBuilder.Export;
using ResumeBuilder.Templates;
using QuestPDF.Fluent;
using UglyToad.PdfPig;

var scratch = Path.Combine(Path.GetTempPath(), "resumebuilder-audit-data-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(scratch);
var registry = new TemplateRegistry();
var export = new ExportService(registry);
var observed = 0;
void Found(string id, bool condition, string detail)
{
    Console.WriteLine($"{id}: {(condition ? "CONFIRMED" : "NOT REPRODUCED")} | {detail}");
    if (condition) observed++;
}

ResumeDbContextFactory Factory(string name)
{
    var f = new ResumeDbContextFactory(Path.Combine(scratch, name + ".db"));
    using var db = f.CreateDbContext();
    DatabaseInitializer.Initialize(db);
    return f;
}

var factory = Factory("main");
var repo = new ResumeRepository(factory);
var applications = new JobApplicationRepository(factory);
var letters = new CoverLetterRepository(factory);

var source = new Resume
{
    Name = "Synthetic audit resume",
    Summary = "Original summary",
    PersonalInfo = new PersonalInfo { FirstName = "Jane", LastName = "Doe" },
    Experiences = new List<Experience> { new() { JobTitle = "Engineer", Company = "Example", Achievements = new() { "Original bullet" } } }
};
var native = await export.ExportAsync(source, "JSON");
using (var stream = new MemoryStream(native))
{
    var viaUi = await export.ImportAsync(stream, "JSON Resume");
    Found("B01", viaUi.Success && viaUi.Data?.PersonalInfo.FirstName != "Jane", $"UI route returns success={viaUi.Success}, firstName='{viaUi.Data?.PersonalInfo.FirstName}', experienceCount={viaUi.Data?.Experiences.Count}");
}
using (var stream = new MemoryStream(native))
{
    var correct = await export.ImportAsync(stream, "JSON");
    Console.WriteLine($"B01 control: sniffer preserved firstName='{correct.Data?.PersonalInfo.FirstName}'");
}
Found("B02", export.GetExporter("TXT") is null && export.GetExporter("TEXT") is not null, "Toolbar TXT does not resolve; registered format is TEXT");

using (var ai = new LocalAiService())
{
    ai.Configure("synthetic-audit-key");
    ai.ConfigureLocal("http://localhost:11434/v1", "local-model");
    var http = (HttpClient)typeof(LocalAiService).GetField("_httpClient", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(ai)!;
    Found("B03", http.DefaultRequestHeaders.Authorization?.Parameter == "synthetic-audit-key", "Switching to local endpoint retains prior bearer header (synthetic key only, no requests sent)");
}

source = await repo.CreateAsync(source);
var application = await applications.CreateAsync(new JobApplication { ResumeId = source.Id, Company = "Example Inc" });
source.Summary = "Edited after application";
await repo.UpdateAsync(source);
var reopened = await repo.GetByIdAsync((await applications.GetByIdAsync(application.Id))!.ResumeId!.Value);
Found("B04", reopened?.Summary == "Edited after application", "Tracked application now opens a resume edited after tracking");

var historic = await applications.CreateAsync(new JobApplication { Company = "Historic", CreatedAt = new DateTime(2020,1,1), Status = ApplicationStatus.Saved });
var initialCreated = historic.CreatedAt;
var row = new JobApplicationViewModel(historic);
row.Status = ApplicationStatus.Applied;
await applications.UpdateAsync(row.ToModel());
var firstUpdate = await applications.GetByIdAsync(historic.Id);
await Task.Delay(30);
await applications.UpdateAsync(row.ToModel());
var secondUpdate = await applications.GetByIdAsync(historic.Id);
Found("B05", row.AppliedOn is null && secondUpdate?.AppliedOn > firstUpdate?.AppliedOn && secondUpdate?.CreatedAt != initialCreated,
    $"Saved->Applied row date stays null={row.AppliedOn is null}; every edit replaces AppliedOn and CreatedAt");

var stale = new Resume { Summary = "New manual wording" };
var n = JobTailoringService.Apply(stale, new[] { new TailoredEdit { Target = TailoredEditTarget.Summary, Original = "Old wording", Proposed = "Stale AI wording", Accepted = true } });
Found("B06", n == 1 && stale.Summary == "Stale AI wording", "Applying a rewrite ignores whether Original matches current text");

var syncA = new LocalFolderSyncService(repo, new SyncStateStore(Path.Combine(scratch, "switch-state.json")));
var folderA = Path.Combine(scratch, "folder-a");
var folderB = Path.Combine(scratch, "folder-b");
await syncA.ConfigureAsync(folderA);
await syncA.SyncAllAsync();
await syncA.ConfigureAsync(folderB);
var switched = await syncA.SyncAllAsync();
Found("B07", switched.UploadedCount == 0 && !Directory.GetFiles(folderB, "*.json").Any(),
    $"New sync folder uploaded={switched.UploadedCount}, warnings={switched.Warnings.Count}");

var variant = await repo.CreateVariantAsync(source.Id, "Target role", "Posting");
await syncA.ConfigureAsync(folderA);
await syncA.SyncAllAsync();
var factoryB = Factory("other-machine");
var repoB = new ResumeRepository(factoryB);
var unrelated = await repoB.CreateAsync(new Resume { Name = "Unrelated local resume" });
var syncB = new LocalFolderSyncService(repoB, new SyncStateStore(Path.Combine(scratch, "other-state.json")));
await syncB.ConfigureAsync(folderA);
await syncB.SyncAllAsync();
var importedVariant = (await repoB.GetAllAsync()).Single(r => r.SyncId == variant.SyncId);
var parent = await repoB.GetByIdAsync(importedVariant.BaseResumeId!.Value);
Found("B08", parent?.SyncId != source.SyncId, $"Imported variant points to '{parent?.Name}', not its synced base");

var multi = new Resume { PersonalInfo = new PersonalInfo { FirstName = "Jane" }, Summary = string.Join(" ", Enumerable.Repeat("Synthetic review content.", 1200)) };
var pngPath = Path.Combine(scratch, "export.png");
await export.ExportToFileAsync(multi, "PNG", pngPath);
var pngs = Directory.GetFiles(scratch, "export*.png");
Found("B09", pngs.Length > 1 && !File.Exists(pngPath), $"PNG writes {pngs.Length} page files, but toast's export.png does not exist");

var services = new AppServices(repo, letters, applications, registry, export, new CoverLetterExportService(registry),
    null!, null!, new UndoRedoManager(), new AiProviderRouter(), null!, null!, new CoverLetterService(new AiProviderRouter()), new UpdateService(), null!);
MainWindowViewModel Vm(IResumeRepository? replacement = null)
{
    var vm = new MainWindowViewModel();
    var s = replacement is null ? services : new AppServices(replacement, letters, applications, registry, export,
        new CoverLetterExportService(registry), null!, null!, new UndoRedoManager(), new AiProviderRouter(), null!, null!, null!, new UpdateService(), null!);
    typeof(MainWindowViewModel).GetField("_services", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(vm, s);
    return vm;
}
object? Call(object vm, string method, params object[] args) => typeof(MainWindowViewModel).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(vm, args);
try
{
    var vm = Vm();
    vm.FirstName = "Jane";
    vm.Summary = "Tailored content overwrites the base";
    vm.JobDescriptionText = "Example role posting";
    vm.TargetRoleText = "Engineer";
    vm.CurrentResume = await repo.CreateAsync(new Resume { Name = "Base for variant probe", Summary = "Untailored base" });
    var baseId = vm.CurrentResume.Id;
    await vm.SaveAsVariantCommand.ExecuteAsync(null);
    Found("B10", (await repo.GetByIdAsync(baseId))?.Summary == "Tailored content overwrites the base" && vm.CurrentResume.Id == baseId,
        "Save as variant overwrites base and leaves editor on base");

    var delayedRepo = new DelayedRepository(repo);
    var vm2 = Vm(delayedRepo);
    vm2.CurrentResume = await repo.CreateAsync(new Resume { Name = "Save race probe" });
    vm2.Summary = "Before save";
    var saving = (Task<bool>)Call(vm2, "SaveAsync", false)!;
    await delayedRepo.Entered.Task;
    vm2.Summary = "Typed during save";
    delayedRepo.Release.SetResult();
    var saved = await saving;
    Found("B11", saved && vm2.IsDirty && (await repo.GetByIdAsync(vm2.CurrentResume.Id))?.Summary != vm2.Summary,
        $"SaveAsync returns {saved} while IsDirty={vm2.IsDirty}; navigation/update callers treat it as permission to proceed");

    var vm3 = Vm();
    vm3.CurrentResume = await repo.CreateAsync(new Resume { Name = "Deletion probe", Summary = "Keep nothing" });
    vm3.FirstName = "Jane";
    var oldId = vm3.CurrentResume.Id;
    await repo.DeleteAsync(oldId);
    vm3.CurrentResume.Id = 0;
    Call(vm3, "MarkDirty");
    await (Task)Call(vm3, "AutoSaveAsync")!;
    Found("B12", vm3.CurrentResume.Id != 0 && (await repo.GetAllAsync()).Any(r => r.Id == vm3.CurrentResume.Id),
        "Executing the post-confirmation deletion state and autosave recreates the deleted resume");

    var vm4 = Vm();
    vm4.CurrentResume = await repo.CreateAsync(new Resume { Name = "Other resume", Summary = "Other" });
    vm4.Summary = "Current document";
    vm4.TailoredEdits.Add(new TailoredEditViewModel(new TailoredEdit { Target = TailoredEditTarget.Summary, Original = "Prior document", Proposed = "Rewrite from prior document", Accepted = true }, "summary"));
    vm4.IsDirty = false;
    await vm4.LoadResumeCommand.ExecuteAsync(await repo.CreateAsync(new Resume { Name = "Next resume" }));
    vm4.ApplyAcceptedEditsCommand.Execute(null);
    Found("B13", vm4.Summary == "Rewrite from prior document", "A proposal from the previous document applies to the newly loaded resume");

    var vm5 = Vm();
    vm5.CurrentResume = new Resume { PersonalInfo = new PersonalInfo { FirstName = "Other" } };
    vm5.Summary = "WRONG RESUME SUMMARY";
    vm5.JobTitle = "Wrong role";
    vm5.CurrentLetter = new CoverLetter { Name = "Letter for different resume", ResumeId = 555, Paragraphs = new() { "Original letter" } };
    vm5.LetterName = vm5.CurrentLetter.Name;
    await vm5.DraftCoverLetterWithAiCommand.ExecuteAsync(null);
    Found("B17", vm5.LetterParagraphs.Any(p => p.Text.Contains("WRONG RESUME SUMMARY")), "Drafting an existing letter uses the unrelated current resume, not letter.ResumeId");

    var vm6 = Vm();
    vm6.ShowFirstRun = true;
    Call(vm6, "AdoptImportedResume", new Resume { PersonalInfo = new PersonalInfo { FirstName = "Jane" } }, false);
    Found("B20", vm6.ShowFirstRun && vm6.FirstName == "Jane", "Successful first-run import leaves onboarding visible over populated editor");
}
catch (Exception ex)
{
    Console.WriteLine($"UI probe exception: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
}

var archives = new LocalFolderSyncService(repoB, new SyncStateStore(Path.Combine(scratch, "archive-state.json")));
var archiveFolder = Path.Combine(scratch, "archives");
await archives.ConfigureAsync(archiveFolder);
await File.WriteAllTextAsync(Path.Combine(archiveFolder, "test.deleted.json"), JsonSerializer.Serialize(new Resume { Name = "Deleted document" }));
Found("B14", (await archives.ListRemoteAsync()).Any(), "Remote list includes a parked .deleted.json archive");

var a = new Resume { Summary = "SHOULD APPEAR IN A", PersonalInfo = new PersonalInfo { FirstName = "Jane" } };
var b = new Resume { Summary = "B", PersonalInfo = new PersonalInfo { FirstName = "Other" } };
b.SectionOrder.SetSectionVisibility(SectionType.Summary, false);
var template = registry.GetTemplateOrDefault("modern");
var documentA = template.CreateDocument(a);
_ = template.CreateDocument(b);
using (var parsed = PdfDocument.Open(documentA.GeneratePdf()))
{
    var text = string.Join(" ", parsed.GetPages().Select(p => p.Text));
    Found("B15", !text.Contains("SHOULD APPEAR IN A"), "Reusing template B's state removes A's visible summary from document A");
}
var hidden = new Resume { Skills = new() { new Skill { Name = "Kubernetes" } } };
hidden.SectionOrder.SetSectionVisibility(SectionType.Skills, false);
Found("B16", ResumeTextExtractor.ToPlainText(hidden).Contains("Kubernetes"), "Keyword analysis includes hidden skills that exported document omits");

Console.WriteLine($"Confirmed probes: {observed}");
Console.WriteLine($"Scratch data: {scratch}");

sealed class DelayedRepository(IResumeRepository inner) : IResumeRepository
{
    public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public Task<List<Resume>> GetAllAsync() => inner.GetAllAsync();
    public Task<Resume?> GetByIdAsync(int id) => inner.GetByIdAsync(id);
    public Task<Resume> CreateAsync(Resume r) => inner.CreateAsync(r);
    public async Task<Resume> UpdateAsync(Resume r)
    {
        var copy = ResumeRepository.DeepClone(r);
        Entered.SetResult();
        await Release.Task;
        await inner.UpdateAsync(copy);
        r.RowVersion = copy.RowVersion;
        return r;
    }
    public Task DeleteAsync(int id) => inner.DeleteAsync(id);
    public Task<Resume> DuplicateAsync(int id) => inner.DuplicateAsync(id);
    public Task<Resume> CreateVariantAsync(int id, string role, string job) => inner.CreateVariantAsync(id, role, job);
    public Task<List<Resume>> GetVariantsAsync(int id) => inner.GetVariantsAsync(id);
}
