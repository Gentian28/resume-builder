using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ResumeBuilder.Core.SmartContent;

/// <summary>
/// AI service that uses OpenAI-compatible APIs (OpenAI, local LLMs like Ollama, etc.)
/// </summary>
public class LocalAiService : IAiService, IDisposable
{
    public const string OpenAiBaseUrl = "https://api.openai.com/v1";
    public const string DefaultModel = "gpt-4o-mini";

    private readonly HttpClient _httpClient;
    private string? _apiKey;
    private string _model = DefaultModel;
    private string _baseUrl = OpenAiBaseUrl;

    /// <summary>
    /// A local server needs no key; a remote one does. Checking the host (rather than searching the
    /// URL for "localhost") also covers 127.0.0.1 and ::1.
    /// </summary>
    public bool IsConfigured => !string.IsNullOrEmpty(_apiKey) || IsLocalEndpoint(_baseUrl);

    /// <summary>The endpoint requests are sent to - surfaced so the UI can say where data goes.</summary>
    public string BaseUrl => _baseUrl;

    public string Model => _model;

    public bool IsLocal => IsLocalEndpoint(_baseUrl);

    private static bool IsLocalEndpoint(string baseUrl) =>
        Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) && uri.IsLoopback;

    public LocalAiService()
    {
        _httpClient = new HttpClient();
    }

    public void Configure(string apiKey, string? model = null)
    {
        _apiKey = apiKey;
        if (!string.IsNullOrEmpty(model))
            _model = model;

        if (!string.IsNullOrEmpty(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }
    }

    /// <summary>
    /// Configure for a local LLM server (e.g., Ollama, LM Studio).
    /// </summary>
    public void ConfigureLocal(string baseUrl, string model)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _model = model;
        _apiKey = "local"; // Mark as configured
    }

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

        return await SendChatRequestAsync(prompt, cancellationToken);
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

        var result = await SendChatRequestAsync(prompt, cancellationToken);
        if (!result.Success || result.Data == null)
            return AiResult<IEnumerable<string>>.Failed(result.ErrorMessage ?? "Failed to generate suggestions");

        var suggestions = result.Data
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(l => l.TrimStart().StartsWith("-"))
            .Select(l => l.TrimStart('-', ' '))
            .ToList();

        return AiResult<IEnumerable<string>>.Succeeded(suggestions);
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

        var result = await SendChatRequestAsync(prompt, cancellationToken);
        if (!result.Success || result.Data == null)
            return AiResult<IEnumerable<string>>.Failed(result.ErrorMessage ?? "Failed to generate suggestions");

        var suggestions = result.Data
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(l => l.TrimStart().StartsWith("-"))
            .Select(l => l.TrimStart('-', ' ').Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        return AiResult<IEnumerable<string>>.Succeeded(suggestions);
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
{jobDescription.Substring(0, Math.Min(1000, jobDescription.Length))}

Current Content:
{content}

Rewrite to highlight relevant experience and use keywords from the job description naturally. Maintain professional tone.";

        return await SendChatRequestAsync(prompt, cancellationToken);
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
{resumeContent.Substring(0, Math.Min(2000, resumeContent.Length))}

For each suggestion, format as:
TYPE: [Summary/ExperienceBullet/SkillSuggestion/Improvement]
SUGGESTION: [Your specific suggestion]
---";

        var result = await SendChatRequestAsync(prompt, cancellationToken);
        if (!result.Success || result.Data == null)
            return AiResult<IEnumerable<AiSuggestion>>.Failed(result.ErrorMessage ?? "Failed to analyze");

        var suggestions = ParseSuggestions(result.Data);
        return AiResult<IEnumerable<AiSuggestion>>.Succeeded(suggestions);
    }

    /// <summary>Pulls <c>error.message</c> out of an OpenAI-style error body, if it looks like one.</summary>
    private static string? ExtractErrorMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;

        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("error", out var error) &&
                error.TryGetProperty("message", out var message))
            {
                return message.GetString();
            }
        }
        catch (JsonException)
        {
            // Not JSON - fall through and use the raw body.
        }

        return body.Length > 200 ? body[..200] : body;
    }

    private async Task<AiResult<string>> SendChatRequestAsync(string prompt, CancellationToken cancellationToken)
    {
        try
        {
            var request = new ChatRequest
            {
                Model = _model,
                Messages = new[]
                {
                    new ChatMessage { Role = "system", Content = "You are a professional resume writing assistant. Provide concise, actionable suggestions." },
                    new ChatMessage { Role = "user", Content = prompt }
                },
                MaxTokens = 500,
                Temperature = 0.7
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_baseUrl}/chat/completions",
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                // Surface the provider's message - "invalid api key" is far more useful than "401".
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                var detail = ExtractErrorMessage(error);
                return AiResult<string>.Failed(
                    string.IsNullOrWhiteSpace(detail)
                        ? $"API error: {(int)response.StatusCode} {response.StatusCode}"
                        : $"API error: {(int)response.StatusCode} {response.StatusCode} - {detail}");
            }

            var result = await response.Content.ReadFromJsonAsync<ChatResponse>(cancellationToken: cancellationToken);
            var content = result?.Choices?.FirstOrDefault()?.Message?.Content;

            if (string.IsNullOrEmpty(content))
                return AiResult<string>.Failed("Empty response from AI");

            return AiResult<string>.Succeeded(content);
        }
        catch (OperationCanceledException)
        {
            return AiResult<string>.Failed("Request cancelled");
        }
        catch (Exception ex)
        {
            return AiResult<string>.Failed($"Request failed: {ex.Message}");
        }
    }

    private List<AiSuggestion> ParseSuggestions(string response)
    {
        var suggestions = new List<AiSuggestion>();
        var blocks = response.Split("---", StringSplitOptions.RemoveEmptyEntries);

        foreach (var block in blocks)
        {
            var lines = block.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            string? type = null;
            string? suggestion = null;

            foreach (var line in lines)
            {
                if (line.StartsWith("TYPE:", StringComparison.OrdinalIgnoreCase))
                    type = line.Substring(5).Trim();
                else if (line.StartsWith("SUGGESTION:", StringComparison.OrdinalIgnoreCase))
                    suggestion = line.Substring(11).Trim();
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

    private SuggestionType ParseSuggestionType(string? type)
    {
        return type?.ToLowerInvariant() switch
        {
            "summary" => SuggestionType.Summary,
            "experiencebullet" => SuggestionType.ExperienceBullet,
            "skillsuggestion" => SuggestionType.SkillSuggestion,
            "jobdescription" => SuggestionType.JobDescription,
            _ => SuggestionType.Improvement
        };
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    // Request/Response models for OpenAI API
    private class ChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = "";

        [JsonPropertyName("messages")]
        public ChatMessage[] Messages { get; set; } = Array.Empty<ChatMessage>();

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }
    }

    private class ChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "";

        [JsonPropertyName("content")]
        public string Content { get; set; } = "";
    }

    private class ChatResponse
    {
        [JsonPropertyName("choices")]
        public ChatChoice[]? Choices { get; set; }
    }

    private class ChatChoice
    {
        [JsonPropertyName("message")]
        public ChatMessage? Message { get; set; }
    }
}
