using AwesomeAssertions;
using ResumeBuilder.Core.Models;
using ResumeBuilder.Export;
using ResumeBuilder.Export.Importers;
using ResumeBuilder.Export.Exporters;
using ResumeBuilder.Templates;
using System.Text;
using System.Text.Json;

namespace ResumeBuilder.Tests;

public class JsonResumeImporterTests
{
    private readonly JsonResumeImporter _importer = new();

    [Fact]
    public void Format_ReturnsJsonResume()
    {
        _importer.Format.Should().Be("JSON Resume");
    }

    [Fact]
    public void SupportedExtensions_IncludesJson()
    {
        _importer.SupportedExtensions.Should().Contain(".json");
    }

    [Fact]
    public async Task ImportAsync_ValidJson_ReturnsResume()
    {
        // Arrange
        var jsonResume = new
        {
            basics = new
            {
                name = "John Doe",
                email = "john@example.com",
                phone = "555-1234",
                summary = "Experienced developer"
            },
            work = new[]
            {
                new
                {
                    company = "Tech Corp",
                    position = "Developer",
                    startDate = "2020-01-01",
                    endDate = "2023-01-01",
                    summary = "Did stuff"
                }
            },
            education = new[]
            {
                new
                {
                    institution = "University",
                    area = "Computer Science",
                    studyType = "Bachelor",
                    startDate = "2016-01-01",
                    endDate = "2020-01-01"
                }
            },
            skills = new[]
            {
                new { name = "Python" },
                new { name = "JavaScript" }
            }
        };

        var json = JsonSerializer.Serialize(jsonResume);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        // Act
        var result = await _importer.ImportAsync(stream);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.PersonalInfo.FirstName.Should().Be("John");
        result.Data.PersonalInfo.LastName.Should().Be("Doe");
        result.Data.PersonalInfo.Email.Should().Be("john@example.com");
        result.Data.Summary.Should().Be("Experienced developer");
        result.Data.Experiences.Should().HaveCount(1);
        result.Data.EducationList.Should().HaveCount(1);
        result.Data.Skills.Should().HaveCount(2);
    }

    [Fact]
    public async Task ImportAsync_InvalidJson_ReturnsError()
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("not valid json"));

        // Act
        var result = await _importer.ImportAsync(stream);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ImportAsync_EmptyStream_ReturnsError()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act
        var result = await _importer.ImportAsync(stream);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ImportAsync_MinimalJson_ReturnsResume()
    {
        // Arrange
        var json = "{ \"basics\": { \"name\": \"Jane Smith\" } }";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        // Act
        var result = await _importer.ImportAsync(stream);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.PersonalInfo.FirstName.Should().Be("Jane");
        result.Data.PersonalInfo.LastName.Should().Be("Smith");
    }
}

public class JsonResumeExporterTests
{
    private readonly JsonResumeExporter _exporter = new();

    [Fact]
    public void Format_ReturnsJsonResume()
    {
        _exporter.Format.Should().Be("JSONRESUME");
    }

    [Fact]
    public void FileExtension_ReturnsJson()
    {
        _exporter.FileExtension.Should().Be(".json");
    }

    [Fact]
    public async Task ExportAsync_ValidResume_ReturnsJsonBytes()
    {
        // Arrange
        var resume = new Resume
        {
            PersonalInfo = new PersonalInfo
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com"
            },
            Summary = "Test summary",
            Skills = new List<Skill>
            {
                new() { Name = "Python" },
                new() { Name = "C#" }
            }
        };

        // Act
        var result = await _exporter.ExportAsync(resume, "modern");

