using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Templates.Templates;

/// <summary>
/// Built for a long career that still has to fit on one sheet. The density comes entirely from the
/// layout — a tighter type scale, tighter leading, narrow margins and two columns that let the short
/// sections stack beside the long ones. Nothing is dropped to buy the space: every skill, achievement
/// and certification is still printed, and a resume that genuinely will not fit runs to a second page
/// rather than silently losing content.
/// </summary>
public class OnePageTemplate : BaseTemplate
{
    public override TemplateInfo Info => new()
    {
        Id = "onepage",
        Name = "One Page",
        Description = "Space-efficient two-column layout with a tight type scale for dense histories",
        Category = TemplateCategory.Professional,
        Layout = TemplateLayout.TwoColumn,
        Tags = new[] { "dense", "compact", "one page", "efficient", "two column" },
        DefaultAccentColor = "#1d4ed8",
        DefaultFontFamily = "Calibri"
    };

    /// <summary>Type-scale multiplier applied on top of the user's own scale.</summary>
    private const float Density = 0.9f;

    private const float ColumnGutter = 16;

    /// <summary>The narrow right rail: the sections that stay short and stack well.</summary>
    private static readonly SectionType[] RailSections =
    {
        SectionType.Skills,
        SectionType.Languages,
        SectionType.Certifications,
        SectionType.Education
    };

    /// <summary>Leading is capped: the whole point of the template is a tight line box.</summary>
    private float TightLineSpacing => Math.Min(LineSpacing, 1.15f);

    private float Size(float points) => points * Density * FontSizeScale;

    private EntryStyle Entry => new()
    {
        TitleFontSize = 9.5f * Density,
        SubtitleFontSize = 8.5f * Density,
        BodyFontSize = 8 * Density,
        MetaFontSize = 7.5f * Density,
        LocationSeparator = " | ",
        Bullet = "• "
    };

    protected override void ConfigurePage(PageDescriptor page)
    {
        page.Size(PageSizes.A4);
        page.Margin(Math.Min(PageMargin, 24));
        page.DefaultTextStyle(x => x
            .FontSize(Size(10))
            .FontFamily(FontFamily)
            .FontColor(ParseColor(TextColor))
            .LineHeight(TightLineSpacing));
    }

    protected override void ComposeContent(IContainer container, Resume resume)
    {
        container.Column(page =>
        {
            if (ShouldRenderSection(SectionType.PersonalInfo, resume))
            {
                page.Item().Element(c => ComposeHeader(c, resume.PersonalInfo));
                page.Item().Height(8);
            }

            page.Item().Row(row =>
            {
                row.RelativeItem(62).Column(mainColumn =>
                {
                    mainColumn.Spacing(9);
                    ComposeColumn(mainColumn, resume, rail: false);
                });

                row.ConstantItem(ColumnGutter);

                row.RelativeItem(38).Column(railColumn =>
                {
                    railColumn.Spacing(9);
                    ComposeColumn(railColumn, resume, rail: true);
                });
            });
        });
    }

