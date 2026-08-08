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

    private EntryStyle Entry => new()
    {
        TitleFontSize = 11,
        SubtitleFontSize = 10,
        BodyFontSize = 9,
        MetaFontSize = 9,
        SubtitleColor = ParseColor(AccentColor),
        LocationSeparator = " | ",
        Bullet = "▪ "
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

                // Skills and Languages share a row when adjacent, as in the default order.
                if (sectionType == SectionType.Skills &&
                    i + 1 < sections.Count && sections[i + 1] == SectionType.Languages)
                {
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Element(c => ComposeSkills(c, resume));
                        row.RelativeItem().PaddingLeft(15).Element(c => ComposeLanguages(c, resume));
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
                                ct.Item().Element(e => ComposeExperienceEntry(e, exp, Entry));
                                ct.Item().Height(12);
                            }
                        }));
                        break;

                    case SectionType.Education:
                        column.Item().Element(c => ComposeSection(c, "Education", ct =>
                        {
                            foreach (var edu in resume.EducationList.OrderBy(e => e.Order))
                            {
                                ct.Item().Element(e => ComposeEducationEntry(e, edu, Entry with { TitleFontSize = 10, SubtitleFontSize = 9, LocationSeparator = ", " }));
                                ct.Item().Height(8);
                            }
                        }));
                        break;

                    case SectionType.Skills:
                        column.Item().Element(c => ComposeSkills(c, resume));
                        break;

                    case SectionType.Languages:
                        column.Item().Element(c => ComposeLanguages(c, resume));
                        break;

                    case SectionType.Certifications:
                        column.Item().Element(c => ComposeCertifications(c, resume));
                        break;

                    case SectionType.Projects:
                        column.Item().Element(c => ComposeSection(c, "Projects", ct =>
                        {
                            foreach (var project in resume.Projects.OrderBy(p => p.Order))
                            {
                                ct.Item().Element(e => ComposeProjectEntry(e, project, Entry));
                                ct.Item().Height(8);
                            }
                        }));
                        break;

                    case SectionType.CustomSections:
                        foreach (var custom in GetVisibleCustomSections(resume))
                        {
                            column.Item().Element(c => ComposeSection(c, custom.Title, ct =>
                                ComposeCustomSectionItems(ct, custom)));
                        }
                        break;
                }
            }
        });
    }

    private void ComposeSkills(IContainer container, Resume resume)
    {
        ComposeSection(container, "Skills", ct =>
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
                        foreach (var skill in group.OrderBy(s => s.Order))
                            skillCol.Item().Element(c => ComposeSkillLine(c, skill));
                    });
                    ct.Item().Height(5);
                }
            }
            else
            {
                ct.Item().Column(skillCol =>
                {
                    foreach (var skill in resume.Skills.OrderBy(s => s.Order))
                        skillCol.Item().Element(c => ComposeSkillLine(c, skill));
                });
            }
        });
    }

    private void ComposeSkillLine(IContainer container, Skill skill)
    {
        container.Row(skillRow =>
        {
            skillRow.AutoItem().Text("•").FontSize(9 * FontSizeScale);
            skillRow.RelativeItem().PaddingLeft(5).Text(text =>
            {
                text.Span(skill.Name).FontSize(9 * FontSizeScale);
                text.Span($" — {GetSkillLevelText(skill.Level)}")
                    .FontSize(8 * FontSizeScale).FontColor(Colors.Grey.Darken1);
            });
        });
    }

    private void ComposeLanguages(IContainer container, Resume resume)
    {
        ComposeSection(container, "Languages", ct =>
        {
            foreach (var lang in resume.Languages.OrderBy(l => l.Order))
            {
                ct.Item().Text($"• {FormatLanguage(lang)}").FontSize(9 * FontSizeScale);
            }
        });
    }

    private void ComposeCertifications(IContainer container, Resume resume)
    {
        ComposeSection(container, "Certifications", ct =>
        {
            foreach (var cert in resume.Certifications.OrderBy(c => c.Order))
            {
                ct.Item().PaddingBottom(3).Column(certCol =>
                {
                    certCol.Item().Text(cert.Name).Bold().FontSize(9 * FontSizeScale);
                    certCol.Item().Text(text =>
                    {
                        text.Span(cert.IssuingOrganization).FontSize(8 * FontSizeScale);
                        if (cert.IssueDate.HasValue)
                            text.Span($" | {ResumeDateFormat.MonthYear(cert.IssueDate)}").FontSize(8 * FontSizeScale);
                    });
                    if (!string.IsNullOrWhiteSpace(cert.CredentialId))
                        certCol.Item().Text($"ID: {cert.CredentialId}").FontSize(8 * FontSizeScale).FontColor(Colors.Grey.Darken1);
                });
            }
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
                        contactCol.Item().AlignRight().Text(FormatLinkedInDisplay(info.LinkedIn)).FontSize(8 * FontSizeScale).FontColor(Colors.Grey.Darken1);
                    if (!string.IsNullOrWhiteSpace(info.GitHub))
                        contactCol.Item().AlignRight().Text($"github.com/{FormatGitHubDisplay(info.GitHub)}").FontSize(8 * FontSizeScale).FontColor(Colors.Grey.Darken1);
                    if (!string.IsNullOrWhiteSpace(info.Website))
                        contactCol.Item().AlignRight().Text(FormatWebsiteDisplay(info.Website)).FontSize(8 * FontSizeScale).FontColor(Colors.Grey.Darken1);
                });
            });

            column.Item().Height(10);
            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
        });
    }

    private void ComposeSection(IContainer container, string title, Action<ColumnDescriptor> content)
    {
        container.EnsureSpace(90).Column(column =>
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
}
