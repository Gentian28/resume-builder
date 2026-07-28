using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Templates.Templates;

public class CreativeTemplate : BaseTemplate
{
    public override TemplateInfo Info => new()
    {
        Id = "creative",
        Name = "Creative",
        Description = "Bold and eye-catching two-column design for creative professionals",
        Category = TemplateCategory.Creative,
        Layout = TemplateLayout.TwoColumn,
        Tags = new[] { "bold", "colorful", "designer" },
        DefaultAccentColor = "#7c3aed",
        DefaultFontFamily = "Arial"
    };

    // Sections rendered in the accent sidebar. PersonalInfo also feeds the main header.
    private static readonly SectionType[] SidebarSections =
    {
        SectionType.PersonalInfo,
        SectionType.Skills,
        SectionType.Languages
    };

    protected override void ComposeContent(IContainer container, Resume resume)
    {
        container.Row(row =>
        {
            // Left sidebar
            row.ConstantItem(180).Background(ParseColor(AccentColor)).Padding(20).Column(sidebar =>
            {
                ComposeSidebar(sidebar, resume);
            });

            // Main content
            row.RelativeItem().PaddingLeft(25).Column(main =>
            {
                main.Spacing(SectionSpacing);
                ComposeMainContent(main, resume);
            });
        });
    }

    private void ComposeSidebar(ColumnDescriptor sidebar, Resume resume)
    {
        sidebar.Spacing(SectionSpacing);

        // Photo or initials
        sidebar.Item().AlignCenter().Element(c => ComposePhotoOrInitials(c, resume, 80, Colors.White, ParseColor(AccentColor)));

        foreach (var sectionType in GetOrderedSections())
        {
            if (!SidebarSections.Contains(sectionType) || !ShouldRenderSection(sectionType, resume))
                continue;

            switch (sectionType)
            {
                case SectionType.PersonalInfo:
                    sidebar.Item().Column(col =>
                    {
                        col.Spacing(6);
                        col.Item().Text("CONTACT").FontSize(10 * FontSizeScale).Bold().FontColor(Colors.White).LetterSpacing(0.1f);
                        col.Item().Height(5);

                        var info = resume.PersonalInfo;
                        if (!string.IsNullOrWhiteSpace(info.Email))
                            col.Item().Text(info.Email).FontSize(8 * FontSizeScale).FontColor(Colors.White);
                        if (!string.IsNullOrWhiteSpace(info.Phone))
                            col.Item().Text(info.Phone).FontSize(8 * FontSizeScale).FontColor(Colors.White);
                        if (!string.IsNullOrWhiteSpace(info.Location))
                            col.Item().Text(info.Location).FontSize(8 * FontSizeScale).FontColor(Colors.White);
                        if (!string.IsNullOrWhiteSpace(info.Website))
                            col.Item().Text(FormatWebsiteDisplay(info.Website)).FontSize(8 * FontSizeScale).FontColor(Colors.White);
                        if (!string.IsNullOrWhiteSpace(info.LinkedIn))
                            col.Item().Text(FormatLinkedInDisplay(info.LinkedIn)).FontSize(8 * FontSizeScale).FontColor(Colors.White);
                        if (!string.IsNullOrWhiteSpace(info.GitHub))
                            col.Item().Text($"github.com/{FormatGitHubDisplay(info.GitHub)}").FontSize(8 * FontSizeScale).FontColor(Colors.White);
                    });
                    break;

                case SectionType.Skills:
                    sidebar.Item().Column(col =>
                    {
                        col.Spacing(4);
                        col.Item().Text("SKILLS").FontSize(10 * FontSizeScale).Bold().FontColor(Colors.White).LetterSpacing(0.1f);
                        col.Item().Height(5);

                        foreach (var skill in resume.Skills.OrderBy(s => s.Order))
                        {
                            col.Item().Row(skillRow =>
                            {
                                skillRow.RelativeItem().Text(skill.Name).FontSize(8 * FontSizeScale).FontColor(Colors.White);
                                skillRow.ConstantItem(50).AlignMiddle().Element(bar =>
                                    ComposeSkillBar(bar, skill.Level, Colors.White, Colors.White.WithAlpha(0.3f)));
                            });
                        }
                    });
                    break;

                case SectionType.Languages:
                    sidebar.Item().Column(col =>
                    {
                        col.Spacing(4);
                        col.Item().Text("LANGUAGES").FontSize(10 * FontSizeScale).Bold().FontColor(Colors.White).LetterSpacing(0.1f);
                        col.Item().Height(5);

                        foreach (var lang in resume.Languages.OrderBy(l => l.Order))
                        {
                            col.Item().Text(FormatLanguage(lang))
                                .FontSize(8 * FontSizeScale).FontColor(Colors.White);
                        }
                    });
                    break;
            }
        }
    }

