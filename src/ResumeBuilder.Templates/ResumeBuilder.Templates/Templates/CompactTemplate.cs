using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Templates.Templates;

public class CompactTemplate : BaseTemplate
{
    public override TemplateInfo Info => new()
    {
        Id = "compact",
        Name = "Compact",
        Description = "Dense layout that fits more content on a single page",
        Category = TemplateCategory.Professional,
        Layout = TemplateLayout.SingleColumn,
        Tags = new[] { "dense", "comprehensive", "detailed" },
        DefaultAccentColor = "#475569",
        DefaultFontFamily = "Arial"
    };

    protected override void ConfigurePage(PageDescriptor page)
    {
        base.ConfigurePage(page);
        page.Margin(25);
    }

    protected override void ComposeContent(IContainer container, Resume resume)
    {
        container.Column(column =>
        {
            column.Spacing(SectionSpacing);

            // Compact header
            column.Item().Element(c => ComposeHeader(c, resume.PersonalInfo, resume.Summary));

            // Experience
            if (resume.Experiences.Any())
            {
                column.Item().Element(c => ComposeSection(c, "EXPERIENCE", ct =>
                {
                    foreach (var exp in resume.Experiences.OrderBy(e => e.Order))
                    {
                        ct.Item().Element(e => ComposeExperience(e, exp));
                        ct.Item().Height(5);
                    }
                }));
            }

            // Education and Skills side by side
            column.Item().Row(row =>
            {
                if (resume.EducationList.Any())
                {
                    row.RelativeItem().Element(c => ComposeSection(c, "EDUCATION", ct =>
                    {
                        foreach (var edu in resume.EducationList.OrderBy(e => e.Order))
                        {
                            ct.Item().PaddingBottom(4).Column(eduCol =>
                            {
                                eduCol.Item().Text(edu.DegreeWithField).Bold().FontSize(8 * FontSizeScale);
                                eduCol.Item().Text($"{edu.Institution} | {edu.DateRange}").FontSize(7 * FontSizeScale);
                            });
                        }
                    }));
                }

                if (resume.Skills.Any())
                {
                    row.RelativeItem().PaddingLeft(10).Element(c => ComposeSection(c, "SKILLS", ct =>
                    {
                        var grouped = resume.Skills.GroupBy(s => s.Category).ToList();

                        foreach (var group in grouped)
                        {
                            ct.Item().Text(text =>
                            {
                                if (!string.IsNullOrWhiteSpace(group.Key))
                                    text.Span($"{group.Key}: ").Bold().FontSize(7 * FontSizeScale);
                                text.Span(string.Join(", ", group.Select(s => s.Name))).FontSize(7 * FontSizeScale);
                            });
                        }
                    }));
                }
            });

            // Projects and Certifications side by side
            if (resume.Projects.Any() || resume.Certifications.Any())
            {
                column.Item().Row(row =>
                {
                    if (resume.Projects.Any())
                    {
                        row.RelativeItem().Element(c => ComposeSection(c, "PROJECTS", ct =>
                        {
                            foreach (var proj in resume.Projects.OrderBy(p => p.Order).Take(3))
                            {
                                ct.Item().PaddingBottom(3).Column(projCol =>
                                {
                                    projCol.Item().Text(proj.Name).Bold().FontSize(8 * FontSizeScale);
                                    if (!string.IsNullOrWhiteSpace(proj.Description))
                                        projCol.Item().Text(proj.Description).FontSize(7 * FontSizeScale).LineHeight(LineSpacing);
                                });
                            }
                        }));
                    }

                    if (resume.Certifications.Any())
                    {
                        row.RelativeItem().PaddingLeft(10).Element(c => ComposeSection(c, "CERTIFICATIONS", ct =>
                        {
                            foreach (var cert in resume.Certifications.OrderBy(c => c.Order).Take(4))
                            {
                                ct.Item().PaddingBottom(2).Text(text =>
                                {
                                    text.Span(cert.Name).Bold().FontSize(7 * FontSizeScale);
                                    if (cert.IssueDate.HasValue)
                                        text.Span($" ({cert.IssueDate.Value:yyyy})").FontSize(7 * FontSizeScale);
                                });
                            }
                        }));
                    }
                });
            }

            // Languages
            if (resume.Languages.Any())
            {
                column.Item().Element(c => ComposeSection(c, "LANGUAGES", ct =>
                {
                    ct.Item().Text(string.Join(" | ", resume.Languages.OrderBy(l => l.Order)
                        .Select(l => $"{l.Name} ({GetLanguageProficiencyText(l.Proficiency)})"))
                    ).FontSize(7 * FontSizeScale);
                }));
            }
        });
    }

