using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Templates.Templates;

public class TechnicalTemplate : BaseTemplate
{
    public override TemplateInfo Info => new()
    {
        Id = "technical",
        Name = "Technical",
        Description = "Code-inspired design with skill badges for developers and engineers",
        Category = TemplateCategory.Technical,
        Layout = TemplateLayout.TwoColumn,
        Tags = new[] { "developer", "engineering", "tech" },
        DefaultAccentColor = "#059669",
        DefaultFontFamily = "Consolas"
    };

    protected override void ComposeContent(IContainer container, Resume resume)
    {
        container.Row(row =>
        {
            // Sidebar
            row.ConstantItem(170).Background(Colors.Grey.Lighten4).Padding(15).Column(sidebar =>
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
        sidebar.Spacing(15);

        // Contact
        sidebar.Item().Column(col =>
        {
            col.Item().Text("// CONTACT").FontSize(9 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor));
            col.Item().Height(6);

            if (!string.IsNullOrWhiteSpace(resume.PersonalInfo.Email))
                col.Item().Text(resume.PersonalInfo.Email).FontSize(8 * FontSizeScale);
            if (!string.IsNullOrWhiteSpace(resume.PersonalInfo.Phone))
                col.Item().Text(resume.PersonalInfo.Phone).FontSize(8 * FontSizeScale);
            if (!string.IsNullOrWhiteSpace(resume.PersonalInfo.Location))
                col.Item().Text(resume.PersonalInfo.Location).FontSize(8 * FontSizeScale);
            if (!string.IsNullOrWhiteSpace(resume.PersonalInfo.GitHub))
                col.Item().Text($"github.com/{resume.PersonalInfo.GitHub}").FontSize(8 * FontSizeScale);
            if (!string.IsNullOrWhiteSpace(resume.PersonalInfo.LinkedIn))
                col.Item().Text(resume.PersonalInfo.LinkedIn).FontSize(8 * FontSizeScale);
            if (!string.IsNullOrWhiteSpace(resume.PersonalInfo.Website))
                col.Item().Text(resume.PersonalInfo.Website).FontSize(8 * FontSizeScale);
        });

        // Skills with badges
        if (resume.Skills.Any())
        {
            sidebar.Item().Column(col =>
            {
                col.Item().Text("// TECH STACK").FontSize(9 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor));
                col.Item().Height(6);

                var grouped = resume.Skills.GroupBy(s => s.Category).ToList();

                foreach (var group in grouped)
                {
                    if (!string.IsNullOrWhiteSpace(group.Key))
                    {
                        col.Item().PaddingTop(4).Text(group.Key).FontSize(8 * FontSizeScale).Bold();
                    }

                    col.Item().PaddingTop(2).Row(row =>
                    {
                        row.Spacing(4);
                        row.AutoItem().Column(badgeCol =>
                        {
                            int count = 0;
                            foreach (var skill in group)
                            {
                                if (count % 2 == 0 && count > 0)
                                {
                                    // New row handled by wrapping
                                }

                                badgeCol.Item().PaddingBottom(3).Container()
                                    .Background(ParseColor(AccentColor))
                                    .Padding(3)
                                    .Text(skill.Name)
                                    .FontSize(7 * FontSizeScale)
                                    .FontColor(Colors.White);

                                count++;
                            }
                        });
                    });
                }
            });
        }

        // Languages
        if (resume.Languages.Any())
        {
            sidebar.Item().Column(col =>
            {
                col.Item().Text("// LANGUAGES").FontSize(9 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor));
                col.Item().Height(6);

                foreach (var lang in resume.Languages.OrderBy(l => l.Order))
                {
                    col.Item().Text($"{lang.Name}: {GetLanguageProficiencyText(lang.Proficiency)}").FontSize(8 * FontSizeScale);
                }
            });
        }

        // Certifications
        if (resume.Certifications.Any())
        {
            sidebar.Item().Column(col =>
            {
                col.Item().Text("// CERTIFICATIONS").FontSize(9 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor));
                col.Item().Height(6);

                foreach (var cert in resume.Certifications.OrderBy(c => c.Order).Take(5))
                {
                    col.Item().PaddingBottom(3).Column(certCol =>
                    {
                        certCol.Item().Text(cert.Name).FontSize(8 * FontSizeScale).Bold();
                        certCol.Item().Text(cert.IssuingOrganization).FontSize(7 * FontSizeScale).FontColor(Colors.Grey.Darken1);
                    });
                }
            });
        }
    }

    private void ComposeMainContent(ColumnDescriptor main, Resume resume)
    {
        // Header
        main.Item().Column(header =>
        {
            header.Item().Text(resume.PersonalInfo.FullName)
                .FontSize(24 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor));

            if (!string.IsNullOrWhiteSpace(resume.PersonalInfo.JobTitle))
            {
                header.Item().Text($"<{resume.PersonalInfo.JobTitle} />")
                    .FontSize(12 * FontSizeScale).FontColor(Colors.Grey.Darken2);
            }
        });

        // Summary
        if (!string.IsNullOrWhiteSpace(resume.Summary))
        {
            main.Item().Column(col =>
            {
                col.Item().Text("/* About */").FontSize(10 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor));
                col.Item().Height(4);
                col.Item().Text(resume.Summary).FontSize(9 * FontSizeScale).LineHeight(LineSpacing);
            });
        }

        // Experience
        if (resume.Experiences.Any())
        {
            main.Item().Column(col =>
            {
                col.Spacing(12);
                col.Item().Text("/* Experience */").FontSize(10 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor));

                foreach (var exp in resume.Experiences.OrderBy(e => e.Order))
                {
                    col.Item().Column(expCol =>
                    {
                        expCol.Item().Row(r =>
                        {
                            r.RelativeItem().Text(exp.JobTitle).Bold().FontSize(11 * FontSizeScale);
                            r.AutoItem().Text(exp.DateRange).FontSize(8 * FontSizeScale).FontColor(Colors.Grey.Darken1);
                        });

                        expCol.Item().Text($"@{exp.Company}").FontSize(9 * FontSizeScale).FontColor(ParseColor(AccentColor));

                        if (!string.IsNullOrWhiteSpace(exp.Description))
                            expCol.Item().PaddingTop(4).Text(exp.Description).FontSize(9 * FontSizeScale).LineHeight(LineSpacing);

                        if (exp.Achievements.Any())
                        {
                            expCol.Item().PaddingTop(4).Column(achCol =>
                            {
                                foreach (var ach in exp.Achievements)
                                    achCol.Item().Text($"→ {ach}").FontSize(8 * FontSizeScale);
                            });
                        }
                    });
                }
            });
        }

        // Projects
        if (resume.Projects.Any())
        {
            main.Item().Column(col =>
            {
                col.Spacing(10);
                col.Item().Text("/* Projects */").FontSize(10 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor));

                foreach (var proj in resume.Projects.OrderBy(p => p.Order))
                {
                    col.Item().Column(projCol =>
                    {
                        projCol.Item().Text(proj.Name).Bold().FontSize(10 * FontSizeScale);

                        if (!string.IsNullOrWhiteSpace(proj.Description))
                            projCol.Item().Text(proj.Description).FontSize(8 * FontSizeScale).LineHeight(LineSpacing);

                        if (proj.Technologies.Any())
                        {
                            projCol.Item().PaddingTop(3).Row(techRow =>
                            {
                                techRow.Spacing(4);
                                foreach (var tech in proj.Technologies.Take(6))
                                {
                                    techRow.AutoItem()
                                        .Border(1).BorderColor(ParseColor(AccentColor))
                                        .Padding(2)
                                        .Text(tech).FontSize(7 * FontSizeScale).FontColor(ParseColor(AccentColor));
                                }
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
                col.Item().Text("/* Education */").FontSize(10 * FontSizeScale).Bold().FontColor(ParseColor(AccentColor));

                foreach (var edu in resume.EducationList.OrderBy(e => e.Order))
                {
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Column(eduCol =>
                        {
                            eduCol.Item().Text(edu.DegreeWithField).Bold().FontSize(10 * FontSizeScale);
                            eduCol.Item().Text(edu.Institution).FontSize(9 * FontSizeScale);
                        });
                        r.AutoItem().Text(edu.DateRange).FontSize(8 * FontSizeScale).FontColor(Colors.Grey.Darken1);
                    });
                }
            });
        }
    }
}
