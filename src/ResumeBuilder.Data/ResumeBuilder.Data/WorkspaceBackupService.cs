using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ResumeBuilder.Core.Models;
using ResumeBuilder.Core.Sync;

namespace ResumeBuilder.Data;

public sealed class WorkspaceArchive
{
    public string Format { get; set; } = "ResumeBuilderWorkspace";
    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Resume> Resumes { get; set; } = new();
    public List<CoverLetter> CoverLetters { get; set; } = new();
    public List<JobApplication> Applications { get; set; } = new();
}

/// <summary>A complete document workspace. Settings and credentials never enter this archive.</summary>
public sealed class WorkspaceBackupService(IResumeDbContextFactory factory)
{
    public const int MaxBytes = 64 * 1024 * 1024;
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public async Task<WorkspaceArchive> CaptureAsync()
    {
        await using var context = factory.CreateDbContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var archive = new WorkspaceArchive
        {
            Resumes = await context.Resumes.AsNoTracking().ToListAsync(),
            CoverLetters = await context.CoverLetters.AsNoTracking().ToListAsync(),
            Applications = await context.JobApplications.AsNoTracking().ToListAsync()
        };
        await transaction.CommitAsync();
        return archive;
    }

    public async Task ExportToFileAsync(string path)
    {
        var archive = await CaptureAsync();
        var json = JsonSerializer.Serialize(archive, Options);
        if (Encoding.UTF8.GetByteCount(json) > MaxBytes)
            throw new InvalidOperationException("This workspace exceeds the 64 MB backup limit.");
        await AtomicFile.WriteAllTextAsync(path, json);
    }

    public static async Task<byte[]> ReadBoundedAsync(Stream stream)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int count;
        while ((count = await stream.ReadAsync(chunk)) > 0)
        {
            if (buffer.Length + count > MaxBytes)
                throw new InvalidDataException("The file exceeds the 64 MB import limit.");
            await buffer.WriteAsync(chunk.AsMemory(0, count));
        }
        return buffer.ToArray();
    }

    public static bool IsWorkspace(ReadOnlyMemory<byte> bytes)
    {
        using var json = JsonDocument.Parse(bytes);
        return json.RootElement.ValueKind == JsonValueKind.Object && json.RootElement.EnumerateObject()
            .Any(p => p.Name.Equals("format", StringComparison.OrdinalIgnoreCase) &&
                p.Value.ValueKind == JsonValueKind.String && p.Value.GetString() == "ResumeBuilderWorkspace");
    }

    public static WorkspaceArchive Read(ReadOnlyMemory<byte> bytes)
    {
        if (bytes.Length > MaxBytes) throw new InvalidDataException("The backup exceeds 64 MB.");
        using var json = JsonDocument.Parse(bytes);
        var names = json.RootElement.ValueKind == JsonValueKind.Object
            ? json.RootElement.EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>();
        if (!IsWorkspace(bytes) || !new[] { "version", "resumes", "coverLetters", "applications" }.All(names.Contains))
            throw new InvalidDataException("This file is not a complete workspace backup.");
        var archive = JsonSerializer.Deserialize<WorkspaceArchive>(bytes.Span, Options)
            ?? throw new InvalidDataException("The backup is empty.");
        Validate(archive);
        return archive;
    }

    public async Task<string?> RestoreAsync(WorkspaceArchive archive)
    {
        // Freeze and validate before opening a database or replacing any records.
        archive = Read(JsonSerializer.SerializeToUtf8Bytes(archive, Options));
        await using var context = factory.CreateDbContext();
        var backup = DatabaseBackup.Create(context);
        if (backup.Error is { } error)
            throw new IOException($"The existing workspace could not be backed up. Restore was cancelled: {error}");

        await using var transaction = await context.Database.BeginTransactionAsync();
        await context.Database.ExecuteSqlRawAsync("DELETE FROM \"JobApplications\"");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM \"CoverLetters\"");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM \"Resumes\"");
        foreach (var resume in archive.Resumes)
        {
            resume.RowVersion = Guid.NewGuid();
            resume.UpdatedAt = DateTime.UtcNow;
        }
        foreach (var letter in archive.CoverLetters) letter.RowVersion = Guid.NewGuid();
        foreach (var application in archive.Applications) application.RowVersion = Guid.NewGuid();
        context.Resumes.AddRange(archive.Resumes);
        context.CoverLetters.AddRange(archive.CoverLetters);
        context.JobApplications.AddRange(archive.Applications);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return backup.Path;
    }

    private static void Validate(WorkspaceArchive archive)
    {
        if (archive.Format != "ResumeBuilderWorkspace" || archive.Version != 1)
            throw new InvalidDataException("This workspace backup format or version is not supported.");
        if (archive.Resumes is null || archive.CoverLetters is null || archive.Applications is null)
            throw new InvalidDataException("The backup is missing document collections.");
        CheckIds(archive.Resumes.Select(r => r?.Id ?? 0));
        CheckIds(archive.CoverLetters.Select(r => r?.Id ?? 0));
        CheckIds(archive.Applications.Select(r => r?.Id ?? 0));
        if (archive.Resumes.Any(r => r.SyncId == Guid.Empty) || archive.Resumes.Select(r => r.SyncId).Distinct().Count() != archive.Resumes.Count)
            throw new InvalidDataException("The backup contains invalid or duplicate sync identities.");
        foreach (var resume in archive.Resumes) ValidateResume(resume);
        foreach (var letter in archive.CoverLetters)
        {
            if (letter.PersonalInfo is null || letter.TemplateSettings is null || letter.Paragraphs is null || letter.Paragraphs.Any(p => p is null))
                throw new InvalidDataException("A cover letter contains incomplete data.");
            if (letter.SourceResumeJson is { } source) ValidateResume(JsonSerializer.Deserialize<Resume>(source)!);
        }
        foreach (var application in archive.Applications)
        {
            if (!Enum.IsDefined(application.Status)) throw new InvalidDataException("An application has an unknown status.");
            if (application.SentResumeJson is { } sent) ValidateResume(JsonSerializer.Deserialize<Resume>(sent)!);
        }
    }

    private static void CheckIds(IEnumerable<int> source)
    {
        var ids = source.ToList();
        if (ids.Count > 10000 || ids.Any(id => id <= 0) || ids.Distinct().Count() != ids.Count)
            throw new InvalidDataException("The backup contains invalid or duplicate document identities.");
    }

    private static void ValidateResume(Resume? resume)
    {
        if (resume is null || resume.PersonalInfo is null || resume.TemplateSettings is null || resume.SectionOrder is null ||
            resume.SectionOrder.OrderedSections is null || resume.SectionOrder.Visibility is null ||
            resume.Experiences is null || resume.EducationList is null || resume.Skills is null || resume.Languages is null ||
            resume.Certifications is null || resume.Projects is null || resume.CustomSections is null ||
            resume.EducationList.Any(e => e is null) || resume.Skills.Any(s => s is null) ||
            resume.Languages.Any(l => l is null) || resume.Certifications.Any(c => c is null) ||
            resume.Experiences.Any(e => e is null || e.Achievements is null) ||
            resume.Projects.Any(p => p is null || p.Technologies is null || p.Highlights is null) ||
            resume.CustomSections.Any(s => s is null || s.Items is null || s.Items.Any(i => i is null)))
            throw new InvalidDataException("A resume contains incomplete data.");
    }
}
