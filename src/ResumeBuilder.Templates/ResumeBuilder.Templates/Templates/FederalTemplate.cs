using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Templates.Templates;

/// <summary>
/// A long-form public-sector CV. Government reviewers score against a posting line by line, so
/// nothing is abbreviated: every role carries its employer, location, month-precision dates and the
/// complete list of achievements, and every credential carries its identifier. Verbosity is the
/// feature — this template is expected to run to several pages.
/// </summary>
public class FederalTemplate : BaseTemplate
{
    public override TemplateInfo Info => new()
    {
        Id = "federal",
        Name = "Federal / Government",
        Description = "Long-form public-sector CV with full detail per role and no truncation anywhere",
        Category = TemplateCategory.Professional,
        Layout = TemplateLayout.SingleColumn,
        Tags = new[] { "federal", "government", "public sector", "detailed", "long-form" },
        DefaultAccentColor = "#1e3a5f",
        DefaultFontFamily = "Times New Roman"
    };

    private const float LabelWidth = 95;

    private static readonly EntryStyle Entry = new()
    {
        TitleFontSize = 11,
        SubtitleFontSize = 10,
        BodyFontSize = 10,
        MetaFontSize = 10,
        Bullet = "• "
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
                        column.Item().Element(c => ComposeSection(c, "PROFESSIONAL SUMMARY", col =>
                            col.Item().Text(resume.Summary).FontSize(10 * FontSizeScale).LineHeight(LineSpacing)));
                        break;

                    case SectionType.Experience:
                        column.Item().Element(c => ComposeSection(c, "PROFESSIONAL EXPERIENCE", col =>
                        {
                            foreach (var exp in resume.Experiences.OrderBy(e => e.Order))
                                col.Item().PaddingBottom(12).Element(e => ComposeExperience(e, exp));
                        }));
                        break;

                    case SectionType.Education:
                        column.Item().Element(c => ComposeSection(c, "EDUCATION", col =>
                        {
                            foreach (var edu in resume.EducationList.OrderBy(e => e.Order))
                                col.Item().PaddingBottom(10).Element(e => ComposeEducation(e, edu));
                        }));
                        break;

                    case SectionType.Skills:
                        column.Item().Element(c => ComposeSection(c, "SKILLS AND COMPETENCIES", col =>
                            ComposeSkills(col, resume.Skills)));
                        break;

                    case SectionType.Languages:
                        column.Item().Element(c => ComposeSection(c, "LANGUAGE PROFICIENCY", col =>
                        {
                            foreach (var lang in resume.Languages.OrderBy(l => l.Order))
                                col.Item().Element(e => ComposeLabelledLine(e, lang.Name, GetLanguageProficiencyText(lang.Proficiency)));
                        }));
                        break;

                    case SectionType.Certifications:
                        column.Item().Element(c => ComposeSection(c, "CERTIFICATIONS AND LICENSES", col =>
                        {
                            foreach (var cert in resume.Certifications.OrderBy(c2 => c2.Order))
                                col.Item().PaddingBottom(8).Element(e => ComposeCertification(e, cert));
                        }));
                        break;

                    case SectionType.Projects:
                        column.Item().Element(c => ComposeSection(c, "PROJECTS", col =>
                        {
                            foreach (var project in resume.Projects.OrderBy(p => p.Order))
                                col.Item().PaddingBottom(10).Element(e => ComposeProjectEntry(e, project, Entry));
                        }));
                        break;

                    case SectionType.CustomSections:
                        foreach (var custom in GetVisibleCustomSections(resume))
                        {
                            column.Item().Element(c => ComposeSection(c, custom.Title.ToUpperInvariant(), col =>
                                ComposeCustomSectionItems(col, custom, 11, 10)));
                        }
                        break;
                }
            }
        });
    }

    private void ComposeHeader(IContainer container, PersonalInfo info)
    {
        var accent = ParseColor(AccentColor);

        container.Column(column =>
        {
            column.Item().AlignCenter().Text(info.FullName)
                .FontSize(20 * FontSizeScale)
                .Bold()
                .FontFamily(HeadingFontFamily)
                .FontColor(accent);

            if (!string.IsNullOrWhiteSpace(info.JobTitle))
                column.Item().AlignCenter().Text(info.JobTitle).FontSize(12 * FontSizeScale);

            column.Item().Height(6);

            // The full mailing address is spelled out: government forms expect it.
            var address = string.Join(", ", new[] { info.Address, info.City, info.PostalCode, info.Country }
                .Where(part => !string.IsNullOrWhiteSpace(part)));

            if (!string.IsNullOrWhiteSpace(address))
                column.Item().AlignCenter().Text(address).FontSize(10 * FontSizeScale);

            var contacts = new[] { info.Phone, info.Email }
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .ToList();

            if (contacts.Count > 0)
                column.Item().AlignCenter().Text(string.Join("  |  ", contacts)).FontSize(10 * FontSizeScale);

            var links = new List<string>();
            if (!string.IsNullOrWhiteSpace(info.LinkedIn)) links.Add(FormatLinkedInDisplay(info.LinkedIn));
            if (!string.IsNullOrWhiteSpace(info.GitHub)) links.Add($"github.com/{FormatGitHubDisplay(info.GitHub)}");
            if (!string.IsNullOrWhiteSpace(info.Website)) links.Add(FormatWebsiteDisplay(info.Website));

            if (links.Count > 0)
                column.Item().AlignCenter().Text(string.Join("  |  ", links)).FontSize(10 * FontSizeScale);

            column.Item().Height(8);
            column.Item().LineHorizontal(1.5f).LineColor(accent);
        });
    }

    private void ComposeSection(IContainer container, string title, Action<ColumnDescriptor> content)
    {
        var accent = ParseColor(AccentColor);

        container.Column(column =>
        {
            column.Item().Text(title)
                .FontSize(12 * FontSizeScale)
                .Bold()
                .FontFamily(HeadingFontFamily)
                .FontColor(accent);

            column.Item().Height(3);
            column.Item().LineHorizontal(0.75f).LineColor(accent);
            column.Item().Height(8);
            column.Item().Column(content);
        });
    }

    /// <summary>A "Label:  value" row; the labelled form is what makes the detail scannable.</summary>
    private void ComposeLabelledLine(IContainer container, string label, string value)
    {
        container.Row(row =>
        {
            row.ConstantItem(LabelWidth).Text($"{label}:").SemiBold().FontSize(10 * FontSizeScale);
            row.RelativeItem().Text(value).FontSize(10 * FontSizeScale).LineHeight(LineSpacing);
        });
    }

    private void ComposeExperience(IContainer container, Experience exp)
    {
        container.Column(column =>
        {
            column.Item().Text(exp.JobTitle)
                .Bold()
                .FontSize(12 * FontSizeScale)
                .FontFamily(HeadingFontFamily);

            column.Item().Height(3);

            if (!string.IsNullOrWhiteSpace(exp.Company))
                column.Item().Element(c => ComposeLabelledLine(c, "Employer", exp.Company));

            if (!string.IsNullOrWhiteSpace(exp.Location))
                column.Item().Element(c => ComposeLabelledLine(c, "Location", exp.Location));

            if (!string.IsNullOrWhiteSpace(exp.DateRange))
                column.Item().Element(c => ComposeLabelledLine(c, "Dates", exp.DateRange));

            if (!string.IsNullOrWhiteSpace(exp.Description))
            {
                column.Item().PaddingTop(5).Text("Duties and Responsibilities")
                    .SemiBold().FontSize(10 * FontSizeScale);
                column.Item().Text(exp.Description).FontSize(10 * FontSizeScale).LineHeight(LineSpacing);
            }

            if (exp.Achievements.Any())
            {
                column.Item().PaddingTop(5).Text("Key Accomplishments")
                    .SemiBold().FontSize(10 * FontSizeScale);
                column.Item().PaddingTop(2).Element(c => ComposeBulletList(c, exp.Achievements, Entry));
            }
        });
    }

    private void ComposeEducation(IContainer container, Education edu)
    {
        container.Column(column =>
        {
            column.Item().Text(edu.DegreeWithField)
                .Bold()
                .FontSize(11 * FontSizeScale)
                .FontFamily(HeadingFontFamily);

            column.Item().Height(3);

            if (!string.IsNullOrWhiteSpace(edu.Institution))
                column.Item().Element(c => ComposeLabelledLine(c, "Institution", edu.Institution));

            if (!string.IsNullOrWhiteSpace(edu.Location))
                column.Item().Element(c => ComposeLabelledLine(c, "Location", edu.Location));

            if (!string.IsNullOrWhiteSpace(edu.DateRange))
                column.Item().Element(c => ComposeLabelledLine(c, "Dates", edu.DateRange));

            if (!string.IsNullOrWhiteSpace(edu.Grade))
                column.Item().Element(c => ComposeLabelledLine(c, "Grade", edu.Grade));

            if (!string.IsNullOrWhiteSpace(edu.Description))
                column.Item().PaddingTop(3).Text(edu.Description).FontSize(10 * FontSizeScale).LineHeight(LineSpacing);
        });
    }

    private void ComposeCertification(IContainer container, Certification cert)
    {
        container.Column(column =>
        {
            column.Item().Text(cert.Name).SemiBold().FontSize(11 * FontSizeScale);

            if (!string.IsNullOrWhiteSpace(cert.IssuingOrganization))
                column.Item().Element(c => ComposeLabelledLine(c, "Issuer", cert.IssuingOrganization));

            if (cert.IssueDate.HasValue)
                column.Item().Element(c => ComposeLabelledLine(c, "Issued", ResumeDateFormat.MonthYear(cert.IssueDate)));

            var expiry = cert.DoesNotExpire
                ? "Does not expire"
                : ResumeDateFormat.MonthYear(cert.ExpirationDate);

            if (!string.IsNullOrWhiteSpace(expiry))
                column.Item().Element(c => ComposeLabelledLine(c, "Expires", expiry));

            if (!string.IsNullOrWhiteSpace(cert.CredentialId))
                column.Item().Element(c => ComposeLabelledLine(c, "Credential ID", cert.CredentialId));

            if (!string.IsNullOrWhiteSpace(cert.CredentialUrl))
                column.Item().Element(c => ComposeLabelledLine(c, "Verification", cert.CredentialUrl));
        });
    }

    private void ComposeSkills(ColumnDescriptor column, List<Skill> skills)
    {
        // Every skill is named with its assessed level; no rolling-up, no "and more".
        var categorised = skills
            .Where(s => !string.IsNullOrWhiteSpace(s.Category))
            .GroupBy(s => s.Category)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .ToList();

        foreach (var group in categorised)
        {
            column.Item().PaddingBottom(4).Column(groupCol =>
            {
                groupCol.Item().Text(group.Key).SemiBold().FontSize(10 * FontSizeScale);
                foreach (var skill in group.OrderBy(s => s.Order))
                {
                    groupCol.Item().Text($"    {skill.Name} — {GetSkillLevelText(skill.Level)}")
                        .FontSize(10 * FontSizeScale)
                        .FontColor(Colors.Grey.Darken3);
                }
            });
        }

        var uncategorised = skills.Where(s => string.IsNullOrWhiteSpace(s.Category)).OrderBy(s => s.Order).ToList();

        foreach (var skill in uncategorised)
        {
            column.Item().Text($"{skill.Name} — {GetSkillLevelText(skill.Level)}")
                .FontSize(10 * FontSizeScale)
                .FontColor(Colors.Grey.Darken3);
        }
    }
}
