using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Templates.Templates;

/// <summary>
/// A date gutter rather than a timeline: dates sit in a fixed left column and content flows in the
/// wide right column, so every "when" lines up down the page and a career reads at a glance. Unlike
/// the Timeline template there are no dots or connectors — the alignment alone carries the structure,
/// which keeps the page quiet and prints cleanly in black and white.
/// </summary>
public class ChronologyTemplate : BaseTemplate
{
    public override TemplateInfo Info => new()
    {
        Id = "chronology",
        Name = "Chronology",
        Description = "Left date gutter with a wide content column for a clean, scannable career history",
        Category = TemplateCategory.Classic,
        Layout = TemplateLayout.SingleColumn,
        Tags = new[] { "chronological", "dates", "gutter", "scannable" },
        DefaultAccentColor = "#334155",
        DefaultFontFamily = "Georgia"
    };

    private const float GutterWidth = 92;

    private static readonly EntryStyle Entry = new()
    {
        TitleFontSize = 11,
        SubtitleFontSize = 10,
        BodyFontSize = 9,
        MetaFontSize = 8
    };

    protected override void ComposeContent(IContainer container, Resume resume)
    {
        container.Column(column =>
        {
            column.Spacing(SectionSpacing);

            foreach (var sectionType in GetOrderedSections())
            {
                if (!ShouldRenderSection(sectionType, resume))
                    continue;

                switch (sectionType)
                {
                    case SectionType.PersonalInfo:
                        column.Item().Element(c => ComposeHeader(c, resume.PersonalInfo));
                        break;

                    case SectionType.Summary:
                        column.Item().Element(c => ComposeSection(c, "Summary", col =>
                            col.Item().Element(e => ComposeRow(e, string.Empty, body =>
                                body.Item().Text(resume.Summary).FontSize(9 * FontSizeScale).LineHeight(LineSpacing)))));
                        break;

                    case SectionType.Experience:
                        column.Item().Element(c => ComposeSection(c, "Experience", col =>
                        {
                            foreach (var exp in resume.Experiences.OrderBy(e => e.Order))
                            {
                                col.Item().PaddingBottom(10).Element(e => ComposeRow(e, exp.DateRange, body =>
                                {
                                    body.Item().Text(exp.JobTitle).Bold().FontSize(11 * FontSizeScale);

                                    body.Item().Text(text =>
                                    {
                                        text.Span(exp.Company).SemiBold().FontSize(10 * FontSizeScale)
                                            .FontColor(ParseColor(AccentColor));
                                        if (!string.IsNullOrWhiteSpace(exp.Location))
                                            text.Span($"  |  {exp.Location}").FontSize(9 * FontSizeScale)
                                                .FontColor(Colors.Grey.Darken1);
                                    });

                                    if (!string.IsNullOrWhiteSpace(exp.Description))
                                        body.Item().PaddingTop(3).Text(exp.Description)
                                            .FontSize(9 * FontSizeScale).LineHeight(LineSpacing);

                                    if (exp.Achievements.Any())
                                        body.Item().PaddingTop(3).Element(b => ComposeBulletList(b, exp.Achievements, Entry));
                                }));
                            }
                        }));
                        break;

                    case SectionType.Education:
                        column.Item().Element(c => ComposeSection(c, "Education", col =>
                        {
                            foreach (var edu in resume.EducationList.OrderBy(e => e.Order))
                            {
                                col.Item().PaddingBottom(8).Element(e => ComposeRow(e, edu.DateRange, body =>
                                {
                                    body.Item().Text(edu.DegreeWithField).Bold().FontSize(11 * FontSizeScale);

                                    body.Item().Text(text =>
                                    {
                                        text.Span(edu.Institution).SemiBold().FontSize(10 * FontSizeScale)
                                            .FontColor(ParseColor(AccentColor));
                                        if (!string.IsNullOrWhiteSpace(edu.Location))
                                            text.Span($"  |  {edu.Location}").FontSize(9 * FontSizeScale)
                                                .FontColor(Colors.Grey.Darken1);
                                    });

                                    if (!string.IsNullOrWhiteSpace(edu.Grade))
                                        body.Item().Text($"Grade: {edu.Grade}").FontSize(9 * FontSizeScale)
                                            .FontColor(Colors.Grey.Darken1);

                                    if (!string.IsNullOrWhiteSpace(edu.Description))
                                        body.Item().PaddingTop(2).Text(edu.Description)
                                            .FontSize(9 * FontSizeScale).LineHeight(LineSpacing);
                                }));
                            }
                        }));
                        break;

                    case SectionType.Skills:
                        column.Item().Element(c => ComposeSection(c, "Skills", col =>
                            col.Item().Element(e => ComposeRow(e, string.Empty, body => ComposeSkills(body, resume.Skills)))));
                        break;

                    case SectionType.Languages:
                        column.Item().Element(c => ComposeSection(c, "Languages", col =>
                            col.Item().Element(e => ComposeRow(e, string.Empty, body =>
                                body.Item().Text(string.Join(ContactSeparator, resume.Languages.OrderBy(l => l.Order).Select(l => FormatLanguage(l))))
                                    .FontSize(9 * FontSizeScale)))));
                        break;

                    case SectionType.Certifications:
                        column.Item().Element(c => ComposeSection(c, "Certifications", col =>
                        {
                            foreach (var cert in resume.Certifications.OrderBy(c2 => c2.Order))
                            {
                                col.Item().PaddingBottom(4).Element(e => ComposeRow(e, ResumeDateFormat.MonthYear(cert.IssueDate), body =>
                                {
                                    body.Item().Text(text =>
                                    {
                                        text.Span(cert.Name).SemiBold().FontSize(10 * FontSizeScale);
                                        if (!string.IsNullOrWhiteSpace(cert.IssuingOrganization))
                                            text.Span($"  |  {cert.IssuingOrganization}").FontSize(9 * FontSizeScale)
                                                .FontColor(Colors.Grey.Darken1);
                                    });

                                    if (!string.IsNullOrWhiteSpace(cert.CredentialId))
                                        body.Item().Text($"Credential ID: {cert.CredentialId}")
                                            .FontSize(8 * FontSizeScale).FontColor(Colors.Grey.Darken1);
                                }));
                            }
                        }));
                        break;

                    case SectionType.Projects:
                        column.Item().Element(c => ComposeSection(c, "Projects", col =>
                        {
                            foreach (var project in resume.Projects.OrderBy(p => p.Order))
                            {
                                var dates = project.StartDate.HasValue
                                    ? FormatDateRange(project.StartDate, project.EndDate, project.IsOngoing)
                                    : string.Empty;

                                col.Item().PaddingBottom(8).Element(e => ComposeRow(e, dates, body =>
                                {
                                    body.Item().Text(project.Name).Bold().FontSize(11 * FontSizeScale);

                                    if (!string.IsNullOrWhiteSpace(project.Description))
                                        body.Item().PaddingTop(2).Text(project.Description)
                                            .FontSize(9 * FontSizeScale).LineHeight(LineSpacing);

                                    if (!string.IsNullOrWhiteSpace(project.Url))
                                        body.Item().Text(project.Url).FontSize(9 * FontSizeScale)
                                            .FontColor(Colors.Grey.Darken1);

                                    if (project.Technologies.Any())
                                    {
                                        body.Item().PaddingTop(2).Text(text =>
                                        {
                                            text.Span("Technologies: ").Bold().FontSize(9 * FontSizeScale);
                                            text.Span(string.Join(", ", project.Technologies)).FontSize(9 * FontSizeScale);
                                        });
                                    }

                                    if (project.Highlights.Any())
                                        body.Item().PaddingTop(2).Element(b => ComposeBulletList(b, project.Highlights, Entry));
                                }));
                            }
                        }));
                        break;

                    case SectionType.CustomSections:
                        foreach (var custom in GetVisibleCustomSections(resume))
                        {
                            column.Item().Element(c => ComposeSection(c, custom.Title, col =>
                            {
                                foreach (var item in custom.Items.OrderBy(i => i.Order))
                                {
                                    col.Item().PaddingBottom(6).Element(e => ComposeRow(e, FormatCustomItemDateRange(item), body =>
                                    {
                                        body.Item().Text(item.Title).Bold().FontSize(10 * FontSizeScale);

                                        if (!string.IsNullOrWhiteSpace(item.Subtitle))
                                            body.Item().Text(item.Subtitle).FontSize(9 * FontSizeScale)
                                                .FontColor(Colors.Grey.Darken1);

                                        if (!string.IsNullOrWhiteSpace(item.Description))
                                            body.Item().PaddingTop(2).Text(item.Description)
                                                .FontSize(9 * FontSizeScale).LineHeight(LineSpacing);
                                    }));
                                }
                            }));
                        }
                        break;
                }
            }
        });
    }

