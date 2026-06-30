namespace ResumeBuilder.Core.SmartContent;

/// <summary>
/// Types of AI suggestions that can be generated.
/// </summary>
public enum SuggestionType
{
    Summary,
    ExperienceBullet,
    SkillSuggestion,
    JobDescription,
    Improvement
}

/// <summary>
/// Represents an AI-generated suggestion.
/// </summary>
public class AiSuggestion
{
    public required SuggestionType Type { get; init; }
    public required string Content { get; init; }
    public string? Context { get; init; }
    public float Confidence { get; init; } = 1.0f;
}

/// <summary>
/// Result of an AI operation.
/// </summary>
public class AiResult<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? ErrorMessage { get; init; }

    public static AiResult<T> Succeeded(T data) => new() { Success = true, Data = data };
    public static AiResult<T> Failed(string error) => new() { Success = false, ErrorMessage = error };
}

/// <summary>
/// Interface for AI-powered content assistance.
/// </summary>
public interface IAiService
{
    /// <summary>
    /// Whether the service is configured and ready to use.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Configure the service with an API key.
    /// </summary>
    void Configure(string apiKey, string? model = null);

    /// <summary>
    /// Generate a professional summary based on experiences and skills.
    /// </summary>
    Task<AiResult<string>> GenerateSummaryAsync(
        string jobTitle,
        IEnumerable<string> experiences,
        IEnumerable<string> skills,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Improve an achievement bullet point to be more impactful.
    /// </summary>
    Task<AiResult<IEnumerable<string>>> ImproveAchievementAsync(
        string achievement,
        string? jobContext = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Suggest relevant skills based on job title and experience.
    /// </summary>
    Task<AiResult<IEnumerable<string>>> SuggestSkillsAsync(
        string jobTitle,
        IEnumerable<string> currentSkills,
        IEnumerable<string>? experiences = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Optimize text for a specific job description.
    /// </summary>
    Task<AiResult<string>> OptimizeForJobAsync(
        string content,
        string jobDescription,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get general improvement suggestions for the entire resume.
    /// </summary>
    Task<AiResult<IEnumerable<AiSuggestion>>> GetImprovementSuggestionsAsync(
        string resumeContent,
        CancellationToken cancellationToken = default);
}
