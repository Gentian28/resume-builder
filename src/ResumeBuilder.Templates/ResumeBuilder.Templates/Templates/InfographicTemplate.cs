using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Templates.Templates;

public class InfographicTemplate : BaseTemplate
{
    public override TemplateInfo Info => new()
    {
        Id = "infographic",
        Name = "Infographic",
        Description = "Visual two-column layout with progress bars and infographic elements",
        Category = TemplateCategory.Modern,
        Layout = TemplateLayout.TwoColumn,
        Tags = new[] { "visual", "infographic", "progress bars", "modern" },
        DefaultAccentColor = "#0ea5e9",
        DefaultFontFamily = "Arial"
    };

    private const float LeftColumnWidth = 200;

    protected override void ComposeContent(IContainer container, Resume resume)
    {
        container.Column(column =>
        {
            // Full-width header
            column.Item().Element(c => ComposeHeader(c, resume));

            column.Item().Height(15);

            // Two-column split
            column.Item().Row(row =>
            {
                // Left column — visual elements
                row.ConstantItem(LeftColumnWidth).Column(left =>
                {
                    left.Spacing(SectionSpacing);
                    ComposeLeftColumn(left, resume);
                });

                row.ConstantItem(20); // Gutter

                // Right column — text content
                row.RelativeItem().Column(right =>
                {
                    right.Spacing(SectionSpacing);
                    ComposeRightColumn(right, resume);
                });
            });
        });
    }

    private void ComposeHeader(IContainer container, Resume resume)
    {
        var info = resume.PersonalInfo;
        var accent = ParseColor(AccentColor);

        container.Column(column =>
        {
            column.Item().Background(accent).Padding(20).Row(row =>
            {
                row.RelativeItem().Column(nameCol =>
                {
                    nameCol.Item().Text(info.FullName)
                        .FontSize(26 * FontSizeScale)
                        .Bold()
                        .FontFamily(HeadingFontFamily)
                        .FontColor(Colors.White);

                    if (!string.IsNullOrWhiteSpace(info.JobTitle))
                    {
                        nameCol.Item().Text(info.JobTitle)
                            .FontSize(13 * FontSizeScale)
                            .FontColor(Colors.White.WithAlpha(0.85f));
                    }

                    nameCol.Item().Height(8);

                    var contacts = new List<string>();
                    if (!string.IsNullOrWhiteSpace(info.Email)) contacts.Add(info.Email);
                    if (!string.IsNullOrWhiteSpace(info.Phone)) contacts.Add(info.Phone);
                    if (!string.IsNullOrWhiteSpace(info.Location)) contacts.Add(info.Location);
                    if (!string.IsNullOrWhiteSpace(info.LinkedIn)) contacts.Add(FormatLinkedInDisplay(info.LinkedIn));
                    if (!string.IsNullOrWhiteSpace(info.GitHub)) contacts.Add($"github.com/{FormatGitHubDisplay(info.GitHub)}");
                    if (!string.IsNullOrWhiteSpace(info.Website)) contacts.Add(FormatWebsiteDisplay(info.Website));

                    nameCol.Item().Text(string.Join(ContactSeparator, contacts))
                        .FontSize(8 * FontSizeScale)
                        .FontColor(Colors.White.WithAlpha(0.8f));
                });

                // Photo on the right if available
                if (info.Photo != null && info.Photo.Length > 0)
                {
                    row.ConstantItem(15);
                    row.ConstantItem(65).AlignMiddle().Element(c =>
                        ComposePhotoOrInitials(c, resume, 65, Colors.White.WithAlpha(0.2f), Colors.White));
                }
            });
        });
    }

    private void ComposeLeftColumn(ColumnDescriptor left, Resume resume)
    {
        foreach (var sectionType in GetOrderedSections())
        {
            if (!ShouldRenderSection(sectionType, resume))
                continue;

            switch (sectionType)
            {
                case SectionType.Summary:
                    left.Item().Element(c => ComposeSummaryCard(c, resume.Summary!));
                    break;
                case SectionType.Skills:
                    left.Item().Element(c => ComposeSkillBlocks(c, resume.Skills));
                    break;
                case SectionType.Languages:
                    left.Item().Element(c => ComposeLanguageBars(c, resume.Languages));
                    break;
                case SectionType.Certifications:
                    left.Item().Element(c => ComposeCertificationCards(c, resume.Certifications));
                    break;
            }
        }
    }

