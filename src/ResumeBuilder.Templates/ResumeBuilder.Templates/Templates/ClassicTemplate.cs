using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Templates.Templates;

public class ClassicTemplate : BaseTemplate
{
    public override TemplateInfo Info => new()
    {
        Id = "classic",
        Name = "Classic",
        Description = "Traditional and timeless design suitable for conservative industries",
        Category = TemplateCategory.Classic,
        Layout = TemplateLayout.SingleColumn,
        Tags = new[] { "traditional", "formal", "conservative" },
        DefaultAccentColor = "#1e3a5f",
        DefaultFontFamily = "Times New Roman"
    };

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
                        column.Item().Element(c => ComposeSection(c, "PROFESSIONAL SUMMARY", ct =>
                        {
                            ct.Item().Text(resume.Summary).FontSize(10 * FontSizeScale).LineHeight(LineSpacing);
                        }));
                        break;

                    case SectionType.Experience:
                        column.Item().Element(c => ComposeSection(c, "PROFESSIONAL EXPERIENCE", ct =>
                        {
                            foreach (var exp in resume.Experiences.OrderBy(e => e.Order))
                            {
                                ct.Item().Element(e => ComposeExperience(e, exp));
                                ct.Item().Height(12);
                            }
                        }));
                        break;

                    case SectionType.Education:
                        column.Item().Element(c => ComposeSection(c, "EDUCATION", ct =>
                        {
                            foreach (var edu in resume.EducationList.OrderBy(e => e.Order))
                            {
                                ct.Item().Element(e => ComposeEducation(e, edu));
                                ct.Item().Height(8);
                            }
                        }));
                        break;

                    case SectionType.Skills:
                        column.Item().Element(c => ComposeSection(c, "SKILLS", ct =>
                        {
                            ct.Item().Text(string.Join(", ", resume.Skills.OrderBy(s => s.Order).Select(s => s.Name)))
                                .FontSize(10 * FontSizeScale);
                        }));
                        break;

                    case SectionType.Languages:
                        column.Item().Element(c => ComposeSection(c, "LANGUAGES", ct =>
                        {
                            ct.Item().Text(string.Join(", ", resume.Languages.OrderBy(l => l.Order)
                                .Select(l => $"{l.Name} ({GetLanguageProficiencyText(l.Proficiency)})")))
                                .FontSize(10 * FontSizeScale);
                        }));
                        break;

                    case SectionType.Certifications:
                        column.Item().Element(c => ComposeSection(c, "CERTIFICATIONS", ct =>
                        {
                            foreach (var cert in resume.Certifications.OrderBy(c => c.Order))
                            {
                                ct.Item().Text(text =>
                                {
                                    text.DefaultTextStyle(x => x.FontSize(10 * FontSizeScale));
                                    text.Span(cert.Name).Bold();
                                    if (!string.IsNullOrWhiteSpace(cert.IssuingOrganization))
                                        text.Span($", {cert.IssuingOrganization}");
                                    if (cert.IssueDate.HasValue)
                                        text.Span($" ({ResumeDateFormat.Year(cert.IssueDate)})");
                                });
                            }
                        }));
                        break;

                    case SectionType.Projects:
                        column.Item().Element(c => ComposeSection(c, "PROJECTS", ct =>
                        {
                            foreach (var project in resume.Projects.OrderBy(p => p.Order))
                            {
                                ct.Item().Element(e => ComposeProject(e, project));
                                ct.Item().Height(8);
                            }
                        }));
                        break;

