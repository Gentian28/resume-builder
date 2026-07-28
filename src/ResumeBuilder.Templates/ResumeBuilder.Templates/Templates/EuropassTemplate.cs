using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Templates.Templates;

/// <summary>
/// The European CV convention: a narrow left column naming the section ("Work experience",
/// "Education and training") against the content on the right, a photo in the header, and languages
/// presented as a proficiency table rather than as bars. Every entry repeats its own label, which is
/// what makes a Europass scannable when it runs long.
/// </summary>
public class EuropassTemplate : BaseTemplate
{
    public override TemplateInfo Info => new()
    {
        Id = "europass",
        Name = "European CV",
        Description = "Europass-inspired label column with photo header and a language proficiency table",
        Category = TemplateCategory.Classic,
        Layout = TemplateLayout.SingleColumn,
        Tags = new[] { "europass", "european", "cv", "photo", "formal" },
        DefaultAccentColor = "#00509e",
        DefaultFontFamily = "Georgia"
    };

    private const float LabelColumnWidth = 130;
    private const float PhotoSize = 85;

    private static readonly EntryStyle Entry = new()
    {
        TitleFontSize = 11,
        SubtitleFontSize = 10,
        BodyFontSize = 9,
        MetaFontSize = 9
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
                        column.Item().Element(c => ComposeHeader(c, resume));
                        break;

                    case SectionType.Summary:
                        column.Item().Element(c => ComposeLabelledSection(c, "Personal statement", col =>
                            col.Item().Text(resume.Summary).FontSize(9 * FontSizeScale).LineHeight(LineSpacing)));
                        break;

                    case SectionType.Experience:
                        column.Item().Element(c => ComposeLabelledSection(c, "Work experience", col =>
                        {
                            col.Spacing(10);
                            foreach (var exp in resume.Experiences.OrderBy(e => e.Order))
                                col.Item().Element(e => ComposeExperienceEntry(e, exp, Entry with { SubtitleColor = ParseColor(AccentColor) }));
                        }));
                        break;

                    case SectionType.Education:
                        column.Item().Element(c => ComposeLabelledSection(c, "Education and training", col =>
                        {
                            col.Spacing(8);
                            foreach (var edu in resume.EducationList.OrderBy(e => e.Order))
                                col.Item().Element(e => ComposeEducationEntry(e, edu, Entry with { SubtitleColor = ParseColor(AccentColor) }));
                        }));
                        break;

                    case SectionType.Skills:
                        column.Item().Element(c => ComposeLabelledSection(c, "Skills and competences", col =>
                            ComposeSkills(col, resume.Skills)));
                        break;

                    case SectionType.Languages:
                        column.Item().Element(c => ComposeLabelledSection(c, "Language skills", col =>
                            col.Item().Element(e => ComposeLanguageTable(e, resume.Languages))));
                        break;

                    case SectionType.Certifications:
                        column.Item().Element(c => ComposeLabelledSection(c, "Certificates", col =>
                        {
                            foreach (var cert in resume.Certifications.OrderBy(c2 => c2.Order))
                            {
                                col.Item().PaddingBottom(4).Text(text =>
                                {
                                    text.Span(cert.Name).SemiBold().FontSize(10 * FontSizeScale);
                                    if (!string.IsNullOrWhiteSpace(cert.IssuingOrganization))
                                        text.Span($" — {cert.IssuingOrganization}").FontSize(9 * FontSizeScale);
                                    if (cert.IssueDate.HasValue)
                                        text.Span($" ({ResumeDateFormat.MonthYear(cert.IssueDate)})")
                                            .FontSize(9 * FontSizeScale).FontColor(Colors.Grey.Darken1);
                                });
                            }
                        }));
                        break;

                    case SectionType.Projects:
                        column.Item().Element(c => ComposeLabelledSection(c, "Projects", col =>
                        {
                            col.Spacing(8);
                            foreach (var project in resume.Projects.OrderBy(p => p.Order))
                                col.Item().Element(e => ComposeProjectEntry(e, project, Entry));
                        }));
                        break;

                    case SectionType.CustomSections:
                        foreach (var custom in GetVisibleCustomSections(resume))
                        {
                            column.Item().Element(c => ComposeLabelledSection(c, custom.Title, col =>
                                ComposeCustomSectionItems(col, custom, 10, 9)));
                        }
                        break;
                }
            }
        });
    }

    private void ComposeHeader(IContainer container, Resume resume)
    {
        var info = resume.PersonalInfo;
        var accent = ParseColor(AccentColor);

        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(details =>
                {
                    details.Item().Text("Curriculum Vitae")
                        .FontSize(9 * FontSizeScale)
                        .LetterSpacing(0.15f)
                        .FontColor(accent);

                    details.Item().PaddingTop(2).Text(info.FullName)
                        .FontSize(22 * FontSizeScale)
                        .Bold()
                        .FontFamily(HeadingFontFamily)
                        .FontColor(ParseColor(HeadingColor));

                    if (!string.IsNullOrWhiteSpace(info.JobTitle))
                    {
                        details.Item().Text(info.JobTitle)
                            .FontSize(11 * FontSizeScale)
                            .FontColor(Colors.Grey.Darken2);
                    }

                    details.Item().Height(8);

                    foreach (var (label, value) in ContactRows(info))
                    {
                        details.Item().PaddingBottom(2).Row(contactRow =>
                        {
                            contactRow.ConstantItem(70).Text(label)
                                .FontSize(8 * FontSizeScale)
                                .FontColor(accent);
                            contactRow.RelativeItem().Text(value).FontSize(9 * FontSizeScale);
                        });
                    }
                });

                // The photo is a first-class part of a Europass; when there is none the header simply
                // reflows to the full width rather than leaving a gap.
                if (info.Photo is { Length: > 0 })
                {
                    row.ConstantItem(PhotoSize + 12).AlignRight().Element(c => c
                        .Width(PhotoSize)
                        .Border(1)
                        .BorderColor(accent)
                        .Padding(2)
                        .Height(PhotoSize)
                        .Image(info.Photo)
                        .FitArea());
                }
            });

            column.Item().PaddingTop(10).LineHorizontal(1.5f).LineColor(accent);
        });
    }

    private static List<(string Label, string Value)> ContactRows(PersonalInfo info)
    {
        var rows = new List<(string, string)>();

        void Add(string label, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                rows.Add((label, value.Trim()));
        }

        var address = string.Join(", ", new[] { info.Address, info.City, info.PostalCode, info.Country }
            .Where(part => !string.IsNullOrWhiteSpace(part)));

        Add("Address", address);
        Add("Telephone", info.Phone);
        Add("Email", info.Email);
        Add("Website", FormatWebsiteDisplay(info.Website));
        Add("LinkedIn", FormatLinkedInDisplay(info.LinkedIn));
        Add("GitHub", FormatGitHubDisplay(info.GitHub));

        return rows;
    }

    /// <summary>The Europass signature: the section name lives in the left gutter, content on the right.</summary>
    private void ComposeLabelledSection(IContainer container, string label, Action<ColumnDescriptor> content)
    {
        container.Row(row =>
        {
            row.ConstantItem(LabelColumnWidth).PaddingRight(12).Column(labelColumn =>
            {
                labelColumn.Item().Text(label)
                    .FontSize(10 * FontSizeScale)
                    .Bold()
                    .FontFamily(HeadingFontFamily)
                    .FontColor(ParseColor(AccentColor));
            });

            row.RelativeItem()
                .BorderLeft(1)
                .BorderColor(ParseColor(AccentColor).WithAlpha(0.25f))
                .PaddingLeft(12)
                .Column(content);
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
                row.ConstantItem(90).Text(group.Key)
                    .SemiBold()
                    .FontSize(9 * FontSizeScale)
                    .FontColor(Colors.Grey.Darken2);
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

    private void ComposeLanguageTable(IContainer container, List<Language> languages)
    {
        var accent = ParseColor(AccentColor);
        var headerFont = 8 * FontSizeScale;
        var bodyFont = 9 * FontSizeScale;

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2);
                columns.RelativeColumn(3);
            });

            table.Cell().Background(accent).Padding(4).Text("Language")
                .FontSize(headerFont).Bold().FontColor(Colors.White);
            table.Cell().Background(accent).Padding(4).Text("Proficiency")
                .FontSize(headerFont).Bold().FontColor(Colors.White);

            foreach (var language in languages.OrderBy(l => l.Order))
            {
                table.Cell()
                    .BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1)
                    .Padding(4)
                    .Text(language.Name).FontSize(bodyFont).SemiBold();

                table.Cell()
                    .BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1)
                    .Padding(4)
                    .Text(GetLanguageProficiencyText(language.Proficiency)).FontSize(bodyFont);
            }
        });
    }
}