    private void ComposeHeader(IContainer container, PersonalInfo info)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(nameColumn =>
                {
                    nameColumn.Item().Text(info.FullName)
                        .FontSize(24 * FontSizeScale)
                        .Bold()
                        .FontFamily(HeadingFontFamily)
                        .FontColor(ParseColor(HeadingColor));

                    if (!string.IsNullOrWhiteSpace(info.JobTitle))
                    {
                        nameColumn.Item().Text(info.JobTitle)
                            .FontSize(12 * FontSizeScale)
                            .FontColor(ParseColor(AccentColor));
                    }
                });

                var contacts = new List<string>();
                if (!string.IsNullOrWhiteSpace(info.Email)) contacts.Add(info.Email);
                if (!string.IsNullOrWhiteSpace(info.Phone)) contacts.Add(info.Phone);
                if (!string.IsNullOrWhiteSpace(info.Location)) contacts.Add(info.Location);
                if (!string.IsNullOrWhiteSpace(info.LinkedIn)) contacts.Add(FormatLinkedInDisplay(info.LinkedIn));
                if (!string.IsNullOrWhiteSpace(info.GitHub)) contacts.Add($"github.com/{FormatGitHubDisplay(info.GitHub)}");
                if (!string.IsNullOrWhiteSpace(info.Website)) contacts.Add(FormatWebsiteDisplay(info.Website));

