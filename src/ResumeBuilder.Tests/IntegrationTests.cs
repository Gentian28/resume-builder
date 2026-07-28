using AwesomeAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ResumeBuilder.Core.Models;
using ResumeBuilder.Data;
using ResumeBuilder.Templates;
using ResumeBuilder.Export;
using QuestPDF.Infrastructure;

namespace ResumeBuilder.Tests;

public class TemplateRegistryIntegrationTests
{
    public TemplateRegistryIntegrationTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Fact]
    public void GetAllTemplates_ReturnsMultipleTemplates()
    {
        // Arrange
        var registry = new TemplateRegistry();

        // Act
        var templates = registry.GetAllTemplates().ToList();

        // Assert
        templates.Should().NotBeEmpty();
        templates.Count.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void GetTemplate_ValidId_ReturnsTemplate()
    {
        // Arrange
        var registry = new TemplateRegistry();
        var allTemplates = registry.GetAllTemplates().ToList();
        var firstTemplate = allTemplates.First();

        // Act
        var template = registry.GetTemplate(firstTemplate.Info.Id);

        // Assert
        template.Should().NotBeNull();
        template!.Info.Id.Should().Be(firstTemplate.Info.Id);
    }

    [Fact]
    public void GetTemplate_InvalidId_ReturnsNull()
    {
        // Arrange
        var registry = new TemplateRegistry();

        // Act
        var template = registry.GetTemplate("nonexistent-template-id");

        // Assert
        template.Should().BeNull();
    }

    [Fact]
    public void AllTemplates_HaveRequiredProperties()
    {
        // Arrange
        var registry = new TemplateRegistry();

        // Act
        var templates = registry.GetAllTemplates().ToList();

        // Assert
        foreach (var template in templates)
        {
            template.Info.Id.Should().NotBeNullOrEmpty();
            template.Info.Name.Should().NotBeNullOrEmpty();
        }
    }
}

public class DatabaseIntegrationTests : IDisposable
{
    private readonly ResumeDbContext _context;
    private readonly ResumeRepository _repository;
    private readonly string _dbPath;

    public DatabaseIntegrationTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
        var options = new DbContextOptionsBuilder<ResumeDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;

        _context = new ResumeDbContext(options);
        _context.Database.EnsureCreated();
        _repository = new ResumeRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();

        // SQLite connection pooling can keep file locked, clear the pool
        SqliteConnection.ClearAllPools();

        // Give a small delay for file handles to be released
        Thread.Sleep(50);

        try
        {
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }
        }
        catch (IOException)
        {
            // Ignore file deletion errors in cleanup
        }
    }

    [Fact]
    public async Task CreateResume_NewResume_CreatesRecord()
    {
        // Arrange
        var resume = new Resume
        {
            Name = "Test Resume",
            PersonalInfo = new PersonalInfo
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com"
            }
        };

        // Act
        await _repository.CreateAsync(resume);

        // Assert
        resume.Id.Should().BeGreaterThan(0);
        var loaded = await _repository.GetByIdAsync(resume.Id);
        loaded.Should().NotBeNull();
        loaded!.Name.Should().Be("Test Resume");
    }

    [Fact]
    public async Task UpdateResume_ExistingResume_UpdatesRecord()
    {
        // Arrange
        var resume = new Resume
        {
            Name = "Original Name",
            PersonalInfo = new PersonalInfo { FirstName = "John", LastName = "Doe" }
        };
        await _repository.CreateAsync(resume);
        var originalId = resume.Id;

        // Act
        resume.Name = "Updated Name";
        await _repository.UpdateAsync(resume);

        // Assert
        resume.Id.Should().Be(originalId);
        var loaded = await _repository.GetByIdAsync(originalId);
        loaded!.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task GetAllResumes_ReturnsAllSavedResumes()
    {
        // Arrange
        await _repository.CreateAsync(new Resume { Name = "Resume 1" });
        await _repository.CreateAsync(new Resume { Name = "Resume 2" });
        await _repository.CreateAsync(new Resume { Name = "Resume 3" });

        // Act
        var resumes = await _repository.GetAllAsync();

        // Assert
        resumes.Should().HaveCount(3);
    }

    [Fact]
    public async Task DeleteResume_RemovesRecord()
    {
        // Arrange
        var resume = new Resume { Name = "To Delete" };
        await _repository.CreateAsync(resume);
        var id = resume.Id;

        // Act
        await _repository.DeleteAsync(id);

        // Assert
        var loaded = await _repository.GetByIdAsync(id);
        loaded.Should().BeNull();
    }

    [Fact]
    public async Task CreateResume_WithCollections_PersistsAll()
    {
        // Arrange
        var resume = new Resume
        {
            Name = "Full Resume",
            PersonalInfo = new PersonalInfo
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com"
            },
            Experiences = new List<Experience>
            {
                new() { JobTitle = "Developer", Company = "Tech Corp" },
                new() { JobTitle = "Senior Dev", Company = "Big Corp" }
            },
            Skills = new List<Skill>
            {
                new() { Name = "C#" },
                new() { Name = "Python" }
            }
        };

        // Act
        await _repository.CreateAsync(resume);
        var loaded = await _repository.GetByIdAsync(resume.Id);

        // Assert
        loaded.Should().NotBeNull();
        loaded!.Experiences.Should().HaveCount(2);
        loaded.Skills.Should().HaveCount(2);
    }
}