        // Assert
        result.Should().NotBeEmpty();
        var json = Encoding.UTF8.GetString(result);
        json.Should().Contain("John Doe");
        json.Should().Contain("john@example.com");
    }

    [Fact]
    public async Task ExportAsync_ThenImport_PreservesData()
    {
        // Arrange
        var original = new Resume
        {
            PersonalInfo = new PersonalInfo
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Phone = "555-1234"
            },
            Summary = "Experienced developer",
            Experiences = new List<Experience>
            {
                new()
                {
                    JobTitle = "Developer",
                    Company = "Tech Corp",
                    StartDate = new DateTime(2020, 1, 1),
                    EndDate = new DateTime(2023, 1, 1)
                }
            },
            Skills = new List<Skill>
            {
                new() { Name = "Python" },
                new() { Name = "JavaScript" }
            }
        };

        // Act - Export then import
        var bytes = await _exporter.ExportAsync(original, "modern");
        using var stream = new MemoryStream(bytes);
        var importer = new JsonResumeImporter();
        var result = await importer.ImportAsync(stream);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.PersonalInfo.FirstName.Should().Be("John");
        result.Data.PersonalInfo.LastName.Should().Be("Doe");
        result.Data.PersonalInfo.Email.Should().Be("john@example.com");
        result.Data.Summary.Should().Be("Experienced developer");
        result.Data.Experiences.Should().HaveCount(1);
        result.Data.Skills.Should().HaveCount(2);
    }
}

public class ExportServiceTests
{
    [Fact]
    public void Constructor_RegistersDefaultExporters()
    {
        // Arrange & Act
        var templateRegistry = new TemplateRegistry();
        var service = new ExportService(templateRegistry);

        // Assert
        var formats = service.GetAvailableFormats().Select(f => f.Name).ToList();
        formats.Should().Contain("PDF");
        formats.Should().Contain("JSON");
        formats.Should().Contain("JSONRESUME");
    }

    [Fact]
    public void GetAvailableFormats_ReturnsNonEmpty()
    {
        // Arrange
        var templateRegistry = new TemplateRegistry();
        var service = new ExportService(templateRegistry);

        // Act
        var formats = service.GetAvailableFormats();

        // Assert
        formats.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExportAsync_UnknownFormat_ThrowsException()
    {
        // Arrange
        var templateRegistry = new TemplateRegistry();
        var service = new ExportService(templateRegistry);
        var resume = new Resume { PersonalInfo = new PersonalInfo { FirstName = "Test" } };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.ExportAsync(resume, "UNKNOWN_FORMAT"));
    }

    [Fact]
    public async Task ExportAsync_JsonFormat_ReturnsBytes()
    {
        // Arrange
        var templateRegistry = new TemplateRegistry();
        var service = new ExportService(templateRegistry);
        var resume = new Resume
        {
            PersonalInfo = new PersonalInfo
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com"
            }
        };

        // Act
        var result = await service.ExportAsync(resume, "JSON");

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }

    [Fact]
    public void GetAllImporters_ReturnsNonEmpty()
    {
        // Arrange
        var templateRegistry = new TemplateRegistry();
        var service = new ExportService(templateRegistry);

        // Act
        var importers = service.GetAllImporters();

        // Assert
        importers.Should().NotBeEmpty();
        importers.Select(i => i.Format).Should().Contain("JSON Resume");
    }

    [Fact]
    public void GetImporter_ValidFormat_ReturnsImporter()
    {
        // Arrange
        var templateRegistry = new TemplateRegistry();
        var service = new ExportService(templateRegistry);

        // Act
        var importer = service.GetImporter("JSON Resume");

        // Assert
        importer.Should().NotBeNull();
        importer!.Format.Should().Be("JSON Resume");
    }

    [Fact]
    public void GetImporter_InvalidFormat_ReturnsNull()
    {
        // Arrange
        var templateRegistry = new TemplateRegistry();
        var service = new ExportService(templateRegistry);

        // Act
        var importer = service.GetImporter("UNKNOWN");

        // Assert
        importer.Should().BeNull();
    }
}

public class PdfImporterTests
{
    private readonly PdfImporter _importer = new();

    [Fact]
    public void Format_ReturnsPdf()
    {
        _importer.Format.Should().Be("PDF");
    }

    [Fact]
    public void SupportedExtensions_IncludesPdf()
    {
        _importer.SupportedExtensions.Should().Contain(".pdf");
    }

    [Fact]
    public async Task ImportAsync_EmptyStream_ReturnsError()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act
        var result = await _importer.ImportAsync(stream);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ImportAsync_InvalidPdf_ReturnsError()
    {
        // Arrange - Not a valid PDF
        var invalidData = Encoding.UTF8.GetBytes("This is not a PDF file");
        using var stream = new MemoryStream(invalidData);

        // Act
        var result = await _importer.ImportAsync(stream);

        // Assert
        result.Success.Should().BeFalse();
    }
}
