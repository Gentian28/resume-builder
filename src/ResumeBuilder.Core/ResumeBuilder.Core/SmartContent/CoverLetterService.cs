using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Core.SmartContent;

/// <summary>
/// Drafts cover letters from a resume and a job posting. Works without AI configured: it falls back
/// to a structured template built from the resume's own content, so the feature is never a dead end.
/// </summary>
public class CoverLetterService
{
    private readonly IAiService _ai;

    public CoverLetterService(IAiService ai)
    {
        _ai = ai;
    }

    public async Task<AiResult<CoverLetter>> DraftAsync(
        Resume resume,
        string companyName,
        string targetRole,
        string jobDescription,
        CancellationToken cancellationToken = default)
    {
        var letter = CoverLetter.FromResume(resume, companyName, targetRole);
        letter.JobDescription = jobDescription;

        if (!_ai.IsConfigured)
        {
            letter.Paragraphs = BuildFallbackParagraphs(resume, companyName, targetRole);
            return AiResult<CoverLetter>.Succeeded(letter);
        }

        var prompt = BuildPrompt(resume, companyName, targetRole, jobDescription);
        var result = await _ai.OptimizeForJobAsync(prompt, jobDescription, cancellationToken);

        if (!result.Success || string.IsNullOrWhiteSpace(result.Data))
        {
            // Still hand back a usable letter rather than nothing.
            letter.Paragraphs = BuildFallbackParagraphs(resume, companyName, targetRole);
            return AiResult<CoverLetter>.Succeeded(letter);
        }

        letter.Paragraphs = SplitParagraphs(result.Data!);
        return AiResult<CoverLetter>.Succeeded(letter);
    }

    private static string BuildPrompt(Resume resume, string companyName, string targetRole, string jobDescription)
    {
        var highlights = resume.Experiences
            .OrderBy(e => e.Order)
            .Take(2)
            .SelectMany(e => e.Achievements.Take(2).Select(a => $"- {a} ({e.JobTitle} at {e.Company})"))
            .ToList();

        var skills = resume.Skills.Take(10).Select(s => s.Name);

        return $"""
            Write a cover letter body of three or four short paragraphs for this application.
            Return only the paragraphs, separated by blank lines. Do not include a salutation,
            a closing, or a signature.

            Role: {targetRole}
            Company: {companyName}

            Candidate: {resume.PersonalInfo.FullName}, {resume.PersonalInfo.JobTitle}
            Summary: {resume.Summary}
            Key skills: {string.Join(", ", skills)}
            Selected achievements:
            {string.Join(Environment.NewLine, highlights)}

            Job description:
            {jobDescription}
            """;
    }

    private static List<string> BuildFallbackParagraphs(Resume resume, string companyName, string targetRole)
    {
        var company = string.IsNullOrWhiteSpace(companyName) ? "your team" : companyName;
        var role = string.IsNullOrWhiteSpace(targetRole) ? "this role" : targetRole;

        var paragraphs = new List<string>
        {
            $"I am writing to apply for {role} at {company}. " +
            (string.IsNullOrWhiteSpace(resume.Summary)
                ? $"I am a {resume.PersonalInfo.JobTitle} and believe my background is a strong match for what you are looking for."
                : resume.Summary)
        };

        var topExperience = resume.Experiences.OrderBy(e => e.Order).FirstOrDefault();
        if (topExperience != null)
        {
            var achievements = topExperience.Achievements.Take(2).ToList();
            var achievementText = achievements.Count > 0
                ? " " + string.Join(" ", achievements.Select(a => a.TrimEnd('.') + "."))
                : string.Empty;

            paragraphs.Add(
                $"Most recently, as {topExperience.JobTitle} at {topExperience.Company}, " +
                $"I {(string.IsNullOrWhiteSpace(topExperience.Description) ? "led work across the team" : LowerFirst(topExperience.Description.TrimEnd('.')))}." +
                achievementText);
        }

        var skills = resume.Skills.Take(6).Select(s => s.Name).ToList();
        if (skills.Count > 0)
        {
            paragraphs.Add($"My core skills include {string.Join(", ", skills)}, and I would welcome the chance to bring them to {company}.");
        }

        paragraphs.Add($"Thank you for your time and consideration. I would be glad to discuss how I can contribute to {company}.");

        return paragraphs;
    }

    private static string LowerFirst(string value) =>
        string.IsNullOrEmpty(value) ? value : char.ToLowerInvariant(value[0]) + value[1..];

    private static List<string> SplitParagraphs(string text) =>
        text.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => p.Length > 0)
            .ToList();
}
