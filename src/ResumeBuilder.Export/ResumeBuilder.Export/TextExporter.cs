using System.Text;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Export;

/// <summary>
/// Plain-text export for applicant tracking systems: no bullet glyphs, box drawing or other
/// decoration that ATS parsers mangle — just uppercase headings and hyphen-prefixed list items.
/// </summary>
public class TextExporter : IExporter
{
    public string Format => "TEXT";
    public string FileExtension => ".txt";
    public string MimeType => "text/plain";

    public Task<byte[]> ExportAsync(Resume resume, string templateId)
    {
        var text = GenerateText(resume);
        return Task.FromResult(Encoding.UTF8.GetBytes(text));
    }

    public async Task ExportToFileAsync(Resume resume, string templateId, string filePath)
    {
        var bytes = await ExportAsync(resume, templateId);
        await File.WriteAllBytesAsync(filePath, bytes);
    }

    private static string GenerateText(Resume resume)
    {
        var sb = new StringBuilder();

        foreach (var section in SectionLayout.OrderedVisibleSections(resume))
        {
            switch (section)
            {
                case SectionType.PersonalInfo:
                    AppendHeader(sb, resume.PersonalInfo);
                    break;
                case SectionType.Summary:
                    AppendHeading(sb, "Professional Summary");
                    sb.AppendLine(resume.Summary.Trim());
                    break;
                case SectionType.Experience:
                    AppendExperience(sb, resume);
                    break;
                case SectionType.Education:
                    AppendEducation(sb, resume);
                    break;
                case SectionType.Skills:
                    AppendSkills(sb, resume);
                    break;
                case SectionType.Languages:
                    AppendLanguages(sb, resume);
                    break;
                case SectionType.Certifications:
                    AppendCertifications(sb, resume);
                    break;
                case SectionType.Projects:
                    AppendProjects(sb, resume);
                    break;
                case SectionType.CustomSections:
                    AppendCustomSections(sb, resume);
                    break;
            }
        }

        return sb.ToString().TrimEnd() + Environment.NewLine;
    }

    private static void AppendHeader(StringBuilder sb, PersonalInfo info)
    {
        if (!string.IsNullOrWhiteSpace(info.FullName))
            sb.AppendLine(info.FullName.ToUpperInvariant());

        if (!string.IsNullOrWhiteSpace(info.JobTitle))
            sb.AppendLine(info.JobTitle);

        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(info.Email))
            sb.AppendLine($"Email: {info.Email}");
        if (!string.IsNullOrWhiteSpace(info.Phone))
            sb.AppendLine($"Phone: {info.Phone}");

        var address = string.Join(", ", new[] { info.Address, info.City, info.PostalCode, info.Country }
            .Where(s => !string.IsNullOrWhiteSpace(s)));
        if (!string.IsNullOrWhiteSpace(address))
            sb.AppendLine($"Location: {address}");

