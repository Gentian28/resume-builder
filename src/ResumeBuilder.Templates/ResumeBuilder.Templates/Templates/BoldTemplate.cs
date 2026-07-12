using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Templates.Templates;

public class BoldTemplate : BaseTemplate
{
    public override TemplateInfo Info => new()
    {
        Id = "bold",
        Name = "Bold",
        Description = "Magazine-style layout with large hero typography and bold accent bars",
        Category = TemplateCategory.Creative,
        Layout = TemplateLayout.SingleColumn,
        Tags = new[] { "magazine", "bold", "hero", "creative" },
        DefaultAccentColor = "#dc2626",
        DefaultFontFamily = "Arial"
    };

    protected override void ConfigurePage(PageDescriptor page)
    {
        page.Size(PageSizes.A4);
        page.Margin(35);
        page.DefaultTextStyle(x => x
            .FontSize(10 * FontSizeScale)
            .FontFamily(FontFamily)
            .FontColor(ParseColor(TextColor)));
    }

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
                        column.Item().Element(c => ComposeSection(c, "SUMMARY", ct =>
                        {
                            ct.Item().Text(resume.Summary).FontSize(10 * FontSizeScale).LineHeight(LineSpacing);
                        }));
                        break;

                    case SectionType.Experience:
                        column.Item().Element(c => ComposeSection(c, "EXPERIENCE", ct =>
                        {
                            ct.Spacing(12);
                            foreach (var exp in resume.Experiences.OrderBy(e => e.Order))
                                ct.Item().Element(e => ComposeExperience(e, exp));
                        }));
                        break;

                    case SectionType.Education:
                        column.Item().Element(c => ComposeSection(c, "EDUCATION", ct =>
                        {
                            ct.Spacing(10);
                            foreach (var edu in resume.EducationList.OrderBy(e => e.Order))
                                ct.Item().Element(e => ComposeEducation(e, edu));
                        }));
                        break;

                    case SectionType.Skills:
                        column.Item().Element(c => ComposeSection(c, "SKILLS", ct =>
                        {
                            ct.Item().Element(e => ComposeSkillPills(e, resume.Skills));
                        }));
                        break;

                    case SectionType.Languages:
                        column.Item().Element(c => ComposeSection(c, "LANGUAGES", ct =>
                        {
                            ct.Item().Row(row =>
                            {
                                foreach (var lang in resume.Languages.OrderBy(l => l.Order))
                                {
                                    row.AutoItem().PaddingRight(20).Text(text =>
                                    {
                                        text.Span(lang.Name).Bold().FontSize(10 * FontSizeScale);
                                        text.Span($" — {GetLanguageProficiencyText(lang.Proficiency)}")
                                            .FontSize(9 * FontSizeScale).FontColor(Colors.Grey.Darken1);
                                    });
                                }
                            });
                        }));
                        break;

                    case SectionType.Certifications:
                        column.Item().Element(c => ComposeSection(c, "CERTIFICATIONS", ct =>
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
                        column.Item().Element(c => ComposeSection(c, "PROJECTS", ct =>
                        {
                            ct.Spacing(10);
                            foreach (var project in resume.Projects.OrderBy(p => p.Order))
                                ct.Item().Element(e => ComposeProject(e, project));
                        }));
                        break;

                    case SectionType.CustomSections:
                        foreach (var custom in GetVisibleCustomSections(resume))
                        {
                            column.Item().Element(c => ComposeSection(c, custom.Title.ToUpper(), ct =>
                                ComposeCustomSectionItems(ct, custom, 11, 9)));
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
            column.Item().Row(row =>
            {
                // Photo on the left (if available)
                if (info.Photo != null && info.Photo.Length > 0)
                {
                    row.ConstantItem(75).Element(c =>
                        ComposePhotoOrInitials(c, resume, 75, ParseColor(AccentColor), Colors.White));
                    row.ConstantItem(15);
                }

                row.RelativeItem().AlignBottom().Column(nameCol =>
                {
                    nameCol.Item().Text(info.FullName)
                        .FontSize(38 * FontSizeScale)
                        .Bold()
                        .FontFamily(HeadingFontFamily)
                        .FontColor(ParseColor(AccentColor));

                    if (!string.IsNullOrWhiteSpace(info.JobTitle))
                    {
                        nameCol.Item().Text(info.JobTitle.ToUpper())
                            .FontSize(12 * FontSizeScale)
                            .LetterSpacing(0.15f)
                            .FontColor(Colors.Grey.Darken2);
                    }
                });
            });

            column.Item().Height(10);

            var contacts = new List<string>();
            if (!string.IsNullOrWhiteSpace(info.Email)) contacts.Add(info.Email);
            if (!string.IsNullOrWhiteSpace(info.Phone)) contacts.Add(info.Phone);
            if (!string.IsNullOrWhiteSpace(info.Location)) contacts.Add(info.Location);
            if (!string.IsNullOrWhiteSpace(info.LinkedIn)) contacts.Add(FormatLinkedInDisplay(info.LinkedIn));
            if (!string.IsNullOrWhiteSpace(info.GitHub)) contacts.Add($"github.com/{FormatGitHubDisplay(info.GitHub)}");
            if (!string.IsNullOrWhiteSpace(info.Website)) contacts.Add(info.Website);

            column.Item().Text(string.Join("  |  ", contacts))
                .FontSize(9 * FontSizeScale)
                .FontColor(Colors.Grey.Darken1);

            column.Item().Height(6);
            column.Item().LineHorizontal(3).LineColor(ParseColor(AccentColor));
        });
    }

