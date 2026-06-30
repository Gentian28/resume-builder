using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Templates.Templates;

public class ProfessionalTemplate : BaseTemplate
{
    public override TemplateInfo Info => new()
    {
        Id = "professional",
        Name = "Professional",
        Description = "Clean corporate design suitable for all industries",
        Category = TemplateCategory.Professional,
        Layout = TemplateLayout.SingleColumn,
        Tags = new[] { "corporate", "business", "universal" },
        DefaultAccentColor = "#1d4ed8",
        DefaultFontFamily = "Arial"
    };

    protected override void ComposeContent(IContainer container, Resume resume)
    {
        container.Column(column =>
        {
            column.Spacing(SectionSpacing);

            // Header with colored bar
            column.Item().Element(c => ComposeHeader(c, resume.PersonalInfo));

            // Summary
            if (!string.IsNullOrWhiteSpace(resume.Summary))
            {
                column.Item().Element(c => ComposeSection(c, "Professional Summary", ct =>
                {
                    ct.Item().Text(resume.Summary).FontSize(10 * FontSizeScale).LineHeight(LineSpacing);
                }));
            }

            // Experience
            if (resume.Experiences.Any())
            {
                column.Item().Element(c => ComposeSection(c, "Work Experience", ct =>
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
                        ct.Item().Height(8);
                    }
                }));
            }

            // Two column section for skills and languages/certifications
            column.Item().Row(row =>
            {
                if (resume.Skills.Any())
                {
                    row.RelativeItem().Element(c => ComposeSection(c, "Skills", ct =>
                    {
                        var grouped = resume.Skills.GroupBy(s => s.Category).ToList();

                        if (grouped.Any(g => !string.IsNullOrWhiteSpace(g.Key)))
                        {
                            foreach (var group in grouped)
                            {
                                if (!string.IsNullOrWhiteSpace(group.Key))
                                    ct.Item().Text(group.Key).Bold().FontSize(9 * FontSizeScale);

                                ct.Item().PaddingLeft(10).Column(skillCol =>
                                {
                                    foreach (var skill in group)
                                    {
                                        skillCol.Item().Row(skillRow =>
                                        {
                                            skillRow.AutoItem().Text("•").FontSize(9 * FontSizeScale);
                                            skillRow.RelativeItem().PaddingLeft(5).Text(skill.Name).FontSize(9 * FontSizeScale);
                                        });
                                    }
                                });
                                ct.Item().Height(5);
                            }
                        }
                        else
                        {
                            ct.Item().Column(skillCol =>
                            {
                                foreach (var skill in resume.Skills.OrderBy(s => s.Order))
                                {
                                    skillCol.Item().Row(skillRow =>
                                    {
                                        skillRow.AutoItem().Text("•").FontSize(9 * FontSizeScale);
                                        skillRow.RelativeItem().PaddingLeft(5).Text(skill.Name).FontSize(9 * FontSizeScale);
                                    });
                                }
                            });
                        }
                    }));
                }

                row.RelativeItem().PaddingLeft(15).Column(rightCol =>
                {
                    if (resume.Languages.Any())
                    {
                        rightCol.Item().Element(c => ComposeSection(c, "Languages", ct =>
                        {
                            foreach (var lang in resume.Languages.OrderBy(l => l.Order))
                            {
                                ct.Item().Text($"• {lang.Name} - {GetLanguageProficiencyText(lang.Proficiency)}").FontSize(9 * FontSizeScale);
                            }
                        }));
                    }

                    if (resume.Certifications.Any())
                    {
                        rightCol.Item().PaddingTop(10).Element(c => ComposeSection(c, "Certifications", ct =>
                        {
                            foreach (var cert in resume.Certifications.OrderBy(c => c.Order).Take(5))
                            {
                                ct.Item().PaddingBottom(3).Column(certCol =>
                                {
                                    certCol.Item().Text(cert.Name).Bold().FontSize(9 * FontSizeScale);
                                    certCol.Item().Text(text =>
                                    {
                                        text.Span(cert.IssuingOrganization).FontSize(8 * FontSizeScale);
                                        if (cert.IssueDate.HasValue)
                                            text.Span($" | {cert.IssueDate.Value:MMM yyyy}").FontSize(8 * FontSizeScale);
                                    });
                                });
                            }
                        }));
                    }
                });
            });
        });
    }

    private void ComposeHeader(IContainer container, PersonalInfo info)
    {
        container.Column(column =>
        {
            // Colored header bar
            column.Item().Height(8).Background(ParseColor(AccentColor));

            column.Item().PaddingTop(15).Row(row =>
            {
                row.RelativeItem().Column(nameCol =>
                {
                    nameCol.Item().Text(info.FullName)
                        .FontSize(26 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor));

                    if (!string.IsNullOrWhiteSpace(info.JobTitle))
                    {
                        nameCol.Item().Text(info.JobTitle).FontSize(13 * FontSizeScale).FontColor(Colors.Grey.Darken2);
                    }
                });

                row.AutoItem().AlignRight().Column(contactCol =>
                {
                    contactCol.Spacing(2);

                    if (!string.IsNullOrWhiteSpace(info.Email))
                        contactCol.Item().AlignRight().Text(info.Email).FontSize(9 * FontSizeScale);
                    if (!string.IsNullOrWhiteSpace(info.Phone))
                        contactCol.Item().AlignRight().Text(info.Phone).FontSize(9 * FontSizeScale);
                    if (!string.IsNullOrWhiteSpace(info.Location))
                        contactCol.Item().AlignRight().Text(info.Location).FontSize(9 * FontSizeScale);
                    if (!string.IsNullOrWhiteSpace(info.LinkedIn))
                        contactCol.Item().AlignRight().Text(info.LinkedIn).FontSize(8 * FontSizeScale).FontColor(Colors.Grey.Darken1);
                    if (!string.IsNullOrWhiteSpace(info.Website))
                        contactCol.Item().AlignRight().Text(info.Website).FontSize(8 * FontSizeScale).FontColor(Colors.Grey.Darken1);
                });
            });

            column.Item().Height(10);
            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
        });
    }

    private void ComposeSection(IContainer container, string title, Action<ColumnDescriptor> content)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.AutoItem().Width(4).Height(14).Background(ParseColor(AccentColor));
                row.AutoItem().PaddingLeft(8).Text(title).FontSize(12 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor));
            });

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
                row.AutoItem().Text(exp.DateRange).FontSize(9 * FontSizeScale).FontColor(Colors.Grey.Darken1);
            });

            column.Item().Text(text =>
            {
                text.DefaultTextStyle(x => x.FontSize(10 * FontSizeScale));
                text.Span(exp.Company).SemiBold().FontColor(ParseColor(AccentColor));
                if (!string.IsNullOrWhiteSpace(exp.Location))
                    text.Span($" | {exp.Location}").FontColor(Colors.Grey.Darken1);
            });

            if (!string.IsNullOrWhiteSpace(exp.Description))
            {
                column.Item().PaddingTop(4).Text(exp.Description).FontSize(9 * FontSizeScale).LineHeight(LineSpacing);
            }

            if (exp.Achievements.Any())
            {
                column.Item().PaddingTop(4).Column(achCol =>
                {
                    foreach (var ach in exp.Achievements)
                    {
                        achCol.Item().Row(achRow =>
                        {
                            achRow.AutoItem().Text("▪").FontSize(8 * FontSizeScale).FontColor(ParseColor(AccentColor));
                            achRow.RelativeItem().PaddingLeft(5).Text(ach).FontSize(9 * FontSizeScale).LineHeight(LineSpacing);
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
                row.RelativeItem().Text(edu.DegreeWithField).Bold().FontSize(10 * FontSizeScale);
                row.AutoItem().Text(edu.DateRange).FontSize(9 * FontSizeScale).FontColor(Colors.Grey.Darken1);
            });

            column.Item().Text(text =>
            {
                text.DefaultTextStyle(x => x.FontSize(9 * FontSizeScale));
                text.Span(edu.Institution).FontColor(ParseColor(AccentColor));
                if (!string.IsNullOrWhiteSpace(edu.Location))
                    text.Span($", {edu.Location}");
            });

            if (!string.IsNullOrWhiteSpace(edu.Grade))
            {
                column.Item().Text($"Grade: {edu.Grade}").FontSize(8 * FontSizeScale).FontColor(Colors.Grey.Darken1);
            }
        });
    }
}