        if (!string.IsNullOrWhiteSpace(info.LinkedIn))
            sb.AppendLine($"LinkedIn: {info.LinkedIn}");
        if (!string.IsNullOrWhiteSpace(info.GitHub))
            sb.AppendLine($"GitHub: {info.GitHub}");
        if (!string.IsNullOrWhiteSpace(info.Website))
            sb.AppendLine($"Website: {info.Website}");
    }

    private static void AppendExperience(StringBuilder sb, Resume resume)
    {
        AppendHeading(sb, "Work Experience");

        foreach (var exp in resume.Experiences.OrderBy(e => e.Order))
        {
            sb.AppendLine(exp.JobTitle);

            var company = string.Join(", ", new[] { exp.Company, exp.Location }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
            if (!string.IsNullOrWhiteSpace(company))
                sb.AppendLine(company);

            if (!string.IsNullOrWhiteSpace(exp.DateRange))
                sb.AppendLine(exp.DateRange);

            if (!string.IsNullOrWhiteSpace(exp.Description))
                sb.AppendLine(exp.Description.Trim());

            AppendItems(sb, exp.Achievements);
            sb.AppendLine();
        }
    }

    private static void AppendEducation(StringBuilder sb, Resume resume)
    {
        AppendHeading(sb, "Education");

        foreach (var edu in resume.EducationList.OrderBy(e => e.Order))
        {
            sb.AppendLine(edu.DegreeWithField);

            var institution = string.Join(", ", new[] { edu.Institution, edu.Location }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
            if (!string.IsNullOrWhiteSpace(institution))
                sb.AppendLine(institution);

            if (!string.IsNullOrWhiteSpace(edu.DateRange))
                sb.AppendLine(edu.DateRange);

            if (!string.IsNullOrWhiteSpace(edu.Grade))
                sb.AppendLine($"Grade: {edu.Grade}");

            if (!string.IsNullOrWhiteSpace(edu.Description))
                sb.AppendLine(edu.Description.Trim());

            sb.AppendLine();
        }
    }

    private static void AppendSkills(StringBuilder sb, Resume resume)
    {
        AppendHeading(sb, "Skills");

        foreach (var group in resume.Skills.GroupBy(s => s.Category))
        {
            var names = string.Join(", ", group.OrderBy(s => s.Order)
                .Select(s => $"{s.Name} ({SectionLayout.SkillLevelText(s.Level)})"));

            sb.AppendLine(string.IsNullOrWhiteSpace(group.Key) ? names : $"{group.Key}: {names}");
        }
    }

    private static void AppendLanguages(StringBuilder sb, Resume resume)
    {
        AppendHeading(sb, "Languages");

        foreach (var lang in resume.Languages.OrderBy(l => l.Order))
        {
            sb.AppendLine($"{lang.Name} - {SectionLayout.LanguageProficiencyText(lang.Proficiency)}");
        }
    }

    private static void AppendCertifications(StringBuilder sb, Resume resume)
    {
        AppendHeading(sb, "Certifications");

        foreach (var cert in resume.Certifications.OrderBy(c => c.Order))
        {
            var parts = new List<string> { cert.Name };

            if (!string.IsNullOrWhiteSpace(cert.IssuingOrganization))
                parts.Add(cert.IssuingOrganization);

            var date = SectionLayout.CertificationDate(cert);
            if (!string.IsNullOrEmpty(date))
                parts.Add(date);

            sb.AppendLine(string.Join(", ", parts));

            if (!string.IsNullOrWhiteSpace(cert.CredentialId))
                sb.AppendLine($"Credential ID: {cert.CredentialId}");
            if (!string.IsNullOrWhiteSpace(cert.CredentialUrl))
                sb.AppendLine($"Credential URL: {cert.CredentialUrl}");
        }
    }

    private static void AppendProjects(StringBuilder sb, Resume resume)
    {
        AppendHeading(sb, "Projects");

        foreach (var proj in resume.Projects.OrderBy(p => p.Order))
        {
            sb.AppendLine(proj.Name);

            if (proj.StartDate.HasValue)
            {
                var end = proj.IsOngoing ? "Present" : ResumeDateFormat.MonthYear(proj.EndDate);
                sb.AppendLine($"{ResumeDateFormat.MonthYear(proj.StartDate)} - {end}");
            }

            if (!string.IsNullOrWhiteSpace(proj.Url))
                sb.AppendLine(proj.Url);

            if (!string.IsNullOrWhiteSpace(proj.Description))
                sb.AppendLine(proj.Description.Trim());

            AppendItems(sb, proj.Highlights);

            if (proj.Technologies.Any())
                sb.AppendLine($"Technologies: {string.Join(", ", proj.Technologies)}");

            sb.AppendLine();
        }
    }

    private static void AppendCustomSections(StringBuilder sb, Resume resume)
    {
        foreach (var custom in SectionLayout.VisibleCustomSections(resume))
        {
            AppendHeading(sb, custom.Title);

            foreach (var item in custom.Items.OrderBy(i => i.Order))
            {
                sb.AppendLine(item.Title);

                if (!string.IsNullOrWhiteSpace(item.Subtitle))
                    sb.AppendLine(item.Subtitle);

                var dateRange = SectionLayout.CustomItemDateRange(item);
                if (!string.IsNullOrEmpty(dateRange))
                    sb.AppendLine(dateRange);

                if (!string.IsNullOrWhiteSpace(item.Description))
                    sb.AppendLine(item.Description.Trim());

                sb.AppendLine();
            }
        }
    }

    private static void AppendItems(StringBuilder sb, List<string> items)
    {
        foreach (var item in items.Where(i => !string.IsNullOrWhiteSpace(i)))
        {
            sb.AppendLine($"- {item.Trim()}");
        }
    }

    private static void AppendHeading(StringBuilder sb, string title)
    {
        sb.AppendLine();
        sb.AppendLine(title.ToUpperInvariant());
        sb.AppendLine();
    }
}