    private void ComposeSection(IContainer container, string title, Action<ColumnDescriptor> content)
    {
        container.Column(column =>
        {
            // Full-width accent background bar for section title
            column.Item().Background(ParseColor(AccentColor)).Padding(6).PaddingLeft(10)
                .Text(title)
                .FontSize(11 * FontSizeScale)
                .Bold()
                .FontColor(Colors.White)
                .LetterSpacing(0.1f);

            column.Item().Height(10);
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
                row.AutoItem().Text(exp.DateRange).FontSize(9 * FontSizeScale).FontColor(Colors.Grey.Darken1);
            });

            column.Item().Text(text =>
            {
                text.Span(exp.Company).SemiBold().FontSize(10 * FontSizeScale).FontColor(ParseColor(AccentColor));
                if (!string.IsNullOrWhiteSpace(exp.Location))
                    text.Span($"  |  {exp.Location}").FontSize(9 * FontSizeScale).FontColor(Colors.Grey.Darken1);
            });

            if (!string.IsNullOrWhiteSpace(exp.Description))
                column.Item().PaddingTop(4).Text(exp.Description).FontSize(9 * FontSizeScale).LineHeight(LineSpacing);

            if (exp.Achievements.Any())
            {
                column.Item().PaddingTop(4).Column(achCol =>
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
                row.AutoItem().Text(edu.DateRange).FontSize(9 * FontSizeScale).FontColor(Colors.Grey.Darken1);
            });

            column.Item().Text(edu.Institution).SemiBold().FontSize(10 * FontSizeScale).FontColor(ParseColor(AccentColor));

            if (!string.IsNullOrWhiteSpace(edu.Grade))
                column.Item().Text($"Grade: {edu.Grade}").FontSize(9 * FontSizeScale).FontColor(Colors.Grey.Darken1);

            if (!string.IsNullOrWhiteSpace(edu.Description))
                column.Item().PaddingTop(2).Text(edu.Description).FontSize(9 * FontSizeScale).LineHeight(LineSpacing);
        });
    }

    private void ComposeSkillPills(IContainer container, List<Skill> skills)
    {
        var accent = ParseColor(AccentColor);
        var lightAccent = accent.WithAlpha(0.1f);
        var orderedSkills = skills.OrderBy(s => s.Order).ToList();

        container.Column(column =>
        {
            // Batch into rows of 5
            for (var i = 0; i < orderedSkills.Count; i += 5)
            {
                var batch = orderedSkills.Skip(i).Take(5).ToList();
                column.Item().PaddingBottom(5).Row(row =>
                {
                    foreach (var skill in batch)
                    {
                        row.AutoItem().PaddingRight(6)
                            .Border(1).BorderColor(accent)
                            .Background(lightAccent)
                            .Padding(4).PaddingLeft(8).PaddingRight(8)
                            .Text(skill.Name).FontSize(8 * FontSizeScale);
                    }
                });
            }
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
                        .FontSize(9 * FontSizeScale).FontColor(Colors.Grey.Darken1);
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
