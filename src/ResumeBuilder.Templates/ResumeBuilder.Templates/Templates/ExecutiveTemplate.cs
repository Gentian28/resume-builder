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

    private const int SkillColumnsPerRow = 3;

    private static readonly EntryStyle Entry = new() { Bullet = "▸ ", BodyFontSize = 9 };

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

            var sections = GetVisibleSections(resume);

            for (var i = 0; i < sections.Count; i++)
            {
                var sectionType = sections[i];

                // Education and Certifications share a row when they sit next to each other.
                if (sectionType == SectionType.Education &&
                    i + 1 < sections.Count && sections[i + 1] == SectionType.Certifications)
                {
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Element(c => ComposeEducationBlock(c, resume));
                        row.RelativeItem().PaddingLeft(20).Element(c => ComposeCertificationBlock(c, resume));
                    });
                    i++;
                    continue;
                }

                switch (sectionType)
                {
                    case SectionType.PersonalInfo:
                        column.Item().Border(2).BorderColor(ParseColor(AccentColor)).Padding(20)
                            .Element(c => ComposeHeader(c, resume.PersonalInfo));
                        break;

                    case SectionType.Summary:
                        column.Item().Column(col =>
                        {
                            col.Item().Element(c => ComposeSectionTitle(c, "EXECUTIVE SUMMARY"));
                            col.Item().Height(8);
                            col.Item().Text(resume.Summary).FontSize(10 * FontSizeScale).LineHeight(LineSpacing).Italic();
                        });
                        break;

                    case SectionType.Experience:
                        column.Item().Column(col =>
                        {
                            col.Spacing(15);
                            col.Item().Element(c => ComposeSectionTitle(c, "PROFESSIONAL EXPERIENCE"));

                            foreach (var exp in resume.Experiences.OrderBy(e => e.Order))
                            {
                                col.Item().Element(c => ComposeExperience(c, exp));
                            }
                        });
                        break;

                    case SectionType.Education:
                        column.Item().Element(c => ComposeEducationBlock(c, resume));
                        break;

                    case SectionType.Certifications:
                        column.Item().Element(c => ComposeCertificationBlock(c, resume));
                        break;

                    case SectionType.Projects:
                        column.Item().Column(col =>
                        {
                            col.Spacing(10);
                            col.Item().Element(c => ComposeSectionTitle(c, "STRATEGIC PROJECTS"));

                            foreach (var project in resume.Projects.OrderBy(p => p.Order))
                            {
                                col.Item().Element(c => ComposeProject(c, project));
                            }
                        });
                        break;

                    case SectionType.Skills:
                        column.Item().Element(c => ComposeSkills(c, resume.Skills));
                        break;

                    case SectionType.Languages:
                        column.Item().Column(col =>
                        {
                            col.Item().Element(c => ComposeSectionTitle(c, "LANGUAGES"));
                            col.Item().Height(8);
                            col.Item().Text(string.Join("   ◆   ", resume.Languages.OrderBy(l => l.Order)
                                .Select(l => FormatLanguage(l, " — ")))).FontSize(9 * FontSizeScale);
                        });
                        break;

                    case SectionType.CustomSections:
                        foreach (var custom in GetVisibleCustomSections(resume))
                        {
                            column.Item().Column(col =>
                            {
                                col.Item().Element(c => ComposeSectionTitle(c, custom.Title.ToUpper()));
                                col.Item().Height(8);
                                col.Item().Column(ct => ComposeCustomSectionItems(ct, custom));
                            });
                        }
                        break;
                }
            }
        });
    }

    private void ComposeSectionTitle(IContainer container, string title)
    {
        container.Text(title)
            .FontSize(12 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor)).LetterSpacing(0.1f);
    }

    private void ComposeEducationBlock(IContainer container, Resume resume)
    {
        container.Column(col =>
        {
            col.Item().Element(c => ComposeSectionTitle(c, "EDUCATION"));
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

    private void ComposeCertificationBlock(IContainer container, Resume resume)
    {
        container.Column(col =>
        {
            col.Item().Element(c => ComposeSectionTitle(c, "CERTIFICATIONS"));
            col.Item().Height(8);

            foreach (var cert in resume.Certifications.OrderBy(c => c.Order))
            {
                col.Item().PaddingBottom(6).Column(certCol =>
                {
                    certCol.Item().Text(cert.Name).Bold().FontSize(10 * FontSizeScale);
                    certCol.Item().Text(cert.IssuingOrganization).FontSize(9 * FontSizeScale);
                    if (cert.IssueDate.HasValue)
                        certCol.Item().Text(ResumeDateFormat.Year(cert.IssueDate)).FontSize(8 * FontSizeScale).FontColor(Colors.Grey.Darken1);
                });
            }
        });
    }

    private void ComposeSkills(IContainer container, List<Skill> skills)
    {
        var skillGroups = skills.GroupBy(s => s.Category).ToList();

        container.Column(col =>
        {
            col.Item().Element(c => ComposeSectionTitle(c, "CORE COMPETENCIES"));
            col.Item().Height(8);

            for (var i = 0; i < skillGroups.Count; i += SkillColumnsPerRow)
            {
                var batch = skillGroups.Skip(i).Take(SkillColumnsPerRow).ToList();

                col.Item().PaddingBottom(6).Row(row =>
                {
                    foreach (var group in batch)
                    {
                        row.RelativeItem().Column(skillCol =>
                        {
                            if (!string.IsNullOrWhiteSpace(group.Key))
                                skillCol.Item().Text(group.Key).Bold().FontSize(9 * FontSizeScale);
                            foreach (var skill in group.OrderBy(s => s.Order))
                                skillCol.Item().Text($"• {skill.Name}").FontSize(9 * FontSizeScale);
                        });
                    }

                    // Keep the last row's columns the same width as the full rows above it.
                    for (var pad = batch.Count; pad < SkillColumnsPerRow; pad++)
                        row.RelativeItem();
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
                if (!string.IsNullOrWhiteSpace(info.LinkedIn)) contacts.Add(FormatLinkedInDisplay(info.LinkedIn));
                if (!string.IsNullOrWhiteSpace(info.GitHub)) contacts.Add($"github.com/{FormatGitHubDisplay(info.GitHub)}");

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
                column.Item().PaddingTop(4).Element(c => ComposeBulletList(c, exp.Achievements, Entry));
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

            if (!string.IsNullOrWhiteSpace(project.Url))
            {
                column.Item().PaddingTop(2).Text(project.Url).FontSize(8 * FontSizeScale).FontColor(Colors.Grey.Darken1);
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
                column.Item().PaddingTop(4).Element(c => ComposeBulletList(c, project.Highlights, Entry));
            }
        });
    }
}
