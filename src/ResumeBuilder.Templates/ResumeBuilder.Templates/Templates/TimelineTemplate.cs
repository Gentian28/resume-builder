using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Templates.Templates;

public class TimelineTemplate : BaseTemplate
{
    public override TemplateInfo Info => new()
    {
        Id = "timeline",
        Name = "Timeline",
        Description = "Vertical timeline connecting chronological entries for a narrative feel",
        Category = TemplateCategory.Modern,
        Layout = TemplateLayout.SingleColumn,
        Tags = new[] { "timeline", "chronological", "modern" },
        DefaultAccentColor = "#6366f1",
        DefaultFontFamily = "Arial"
    };

    private const float DateColumnWidth = 65;
    private const float TimelineColumnWidth = 20;
    private const float DotSize = 8;

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
                        column.Item().Element(c => ComposeNonTimelineSection(c, "SUMMARY", ct =>
                        {
                            ct.Item().Text(resume.Summary).FontSize(9 * FontSizeScale).LineHeight(LineSpacing);
                        }));
                        break;

                    case SectionType.Experience:
                        column.Item().Element(c => ComposeSectionTitle(c, "EXPERIENCE"));
                        foreach (var exp in resume.Experiences.OrderBy(e => e.Order))
                        {
                            var isLast = exp == resume.Experiences.OrderBy(e => e.Order).Last();
                            column.Item().Element(c => ComposeTimelineEntry(c, exp.DateRange,
                                col =>
                                {
                                    col.Item().Text(exp.JobTitle).Bold().FontSize(11 * FontSizeScale);
                                    col.Item().Text(text =>
                                    {
                                        text.Span(exp.Company).SemiBold().FontSize(9 * FontSizeScale);
                                        if (!string.IsNullOrWhiteSpace(exp.Location))
                                            text.Span($"  |  {exp.Location}").FontSize(9 * FontSizeScale).FontColor(Colors.Grey.Darken1);
                                    });
                                    if (!string.IsNullOrWhiteSpace(exp.Description))
                                        col.Item().PaddingTop(3).Text(exp.Description).FontSize(9 * FontSizeScale).LineHeight(LineSpacing);
                                    if (exp.Achievements.Any())
                                    {
                                        col.Item().PaddingTop(3).Column(achCol =>
                                        {
                                            foreach (var ach in exp.Achievements)
                                            {
                                                achCol.Item().Row(row =>
                                                {
                                                    row.AutoItem().Text("• ").FontSize(9 * FontSizeScale);
                                                    row.RelativeItem().Text(ach).FontSize(9 * FontSizeScale).LineHeight(LineSpacing);
                                                });
                                            }
                                        });
                                    }
                                }, isLast));
                        }
                        break;

                    case SectionType.Education:
                        column.Item().Element(c => ComposeSectionTitle(c, "EDUCATION"));
                        foreach (var edu in resume.EducationList.OrderBy(e => e.Order))
                        {
                            var isLast = edu == resume.EducationList.OrderBy(e => e.Order).Last();
                            column.Item().Element(c => ComposeTimelineEntry(c, edu.DateRange,
                                col =>
                                {
                                    col.Item().Text(edu.DegreeWithField).Bold().FontSize(11 * FontSizeScale);
                                    col.Item().Text(edu.Institution).SemiBold().FontSize(9 * FontSizeScale);
                                    if (!string.IsNullOrWhiteSpace(edu.Grade))
                                        col.Item().Text($"Grade: {edu.Grade}").FontSize(9 * FontSizeScale).FontColor(Colors.Grey.Darken1);
                                    if (!string.IsNullOrWhiteSpace(edu.Description))
                                        col.Item().PaddingTop(2).Text(edu.Description).FontSize(9 * FontSizeScale).LineHeight(LineSpacing);
                                }, isLast));
                        }
                        break;

                    case SectionType.Skills:
                        column.Item().Element(c => ComposeNonTimelineSection(c, "SKILLS", ct =>
                        {
                            ct.Item().Text(string.Join("  •  ", resume.Skills.OrderBy(s => s.Order).Select(s => s.Name)))
                                .FontSize(9 * FontSizeScale);
                        }));
                        break;

                    case SectionType.Languages:
                        column.Item().Element(c => ComposeNonTimelineSection(c, "LANGUAGES", ct =>
                        {
                            ct.Item().Row(row =>
                            {
                                foreach (var lang in resume.Languages.OrderBy(l => l.Order))
                                {
                                    row.AutoItem().PaddingRight(20).Text(text =>
                                    {
                                        text.Span(lang.Name).Bold().FontSize(9 * FontSizeScale);
                                        text.Span($" - {GetLanguageProficiencyText(lang.Proficiency)}").FontSize(9 * FontSizeScale);
                                    });
                                }
                            });
                        }));
                        break;

                    case SectionType.Certifications:
                        column.Item().Element(c => ComposeSectionTitle(c, "CERTIFICATIONS"));
                        foreach (var cert in resume.Certifications.OrderBy(c => c.Order))
                        {
                            var isLast = cert == resume.Certifications.OrderBy(c => c.Order).Last();
                            var dateStr = cert.IssueDate?.ToString("MMM yyyy") ?? "";
                            column.Item().Element(c => ComposeTimelineEntry(c, dateStr,
                                col =>
                                {
                                    col.Item().Text(text =>
                                    {
                                        text.Span(cert.Name).Bold().FontSize(10 * FontSizeScale);
                                        if (!string.IsNullOrWhiteSpace(cert.IssuingOrganization))
                                            text.Span($" - {cert.IssuingOrganization}").FontSize(9 * FontSizeScale);
                                    });
                                }, isLast));
                        }
                        break;

