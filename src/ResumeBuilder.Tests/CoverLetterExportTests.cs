using System.Text;
using AwesomeAssertions;
using QuestPDF.Infrastructure;
using ResumeBuilder.Core.Models;
using ResumeBuilder.Export;
using ResumeBuilder.Templates;

namespace ResumeBuilder.Tests;

/// <summary>
/// Covers the cover-letter template and its exporters: the letter has to render, carry every block
/// of a business letter, and inherit the resume's styling rather than resetting to the defaults.
/// </summary>
public class CoverLetterExportTests
{
    private readonly TemplateRegistry _registry = new();
    private readonly CoverLetterExportService _service;

    public CoverLetterExportTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        _service = new CoverLetterExportService(_registry);
    }

    private static CoverLetter SampleLetter()
    {
        var resume = TestResumes.FullyPopulated();
        resume.TemplateSettings.AccentColor = "#ea580c";
        resume.TemplateSettings.IsAccentColorCustomized = true;

        var letter = CoverLetter.FromResume(resume, "Northwind", "Staff Engineer");

        letter.RecipientName = "Alex Chen";
        letter.RecipientTitle = "Head of Engineering";
        letter.CompanyAddress = "100 Market Street\nSpringfield, IL";
        letter.LetterDate = new DateTime(2024, 6, 1);
        letter.Paragraphs = new List<string>
        {
            "I am writing to apply for the Staff Engineer role.",
            "At Northwind I cut processing from eighteen hours to five minutes."
        };

        return letter;
    }

    [Fact]
    public async Task PdfExport_ProducesAValidPdf()
    {
        var pdf = await _service.ExportAsync(SampleLetter(), "PDF");

        pdf.Should().NotBeNullOrEmpty();
        Encoding.ASCII.GetString(pdf, 0, 4).Should().Be("%PDF");
    }

    [Fact]
    public async Task PdfExport_RendersAnEmptyLetterWithoutThrowing()
    {
        var pdf = await _service.ExportAsync(new CoverLetter(), "PDF");

        pdf.Should().NotBeNullOrEmpty();
    }

    public static TheoryData<string> AllCoverLetterTemplateIds()
    {
        var data = new TheoryData<string>();
        foreach (var template in new TemplateRegistry().GetAllCoverLetterTemplates())
        {
            data.Add(template.Info.Id);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(AllCoverLetterTemplateIds))]
    public async Task EveryCoverLetterTemplate_RendersAFullLetter(string templateId)
    {
        var letter = SampleLetter();
        letter.SelectedTemplateId = templateId;

        var pdf = await _service.ExportAsync(letter, "PDF");

        pdf.Should().NotBeNullOrEmpty();
        Encoding.ASCII.GetString(pdf, 0, 4).Should().Be("%PDF");
    }

    [Theory]
    [MemberData(nameof(AllCoverLetterTemplateIds))]
    public async Task EveryCoverLetterTemplate_RendersAnEmptyLetterWithoutThrowing(string templateId)
    {
        var pdf = await _service.ExportAsync(new CoverLetter { SelectedTemplateId = templateId }, "PDF");

        pdf.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CoverLetterGallery_OffersMoreThanOneTemplate()
    {
        var templates = _registry.GetAllCoverLetterTemplates().ToList();

        templates.Should().HaveCountGreaterThan(1);
        templates.Select(t => t.Info.Id).Should()
            .OnlyHaveUniqueItems()
            .And.Contain(new[] { "letter-modern", "letter-classic", "letter-minimal" });
        templates.Should().OnlyContain(t => t.Info.DefaultAccentColor.StartsWith("#"));
        templates.Should().OnlyContain(t => !string.IsNullOrWhiteSpace(t.Info.Description));
    }

    [Fact]
    public async Task DocxExport_IncludesEveryBlockOfTheLetter()
    {
        var bytes = await _service.ExportAsync(SampleLetter(), "DOCX");

        var xml = DocxText(bytes);
        xml.Should().Contain("Jane Doe");
        xml.Should().Contain("Alex Chen");
        xml.Should().Contain("Head of Engineering");
        xml.Should().Contain("Northwind");
        xml.Should().Contain("Springfield, IL");
        xml.Should().Contain("Application for Staff Engineer");
        xml.Should().Contain("Dear Alex Chen,");
        xml.Should().Contain("I am writing to apply for the Staff Engineer role.");
        xml.Should().Contain("Sincerely,");
    }

    [Fact]
    public async Task TextExport_IncludesEveryBlockOfTheLetter()
    {
        var bytes = await _service.ExportAsync(SampleLetter(), "TEXT");
        var text = Encoding.UTF8.GetString(bytes);

        text.Should().Contain("Jane Doe");
        text.Should().Contain("Alex Chen");
        text.Should().Contain("Northwind");
        text.Should().Contain("1 June 2024");
        text.Should().Contain("Dear Alex Chen,");
        text.Should().Contain("cut processing from eighteen hours");
        text.Should().Contain("Sincerely,");
    }

    [Fact]
    public async Task TextExport_FallsBackToTheDefaultSalutation()
    {
        var letter = SampleLetter();
        letter.RecipientName = "";
        letter.Salutation = "";

        var text = Encoding.UTF8.GetString(await _service.ExportAsync(letter, "TEXT"));

        text.Should().Contain("Dear Hiring Manager,");
    }

    [Fact]
    public void Template_KeepsTheStylingItInheritedFromTheResume()
    {
        var resume = TestResumes.FullyPopulated();
        resume.TemplateSettings.AccentColor = "#ea580c";
        resume.TemplateSettings.FontFamily = "Georgia";

        var letter = CoverLetter.FromResume(resume, "Northwind", "Staff Engineer");

        // The letter carries the resume's resolved styling; the letter template must not re-default it.
        letter.TemplateSettings.AccentColor.Should().Be("#ea580c");
        letter.TemplateSettings.FontFamily.Should().Be("Georgia");

        var document = _registry
            .GetCoverLetterTemplateOrDefault(letter.SelectedTemplateId)
            .CreateDocument(letter);

        document.Should().NotBeNull();
        letter.TemplateSettings.AccentColor.Should().Be("#ea580c");
    }

    [Fact]
    public void Registry_ResolvesTheDefaultCoverLetterTemplateForAnUnknownId()
    {
        var template = _registry.GetCoverLetterTemplateOrDefault("does-not-exist");

        template.Info.Id.Should().Be(TemplateRegistry.DefaultCoverLetterTemplateId);
    }

    [Fact]
    public void ExportService_ListsPdfDocxAndText()
    {
        _service.GetAvailableFormats().Select(f => f.Name)
            .Should().BeEquivalentTo("PDF", "DOCX", "TEXT");
    }

    [Fact]
    public async Task ExportService_RejectsAnUnknownFormat()
    {
        var export = () => _service.ExportAsync(SampleLetter(), "XLSX");

        await export.Should().ThrowAsync<ArgumentException>();
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
