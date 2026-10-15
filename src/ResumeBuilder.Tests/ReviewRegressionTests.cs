using System.Net;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Data.Sqlite;
using QuestPDF.Fluent;
using ResumeBuilder.Core.Editing;
using ResumeBuilder.Core.Models;
using ResumeBuilder.Core.SmartContent;
using ResumeBuilder.Core.Sync;
using ResumeBuilder.Data;
using ResumeBuilder.Export;
using ResumeBuilder.Templates;
using UglyToad.PdfPig;

namespace ResumeBuilder.Tests;

public class ReviewRegressionTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "ReviewTests_" + Guid.NewGuid().ToString("N"));

    private ResumeDbContextFactory Factory(string name)
    {
        Directory.CreateDirectory(_folder);
        var factory = new ResumeDbContextFactory(Path.Combine(_folder, name + ".db"));
        using var context = factory.CreateDbContext();
        DatabaseInitializer.Initialize(context);
        return factory;
    }

    [Fact]
    public async Task AiEndpointChange_DoesNotForwardThePriorCredential()
    {
        var transport = new CapturingHandler();
        using var ai = new LocalAiService(transport);
        ai.Configure("synthetic-key");
        (await ai.GenerateSummaryAsync("Engineer", [], [])).Success.Should().BeTrue();
        ai.ConfigureLocal("http://localhost:11434/v1", "local");
        (await ai.GenerateSummaryAsync("Engineer", [], [])).Success.Should().BeTrue();
        transport.Requests.Should().Equal(
            ("api.openai.com", "synthetic-key"), ("localhost", null));
    }

    [Theory]
    [InlineData("http://remote.example.com/v1")]
    [InlineData("file:///tmp/model")]
    [InlineData("https://user:secret@example.com/v1")]
    public void InvalidAiEndpoint_DoesNotChangeConfiguration(string endpoint)
    {
        using var ai = new LocalAiService();
        Action configure = () => ai.ConfigureEndpoint(endpoint, "model", "synthetic");
        configure.Should().Throw<ArgumentException>();
        ai.BaseUrl.Should().Be(LocalAiService.OpenAiBaseUrl);
        ai.IsConfigured.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(TemplateIds))]
    public void PreparedDocuments_KeepTheirOwnVisibility(string id)
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        var template = new TemplateRegistry().GetTemplateOrDefault(id);
        var first = new Resume { Summary = "VISIBLE SUMMARY SENTINEL", PersonalInfo = new() { FirstName = "Jane" } };
        var second = new Resume();
        second.SectionOrder.SetSectionVisibility(SectionType.Summary, false);
        var document = template.CreateDocument(first);
        _ = template.CreateDocument(second);
        using var pdf = PdfDocument.Open(document.GeneratePdf());
        string.Concat(pdf.GetPages().Select(p => p.Text)).Should().Contain("VISIBLE SUMMARY SENTINEL");
    }

    public static IEnumerable<object[]> TemplateIds() => new TemplateRegistry().GetAllTemplates().Select(t => new object[] { t.Info.Id });

    [Theory]
    [InlineData("letter-modern")]
    [InlineData("letter-classic")]
    [InlineData("letter-minimal")]
    public void PreparedCoverLetters_KeepTheirOwnStyling(string id)
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        var registry = new TemplateRegistry();
        var template = registry.GetCoverLetterTemplateOrDefault(id);
        var first = new CoverLetter { PersonalInfo = new() { FirstName = "Jane" }, Paragraphs = ["Body"] };
        var document = template.CreateDocument(first);
        _ = template.CreateDocument(new CoverLetter { TemplateSettings = new() { FontSizeScale = 1.5f } });
        using var actual = PdfDocument.Open(document.GeneratePdf());
        using var expected = PdfDocument.Open(new TemplateRegistry().GetCoverLetterTemplateOrDefault(id).CreateDocument(first).GeneratePdf());
        actual.GetPage(1).Letters.Max(l => l.FontSize).Should().Be(expected.GetPage(1).Letters.Max(l => l.FontSize));
    }

    [Fact]
    public async Task MultiPagePng_ReturnsRealPathsAndDoesNotOverwriteSiblingFiles()
    {
        Directory.CreateDirectory(_folder);
        var service = new ExportService(new TemplateRegistry());
        var resume = new Resume { Summary = string.Join(" ", Enumerable.Repeat("Synthetic content.", 1200)) };
        var path = Path.Combine(_folder, "resume.png");
        var paths = await service.ExportToFileAsync(resume, "PNG", path);
        paths.Count.Should().BeGreaterThan(1);
        paths.Should().OnlyContain(p => File.Exists(p));
        var before = await File.ReadAllBytesAsync(paths[0]);
        Func<Task> repeat = () => service.ExportToFileAsync(resume, "PNG", path);
        await repeat.Should().ThrowAsync<IOException>();
        (await File.ReadAllBytesAsync(paths[0])).Should().Equal(before);
    }

    [Fact]
    public void Tailoring_RejectsNewerTextAndMovedTargets()
    {
        var first = new Experience { Achievements = ["Same bullet"] };
        var other = new Experience { Achievements = ["Same bullet"] };
        var resume = new Resume { Summary = "Newer text", Experiences = [other, first] };
        var edits = new[]
        {
            new TailoredEdit { Target = TailoredEditTarget.Summary, Original = "Old text", Proposed = "Wrong", Accepted = true },
            new TailoredEdit { Target = TailoredEditTarget.ExperienceAchievement, ItemKey = first.EditKey, ItemIndex = 0, SubIndex = 0,
                Original = "Same bullet", Proposed = "Wrong target", Accepted = true }
        };
        JobTailoringService.Apply(resume, edits).Should().Be(0);
        resume.Summary.Should().Be("Newer text");
        other.Achievements.Should().Equal("Same bullet");
    }

    [Fact]
    public void KeywordText_ExcludesHiddenSections()
    {
        var resume = new Resume { Skills = [new Skill { Name = "Kubernetes" }] };
        resume.SectionOrder.SetSectionVisibility(SectionType.Skills, false);
        ResumeTextExtractor.ToPlainText(resume).Should().NotContain("Kubernetes");
        ResumeTextExtractor.ToPlainText(resume, includeHidden: true).Should().Contain("Kubernetes");
    }

    [Fact]
    public async Task JsonEntryPoint_PreservesNativeDataAndRejectsOtherJson()
    {
        var export = new ExportService(new TemplateRegistry());
        var resume = new Resume { Summary = "Summary", PersonalInfo = new() { FirstName = "Jane" }, Experiences = [new() { Company = "Example" }] };
        using var json = new MemoryStream(await export.ExportAsync(resume, "JSON"));
        var imported = await export.ImportAsync(json, "JSON");
        imported.Data!.PersonalInfo.FirstName.Should().Be("Jane");
        imported.Data.Experiences.Single().Company.Should().Be("Example");
        using var unrelated = new MemoryStream(Encoding.UTF8.GetBytes("{\"message\":\"not a resume\"}"));
        (await export.ImportAsync(unrelated, "JSON")).Success.Should().BeFalse();
    }

    [Fact]
    public async Task SyncFolderChange_StartsAnIndependentHistory()
    {
        var repository = new ResumeRepository(Factory("switch"));
        await repository.CreateAsync(new Resume { Name = "Local" });
        var sync = new LocalFolderSyncService(repository, new SyncStateStore(Path.Combine(_folder, "state.json")));
        await sync.ConfigureAsync(Path.Combine(_folder, "a"));
        (await sync.SyncAllAsync()).UploadedCount.Should().Be(1);
        await sync.ConfigureAsync(Path.Combine(_folder, "b"));
        (await sync.SyncAllAsync()).UploadedCount.Should().Be(1);
        await sync.ConfigureAsync(Path.Combine(_folder, "a"));
        (await sync.SyncAllAsync()).UploadedCount.Should().Be(0);
    }

    [Fact]
    public async Task SyncedVariant_ResolvesTheParentInTheDestinationDatabase()
    {
        var source = new ResumeRepository(Factory("source"));
        var parent = await source.CreateAsync(new Resume { Name = "Base" });
        var variant = await source.CreateVariantAsync(parent.Id, "Engineer", "Posting");
        var syncFolder = Path.Combine(_folder, "shared");
        var first = new LocalFolderSyncService(source, new SyncStateStore(Path.Combine(_folder, "first-state")));
        await first.ConfigureAsync(syncFolder);
        (await first.SyncAllAsync()).Success.Should().BeTrue();
        var destination = new ResumeRepository(Factory("destination"));
        await destination.CreateAsync(new Resume { Name = "Unrelated" });
        var second = new LocalFolderSyncService(destination, new SyncStateStore(Path.Combine(_folder, "second-state")));
        await second.ConfigureAsync(syncFolder);
        (await second.SyncAllAsync()).Success.Should().BeTrue();
        var imported = (await destination.GetAllAsync()).Single(r => r.SyncId == variant.SyncId);
        var importedParent = await destination.GetByIdAsync(imported.BaseResumeId!.Value);
        importedParent!.SyncId.Should().Be(parent.SyncId);
        await File.WriteAllTextAsync(Path.Combine(syncFolder, "archived.deleted.json"), JsonSerializer.Serialize(new Resume()));
        (await second.ListRemoteAsync()).Should().NotContain(r => r.Path.EndsWith(".deleted.json"));
    }

    [Fact]
    public async Task ApplicationSnapshot_SurvivesEditsAndDeletionAndCannotBeReplaced()
    {
        var factory = Factory("snapshot");
        var resumes = new ResumeRepository(factory);
        var applications = new JobApplicationRepository(factory);
        var resume = await resumes.CreateAsync(new Resume { Summary = "Sent content" });
        var application = await applications.CreateAsync(new JobApplication { ResumeId = resume.Id });
        resume.Summary = "Later content";
        await resumes.UpdateAsync(resume);
        await resumes.DeleteAsync(resume.Id);
        application.SentResumeJson = "attempted replacement";
        application.Notes = "Interview next week";
        var saved = await applications.UpdateAsync(application);
        JsonSerializer.Deserialize<Resume>(saved.SentResumeJson!)!.Summary.Should().Be("Sent content");
        saved.ResumeCapturedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ApplicationEdits_PreserveCreationAndAppliedDates()
    {
        var applications = new JobApplicationRepository(Factory("dates"));
        var saved = await applications.CreateAsync(new JobApplication { Status = ApplicationStatus.Saved });
        var createdAt = saved.CreatedAt;
        saved.Status = ApplicationStatus.Applied;
        saved = await applications.UpdateAsync(saved);
        var appliedOn = saved.AppliedOn;
        saved.Notes = "Edited notes";
        saved.AppliedOn = null;
        saved.CreatedAt = DateTime.UtcNow.AddYears(-10);
        saved = await applications.UpdateAsync(saved);
        saved.CreatedAt.Should().Be(createdAt);
        saved.AppliedOn.Should().Be(appliedOn);
    }

    [Fact]
    public async Task PendingSave_RetainsFailureAndFlushesNewerEditsInOrder()
    {
        var text = "First";
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var saved = new List<string>();
        var fail = true;
        var queue = new PendingSave<string>(() => text, async value =>
        {
            if (fail) throw new IOException("Disk unavailable");
            if (saved.Count == 0) { entered.TrySetResult(); await release.Task; }
            saved.Add(value);
            return value;
        }, _ => { }, () => { }, TimeSpan.FromHours(1));
        queue.MarkDirty();
        (await queue.FlushAsync()).Should().BeFalse();
        queue.HasChanges.Should().BeTrue();
        fail = false;
        var flush = queue.FlushAsync();
        await entered.Task;
        text = "Latest";
        queue.MarkDirty();
        release.SetResult();
        (await flush).Should().BeTrue();
        saved.Should().Equal("First", "Latest");
        queue.HasChanges.Should().BeFalse();
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
    }

    [Fact]
    public async Task AtomicBinaryWrite_PreservesExistingOutputOnCancellationOrCollision()
    {
        Directory.CreateDirectory(_folder);
        var path = Path.Combine(_folder, "export.pdf");
        await File.WriteAllTextAsync(path, "Previous export");
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        Func<Task> write = () => AtomicFile.WriteAllBytesAsync(path, [1, 2, 3], cancellationToken: cancelled.Token);
        await write.Should().ThrowAsync<OperationCanceledException>();
        (await File.ReadAllTextAsync(path)).Should().Be("Previous export");
        write = () => AtomicFile.WriteAllBytesAsync(path, [1, 2, 3], overwrite: false);
        await write.Should().ThrowAsync<IOException>();
        (await File.ReadAllTextAsync(path)).Should().Be("Previous export");
        Directory.GetFiles(_folder).Should().ContainSingle();
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public List<(string Host, string? Key)> Requests { get; } = new();
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add((request.RequestUri!.Host, request.Headers.Authorization?.Parameter));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"choices\":[{\"message\":{\"content\":\"Summary\"}}]}", Encoding.UTF8, "application/json")
            });
        }
    }
}
