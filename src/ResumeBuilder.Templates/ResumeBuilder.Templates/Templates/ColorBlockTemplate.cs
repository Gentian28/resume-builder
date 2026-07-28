using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Templates.Templates;

/// <summary>
/// A full-bleed colour block: the sidebar is painted in the resume's own accent colour and carries
/// the contact details, skills and languages; the main column stays white and airy. Where the Dark
/// Sidebar template is near-black and dense, this one takes its personality from whatever accent the
/// user picked and leaves more air around everything.
/// </summary>
public class ColorBlockTemplate : BaseTemplate
{
    public override TemplateInfo Info => new()
    {
        Id = "colorblock",
        Name = "Color Block",
        Description = "Full-bleed accent-coloured sidebar for contact, skills and languages beside an airy white column",
        Category = TemplateCategory.Modern,
        Layout = TemplateLayout.Sidebar,
        Tags = new[] { "color", "sidebar", "block", "modern", "airy" },
        DefaultAccentColor = "#e11d48",
        DefaultFontFamily = "Calibri"
    };

    private const float SidebarWidth = 185;

    /// <summary>Sections routed to the coloured block; everything else stays in the white column.</summary>
    private static readonly SectionType[] SidebarSections =
    {
        SectionType.PersonalInfo,
        SectionType.Skills,
        SectionType.Languages
    };

    private static readonly EntryStyle Entry = new();

    protected override void ConfigurePage(PageDescriptor page)
    {
        page.Size(PageSizes.A4);
        page.Margin(0); // Full-bleed sidebar; each column supplies its own padding.
        page.DefaultTextStyle(x => x
            .FontSize(10 * FontSizeScale)
            .FontFamily(FontFamily)
            .FontColor(ParseColor(TextColor)));
    }

    protected override void ComposeContent(IContainer container, Resume resume)
    {
        var accent = ParseColor(AccentColor);

        container.Row(row =>
        {
            row.ConstantItem(SidebarWidth)
                .Background(accent)
                .PaddingVertical(28)
                .PaddingHorizontal(20)
                .Column(sidebar =>
                {
                    sidebar.Spacing(SectionSpacing + 4);
                    ComposeSidebar(sidebar, resume, accent);
                });

            row.RelativeItem()
                .PaddingVertical(28)
                .PaddingHorizontal(26)
                .Column(main =>
                {
                    main.Spacing(SectionSpacing + 2);
                    ComposeMain(main, resume, accent);
                });
        });
    }

    private void ComposeSidebar(ColumnDescriptor sidebar, Resume resume, Color accent)
    {
        // The name heads the block whether or not the contact section is switched on, so the coloured
        // column never opens on a stray heading.
        sidebar.Item().Column(nameColumn =>
        {
            nameColumn.Item().Text(resume.PersonalInfo.FirstName)
                .FontSize(21 * FontSizeScale).Bold()
                .FontFamily(HeadingFontFamily).FontColor(Colors.White);

            nameColumn.Item().Text(resume.PersonalInfo.LastName)
                .FontSize(21 * FontSizeScale).Bold()
                .FontFamily(HeadingFontFamily).FontColor(Colors.White);

            if (!string.IsNullOrWhiteSpace(resume.PersonalInfo.JobTitle))
            {
                nameColumn.Item().PaddingTop(4).Text(resume.PersonalInfo.JobTitle)
                    .FontSize(10 * FontSizeScale)
                    .FontColor(Colors.White.WithAlpha(0.85f));
            }
        });

        foreach (var sectionType in GetOrderedSections())
        {
            if (!SidebarSections.Contains(sectionType) || !ShouldRenderSection(sectionType, resume))
                continue;

            switch (sectionType)
            {
                case SectionType.PersonalInfo:
                    sidebar.Item().Element(c => ComposeSidebarSection(c, "Contact", col =>
                    {
                        foreach (var contact in ContactLines(resume.PersonalInfo))
                        {
                            col.Item().PaddingBottom(4).Text(contact)
                                .FontSize(8.5f * FontSizeScale)
                                .FontColor(Colors.White)
                                .LineHeight(1.2f);
                        }
                    }));
                    break;

                case SectionType.Skills:
                    sidebar.Item().Element(c => ComposeSidebarSection(c, "Skills", col =>
                    {
                        foreach (var group in resume.Skills
                                     .GroupBy(s => string.IsNullOrWhiteSpace(s.Category) ? string.Empty : s.Category)
                                     .OrderBy(g => g.Key, StringComparer.Ordinal))
                        {
                            if (!string.IsNullOrWhiteSpace(group.Key))
                            {
                                col.Item().PaddingTop(2).PaddingBottom(3).Text(group.Key)
                                    .FontSize(8 * FontSizeScale)
                                    .SemiBold()
                                    .LetterSpacing(0.08f)
                                    .FontColor(Colors.White.WithAlpha(0.75f));
                            }

                            foreach (var skill in group.OrderBy(s => s.Order))
                            {
                                col.Item().PaddingBottom(6).Column(skillColumn =>
                                {
                                    skillColumn.Item().Text(skill.Name)
                                        .FontSize(8.5f * FontSizeScale)
                                        .FontColor(Colors.White);
                                    skillColumn.Item().Height(3);

                                    // The helper guards the 100% case that a raw RelativeItem(0) would throw on.
                                    skillColumn.Item().Element(bar => ComposeSkillBar(
                                        bar, skill.Level, Colors.White, Colors.White.WithAlpha(0.3f), 3));
                                });
                            }
                        }
                    }));
                    break;

                case SectionType.Languages:
                    sidebar.Item().Element(c => ComposeSidebarSection(c, "Languages", col =>
                    {
                        foreach (var lang in resume.Languages.OrderBy(l => l.Order))
                        {
                            col.Item().PaddingBottom(4).Text(text =>
                            {
                                text.Span(lang.Name).FontSize(8.5f * FontSizeScale)
                                    .SemiBold().FontColor(Colors.White);
                                text.Span($" — {GetLanguageProficiencyText(lang.Proficiency)}")
                                    .FontSize(8 * FontSizeScale)
                                    .FontColor(Colors.White.WithAlpha(0.8f));
                            });
                        }
                    }));
                    break;
            }
        }
    }