                if (contacts.Count > 0)
                {
                    row.ConstantItem(170).AlignRight().Column(contactColumn =>
                    {
                        foreach (var contact in contacts)
                        {
                            contactColumn.Item().AlignRight().Text(contact)
                                .FontSize(8.5f * FontSizeScale)
                                .FontColor(Colors.Grey.Darken1);
                        }
                    });
                }
            });

            column.Item().PaddingTop(8).LineHorizontal(1).LineColor(ParseColor(AccentColor));
        });
    }

    private void ComposeSection(IContainer container, string title, Action<ColumnDescriptor> content)
    {
        container.Column(column =>
        {
            // The heading starts in the gutter so it anchors the column it labels.
            column.Item().Text(title.ToUpperInvariant())
                .FontSize(10 * FontSizeScale)
                .Bold()
                .FontFamily(HeadingFontFamily)
                .LetterSpacing(0.12f)
                .FontColor(ParseColor(AccentColor));

            column.Item().PaddingTop(3).PaddingBottom(8)
                .LineHorizontal(0.5f)
                .LineColor(Colors.Grey.Lighten1);

            column.Item().Column(content);
        });
    }

    /// <summary>One gutter row: the date on the left, the entry on the right.</summary>
    private void ComposeRow(IContainer container, string dateLabel, Action<ColumnDescriptor> content)
    {
        container.Row(row =>
        {
            row.ConstantItem(GutterWidth).PaddingRight(12).Text(dateLabel)
                .FontSize(8.5f * FontSizeScale)
                .FontColor(Colors.Grey.Darken2);

            row.RelativeItem().Column(content);
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
            column.Item().PaddingBottom(2).Text(text =>
            {
                text.Span($"{group.Key}: ").SemiBold().FontSize(9 * FontSizeScale);
                text.Span(string.Join(", ", group.OrderBy(s => s.Order).Select(s => s.Name)))
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
