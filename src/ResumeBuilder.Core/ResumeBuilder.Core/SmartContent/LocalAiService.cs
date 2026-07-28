using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ResumeBuilder.Core.SmartContent;

/// <summary>
/// AI service that uses OpenAI-compatible APIs (OpenAI, local LLMs like Ollama, etc.)
/// </summary>
public class LocalAiService : PromptBasedAiService, IDisposable
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
    public override bool IsConfigured => !string.IsNullOrEmpty(_apiKey) || IsLocalEndpoint(_baseUrl);

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

    public override void Configure(string apiKey, string? model = null)
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

    protected override async Task<AiResult<string>> SendAsync(string prompt, CancellationToken cancellationToken)
    {
        try
        {
            var request = new ChatRequest
            {
                Model = _model,
                Messages = new[]
                {
                    new ChatMessage { Role = "system", Content = SystemPrompt },
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