    private static List<string> ContactLines(PersonalInfo info)
    {
        var lines = new List<string>();

        void Add(string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                lines.Add(value.Trim());
        }

        Add(info.Email);
        Add(info.Phone);
        Add(info.Location);
        Add(FormatLinkedInDisplay(info.LinkedIn));
        Add(string.IsNullOrWhiteSpace(info.GitHub) ? null : $"github.com/{FormatGitHubDisplay(info.GitHub)}");
        Add(FormatWebsiteDisplay(info.Website));

        return lines;
    }

    private void ComposeSidebarSection(IContainer container, string title, Action<ColumnDescriptor> content)
    {
        container.Column(column =>
        {
            column.Item().Text(title.ToUpperInvariant())
                .FontSize(9 * FontSizeScale)
                .Bold()
                .LetterSpacing(0.14f)
                .FontColor(Colors.White);

            column.Item().PaddingTop(4).PaddingBottom(8)
                .Width(26).Height(2)
                .Background(Colors.White.WithAlpha(0.7f));

            column.Item().Column(content);
        });
    }

    private void ComposeMain(ColumnDescriptor main, Resume resume, Color accent)
    {
        foreach (var sectionType in GetOrderedSections())
        {
            if (SidebarSections.Contains(sectionType) || !ShouldRenderSection(sectionType, resume))
                continue;

            switch (sectionType)
            {
                case SectionType.Summary:
                    main.Item().Element(c => ComposeMainSection(c, "Profile", accent, col =>
                        col.Item().Text(resume.Summary).FontSize(9.5f * FontSizeScale).LineHeight(LineSpacing)));
                    break;

                case SectionType.Experience:
                    main.Item().Element(c => ComposeMainSection(c, "Experience", accent, col =>
                    {
                        col.Spacing(11);
                        foreach (var exp in resume.Experiences.OrderBy(e => e.Order))
                            col.Item().Element(e => ComposeExperienceEntry(e, exp, Entry with { SubtitleColor = accent }));
                    }));
                    break;

                case SectionType.Education:
                    main.Item().Element(c => ComposeMainSection(c, "Education", accent, col =>
                    {
                        col.Spacing(8);
                        foreach (var edu in resume.EducationList.OrderBy(e => e.Order))
                            col.Item().Element(e => ComposeEducationEntry(e, edu, Entry with { SubtitleColor = accent }));
                    }));
                    break;

                case SectionType.Certifications:
                    main.Item().Element(c => ComposeMainSection(c, "Certifications", accent, col =>
                    {
                        foreach (var cert in resume.Certifications.OrderBy(c2 => c2.Order))
                        {
                            col.Item().PaddingBottom(4).Row(row =>
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
                                        .FontSize(9 * FontSizeScale).FontColor(Colors.Grey.Darken1);
                                }
                            });
                        }
                    }));
                    break;

                case SectionType.Projects:
                    main.Item().Element(c => ComposeMainSection(c, "Projects", accent, col =>
                    {
                        col.Spacing(9);
                        foreach (var project in resume.Projects.OrderBy(p => p.Order))
                            col.Item().Element(e => ComposeProjectEntry(e, project, Entry));
                    }));
                    break;

                case SectionType.CustomSections:
                    foreach (var custom in GetVisibleCustomSections(resume))
                    {
                        main.Item().Element(c => ComposeMainSection(c, custom.Title, accent, col =>
                            ComposeCustomSectionItems(col, custom)));
                    }
                    break;
            }
        }
    }

    private void ComposeMainSection(IContainer container, string title, Color accent, Action<ColumnDescriptor> content)
    {
        container.Column(column =>
        {
            column.Item().Text(title.ToUpperInvariant())
                .FontSize(12 * FontSizeScale)
                .Bold()
                .FontFamily(HeadingFontFamily)
                .LetterSpacing(0.1f)
                .FontColor(accent);

            column.Item().PaddingTop(4).PaddingBottom(9)
                .LineHorizontal(0.75f)
                .LineColor(accent.WithAlpha(0.3f));

            column.Item().Column(content);
        });
    }
}
