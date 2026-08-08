using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Templates.Templates;

/// <summary>
/// Builds the whole page around the portrait: a tinted header band carries a framed photo, the name,
/// the title and a contact strip. Where no photo is set the frame holds the initials on the accent
/// colour instead, so the band stays balanced rather than showing an empty well.
/// </summary>
public class PhotoHeaderTemplate : BaseTemplate
{
    public override TemplateInfo Info => new()
    {
        Id = "photo-header",
        Name = "Photo Header",
        Description = "Prominent header band with a framed photo, name, title and contact strip",
        Category = TemplateCategory.Modern,
        Layout = TemplateLayout.SingleColumn,
        Tags = new[] { "photo", "portrait", "header", "modern" },
        DefaultAccentColor = "#0f766e",
        DefaultFontFamily = "Calibri"
    };

    private const float PhotoSize = 92;

    private static readonly EntryStyle Entry = new();

    protected override void ConfigurePage(PageDescriptor page)
    {
        page.Size(PageSizes.A4);
        page.Margin(0); // The header band is full-bleed; the body supplies its own padding.
        page.DefaultTextStyle(x => x
            .FontSize(10 * FontSizeScale)
            .FontFamily(FontFamily)
            .FontColor(ParseColor(TextColor)));
    }

    protected override void ComposeContent(IContainer container, Resume resume)
    {
        container.Column(page =>
        {
            // The header band is chrome rather than a section, so it is drawn whenever the personal
            // info section is visible — and the body below still follows the configured order.
            if (ShouldRenderSection(SectionType.PersonalInfo, resume))
                page.Item().Element(c => ComposeHeaderBand(c, resume));

            page.Item().Padding(PageMargin).Column(body =>
            {
                body.Spacing(SectionSpacing);
                ComposeBody(body, resume);
            });
        });
    }

    private void ComposeHeaderBand(IContainer container, Resume resume)
    {
        var info = resume.PersonalInfo;
        var accent = ParseColor(AccentColor);

        container
            .Background(accent.WithAlpha(0.08f))
            .BorderBottom(3)
            .BorderColor(accent)
            .PaddingVertical(22)
            .PaddingHorizontal(PageMargin)
            .Row(row =>
            {
                row.ConstantItem(PhotoSize + 8).AlignMiddle().Element(c => c
                    .Width(PhotoSize + 8)
                    .Height(PhotoSize + 8)
                    .Background(Colors.White)
                    .Border(2)
                    .BorderColor(accent)
                    .Padding(4)
                    .Element(inner => ComposePhotoOrInitials(inner, resume, PhotoSize, accent, Colors.White)));

                row.ConstantItem(18);

                row.RelativeItem().AlignMiddle().Column(column =>
                {
                    column.Item().Text(info.FullName)
                        .FontSize(26 * FontSizeScale)
                        .Bold()
                        .FontFamily(HeadingFontFamily)
                        .FontColor(ParseColor(HeadingColor));

                    if (!string.IsNullOrWhiteSpace(info.JobTitle))
                    {
                        column.Item().PaddingTop(1).Text(info.JobTitle)
                            .FontSize(13 * FontSizeScale)
                            .FontColor(accent)
                            .LetterSpacing(0.05f);
                    }

                    var contacts = ContactStrip(info);
                    if (contacts.Count > 0)
                    {
                        column.Item().PaddingTop(8).Text(string.Join("   •   ", contacts))
                            .FontSize(9 * FontSizeScale)
                            .FontColor(Colors.Grey.Darken2);
                    }
                });
            });
    }

    private static List<string> ContactStrip(PersonalInfo info)
    {
        var contacts = new List<string>();

        void Add(string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                contacts.Add(value.Trim());
        }

        Add(info.Email);
        Add(info.Phone);
        Add(info.Location);
        Add(FormatLinkedInDisplay(info.LinkedIn));
        Add(string.IsNullOrWhiteSpace(info.GitHub) ? null : $"github.com/{FormatGitHubDisplay(info.GitHub)}");
        Add(FormatWebsiteDisplay(info.Website));

        return contacts;
    }

