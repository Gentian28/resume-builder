using System.IO.Compression;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using QuestPDF.Fluent;
using ResumeBuilder.Core.Editing;
using ResumeBuilder.Core.Models;
using ResumeBuilder.Export;
using ResumeBuilder.Export.Importers;
using ResumeBuilder.Templates;
using UglyToad.PdfPig;

namespace ResumeBuilder.Tests;

public class ProductivityTests
{
    public static IEnumerable<object[]> Templates => new TemplateRegistry().GetAllTemplates().Select(t => new object[] { t.Info.Id });
    [Theory, MemberData(nameof(Templates))]
    public void EveryTemplate_UsesLetterPaperAndAlbanianHeadings(string template)
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        var resume = new Resume
        {
            PersonalInfo = new() { FirstName = "Jane", LastName = "Doe" },
            Summary = "Synthetic summary",
            EducationList = [new() { Degree = "Computer Science", Institution = "Example University" }],
            TemplateSettings = new() { PaperSize = PaperSize.Letter, DocumentLanguage = DocumentLanguage.Albanian }
        };
        using var pdf = PdfDocument.Open(new TemplateRegistry().GetTemplateOrDefault(template).CreateDocument(resume).GeneratePdf());
        var page = pdf.GetPage(1);
        ((double)page.Width).Should().BeApproximately(612, 1);
        ((double)page.Height).Should().BeApproximately(792, 1);
        var text = string.Join(" ", pdf.GetPages().Select(p => p.Text));
        text.Should().ContainEquivalentOf("Jane").And.Contain("Computer Science");
        text.ToLowerInvariant().Should().Contain("arsim");
    }
    [Fact]
    public async Task DocumentLocale_IsIsolatedAcrossConcurrentExports()
    {
        var first = Task.Run(() => { using var scope = DocumentText.Use(DocumentLanguage.Albanian); return ResumeDateFormat.MonthYear(new DateTime(2024, 1, 1)); });
        var second = Task.Run(() => { using var scope = DocumentText.Use(DocumentLanguage.English); return ResumeDateFormat.MonthYear(new DateTime(2024, 1, 1)); });
        (await first).Should().NotBe(await second);
        ResumeDateFormat.MonthYear(new DateTime(2024, 1, 1)).Should().Be("Jan 2024");
    }

    [Fact]
    public async Task ConcurrentPdfExports_PreserveExtractableTextInEveryDocument()
    {
        var registry = new TemplateRegistry();
        var exports = await Task.WhenAll(Enumerable.Range(0, 24).Select(seed => Task.Run(async () =>
        {
            var marker = $"DOCUMENT-SENTINEL-{seed:D4}";
            var resume = new Resume { PersonalInfo = new() { FirstName = "Jane" }, Summary = marker };
            var bytes = await new PdfExporter(registry).ExportAsync(resume, seed % 2 == 0 ? "minimal" : "starter");
            return (marker, bytes);
        })));
        foreach (var (marker, bytes) in exports)
        {
            using var pdf = PdfDocument.Open(bytes);
            string.Join(" ", pdf.GetPages().Select(p => p.Text)).Should().Contain(marker);
        }
    }
    [Theory]
    [InlineData("letter-modern")]
    [InlineData("letter-classic")]
    [InlineData("letter-minimal")]
    public void LetterTemplates_HonorPaperAndLocalizedDate(string template)
    {
        var letter = new CoverLetter { PersonalInfo = new() { FirstName = "Jane" }, LetterDate = new DateTime(2024, 1, 15), Paragraphs = ["Synthetic letter"], TemplateSettings = new() { PaperSize = PaperSize.Letter, DocumentLanguage = DocumentLanguage.Albanian } };
        using var pdf = PdfDocument.Open(new TemplateRegistry().GetCoverLetterTemplateOrDefault(template).CreateDocument(letter).GeneratePdf());
        ((double)pdf.GetPage(1).Width).Should().BeApproximately(612, 1);
        string.Join(" ", pdf.GetPages().Select(p => p.Text)).Should().Contain("janar");
    }
    [Fact]
    public async Task HtmlAndTextLocalizeLabelsWithoutChangingMachineReadableFormats()
    {
        var resume = new Resume { Summary = "Authored content", EducationList = [new() { Degree = "Degree" }], TemplateSettings = new() { PaperSize = PaperSize.Letter, DocumentLanguage = DocumentLanguage.Albanian } };
        var html = Encoding.UTF8.GetString(await new HtmlExporter(new TemplateRegistry()).ExportAsync(resume, "modern"));
        html.Should().Contain("lang=\"sq\"").And.Contain("size: Letter").And.Contain("Arsimi").And.Contain("line-height: 1.4");
        Encoding.UTF8.GetString(await new TextExporter().ExportAsync(resume, "modern")).Should().Contain("ARSIMI").And.Contain("Authored content");
    }
    [Fact]
    public async Task DocxRoundtrip_PreservesBodyAndImportsFieldsLocally()
    {
        var registry = new TemplateRegistry();
        var resume = new Resume { PersonalInfo = new() { FirstName = "Jane", LastName = "Doe", Email = "jane@example.com" }, Summary = "Synthetic summary" };
        resume.Experiences = [
            new() { JobTitle = "Software Architect", Company = "Example Company", StartDate = new DateTime(2022, 10, 1), Order = 0 },
            new() { JobTitle = "Data Scientist", Company = "Previous Company", StartDate = new DateTime(2020, 10, 1), EndDate = new DateTime(2022, 9, 1), Order = 1 }
        ];
        var bytes = await new DocxExporter(registry).ExportAsync(resume, "modern");
        using var input = new MemoryStream(bytes);
        var result = await new DocxImporter().ImportAsync(input);
        result.Success.Should().BeTrue();
        result.Data!.PersonalInfo.Email.Should().Be("jane@example.com");
        result.Data.PersonalInfo.FullName.Should().Be("Jane Doe");
        result.Data.Experiences.Select(e => e.JobTitle).Should().Equal("Software Architect", "Data Scientist");
        result.Data.Experiences[0].StartDate.Should().Be(new DateTime(2022, 10, 1));
        result.Data.Experiences[0].EndDate.Should().BeNull();
        result.Data.Experiences[0].IsCurrentRole.Should().BeFalse();
        result.Data.Experiences[1].StartDate.Should().Be(new DateTime(2020, 10, 1));
        result.Data.Experiences[1].EndDate.Should().Be(new DateTime(2022, 9, 1));
        result.Data.Summary.Should().Contain("Synthetic summary");
        string.Join(" ", result.Data.CustomSections.Single().Items.Select(i => i.Description)).Should().Contain("Jane Doe").And.Contain("Synthetic summary");
        result.Warnings.Should().NotBeEmpty();
    }
    [Fact]
    public async Task DocxImporter_RejectsExpandedArchivesBeforeParsing()
    {
        using var bytes = new MemoryStream();
        using (var archive = new ZipArchive(bytes, ZipArchiveMode.Create, true))
        {
            using var entry = archive.CreateEntry("word/document.xml").Open();
            await entry.WriteAsync(new byte[16 * 1024 * 1024 + 1]);
        }
        bytes.Position = 0;
        var result = await new DocxImporter().ImportAsync(bytes);
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("limit");
    }
    [Fact]
    public void CsvEscapesMultilineContentAndNeutralizesFormulas()
    {
        var text = ApplicationTools.Csv([new() { Company = "  =HYPERLINK(\"bad\")", Role = "Engineer", Notes = "Line one\nLine two, with comma" }]);
        text.Should().Contain("'  =HYPERLINK").And.Contain("Line one\nLine two, with comma");
    }
    [Fact]
    public void Calendar_UsesLocalAllDayDatesStableIdsAndUtf8Folding()
    {
        var application = new JobApplication { Id = 4, Company = new string('\u00eb', 90), NextActionOn = new DateTime(2026, 10, 19), Contact = "Name;Surname", Notes = "one\ntwo" };
        var text = ApplicationTools.Calendar([application]);
        text.Should().Contain("DTSTART;VALUE=DATE:20261019").And.Contain("DTEND;VALUE=DATE:20261020").And.Contain("Name\\;Surname").And.Contain("one\\ntwo");
        text.Split("\r\n").Should().OnlyContain(line => Encoding.UTF8.GetByteCount(line) <= 75);
        ApplicationTools.Calendar([application]).Should().Be(text);
        application.Status = ApplicationStatus.Rejected;
        ApplicationTools.Calendar([application]).Should().NotContain("BEGIN:VEVENT");
    }
    [Fact]
    public async Task NullNestedInputAndUnsafeLayout_AreHandledBeforeAdoption()
    {
        using var input = new MemoryStream(Encoding.UTF8.GetBytes("{\"personalInfo\":{},\"experiences\":[null,{\"achievements\":null}],\"templateSettings\":{\"pageMargin\":900,\"fontSizeScale\":0}}"));
        var result = await new ExportService(new TemplateRegistry()).ImportAsync(input, "JSON");
        result.Success.Should().BeTrue();
        result.Data!.Experiences.Should().ContainSingle();
        result.Data.Experiences[0].Achievements.Should().NotBeNull();
        result.Data.TemplateSettings.PageMargin.Should().Be(100);
        result.Data.TemplateSettings.FontSizeScale.Should().Be(0.6f);
        result.Warnings.Should().NotBeEmpty();
    }
}