                    case SectionType.Projects:
                        column.Item().Element(c => ComposeSectionTitle(c, "PROJECTS"));
                        foreach (var project in resume.Projects.OrderBy(p => p.Order))
                        {
                            var isLast = project == resume.Projects.OrderBy(p => p.Order).Last();
                            var dateStr = project.StartDate.HasValue
                                ? FormatDateRange(project.StartDate, project.EndDate, project.IsOngoing)
                                : "";
                            column.Item().Element(c => ComposeTimelineEntry(c, dateStr,
                                col =>
                                {
                                    col.Item().Text(project.Name).Bold().FontSize(11 * FontSizeScale);
                                    if (!string.IsNullOrWhiteSpace(project.Description))
                                        col.Item().PaddingTop(2).Text(project.Description).FontSize(9 * FontSizeScale).LineHeight(LineSpacing);
                                    if (project.Technologies.Any())
                                    {
                                        col.Item().PaddingTop(2).Text(text =>
                                        {
                                            text.Span("Technologies: ").Bold().FontSize(9 * FontSizeScale);
                                            text.Span(string.Join(", ", project.Technologies)).FontSize(9 * FontSizeScale);
                                        });
                                    }
                                    if (project.Highlights.Any())
                                    {
                                        col.Item().PaddingTop(2).Column(hlCol =>
                                        {
                                            foreach (var hl in project.Highlights)
                                            {
                                                hlCol.Item().Row(row =>
                                                {
                                                    row.AutoItem().Text("• ").FontSize(9 * FontSizeScale);
                                                    row.RelativeItem().Text(hl).FontSize(9 * FontSizeScale).LineHeight(LineSpacing);
                                                });
                                            }
                                        });
                                    }
                                }, isLast));
                        }
                        break;
                }
            }
        });
    }

    private void ComposeHeader(IContainer container, Resume resume)
    {
        var info = resume.PersonalInfo;
        container.Column(column =>
        {
            column.Item().Text(info.FullName)
                .FontSize(28 * FontSizeScale)
                .Bold()
                .FontFamily(HeadingFontFamily)
                .FontColor(ParseColor(HeadingColor));

            if (!string.IsNullOrWhiteSpace(info.JobTitle))
            {
                column.Item().Text(info.JobTitle)
                    .FontSize(14 * FontSizeScale)
                    .FontColor(ParseColor(AccentColor));
            }

            column.Item().Height(8);

            var contacts = new List<string>();
            if (!string.IsNullOrWhiteSpace(info.Email)) contacts.Add(info.Email);
            if (!string.IsNullOrWhiteSpace(info.Phone)) contacts.Add(info.Phone);
            if (!string.IsNullOrWhiteSpace(info.Location)) contacts.Add(info.Location);
            if (!string.IsNullOrWhiteSpace(info.LinkedIn)) contacts.Add(info.LinkedIn);
            if (!string.IsNullOrWhiteSpace(info.Website)) contacts.Add(info.Website);

            column.Item().Text(string.Join("  |  ", contacts))
                .FontSize(9 * FontSizeScale)
                .FontColor(Colors.Grey.Darken1);

            column.Item().Height(5);
            column.Item().LineHorizontal(2).LineColor(ParseColor(AccentColor));
        });
    }

    private void ComposeSectionTitle(IContainer container, string title)
    {
        container.PaddingLeft(DateColumnWidth + TimelineColumnWidth).Column(col =>
        {
            col.Item().Text(title)
                .FontSize(13 * FontSizeScale)
                .Bold()
                .FontFamily(HeadingFontFamily)
                .FontColor(ParseColor(AccentColor))
                .LetterSpacing(0.05f);
            col.Item().Height(4);
        });
    }

    private void ComposeTimelineEntry(IContainer container, string dateLabel, Action<ColumnDescriptor> content, bool isLast)
    {
        container.Row(row =>
        {
            // Date column
            row.ConstantItem(DateColumnWidth).AlignRight().PaddingRight(8).PaddingTop(2)
                .Text(dateLabel).FontSize(8 * FontSizeScale).FontColor(Colors.Grey.Darken1);

            // Timeline column (dot + line)
            row.ConstantItem(TimelineColumnWidth).Column(timeCol =>
            {
                // Dot
                timeCol.Item().AlignCenter()
                    .Width(DotSize).Height(DotSize)
                    .Background(ParseColor(AccentColor));

                // Vertical connector line
                if (!isLast)
                {
                    timeCol.Item().AlignCenter()
                        .Width(2).ExtendVertical()
                        .Background(ParseColor(AccentColor).WithAlpha(0.3f));
                }
            });

            // Content column
            row.RelativeItem().PaddingLeft(5).PaddingBottom(12).Column(content);
        });
    }

    private void ComposeNonTimelineSection(IContainer container, string title, Action<ColumnDescriptor> content)
    {
        container.PaddingLeft(DateColumnWidth + TimelineColumnWidth).Column(col =>
        {
            col.Item().Text(title)
                .FontSize(13 * FontSizeScale)
                .Bold()
                .FontFamily(HeadingFontFamily)
                .FontColor(ParseColor(AccentColor))
                .LetterSpacing(0.05f);
            col.Item().Height(6);
            col.Item().Column(content);
        });
    }
}