    private void ComposeRightColumn(ColumnDescriptor right, Resume resume)
    {
        foreach (var sectionType in GetOrderedSections())
        {
            if (!ShouldRenderSection(sectionType, resume))
                continue;

            switch (sectionType)
            {
                case SectionType.Experience:
                    right.Item().Element(c => ComposeTextSection(c, "EXPERIENCE", ct =>
                    {
                        ct.Spacing(10);
                        foreach (var exp in resume.Experiences.OrderBy(e => e.Order))
                            ct.Item().Element(e => ComposeExperience(e, exp));
                    }));
                    break;

                case SectionType.Education:
                    right.Item().Element(c => ComposeTextSection(c, "EDUCATION", ct =>
                    {
                        ct.Spacing(8);
                        foreach (var edu in resume.EducationList.OrderBy(e => e.Order))
                            ct.Item().Element(e => ComposeEducation(e, edu));
                    }));
                    break;

                case SectionType.Projects:
                    right.Item().Element(c => ComposeTextSection(c, "PROJECTS", ct =>
                    {
                        ct.Spacing(10);
                        foreach (var project in resume.Projects.OrderBy(p => p.Order))
                            ct.Item().Element(e => ComposeProject(e, project));
                    }));
                    break;

                case SectionType.CustomSections:
                    foreach (var custom in GetVisibleCustomSections(resume))
                    {
                        right.Item().Element(c => ComposeTextSection(c, custom.Title.ToUpper(), ct =>
                            ComposeCustomSectionItems(ct, custom, 10, 8)));
                    }
                    break;
            }
        }
    }

    private void ComposeSummaryCard(IContainer container, string summary)
    {
        var accent = ParseColor(AccentColor);
        container.Column(col =>
        {
            col.Item().Text("SUMMARY")
                .FontSize(12 * FontSizeScale).Bold().FontColor(accent).LetterSpacing(0.05f);
            col.Item().Height(5);
            col.Item().BorderLeft(3).BorderColor(accent).PaddingLeft(8)
                .Text(summary).FontSize(9 * FontSizeScale).LineHeight(LineSpacing);
        });
    }

    private void ComposeSkillBlocks(IContainer container, List<Skill> skills)
    {
        var accent = ParseColor(AccentColor);
        var lightGrey = Colors.Grey.Lighten3;

        container.Column(col =>
        {
            col.Item().Text("SKILLS")
                .FontSize(12 * FontSizeScale).Bold().FontColor(accent).LetterSpacing(0.05f);
            col.Item().Height(8);

            foreach (var skill in skills.OrderBy(s => s.Order))
            {
                col.Item().PaddingBottom(6).Column(skillCol =>
                {
                    skillCol.Item().Text(skill.Name).FontSize(8 * FontSizeScale);
                    skillCol.Item().Height(3);

                    // 5-block progress bar
                    skillCol.Item().Height(8).Row(blockRow =>
                    {
                        var level = Math.Clamp((int)skill.Level, 0, 5);
                        for (var i = 0; i < 5; i++)
                        {
                            if (i > 0)
                                blockRow.ConstantItem(2); // gap between blocks
                            blockRow.RelativeItem()
                                .Background(i < level ? accent : lightGrey);
                        }
                    });
                });
            }
        });
    }

    private void ComposeLanguageBars(IContainer container, List<Language> languages)
    {
        var accent = ParseColor(AccentColor);
        var lightGrey = Colors.Grey.Lighten3;

        container.Column(col =>
        {
            col.Item().Text("LANGUAGES")
                .FontSize(12 * FontSizeScale).Bold().FontColor(accent).LetterSpacing(0.05f);
            col.Item().Height(8);

            foreach (var lang in languages.OrderBy(l => l.Order))
            {
                col.Item().PaddingBottom(6).Column(langCol =>
                {
                    langCol.Item().Row(labelRow =>
                    {
                        labelRow.RelativeItem().Text(lang.Name).FontSize(8 * FontSizeScale);
                        labelRow.AutoItem().Text(GetLanguageProficiencyText(lang.Proficiency))
                            .FontSize(7 * FontSizeScale).FontColor(Colors.Grey.Darken1);
                    });
                    langCol.Item().Height(3);

                    // Horizontal proficiency bar. The remainder item is omitted at 100% because
                    // QuestPDF rejects RelativeItem(0).
                    var pct = GetLanguageProficiencyPercent(lang.Proficiency);
                    langCol.Item().Height(6).Background(lightGrey).Row(bar =>
                    {
                        if (pct > 0)
                            bar.RelativeItem(pct).Background(accent);

                        if (pct < 100)
                            bar.RelativeItem(100 - pct);
                    });
                });
            }
        });
    }

    private static int GetLanguageProficiencyPercent(LanguageProficiency level) => level switch
    {
        LanguageProficiency.Basic => 20,
        LanguageProficiency.Conversational => 40,
        LanguageProficiency.Professional => 60,
        LanguageProficiency.Fluent => 80,
        LanguageProficiency.Native => 100,
        _ => 60
    };