                    case SectionType.CustomSections:
                        foreach (var custom in GetVisibleCustomSections(resume))
                        {
                            column.Item().Element(c => ComposeSection(c, custom.Title.ToUpper(), ct =>
                                ComposeCustomSectionItems(ct, custom, 11, 10)));
                        }
                        break;
                }
            }
        });
    }

    private void ComposeHeader(IContainer container, PersonalInfo info)
    {
        container.Column(column =>
        {
            column.Item().AlignCenter().Text(info.FullName.ToUpper())
                .FontSize(24 * FontSizeScale)
                .Bold()
                .LetterSpacing(0.1f);

            if (!string.IsNullOrWhiteSpace(info.JobTitle))
            {
                column.Item().AlignCenter().Text(info.JobTitle)
                    .FontSize(12 * FontSizeScale)
                    .Italic();
            }

            column.Item().Height(8);

            var contactParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(info.Address))
                contactParts.Add(info.Address);
            if (!string.IsNullOrWhiteSpace(info.City))
                contactParts.Add(info.City);
            if (!string.IsNullOrWhiteSpace(info.Country))
                contactParts.Add(info.Country);

            if (contactParts.Any())
            {
                column.Item().AlignCenter().Text(string.Join(", ", contactParts)).FontSize(10 * FontSizeScale);
            }

            var contactLine = new List<string>();
            if (!string.IsNullOrWhiteSpace(info.Phone))
                contactLine.Add(info.Phone);
            if (!string.IsNullOrWhiteSpace(info.Email))
                contactLine.Add(info.Email);
            if (!string.IsNullOrWhiteSpace(info.LinkedIn))
                contactLine.Add(FormatLinkedInDisplay(info.LinkedIn));
            if (!string.IsNullOrWhiteSpace(info.GitHub))
                contactLine.Add($"github.com/{FormatGitHubDisplay(info.GitHub)}");

            if (contactLine.Any())
            {
                column.Item().AlignCenter().Text(string.Join(ContactSeparator, contactLine)).FontSize(10 * FontSizeScale);
            }

            column.Item().Height(8);
            column.Item().LineHorizontal(1).LineColor(ParseColor(AccentColor));
        });
    }

    private void ComposeSection(IContainer container, string title, Action<ColumnDescriptor> content)
    {
        container.Column(column =>
        {
            column.Item().BorderBottom(1).BorderColor(ParseColor(AccentColor))
                .PaddingBottom(2)
                .Text(title)
                .FontSize(12 * FontSizeScale)
                .Bold()
                .LetterSpacing(0.05f);

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
                row.RelativeItem().Text(text =>
                {
                    text.Span(exp.Company).Bold().FontSize(11 * FontSizeScale);
                    if (!string.IsNullOrWhiteSpace(exp.Location))
                    {
                        text.Span($", {exp.Location}");
                    }
                });
                row.AutoItem().Text(exp.DateRange).FontSize(10 * FontSizeScale);
            });

            column.Item().Text(exp.JobTitle).Italic().FontSize(10 * FontSizeScale);

            if (!string.IsNullOrWhiteSpace(exp.Description))
            {
                column.Item().PaddingTop(4).Text(exp.Description).FontSize(10 * FontSizeScale).LineHeight(LineSpacing);
            }

            if (exp.Achievements.Any())
            {
                column.Item().PaddingTop(4).PaddingLeft(15)
                    .Element(c => ComposeBulletList(c, exp.Achievements, BulletStyle));
            }
        });
    }

    private static readonly EntryStyle BulletStyle = new() { BodyFontSize = 10 };

    private void ComposeEducation(IContainer container, Education edu)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.Span(edu.Institution).Bold().FontSize(11 * FontSizeScale);
                    if (!string.IsNullOrWhiteSpace(edu.Location))
                    {
                        text.Span($", {edu.Location}");
                    }
                });
                row.AutoItem().Text(edu.DateRange).FontSize(10 * FontSizeScale);
            });

            column.Item().Text(edu.DegreeWithField).Italic().FontSize(10 * FontSizeScale);

            if (!string.IsNullOrWhiteSpace(edu.Grade))
            {
                column.Item().Text($"GPA/Grade: {edu.Grade}").FontSize(10 * FontSizeScale);
            }

            if (!string.IsNullOrWhiteSpace(edu.Description))
            {
                column.Item().PaddingTop(2).Text(edu.Description).FontSize(10 * FontSizeScale).LineHeight(LineSpacing);
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
                {
                    row.AutoItem().Text(FormatDateRange(project.StartDate, project.EndDate, project.IsOngoing))
                        .FontSize(10 * FontSizeScale);
                }
            });

            if (!string.IsNullOrWhiteSpace(project.Description))
            {
                column.Item().PaddingTop(4).Text(project.Description).FontSize(10 * FontSizeScale).LineHeight(LineSpacing);
            }

            if (!string.IsNullOrWhiteSpace(project.Url))
            {
                column.Item().PaddingTop(2).Text(project.Url).FontSize(9 * FontSizeScale).FontColor(Colors.Grey.Darken1);
            }

            if (project.Technologies.Any())
            {
                column.Item().PaddingTop(2).Text(text =>
                {
                    text.Span("Technologies: ").Bold().FontSize(10 * FontSizeScale);
                    text.Span(string.Join(", ", project.Technologies)).FontSize(10 * FontSizeScale);
                });
            }

            if (project.Highlights.Any())
            {
                column.Item().PaddingTop(4).PaddingLeft(15)
                    .Element(c => ComposeBulletList(c, project.Highlights, BulletStyle));
            }
        });
    }
}
