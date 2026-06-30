using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Templates.Templates;

public class TwoColumnTemplate : BaseTemplate
{
    public override TemplateInfo Info => new()
    {
        Id = "two-column",
        Name = "Two Column",
        Description = "Efficient two-column layout with sidebar for contact and skills",
        Category = TemplateCategory.Professional,
        Layout = TemplateLayout.Sidebar,
        Tags = new[] { "efficient", "organized", "sidebar" },
        DefaultAccentColor = "#0284c7",
        DefaultFontFamily = "Arial"
    };

    protected override void ComposeContent(IContainer container, Resume resume)
    {
        container.Row(row =>
        {
            // Left sidebar (lighter)
            row.ConstantItem(165).Background(Colors.Grey.Lighten4).Padding(15).Column(sidebar =>
            {
                ComposeSidebar(sidebar, resume);
            });

            // Main content
            row.RelativeItem().PaddingLeft(20).Column(main =>
            {
                main.Spacing(SectionSpacing);
                ComposeMainContent(main, resume);
            });
        });
    }

    private void ComposeSidebar(ColumnDescriptor sidebar, Resume resume)
    {
        sidebar.Spacing(SectionSpacing);

        // Name (in sidebar)
        sidebar.Item().Column(col =>
        {
            col.Item().Text(resume.PersonalInfo.FirstName).FontSize(18 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor));
            col.Item().Text(resume.PersonalInfo.LastName).FontSize(18 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor));

            if (!string.IsNullOrWhiteSpace(resume.PersonalInfo.JobTitle))
            {
                col.Item().Height(5);
                col.Item().Text(resume.PersonalInfo.JobTitle).FontSize(9 * FontSizeScale).FontColor(Colors.Grey.Darken2);
            }
        });

        // Contact
        sidebar.Item().Column(col =>
        {
            col.Item().Text("CONTACT").FontSize(9 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor)).LetterSpacing(0.1f);
            col.Item().Height(5);
            col.Item().LineHorizontal(1).LineColor(ParseColor(AccentColor));
            col.Item().Height(5);

            if (!string.IsNullOrWhiteSpace(resume.PersonalInfo.Email))
                col.Item().PaddingBottom(3).Text(resume.PersonalInfo.Email).FontSize(8 * FontSizeScale);
            if (!string.IsNullOrWhiteSpace(resume.PersonalInfo.Phone))
                col.Item().PaddingBottom(3).Text(resume.PersonalInfo.Phone).FontSize(8 * FontSizeScale);
            if (!string.IsNullOrWhiteSpace(resume.PersonalInfo.Location))
                col.Item().PaddingBottom(3).Text(resume.PersonalInfo.Location).FontSize(8 * FontSizeScale);
            if (!string.IsNullOrWhiteSpace(resume.PersonalInfo.Website))
                col.Item().PaddingBottom(3).Text(resume.PersonalInfo.Website).FontSize(8 * FontSizeScale);
            if (!string.IsNullOrWhiteSpace(resume.PersonalInfo.LinkedIn))
                col.Item().PaddingBottom(3).Text(resume.PersonalInfo.LinkedIn).FontSize(8 * FontSizeScale);
            if (!string.IsNullOrWhiteSpace(resume.PersonalInfo.GitHub))
                col.Item().PaddingBottom(3).Text(resume.PersonalInfo.GitHub).FontSize(8 * FontSizeScale);
        });

        // Skills
        if (resume.Skills.Any())
        {
            sidebar.Item().Column(col =>
            {
                col.Item().Text("SKILLS").FontSize(9 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor)).LetterSpacing(0.1f);
                col.Item().Height(5);
                col.Item().LineHorizontal(1).LineColor(ParseColor(AccentColor));
                col.Item().Height(5);

                var grouped = resume.Skills.GroupBy(s => s.Category).ToList();

                foreach (var group in grouped)
                {
                    if (!string.IsNullOrWhiteSpace(group.Key))
                    {
                        col.Item().PaddingTop(4).Text(group.Key).FontSize(8 * FontSizeScale).Bold();
                    }

                    foreach (var skill in group)
                    {
                        col.Item().PaddingBottom(4).Column(skillCol =>
                        {
                            skillCol.Item().Text(skill.Name).FontSize(8 * FontSizeScale);
                            skillCol.Item().Height(3);
                            skillCol.Item().Height(4).Width(130).Background(Colors.Grey.Lighten2)
                                .Row(bar =>
                                {
                                    var pct = (int)skill.Level * 20;
                                    bar.RelativeItem(pct).Background(ParseColor(AccentColor));
                                    bar.RelativeItem(100 - pct);
                                });
                        });
                    }
                }
            });
        }

        // Languages
        if (resume.Languages.Any())
        {
            sidebar.Item().Column(col =>
            {
                col.Item().Text("LANGUAGES").FontSize(9 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor)).LetterSpacing(0.1f);
                col.Item().Height(5);
                col.Item().LineHorizontal(1).LineColor(ParseColor(AccentColor));
                col.Item().Height(5);

                foreach (var lang in resume.Languages.OrderBy(l => l.Order))
                {
                    col.Item().PaddingBottom(3).Text(text =>
                    {
                        text.Span(lang.Name).FontSize(8 * FontSizeScale);
                        text.Span($" - {GetLanguageProficiencyText(lang.Proficiency)}").FontSize(7 * FontSizeScale).FontColor(Colors.Grey.Darken1);
                    });
                }
            });
        }

        // Certifications
        if (resume.Certifications.Any())
        {
            sidebar.Item().Column(col =>
            {
                col.Item().Text("CERTIFICATIONS").FontSize(9 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor)).LetterSpacing(0.1f);
                col.Item().Height(5);
                col.Item().LineHorizontal(1).LineColor(ParseColor(AccentColor));
                col.Item().Height(5);

                foreach (var cert in resume.Certifications.OrderBy(c => c.Order).Take(4))
                {
                    col.Item().PaddingBottom(5).Column(certCol =>
                    {
                        certCol.Item().Text(cert.Name).FontSize(8 * FontSizeScale).Bold();
                        certCol.Item().Text(cert.IssuingOrganization).FontSize(7 * FontSizeScale);
                        if (cert.IssueDate.HasValue)
                            certCol.Item().Text(cert.IssueDate.Value.ToString("MMM yyyy")).FontSize(7 * FontSizeScale).FontColor(Colors.Grey.Darken1);
                    });
                }
            });
        }
    }

    private void ComposeMainContent(ColumnDescriptor main, Resume resume)
    {
        // Summary
        if (!string.IsNullOrWhiteSpace(resume.Summary))
        {
            main.Item().Column(col =>
            {
                col.Item().Text("PROFILE").FontSize(11 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor));
                col.Item().Height(5);
                col.Item().Text(resume.Summary).FontSize(9 * FontSizeScale).LineHeight(LineSpacing);
            });
        }

        // Experience
        if (resume.Experiences.Any())
        {
            main.Item().Column(col =>
            {
                col.Spacing(10);
                col.Item().Text("EXPERIENCE").FontSize(11 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor));

                foreach (var exp in resume.Experiences.OrderBy(e => e.Order))
                {
                    col.Item().Column(expCol =>
                    {
                        expCol.Item().Row(r =>
                        {
                            r.RelativeItem().Text(exp.JobTitle).Bold().FontSize(10 * FontSizeScale);
                            r.AutoItem().Text(exp.DateRange).FontSize(8 * FontSizeScale).FontColor(Colors.Grey.Darken1);
                        });

                        expCol.Item().Text(text =>
                        {
                            text.DefaultTextStyle(x => x.FontSize(9 * FontSizeScale));
                            text.Span(exp.Company).SemiBold().FontColor(ParseColor(AccentColor));
                            if (!string.IsNullOrWhiteSpace(exp.Location))
                                text.Span($" | {exp.Location}").FontColor(Colors.Grey.Darken1);
                        });

                        if (!string.IsNullOrWhiteSpace(exp.Description))
                            expCol.Item().PaddingTop(4).Text(exp.Description).FontSize(8 * FontSizeScale).LineHeight(LineSpacing);

                        if (exp.Achievements.Any())
                        {
                            expCol.Item().PaddingTop(3).Column(achCol =>
                            {
                                foreach (var ach in exp.Achievements)
                                    achCol.Item().Text($"• {ach}").FontSize(8 * FontSizeScale).LineHeight(LineSpacing);
                            });
                        }
                    });
                }
            });
        }

        // Education
        if (resume.EducationList.Any())
        {
            main.Item().Column(col =>
            {
                col.Spacing(8);
                col.Item().Text("EDUCATION").FontSize(11 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor));

                foreach (var edu in resume.EducationList.OrderBy(e => e.Order))
                {
                    col.Item().Column(eduCol =>
                    {
                        eduCol.Item().Row(r =>
                        {
                            r.RelativeItem().Text(edu.DegreeWithField).Bold().FontSize(10 * FontSizeScale);
                            r.AutoItem().Text(edu.DateRange).FontSize(8 * FontSizeScale).FontColor(Colors.Grey.Darken1);
                        });

                        eduCol.Item().Text(edu.Institution).FontSize(9 * FontSizeScale).FontColor(ParseColor(AccentColor));

                        if (!string.IsNullOrWhiteSpace(edu.Grade))
                            eduCol.Item().Text($"Grade: {edu.Grade}").FontSize(8 * FontSizeScale);
                    });
                }
            });
        }

        // Projects
        if (resume.Projects.Any())
        {
            main.Item().Column(col =>
            {
                col.Spacing(8);
                col.Item().Text("PROJECTS").FontSize(11 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor));

                foreach (var proj in resume.Projects.OrderBy(p => p.Order))
                {
                    col.Item().Column(projCol =>
                    {
                        projCol.Item().Text(proj.Name).Bold().FontSize(10 * FontSizeScale);
                        if (!string.IsNullOrWhiteSpace(proj.Description))
                            projCol.Item().Text(proj.Description).FontSize(8 * FontSizeScale).LineHeight(LineSpacing);
                        if (proj.Technologies.Any())
                            projCol.Item().Text($"Technologies: {string.Join(", ", proj.Technologies)}").FontSize(7 * FontSizeScale).FontColor(Colors.Grey.Darken1);
                    });
                }
            });
        }
    }
}
