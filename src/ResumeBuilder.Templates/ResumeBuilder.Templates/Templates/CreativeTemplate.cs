using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Templates.Templates;

public class CreativeTemplate : BaseTemplate
{
    public override TemplateInfo Info => new()
    {
        Id = "creative",
        Name = "Creative",
        Description = "Bold and eye-catching two-column design for creative professionals",
        Category = TemplateCategory.Creative,
        Layout = TemplateLayout.TwoColumn,
        Tags = new[] { "bold", "colorful", "designer" },
        DefaultAccentColor = "#7c3aed",
        DefaultFontFamily = "Arial"
    };

    protected override void ComposeContent(IContainer container, Resume resume)
    {
        container.Row(row =>
        {
            // Left sidebar
            row.ConstantItem(180).Background(ParseColor(AccentColor)).Padding(20).Column(sidebar =>
            {
                ComposeSidebar(sidebar, resume);
            });

            // Main content
            row.RelativeItem().PaddingLeft(25).Column(main =>
            {
                main.Spacing(SectionSpacing);
                ComposeMainContent(main, resume);
            });
        });
    }

    private void ComposeSidebar(ColumnDescriptor sidebar, Resume resume)
    {
        sidebar.Spacing(SectionSpacing);

        // Photo or initials
        sidebar.Item().AlignCenter().Element(c => ComposePhotoOrInitials(c, resume, 80, Colors.White, ParseColor(AccentColor)));

        // Contact info
        sidebar.Item().Column(col =>
        {
            col.Spacing(6);
            col.Item().Text("CONTACT").FontSize(10 * FontSizeScale).Bold().FontColor(Colors.White).LetterSpacing(0.1f);
            col.Item().Height(5);

            if (!string.IsNullOrWhiteSpace(resume.PersonalInfo.Email))
                col.Item().Text(resume.PersonalInfo.Email).FontSize(8 * FontSizeScale).FontColor(Colors.White);
            if (!string.IsNullOrWhiteSpace(resume.PersonalInfo.Phone))
                col.Item().Text(resume.PersonalInfo.Phone).FontSize(8 * FontSizeScale).FontColor(Colors.White);
            if (!string.IsNullOrWhiteSpace(resume.PersonalInfo.Location))
                col.Item().Text(resume.PersonalInfo.Location).FontSize(8 * FontSizeScale).FontColor(Colors.White);
            if (!string.IsNullOrWhiteSpace(resume.PersonalInfo.Website))
                col.Item().Text(resume.PersonalInfo.Website).FontSize(8 * FontSizeScale).FontColor(Colors.White);
            if (!string.IsNullOrWhiteSpace(resume.PersonalInfo.LinkedIn))
                col.Item().Text(resume.PersonalInfo.LinkedIn).FontSize(8 * FontSizeScale).FontColor(Colors.White);
        });

        // Skills
        if (resume.Skills.Any())
        {
            sidebar.Item().Column(col =>
            {
                col.Spacing(4);
                col.Item().Text("SKILLS").FontSize(10 * FontSizeScale).Bold().FontColor(Colors.White).LetterSpacing(0.1f);
                col.Item().Height(5);

                foreach (var skill in resume.Skills.OrderBy(s => s.Order).Take(10))
                {
                    col.Item().Row(skillRow =>
                    {
                        skillRow.RelativeItem().Text(skill.Name).FontSize(8 * FontSizeScale).FontColor(Colors.White);
                        skillRow.ConstantItem(50).Column(barCol =>
                        {
                            barCol.Item().Height(4).Background(Colors.White.WithAlpha(0.3f)).Row(bar =>
                            {
                                var percentage = (int)skill.Level * 20;
                                bar.RelativeItem(percentage).Background(Colors.White);
                                bar.RelativeItem(100 - percentage);
                            });
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
                col.Spacing(4);
                col.Item().Text("LANGUAGES").FontSize(10 * FontSizeScale).Bold().FontColor(Colors.White).LetterSpacing(0.1f);
                col.Item().Height(5);

                foreach (var lang in resume.Languages.OrderBy(l => l.Order))
                {
                    col.Item().Text($"{lang.Name} - {GetLanguageProficiencyText(lang.Proficiency)}")
                        .FontSize(8 * FontSizeScale).FontColor(Colors.White);
                }
            });
        }
    }

    private void ComposeMainContent(ColumnDescriptor main, Resume resume)
    {
        // Header
        main.Item().Column(header =>
        {
            header.Item().Text(resume.PersonalInfo.FullName.ToUpper())
                .FontSize(28 * FontSizeScale).Bold().FontFamily(HeadingFontFamily).FontColor(ParseColor(AccentColor)).LetterSpacing(0.05f);

            if (!string.IsNullOrWhiteSpace(resume.PersonalInfo.JobTitle))
            {
                header.Item().Text(resume.PersonalInfo.JobTitle.ToUpper())
                    .FontSize(12 * FontSizeScale).FontColor(Colors.Grey.Darken1).LetterSpacing(0.1f);
            }
        });

        // Summary
        if (!string.IsNullOrWhiteSpace(resume.Summary))
        {
            main.Item().Column(col =>
            {
                col.Item().Text("ABOUT ME").FontSize(11 * FontSizeScale).Bold().FontFamily(HeadingFontFamily).FontColor(ParseColor(AccentColor));
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
                col.Item().Text("EXPERIENCE").FontSize(11 * FontSizeScale).Bold().FontFamily(HeadingFontFamily).FontColor(ParseColor(AccentColor));

                foreach (var exp in resume.Experiences.OrderBy(e => e.Order))
                {
                    col.Item().Column(expCol =>
                    {
                        expCol.Item().Row(r =>
                        {
                            r.RelativeItem().Text(exp.JobTitle).Bold().FontSize(10 * FontSizeScale);
                            r.AutoItem().Text(exp.DateRange).FontSize(8 * FontSizeScale).FontColor(Colors.Grey.Darken1);
                        });
                        expCol.Item().Text(exp.Company).FontSize(9 * FontSizeScale).FontColor(ParseColor(AccentColor));

                        if (!string.IsNullOrWhiteSpace(exp.Description))
                            expCol.Item().PaddingTop(3).Text(exp.Description).FontSize(8 * FontSizeScale).LineHeight(LineSpacing);

                        if (exp.Achievements.Any())
                        {
                            expCol.Item().PaddingTop(3).Column(achCol =>
                            {
                                foreach (var ach in exp.Achievements)
                                    achCol.Item().Text($"→ {ach}").FontSize(8 * FontSizeScale).LineHeight(LineSpacing);
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
                col.Item().Text("EDUCATION").FontSize(11 * FontSizeScale).Bold().FontFamily(HeadingFontFamily).FontColor(ParseColor(AccentColor));

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
                col.Item().Text("PROJECTS").FontSize(11 * FontSizeScale).Bold().FontFamily(HeadingFontFamily).FontColor(ParseColor(AccentColor));

                foreach (var proj in resume.Projects.OrderBy(p => p.Order))
                {
                    col.Item().Column(projCol =>
                    {
                        projCol.Item().Text(proj.Name).Bold().FontSize(10 * FontSizeScale);
                        if (!string.IsNullOrWhiteSpace(proj.Description))
                            projCol.Item().Text(proj.Description).FontSize(8 * FontSizeScale).LineHeight(LineSpacing);
                        if (proj.Technologies.Any())
                            projCol.Item().Text($"Tech: {string.Join(", ", proj.Technologies)}").FontSize(8 * FontSizeScale).FontColor(Colors.Grey.Darken1);
                    });
                }
            });
        }

        // Certifications
        if (resume.Certifications.Any())
        {
            main.Item().Column(col =>
            {
                col.Spacing(8);
                col.Item().Text("CERTIFICATIONS").FontSize(11 * FontSizeScale).Bold().FontFamily(HeadingFontFamily).FontColor(ParseColor(AccentColor));

                foreach (var cert in resume.Certifications.OrderBy(c => c.Order))
                {
                    col.Item().Column(certCol =>
                    {
                        certCol.Item().Row(r =>
                        {
                            r.RelativeItem().Text(cert.Name).Bold().FontSize(10 * FontSizeScale);
                            if (cert.IssueDate.HasValue)
                                r.AutoItem().Text(cert.IssueDate.Value.ToString("MMM yyyy")).FontSize(8 * FontSizeScale).FontColor(Colors.Grey.Darken1);
                        });
                        if (!string.IsNullOrWhiteSpace(cert.IssuingOrganization))
                            certCol.Item().Text(cert.IssuingOrganization).FontSize(9 * FontSizeScale).FontColor(ParseColor(AccentColor));
                    });
                }
            });
        }
    }
}
