using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Templates.Templates;

public class ModernTemplate : BaseTemplate
{
    public override TemplateInfo Info => new()
    {
        Id = "modern",
        Name = "Modern",
        Description = "Clean and contemporary design with accent colors and clear hierarchy",
        Category = TemplateCategory.Modern,
        Layout = TemplateLayout.SingleColumn,
        Tags = new[] { "clean", "professional", "minimalist" },
        DefaultAccentColor = "#2563eb",
        DefaultFontFamily = "Arial"
    };

    protected override void ComposeContent(IContainer container, Resume resume)
    {
        container.Column(column =>
        {
            column.Spacing(SectionSpacing);

            // Render sections in the configured order
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
                        column.Item().Element(c => ComposeSection(c, "Professional Summary", ct =>
                        {
                            ct.Item().Text(resume.Summary).FontSize(10 * FontSizeScale).LineHeight(LineSpacing);
                        }));
                        break;

                    case SectionType.Experience:
                        column.Item().Element(c => ComposeSection(c, "Work Experience", ct =>
                        {
                            foreach (var exp in resume.Experiences.OrderBy(e => e.Order))
                            {
                                ct.Item().Element(e => ComposeExperience(e, exp));
                                ct.Item().Height(10);
                            }
                        }));
                        break;

                    case SectionType.Education:
                        column.Item().Element(c => ComposeSection(c, "Education", ct =>
                        {
                            foreach (var edu in resume.EducationList.OrderBy(e => e.Order))
                            {
                                ct.Item().Element(e => ComposeEducation(e, edu));
                                ct.Item().Height(8);
                            }
                        }));
                        break;

                    case SectionType.Skills:
                        column.Item().Element(c => ComposeSection(c, "Skills", ct =>
                        {
                            ct.Item().Element(e => ComposeSkills(e, resume.Skills));
                        }));
                        break;

                    case SectionType.Languages:
                        column.Item().Element(c => ComposeSection(c, "Languages", ct =>
                        {
                            ct.Item().Row(row =>
                            {
                                foreach (var lang in resume.Languages.OrderBy(l => l.Order))
                                {
                                    row.AutoItem().PaddingRight(20).Text(text =>
                                    {
                                        text.Span(lang.Name).Bold();
                                        text.Span($" - {GetLanguageProficiencyText(lang.Proficiency)}");
                                    });
                                }
                            });
                        }));
                        break;

                    case SectionType.Certifications:
                        column.Item().Element(c => ComposeSection(c, "Certifications", ct =>
                        {
                            foreach (var cert in resume.Certifications.OrderBy(c => c.Order))
                            {
                                ct.Item().Row(row =>
                                {
                                    row.RelativeItem().Text(text =>
                                    {
                                        text.Span(cert.Name).Bold();
                                        if (!string.IsNullOrWhiteSpace(cert.IssuingOrganization))
                                        {
                                            text.Span($" - {cert.IssuingOrganization}");
                                        }
                                    });
                                    if (cert.IssueDate.HasValue)
                                    {
                                        row.AutoItem().Text(cert.IssueDate.Value.ToString("MMM yyyy")).FontColor(Colors.Grey.Darken1);
                                    }
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
                                ct.Item().Height(8);
                            }
                        }));
                        break;
                }
            }
        });
    }

    private void ComposeHeader(IContainer container, Resume resume)
    {
        var info = resume.PersonalInfo;
        container.Row(row =>
        {
            // Photo on the left (if available)
            if (resume.PersonalInfo.Photo != null && resume.PersonalInfo.Photo.Length > 0)
            {
                row.ConstantItem(70).Element(c => ComposePhotoOrInitials(c, resume, 70, Colors.Grey.Lighten3, ParseColor(AccentColor)));
                row.ConstantItem(15); // Spacing
            }

            // Header content
            row.RelativeItem().Column(column =>
            {
                column.Item().Text(info.FullName)
                    .FontSize(28 * FontSizeScale)
                    .Bold()
                    .FontFamily(HeadingFontFamily)
                    .FontColor(ParseColor(AccentColor));

                if (!string.IsNullOrWhiteSpace(info.JobTitle))
                {
                    column.Item().Text(info.JobTitle)
                        .FontSize(14 * FontSizeScale)
                        .FontColor(Colors.Grey.Darken2);
                }

                column.Item().Height(10);

                column.Item().Row(contactRow =>
                {
                    var contactItems = new List<string>();

                    if (!string.IsNullOrWhiteSpace(info.Email))
                        contactItems.Add(info.Email);
                    if (!string.IsNullOrWhiteSpace(info.Phone))
                        contactItems.Add(info.Phone);
                    if (!string.IsNullOrWhiteSpace(info.Location))
                        contactItems.Add(info.Location);
                    if (!string.IsNullOrWhiteSpace(info.LinkedIn))
                        contactItems.Add(info.LinkedIn);
                    if (!string.IsNullOrWhiteSpace(info.Website))
                        contactItems.Add(info.Website);

                    contactRow.RelativeItem().Text(string.Join("  |  ", contactItems))
                        .FontSize(9 * FontSizeScale)
                        .FontColor(Colors.Grey.Darken1);
                });

                column.Item().Height(5);
                column.Item().LineHorizontal(2).LineColor(ParseColor(AccentColor));
            });
        });
    }

