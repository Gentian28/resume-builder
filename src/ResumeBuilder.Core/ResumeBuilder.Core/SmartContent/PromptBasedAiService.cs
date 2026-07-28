namespace ResumeBuilder.Core.SmartContent;

/// <summary>
/// The résumé-writing half of <see cref="IAiService"/>: every prompt, and the parsing of every
/// reply. Providers supply only transport.
///
/// This exists so the two providers cannot drift. The prompts here are the product — the wording
/// that stops summaries reading like "results-driven team player", the bullet format the parsers
/// depend on — and a user who switches provider is switching where their text goes, not which
/// features work or how good the output is.
/// </summary>
public abstract class PromptBasedAiService : IAiService
{
    /// <summary>Applies to every request, whichever provider serves it.</summary>
    protected const string SystemPrompt =
        "You are a professional resume writing assistant. Provide concise, actionable suggestions.";

    public abstract bool IsConfigured { get; }

    public abstract void Configure(string apiKey, string? model = null);

    /// <summary>
    /// Send one prompt and return the reply text. The only thing a provider has to implement.
    /// Implementations report failure as <see cref="AiResult{T}.Failed"/> rather than throwing —
    /// AI features degrade, they don't crash the editor.
    /// </summary>
    protected abstract Task<AiResult<string>> SendAsync(string prompt, CancellationToken cancellationToken);

    public async Task<AiResult<string>> GenerateSummaryAsync(
        string jobTitle,
        IEnumerable<string> experiences,
        IEnumerable<string> skills,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return AiResult<string>.Failed("AI service not configured. Please set API key in settings.");

        var prompt = $@"Write a professional resume summary (2-3 sentences) for a {jobTitle}.

Experience highlights:
{string.Join("\n- ", experiences.Take(5))}

Key skills: {string.Join(", ", skills.Take(10))}

Write in first person, be concise, and highlight key strengths. Do not use generic phrases like 'results-driven' or 'team player'. Focus on specific achievements and expertise.";

        return await SendAsync(prompt, cancellationToken);
    }

    public async Task<AiResult<IEnumerable<string>>> ImproveAchievementAsync(
        string achievement,
        string? jobContext = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return AiResult<IEnumerable<string>>.Failed("AI service not configured.");

        var contextPart = !string.IsNullOrEmpty(jobContext)
            ? $" in the context of {jobContext}"
            : "";

        var prompt = $@"Improve this resume achievement bullet point{contextPart}. Make it more impactful using the STAR method (Situation, Task, Action, Result). Include metrics where possible.

Original: {achievement}

Provide exactly 3 alternative versions, each on a new line starting with '- '. Be specific and quantify results when possible.";

        var result = await SendAsync(prompt, cancellationToken);
        if (!result.Success || result.Data == null)
            return AiResult<IEnumerable<string>>.Failed(result.ErrorMessage ?? "Failed to generate suggestions");

        return AiResult<IEnumerable<string>>.Succeeded(ParseBulletList(result.Data));
    }

    public async Task<AiResult<IEnumerable<string>>> SuggestSkillsAsync(
        string jobTitle,
        IEnumerable<string> currentSkills,
        IEnumerable<string>? experiences = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return AiResult<IEnumerable<string>>.Failed("AI service not configured.");

        var experiencePart = experiences?.Any() == true
            ? $"\n\nBased on this experience:\n{string.Join("\n- ", experiences.Take(5))}"
            : "";

        var prompt = $@"Suggest 5-8 relevant skills for a {jobTitle} position that are NOT already listed.

Current skills: {string.Join(", ", currentSkills)}{experiencePart}

List only skill names (no descriptions), one per line starting with '- '. Focus on in-demand technical and soft skills for this role.";

        var result = await SendAsync(prompt, cancellationToken);
        if (!result.Success || result.Data == null)
            return AiResult<IEnumerable<string>>.Failed(result.ErrorMessage ?? "Failed to generate suggestions");

        return AiResult<IEnumerable<string>>.Succeeded(ParseBulletList(result.Data));
    }

    public async Task<AiResult<string>> OptimizeForJobAsync(
        string content,
        string jobDescription,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return AiResult<string>.Failed("AI service not configured.");

        var prompt = $@"Rewrite this resume content to better match the following job description. Keep the same structure but optimize keyword usage and emphasis.

Job Description:
{jobDescription[..Math.Min(1000, jobDescription.Length)]}

Current Content:
{content}

Rewrite to highlight relevant experience and use keywords from the job description naturally. Maintain professional tone.";

        return await SendAsync(prompt, cancellationToken);
    }

    public async Task<AiResult<IEnumerable<AiSuggestion>>> GetImprovementSuggestionsAsync(
        string resumeContent,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return AiResult<IEnumerable<AiSuggestion>>.Failed("AI service not configured.");

        var prompt = $@"Review this resume content and provide 3-5 specific improvement suggestions. Focus on:
1. Content gaps or weak points
2. Quantification opportunities (adding metrics)
3. Clarity and impact of language
4. Missing relevant information

Resume:
{resumeContent[..Math.Min(2000, resumeContent.Length)]}

For each suggestion, format as:
TYPE: [Summary/ExperienceBullet/SkillSuggestion/Improvement]
SUGGESTION: [Your specific suggestion]
---";

        var result = await SendAsync(prompt, cancellationToken);
        if (!result.Success || result.Data == null)
            return AiResult<IEnumerable<AiSuggestion>>.Failed(result.ErrorMessage ?? "Failed to analyze");

        return AiResult<IEnumerable<AiSuggestion>>.Succeeded(ParseSuggestions(result.Data));
    }

    /// <summary>
    /// Reads the "one per line, starting with -" format the prompts ask for. Kept here because both
    /// providers get the same shape back, and a model that ignores the format should degrade to an
    /// empty list rather than dumping prose into the user's skills.
    /// </summary>
    protected static List<string> ParseBulletList(string text) =>
        text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(l => l.TrimStart().StartsWith('-'))
            .Select(l => l.TrimStart('-', ' ').Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

    /// <summary>
    /// Reads the TYPE/SUGGESTION block format. Carried over unchanged from the OpenAI-compatible
    /// service so extracting it stays a pure move — an unrecognised TYPE still falls back to
    /// <see cref="SuggestionType.Improvement"/> rather than dropping the suggestion.
    /// </summary>
    protected static List<AiSuggestion> ParseSuggestions(string response)
    {
        var suggestions = new List<AiSuggestion>();

        foreach (var block in response.Split("---", StringSplitOptions.RemoveEmptyEntries))
        {
            string? type = null;
            string? suggestion = null;

            foreach (var line in block.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.StartsWith("TYPE:", StringComparison.OrdinalIgnoreCase))
                    type = line[5..].Trim();
                else if (line.StartsWith("SUGGESTION:", StringComparison.OrdinalIgnoreCase))
                    suggestion = line[11..].Trim();
            }

            if (!string.IsNullOrEmpty(suggestion))
            {
                suggestions.Add(new AiSuggestion
                {
                    Type = ParseSuggestionType(type),
                    Content = suggestion
                });
            }
        }

        return suggestions;
    }

    private static SuggestionType ParseSuggestionType(string? type) => type?.ToLowerInvariant() switch
    {
        "summary" => SuggestionType.Summary,
        "experiencebullet" => SuggestionType.ExperienceBullet,
        "skillsuggestion" => SuggestionType.SkillSuggestion,
        "jobdescription" => SuggestionType.JobDescription,
        _ => SuggestionType.Improvement
    };
}
