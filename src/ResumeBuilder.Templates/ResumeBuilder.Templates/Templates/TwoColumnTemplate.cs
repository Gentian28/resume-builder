using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Templates.Templates;

public class TwoColumnTemplate : BaseTemplate
{
    public override TemplateInfo Info => new()
    {
        Id = "two-column",
        Name = "Two Column",
        Description = "Efficient two-column layout with sidebar for contact and skills",
        Category = TemplateCategory.Professional,
        Layout = TemplateLayout.Sidebar,
        Tags = new[] { "efficient", "organized", "sidebar" },
        DefaultAccentColor = "#0284c7",
        DefaultFontFamily = "Arial"
    };

    // Sections that live in the sidebar; the rest go in the main column.
    private static readonly SectionType[] SidebarSections =
    {
        SectionType.PersonalInfo,
        SectionType.Skills,
        SectionType.Languages,
        SectionType.Certifications
    };

    private static readonly EntryStyle Entry = new()
    {
        TitleFontSize = 10,
        SubtitleFontSize = 9,
        BodyFontSize = 8,
        MetaFontSize = 8,
        LocationSeparator = " | "
    };

    protected override void ComposeContent(IContainer container, Resume resume)
    {
        container.Row(row =>
        {
            // Left sidebar (lighter)
            row.ConstantItem(165).Background(Colors.Grey.Lighten4).Padding(15).Column(sidebar =>
            {
                ComposeSidebar(sidebar, resume);
            });

            // Main content
            row.RelativeItem().PaddingLeft(20).Column(main =>
            {
                main.Spacing(SectionSpacing);
                ComposeMainContent(main, resume);
            });
        });
    }

