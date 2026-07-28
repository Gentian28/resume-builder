using Anthropic;
using Anthropic.Models.Messages;

namespace ResumeBuilder.Core.SmartContent;

/// <summary>
/// AI service backed by Anthropic's Messages API.
///
/// Separate from <see cref="LocalAiService"/> rather than another base URL on it, because
/// Anthropic's API is not OpenAI-compatible: different endpoint shape, different auth header, and
/// a response whose content is a list of typed blocks rather than a single message string. Sharing
/// one HTTP path would mean branching on provider at every step.
/// </summary>
public class AnthropicAiService : PromptBasedAiService
{
    /// <summary>
    /// Anthropic's current flagship. Overridable via <see cref="Configure"/> for anyone who wants a
    /// cheaper or faster model.
    /// </summary>
    public const string DefaultModel = "claude-opus-5";

    private AnthropicClient? _client;
    private string? _apiKey;
    private string _model = DefaultModel;

    /// <summary>
    /// Always a remote endpoint, so unlike the OpenAI-compatible service there is no key-free case:
    /// no key means not configured.
    /// </summary>
    public override bool IsConfigured => !string.IsNullOrEmpty(_apiKey);

    public string Model => _model;

    public override void Configure(string apiKey, string? model = null)
    {
        _apiKey = apiKey;
        if (!string.IsNullOrEmpty(model))
            _model = model;

        _client = string.IsNullOrEmpty(_apiKey)
            ? null
            : new AnthropicClient { ApiKey = _apiKey };
    }

    protected override async Task<AiResult<string>> SendAsync(string prompt, CancellationToken cancellationToken)
    {
        if (_client is null)
            return AiResult<string>.Failed("Anthropic API key not set.");

        try
        {
            var response = await _client.Messages.Create(new MessageCreateParams
            {
                Model = _model,
                // Generous because thinking and the reply share this budget on current models: a
                // limit sized to the answer alone would truncate mid-sentence.
                MaxTokens = 4096,
                // These are short rewriting tasks, not research. Low effort keeps the editor
                // responsive; the quality difference on a three-sentence summary is not worth
                // the wait.
                OutputConfig = new OutputConfig { Effort = Effort.Low },
                System = new List<TextBlockParam> { new() { Text = SystemPrompt } },
                Messages = [new() { Role = Role.User, Content = prompt }],
            });

            // A safety decline arrives as a normal 200 with no text, not an exception - check
            // before reading content, or an unrelated résumé trips an index error.
            if (response.StopReason == "refusal")
                return AiResult<string>.Failed("Anthropic declined this request.");

            var text = string.Concat(
                response.Content.Select(b => b.Value).OfType<TextBlock>().Select(b => b.Text));

            return string.IsNullOrWhiteSpace(text)
                ? AiResult<string>.Failed("Empty response from Anthropic.")
                : AiResult<string>.Succeeded(text.Trim());
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Same contract as the OpenAI-compatible service: surface the provider's own wording,
            // which says "invalid x-api-key" where a status code alone would not.
            return AiResult<string>.Failed($"Anthropic error: {ex.Message}");
        }
    }
}
