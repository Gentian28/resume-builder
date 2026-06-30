using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Templates.Templates;

public class ElegantTemplate : BaseTemplate
{
    public override TemplateInfo Info => new()
    {
        Id = "elegant",
        Name = "Elegant",
        Description = "Refined and sophisticated design with decorative elements",
        Category = TemplateCategory.Classic,
        Layout = TemplateLayout.SingleColumn,
        Tags = new[] { "refined", "sophisticated", "stylish" },
        DefaultAccentColor = "#991b1b",
        DefaultFontFamily = "Georgia"
    };

    protected override void ConfigurePage(PageDescriptor page)
    {
        base.ConfigurePage(page);
        page.Margin(40);
    }

    protected override void ComposeContent(IContainer container, Resume resume)
    {
        container.Column(column =>
        {
            column.Spacing(SectionSpacing);

            // Decorative header
            column.Item().Element(c => ComposeHeader(c, resume.PersonalInfo));

            // Summary
            if (!string.IsNullOrWhiteSpace(resume.Summary))
            {
                column.Item().Column(col =>
                {
                    col.Item().AlignCenter().Text("~ Profile ~").FontSize(11 * FontSizeScale).FontColor(ParseColor(AccentColor)).Italic();
                    col.Item().Height(8);
                    col.Item().AlignCenter().PaddingHorizontal(30).Text(resume.Summary).FontSize(10 * FontSizeScale).LineHeight(LineSpacing).Italic();
                });
            }

            // Experience
            if (resume.Experiences.Any())
            {
                column.Item().Element(c => ComposeSection(c, "Experience", ct =>
                {
                    foreach (var exp in resume.Experiences.OrderBy(e => e.Order))
                    {
                        ct.Item().Element(e => ComposeExperience(e, exp));
                        ct.Item().Height(12);
                    }
                }));
            }

            // Education
            if (resume.EducationList.Any())
            {
                column.Item().Element(c => ComposeSection(c, "Education", ct =>
                {
                    foreach (var edu in resume.EducationList.OrderBy(e => e.Order))
                    {
                        ct.Item().Element(e => ComposeEducation(e, edu));
                        ct.Item().Height(10);
                    }
                }));
            }

            // Skills
            if (resume.Skills.Any())
            {
                column.Item().Element(c => ComposeSection(c, "Expertise", ct =>
                {
                    ct.Item().AlignCenter().Text(string.Join("  ◈  ", resume.Skills.OrderBy(s => s.Order).Select(s => s.Name)))
                        .FontSize(10 * FontSizeScale);
                }));
            }

            // Projects
            if (resume.Projects.Any())
            {
                column.Item().Element(c => ComposeSection(c, "Projects", ct =>
                {
                    foreach (var project in resume.Projects.OrderBy(p => p.Order))
                    {
                        ct.Item().Element(e => ComposeProject(e, project));
                        ct.Item().Height(10);
                    }
                }));
            }

            // Additional info row
            column.Item().Row(row =>
            {
                if (resume.Languages.Any())
                {
                    row.RelativeItem().Element(c => ComposeSection(c, "Languages", ct =>
                    {
                        foreach (var lang in resume.Languages.OrderBy(l => l.Order))
                        {
                            ct.Item().Text($"{lang.Name} — {GetLanguageProficiencyText(lang.Proficiency)}").FontSize(9 * FontSizeScale);
                        }
                    }));
                }

                if (resume.Certifications.Any())
                {
                    row.RelativeItem().Element(c => ComposeSection(c, "Certifications", ct =>
                    {
                        foreach (var cert in resume.Certifications.OrderBy(c => c.Order).Take(4))
                        {
                            ct.Item().Text(text =>
                            {
                                text.Span(cert.Name).FontSize(9 * FontSizeScale);
                                if (cert.IssueDate.HasValue)
                                    text.Span($" ({cert.IssueDate.Value:yyyy})").FontSize(8 * FontSizeScale).Italic();
                            });
                        }
                    }));
                }
            });
        });
    }