    private void ComposeSidebar(ColumnDescriptor sidebar, Resume resume)
    {
        sidebar.Spacing(SectionSpacing);

        // Name always heads the sidebar
        sidebar.Item().Column(col =>
        {
            col.Item().Text(resume.PersonalInfo.FirstName).FontSize(18 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor));
            col.Item().Text(resume.PersonalInfo.LastName).FontSize(18 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor));

            if (!string.IsNullOrWhiteSpace(resume.PersonalInfo.JobTitle))
            {
                col.Item().Height(5);
                col.Item().Text(resume.PersonalInfo.JobTitle).FontSize(9 * FontSizeScale).FontColor(Colors.Grey.Darken2);
            }
        });

        foreach (var sectionType in GetOrderedSections())
        {
            if (!SidebarSections.Contains(sectionType) || !ShouldRenderSection(sectionType, resume))
                continue;

            switch (sectionType)
            {
                case SectionType.PersonalInfo:
                    sidebar.Item().Element(c => ComposeSidebarSection(c, "CONTACT", col =>
                    {
                        var info = resume.PersonalInfo;
                        if (!string.IsNullOrWhiteSpace(info.Email))
                            col.Item().PaddingBottom(3).Text(info.Email).FontSize(8 * FontSizeScale);
                        if (!string.IsNullOrWhiteSpace(info.Phone))
                            col.Item().PaddingBottom(3).Text(info.Phone).FontSize(8 * FontSizeScale);
                        if (!string.IsNullOrWhiteSpace(info.Location))
                            col.Item().PaddingBottom(3).Text(info.Location).FontSize(8 * FontSizeScale);
                        if (!string.IsNullOrWhiteSpace(info.Website))
                            col.Item().PaddingBottom(3).Text(info.Website).FontSize(8 * FontSizeScale);
                        if (!string.IsNullOrWhiteSpace(info.LinkedIn))
                            col.Item().PaddingBottom(3).Text(FormatLinkedInDisplay(info.LinkedIn)).FontSize(8 * FontSizeScale);
                        if (!string.IsNullOrWhiteSpace(info.GitHub))
                            col.Item().PaddingBottom(3).Text($"github.com/{FormatGitHubDisplay(info.GitHub)}").FontSize(8 * FontSizeScale);
                    }));
                    break;

                case SectionType.Skills:
                    sidebar.Item().Element(c => ComposeSidebarSection(c, "SKILLS", col =>
                    {
                        foreach (var group in resume.Skills.GroupBy(s => s.Category))
                        {
                            if (!string.IsNullOrWhiteSpace(group.Key))
                            {
                                col.Item().PaddingTop(4).Text(group.Key).FontSize(8 * FontSizeScale).Bold();
                            }

                            foreach (var skill in group.OrderBy(s => s.Order))
                            {
                                col.Item().PaddingBottom(4).Column(skillCol =>
                                {
                                    skillCol.Item().Text(skill.Name).FontSize(8 * FontSizeScale);
                                    skillCol.Item().Height(3);
                                    skillCol.Item().Width(130).Element(bar =>
                                        ComposeSkillBar(bar, skill.Level, ParseColor(AccentColor), Colors.Grey.Lighten2));
                                });
                            }
                        }
                    }));
                    break;

                case SectionType.Languages:
                    sidebar.Item().Element(c => ComposeSidebarSection(c, "LANGUAGES", col =>
                    {
                        foreach (var lang in resume.Languages.OrderBy(l => l.Order))
                        {
                            col.Item().PaddingBottom(3).Text(text =>
                            {
                                text.Span(lang.Name).FontSize(8 * FontSizeScale);
                                text.Span($" - {GetLanguageProficiencyText(lang.Proficiency)}").FontSize(7 * FontSizeScale).FontColor(Colors.Grey.Darken1);
                            });
                        }
                    }));
                    break;

                case SectionType.Certifications:
                    sidebar.Item().Element(c => ComposeSidebarSection(c, "CERTIFICATIONS", col =>
                    {
                        foreach (var cert in resume.Certifications.OrderBy(c => c.Order))
                        {
                            col.Item().PaddingBottom(5).Column(certCol =>
                            {
                                certCol.Item().Text(cert.Name).FontSize(8 * FontSizeScale).Bold();
                                certCol.Item().Text(cert.IssuingOrganization).FontSize(7 * FontSizeScale);
                                if (cert.IssueDate.HasValue)
                                    certCol.Item().Text(ResumeDateFormat.MonthYear(cert.IssueDate)).FontSize(7 * FontSizeScale).FontColor(Colors.Grey.Darken1);
                            });
                        }
                    }));
                    break;
            }
        }
    }

    private void ComposeSidebarSection(IContainer container, string title, Action<ColumnDescriptor> content)
    {
        container.Column(col =>
        {
            col.Item().Text(title).FontSize(9 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor)).LetterSpacing(0.1f);
            col.Item().Height(5);
            col.Item().LineHorizontal(1).LineColor(ParseColor(AccentColor));
            col.Item().Height(5);
            col.Item().Column(content);
        });
    }

    private void ComposeMainContent(ColumnDescriptor main, Resume resume)
    {
        foreach (var sectionType in GetOrderedSections())
        {
            if (SidebarSections.Contains(sectionType) || !ShouldRenderSection(sectionType, resume))
                continue;

            switch (sectionType)
            {
                case SectionType.Summary:
                    main.Item().Element(c => ComposeMainSection(c, "PROFILE", col =>
                    {
                        col.Item().Text(resume.Summary).FontSize(9 * FontSizeScale).LineHeight(LineSpacing);
                    }));
                    break;

                case SectionType.Experience:
                    main.Item().Element(c => ComposeMainSection(c, "EXPERIENCE", col =>
                    {
                        col.Spacing(10);
                        foreach (var exp in resume.Experiences.OrderBy(e => e.Order))
                            col.Item().Element(e => ComposeExperienceEntry(e, exp, Entry with { SubtitleColor = ParseColor(AccentColor) }));
                    }));
                    break;

                case SectionType.Education:
                    main.Item().Element(c => ComposeMainSection(c, "EDUCATION", col =>
                    {
                        col.Spacing(8);
                        foreach (var edu in resume.EducationList.OrderBy(e => e.Order))
                            col.Item().Element(e => ComposeEducationEntry(e, edu, Entry with { SubtitleColor = ParseColor(AccentColor) }));
                    }));
                    break;

                case SectionType.Projects:
                    main.Item().Element(c => ComposeMainSection(c, "PROJECTS", col =>
                    {
                        col.Spacing(8);
                        foreach (var proj in resume.Projects.OrderBy(p => p.Order))
                            col.Item().Element(e => ComposeProjectEntry(e, proj, Entry));
                    }));
                    break;

                case SectionType.CustomSections:
                    foreach (var custom in GetVisibleCustomSections(resume))
                    {
                        main.Item().Element(c => ComposeMainSection(c, custom.Title.ToUpper(), col =>
                            ComposeCustomSectionItems(col, custom, 10, 8)));
                    }
                    break;
            }
        }
    }

    private void ComposeMainSection(IContainer container, string title, Action<ColumnDescriptor> content)
    {
        container.Column(col =>
        {
            col.Item().Text(title).FontSize(11 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor));
            col.Item().Height(5);
            col.Item().Column(content);
        });
    }
}