    private void ComposeSection(IContainer container, string title, Action<ColumnDescriptor> content)
    {
        container.Column(column =>
        {
            column.Item().Text(title)
                .FontSize(14 * FontSizeScale)
                .Bold()
                .FontFamily(HeadingFontFamily)
                .FontColor(ParseColor(AccentColor));

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
                    text.Span(exp.JobTitle).Bold().FontSize(11 * FontSizeScale);
                });
                row.AutoItem().Text(exp.DateRange).FontSize(9 * FontSizeScale).FontColor(Colors.Grey.Darken1);
            });

            column.Item().Text(text =>
            {
                text.DefaultTextStyle(x => x.FontSize(10 * FontSizeScale));
                text.Span(exp.Company).SemiBold();
                if (!string.IsNullOrWhiteSpace(exp.Location))
                {
                    text.Span($"  |  {exp.Location}").FontColor(Colors.Grey.Darken1);
                }
            });

            if (!string.IsNullOrWhiteSpace(exp.Description))
            {
                column.Item().PaddingTop(4).Text(exp.Description).FontSize(9 * FontSizeScale).LineHeight(LineSpacing);
            }

            if (exp.Achievements.Any())
            {
                column.Item().PaddingTop(4).Column(achieveCol =>
                {
                    foreach (var achievement in exp.Achievements)
                    {
                        achieveCol.Item().Row(row =>
                        {
                            row.AutoItem().Text("• ").FontSize(9 * FontSizeScale);
                            row.RelativeItem().Text(achievement).FontSize(9 * FontSizeScale).LineHeight(LineSpacing);
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
                row.RelativeItem().Text(text =>
                {
                    text.Span(edu.DegreeWithField).Bold().FontSize(11 * FontSizeScale);
                });
                row.AutoItem().Text(edu.DateRange).FontSize(9 * FontSizeScale).FontColor(Colors.Grey.Darken1);
            });

            column.Item().Text(text =>
            {
                text.DefaultTextStyle(x => x.FontSize(10 * FontSizeScale));
                text.Span(edu.Institution).SemiBold();
                if (!string.IsNullOrWhiteSpace(edu.Location))
                {
                    text.Span($"  |  {edu.Location}").FontColor(Colors.Grey.Darken1);
                }
            });

            if (!string.IsNullOrWhiteSpace(edu.Grade))
            {
                column.Item().Text($"Grade: {edu.Grade}").FontSize(9 * FontSizeScale).FontColor(Colors.Grey.Darken1);
            }

            if (!string.IsNullOrWhiteSpace(edu.Description))
            {
                column.Item().PaddingTop(2).Text(edu.Description).FontSize(9 * FontSizeScale).LineHeight(LineSpacing);
            }
        });
    }

    private void ComposeSkills(IContainer container, List<Skill> skills)
    {
        var groupedSkills = skills.GroupBy(s => s.Category).ToList();

        container.Column(column =>
        {
            if (groupedSkills.Any(g => !string.IsNullOrWhiteSpace(g.Key)))
            {
                foreach (var group in groupedSkills.OrderBy(g => g.Key))
                {
                    column.Item().Row(row =>
                    {
                        if (!string.IsNullOrWhiteSpace(group.Key))
                        {
                            row.AutoItem().MinWidth(80).Text(group.Key + ":").Bold().FontSize(9 * FontSizeScale);
                        }
                        row.RelativeItem().Text(string.Join(", ", group.Select(s => s.Name))).FontSize(9 * FontSizeScale);
                    });
                }
            }
            else
            {
                column.Item().Text(string.Join("  •  ", skills.Select(s => s.Name))).FontSize(10 * FontSizeScale);
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
                        .FontSize(9 * FontSizeScale).FontColor(Colors.Grey.Darken1);
                }
            });

            if (!string.IsNullOrWhiteSpace(project.Description))
            {
                column.Item().PaddingTop(2).Text(project.Description).FontSize(9 * FontSizeScale).LineHeight(LineSpacing);
            }

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
                column.Item().PaddingTop(2).Column(highlightCol =>
                {
                    foreach (var highlight in project.Highlights)
                    {
                        highlightCol.Item().Row(row =>
                        {
                            row.AutoItem().Text("• ").FontSize(9 * FontSizeScale);
                            row.RelativeItem().Text(highlight).FontSize(9 * FontSizeScale).LineHeight(LineSpacing);
                        });
                    }
                });
            }
        });
    }
}