    private void ComposeMainContent(ColumnDescriptor main, Resume resume)
    {
        foreach (var sectionType in GetOrderedSections())
        {
            if (!ShouldRenderSection(sectionType, resume))
                continue;

            // Skills and Languages live only in the sidebar; PersonalInfo also heads this column.
            if (sectionType is SectionType.Skills or SectionType.Languages)
                continue;

            switch (sectionType)
            {
                case SectionType.PersonalInfo:
                    main.Item().Column(header =>
                    {
                        header.Item().Text(resume.PersonalInfo.FullName.ToUpper())
                            .FontSize(28 * FontSizeScale).Bold().FontFamily(HeadingFontFamily)
                            .FontColor(ParseColor(AccentColor)).LetterSpacing(0.05f);

                        if (!string.IsNullOrWhiteSpace(resume.PersonalInfo.JobTitle))
                        {
                            header.Item().Text(resume.PersonalInfo.JobTitle.ToUpper())
                                .FontSize(12 * FontSizeScale).FontColor(Colors.Grey.Darken1).LetterSpacing(0.1f);
                        }
                    });
                    break;

                case SectionType.Summary:
                    main.Item().Element(c => ComposeSection(c, "ABOUT ME", col =>
                    {
                        col.Item().Text(resume.Summary).FontSize(9 * FontSizeScale).LineHeight(LineSpacing);
                    }));
                    break;

                case SectionType.Experience:
                    main.Item().Element(c => ComposeSection(c, "EXPERIENCE", col =>
                    {
                        col.Spacing(10);
                        foreach (var exp in resume.Experiences.OrderBy(e => e.Order))
                        {
                            col.Item().Column(expCol =>
                            {
                                expCol.Item().Row(r =>
                                {
                                    r.RelativeItem().Text(exp.JobTitle).Bold().FontSize(10 * FontSizeScale);
                                    r.AutoItem().Text(exp.DateRange).FontSize(8 * FontSizeScale).FontColor(Colors.Grey.Darken1);
                                });
                                expCol.Item().Text(exp.Company).FontSize(9 * FontSizeScale).FontColor(ParseColor(AccentColor));

                                if (!string.IsNullOrWhiteSpace(exp.Description))
                                    expCol.Item().PaddingTop(3).Text(exp.Description).FontSize(8 * FontSizeScale).LineHeight(LineSpacing);

                                if (exp.Achievements.Any())
                                {
                                    expCol.Item().PaddingTop(3).Column(achCol =>
                                    {
                                        foreach (var ach in exp.Achievements)
                                            achCol.Item().Text($"→ {ach}").FontSize(8 * FontSizeScale).LineHeight(LineSpacing);
                                    });
                                }
                            });
                        }
                    }));
                    break;

                case SectionType.Education:
                    main.Item().Element(c => ComposeSection(c, "EDUCATION", col =>
                    {
                        col.Spacing(8);
                        foreach (var edu in resume.EducationList.OrderBy(e => e.Order))
                        {
                            col.Item().Column(eduCol =>
                            {
                                eduCol.Item().Row(r =>
                                {
                                    r.RelativeItem().Text(edu.DegreeWithField).Bold().FontSize(10 * FontSizeScale);
                                    r.AutoItem().Text(edu.DateRange).FontSize(8 * FontSizeScale).FontColor(Colors.Grey.Darken1);
                                });
                                eduCol.Item().Text(edu.Institution).FontSize(9 * FontSizeScale).FontColor(ParseColor(AccentColor));

                                if (!string.IsNullOrWhiteSpace(edu.Description))
                                    eduCol.Item().PaddingTop(2).Text(edu.Description).FontSize(8 * FontSizeScale).LineHeight(LineSpacing);
                            });
                        }
                    }));
                    break;

                case SectionType.Projects:
                    main.Item().Element(c => ComposeSection(c, "PROJECTS", col =>
                    {
                        col.Spacing(8);
                        foreach (var proj in resume.Projects.OrderBy(p => p.Order))
                        {
                            col.Item().Column(projCol =>
                            {
                                projCol.Item().Text(proj.Name).Bold().FontSize(10 * FontSizeScale);
                                if (!string.IsNullOrWhiteSpace(proj.Description))
                                    projCol.Item().Text(proj.Description).FontSize(8 * FontSizeScale).LineHeight(LineSpacing);
                                if (!string.IsNullOrWhiteSpace(proj.Url))
                                    projCol.Item().Text(proj.Url).FontSize(8 * FontSizeScale).FontColor(Colors.Grey.Darken1);
                                if (proj.Highlights.Any())
                                {
                                    projCol.Item().PaddingTop(2).Column(hlCol =>
                                    {
                                        foreach (var hl in proj.Highlights)
                                            hlCol.Item().Text($"→ {hl}").FontSize(8 * FontSizeScale).LineHeight(LineSpacing);
                                    });
                                }
                                if (proj.Technologies.Any())
                                    projCol.Item().Text($"Tech: {string.Join(", ", proj.Technologies)}").FontSize(8 * FontSizeScale).FontColor(Colors.Grey.Darken1);
                            });
                        }
                    }));
                    break;

                case SectionType.Certifications:
                    main.Item().Element(c => ComposeSection(c, "CERTIFICATIONS", col =>
                    {
                        col.Spacing(8);
                        foreach (var cert in resume.Certifications.OrderBy(c => c.Order))
                        {
                            col.Item().Column(certCol =>
                            {
                                certCol.Item().Row(r =>
                                {
                                    r.RelativeItem().Text(cert.Name).Bold().FontSize(10 * FontSizeScale);
                                    if (cert.IssueDate.HasValue)
                                        r.AutoItem().Text(ResumeDateFormat.MonthYear(cert.IssueDate)).FontSize(8 * FontSizeScale).FontColor(Colors.Grey.Darken1);
                                });
                                if (!string.IsNullOrWhiteSpace(cert.IssuingOrganization))
                                    certCol.Item().Text(cert.IssuingOrganization).FontSize(9 * FontSizeScale).FontColor(ParseColor(AccentColor));
                            });
                        }
                    }));
                    break;

                case SectionType.CustomSections:
                    foreach (var custom in GetVisibleCustomSections(resume))
                    {
                        main.Item().Element(c => ComposeSection(c, custom.Title.ToUpper(), col =>
                            ComposeCustomSectionItems(col, custom, 10, 8)));
                    }
                    break;
            }
        }
    }

    private void ComposeSection(IContainer container, string title, Action<ColumnDescriptor> content)
    {
        container.Column(col =>
        {
            col.Item().Text(title).FontSize(11 * FontSizeScale).Bold().FontFamily(HeadingFontFamily).FontColor(ParseColor(AccentColor));
            col.Item().Height(5);
            col.Item().Column(content);
        });
    }
}
