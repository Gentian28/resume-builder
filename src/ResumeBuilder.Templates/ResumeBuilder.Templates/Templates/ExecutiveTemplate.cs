using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Templates.Templates;

public class ExecutiveTemplate : BaseTemplate
{
    public override TemplateInfo Info => new()
    {
        Id = "executive",
        Name = "Executive",
        Description = "Premium and sophisticated design for senior professionals",
        Category = TemplateCategory.Professional,
        Layout = TemplateLayout.SingleColumn,
        Tags = new[] { "premium", "senior", "leadership" },
        DefaultAccentColor = "#0f172a",
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

            // Header with border
            column.Item().Border(2).BorderColor(ParseColor(AccentColor)).Padding(20)
                .Element(c => ComposeHeader(c, resume.PersonalInfo));

            // Summary
            if (!string.IsNullOrWhiteSpace(resume.Summary))
            {
                column.Item().Column(col =>
                {
                    col.Item().Text("EXECUTIVE SUMMARY")
                        .FontSize(12 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor)).LetterSpacing(0.1f);
                    col.Item().Height(8);
                    col.Item().Text(resume.Summary).FontSize(10 * FontSizeScale).LineHeight(LineSpacing).Italic();
                });
            }

            // Key Achievements (from first experience)
            var achievements = resume.Experiences.SelectMany(e => e.Achievements).Take(4).ToList();
            if (achievements.Any())
            {
                column.Item().Column(col =>
                {
                    col.Item().Text("KEY ACHIEVEMENTS")
                        .FontSize(12 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor)).LetterSpacing(0.1f);
                    col.Item().Height(8);
                    col.Item().Column(achCol =>
                    {
                        foreach (var ach in achievements)
                        {
                            achCol.Item().PaddingBottom(4).Row(row =>
                            {
                                row.AutoItem().PaddingRight(8).Text("■").FontSize(8 * FontSizeScale).FontColor(ParseColor(AccentColor));
                                row.RelativeItem().Text(ach).FontSize(10 * FontSizeScale).LineHeight(LineSpacing);
                            });
                        }
                    });
                });
            }

            // Experience
            if (resume.Experiences.Any())
            {
                column.Item().Column(col =>
                {
                    col.Spacing(15);
                    col.Item().Text("PROFESSIONAL EXPERIENCE")
                        .FontSize(12 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor)).LetterSpacing(0.1f);

                    foreach (var exp in resume.Experiences.OrderBy(e => e.Order))
                    {
                        col.Item().Element(c => ComposeExperience(c, exp));
                    }
                });
            }

            // Education & Certifications side by side
            column.Item().Row(row =>
            {
                if (resume.EducationList.Any())
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("EDUCATION")
                            .FontSize(12 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor)).LetterSpacing(0.1f);
                        col.Item().Height(8);

                        foreach (var edu in resume.EducationList.OrderBy(e => e.Order))
                        {
                            col.Item().PaddingBottom(8).Column(eduCol =>
                            {
                                eduCol.Item().Text(edu.DegreeWithField).Bold().FontSize(10 * FontSizeScale);
                                eduCol.Item().Text(edu.Institution).FontSize(9 * FontSizeScale);
                                eduCol.Item().Text(edu.DateRange).FontSize(8 * FontSizeScale).FontColor(Colors.Grey.Darken1);
                            });
                        }
                    });
                }

                if (resume.Certifications.Any())
                {
                    row.RelativeItem().PaddingLeft(20).Column(col =>
                    {
                        col.Item().Text("CERTIFICATIONS")
                            .FontSize(12 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor)).LetterSpacing(0.1f);
                        col.Item().Height(8);

                        foreach (var cert in resume.Certifications.OrderBy(c => c.Order))
                        {
                            col.Item().PaddingBottom(6).Column(certCol =>
                            {
                                certCol.Item().Text(cert.Name).Bold().FontSize(10 * FontSizeScale);
                                certCol.Item().Text(cert.IssuingOrganization).FontSize(9 * FontSizeScale);
                                if (cert.IssueDate.HasValue)
                                    certCol.Item().Text(cert.IssueDate.Value.ToString("yyyy")).FontSize(8 * FontSizeScale).FontColor(Colors.Grey.Darken1);
                            });
                        }
                    });
                }
            });

            // Projects
            if (resume.Projects.Any())
            {
                column.Item().Column(col =>
                {
                    col.Spacing(10);
                    col.Item().Text("STRATEGIC PROJECTS")
                        .FontSize(12 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor)).LetterSpacing(0.1f);

                    foreach (var project in resume.Projects.OrderBy(p => p.Order))
                    {
                        col.Item().Element(c => ComposeProject(c, project));
                    }
                });
            }

            // Skills
            if (resume.Skills.Any())
            {
                column.Item().Column(col =>
                {
                    col.Item().Text("CORE COMPETENCIES")
                        .FontSize(12 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor)).LetterSpacing(0.1f);
                    col.Item().Height(8);

                    var skillGroups = resume.Skills.GroupBy(s => s.Category).ToList();
                    col.Item().Row(row =>
                    {
                        int colCount = 0;
                        foreach (var group in skillGroups)
                        {
                            if (colCount > 0 && colCount % 3 == 0) break;

                            row.RelativeItem().Column(skillCol =>
                            {
                                if (!string.IsNullOrWhiteSpace(group.Key))
                                    skillCol.Item().Text(group.Key).Bold().FontSize(9 * FontSizeScale);
                                foreach (var skill in group)
                                    skillCol.Item().Text($"• {skill.Name}").FontSize(9 * FontSizeScale);
                            });
                            colCount++;
                        }
                    });
                });
            }
        });
    }

    private void ComposeHeader(IContainer container, PersonalInfo info)
    {
        container.Column(column =>
        {
            column.Item().AlignCenter().Text(info.FullName.ToUpper())
                .FontSize(26 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor)).LetterSpacing(0.15f);

            if (!string.IsNullOrWhiteSpace(info.JobTitle))
            {
                column.Item().AlignCenter().Text(info.JobTitle.ToUpper())
                    .FontSize(11 * FontSizeScale).FontColor(Colors.Grey.Darken2).LetterSpacing(0.1f);
            }

            column.Item().Height(12);

            column.Item().AlignCenter().Row(row =>
            {
                var contacts = new List<string>();
                if (!string.IsNullOrWhiteSpace(info.Email)) contacts.Add(info.Email);
                if (!string.IsNullOrWhiteSpace(info.Phone)) contacts.Add(info.Phone);
                if (!string.IsNullOrWhiteSpace(info.Location)) contacts.Add(info.Location);
                if (!string.IsNullOrWhiteSpace(info.LinkedIn)) contacts.Add(info.LinkedIn);

                row.RelativeItem().AlignCenter().Text(string.Join("   ◆   ", contacts))
                    .FontSize(9 * FontSizeScale);
            });
        });
    }

    private void ComposeExperience(IContainer container, Experience exp)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text(exp.JobTitle.ToUpper()).Bold().FontSize(11 * FontSizeScale);
                    left.Item().Text(exp.Company).FontSize(10 * FontSizeScale).FontColor(ParseColor(AccentColor));
                });
                row.AutoItem().Column(right =>
                {
                    right.Item().AlignRight().Text(exp.DateRange).FontSize(9 * FontSizeScale);
                    if (!string.IsNullOrWhiteSpace(exp.Location))
                        right.Item().AlignRight().Text(exp.Location).FontSize(8 * FontSizeScale).FontColor(Colors.Grey.Darken1);
                });
            });

            if (!string.IsNullOrWhiteSpace(exp.Description))
            {
                column.Item().PaddingTop(6).Text(exp.Description).FontSize(9 * FontSizeScale).LineHeight(LineSpacing);
            }

            if (exp.Achievements.Any())
            {
                column.Item().PaddingTop(4).Column(achCol =>
                {
                    foreach (var ach in exp.Achievements)
                    {
                        achCol.Item().PaddingBottom(2).Row(row =>
                        {
                            row.AutoItem().PaddingRight(6).Text("▸").FontSize(8 * FontSizeScale);
                            row.RelativeItem().Text(ach).FontSize(9 * FontSizeScale).LineHeight(LineSpacing);
                        });
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
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text(project.Name.ToUpper()).Bold().FontSize(11 * FontSizeScale);
                });
                if (project.StartDate.HasValue)
                {
                    row.AutoItem().AlignRight().Text(FormatDateRange(project.StartDate, project.EndDate, project.IsOngoing))
                        .FontSize(9 * FontSizeScale);
                }
            });

            if (!string.IsNullOrWhiteSpace(project.Description))
            {
                column.Item().PaddingTop(4).Text(project.Description).FontSize(9 * FontSizeScale).LineHeight(LineSpacing);
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
                column.Item().PaddingTop(4).Column(highlightCol =>
                {
                    foreach (var highlight in project.Highlights)
                    {
                        highlightCol.Item().PaddingBottom(2).Row(row =>
                        {
                            row.AutoItem().PaddingRight(6).Text("▸").FontSize(8 * FontSizeScale);
                            row.RelativeItem().Text(highlight).FontSize(9 * FontSizeScale).LineHeight(LineSpacing);
                        });
                    }
                });
            }
        });
    }
}