    private void ComposeBody(ColumnDescriptor body, Resume resume)
    {
        foreach (var sectionType in GetOrderedSections())
        {
            if (!ShouldRenderSection(sectionType, resume))
                continue;

            switch (sectionType)
            {
                case SectionType.PersonalInfo:
                    // Already rendered as the header band.
                    break;

                case SectionType.Summary:
                    body.Item().Element(c => ComposeSection(c, "About", col =>
                        col.Item().Text(resume.Summary).FontSize(10 * FontSizeScale).LineHeight(LineSpacing)));
                    break;

                case SectionType.Experience:
                    body.Item().Element(c => ComposeSection(c, "Experience", col =>
                    {
                        col.Spacing(10);
                        foreach (var exp in resume.Experiences.OrderBy(e => e.Order))
                            col.Item().Element(e => ComposeExperienceEntry(e, exp, Entry with { SubtitleColor = ParseColor(AccentColor) }));
                    }));
                    break;

                case SectionType.Education:
                    body.Item().Element(c => ComposeSection(c, "Education", col =>
                    {
                        col.Spacing(8);
                        foreach (var edu in resume.EducationList.OrderBy(e => e.Order))
                            col.Item().Element(e => ComposeEducationEntry(e, edu, Entry with { SubtitleColor = ParseColor(AccentColor) }));
                    }));
                    break;

                case SectionType.Skills:
                    body.Item().Element(c => ComposeSection(c, "Skills", col => ComposeSkills(col, resume.Skills)));
                    break;

                case SectionType.Languages:
                    body.Item().Element(c => ComposeSection(c, "Languages", col =>
                        col.Item().Text(string.Join("   •   ", resume.Languages.OrderBy(l => l.Order).Select(l => FormatLanguage(l))))
                            .FontSize(9 * FontSizeScale)));
                    break;

                case SectionType.Certifications:
                    body.Item().Element(c => ComposeSection(c, "Certifications", col =>
                    {
                        foreach (var cert in resume.Certifications.OrderBy(c2 => c2.Order))
                        {
                            col.Item().PaddingBottom(3).Row(row =>
                            {
                                row.RelativeItem().Text(text =>
                                {
                                    text.Span(cert.Name).SemiBold().FontSize(10 * FontSizeScale);
                                    if (!string.IsNullOrWhiteSpace(cert.IssuingOrganization))
                                        text.Span($" — {cert.IssuingOrganization}").FontSize(9 * FontSizeScale);
                                });

                                if (cert.IssueDate.HasValue)
                                {
                                    row.AutoItem().Text(ResumeDateFormat.MonthYear(cert.IssueDate))
                                        .FontSize(9 * FontSizeScale)
                                        .FontColor(Colors.Grey.Darken1);
                                }
                            });
                        }
                    }));
                    break;

                case SectionType.Projects:
                    body.Item().Element(c => ComposeSection(c, "Projects", col =>
                    {
                        col.Spacing(8);
                        foreach (var project in resume.Projects.OrderBy(p => p.Order))
                            col.Item().Element(e => ComposeProjectEntry(e, project, Entry));
                    }));
                    break;

                case SectionType.CustomSections:
                    foreach (var custom in GetVisibleCustomSections(resume))
                    {
                        body.Item().Element(c => ComposeSection(c, custom.Title, col =>
                            ComposeCustomSectionItems(col, custom)));
                    }
                    break;
            }
        }
    }

    private void ComposeSection(IContainer container, string title, Action<ColumnDescriptor> content)
    {
        var accent = ParseColor(AccentColor);

        container.EnsureSpace(90).Column(column =>
        {
            column.Item().Row(row =>
            {
                row.ConstantItem(18).AlignMiddle().PaddingRight(6)
                    .Height(3).Background(accent);

                row.RelativeItem().Text(title.ToUpperInvariant())
                    .FontSize(12 * FontSizeScale)
                    .Bold()
                    .FontFamily(HeadingFontFamily)
                    .LetterSpacing(0.08f)
                    .FontColor(ParseColor(HeadingColor));
            });

            column.Item().Height(7);
            column.Item().Column(content);
        });
    }

    private void ComposeSkills(ColumnDescriptor column, List<Skill> skills)
    {
        var categorised = skills
            .Where(s => !string.IsNullOrWhiteSpace(s.Category))
            .GroupBy(s => s.Category)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .ToList();

        foreach (var group in categorised)
        {
            column.Item().PaddingBottom(3).Row(row =>
            {
                row.ConstantItem(95).Text(group.Key)
                    .SemiBold()
                    .FontSize(9 * FontSizeScale)
                    .FontColor(ParseColor(AccentColor));
                row.RelativeItem().Text(string.Join(", ", group.OrderBy(s => s.Order).Select(s => s.Name)))
                    .FontSize(9 * FontSizeScale);
            });
        }

        var uncategorised = skills
            .Where(s => string.IsNullOrWhiteSpace(s.Category))
            .OrderBy(s => s.Order)
            .Select(s => s.Name)
            .ToList();

        if (uncategorised.Count > 0)
            column.Item().Text(string.Join(", ", uncategorised)).FontSize(9 * FontSizeScale);
    }
}
