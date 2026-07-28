using AwesomeAssertions;
using QuestPDF.Infrastructure;
using ResumeBuilder.Core.Models;
using ResumeBuilder.Export;
using ResumeBuilder.Templates;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace ResumeBuilder.Tests;

/// <summary>
/// The render tests prove a template produces a PDF; these prove it produces the user's PDF. Layouts
/// buy space by batching or shrinking, never by dropping data, so every skill, achievement, language,
/// certification and custom item must survive into the extracted text.
/// </summary>
public class TemplateContentTests
{
    private readonly TemplateRegistry _registry = new();

    public TemplateContentTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static TheoryData<string> NewTemplateIds() => new()
    {
        "ats-plain",
        "federal",
        "europass",
        "photo-header",
        "developer",
        "chronology",
        "colorblock",
        "onepage"
    };

    [Theory]
    [MemberData(nameof(NewTemplateIds))]
    public async Task Template_PrintsEveryPieceOfContent(string templateId)
    {
        var resume = TestResumes.FullyPopulated();
        resume.SelectedTemplateId = templateId;

        var text = Normalize(await RenderText(resume));

        // Every skill, including the fifth category that a "top three" layout would have dropped.
        foreach (var skill in resume.Skills)
            text.Should().Contain(Normalize(skill.Name), $"'{skill.Name}' must survive into {templateId}");

        // Every achievement of every role, not just the first couple.
        foreach (var achievement in resume.Experiences.SelectMany(e => e.Achievements))
            text.Should().Contain(Normalize(achievement));

        foreach (var language in resume.Languages)
            text.Should().Contain(Normalize(language.Name));

        foreach (var certification in resume.Certifications)
            text.Should().Contain(Normalize(certification.Name));

        foreach (var project in resume.Projects)
        {
            text.Should().Contain(Normalize(project.Name));
            foreach (var highlight in project.Highlights)
                text.Should().Contain(Normalize(highlight));
        }

        // Custom sections are a section like any other and used to be quietly skipped.
        foreach (var section in resume.CustomSections)
        {
            text.Should().Contain(Normalize(section.Title));
            foreach (var item in section.Items)
                text.Should().Contain(Normalize(item.Title));
        }

        foreach (var education in resume.EducationList)
            text.Should().Contain(Normalize(education.Institution));
    }

    [Theory]
    [MemberData(nameof(NewTemplateIds))]
    public async Task Template_HonoursSectionOrderAndVisibility(string templateId)
    {
        var resume = TestResumes.FullyPopulated();
        resume.SelectedTemplateId = templateId;
        resume.SectionOrder.SetSectionVisibility(SectionType.Projects, false);
        resume.SectionOrder.SetSectionVisibility(SectionType.Certifications, false);

        var text = Normalize(await RenderText(resume));

        text.Should().NotContain(Normalize("Open Source Tool"));
        text.Should().NotContain(Normalize("AWS Solutions Architect"));

        // The sections that remain visible are untouched.
        text.Should().Contain(Normalize("Northwind"));
        text.Should().Contain(Normalize("State University"));
    }

    private async Task<string> RenderText(Resume resume)
    {
        var pdf = await new ExportService(_registry).ExportAsync(resume, "PDF");

        using var document = PdfDocument.Open(pdf!);

        return string.Join(
            "\n",
            document.GetPages().Select(page => ContentOrderTextExtractor.GetText(page)));
    }

    /// <summary>
    /// PDF text extraction reconstructs runs from glyph positions, so spacing is not reliable, and
    /// templates are free to case their own headings. The comparison is made on the letters alone.
    /// </summary>
    private static string Normalize(string value) =>
        new(value.Where(c => !char.IsWhiteSpace(c)).Select(char.ToLowerInvariant).ToArray());
}
