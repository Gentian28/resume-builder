using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Templates.Templates;

/// <summary>
/// A code-editor reading of a resume: monospace type, markdown-style section headings, comment-styled
/// metadata and skills as inline tags, with the repository and site links promoted to the top of the
/// contact block. The conceit stops at the point where it would cost legibility — body copy still
/// wraps and prints normally, and every glyph used is plain ASCII so it survives any monospace face.
/// </summary>
public class DeveloperTemplate : BaseTemplate
{
    public override TemplateInfo Info => new()
    {
        Id = "developer",
        Name = "Developer",
        Description = "Monospace, code-editor aesthetic with markdown headings, skill tags and prominent GitHub links",
        Category = TemplateCategory.Technical,
        Layout = TemplateLayout.SingleColumn,
        Tags = new[] { "developer", "monospace", "code", "engineer", "technical" },
        DefaultAccentColor = "#22a06b",
        DefaultFontFamily = "Courier New"
    };

    private static readonly EntryStyle Entry = new()
    {
        TitleFontSize = 11,
        SubtitleFontSize = 9,
        BodyFontSize = 9,
        MetaFontSize = 8,
        Bullet = "- "
    };

    private Color Muted => Colors.Grey.Darken1;

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
                        column.Item().Element(c => ComposeSection(c, "about", col =>
                            col.Item().Text(resume.Summary).FontSize(9 * FontSizeScale).LineHeight(LineSpacing)));
                        break;

                    case SectionType.Experience:
                        column.Item().Element(c => ComposeSection(c, "experience", col =>
                        {
                            col.Spacing(10);
                            foreach (var exp in resume.Experiences.OrderBy(e => e.Order))
                                col.Item().Element(e => ComposeExperience(e, exp));
                        }));
                        break;

                    case SectionType.Education:
                        column.Item().Element(c => ComposeSection(c, "education", col =>
                        {
                            col.Spacing(6);
                            foreach (var edu in resume.EducationList.OrderBy(e => e.Order))
                                col.Item().Element(e => ComposeEducationEntry(e, edu, Entry with { SubtitleColor = ParseColor(AccentColor) }));
                        }));
                        break;

                    case SectionType.Skills:
                        column.Item().Element(c => ComposeSection(c, "skills", col => ComposeSkillTags(col, resume.Skills)));
                        break;

                    case SectionType.Languages:
                        column.Item().Element(c => ComposeSection(c, "languages", col =>
                        {
                            foreach (var lang in resume.Languages.OrderBy(l => l.Order))
                                col.Item().Element(e => ComposeKeyValue(e, lang.Name, GetLanguageProficiencyText(lang.Proficiency)));
                        }));
                        break;

                    case SectionType.Certifications:
                        column.Item().Element(c => ComposeSection(c, "certifications", col =>
                        {
                            foreach (var cert in resume.Certifications.OrderBy(c2 => c2.Order))
                            {
                                col.Item().PaddingBottom(2).Text(text =>
                                {
                                    text.Span("- ").FontSize(9 * FontSizeScale).FontColor(ParseColor(AccentColor));
                                    text.Span(cert.Name).Bold().FontSize(9 * FontSizeScale);
                                    if (!string.IsNullOrWhiteSpace(cert.IssuingOrganization))
                                        text.Span($" @ {cert.IssuingOrganization}").FontSize(9 * FontSizeScale);
                                    if (cert.IssueDate.HasValue)
                                        text.Span($"  // {ResumeDateFormat.MonthYear(cert.IssueDate)}")
                                            .FontSize(8 * FontSizeScale).FontColor(Muted);
                                });
                            }
                        }));
                        break;

                    case SectionType.Projects:
                        column.Item().Element(c => ComposeSection(c, "projects", col =>
                        {
                            col.Spacing(8);
                            foreach (var project in resume.Projects.OrderBy(p => p.Order))
                                col.Item().Element(e => ComposeProjectEntry(e, project, Entry));
                        }));
                        break;

