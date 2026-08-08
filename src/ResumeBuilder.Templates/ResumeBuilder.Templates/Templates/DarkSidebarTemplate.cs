using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Templates.Templates;

public class DarkSidebarTemplate : BaseTemplate
{
    public override TemplateInfo Info => new()
    {
        Id = "dark-sidebar",
        Name = "Dark Sidebar",
        Description = "Striking dark sidebar with light main content for a modern creative look",
        Category = TemplateCategory.Creative,
        Layout = TemplateLayout.Sidebar,
        Tags = new[] { "dark", "sidebar", "creative", "contrast" },
        DefaultAccentColor = "#8b5cf6",
        DefaultFontFamily = "Arial"
    };

    private static readonly Color DarkBg = Color.FromRGB(30, 30, 35); // #1e1e23

    protected override void ConfigurePage(PageDescriptor page)
    {
        page.Size(PageSizes.A4);
        page.Margin(0); // Full-bleed, padding handled per column
        page.DefaultTextStyle(x => x
            .FontSize(10 * FontSizeScale)
            .FontFamily(FontFamily)
            .FontColor(ParseColor(TextColor)));
    }

    protected override void ComposeContent(IContainer container, Resume resume)
    {
        container.Row(row =>
        {
            // Dark sidebar
            row.ConstantItem(190).Background(DarkBg).Padding(20).Column(sidebar =>
            {
                ComposeSidebar(sidebar, resume);
            });

            // Main content
            row.RelativeItem().Padding(25).Column(main =>
            {
                main.Spacing(SectionSpacing);
                ComposeMainContent(main, resume);
            });
        });
    }

    private void ComposeSidebar(ColumnDescriptor sidebar, Resume resume)
    {
        sidebar.Spacing(SectionSpacing);
        var accent = ParseColor(AccentColor);

        // Photo or initials
        sidebar.Item().AlignCenter().Element(c =>
            ComposePhotoOrInitials(c, resume, 80, accent, Colors.White));

        // Name and job title
        sidebar.Item().Column(col =>
        {
            col.Item().AlignCenter().Text(resume.PersonalInfo.FirstName)
                .FontSize(18 * FontSizeScale).Bold().FontColor(Colors.White);
            col.Item().AlignCenter().Text(resume.PersonalInfo.LastName)
                .FontSize(18 * FontSizeScale).Bold().FontColor(Colors.White);

            if (!string.IsNullOrWhiteSpace(resume.PersonalInfo.JobTitle))
            {
                col.Item().Height(4);
                col.Item().AlignCenter().Text(resume.PersonalInfo.JobTitle)
                    .FontSize(9 * FontSizeScale).FontColor(accent);
            }
        });

        // Render sidebar sections based on order
        foreach (var sectionType in GetOrderedSections())
        {
            if (!ShouldRenderSection(sectionType, resume))
                continue;

            switch (sectionType)
            {
                case SectionType.PersonalInfo:
                    sidebar.Item().Element(c => ComposeSidebarContact(c, resume, accent));
                    break;
                case SectionType.Skills:
                    sidebar.Item().Element(c => ComposeSidebarSkills(c, resume, accent));
                    break;
                case SectionType.Languages:
                    sidebar.Item().Element(c => ComposeSidebarLanguages(c, resume, accent));
                    break;
            }
        }
    }

    private void ComposeSidebarContact(IContainer container, Resume resume, Color accent)
    {
        var info = resume.PersonalInfo;
        container.Column(col =>
        {
            col.Item().Text("CONTACT").FontSize(10 * FontSizeScale).Bold().FontColor(accent).LetterSpacing(0.1f);
            col.Item().Height(5);
            col.Item().LineHorizontal(1).LineColor(accent.WithAlpha(0.4f));
            col.Item().Height(5);

            if (!string.IsNullOrWhiteSpace(info.Email))
                col.Item().PaddingBottom(3).Text(info.Email).FontSize(8 * FontSizeScale).FontColor(Colors.White);
            if (!string.IsNullOrWhiteSpace(info.Phone))
                col.Item().PaddingBottom(3).Text(info.Phone).FontSize(8 * FontSizeScale).FontColor(Colors.White);
            if (!string.IsNullOrWhiteSpace(info.Location))
                col.Item().PaddingBottom(3).Text(info.Location).FontSize(8 * FontSizeScale).FontColor(Colors.White);
            if (!string.IsNullOrWhiteSpace(info.Website))
                col.Item().PaddingBottom(3).Text(FormatWebsiteDisplay(info.Website)).FontSize(8 * FontSizeScale).FontColor(Colors.White);
            if (!string.IsNullOrWhiteSpace(info.LinkedIn))
                col.Item().PaddingBottom(3).Text(FormatLinkedInDisplay(info.LinkedIn)).FontSize(8 * FontSizeScale).FontColor(Colors.White);
            if (!string.IsNullOrWhiteSpace(info.GitHub))
                col.Item().PaddingBottom(3).Text($"github.com/{FormatGitHubDisplay(info.GitHub)}").FontSize(8 * FontSizeScale).FontColor(Colors.White);
        });
    }

