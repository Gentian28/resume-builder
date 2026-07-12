using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Templates.Templates;

public class TechnicalTemplate : BaseTemplate
{
    public override TemplateInfo Info => new()
    {
        Id = "technical",
        Name = "Technical",
        Description = "Code-inspired design with skill badges for developers and engineers",
        Category = TemplateCategory.Technical,
        Layout = TemplateLayout.TwoColumn,
        Tags = new[] { "developer", "engineering", "tech" },
        DefaultAccentColor = "#059669",
        DefaultFontFamily = "Consolas"
    };

    // Sections rendered in the left sidebar; everything else goes in the main column.
    // PersonalInfo appears in both: contact details in the sidebar, name and title in the main header.
    private static readonly SectionType[] SidebarSections =
    {
        SectionType.PersonalInfo,
        SectionType.Skills,
        SectionType.Languages,
        SectionType.Certifications
    };

    private static readonly SectionType[] MainOnlySections = { SectionType.PersonalInfo };

    protected override void ComposeContent(IContainer container, Resume resume)
    {
        container.Row(row =>
        {
            // Sidebar
            row.ConstantItem(170).Background(Colors.Grey.Lighten4).Padding(15).Column(sidebar =>
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
        sidebar.Spacing(15);

        foreach (var sectionType in GetOrderedSections())
        {
            if (!SidebarSections.Contains(sectionType) || !ShouldRenderSection(sectionType, resume))
                continue;

            switch (sectionType)
            {
                case SectionType.PersonalInfo:
                    sidebar.Item().Element(c => ComposeContact(c, resume.PersonalInfo));
                    break;

                case SectionType.Skills:
                    sidebar.Item().Element(c => ComposeSkillBadges(c, resume.Skills));
                    break;

                case SectionType.Languages:
                    sidebar.Item().Column(col =>
                    {
                        col.Item().Text("// LANGUAGES").FontSize(9 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor));
                        col.Item().Height(6);

                        foreach (var lang in resume.Languages.OrderBy(l => l.Order))
                        {
                            col.Item().Text(FormatLanguage(lang, ": ")).FontSize(8 * FontSizeScale);
                        }
                    });
                    break;

                case SectionType.Certifications:
                    sidebar.Item().Column(col =>
                    {
                        col.Item().Text("// CERTIFICATIONS").FontSize(9 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor));
                        col.Item().Height(6);

                        foreach (var cert in resume.Certifications.OrderBy(c => c.Order))
                        {
                            col.Item().PaddingBottom(3).Column(certCol =>
                            {
                                certCol.Item().Text(cert.Name).FontSize(8 * FontSizeScale).Bold();
                                certCol.Item().Text(cert.IssuingOrganization).FontSize(7 * FontSizeScale).FontColor(Colors.Grey.Darken1);
                            });
                        }
                    });
                    break;
            }
        }
    }

    private void ComposeContact(IContainer container, PersonalInfo info)
    {
        container.Column(col =>
        {
            col.Item().Text("// CONTACT").FontSize(9 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor));
            col.Item().Height(6);

            if (!string.IsNullOrWhiteSpace(info.Email))
                col.Item().Text(info.Email).FontSize(8 * FontSizeScale);
            if (!string.IsNullOrWhiteSpace(info.Phone))
                col.Item().Text(info.Phone).FontSize(8 * FontSizeScale);
            if (!string.IsNullOrWhiteSpace(info.Location))
                col.Item().Text(info.Location).FontSize(8 * FontSizeScale);
            if (!string.IsNullOrWhiteSpace(info.GitHub))
                col.Item().Text($"github.com/{FormatGitHubDisplay(info.GitHub)}").FontSize(8 * FontSizeScale);
            if (!string.IsNullOrWhiteSpace(info.LinkedIn))
                col.Item().Text(FormatLinkedInDisplay(info.LinkedIn)).FontSize(8 * FontSizeScale);
            if (!string.IsNullOrWhiteSpace(info.Website))
                col.Item().Text(info.Website).FontSize(8 * FontSizeScale);
        });
    }

