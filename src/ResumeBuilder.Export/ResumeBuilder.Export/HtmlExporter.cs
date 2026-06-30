using System.Text;
using ResumeBuilder.Core.Models;
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
        var html = GenerateHtml(resume);
        var bytes = Encoding.UTF8.GetBytes(html);
        return Task.FromResult(bytes);
    }

    public async Task ExportToFileAsync(Resume resume, string templateId, string filePath)
    {
        var bytes = await ExportAsync(resume, templateId);
        await File.WriteAllBytesAsync(filePath, bytes);
    }

    private string GenerateHtml(Resume resume)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"  <title>{Escape(resume.PersonalInfo.FullName)} - Resume</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine(GetStyles(resume.AccentColor, resume.FontFamily));
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("  <div class=\"resume\">");

        // Header
        sb.AppendLine("    <header>");
        sb.AppendLine($"      <h1>{Escape(resume.PersonalInfo.FullName)}</h1>");
        if (!string.IsNullOrWhiteSpace(resume.PersonalInfo.JobTitle))
            sb.AppendLine($"      <p class=\"job-title\">{Escape(resume.PersonalInfo.JobTitle)}</p>");

        sb.AppendLine("      <div class=\"contact\">");
        var contacts = new List<string>();
        if (!string.IsNullOrWhiteSpace(resume.PersonalInfo.Email))
            contacts.Add($"<a href=\"mailto:{Escape(resume.PersonalInfo.Email)}\">{Escape(resume.PersonalInfo.Email)}</a>");
        if (!string.IsNullOrWhiteSpace(resume.PersonalInfo.Phone))
            contacts.Add($"<span>{Escape(resume.PersonalInfo.Phone)}</span>");
        if (!string.IsNullOrWhiteSpace(resume.PersonalInfo.Location))
            contacts.Add($"<span>{Escape(resume.PersonalInfo.Location)}</span>");
        if (!string.IsNullOrWhiteSpace(resume.PersonalInfo.LinkedIn))
            contacts.Add($"<a href=\"{Escape(resume.PersonalInfo.LinkedIn)}\" target=\"_blank\">{Escape(resume.PersonalInfo.LinkedIn)}</a>");
        if (!string.IsNullOrWhiteSpace(resume.PersonalInfo.Website))
            contacts.Add($"<a href=\"{Escape(resume.PersonalInfo.Website)}\" target=\"_blank\">{Escape(resume.PersonalInfo.Website)}</a>");

        sb.AppendLine($"        {string.Join(" <span class=\"separator\">|</span> ", contacts)}");
        sb.AppendLine("      </div>");
        sb.AppendLine("    </header>");

        // Summary
        if (!string.IsNullOrWhiteSpace(resume.Summary))
        {
            sb.AppendLine("    <section class=\"summary\">");
            sb.AppendLine("      <h2>Professional Summary</h2>");
            sb.AppendLine($"      <p>{Escape(resume.Summary)}</p>");
            sb.AppendLine("    </section>");
        }

        // Experience
        if (resume.Experiences.Any())
        {
            sb.AppendLine("    <section class=\"experience\">");
            sb.AppendLine("      <h2>Work Experience</h2>");
            foreach (var exp in resume.Experiences.OrderBy(e => e.Order))
            {
                sb.AppendLine("      <div class=\"entry\">");
                sb.AppendLine($"        <div class=\"entry-header\">");
                sb.AppendLine($"          <h3>{Escape(exp.JobTitle)}</h3>");
                sb.AppendLine($"          <span class=\"date\">{Escape(exp.DateRange)}</span>");
                sb.AppendLine($"        </div>");
                sb.AppendLine($"        <p class=\"company\">{Escape(exp.Company)}{(!string.IsNullOrWhiteSpace(exp.Location) ? $" | {Escape(exp.Location)}" : "")}</p>");

                if (!string.IsNullOrWhiteSpace(exp.Description))
                    sb.AppendLine($"        <p class=\"description\">{Escape(exp.Description)}</p>");

                if (exp.Achievements.Any())
                {
                    sb.AppendLine("        <ul>");
                    foreach (var ach in exp.Achievements)
                        sb.AppendLine($"          <li>{Escape(ach)}</li>");
                    sb.AppendLine("        </ul>");
                }
                sb.AppendLine("      </div>");
            }
            sb.AppendLine("    </section>");
        }

        // Education
        if (resume.EducationList.Any())
        {
            sb.AppendLine("    <section class=\"education\">");
            sb.AppendLine("      <h2>Education</h2>");
            foreach (var edu in resume.EducationList.OrderBy(e => e.Order))
            {
                sb.AppendLine("      <div class=\"entry\">");
                sb.AppendLine($"        <div class=\"entry-header\">");
                sb.AppendLine($"          <h3>{Escape(edu.DegreeWithField)}</h3>");
                sb.AppendLine($"          <span class=\"date\">{Escape(edu.DateRange)}</span>");
                sb.AppendLine($"        </div>");
                sb.AppendLine($"        <p class=\"institution\">{Escape(edu.Institution)}{(!string.IsNullOrWhiteSpace(edu.Location) ? $" | {Escape(edu.Location)}" : "")}</p>");
                if (!string.IsNullOrWhiteSpace(edu.Grade))
                    sb.AppendLine($"        <p class=\"grade\">Grade: {Escape(edu.Grade)}</p>");
                sb.AppendLine("      </div>");
            }
            sb.AppendLine("    </section>");
        }

        // Skills
        if (resume.Skills.Any())
        {
            sb.AppendLine("    <section class=\"skills\">");
            sb.AppendLine("      <h2>Skills</h2>");
            sb.AppendLine("      <div class=\"skill-list\">");
            foreach (var skill in resume.Skills.OrderBy(s => s.Order))
            {
                sb.AppendLine($"        <span class=\"skill-badge\">{Escape(skill.Name)}</span>");
            }
            sb.AppendLine("      </div>");
            sb.AppendLine("    </section>");
        }

        // Languages
        if (resume.Languages.Any())
        {
            sb.AppendLine("    <section class=\"languages\">");
            sb.AppendLine("      <h2>Languages</h2>");
            sb.AppendLine("      <ul>");
            foreach (var lang in resume.Languages.OrderBy(l => l.Order))
            {
                sb.AppendLine($"        <li>{Escape(lang.Name)} - {GetLanguageProficiencyText(lang.Proficiency)}</li>");
            }
            sb.AppendLine("      </ul>");
            sb.AppendLine("    </section>");
        }

        // Certifications
        if (resume.Certifications.Any())
        {
            sb.AppendLine("    <section class=\"certifications\">");
            sb.AppendLine("      <h2>Certifications</h2>");
            sb.AppendLine("      <ul>");
            foreach (var cert in resume.Certifications.OrderBy(c => c.Order))
            {
                var certText = cert.Name;
                if (!string.IsNullOrWhiteSpace(cert.IssuingOrganization))
                    certText += $" - {cert.IssuingOrganization}";
                if (cert.IssueDate.HasValue)
                    certText += $" ({cert.IssueDate.Value:MMM yyyy})";
                sb.AppendLine($"        <li>{Escape(certText)}</li>");
            }
            sb.AppendLine("      </ul>");
            sb.AppendLine("    </section>");
        }

        // Projects
        if (resume.Projects.Any())
        {
            sb.AppendLine("    <section class=\"projects\">");
            sb.AppendLine("      <h2>Projects</h2>");
            foreach (var proj in resume.Projects.OrderBy(p => p.Order))
            {
                sb.AppendLine("      <div class=\"entry\">");
                sb.AppendLine($"        <h3>{Escape(proj.Name)}</h3>");
                if (!string.IsNullOrWhiteSpace(proj.Description))
                    sb.AppendLine($"        <p class=\"description\">{Escape(proj.Description)}</p>");
                if (proj.Technologies.Any())
                    sb.AppendLine($"        <p class=\"tech\">Technologies: {Escape(string.Join(", ", proj.Technologies))}</p>");
                sb.AppendLine("      </div>");
            }
            sb.AppendLine("    </section>");
        }

        sb.AppendLine("  </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private static string GetStyles(string accentColor, string fontFamily)
    {
        return $@"
    :root {{
      --accent-color: {accentColor};
      --text-color: #333;
      --light-gray: #666;
    }}
    * {{ margin: 0; padding: 0; box-sizing: border-box; }}
    body {{
      font-family: '{fontFamily}', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
      line-height: 1.6;
      color: var(--text-color);
      background: #f5f5f5;
    }}
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
      margin-bottom: 25px;
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
    }}
    .entry h3 {{
      font-size: 1.1em;
    }}
    .date {{
      color: var(--light-gray);
      font-size: 0.9em;
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

    private static string GetLanguageProficiencyText(LanguageProficiency level) => level switch
    {
        LanguageProficiency.Basic => "Basic",
        LanguageProficiency.Conversational => "Conversational",
        LanguageProficiency.Professional => "Professional",
        LanguageProficiency.Fluent => "Fluent",
        LanguageProficiency.Native => "Native",
        _ => "Professional"
    };
}
