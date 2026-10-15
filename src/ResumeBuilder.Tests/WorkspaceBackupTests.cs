using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Data.Sqlite;
using ResumeBuilder.Core.Editing;
using ResumeBuilder.Core.Models;
using ResumeBuilder.Core.SmartContent;
using ResumeBuilder.Data;

namespace ResumeBuilder.Tests;

public class WorkspaceBackupTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "WorkspaceTests_" + Guid.NewGuid().ToString("N"));

    private ResumeDbContextFactory Factory(string name)
    {
        Directory.CreateDirectory(_folder);
        var factory = new ResumeDbContextFactory(Path.Combine(_folder, name + ".db"));
        using var context = factory.CreateDbContext();
        DatabaseInitializer.Initialize(context);
        return factory;
    }

    [Fact]
    public async Task CompleteBackup_RestoresRelationshipsAndSentSnapshots()
    {
        var source = Factory("source");
        var resumes = new ResumeRepository(source);
        var resume = await resumes.CreateAsync(new Resume { Name = "Base", Summary = "Sent content" });
        var variant = await resumes.CreateVariantAsync(resume.Id, "Engineer", "Posting");
        var letter = await new CoverLetterRepository(source).CreateAsync(CoverLetter.FromResume(variant, "Example"));
        var application = await new JobApplicationRepository(source, new ResumeBuilder.Export.SentDocumentRenderer(new ResumeBuilder.Templates.TemplateRegistry())).CreateAsync(new JobApplication { ResumeId = variant.Id, Company = "Example", CoverLetterId = letter.Id, Contact = "Recruiter", NextActionOn = new DateTime(2026, 10, 20) });
        var path = Path.Combine(_folder, "workspace.json");
        await new WorkspaceBackupService(source).ExportToFileAsync(path);
        var archive = WorkspaceBackupService.Read(await File.ReadAllBytesAsync(path));
        var target = Factory("target");
        await new ResumeRepository(target).CreateAsync(new Resume { Name = "Old target" });
        var safety = await new WorkspaceBackupService(target).RestoreAsync(archive);
        File.Exists(safety).Should().BeTrue();
        var restored = await new ResumeRepository(target).GetByIdAsync(variant.Id);
        restored!.BaseResumeId.Should().Be(resume.Id);
        restored.BaseResumeSyncId.Should().Be(resume.SyncId);
        (await new CoverLetterRepository(target).GetByIdAsync(letter.Id))!.ResumeId.Should().Be(variant.Id);
        var restoredApplication = await new JobApplicationRepository(target).GetByIdAsync(application.Id);
        JsonSerializer.Deserialize<Resume>(restoredApplication!.SentResumeJson!)!.Summary.Should().Be("Sent content");
        restoredApplication.RowVersion.Should().NotBe(application.RowVersion);
        restoredApplication.SentResumePdf.Should().Equal(application.SentResumePdf!);
        restoredApplication.SentCoverLetterPdf.Should().Equal(application.SentCoverLetterPdf!);
        restoredApplication.Contact.Should().Be("Recruiter");
        restoredApplication.NextActionOn.Should().Be(new DateTime(2026, 10, 20));
        using var old = new ResumeDbContextFactory(safety!).CreateDbContext();
        old.Resumes.Single().Name.Should().Be("Old target");
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"format\":\"ResumeBuilderWorkspace\",\"version\":99,\"resumes\":[],\"coverLetters\":[],\"applications\":[]}")]
    [InlineData("{\"format\":\"ResumeBuilderWorkspace\",\"version\":1}")]
    public void OtherOrIncompleteFiles_CannotBecomeAnEmptyRestore(string json)
    {
        Action read = () => WorkspaceBackupService.Read(Encoding.UTF8.GetBytes(json));
        read.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public async Task FailedRestore_RollsBackTheWholeWorkspace()
    {
        var target = Factory("rollback");
        var repository = new ResumeRepository(target);
        await repository.CreateAsync(new Resume { Name = "Keep me" });
        var archive = new WorkspaceArchive { Resumes = [new Resume { Id = 1, Name = null! }] };
        Func<Task> restore = () => new WorkspaceBackupService(target).RestoreAsync(archive);
        await restore.Should().ThrowAsync<Exception>();
        (await repository.GetAllAsync()).Single().Name.Should().Be("Keep me");
    }

    [Fact]
    public void Preferences_PersistLocalConfigurationWithoutCredentials()
    {
        Directory.CreateDirectory(_folder);
        var path = Path.Combine(_folder, "preferences.json");
        var store = new AppPreferencesStore(path);
        store.Save(new AppPreferences { AiBaseUrl = "http://localhost:11434/v1", OpenAiModel = "local-model", LastResumeId = 7 });
        var read = new AppPreferencesStore(path).Load();
        read.LastResumeId.Should().Be(7);
        using var ai = new LocalAiService();
        ai.ConfigureEndpoint(read.AiBaseUrl, read.OpenAiModel, null);
        ai.IsConfigured.Should().BeTrue();
        File.ReadAllText(path).Should().NotContain("ApiKey").And.NotContain("Bearer");
        File.WriteAllText(path, "invalid json");
        store.Load().AiBaseUrl.Should().Be(LocalAiService.OpenAiBaseUrl);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
    }
}