    private void ComposeHeader(IContainer container, PersonalInfo info, string summary)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(nameCol =>
                {
                    nameCol.Item().Text(info.FullName).FontSize(18 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor));
                    if (!string.IsNullOrWhiteSpace(info.JobTitle))
                        nameCol.Item().Text(info.JobTitle).FontSize(10 * FontSizeScale);
                });

                row.AutoItem().AlignRight().Column(contactCol =>
                {
                    contactCol.Spacing(1);
                    if (!string.IsNullOrWhiteSpace(info.Email))
                        contactCol.Item().AlignRight().Text(info.Email).FontSize(8 * FontSizeScale);
                    if (!string.IsNullOrWhiteSpace(info.Phone))
                        contactCol.Item().AlignRight().Text(info.Phone).FontSize(8 * FontSizeScale);
                    if (!string.IsNullOrWhiteSpace(info.Location))
                        contactCol.Item().AlignRight().Text(info.Location).FontSize(8 * FontSizeScale);

                    var links = new List<string>();
                    if (!string.IsNullOrWhiteSpace(info.LinkedIn)) links.Add(info.LinkedIn);
                    if (!string.IsNullOrWhiteSpace(info.GitHub)) links.Add(info.GitHub);
                    if (!string.IsNullOrWhiteSpace(info.Website)) links.Add(info.Website);

                    if (links.Any())
                        contactCol.Item().AlignRight().Text(string.Join(" | ", links)).FontSize(7 * FontSizeScale);
                });
            });

            if (!string.IsNullOrWhiteSpace(summary))
            {
                column.Item().Height(5);
                column.Item().Text(summary).FontSize(8 * FontSizeScale).LineHeight(LineSpacing);
            }

            column.Item().Height(5);
            column.Item().LineHorizontal(1).LineColor(ParseColor(AccentColor));
        });
    }

    private void ComposeSection(IContainer container, string title, Action<ColumnDescriptor> content)
    {
        container.Column(column =>
        {
            column.Item().Text(title).FontSize(9 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor)).LetterSpacing(0.05f);
            column.Item().Height(4);
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
                    text.Span(exp.JobTitle).Bold().FontSize(9 * FontSizeScale);
                    text.Span($" at {exp.Company}").FontSize(9 * FontSizeScale);
                    if (!string.IsNullOrWhiteSpace(exp.Location))
                        text.Span($", {exp.Location}").FontSize(8 * FontSizeScale).FontColor(Colors.Grey.Darken1);
                });
                row.AutoItem().Text(exp.DateRange).FontSize(7 * FontSizeScale).FontColor(Colors.Grey.Darken1);
            });

            if (!string.IsNullOrWhiteSpace(exp.Description))
            {
                column.Item().PaddingTop(2).Text(exp.Description).FontSize(7 * FontSizeScale).LineHeight(LineSpacing);
            }

            if (exp.Achievements.Any())
            {
                column.Item().PaddingTop(2).Row(row =>
                {
                    row.RelativeItem().Text(string.Join(" • ", exp.Achievements)).FontSize(7 * FontSizeScale).LineHeight(LineSpacing);
                });
            }
        });
    }
}
