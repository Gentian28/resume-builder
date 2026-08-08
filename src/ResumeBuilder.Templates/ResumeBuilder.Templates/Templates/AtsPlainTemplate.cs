using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Templates.Templates;

/// <summary>
/// Deliberately featureless: one column, one font, no rules, no bars, no tables, no images.
/// Everything a naive resume parser trips over has been left out, so the text it lifts out of the
/// PDF reads in the same order a human does. The accent color is honoured but defaults to black —
/// color does not affect text extraction, layout does.
/// </summary>
public class AtsPlainTemplate : BaseTemplate
{
    public override TemplateInfo Info => new()
    {
        Id = "ats-plain",
        Name = "ATS Plain",
        Description = "Single column, no graphics, no columns — built to survive naive applicant tracking systems",
        Category = TemplateCategory.Professional,
        Layout = TemplateLayout.SingleColumn,
        Tags = new[] { "ats", "plain", "parseable", "no-graphics", "safe" },
        DefaultAccentColor = "#000000",
        DefaultFontFamily = "Arial"
    };

    private static readonly EntryStyle Entry = new()
    {
        TitleFontSize = 11,
        SubtitleFontSize = 10,
        BodyFontSize = 10,
        MetaFontSize = 10,
        Bullet = "- "
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
                        column.Item().Element(c => ComposeSection(c, "WORK EXPERIENCE", col =>
                        {
                            foreach (var exp in resume.Experiences.OrderBy(e => e.Order))
                                col.Item().PaddingBottom(10).Element(e => ComposeExperience(e, exp));
                        }));
                        break;

                    case SectionType.Education:
                        column.Item().Element(c => ComposeSection(c, "EDUCATION", col =>
                        {
                            foreach (var edu in resume.EducationList.OrderBy(e => e.Order))
                                col.Item().PaddingBottom(8).Element(e => ComposeEducation(e, edu));
                        }));
                        break;

                    case SectionType.Skills:
                        column.Item().Element(c => ComposeSection(c, "SKILLS", col => ComposeSkills(col, resume.Skills)));
                        break;

                    case SectionType.Languages:
                        column.Item().Element(c => ComposeSection(c, "LANGUAGES", col =>
                        {
                            foreach (var lang in resume.Languages.OrderBy(l => l.Order))
                                col.Item().Text($"- {FormatLanguage(lang)}").FontSize(10 * FontSizeScale);
                        }));
                        break;

                    case SectionType.Certifications:
                        column.Item().Element(c => ComposeSection(c, "CERTIFICATIONS", col =>
                        {
                            foreach (var cert in resume.Certifications.OrderBy(c2 => c2.Order))
                                col.Item().Text($"- {DescribeCertification(cert)}").FontSize(10 * FontSizeScale);
                        }));
                        break;

                    case SectionType.Projects:
                        column.Item().Element(c => ComposeSection(c, "PROJECTS", col =>
                        {
                            foreach (var project in resume.Projects.OrderBy(p => p.Order))
                                col.Item().PaddingBottom(8).Element(e => ComposeProjectEntry(e, project, Entry));
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
        container.Column(column =>
        {
            column.Item().Text(info.FullName).FontSize(18 * FontSizeScale).Bold();

            if (!string.IsNullOrWhiteSpace(info.JobTitle))
                column.Item().Text(info.JobTitle).FontSize(11 * FontSizeScale);

            column.Item().Height(4);

            // One contact per line: a parser that reads the PDF's text stream sees discrete labels
            // rather than one run it has to split on a separator it may not know.
            foreach (var line in ContactLines(info))
                column.Item().Text(line).FontSize(10 * FontSizeScale);
        });
    }

    private static List<string> ContactLines(PersonalInfo info)
    {
        var lines = new List<string>();

        void Add(string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                lines.Add(value.Trim());
        }

        Add(info.Email);
        Add(info.Phone);
        Add(FullAddress(info));
        Add(info.LinkedIn);
        Add(info.GitHub);
        Add(info.Website);

        return lines;
    }

    private static string FullAddress(PersonalInfo info) => string.Join(", ", new[]
    {
        info.Address,
        info.City,
        info.PostalCode,
        info.Country
    }.Where(part => !string.IsNullOrWhiteSpace(part)));

    private void ComposeSection(IContainer container, string title, Action<ColumnDescriptor> content)
    {
        container.EnsureSpace(90).Column(column =>
        {
            column.Item().Text(title)
                .FontSize(11 * FontSizeScale)
                .Bold()
                .FontColor(ParseColor(AccentColor));

            column.Item().Height(6);
            column.Item().Column(content);
        });
    }

    private void ComposeExperience(IContainer container, Experience exp)
    {
        container.Column(column =>
        {
            column.Item().Text(exp.JobTitle).Bold().FontSize(11 * FontSizeScale);

            var employer = string.Join(", ", new[] { exp.Company, exp.Location }
                .Where(part => !string.IsNullOrWhiteSpace(part)));

            if (!string.IsNullOrWhiteSpace(employer))
                column.Item().Text(employer).FontSize(10 * FontSizeScale);

            if (!string.IsNullOrWhiteSpace(exp.DateRange))
                column.Item().Text(exp.DateRange).FontSize(10 * FontSizeScale);

            if (!string.IsNullOrWhiteSpace(exp.Description))
                column.Item().PaddingTop(3).Text(exp.Description).FontSize(10 * FontSizeScale).LineHeight(LineSpacing);

            if (exp.Achievements.Any())
                column.Item().PaddingTop(3).Element(c => ComposeBulletList(c, exp.Achievements, Entry));
        });
    }

    private void ComposeEducation(IContainer container, Education edu)
    {
        container.Column(column =>
        {
            column.Item().Text(edu.DegreeWithField).Bold().FontSize(11 * FontSizeScale);

            var institution = string.Join(", ", new[] { edu.Institution, edu.Location }
                .Where(part => !string.IsNullOrWhiteSpace(part)));

            if (!string.IsNullOrWhiteSpace(institution))
                column.Item().Text(institution).FontSize(10 * FontSizeScale);

            if (!string.IsNullOrWhiteSpace(edu.DateRange))
                column.Item().Text(edu.DateRange).FontSize(10 * FontSizeScale);

            if (!string.IsNullOrWhiteSpace(edu.Grade))
                column.Item().Text($"Grade: {edu.Grade}").FontSize(10 * FontSizeScale);

            if (!string.IsNullOrWhiteSpace(edu.Description))
                column.Item().Text(edu.Description).FontSize(10 * FontSizeScale).LineHeight(LineSpacing);
        });
    }

    private void ComposeSkills(ColumnDescriptor column, List<Skill> skills)
    {
        // Plain "Label: a, b, c" lines. Every skill is listed, categorised or not.
        var categorised = skills
            .Where(s => !string.IsNullOrWhiteSpace(s.Category))
            .GroupBy(s => s.Category)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .ToList();

        foreach (var group in categorised)
        {
            column.Item().Text(text =>
            {
                text.Span($"{group.Key}: ").Bold().FontSize(10 * FontSizeScale);
                text.Span(string.Join(", ", group.OrderBy(s => s.Order).Select(s => s.Name)))
                    .FontSize(10 * FontSizeScale);
            });
        }

        var uncategorised = skills
            .Where(s => string.IsNullOrWhiteSpace(s.Category))
            .OrderBy(s => s.Order)
            .Select(s => s.Name)
            .ToList();

        if (uncategorised.Count > 0)
            column.Item().Text(string.Join(", ", uncategorised)).FontSize(10 * FontSizeScale);
    }

    private static string DescribeCertification(Certification cert)
    {
        var parts = new List<string> { cert.Name };

        if (!string.IsNullOrWhiteSpace(cert.IssuingOrganization))
            parts.Add(cert.IssuingOrganization);

        if (cert.IssueDate.HasValue)
            parts.Add(ResumeDateFormat.MonthYear(cert.IssueDate));

        if (!string.IsNullOrWhiteSpace(cert.CredentialId))
            parts.Add($"Credential ID: {cert.CredentialId}");

        return string.Join(", ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }
}