    private void ComposeSidebarSkills(IContainer container, Resume resume, Color accent)
    {
        container.Column(col =>
        {
            col.Item().Text("SKILLS").FontSize(10 * FontSizeScale).Bold().FontColor(accent).LetterSpacing(0.1f);
            col.Item().Height(5);
            col.Item().LineHorizontal(1).LineColor(accent.WithAlpha(0.4f));
            col.Item().Height(5);

            foreach (var skill in resume.Skills.OrderBy(s => s.Order))
            {
                col.Item().PaddingBottom(4).Row(row =>
                {
                    row.RelativeItem().Text(skill.Name).FontSize(8 * FontSizeScale).FontColor(Colors.White);
                    row.AutoItem().Text(GetDotIndicator((int)skill.Level))
                        .FontSize(8 * FontSizeScale).FontColor(accent);
                });
            }
        });
    }

    private void ComposeSidebarLanguages(IContainer container, Resume resume, Color accent)
    {
        container.Column(col =>
        {
            col.Item().Text("LANGUAGES").FontSize(10 * FontSizeScale).Bold().FontColor(accent).LetterSpacing(0.1f);
            col.Item().Height(5);
            col.Item().LineHorizontal(1).LineColor(accent.WithAlpha(0.4f));
            col.Item().Height(5);

            foreach (var lang in resume.Languages.OrderBy(l => l.Order))
            {
                col.Item().PaddingBottom(3).Text(text =>
                {
                    text.Span(lang.Name).FontSize(8 * FontSizeScale).FontColor(Colors.White);
                    text.Span($" - {GetLanguageProficiencyText(lang.Proficiency)}")
                        .FontSize(7 * FontSizeScale).FontColor(Colors.White.WithAlpha(0.7f));
                });
            }
        });
    }

    private static string GetDotIndicator(int level)
    {
        // level 0-5 mapped to filled/empty dots out of 5
        var filled = Math.Clamp(level, 0, 5);
        var empty = 5 - filled;
        return new string('●', filled) + new string('○', empty);
    }