    private void ComposeCertificationCards(IContainer container, List<Certification> certifications)
    {
        var accent = ParseColor(AccentColor);

        container.Column(col =>
        {
            col.Item().Text("CERTIFICATIONS")
                .FontSize(12 * FontSizeScale).Bold().FontColor(accent).LetterSpacing(0.05f);
            col.Item().Height(8);

            foreach (var cert in certifications.OrderBy(c => c.Order))
            {
                col.Item().PaddingBottom(6)
                    .Border(1).BorderColor(accent.WithAlpha(0.3f))
                    .Padding(8)
                    .Column(cardCol =>
                    {
                        cardCol.Item().Text(cert.Name).Bold().FontSize(9 * FontSizeScale);
                        if (!string.IsNullOrWhiteSpace(cert.IssuingOrganization))
                            cardCol.Item().Text(cert.IssuingOrganization).FontSize(8 * FontSizeScale).FontColor(accent);
                        if (cert.IssueDate.HasValue)
                            cardCol.Item().Text(ResumeDateFormat.MonthYear(cert.IssueDate))
                                .FontSize(7 * FontSizeScale).FontColor(Colors.Grey.Darken1);
                    });
            }
        });
    }

    private void ComposeTextSection(IContainer container, string title, Action<ColumnDescriptor> content)
    {
        var accent = ParseColor(AccentColor);
        container.EnsureSpace(90).Column(column =>
        {
            column.Item().Text(title)
                .FontSize(12 * FontSizeScale)
                .Bold()
                .FontFamily(HeadingFontFamily)
                .FontColor(accent)
                .LetterSpacing(0.05f);

            column.Item().Height(3);
            column.Item().LineHorizontal(1).LineColor(accent.WithAlpha(0.3f));
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
                row.RelativeItem().Text(exp.JobTitle).Bold().FontSize(10 * FontSizeScale);
                row.AutoItem().Text(exp.DateRange).FontSize(8 * FontSizeScale).FontColor(Colors.Grey.Darken1);
            });

            column.Item().Text(text =>
            {
                text.Span(exp.Company).SemiBold().FontSize(9 * FontSizeScale).FontColor(ParseColor(AccentColor));
                if (!string.IsNullOrWhiteSpace(exp.Location))
                    text.Span($"  |  {exp.Location}").FontSize(8 * FontSizeScale).FontColor(Colors.Grey.Darken1);
            });

            if (!string.IsNullOrWhiteSpace(exp.Description))
                column.Item().PaddingTop(3).Text(exp.Description).FontSize(8 * FontSizeScale).LineHeight(LineSpacing);

            if (exp.Achievements.Any())
            {
                column.Item().PaddingTop(3).Column(achCol =>
                {
                    foreach (var ach in exp.Achievements)
                    {
                        achCol.Item().Row(row =>
                        {
                            row.AutoItem().Text("• ").FontSize(8 * FontSizeScale);
                            row.RelativeItem().Text(ach).FontSize(8 * FontSizeScale).LineHeight(LineSpacing);
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
                row.AutoItem().Text(edu.DateRange).FontSize(8 * FontSizeScale).FontColor(Colors.Grey.Darken1);
            });

            column.Item().Text(edu.Institution).SemiBold().FontSize(9 * FontSizeScale).FontColor(ParseColor(AccentColor));

            if (!string.IsNullOrWhiteSpace(edu.Grade))
                column.Item().Text($"Grade: {edu.Grade}").FontSize(8 * FontSizeScale).FontColor(Colors.Grey.Darken1);
        });
    }

    private void ComposeProject(IContainer container, Project project)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Text(project.Name).Bold().FontSize(10 * FontSizeScale);
                if (project.StartDate.HasValue)
                    row.AutoItem().Text(FormatDateRange(project.StartDate, project.EndDate, project.IsOngoing))
                        .FontSize(8 * FontSizeScale).FontColor(Colors.Grey.Darken1);
            });

            if (!string.IsNullOrWhiteSpace(project.Description))
                column.Item().PaddingTop(2).Text(project.Description).FontSize(8 * FontSizeScale).LineHeight(LineSpacing);

            if (project.Technologies.Any())
            {
                column.Item().PaddingTop(2).Text(text =>
                {
                    text.Span("Technologies: ").Bold().FontSize(8 * FontSizeScale);
                    text.Span(string.Join(", ", project.Technologies)).FontSize(8 * FontSizeScale);
                });
            }

            if (project.Highlights.Any())
            {
                column.Item().PaddingTop(3).Column(hlCol =>
                {
                    foreach (var hl in project.Highlights)
                    {
                        hlCol.Item().Row(row =>
                        {
                            row.AutoItem().Text("• ").FontSize(8 * FontSizeScale);
                            row.RelativeItem().Text(hl).FontSize(8 * FontSizeScale).LineHeight(LineSpacing);
                        });
                    }
                });
            }
        });
    }
}
