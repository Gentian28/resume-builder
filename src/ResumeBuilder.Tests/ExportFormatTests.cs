using System.Text;
using AwesomeAssertions;
using QuestPDF.Infrastructure;
using ResumeBuilder.Core.Models;
using ResumeBuilder.Export;
using ResumeBuilder.Templates;

namespace ResumeBuilder.Tests;

/// <summary>
/// Locks in the export bugs that were shipping silently: sections dropped from HTML/DOCX, custom
/// sections dropped everywhere, unescaped styling in HTML, and multi-page PNG losing every page
/// after the first.
/// </summary>
public class ExportFormatTests
{
    private readonly ExportService _service;

    public ExportFormatTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        _service = new ExportService(new TemplateRegistry());
    }

    private async Task<string> ExportTextAsync(Resume resume, string format)
    {
        var bytes = await _service.ExportAsync(resume, format);
        return Encoding.UTF8.GetString(bytes!);
    }

    [Theory]
    [InlineData("HTML")]
    [InlineData("TEXT")]
    public async Task Export_IncludesCustomSections(string format)
    {
        var resume = TestResumes.FullyPopulated();

        var output = await ExportTextAsync(resume, format);

        // Section headings are cased per format (TEXT upper-cases them), so match the title
        // case-insensitively and the item content exactly.
        output.Should().ContainEquivalentOf("Speaking");
        output.Should().Contain("Scaling Ingestion Pipelines");
    }

    [Fact]
    public async Task DocxExport_IncludesCustomSections()
    {
        var resume = TestResumes.FullyPopulated();

        var bytes = await _service.ExportAsync(resume, "DOCX");

        bytes.Should().NotBeNullOrEmpty();
        var xml = DocxText(bytes!);
        xml.Should().ContainEquivalentOf("Speaking");
        xml.Should().Contain("Scaling Ingestion Pipelines");
    }

    [Theory]
    [InlineData("HTML")]
    [InlineData("TEXT")]
    public async Task Export_IncludesGitHub(string format)
    {
        var resume = TestResumes.FullyPopulated();

        var output = await ExportTextAsync(resume, format);

        output.Should().Contain("janedoe");
    }

    [Fact]
    public async Task DocxExport_IncludesGitHub()
    {
        var bytes = await _service.ExportAsync(TestResumes.FullyPopulated(), "DOCX");

        DocxText(bytes!).Should().Contain("janedoe");
    }

    [Fact]
    public async Task HtmlExport_DoesNotLetStylingBreakOutOfTheStyleBlock()
    {
        var resume = TestResumes.FullyPopulated();
        resume.TemplateSettings.AccentColor = "red; } body { display: none } .x {";
        resume.TemplateSettings.FontFamily = "Arial'; } body { display: none } .y { font-family: '";
        resume.SyncLegacyStyling();

        var html = await ExportTextAsync(resume, "HTML");

        html.Should().NotContain("display: none");
    }

    [Fact]
    public async Task HtmlExport_DoesNotEmitJavascriptLinks()
    {
        var resume = TestResumes.FullyPopulated();
        resume.PersonalInfo.Website = "javascript:alert(1)";

        var html = await ExportTextAsync(resume, "HTML");

        html.Should().NotContain("href=\"javascript:");
    }

    [Fact]
    public async Task HtmlExport_EscapesUserContent()
    {
        var resume = TestResumes.FullyPopulated();
        resume.Summary = "<script>alert('xss')</script>";

        var html = await ExportTextAsync(resume, "HTML");

        html.Should().NotContain("<script>alert");
    }

    [Fact]
    public async Task Export_RespectsSectionVisibility()
    {
        var resume = TestResumes.FullyPopulated();
        resume.SectionOrder.SetSectionVisibility(SectionType.Projects, false);

        var html = await ExportTextAsync(resume, "HTML");
        var text = await ExportTextAsync(resume, "TEXT");

        html.Should().NotContain("Open Source Tool");
        text.Should().NotContain("Open Source Tool");
    }

    [Fact]
    public async Task PngExport_MultiPageResume_KeepsEveryPage()
    {
        var single = TestResumes.FullyPopulated();
        var singlePage = await _service.ExportAsync(single, "PNG");

        // Enough content to spill onto more pages.
        var long_ = TestResumes.FullyPopulated();
        for (var i = 0; i < 30; i++)
        {
            long_.Experiences.Add(new Experience
            {
                Order = i + 2,
                JobTitle = $"Engineer {i}",
                Company = $"Company {i}",
                StartDate = new DateTime(2010, 1, 1),
                EndDate = new DateTime(2011, 1, 1),
                Description = new string('x', 400),
                Achievements = { new string('y', 200), new string('z', 200) }
            });
        }

        var multiPage = await _service.ExportAsync(long_, "PNG");

        multiPage.Should().NotBeNullOrEmpty();

        // A stitched multi-page image must be bigger than the single-page one; returning only
        // page 1 (the old behavior) would make these roughly the same size.
        multiPage!.Length.Should().BeGreaterThan(singlePage!.Length);
    }

    [Fact]
    public async Task TextExport_IsAtsFriendly()
    {
        var text = await ExportTextAsync(TestResumes.FullyPopulated(), "TEXT");

        // The name heading is upper-cased for ATS parsers.
        text.Should().ContainEquivalentOf("Jane Doe");
        text.Should().Contain("Northwind");

        // Glyphs that ATS parsers commonly mangle.
        text.Should().NotContain("●");
        text.Should().NotContain("■");
        text.Should().NotContain("►");
    }

    [Fact]
    public async Task NativeJson_RoundTripsThroughTheSharedJsonImporter()
    {
        var original = TestResumes.FullyPopulated();

        var bytes = await _service.ExportAsync(original, "JSON");
        using var stream = new MemoryStream(bytes!);
        var result = await _service.ImportAsync(stream, "JSON");

        result.Success.Should().BeTrue();
        result.Data!.PersonalInfo.FullName.Should().Be("Jane Doe");
        result.Data.CustomSections.Should().ContainSingle(s => s.Title == "Speaking");
        result.Data.Experiences.Should().HaveCount(original.Experiences.Count);
    }

    [Fact]
    public async Task JsonResume_RoundTripsThroughTheSameExtension()
    {
        var original = TestResumes.FullyPopulated();

        var bytes = await _service.ExportAsync(original, "JSONRESUME");
        using var stream = new MemoryStream(bytes!);

        // Both formats claim ".json"; the importer must dispatch on content, not on the extension.
        var result = await _service.ImportAsync(stream, "JSON");

        result.Success.Should().BeTrue();
        result.Data!.PersonalInfo.FirstName.Should().Be("Jane");
        result.Data.Experiences.Should().NotBeEmpty();
        result.Data.Experiences[0].Company.Should().Be("Northwind");
    }

    [Fact]
    public async Task JsonResumeImport_MissingEndDate_IsNotTreatedAsCurrentRole()
    {
        var json = """
            {
              "basics": { "name": "Jane Doe" },
              "work": [
                { "name": "Old Corp", "position": "Engineer", "startDate": "2015-01" }
              ]
            }
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var result = await _service.ImportAsync(stream, "JSON");

        result.Success.Should().BeTrue();
        var job = result.Data!.Experiences.Single();

        // An absent end date means "unknown", not "still working here".
        job.IsCurrentRole.Should().BeFalse();
        job.DateRange.Should().NotContain("Present");
    }

    [Fact]
    public async Task Export_UsesInvariantDates_RegardlessOfMachineCulture()
    {
        var previous = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("fr-FR");

            var text = await ExportTextAsync(TestResumes.FullyPopulated(), "TEXT");

            text.Should().Contain("Mar 2022");
            text.Should().NotContain("mars");
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }

    private static string DocxText(byte[] docx)
    {
        using var stream = new MemoryStream(docx);
        using var archive = new System.IO.Compression.ZipArchive(stream);
        var document = archive.GetEntry("word/document.xml");
        document.Should().NotBeNull();

        using var reader = new StreamReader(document!.Open());
        return reader.ReadToEnd();
    }
}
