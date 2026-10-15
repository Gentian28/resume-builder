using System.Net.Http.Headers;
using System.Net.Http.Json;
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
    private sealed record Configuration(string BaseUrl, string Model, string? ApiKey);
    private Configuration _configuration = new(OpenAiBaseUrl, DefaultModel, null);

    /// <summary>
    /// A local server needs no key; a remote one does. Checking the host (rather than searching the
    /// URL for "localhost") also covers 127.0.0.1 and ::1.
    /// </summary>
    public override bool IsConfigured => !string.IsNullOrEmpty(_configuration.ApiKey) || IsLocalEndpoint(_configuration.BaseUrl);

    /// <summary>The endpoint requests are sent to - surfaced so the UI can say where data goes.</summary>
    public string BaseUrl => _configuration.BaseUrl;

    public string Model => _configuration.Model;

    public bool IsLocal => IsLocalEndpoint(_configuration.BaseUrl);

    private static bool IsLocalEndpoint(string baseUrl) =>
        Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) && uri.IsLoopback;

    public LocalAiService() : this(new HttpClientHandler { AllowAutoRedirect = false })
    {
    }

    public LocalAiService(HttpMessageHandler handler) => _httpClient = new HttpClient(handler);

    public override void Configure(string apiKey, string? model = null)
    {
        var current = _configuration;
        ConfigureEndpoint(current.BaseUrl, model ?? current.Model, apiKey);
    }

    /// <summary>
    /// Configure for a local LLM server (e.g., Ollama, LM Studio).
    /// </summary>
    public void ConfigureLocal(string baseUrl, string model)
    {
        ConfigureEndpoint(baseUrl, model, null);
    }

    public void ConfigureEndpoint(string baseUrl, string model, string? apiKey)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) ||
            (!uri.IsLoopback && uri.Scheme != Uri.UriSchemeHttps) ||
            !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            throw new ArgumentException("Use HTTPS for remote AI endpoints, or HTTP on localhost.", nameof(baseUrl));

        if (string.IsNullOrWhiteSpace(model))
            throw new ArgumentException("Choose an AI model.", nameof(model));

        _configuration = new Configuration(uri.AbsoluteUri.TrimEnd('/'), model.Trim(),
            string.IsNullOrWhiteSpace(apiKey) ? null : apiKey.Trim());
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
            var configuration = _configuration;
            var request = new ChatRequest
            {
                Model = configuration.Model,
                Messages = new[]
                {
                    new ChatMessage { Role = "system", Content = SystemPrompt },
                    new ChatMessage { Role = "user", Content = prompt }
                },
                MaxTokens = 500,
                Temperature = 0.7
            };

            using var message = new HttpRequestMessage(HttpMethod.Post, $"{configuration.BaseUrl}/chat/completions")
            {
                Content = JsonContent.Create(request)
            };
            if (configuration.ApiKey is { } key)
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);

            using var response = await _httpClient.SendAsync(message, cancellationToken);

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
