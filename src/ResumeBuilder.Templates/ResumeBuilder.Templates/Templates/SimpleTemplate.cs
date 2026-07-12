using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Templates.Templates;

public class SimpleTemplate : BaseTemplate
{
    public override TemplateInfo Info => new()
    {
        Id = "simple",
        Name = "Simple",
        Description = "Straightforward and easy-to-read format for all purposes",
        Category = TemplateCategory.Classic,
        Layout = TemplateLayout.SingleColumn,
        Tags = new[] { "basic", "readable", "straightforward" },
        DefaultAccentColor = "#3b82f6",
        DefaultFontFamily = "Arial"
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
                        column.Item().Text(resume.Summary).FontSize(10 * FontSizeScale).LineHeight(LineSpacing);
                        column.Item().Height(5);
                        break;

                    case SectionType.Experience:
                        column.Item().Column(col =>
                        {
                            col.Item().Text("EXPERIENCE").FontSize(11 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor));
                            col.Item().Height(8);

                            foreach (var exp in resume.Experiences.OrderBy(e => e.Order))
                            {
                                col.Item().PaddingBottom(10).Column(expCol =>
                                {
                                    expCol.Item().Text(exp.JobTitle).Bold().FontSize(10 * FontSizeScale);
                                    expCol.Item().Text($"{exp.Company} | {exp.DateRange}").FontSize(9 * FontSizeScale).FontColor(Colors.Grey.Darken1);

                                    if (!string.IsNullOrWhiteSpace(exp.Description))
                                        expCol.Item().PaddingTop(3).Text(exp.Description).FontSize(9 * FontSizeScale).LineHeight(LineSpacing);

                                    if (exp.Achievements.Any())
                                    {
                                        expCol.Item().PaddingTop(3).Column(achCol =>
                                        {
                                            foreach (var ach in exp.Achievements)
                                                achCol.Item().Text($"- {ach}").FontSize(9 * FontSizeScale).LineHeight(LineSpacing);
                                        });
                                    }
                                });
                            }
                        });
                        break;

                    case SectionType.Education:
                        column.Item().Column(col =>
                        {
                            col.Item().Text("EDUCATION").FontSize(11 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor));
                            col.Item().Height(8);

                            foreach (var edu in resume.EducationList.OrderBy(e => e.Order))
                            {
                                col.Item().PaddingBottom(8).Column(eduCol =>
                                {
                                    eduCol.Item().Text(edu.DegreeWithField).Bold().FontSize(10 * FontSizeScale);
                                    eduCol.Item().Text($"{edu.Institution} | {edu.DateRange}").FontSize(9 * FontSizeScale).FontColor(Colors.Grey.Darken1);
                                });
                            }
                        });
                        break;

                    case SectionType.Skills:
                        column.Item().Column(col =>
                        {
                            col.Item().Text("SKILLS").FontSize(11 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor));
                            col.Item().Height(5);
                            col.Item().Text(string.Join(", ", resume.Skills.OrderBy(s => s.Order).Select(s => s.Name))).FontSize(9 * FontSizeScale);
                        });
                        break;

                    case SectionType.Languages:
                        column.Item().Column(col =>
                        {
                            col.Item().Text("LANGUAGES").FontSize(11 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor));
                            col.Item().Height(5);
                            foreach (var lang in resume.Languages.OrderBy(l => l.Order))
                                col.Item().Text($"- {lang.Name} ({GetLanguageProficiencyText(lang.Proficiency)})").FontSize(9 * FontSizeScale);
                        });
                        break;

                    case SectionType.Certifications:
                        column.Item().Column(col =>
                        {
                            col.Item().Text("CERTIFICATIONS").FontSize(11 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor));
                            col.Item().Height(5);
                            foreach (var cert in resume.Certifications.OrderBy(c => c.Order))
                            {
                                col.Item().Text(text =>
                                {
                                    text.DefaultTextStyle(x => x.FontSize(9 * FontSizeScale));
                                    text.Span($"- {cert.Name}").Bold();
                                    if (!string.IsNullOrWhiteSpace(cert.IssuingOrganization))
                                        text.Span($", {cert.IssuingOrganization}");
                                    if (cert.IssueDate.HasValue)
                                        text.Span($" ({ResumeDateFormat.Year(cert.IssueDate)})");
                                });
                            }
                        });
                        break;

                    case SectionType.Projects:
                        column.Item().Column(col =>
                        {
                            col.Item().Text("PROJECTS").FontSize(11 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor));
                            col.Item().Height(8);

                            foreach (var project in resume.Projects.OrderBy(p => p.Order))
                            {
                                col.Item().PaddingBottom(10).Column(projCol =>
                                {
                                    projCol.Item().Text(project.Name).Bold().FontSize(10 * FontSizeScale);
                                    if (project.StartDate.HasValue)
                                        projCol.Item().Text(FormatDateRange(project.StartDate, project.EndDate, project.IsOngoing))
                                            .FontSize(9 * FontSizeScale).FontColor(Colors.Grey.Darken1);

                                    if (!string.IsNullOrWhiteSpace(project.Description))
                                        projCol.Item().PaddingTop(3).Text(project.Description).FontSize(9 * FontSizeScale).LineHeight(LineSpacing);

                                    if (project.Technologies.Any())
                                    {
                                        projCol.Item().PaddingTop(2).Text(text =>
                                        {
                                            text.Span("Tech: ").Bold().FontSize(9 * FontSizeScale);
                                            text.Span(string.Join(", ", project.Technologies)).FontSize(9 * FontSizeScale);
                                        });
                                    }

                                    if (project.Highlights.Any())
                                    {
                                        projCol.Item().PaddingTop(3).Column(hlCol =>
                                        {
                                            foreach (var highlight in project.Highlights)
                                                hlCol.Item().Text($"- {highlight}").FontSize(9 * FontSizeScale).LineHeight(LineSpacing);
                                        });
                                    }
                                });
                            }
                        });
                        break;

                    case SectionType.CustomSections:
                        foreach (var custom in GetVisibleCustomSections(resume))
                        {
                            column.Item().Column(col =>
                            {
                                col.Item().Text(custom.Title.ToUpper()).FontSize(11 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor));
                                col.Item().Height(8);
                                col.Item().Column(ct => ComposeCustomSectionItems(ct, custom, 10, 9));
                            });
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
            column.Item().Text(info.FullName).FontSize(22 * FontSizeScale).Bold();

            if (!string.IsNullOrWhiteSpace(info.JobTitle))
                column.Item().Text(info.JobTitle).FontSize(12 * FontSizeScale).FontColor(Colors.Grey.Darken2);

            column.Item().Height(8);

            var contacts = new List<string>();
            if (!string.IsNullOrWhiteSpace(info.Email)) contacts.Add(info.Email);
            if (!string.IsNullOrWhiteSpace(info.Phone)) contacts.Add(info.Phone);
            if (!string.IsNullOrWhiteSpace(info.Location)) contacts.Add(info.Location);
            if (!string.IsNullOrWhiteSpace(info.LinkedIn)) contacts.Add(FormatLinkedInDisplay(info.LinkedIn));
            if (!string.IsNullOrWhiteSpace(info.GitHub)) contacts.Add($"github.com/{FormatGitHubDisplay(info.GitHub)}");
            if (!string.IsNullOrWhiteSpace(info.Website)) contacts.Add(info.Website);

            column.Item().Text(string.Join(" | ", contacts)).FontSize(9 * FontSizeScale);

            column.Item().Height(5);
            column.Item().LineHorizontal(1).LineColor(ParseColor(AccentColor));
        });
    }
}
