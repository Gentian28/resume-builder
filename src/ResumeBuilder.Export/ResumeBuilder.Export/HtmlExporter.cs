using System.Text;
using System.Text.RegularExpressions;
using ResumeBuilder.Core.Models;
using ResumeBuilder.Core.Validation;
using ResumeBuilder.Templates;

namespace ResumeBuilder.Export;

public class HtmlExporter : IExporter
{
    private readonly TemplateRegistry _templateRegistry;

    public string Format => "HTML";
    public string FileExtension => ".html";
    public string MimeType => "text/html";

    public HtmlExporter(TemplateRegistry templateRegistry)
    {
        _templateRegistry = templateRegistry;
    }

    public Task<byte[]> ExportAsync(Resume resume, string templateId)
    {
        var html = GenerateHtml(resume, templateId);
        var bytes = Encoding.UTF8.GetBytes(html);
        return Task.FromResult(bytes);
    }

    public async Task ExportToFileAsync(Resume resume, string templateId, string filePath)
    {
        var bytes = await ExportAsync(resume, templateId);
        await File.WriteAllBytesAsync(filePath, bytes);
    }

    private string GenerateHtml(Resume resume, string templateId)
    {
        var settings = SectionLayout.EffectiveSettings(resume, _templateRegistry, templateId);
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"  <title>{Escape(resume.PersonalInfo.FullName)} - Resume</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine(GetStyles(settings));
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("  <div class=\"resume\">");

        foreach (var section in SectionLayout.OrderedVisibleSections(resume))
        {
            switch (section)
            {
                case SectionType.PersonalInfo:
                    AppendHeader(sb, resume.PersonalInfo);
                    break;
                case SectionType.Summary:
                    AppendSummary(sb, resume);
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

        sb.AppendLine("  </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private static void AppendHeader(StringBuilder sb, PersonalInfo info)
    {
        sb.AppendLine("    <header>");

        if (info.Photo is { Length: > 0 })
        {
            var base64 = Convert.ToBase64String(info.Photo);
            sb.AppendLine($"      <img class=\"photo\" src=\"data:image/png;base64,{base64}\" alt=\"{Escape(info.FullName)}\">");
        }

        sb.AppendLine($"      <h1>{Escape(info.FullName)}</h1>");
        if (!string.IsNullOrWhiteSpace(info.JobTitle))
            sb.AppendLine($"      <p class=\"job-title\">{Escape(info.JobTitle)}</p>");

        sb.AppendLine("      <div class=\"contact\">");
        var contacts = new List<string>();
        if (!string.IsNullOrWhiteSpace(info.Email))
            contacts.Add($"<a href=\"mailto:{Escape(info.Email)}\">{Escape(info.Email)}</a>");
        if (!string.IsNullOrWhiteSpace(info.Phone))
            contacts.Add($"<span>{Escape(info.Phone)}</span>");
        if (!string.IsNullOrWhiteSpace(info.Location))
            contacts.Add($"<span>{Escape(info.Location)}</span>");
        if (!string.IsNullOrWhiteSpace(info.LinkedIn))
            contacts.Add(Link(info.LinkedIn, info.LinkedIn));
        if (!string.IsNullOrWhiteSpace(info.GitHub))
            contacts.Add(Link(GitHubUrl(info.GitHub), info.GitHub));
        if (!string.IsNullOrWhiteSpace(info.Website))
            contacts.Add(Link(info.Website, info.Website));

        sb.AppendLine($"        {string.Join(" <span class=\"separator\">|</span> ", contacts)}");
        sb.AppendLine("      </div>");
        sb.AppendLine("    </header>");
    }

    private static void AppendSummary(StringBuilder sb, Resume resume)
    {
        sb.AppendLine("    <section class=\"summary\">");
        sb.AppendLine("      <h2>Professional Summary</h2>");
        sb.AppendLine($"      <p>{Escape(resume.Summary)}</p>");
        sb.AppendLine("    </section>");
    }

    private static void AppendExperience(StringBuilder sb, Resume resume)
    {
        sb.AppendLine("    <section class=\"experience\">");
        sb.AppendLine("      <h2>Work Experience</h2>");
        foreach (var exp in resume.Experiences.OrderBy(e => e.Order))
        {
            sb.AppendLine("      <div class=\"entry\">");
            sb.AppendLine("        <div class=\"entry-header\">");
            sb.AppendLine($"          <h3>{Escape(exp.JobTitle)}</h3>");
            sb.AppendLine($"          <span class=\"date\">{Escape(exp.DateRange)}</span>");
            sb.AppendLine("        </div>");
            sb.AppendLine($"        <p class=\"company\">{Escape(exp.Company)}{(!string.IsNullOrWhiteSpace(exp.Location) ? $" | {Escape(exp.Location)}" : "")}</p>");

            if (!string.IsNullOrWhiteSpace(exp.Description))
                sb.AppendLine($"        <p class=\"description\">{Escape(exp.Description)}</p>");

            AppendList(sb, exp.Achievements);
            sb.AppendLine("      </div>");
        }
        sb.AppendLine("    </section>");
    }

    private static void AppendEducation(StringBuilder sb, Resume resume)
    {
        sb.AppendLine("    <section class=\"education\">");
        sb.AppendLine("      <h2>Education</h2>");
        foreach (var edu in resume.EducationList.OrderBy(e => e.Order))
        {
            sb.AppendLine("      <div class=\"entry\">");
            sb.AppendLine("        <div class=\"entry-header\">");
            sb.AppendLine($"          <h3>{Escape(edu.DegreeWithField)}</h3>");
            sb.AppendLine($"          <span class=\"date\">{Escape(edu.DateRange)}</span>");
            sb.AppendLine("        </div>");
            sb.AppendLine($"        <p class=\"institution\">{Escape(edu.Institution)}{(!string.IsNullOrWhiteSpace(edu.Location) ? $" | {Escape(edu.Location)}" : "")}</p>");
            if (!string.IsNullOrWhiteSpace(edu.Grade))
                sb.AppendLine($"        <p class=\"grade\">Grade: {Escape(edu.Grade)}</p>");
            if (!string.IsNullOrWhiteSpace(edu.Description))
                sb.AppendLine($"        <p class=\"description\">{Escape(edu.Description)}</p>");
            sb.AppendLine("      </div>");
        }
        sb.AppendLine("    </section>");
    }

    private static void AppendSkills(StringBuilder sb, Resume resume)
    {
        sb.AppendLine("    <section class=\"skills\">");
        sb.AppendLine("      <h2>Skills</h2>");

        foreach (var group in resume.Skills.GroupBy(s => s.Category))
        {
            if (!string.IsNullOrWhiteSpace(group.Key))
                sb.AppendLine($"      <h3 class=\"skill-category\">{Escape(group.Key)}</h3>");

            sb.AppendLine("      <div class=\"skill-list\">");
            foreach (var skill in group.OrderBy(s => s.Order))
            {
                sb.AppendLine($"        <span class=\"skill-badge\">{Escape(skill.Name)}<span class=\"skill-level\">{Escape(SectionLayout.SkillLevelText(skill.Level))}</span></span>");
            }
            sb.AppendLine("      </div>");
        }

        sb.AppendLine("    </section>");
    }

    private static void AppendLanguages(StringBuilder sb, Resume resume)
    {
        sb.AppendLine("    <section class=\"languages\">");
        sb.AppendLine("      <h2>Languages</h2>");
        sb.AppendLine("      <ul>");
        foreach (var lang in resume.Languages.OrderBy(l => l.Order))
        {
            sb.AppendLine($"        <li>{Escape(lang.Name)} - {Escape(SectionLayout.LanguageProficiencyText(lang.Proficiency))}</li>");
        }
        sb.AppendLine("      </ul>");
        sb.AppendLine("    </section>");
    }

    private static void AppendCertifications(StringBuilder sb, Resume resume)
    {
        sb.AppendLine("    <section class=\"certifications\">");
        sb.AppendLine("      <h2>Certifications</h2>");
        foreach (var cert in resume.Certifications.OrderBy(c => c.Order))
        {
            sb.AppendLine("      <div class=\"entry\">");

            var name = Escape(cert.Name);
            var title = string.IsNullOrWhiteSpace(cert.CredentialUrl) ? name : Link(cert.CredentialUrl, cert.Name);
            sb.AppendLine($"        <h3>{title}</h3>");

            var meta = new List<string>();
            if (!string.IsNullOrWhiteSpace(cert.IssuingOrganization))
                meta.Add(Escape(cert.IssuingOrganization));

            var date = SectionLayout.CertificationDate(cert);
            if (!string.IsNullOrEmpty(date))
                meta.Add(Escape(date));
            else if (cert.DoesNotExpire)
                meta.Add("No expiration");

            if (meta.Any())
                sb.AppendLine($"        <p class=\"institution\">{string.Join(" | ", meta)}</p>");

            if (!string.IsNullOrWhiteSpace(cert.CredentialId))
                sb.AppendLine($"        <p class=\"grade\">Credential ID: {Escape(cert.CredentialId)}</p>");

            sb.AppendLine("      </div>");
        }
        sb.AppendLine("    </section>");
    }

    private static void AppendProjects(StringBuilder sb, Resume resume)
    {
        sb.AppendLine("    <section class=\"projects\">");
        sb.AppendLine("      <h2>Projects</h2>");
        foreach (var proj in resume.Projects.OrderBy(p => p.Order))
        {
            sb.AppendLine("      <div class=\"entry\">");
            sb.AppendLine("        <div class=\"entry-header\">");

            var name = string.IsNullOrWhiteSpace(proj.Url) ? Escape(proj.Name) : Link(proj.Url, proj.Name);
            sb.AppendLine($"          <h3>{name}</h3>");

            var dateRange = FormatDateRange(proj.StartDate, proj.EndDate, proj.IsOngoing);
            if (!string.IsNullOrEmpty(dateRange))
                sb.AppendLine($"          <span class=\"date\">{Escape(dateRange)}</span>");
            sb.AppendLine("        </div>");

            if (!string.IsNullOrWhiteSpace(proj.Description))
                sb.AppendLine($"        <p class=\"description\">{Escape(proj.Description)}</p>");

            AppendList(sb, proj.Highlights);

            if (proj.Technologies.Any())
                sb.AppendLine($"        <p class=\"tech\">Technologies: {Escape(string.Join(", ", proj.Technologies))}</p>");

            sb.AppendLine("      </div>");
        }
        sb.AppendLine("    </section>");
    }

    private static void AppendCustomSections(StringBuilder sb, Resume resume)
    {
        foreach (var custom in SectionLayout.VisibleCustomSections(resume))
        {
            sb.AppendLine("    <section class=\"custom\">");
            sb.AppendLine($"      <h2>{Escape(custom.Title)}</h2>");

            foreach (var item in custom.Items.OrderBy(i => i.Order))
            {
                sb.AppendLine("      <div class=\"entry\">");
                sb.AppendLine("        <div class=\"entry-header\">");
                sb.AppendLine($"          <h3>{Escape(item.Title)}</h3>");

                var dateRange = SectionLayout.CustomItemDateRange(item);
                if (!string.IsNullOrEmpty(dateRange))
                    sb.AppendLine($"          <span class=\"date\">{Escape(dateRange)}</span>");
                sb.AppendLine("        </div>");

                if (!string.IsNullOrWhiteSpace(item.Subtitle))
                    sb.AppendLine($"        <p class=\"institution\">{Escape(item.Subtitle)}</p>");

                if (!string.IsNullOrWhiteSpace(item.Description))
                    sb.AppendLine($"        <p class=\"description\">{Escape(item.Description)}</p>");

                sb.AppendLine("      </div>");
            }

            sb.AppendLine("    </section>");
        }
    }

    private static void AppendList(StringBuilder sb, List<string> items)
    {
        if (!items.Any())
            return;

        sb.AppendLine("        <ul>");
        foreach (var item in items)
            sb.AppendLine($"          <li>{Escape(item)}</li>");
        sb.AppendLine("        </ul>");
    }

    private static string FormatDateRange(DateTime? start, DateTime? end, bool isOngoing)
    {
        if (!start.HasValue)
            return string.Empty;

        var endStr = isOngoing ? "Present" : ResumeDateFormat.MonthYear(end);
        return $"{ResumeDateFormat.MonthYear(start)} - {endStr}";
    }

    private static string GitHubUrl(string gitHub) =>
        gitHub.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? gitHub : $"https://github.com/{gitHub}";

    /// <summary>
    /// Renders an anchor only when the value is a genuine http(s) URL. A stored "javascript:" value
    /// would otherwise become a live link, so it falls back to plain text.
    /// </summary>
    private static string Link(string? url, string? text)
    {
        var safeText = Escape(text ?? url ?? "");
        var safeUrl = UrlRule.ToSafeAbsoluteUrl(url);

        return safeUrl == null
            ? $"<span>{safeText}</span>"
            : $"<a href=\"{Escape(safeUrl)}\" target=\"_blank\" rel=\"noopener noreferrer\">{safeText}</a>";
    }

    private static readonly Regex HexColorRegex = new(
        @"^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$",
        RegexOptions.Compiled);

    private static readonly Regex UnsafeFontCharsRegex = new(
        @"[^A-Za-z0-9 \-]",
        RegexOptions.Compiled);

    /// <summary>Only a literal hex color may reach the stylesheet; anything else could close the rule and inject CSS.</summary>
    private static string SafeColor(string? color)
    {
        var value = color?.Trim();
        return !string.IsNullOrEmpty(value) && HexColorRegex.IsMatch(value)
            ? value
            : TemplateSettings.DefaultAccentColor;
    }

    /// <summary>Strips quotes, braces, semicolons and backslashes so a font name cannot break out of the declaration.</summary>
    private static string SafeFontFamily(string? fontFamily)
    {
        var value = UnsafeFontCharsRegex.Replace(fontFamily ?? "", "").Trim();

        if (string.IsNullOrEmpty(value))
            value = TemplateSettings.DefaultFontFamily;

        return $"'{value}'";
    }

    private static string GetStyles(TemplateSettings settings)
    {
        var accentColor = SafeColor(settings.AccentColor);
        var textColor = SafeColor(settings.TextColor);
        var headingColor = SafeColor(settings.HeadingColor);
        var fontFamily = SafeFontFamily(settings.FontFamily);
        var headingFontFamily = SafeFontFamily(settings.HeadingFontFamily);
        var lineHeight = settings.LineSpacing.ToString(ResumeDateFormat.Culture);
        var sectionSpacing = settings.SectionSpacing.ToString(ResumeDateFormat.Culture);

        return $@"
    :root {{
      --accent-color: {accentColor};
      --text-color: {textColor};
      --heading-color: {headingColor};
      --light-gray: #666;
    }}
    * {{ margin: 0; padding: 0; box-sizing: border-box; }}
    body {{
      font-family: {fontFamily}, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
      line-height: {lineHeight};
      color: var(--text-color);
      background: #f5f5f5;
    }}
    h1, h2, h3 {{ font-family: {headingFontFamily}, inherit; color: var(--heading-color); }}
    .resume {{
      max-width: 800px;
      margin: 40px auto;
      background: white;
      padding: 40px 50px;
      box-shadow: 0 2px 10px rgba(0,0,0,0.1);
    }}
    header {{
      text-align: center;
      border-bottom: 2px solid var(--accent-color);
      padding-bottom: 20px;
      margin-bottom: 25px;
    }}
    header h1 {{
      color: var(--accent-color);
      font-size: 2.5em;
      margin-bottom: 5px;
    }}
    .photo {{
      width: 110px;
      height: 110px;
      object-fit: cover;
      border-radius: 50%;
      margin-bottom: 12px;
    }}
    .job-title {{
      font-size: 1.2em;
      color: var(--light-gray);
      margin-bottom: 10px;
    }}
    .contact {{
      font-size: 0.9em;
    }}
    .contact a {{
      color: var(--accent-color);
      text-decoration: none;
    }}
    .contact a:hover {{ text-decoration: underline; }}
    .separator {{ color: #ccc; margin: 0 8px; }}
    section {{
      margin-bottom: {sectionSpacing}px;
    }}
    section h2 {{
      color: var(--accent-color);
      font-size: 1.3em;
      border-bottom: 1px solid #eee;
      padding-bottom: 5px;
      margin-bottom: 15px;
    }}
    .entry {{
      margin-bottom: 15px;
    }}
    .entry-header {{
      display: flex;
      justify-content: space-between;
      align-items: baseline;
      gap: 12px;
    }}
    .entry h3 {{
      font-size: 1.1em;
    }}
    .entry h3 a {{
      color: inherit;
      text-decoration: none;
    }}
    .entry h3 a:hover {{ text-decoration: underline; }}
    .date {{
      color: var(--light-gray);
      font-size: 0.9em;
      white-space: nowrap;
    }}
    .company, .institution {{
      color: var(--accent-color);
      font-size: 0.95em;
      margin-bottom: 5px;
    }}
    .description {{
      font-size: 0.95em;
      margin-top: 5px;
    }}
    ul {{
      margin-left: 20px;
      margin-top: 8px;
    }}
    li {{
      margin-bottom: 4px;
      font-size: 0.95em;
    }}
    .skill-category {{
      font-size: 0.95em;
      margin: 10px 0 6px;
    }}
    .skill-list {{
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
    }}
    .skill-badge {{
      background: var(--accent-color);
      color: white;
      padding: 4px 12px;
      border-radius: 15px;
      font-size: 0.85em;
    }}
    .skill-level {{
      margin-left: 6px;
      opacity: 0.75;
      font-size: 0.85em;
    }}
    .tech {{
      font-size: 0.9em;
      color: var(--light-gray);
    }}
    .grade {{
      font-size: 0.9em;
      font-style: italic;
    }}
    @media print {{
      body {{ background: white; }}
      .resume {{ box-shadow: none; margin: 0; max-width: none; }}
    }}
    @media (max-width: 600px) {{
      .resume {{ padding: 20px; margin: 20px; }}
      .entry-header {{ flex-direction: column; }}
    }}
    ";
    }

    private static string Escape(string text)
    {
        return System.Net.WebUtility.HtmlEncode(text ?? "");
    }
}
