using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Templates.Templates;

public class AcademicTemplate : BaseTemplate
{
    public override TemplateInfo Info => new()
    {
        Id = "academic",
        Name = "Academic",
        Description = "Formal CV format ideal for academic and research positions",
        Category = TemplateCategory.Academic,
        Layout = TemplateLayout.SingleColumn,
        Tags = new[] { "research", "university", "publications" },
        DefaultAccentColor = "#1e40af",
        DefaultFontFamily = "Times New Roman"
    };

    protected override void ConfigurePage(PageDescriptor page)
    {
        base.ConfigurePage(page);
        page.Margin(35);
    }

    protected override void ComposeContent(IContainer container, Resume resume)
    {
        container.Column(column =>
        {
            column.Spacing(SectionSpacing);

            // Header
            column.Item().Element(c => ComposeHeader(c, resume.PersonalInfo));

            // Education first (important for academics)
            if (resume.EducationList.Any())
            {
                column.Item().Element(c => ComposeSection(c, "EDUCATION", ct =>
                {
                    foreach (var edu in resume.EducationList.OrderBy(e => e.Order))
                    {
                        ct.Item().Element(e => ComposeEducation(e, edu));
                        ct.Item().Height(10);
                    }
                }));
            }

            // Research/Professional Experience
            if (resume.Experiences.Any())
            {
                column.Item().Element(c => ComposeSection(c, "RESEARCH & PROFESSIONAL EXPERIENCE", ct =>
                {
                    foreach (var exp in resume.Experiences.OrderBy(e => e.Order))
                    {
                        ct.Item().Element(e => ComposeExperience(e, exp));
                        ct.Item().Height(10);
                    }
                }));
            }

            // Projects (as Research Projects/Publications)
            if (resume.Projects.Any())
            {
                column.Item().Element(c => ComposeSection(c, "RESEARCH PROJECTS", ct =>
                {
                    foreach (var proj in resume.Projects.OrderBy(p => p.Order))
                    {
                        ct.Item().Element(e => ComposeProject(e, proj));
                        ct.Item().Height(8);
                    }
                }));
            }

            // Skills (as Areas of Expertise)
            if (resume.Skills.Any())
            {
                column.Item().Element(c => ComposeSection(c, "AREAS OF EXPERTISE", ct =>
                {
                    var grouped = resume.Skills.GroupBy(s => s.Category).ToList();

                    foreach (var group in grouped)
                    {
                        ct.Item().Row(row =>
                        {
                            if (!string.IsNullOrWhiteSpace(group.Key))
                            {
                                row.ConstantItem(120).Text(group.Key + ":").Bold().FontSize(10 * FontSizeScale);
                            }
                            row.RelativeItem().Text(string.Join(", ", group.Select(s => s.Name))).FontSize(10 * FontSizeScale);
                        });
                    }
                }));
            }

            // Certifications (as Honors & Awards)
            if (resume.Certifications.Any())
            {
                column.Item().Element(c => ComposeSection(c, "HONORS, AWARDS & CERTIFICATIONS", ct =>
                {
                    foreach (var cert in resume.Certifications.OrderBy(c => c.Order))
                    {
                        ct.Item().Row(row =>
                        {
                            row.AutoItem().MinWidth(60).Text(cert.IssueDate?.ToString("yyyy") ?? "").FontSize(10 * FontSizeScale);
                            row.RelativeItem().Text(text =>
                            {
                                text.DefaultTextStyle(x => x.FontSize(10 * FontSizeScale));
                                text.Span(cert.Name).Bold();
                                if (!string.IsNullOrWhiteSpace(cert.IssuingOrganization))
                                    text.Span($", {cert.IssuingOrganization}");
                            });
                        });
                    }
                }));
            }

            // Languages
            if (resume.Languages.Any())
            {
                column.Item().Element(c => ComposeSection(c, "LANGUAGES", ct =>
                {
                    ct.Item().Text(string.Join("; ", resume.Languages.OrderBy(l => l.Order)
                        .Select(l => $"{l.Name} ({GetLanguageProficiencyText(l.Proficiency)})"))).FontSize(10 * FontSizeScale);
                }));
            }
        });
    }

