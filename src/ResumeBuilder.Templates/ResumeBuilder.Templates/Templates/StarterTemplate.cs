using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Templates.Templates;

public class StarterTemplate : BaseTemplate
{
    public override TemplateInfo Info => new()
    {
        Id = "starter",
        Name = "Starter",
        Description = "Entry-level focused design emphasizing education and skills",
        Category = TemplateCategory.Modern,
        Layout = TemplateLayout.SingleColumn,
        Tags = new[] { "entry-level", "graduate", "beginner" },
        DefaultAccentColor = "#0891b2",
        DefaultFontFamily = "Arial"
    };

    protected override void ComposeContent(IContainer container, Resume resume)
    {
        container.Column(column =>
        {
            column.Spacing(SectionSpacing);

            var sections = GetVisibleSections(resume);

            for (var i = 0; i < sections.Count; i++)
            {
                var sectionType = sections[i];

                // Languages and Certifications share a row when adjacent.
                if (sectionType == SectionType.Languages &&
                    i + 1 < sections.Count && sections[i + 1] == SectionType.Certifications)
                {
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Element(c => ComposeLanguages(c, resume));
                        row.RelativeItem().Element(c => ComposeCertifications(c, resume));
                    });
                    i++;
                    continue;
                }

                switch (sectionType)
                {
                    case SectionType.PersonalInfo:
                        column.Item().Element(c => ComposeHeader(c, resume.PersonalInfo));
                        break;

                    case SectionType.Summary:
                        column.Item().Element(c => ComposeSection(c, "Career Objective", ct =>
                        {
                            ct.Item().Text(resume.Summary).FontSize(10 * FontSizeScale).LineHeight(LineSpacing);
                        }));
                        break;

                    case SectionType.Education:
                        column.Item().Element(c => ComposeSection(c, "Education", ct =>
                        {
                            foreach (var edu in resume.EducationList.OrderBy(e => e.Order))
                            {
                                ct.Item().Element(e => ComposeEducation(e, edu));
                                ct.Item().Height(10);
                            }
                        }));
                        break;

                    case SectionType.Skills:
                        column.Item().Element(c => ComposeSection(c, "Skills & Abilities", ct =>
                        {
                            ct.Item().Element(e => ComposeSkills(e, resume.Skills));
                        }));
                        break;

                    case SectionType.Experience:
                        column.Item().Element(c => ComposeSection(c, "Experience", ct =>
                        {
                            foreach (var exp in resume.Experiences.OrderBy(e => e.Order))
                            {
                                ct.Item().Element(e => ComposeExperience(e, exp));
                                ct.Item().Height(10);
                            }
                        }));
                        break;

                    case SectionType.Projects:
                        column.Item().Element(c => ComposeSection(c, "Projects & Portfolio", ct =>
                        {
                            foreach (var proj in resume.Projects.OrderBy(p => p.Order))
                            {
                                ct.Item().Element(e => ComposeProject(e, proj));
                                ct.Item().Height(8);
                            }
                        }));
                        break;

                    case SectionType.Languages:
                        column.Item().Element(c => ComposeLanguages(c, resume));
                        break;

                    case SectionType.Certifications:
                        column.Item().Element(c => ComposeCertifications(c, resume));
                        break;

                    case SectionType.CustomSections:
                        foreach (var custom in GetVisibleCustomSections(resume))
                        {
                            column.Item().Element(c => ComposeSection(c, custom.Title, ct =>
                                ComposeCustomSectionItems(ct, custom, 10, 9)));
                        }
                        break;
                }
            }
        });
    }

    private void ComposeLanguages(IContainer container, Resume resume)
    {
        ComposeSection(container, "Languages", ct =>
        {
            foreach (var lang in resume.Languages.OrderBy(l => l.Order))
            {
                ct.Item().Text($"• {lang.Name} ({GetLanguageProficiencyText(lang.Proficiency)})").FontSize(9 * FontSizeScale);
            }
        });
    }

    private void ComposeCertifications(IContainer container, Resume resume)
    {
        ComposeSection(container, "Certifications", ct =>
        {
            foreach (var cert in resume.Certifications.OrderBy(c => c.Order))
            {
                ct.Item().PaddingBottom(3).Text(text =>
                {
                    text.Span(cert.Name).FontSize(9 * FontSizeScale);
                    if (!string.IsNullOrWhiteSpace(cert.IssuingOrganization))
                        text.Span($" - {cert.IssuingOrganization}").FontSize(8 * FontSizeScale).FontColor(Colors.Grey.Darken1);
                });
            }
        });
    }

    private void ComposeHeader(IContainer container, PersonalInfo info)
    {
        container.Background(ParseColor(AccentColor)).Padding(15).Column(column =>
        {
            column.Item().Text(info.FullName)
                .FontSize(24 * FontSizeScale).Bold().FontColor(Colors.White);

            if (!string.IsNullOrWhiteSpace(info.JobTitle))
            {
                column.Item().Text(info.JobTitle)
                    .FontSize(12 * FontSizeScale).FontColor(Colors.White.WithAlpha(0.9f));
            }

            column.Item().Height(8);

            column.Item().Row(row =>
            {
                var contacts = new List<string>();
                if (!string.IsNullOrWhiteSpace(info.Email)) contacts.Add(info.Email);
                if (!string.IsNullOrWhiteSpace(info.Phone)) contacts.Add(info.Phone);
                if (!string.IsNullOrWhiteSpace(info.Location)) contacts.Add(info.Location);

                row.RelativeItem().Text(string.Join("  |  ", contacts))
                    .FontSize(9 * FontSizeScale).FontColor(Colors.White.WithAlpha(0.9f));
            });

            var links = new List<string>();
            if (!string.IsNullOrWhiteSpace(info.LinkedIn)) links.Add(FormatLinkedInDisplay(info.LinkedIn));
            if (!string.IsNullOrWhiteSpace(info.GitHub)) links.Add($"github.com/{FormatGitHubDisplay(info.GitHub)}");
            if (!string.IsNullOrWhiteSpace(info.Website)) links.Add(info.Website);

            if (links.Any())
            {
                column.Item().Text(string.Join("  |  ", links))
                    .FontSize(8 * FontSizeScale).FontColor(Colors.White.WithAlpha(0.8f));
            }
        });
    }

    private void ComposeSection(IContainer container, string title, Action<ColumnDescriptor> content)
    {
        container.Column(column =>
        {
            column.Item().BorderBottom(2).BorderColor(ParseColor(AccentColor)).PaddingBottom(3)
                .Text(title).FontSize(12 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor));

            column.Item().Height(8);
            column.Item().Column(content);
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

            column.Item().Text(edu.Institution).FontSize(10 * FontSizeScale).FontColor(ParseColor(AccentColor));

            if (!string.IsNullOrWhiteSpace(edu.Location))
                column.Item().Text(edu.Location).FontSize(9 * FontSizeScale).FontColor(Colors.Grey.Darken1);

            if (!string.IsNullOrWhiteSpace(edu.Grade))
                column.Item().Text($"GPA/Grade: {edu.Grade}").FontSize(9 * FontSizeScale).Bold();

            if (!string.IsNullOrWhiteSpace(edu.Description))
            {
                column.Item().PaddingTop(3).Text(edu.Description).FontSize(9 * FontSizeScale).LineHeight(LineSpacing);
            }
        });
    }

    private void ComposeSkills(IContainer container, List<Skill> skills)
    {
        container.Column(column =>
        {
            foreach (var group in skills.GroupBy(s => s.Category))
            {
                column.Item().Row(row =>
                {
                    if (!string.IsNullOrWhiteSpace(group.Key))
                    {
                        row.AutoItem().MinWidth(100).Text(group.Key + ":").Bold().FontSize(9 * FontSizeScale);
                    }
                    row.RelativeItem().Text(string.Join(" • ", group.OrderBy(s => s.Order).Select(s => s.Name))).FontSize(9 * FontSizeScale);
                });
            }
        });
    }

    private void ComposeExperience(IContainer container, Experience exp)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Text(exp.JobTitle).Bold().FontSize(10 * FontSizeScale);
                row.AutoItem().Text(exp.DateRange).FontSize(9 * FontSizeScale).FontColor(Colors.Grey.Darken1);
            });

            column.Item().Text(text =>
            {
                text.DefaultTextStyle(x => x.FontSize(9 * FontSizeScale));
                text.Span(exp.Company).FontColor(ParseColor(AccentColor));
                if (!string.IsNullOrWhiteSpace(exp.Location))
                    text.Span($" | {exp.Location}").FontColor(Colors.Grey.Darken1);
            });

            if (!string.IsNullOrWhiteSpace(exp.Description))
            {
                column.Item().PaddingTop(3).Text(exp.Description).FontSize(9 * FontSizeScale).LineHeight(LineSpacing);
            }

            if (exp.Achievements.Any())
            {
                column.Item().PaddingTop(3).Column(achCol =>
                {
                    foreach (var ach in exp.Achievements)
                    {
                        achCol.Item().Text($"• {ach}").FontSize(9 * FontSizeScale).LineHeight(LineSpacing);
                    }
                });
            }
        });
    }

    private void ComposeProject(IContainer container, Project proj)
    {
        container.Column(column =>
        {
            column.Item().Text(proj.Name).Bold().FontSize(10 * FontSizeScale);

            if (!string.IsNullOrWhiteSpace(proj.Description))
            {
                column.Item().Text(proj.Description).FontSize(9 * FontSizeScale).LineHeight(LineSpacing);
            }

            if (!string.IsNullOrWhiteSpace(proj.Url))
            {
                column.Item().Text(proj.Url).FontSize(8 * FontSizeScale).FontColor(Colors.Grey.Darken1);
            }

            if (proj.Technologies.Any())
            {
                column.Item().PaddingTop(2).Text($"Technologies: {string.Join(", ", proj.Technologies)}")
                    .FontSize(8 * FontSizeScale).FontColor(Colors.Grey.Darken1);
            }

            if (proj.Highlights.Any())
            {
                column.Item().PaddingTop(2).Column(hlCol =>
                {
                    foreach (var hl in proj.Highlights)
                    {
                        hlCol.Item().Text($"• {hl}").FontSize(8 * FontSizeScale);
                    }
                });
            }
        });
    }
}