    private void ComposeMainContent(ColumnDescriptor main, Resume resume)
    {
        foreach (var sectionType in GetOrderedSections())
        {
            if (!ShouldRenderSection(sectionType, resume))
                continue;

            // Skip sections routed to sidebar
            if (sectionType is SectionType.PersonalInfo or SectionType.Skills or SectionType.Languages)
                continue;

            switch (sectionType)
            {
                case SectionType.Summary:
                    main.Item().Element(c => ComposeMainSection(c, "SUMMARY", ct =>
                    {
                        ct.Item().Text(resume.Summary).FontSize(9 * FontSizeScale).LineHeight(LineSpacing);
                    }));
                    break;

                case SectionType.Experience:
                    main.Item().Element(c => ComposeMainSection(c, "EXPERIENCE", ct =>
                    {
                        ct.Spacing(10);
                        foreach (var exp in resume.Experiences.OrderBy(e => e.Order))
                            ct.Item().Element(e => ComposeExperience(e, exp));
                    }));
                    break;

                case SectionType.Education:
                    main.Item().Element(c => ComposeMainSection(c, "EDUCATION", ct =>
                    {
                        ct.Spacing(8);
                        foreach (var edu in resume.EducationList.OrderBy(e => e.Order))
                            ct.Item().Element(e => ComposeEducation(e, edu));
                    }));
                    break;

                case SectionType.Certifications:
                    main.Item().Element(c => ComposeMainSection(c, "CERTIFICATIONS", ct =>
                    {
                        ct.Spacing(6);
                        foreach (var cert in resume.Certifications.OrderBy(c => c.Order))
                        {
                            ct.Item().Text(text =>
                            {
                                text.Span(cert.Name).Bold().FontSize(10 * FontSizeScale);
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
                    main.Item().Element(c => ComposeMainSection(c, "PROJECTS", ct =>
                    {
                        ct.Spacing(10);
                        foreach (var project in resume.Projects.OrderBy(p => p.Order))
                            ct.Item().Element(e => ComposeProject(e, project));
                    }));
                    break;

                case SectionType.CustomSections:
                    foreach (var custom in GetVisibleCustomSections(resume))
                    {
                        main.Item().Element(c => ComposeMainSection(c, custom.Title.ToUpper(), ct =>
                            ComposeCustomSectionItems(ct, custom, 10, 9)));
                    }
                    break;
            }
        }
    }

    private void ComposeMainSection(IContainer container, string title, Action<ColumnDescriptor> content)
    {
        var accent = ParseColor(AccentColor);
        container.EnsureSpace(90).Column(column =>
        {
            column.Item().Text(title)
                .FontSize(13 * FontSizeScale)
                .Bold()
                .FontFamily(HeadingFontFamily)
                .FontColor(accent)
                .LetterSpacing(0.05f);

            column.Item().Height(3);
            column.Item().LineHorizontal(1).LineColor(accent.WithAlpha(0.4f));
            column.Item().Height(8);
            column.Item().Column(content);
        });
    }

    private void ComposeExperience(IContainer container, Experience exp)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Text(exp.JobTitle).Bold().FontSize(11 * FontSizeScale);
                row.AutoItem().Text(exp.DateRange).FontSize(8 * FontSizeScale).FontColor(Colors.Grey.Darken1);
            });

            column.Item().Text(text =>
            {
                text.Span(exp.Company).SemiBold().FontSize(9 * FontSizeScale).FontColor(ParseColor(AccentColor));
                if (!string.IsNullOrWhiteSpace(exp.Location))
                    text.Span($"  |  {exp.Location}").FontSize(9 * FontSizeScale).FontColor(Colors.Grey.Darken1);
            });

            if (!string.IsNullOrWhiteSpace(exp.Description))
                column.Item().PaddingTop(3).Text(exp.Description).FontSize(9 * FontSizeScale).LineHeight(LineSpacing);

            if (exp.Achievements.Any())
            {
                column.Item().PaddingTop(3).Column(achCol =>
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
        });
    }

    private void ComposeEducation(IContainer container, Education edu)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Text(edu.DegreeWithField).Bold().FontSize(11 * FontSizeScale);
                row.AutoItem().Text(edu.DateRange).FontSize(8 * FontSizeScale).FontColor(Colors.Grey.Darken1);
            });

            column.Item().Text(edu.Institution).SemiBold().FontSize(9 * FontSizeScale).FontColor(ParseColor(AccentColor));

            if (!string.IsNullOrWhiteSpace(edu.Grade))
                column.Item().Text($"Grade: {edu.Grade}").FontSize(8 * FontSizeScale).FontColor(Colors.Grey.Darken1);
        });
    }

    private void ComposeProject(IContainer container, Project project)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Text(project.Name).Bold().FontSize(11 * FontSizeScale);
                if (project.StartDate.HasValue)
                    row.AutoItem().Text(FormatDateRange(project.StartDate, project.EndDate, project.IsOngoing))
                        .FontSize(8 * FontSizeScale).FontColor(Colors.Grey.Darken1);
            });

            if (!string.IsNullOrWhiteSpace(project.Description))
                column.Item().PaddingTop(2).Text(project.Description).FontSize(9 * FontSizeScale).LineHeight(LineSpacing);

            if (project.Technologies.Any())
            {
                column.Item().PaddingTop(2).Text(text =>
                {
                    text.Span("Technologies: ").Bold().FontSize(9 * FontSizeScale);
                    text.Span(string.Join(", ", project.Technologies)).FontSize(9 * FontSizeScale);
                });
            }

            if (project.Highlights.Any())
            {
                column.Item().PaddingTop(3).Column(hlCol =>
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
        });
    }
}