                    case SectionType.CustomSections:
                        foreach (var custom in GetVisibleCustomSections(resume))
                        {
                            column.Item().Element(c => ComposeSection(c, custom.Title.ToLowerInvariant(), col =>
                                ComposeCustomSectionItems(col, custom, 10, 9)));
                        }
                        break;
                }
            }
        });
    }

    private void ComposeHeader(IContainer container, PersonalInfo info)
    {
        var accent = ParseColor(AccentColor);

        container.Column(column =>
        {
            column.Item().Text(text =>
            {
                text.Span("# ").FontSize(20 * FontSizeScale).FontColor(accent);
                text.Span(info.FullName)
                    .FontSize(20 * FontSizeScale)
                    .Bold()
                    .FontFamily(HeadingFontFamily)
                    .FontColor(ParseColor(HeadingColor));
            });

            if (!string.IsNullOrWhiteSpace(info.JobTitle))
            {
                column.Item().Text($"// {info.JobTitle}")
                    .FontSize(11 * FontSizeScale)
                    .FontColor(Muted);
            }

            column.Item().Height(8);

            // Repository and site come first: for an engineer they are the portfolio.
            if (!string.IsNullOrWhiteSpace(info.GitHub))
                column.Item().Element(c => ComposeKeyValue(c, "github", $"github.com/{FormatGitHubDisplay(info.GitHub)}", accent));

            if (!string.IsNullOrWhiteSpace(info.Website))
                column.Item().Element(c => ComposeKeyValue(c, "website", FormatWebsiteDisplay(info.Website), accent));

            if (!string.IsNullOrWhiteSpace(info.LinkedIn))
                column.Item().Element(c => ComposeKeyValue(c, "linkedin", FormatLinkedInDisplay(info.LinkedIn)));

            if (!string.IsNullOrWhiteSpace(info.Email))
                column.Item().Element(c => ComposeKeyValue(c, "email", info.Email));

            if (!string.IsNullOrWhiteSpace(info.Phone))
                column.Item().Element(c => ComposeKeyValue(c, "phone", info.Phone));

            if (!string.IsNullOrWhiteSpace(info.Location))
                column.Item().Element(c => ComposeKeyValue(c, "location", info.Location));

            column.Item().Height(6);
            column.Item().LineHorizontal(1).LineColor(accent.WithAlpha(0.5f));
        });
    }

    private void ComposeKeyValue(IContainer container, string key, string value, Color? valueColor = null)
    {
        container.PaddingBottom(2).Row(row =>
        {
            row.ConstantItem(70).Text($"{key}:")
                .FontSize(9 * FontSizeScale)
                .FontColor(Muted);

            var text = row.RelativeItem().Text(value).FontSize(9 * FontSizeScale);
            if (valueColor.HasValue)
                text.FontColor(valueColor.Value);
        });
    }

    private void ComposeSection(IContainer container, string title, Action<ColumnDescriptor> content)
    {
        var accent = ParseColor(AccentColor);

        container.EnsureSpace(90).Column(column =>
        {
            column.Item().Text(text =>
            {
                text.Span("## ").FontSize(12 * FontSizeScale).Bold().FontColor(Muted);
                text.Span(title)
                    .FontSize(12 * FontSizeScale)
                    .Bold()
                    .FontFamily(HeadingFontFamily)
                    .FontColor(accent);
            });

            column.Item().Height(6);
            column.Item().Column(content);
        });
    }

    private void ComposeExperience(IContainer container, Experience exp)
    {
        var accent = ParseColor(AccentColor);

        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.Span(exp.JobTitle).Bold().FontSize(11 * FontSizeScale);
                    if (!string.IsNullOrWhiteSpace(exp.Company))
                        text.Span($" @ {exp.Company}").FontSize(10 * FontSizeScale).FontColor(accent);
                });

                if (!string.IsNullOrWhiteSpace(exp.DateRange))
                {
                    row.AutoItem().Text($"// {exp.DateRange}")
                        .FontSize(8 * FontSizeScale)
                        .FontColor(Muted);
                }
            });

            if (!string.IsNullOrWhiteSpace(exp.Location))
                column.Item().Text($"// {exp.Location}").FontSize(8 * FontSizeScale).FontColor(Muted);

            if (!string.IsNullOrWhiteSpace(exp.Description))
                column.Item().PaddingTop(3).Text(exp.Description).FontSize(9 * FontSizeScale).LineHeight(LineSpacing);

            if (exp.Achievements.Any())
                column.Item().PaddingTop(3).Element(c => ComposeBulletList(c, exp.Achievements, Entry));
        });
    }

    private void ComposeSkillTags(ColumnDescriptor column, List<Skill> skills)
    {
        var groups = skills
            .GroupBy(s => string.IsNullOrWhiteSpace(s.Category) ? string.Empty : s.Category)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .ToList();

        foreach (var group in groups)
        {
            column.Item().PaddingBottom(6).Column(groupColumn =>
            {
                if (!string.IsNullOrWhiteSpace(group.Key))
                {
                    groupColumn.Item().PaddingBottom(3).Text($"// {group.Key}")
                        .FontSize(8 * FontSizeScale)
                        .FontColor(Muted);
                }

                // Inlined wraps to as many rows as the tags need, so no skill is ever dropped.
                groupColumn.Item().Inlined(inlined =>
                {
                    inlined.Spacing(5);
                    inlined.VerticalSpacing(5);

                    foreach (var skill in group.OrderBy(s => s.Order))
                        inlined.Item().Element(c => ComposeTag(c, skill.Name));
                });
            });
        }
    }

    private void ComposeTag(IContainer container, string label)
    {
        var accent = ParseColor(AccentColor);

        container
            .Background(accent.WithAlpha(0.10f))
            .Border(0.75f)
            .BorderColor(accent.WithAlpha(0.45f))
            .PaddingVertical(2)
            .PaddingHorizontal(5)
            .Text(label)
            .FontSize(8.5f * FontSizeScale)
            .FontColor(ParseColor(TextColor));
    }
}