    private void ComposeSkillBadges(IContainer container, List<Skill> skills)
    {
        container.Column(col =>
        {
            col.Item().Text("// TECH STACK").FontSize(9 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor));
            col.Item().Height(6);

            foreach (var group in skills.GroupBy(s => s.Category))
            {
                if (!string.IsNullOrWhiteSpace(group.Key))
                {
                    col.Item().PaddingTop(4).Text(group.Key).FontSize(8 * FontSizeScale).Bold();
                }

                col.Item().PaddingTop(2).Column(badgeCol =>
                {
                    foreach (var skill in group.OrderBy(s => s.Order))
                    {
                        badgeCol.Item().PaddingBottom(3)
                            .Background(ParseColor(AccentColor))
                            .Padding(3)
                            .Text(skill.Name)
                            .FontSize(7 * FontSizeScale)
                            .FontColor(Colors.White);
                    }
                });
            }
        });
    }

    private void ComposeMainContent(ColumnDescriptor main, Resume resume)
    {
        foreach (var sectionType in GetOrderedSections())
        {
            if (!ShouldRenderSection(sectionType, resume))
                continue;

            if (SidebarSections.Contains(sectionType) && !MainOnlySections.Contains(sectionType))
                continue;

            switch (sectionType)
            {
                case SectionType.PersonalInfo:
                    main.Item().Column(header => ComposeHeaderText(header, resume.PersonalInfo));
                    break;

                case SectionType.Summary:
                    main.Item().Column(col =>
                    {
                        col.Item().Element(c => ComposeSectionTitle(c, "About"));
                        col.Item().Height(4);
                        col.Item().Text(resume.Summary).FontSize(9 * FontSizeScale).LineHeight(LineSpacing);
                    });
                    break;

                case SectionType.Experience:
                    main.Item().Column(col =>
                    {
                        col.Spacing(12);
                        col.Item().Element(c => ComposeSectionTitle(c, "Experience"));

                        foreach (var exp in resume.Experiences.OrderBy(e => e.Order))
                        {
                            col.Item().Element(c => ComposeExperience(c, exp));
                        }
                    });
                    break;

                case SectionType.Projects:
                    main.Item().Column(col =>
                    {
                        col.Spacing(10);
                        col.Item().Element(c => ComposeSectionTitle(c, "Projects"));

                        foreach (var proj in resume.Projects.OrderBy(p => p.Order))
                        {
                            col.Item().Element(c => ComposeProject(c, proj));
                        }
                    });
                    break;

                case SectionType.Education:
                    main.Item().Column(col =>
                    {
                        col.Spacing(8);
                        col.Item().Element(c => ComposeSectionTitle(c, "Education"));

                        foreach (var edu in resume.EducationList.OrderBy(e => e.Order))
                        {
                            col.Item().Row(r =>
                            {
                                r.RelativeItem().Column(eduCol =>
                                {
                                    eduCol.Item().Text(edu.DegreeWithField).Bold().FontSize(10 * FontSizeScale);
                                    eduCol.Item().Text(edu.Institution).FontSize(9 * FontSizeScale);
                                    if (!string.IsNullOrWhiteSpace(edu.Description))
                                        eduCol.Item().PaddingTop(2).Text(edu.Description).FontSize(8 * FontSizeScale).LineHeight(LineSpacing);
                                });
                                r.AutoItem().Text(edu.DateRange).FontSize(8 * FontSizeScale).FontColor(Colors.Grey.Darken1);
                            });
                        }
                    });
                    break;

                case SectionType.CustomSections:
                    foreach (var custom in GetVisibleCustomSections(resume))
                    {
                        main.Item().Column(col =>
                        {
                            col.Item().Element(c => ComposeSectionTitle(c, custom.Title));
                            col.Item().Height(4);
                            col.Item().Column(ct => ComposeCustomSectionItems(ct, custom));
                        });
                    }
                    break;
            }
        }
    }

    private void ComposeSectionTitle(IContainer container, string title)
    {
        container.Text($"/* {title} */").FontSize(10 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor));
    }

    private void ComposeHeaderText(ColumnDescriptor header, PersonalInfo info)
    {
        header.Item().Text(info.FullName)
            .FontSize(24 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor));

        if (!string.IsNullOrWhiteSpace(info.JobTitle))
        {
            header.Item().Text($"<{info.JobTitle} />")
                .FontSize(12 * FontSizeScale).FontColor(Colors.Grey.Darken2);
        }
    }

    private void ComposeExperience(IContainer container, Experience exp)
    {
        container.Column(expCol =>
        {
            expCol.Item().Row(r =>
            {
                r.RelativeItem().Text(exp.JobTitle).Bold().FontSize(11 * FontSizeScale);
                r.AutoItem().Text(exp.DateRange).FontSize(8 * FontSizeScale).FontColor(Colors.Grey.Darken1);
            });

            expCol.Item().Text($"@{exp.Company}").FontSize(9 * FontSizeScale).FontColor(ParseColor(AccentColor));

            if (!string.IsNullOrWhiteSpace(exp.Description))
                expCol.Item().PaddingTop(4).Text(exp.Description).FontSize(9 * FontSizeScale).LineHeight(LineSpacing);

            if (exp.Achievements.Any())
            {
                expCol.Item().PaddingTop(4).Column(achCol =>
                {
                    foreach (var ach in exp.Achievements)
                        achCol.Item().Text($"→ {ach}").FontSize(8 * FontSizeScale);
                });
            }
        });
    }

    private void ComposeProject(IContainer container, Project proj)
    {
        container.Column(projCol =>
        {
            projCol.Item().Text(proj.Name).Bold().FontSize(10 * FontSizeScale);

            if (!string.IsNullOrWhiteSpace(proj.Description))
                projCol.Item().Text(proj.Description).FontSize(8 * FontSizeScale).LineHeight(LineSpacing);

            if (!string.IsNullOrWhiteSpace(proj.Url))
                projCol.Item().Text(proj.Url).FontSize(7 * FontSizeScale).FontColor(Colors.Grey.Darken1);

            if (proj.Highlights.Any())
            {
                projCol.Item().PaddingTop(2).Column(hlCol =>
                {
                    foreach (var hl in proj.Highlights)
                        hlCol.Item().Text($"→ {hl}").FontSize(8 * FontSizeScale);
                });
            }

            if (proj.Technologies.Any())
            {
                projCol.Item().PaddingTop(3).Row(techRow =>
                {
                    techRow.Spacing(4);
                    foreach (var tech in proj.Technologies)
                    {
                        techRow.AutoItem()
                            .Border(1).BorderColor(ParseColor(AccentColor))
                            .Padding(2)
                            .Text(tech).FontSize(7 * FontSizeScale).FontColor(ParseColor(AccentColor));
                    }
                });
            }
        });
    }
}