    private void ComposeHeader(IContainer container, PersonalInfo info)
    {
        container.Column(column =>
        {
            column.Item().AlignCenter().Text(info.FullName)
                .FontSize(20 * FontSizeScale).Bold();

            if (!string.IsNullOrWhiteSpace(info.JobTitle))
            {
                column.Item().AlignCenter().Text(info.JobTitle)
                    .FontSize(12 * FontSizeScale);
            }

            column.Item().Height(8);

            var addressParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(info.Address)) addressParts.Add(info.Address);
            if (!string.IsNullOrWhiteSpace(info.City)) addressParts.Add(info.City);
            if (!string.IsNullOrWhiteSpace(info.PostalCode)) addressParts.Add(info.PostalCode);
            if (!string.IsNullOrWhiteSpace(info.Country)) addressParts.Add(info.Country);

            if (addressParts.Any())
                column.Item().AlignCenter().Text(string.Join(", ", addressParts)).FontSize(10 * FontSizeScale);

            var contacts = new List<string>();
            if (!string.IsNullOrWhiteSpace(info.Email)) contacts.Add($"Email: {info.Email}");
            if (!string.IsNullOrWhiteSpace(info.Phone)) contacts.Add($"Tel: {info.Phone}");

            if (contacts.Any())
                column.Item().AlignCenter().Text(string.Join("  |  ", contacts)).FontSize(10 * FontSizeScale);

            if (!string.IsNullOrWhiteSpace(info.Website))
                column.Item().AlignCenter().Text(info.Website).FontSize(10 * FontSizeScale);

            column.Item().Height(8);
            column.Item().LineHorizontal(1).LineColor(Colors.Black);
        });
    }

    private void ComposeSection(IContainer container, string title, Action<ColumnDescriptor> content)
    {
        container.Column(column =>
        {
            column.Item().Text(title).FontSize(11 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor));
            column.Item().Height(6);
            column.Item().Column(content);
        });
    }

    private void ComposeEducation(IContainer container, Education edu)
    {
        container.Row(row =>
        {
            row.AutoItem().MinWidth(80).Text(edu.DateRange).FontSize(10 * FontSizeScale);

            row.RelativeItem().Column(col =>
            {
                col.Item().Text(text =>
                {
                    text.DefaultTextStyle(x => x.FontSize(10 * FontSizeScale));
                    text.Span(edu.Degree).Bold();
                    if (!string.IsNullOrWhiteSpace(edu.FieldOfStudy))
                        text.Span($" in {edu.FieldOfStudy}");
                });

                col.Item().Text(text =>
                {
                    text.DefaultTextStyle(x => x.FontSize(10 * FontSizeScale));
                    text.Span(edu.Institution);
                    if (!string.IsNullOrWhiteSpace(edu.Location))
                        text.Span($", {edu.Location}");
                });

                if (!string.IsNullOrWhiteSpace(edu.Description))
                {
                    col.Item().PaddingTop(3).Text(edu.Description).FontSize(9 * FontSizeScale).Italic().LineHeight(LineSpacing);
                }

                if (!string.IsNullOrWhiteSpace(edu.Grade))
                {
                    col.Item().Text($"Honors: {edu.Grade}").FontSize(9 * FontSizeScale).Italic();
                }
            });
        });
    }

    private void ComposeExperience(IContainer container, Experience exp)
    {
        container.Row(row =>
        {
            row.AutoItem().MinWidth(80).Text(exp.DateRange).FontSize(10 * FontSizeScale);

            row.RelativeItem().Column(col =>
            {
                col.Item().Text(exp.JobTitle).Bold().FontSize(10 * FontSizeScale);

                col.Item().Text(text =>
                {
                    text.DefaultTextStyle(x => x.FontSize(10 * FontSizeScale));
                    text.Span(exp.Company);
                    if (!string.IsNullOrWhiteSpace(exp.Location))
                        text.Span($", {exp.Location}");
                });

                if (!string.IsNullOrWhiteSpace(exp.Description))
                {
                    col.Item().PaddingTop(3).Text(exp.Description).FontSize(9 * FontSizeScale).LineHeight(LineSpacing);
                }

                if (exp.Achievements.Any())
                {
                    col.Item().PaddingTop(3).Column(achCol =>
                    {
                        foreach (var ach in exp.Achievements)
                        {
                            achCol.Item().Row(achRow =>
                            {
                                achRow.AutoItem().Text("• ").FontSize(9 * FontSizeScale);
                                achRow.RelativeItem().Text(ach).FontSize(9 * FontSizeScale).LineHeight(LineSpacing);
                            });
                        }
                    });
                }
            });
        });
    }

    private void ComposeProject(IContainer container, Project proj)
    {
        container.Row(row =>
        {
            if (proj.StartDate.HasValue)
            {
                row.AutoItem().MinWidth(80).Text(FormatYearRange(proj.StartDate, proj.EndDate, proj.IsOngoing)).FontSize(10 * FontSizeScale);
            }
            else
            {
                row.AutoItem().MinWidth(80);
            }

            row.RelativeItem().Column(col =>
            {
                col.Item().Text(proj.Name).Bold().FontSize(10 * FontSizeScale);

                if (!string.IsNullOrWhiteSpace(proj.Description))
                {
                    col.Item().Text(proj.Description).FontSize(9 * FontSizeScale).LineHeight(LineSpacing);
                }

                if (proj.Highlights.Any())
                {
                    col.Item().PaddingTop(2).Column(hlCol =>
                    {
                        foreach (var hl in proj.Highlights)
                        {
                            hlCol.Item().Text($"• {hl}").FontSize(9 * FontSizeScale);
                        }
                    });
                }
            });
        });
    }
}
