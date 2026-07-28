namespace ResumeBuilder.Core.SmartContent;

/// <summary>Which backend AI requests go to.</summary>
public enum AiProvider
{
    /// <summary>OpenAI itself, or any OpenAI-compatible server — including a local LLM.</summary>
    OpenAiCompatible,

    /// <summary>Anthropic's Messages API.</summary>
    Anthropic
}

/// <summary>
/// Presents the two providers as one <see cref="IAiService"/> and forwards to whichever is active.
///
/// Both instances are kept alive rather than one being rebuilt on each switch, so each keeps its
/// own endpoint, model, and key. Switching to Anthropic and back does not make the user re-enter
/// their OpenAI settings, and neither provider's key is ever handed to the other.
/// </summary>
public class AiProviderRouter : IAiService, IDisposable
{
    public LocalAiService OpenAiCompatible { get; } = new();

    public AnthropicAiService Anthropic { get; } = new();

    /// <summary>The provider that serves requests. Defaults to the OpenAI-compatible path.</summary>
    public AiProvider Active { get; set; } = AiProvider.OpenAiCompatible;

    private IAiService Current =>
        Active == AiProvider.Anthropic ? Anthropic : (IAiService)OpenAiCompatible;

    /// <summary>
    /// Reflects the active provider only. A configured Anthropic key does not make an
    /// unconfigured OpenAI endpoint usable, so the UI must not read this as "some provider works".
    /// </summary>
    public bool IsConfigured => Current.IsConfigured;

    public void Configure(string apiKey, string? model = null) => Current.Configure(apiKey, model);

    public Task<AiResult<string>> GenerateSummaryAsync(
        string jobTitle,
        IEnumerable<string> experiences,
        IEnumerable<string> skills,
        CancellationToken cancellationToken = default) =>
        Current.GenerateSummaryAsync(jobTitle, experiences, skills, cancellationToken);

    public Task<AiResult<IEnumerable<string>>> ImproveAchievementAsync(
        string achievement,
        string? jobContext = null,
        CancellationToken cancellationToken = default) =>
        Current.ImproveAchievementAsync(achievement, jobContext, cancellationToken);

    public Task<AiResult<IEnumerable<string>>> SuggestSkillsAsync(
        string jobTitle,
        IEnumerable<string> currentSkills,
        IEnumerable<string>? experiences = null,
        CancellationToken cancellationToken = default) =>
        Current.SuggestSkillsAsync(jobTitle, currentSkills, experiences, cancellationToken);

    public Task<AiResult<string>> OptimizeForJobAsync(
        string content,
        string jobDescription,
        CancellationToken cancellationToken = default) =>
        Current.OptimizeForJobAsync(content, jobDescription, cancellationToken);

    public Task<AiResult<IEnumerable<AiSuggestion>>> GetImprovementSuggestionsAsync(
        string resumeContent,
        CancellationToken cancellationToken = default) =>
        Current.GetImprovementSuggestionsAsync(resumeContent, cancellationToken);

    public void Dispose() => OpenAiCompatible.Dispose();
}
