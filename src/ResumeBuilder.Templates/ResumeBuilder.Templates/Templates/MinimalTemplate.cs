using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Templates.Templates;

public class MinimalTemplate : BaseTemplate
{
    public override TemplateInfo Info => new()
    {
        Id = "minimal",
        Name = "Minimal",
        Description = "Clean and spacious design with maximum whitespace",
        Category = TemplateCategory.Modern,
        Layout = TemplateLayout.SingleColumn,
        Tags = new[] { "simple", "clean", "whitespace" },
        DefaultAccentColor = "#374151",
        DefaultFontFamily = "Arial"
    };

    protected override void ConfigurePage(PageDescriptor page)
    {
        base.ConfigurePage(page);
        page.Margin(50);
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
                        column.Item().Element(c => ComposeHeader(c, resume.PersonalInfo));
                        break;

                    case SectionType.Summary:
                        column.Item().Text(resume.Summary).FontSize(10 * FontSizeScale).LineHeight(LineSpacing);
                        break;

                    case SectionType.Experience:
                        column.Item().Element(c => ComposeSection(c, "Experience", ct =>
                        {
                            foreach (var exp in resume.Experiences.OrderBy(e => e.Order))
                            {
                                ct.Item().Element(e => ComposeExperience(e, exp));
                                ct.Item().Height(15);
                            }
                        }));
                        break;

                    case SectionType.Education:
                        column.Item().Element(c => ComposeSection(c, "Education", ct =>
                        {
                            foreach (var edu in resume.EducationList.OrderBy(e => e.Order))
                            {
                                ct.Item().Element(e => ComposeEducation(e, edu));
                                ct.Item().Height(12);
                            }
                        }));
                        break;

                    case SectionType.Skills:
                        column.Item().Element(c => ComposeSection(c, "Skills", ct =>
                        {
                            ct.Item().Text(string.Join("   /   ", resume.Skills.OrderBy(s => s.Order).Select(s => s.Name)))
                                .FontSize(10 * FontSizeScale);
                        }));
                        break;

                    case SectionType.Languages:
                        column.Item().Element(c => ComposeSection(c, "Languages", ct =>
                        {
                            foreach (var lang in resume.Languages.OrderBy(l => l.Order))
                            {
                                ct.Item().Text($"{lang.Name}  /  {GetLanguageProficiencyText(lang.Proficiency)}")
                                    .FontSize(10 * FontSizeScale);
                            }
                        }));
                        break;

                    case SectionType.Certifications:
                        column.Item().Element(c => ComposeSection(c, "Certifications", ct =>
                        {
                            foreach (var cert in resume.Certifications.OrderBy(c => c.Order))
                            {
                                ct.Item().Text(text =>
                                {
                                    text.DefaultTextStyle(x => x.FontSize(10 * FontSizeScale));
                                    text.Span(cert.Name).Bold();
                                    if (!string.IsNullOrWhiteSpace(cert.IssuingOrganization))
                                        text.Span($"  /  {cert.IssuingOrganization}");
                                    if (cert.IssueDate.HasValue)
                                        text.Span($"  /  {cert.IssueDate.Value:yyyy}").FontColor(Colors.Grey.Darken1);
                                });
                            }
                        }));
                        break;

                    case SectionType.Projects:
                        column.Item().Element(c => ComposeSection(c, "Projects", ct =>
                        {
                            foreach (var project in resume.Projects.OrderBy(p => p.Order))
                            {
                                ct.Item().Element(e => ComposeProject(e, project));
                                ct.Item().Height(12);
                            }
                        }));
                        break;
                }
            }
        });
    }

    private void ComposeHeader(IContainer container, PersonalInfo info)
    {
        container.Column(column =>
        {
            column.Item().Text(info.FullName)
                .FontSize(32 * FontSizeScale)
                .FontColor(ParseColor(AccentColor));

            column.Item().Height(5);

            if (!string.IsNullOrWhiteSpace(info.JobTitle))
            {
                column.Item().Text(info.JobTitle)
                    .FontSize(12 * FontSizeScale)
                    .FontColor(Colors.Grey.Darken1);
            }

            column.Item().Height(10);

            var contacts = new List<string>();
            if (!string.IsNullOrWhiteSpace(info.Email)) contacts.Add(info.Email);
            if (!string.IsNullOrWhiteSpace(info.Phone)) contacts.Add(info.Phone);
            if (!string.IsNullOrWhiteSpace(info.Location)) contacts.Add(info.Location);

            if (contacts.Any())
            {
                column.Item().Text(string.Join("   /   ", contacts))
                    .FontSize(9 * FontSizeScale)
                    .FontColor(Colors.Grey.Darken1);
            }
        });
    }

    private void ComposeSection(IContainer container, string title, Action<ColumnDescriptor> content)
    {
        container.Column(column =>
        {
            column.Item().Text(title.ToUpper())
                .FontSize(9 * FontSizeScale)
                .Bold()
                .LetterSpacing(0.15f)
                .FontColor(Colors.Grey.Darken1);

            column.Item().Height(10);
            column.Item().Column(content);
        });
    }

    private void ComposeExperience(IContainer container, Experience exp)
    {
        container.Column(column =>
        {
            column.Item().Text(exp.JobTitle).Bold().FontSize(11 * FontSizeScale);

            column.Item().Height(3);

            column.Item().Text(text =>
            {
                text.DefaultTextStyle(x => x.FontSize(9 * FontSizeScale));
                text.Span(exp.Company);
                if (!string.IsNullOrWhiteSpace(exp.Location))
                    text.Span($"  /  {exp.Location}");
                text.Span($"  /  {exp.DateRange}").FontColor(Colors.Grey.Darken1);
            });

            if (!string.IsNullOrWhiteSpace(exp.Description))
            {
                column.Item().PaddingTop(8).Text(exp.Description).FontSize(9 * FontSizeScale).LineHeight(LineSpacing);
            }

            if (exp.Achievements.Any())
            {
                column.Item().PaddingTop(5).Column(achieveCol =>
                {
                    foreach (var achievement in exp.Achievements)
                    {
                        achieveCol.Item().PaddingBottom(2).Text($"— {achievement}").FontSize(9 * FontSizeScale).LineHeight(LineSpacing);
                    }
                });
            }
        });
    }

    private void ComposeEducation(IContainer container, Education edu)
    {
        container.Column(column =>
        {
            column.Item().Text(edu.DegreeWithField).Bold().FontSize(11 * FontSizeScale);

            column.Item().Height(3);

            column.Item().Text(text =>
            {
                text.DefaultTextStyle(x => x.FontSize(9 * FontSizeScale));
                text.Span(edu.Institution);
                text.Span($"  /  {edu.DateRange}").FontColor(Colors.Grey.Darken1);
            });
        });
    }

    private void ComposeProject(IContainer container, Project project)
    {
        container.Column(column =>
        {
            column.Item().Text(project.Name).Bold().FontSize(11 * FontSizeScale);

            column.Item().Height(3);

            column.Item().Text(text =>
            {
                text.DefaultTextStyle(x => x.FontSize(9 * FontSizeScale));
                if (project.StartDate.HasValue)
                {
                    text.Span(FormatDateRange(project.StartDate, project.EndDate, project.IsOngoing))
                        .FontColor(Colors.Grey.Darken1);
                }
            });

            if (!string.IsNullOrWhiteSpace(project.Description))
            {
                column.Item().PaddingTop(8).Text(project.Description).FontSize(9 * FontSizeScale).LineHeight(LineSpacing);
            }

            if (project.Technologies.Any())
            {
                column.Item().PaddingTop(3).Text(string.Join("   /   ", project.Technologies))
                    .FontSize(9 * FontSizeScale).FontColor(Colors.Grey.Darken1);
            }

            if (project.Highlights.Any())
            {
                column.Item().PaddingTop(5).Column(highlightCol =>
                {
                    foreach (var highlight in project.Highlights)
                    {
                        highlightCol.Item().PaddingBottom(2).Text($"— {highlight}").FontSize(9 * FontSizeScale).LineHeight(LineSpacing);
                    }
                });
            }
        });
    }
}