    private void ComposeHeader(IContainer container, PersonalInfo info)
    {
        var accent = ParseColor(AccentColor);

        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.Span(info.FullName)
                        .FontSize(Size(19))
                        .Bold()
                        .FontFamily(HeadingFontFamily)
                        .FontColor(accent);

                    if (!string.IsNullOrWhiteSpace(info.JobTitle))
                    {
                        text.Span($"   {info.JobTitle}")
                            .FontSize(Size(10))
                            .FontColor(Colors.Grey.Darken2);
                    }
                });
            });

            var contacts = new List<string>();
            if (!string.IsNullOrWhiteSpace(info.Email)) contacts.Add(info.Email);
            if (!string.IsNullOrWhiteSpace(info.Phone)) contacts.Add(info.Phone);
            if (!string.IsNullOrWhiteSpace(info.Location)) contacts.Add(info.Location);
            if (!string.IsNullOrWhiteSpace(info.LinkedIn)) contacts.Add(FormatLinkedInDisplay(info.LinkedIn));
            if (!string.IsNullOrWhiteSpace(info.GitHub)) contacts.Add($"github.com/{FormatGitHubDisplay(info.GitHub)}");
            if (!string.IsNullOrWhiteSpace(info.Website)) contacts.Add(info.Website);

            if (contacts.Count > 0)
            {
                column.Item().PaddingTop(1).Text(string.Join("  •  ", contacts))
                    .FontSize(Size(8))
                    .FontColor(Colors.Grey.Darken1);
            }

            column.Item().PaddingTop(4).LineHorizontal(1).LineColor(accent);
        });
    }

    private void ComposeColumn(ColumnDescriptor column, Resume resume, bool rail)
    {
        foreach (var sectionType in GetOrderedSections())
        {
            if (sectionType == SectionType.PersonalInfo)
                continue; // Rendered as the full-width header.

            if (RailSections.Contains(sectionType) != rail || !ShouldRenderSection(sectionType, resume))
                continue;

            switch (sectionType)
            {
                case SectionType.Summary:
                    column.Item().Element(c => ComposeSection(c, "Summary", col =>
                        col.Item().Text(resume.Summary).FontSize(Size(8.5f)).LineHeight(TightLineSpacing)));
                    break;

                case SectionType.Experience:
                    column.Item().Element(c => ComposeSection(c, "Experience", col =>
                    {
                        col.Spacing(7);
                        foreach (var exp in resume.Experiences.OrderBy(e => e.Order))
                            col.Item().Element(e => ComposeExperienceEntry(e, exp, Entry with { SubtitleColor = ParseColor(AccentColor) }));
                    }));
                    break;

                case SectionType.Projects:
                    column.Item().Element(c => ComposeSection(c, "Projects", col =>
                    {
                        col.Spacing(6);
                        foreach (var project in resume.Projects.OrderBy(p => p.Order))
                            col.Item().Element(e => ComposeProjectEntry(e, project, Entry));
                    }));
                    break;

                case SectionType.Education:
                    column.Item().Element(c => ComposeSection(c, "Education", col =>
                    {
                        col.Spacing(5);
                        foreach (var edu in resume.EducationList.OrderBy(e => e.Order))
                            col.Item().Element(e => ComposeEducationEntry(e, edu, Entry with { SubtitleColor = ParseColor(AccentColor) }));
                    }));
                    break;

                case SectionType.Skills:
                    column.Item().Element(c => ComposeSection(c, "Skills", col => ComposeSkills(col, resume.Skills)));
                    break;

                case SectionType.Languages:
                    column.Item().Element(c => ComposeSection(c, "Languages", col =>
                    {
                        foreach (var lang in resume.Languages.OrderBy(l => l.Order))
                        {
                            col.Item().Text(text =>
                            {
                                text.Span(lang.Name).SemiBold().FontSize(Size(8));
                                text.Span($" — {GetLanguageProficiencyText(lang.Proficiency)}")
                                    .FontSize(Size(8)).FontColor(Colors.Grey.Darken1);
                            });
                        }
                    }));
                    break;

                case SectionType.Certifications:
                    column.Item().Element(c => ComposeSection(c, "Certifications", col =>
                    {
                        foreach (var cert in resume.Certifications.OrderBy(c2 => c2.Order))
                        {
                            col.Item().PaddingBottom(3).Text(text =>
                            {
                                text.Span(cert.Name).SemiBold().FontSize(Size(8));
                                if (!string.IsNullOrWhiteSpace(cert.IssuingOrganization))
                                    text.Span($" — {cert.IssuingOrganization}").FontSize(Size(7.5f));
                                if (cert.IssueDate.HasValue)
                                    text.Span($" ({ResumeDateFormat.Year(cert.IssueDate)})")
                                        .FontSize(Size(7.5f)).FontColor(Colors.Grey.Darken1);
                            });
                        }
                    }));
                    break;

                case SectionType.CustomSections:
                    foreach (var custom in GetVisibleCustomSections(resume))
                    {
                        column.Item().Element(c => ComposeSection(c, custom.Title, col =>
                            ComposeCustomSectionItems(col, custom, 9 * Density, 8 * Density)));
                    }
                    break;
            }
        }
    }

    private void ComposeSection(IContainer container, string title, Action<ColumnDescriptor> content)
    {
        var accent = ParseColor(AccentColor);

        container.Column(column =>
        {
            column.Item().Text(title.ToUpperInvariant())
                .FontSize(Size(9))
                .Bold()
                .FontFamily(HeadingFontFamily)
                .LetterSpacing(0.1f)
                .FontColor(accent);

            column.Item().PaddingTop(2).PaddingBottom(5)
                .LineHorizontal(0.5f)
                .LineColor(accent.WithAlpha(0.35f));

            column.Item().Column(content);
        });
    }

    private void ComposeSkills(ColumnDescriptor column, List<Skill> skills)
    {
        // Comma-run per category: the densest way to print every skill without a bar or a chip each.
        var categorised = skills
            .Where(s => !string.IsNullOrWhiteSpace(s.Category))
            .GroupBy(s => s.Category)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .ToList();

        foreach (var group in categorised)
        {
            column.Item().PaddingBottom(3).Column(groupColumn =>
            {
                groupColumn.Item().Text(group.Key)
                    .SemiBold()
                    .FontSize(Size(8))
                    .FontColor(ParseColor(AccentColor));

                groupColumn.Item().Text(string.Join(", ", group.OrderBy(s => s.Order).Select(s => s.Name)))
                    .FontSize(Size(8))
                    .LineHeight(TightLineSpacing);
            });
        }

        var uncategorised = skills
            .Where(s => string.IsNullOrWhiteSpace(s.Category))
            .OrderBy(s => s.Order)
            .Select(s => s.Name)
            .ToList();

        if (uncategorised.Count > 0)
        {
            column.Item().Text(string.Join(", ", uncategorised))
                .FontSize(Size(8))
                .LineHeight(TightLineSpacing);
        }
    }
}
