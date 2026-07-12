using FluentAssertions;
using Microsoft.Data.Sqlite;
using ResumeBuilder.Core.Models;
using ResumeBuilder.Data;

namespace ResumeBuilder.Tests;

/// <summary>
/// Covers the persistence behavior that used to silently lose data: owned-entity updates,
/// in-place collection edits, concurrent writes, and identity reuse on copy.
/// </summary>
public class RepositoryTests : IDisposable
{
    private readonly string _dbPath;
    private readonly ResumeDbContextFactory _factory;
    private readonly ResumeRepository _repository;

    public RepositoryTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"repo_{Guid.NewGuid()}.db");
        _factory = new ResumeDbContextFactory(_dbPath);

        using var context = _factory.CreateDbContext();
        DatabaseInitializer.Initialize(context);

        _repository = new ResumeRepository(_factory);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try
        {
            if (File.Exists(_dbPath)) File.Delete(_dbPath);
        }
        catch (IOException)
        {
            // Ignore file deletion errors in cleanup
        }
    }

    [Fact]
    public async Task Update_EditedPersonalInfo_Persists()
    {
        var resume = await _repository.CreateAsync(new Resume
        {
            Name = "R",
            PersonalInfo = new PersonalInfo { FirstName = "John", Email = "john@example.com" }
        });

        var loaded = await _repository.GetByIdAsync(resume.Id);
        loaded!.PersonalInfo.FirstName = "Jane";
        loaded.PersonalInfo.Email = "jane@example.com";
        await _repository.UpdateAsync(loaded);

        var reloaded = await _repository.GetByIdAsync(resume.Id);
        reloaded!.PersonalInfo.FirstName.Should().Be("Jane");
        reloaded.PersonalInfo.Email.Should().Be("jane@example.com");
    }

    [Fact]
    public async Task Update_ItemAddedToExistingCollection_Persists()
    {
        var resume = await _repository.CreateAsync(new Resume
        {
            Name = "R",
            Skills = new List<Skill> { new() { Name = "C#" } }
        });

        var loaded = await _repository.GetByIdAsync(resume.Id);

        // An in-place mutation - the case a missing ValueComparer would not have detected.
        loaded!.Skills.Add(new Skill { Name = "SQL" });
        await _repository.UpdateAsync(loaded);

        var reloaded = await _repository.GetByIdAsync(resume.Id);
        reloaded!.Skills.Should().HaveCount(2);
        reloaded.Skills.Select(s => s.Name).Should().Contain("SQL");
    }

    [Fact]
    public async Task Update_TemplateSettingsMutatedInPlace_Persists()
    {
        var resume = await _repository.CreateAsync(new Resume { Name = "R" });

        var loaded = await _repository.GetByIdAsync(resume.Id);
        loaded!.TemplateSettings.AccentColor = "#ff0000";
        loaded.TemplateSettings.IsAccentColorCustomized = true;
        await _repository.UpdateAsync(loaded);

        var reloaded = await _repository.GetByIdAsync(resume.Id);
        reloaded!.TemplateSettings.AccentColor.Should().Be("#ff0000");
        reloaded.TemplateSettings.IsAccentColorCustomized.Should().BeTrue();

        // The legacy field is kept in step so exporters reading either one agree.
        reloaded.AccentColor.Should().Be("#ff0000");
    }

    [Fact]
    public async Task Update_StaleCopy_ThrowsInsteadOfClobbering()
    {
        var resume = await _repository.CreateAsync(new Resume { Name = "Original" });

        var first = await _repository.GetByIdAsync(resume.Id);
        var second = await _repository.GetByIdAsync(resume.Id);

        first!.Name = "First writer";
        await _repository.UpdateAsync(first);

        second!.Name = "Second writer";
        var stale = async () => await _repository.UpdateAsync(second);

        await stale.Should().ThrowAsync<ResumeConcurrencyException>();

        var reloaded = await _repository.GetByIdAsync(resume.Id);
        reloaded!.Name.Should().Be("First writer");
    }

    [Fact]
    public async Task Duplicate_DoesNotShareIdentityWithOriginal()
    {
        var original = await _repository.CreateAsync(new Resume
        {
            Name = "Original",
            Skills = new List<Skill> { new() { Name = "C#" } }
        });

        var copy = await _repository.DuplicateAsync(original.Id);

        copy.Id.Should().NotBe(original.Id);
        copy.Name.Should().Be("Original (Copy)");

        // A copy that kept the original's SyncId would be treated as the same resume by sync.
        copy.SyncId.Should().NotBe(original.SyncId);
        copy.Skills.Should().OnlyContain(s => s.Id == 0);
    }

    [Fact]
    public async Task CreateVariant_BranchesFromBaseAndStaysFlat()
    {
        var basis = await _repository.CreateAsync(new Resume { Name = "Base", Summary = "Hi" });

        var variant = await _repository.CreateVariantAsync(basis.Id, "Backend Engineer", "job posting text");

        variant.BaseResumeId.Should().Be(basis.Id);
        variant.TargetRole.Should().Be("Backend Engineer");
        variant.JobDescription.Should().Be("job posting text");
        variant.IsVariant.Should().BeTrue();
        variant.Summary.Should().Be("Hi");

        // A variant of a variant still points at the original base.
        var nested = await _repository.CreateVariantAsync(variant.Id, "Staff Engineer", "other posting");
        nested.BaseResumeId.Should().Be(basis.Id);

        var variants = await _repository.GetVariantsAsync(basis.Id);
        variants.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetById_OldResumeMissingNewSections_GetsThemBack()
    {
        var resume = new Resume { Name = "Legacy" };
        resume.SectionOrder.OrderedSections.Remove(SectionType.CustomSections);
        resume.SectionOrder.Visibility.Remove(SectionType.CustomSections);
        await _repository.CreateAsync(resume);

        var loaded = await _repository.GetByIdAsync(resume.Id);

        loaded!.SectionOrder.OrderedSections.Should().Contain(SectionType.CustomSections);
        loaded.SectionOrder.IsSectionVisible(SectionType.CustomSections).Should().BeTrue();
    }
}