public class ExportIntegrationTests
{
    public ExportIntegrationTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Fact]
    public async Task ExportPdf_ValidResume_GeneratesPdf()
    {
        // Arrange
        var templateRegistry = new TemplateRegistry();
        var exportService = new ExportService(templateRegistry);
        var resume = CreateSampleResume();

        // Act
        var pdfBytes = await exportService.ExportAsync(resume, "PDF");

        // Assert
        pdfBytes.Should().NotBeNull();
        pdfBytes.Should().NotBeEmpty();
        // PDF files start with %PDF
        pdfBytes![0].Should().Be((byte)'%');
        pdfBytes[1].Should().Be((byte)'P');
        pdfBytes[2].Should().Be((byte)'D');
        pdfBytes[3].Should().Be((byte)'F');
    }

    [Fact]
    public async Task ExportJson_ThenImport_RoundTrip()
    {
        // Arrange
        var templateRegistry = new TemplateRegistry();
        var exportService = new ExportService(templateRegistry);
        var original = CreateSampleResume();

        // Act - Export (use JSONRESUME exporter format)
        var jsonBytes = await exportService.ExportAsync(original, "JSONRESUME");

        // Import (use "JSON Resume" importer format)
        using var stream = new MemoryStream(jsonBytes!);
        var importResult = await exportService.ImportAsync(stream, "JSON Resume");

        // Assert
        importResult.Success.Should().BeTrue();
        importResult.Data.Should().NotBeNull();
        importResult.Data!.PersonalInfo.FirstName.Should().Be("John");
        importResult.Data.PersonalInfo.LastName.Should().Be("Doe");
        importResult.Data.Skills.Should().HaveCount(original.Skills.Count);
    }

    [Fact]
    public async Task ExportHtml_ValidResume_GeneratesHtml()
    {
        // Arrange
        var templateRegistry = new TemplateRegistry();
        var exportService = new ExportService(templateRegistry);
        var resume = CreateSampleResume();

        // Act
        var htmlBytes = await exportService.ExportAsync(resume, "HTML");

        // Assert
        htmlBytes.Should().NotBeNull();
        var html = System.Text.Encoding.UTF8.GetString(htmlBytes!);
        html.Should().Contain("<!DOCTYPE html>");
        html.Should().Contain("John Doe");
    }

    private static Resume CreateSampleResume()
    {
        return new Resume
        {
            Name = "Sample Resume",
            SelectedTemplateId = "modern",
            PersonalInfo = new PersonalInfo
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                Phone = "555-1234",
                City = "New York",
                Country = "NY",
                JobTitle = "Software Engineer"
            },
            Summary = "Experienced software engineer with 5 years of experience in full-stack development.",
            Experiences = new List<Experience>
            {
                new()
                {
                    JobTitle = "Senior Developer",
                    Company = "Tech Corp",
                    Location = "New York, NY",
                    StartDate = new DateTime(2020, 1, 1),
                    EndDate = new DateTime(2023, 12, 31),
                    Description = "Led development of key features",
                    Achievements = new List<string> { "Improved performance by 50%", "Mentored 3 junior developers" }
                }
            },
            EducationList = new List<Education>
            {
                new()
                {
                    Degree = "Bachelor of Science",
                    FieldOfStudy = "Computer Science",
                    Institution = "State University",
                    StartDate = new DateTime(2012, 9, 1),
                    EndDate = new DateTime(2016, 5, 31)
                }
            },
            Skills = new List<Skill>
            {
                new() { Name = "C#", Level = SkillLevel.Expert },
                new() { Name = "Python", Level = SkillLevel.Advanced },
                new() { Name = "JavaScript", Level = SkillLevel.Advanced }
            },
            Languages = new List<Language>
            {
                new() { Name = "English", Proficiency = LanguageProficiency.Native },
                new() { Name = "Spanish", Proficiency = LanguageProficiency.Conversational }
            }
        };
    }
}
