using System.Text;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Core.SmartContent;

/// <summary>
/// Flattens a resume to plain text. Keyword analysis and the AI prompts both need "the resume as
/// words", and previously each caller assembled its own partial version of this.
/// </summary>
public static class ResumeTextExtractor
{
    public static string ToPlainText(Resume resume, bool includeHidden = false)
    {
        var sb = new StringBuilder();
        bool Visible(SectionType section) => includeHidden || resume.SectionOrder.IsSectionVisible(section);

        var info = resume.PersonalInfo;
        if (Visible(SectionType.PersonalInfo))
        {
            AppendLine(sb, info.FullName);
            AppendLine(sb, info.JobTitle);
            AppendLine(sb, info.Email);
            AppendLine(sb, info.Phone);
            AppendLine(sb, info.Location);
        }
        if (Visible(SectionType.Summary))
            AppendLine(sb, resume.Summary);

        foreach (var exp in resume.Experiences.Where(_ => Visible(SectionType.Experience)))
        {
            AppendLine(sb, exp.JobTitle);
            AppendLine(sb, exp.Company);
            AppendLine(sb, exp.Location);
            AppendLine(sb, exp.Description);
            foreach (var achievement in exp.Achievements)
            {
                AppendLine(sb, achievement);
            }
        }

        foreach (var edu in resume.EducationList.Where(_ => Visible(SectionType.Education)))
        {
            AppendLine(sb, edu.DegreeWithField);
            AppendLine(sb, edu.Institution);
            AppendLine(sb, edu.Description);
        }

        foreach (var skill in resume.Skills.Where(_ => Visible(SectionType.Skills)))
        {
            AppendLine(sb, skill.Name);
            AppendLine(sb, skill.Category);
        }

        foreach (var language in resume.Languages.Where(_ => Visible(SectionType.Languages)))
        {
            AppendLine(sb, language.Name);
        }

        foreach (var cert in resume.Certifications.Where(_ => Visible(SectionType.Certifications)))
        {
            AppendLine(sb, cert.Name);
            AppendLine(sb, cert.IssuingOrganization);
        }

        foreach (var project in resume.Projects.Where(_ => Visible(SectionType.Projects)))
        {
            AppendLine(sb, project.Name);
            AppendLine(sb, project.Description);
            foreach (var tech in project.Technologies) AppendLine(sb, tech);
            foreach (var highlight in project.Highlights) AppendLine(sb, highlight);
        }

        foreach (var section in resume.CustomSections.Where(_ => Visible(SectionType.CustomSections)))
        {
            AppendLine(sb, section.Title);
            foreach (var item in section.Items)
            {
                AppendLine(sb, item.Title);
                AppendLine(sb, item.Subtitle);
                AppendLine(sb, item.Description);
            }
        }

        return sb.ToString();
    }

    private static void AppendLine(StringBuilder sb, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            sb.AppendLine(value);
        }
    }
}