    private void ComposeHeader(IContainer container, PersonalInfo info)
    {
        container.Column(column =>
        {
            // Top decorative line
            column.Item().AlignCenter().Width(100).LineHorizontal(1).LineColor(ParseColor(AccentColor));
            column.Item().Height(15);

            column.Item().AlignCenter().Text(info.FullName)
                .FontSize(28 * FontSizeScale).FontColor(ParseColor(AccentColor)).LetterSpacing(0.2f);

            if (!string.IsNullOrWhiteSpace(info.JobTitle))
            {
                column.Item().Height(5);
                column.Item().AlignCenter().Text(info.JobTitle).FontSize(12 * FontSizeScale).Italic();
            }

            column.Item().Height(12);

            // Contact info centered
            var contacts = new List<string>();
            if (!string.IsNullOrWhiteSpace(info.Email)) contacts.Add(info.Email);
            if (!string.IsNullOrWhiteSpace(info.Phone)) contacts.Add(info.Phone);
            if (!string.IsNullOrWhiteSpace(info.Location)) contacts.Add(info.Location);

            if (contacts.Any())
            {
                column.Item().AlignCenter().Text(string.Join("  ·  ", contacts)).FontSize(9 * FontSizeScale);
            }

            var links = new List<string>();
            if (!string.IsNullOrWhiteSpace(info.Website)) links.Add(info.Website);
            if (!string.IsNullOrWhiteSpace(info.LinkedIn)) links.Add(info.LinkedIn);

            if (links.Any())
            {
                column.Item().Height(3);
                column.Item().AlignCenter().Text(string.Join("  ·  ", links)).FontSize(8 * FontSizeScale).FontColor(Colors.Grey.Darken1);
            }

            column.Item().Height(15);

            // Bottom decorative line
            column.Item().AlignCenter().Width(100).LineHorizontal(1).LineColor(ParseColor(AccentColor));
        });
    }

    private void ComposeSection(IContainer container, string title, Action<ColumnDescriptor> content)
    {
        container.Column(column =>
        {
            column.Item().AlignCenter().Text($"~ {title} ~")
                .FontSize(12 * FontSizeScale).FontColor(ParseColor(AccentColor)).Italic();

            column.Item().Height(10);
            column.Item().Column(content);
        });
    }

    private void ComposeExperience(IContainer container, Experience exp)
    {
        container.Column(column =>
        {
            column.Item().AlignCenter().Text(exp.JobTitle).Bold().FontSize(11 * FontSizeScale);
            column.Item().AlignCenter().Text(text =>
            {
                text.DefaultTextStyle(x => x.FontSize(10 * FontSizeScale));
                text.Span(exp.Company).Italic();
                if (!string.IsNullOrWhiteSpace(exp.Location))
                    text.Span($" · {exp.Location}");
            });
            column.Item().AlignCenter().Text(exp.DateRange).FontSize(9 * FontSizeScale).FontColor(Colors.Grey.Darken1);

            if (!string.IsNullOrWhiteSpace(exp.Description))
            {
                column.Item().PaddingTop(6).PaddingHorizontal(20).Text(exp.Description)
                    .FontSize(9 * FontSizeScale).LineHeight(LineSpacing);
            }

            if (exp.Achievements.Any())
            {
                column.Item().PaddingTop(5).PaddingHorizontal(30).Column(achCol =>
                {
                    foreach (var ach in exp.Achievements)
                    {
                        achCol.Item().Text($"◇ {ach}").FontSize(9 * FontSizeScale).LineHeight(LineSpacing);
                    }
                });
            }
        });
    }

    private void ComposeEducation(IContainer container, Education edu)
    {
        container.Column(column =>
        {
            column.Item().AlignCenter().Text(edu.DegreeWithField).Bold().FontSize(11 * FontSizeScale);
            column.Item().AlignCenter().Text(edu.Institution).Italic().FontSize(10 * FontSizeScale);
            column.Item().AlignCenter().Text(edu.DateRange).FontSize(9 * FontSizeScale).FontColor(Colors.Grey.Darken1);

            if (!string.IsNullOrWhiteSpace(edu.Grade))
            {
                column.Item().AlignCenter().Text($"Distinction: {edu.Grade}").FontSize(9 * FontSizeScale).Italic();
            }
        });
    }

    private void ComposeProject(IContainer container, Project project)
    {
        container.Column(column =>
        {
            column.Item().AlignCenter().Text(project.Name).Bold().FontSize(11 * FontSizeScale);

            if (project.StartDate.HasValue)
            {
                column.Item().AlignCenter().Text(FormatDateRange(project.StartDate, project.EndDate, project.IsOngoing))
                    .FontSize(9 * FontSizeScale).FontColor(Colors.Grey.Darken1);
            }

            if (!string.IsNullOrWhiteSpace(project.Description))
            {
                column.Item().PaddingTop(6).PaddingHorizontal(20).Text(project.Description)
                    .FontSize(9 * FontSizeScale).LineHeight(LineSpacing);
            }

            if (project.Technologies.Any())
            {
                column.Item().PaddingTop(3).AlignCenter().Text(text =>
                {
                    text.Span("Technologies: ").Italic().FontSize(9 * FontSizeScale);
                    text.Span(string.Join(", ", project.Technologies)).FontSize(9 * FontSizeScale);
                });
            }

            if (project.Highlights.Any())
            {
                column.Item().PaddingTop(5).PaddingHorizontal(30).Column(highlightCol =>
                {
                    foreach (var highlight in project.Highlights)
                    {
                        highlightCol.Item().Text($"◇ {highlight}").FontSize(9 * FontSizeScale).LineHeight(LineSpacing);
                    }
                });
            }
        });
    }
}
