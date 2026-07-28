using AwesomeAssertions;
using Xunit;
using ResumeBuilder.Core.SmartContent;

namespace ResumeBuilder.Tests;

/// <summary>
/// The one thing the rest of the suite cannot cover: an actual request to Anthropic.
///
/// Everything else about the provider is verified offline — configuration, isolation between
/// providers, graceful failure — but none of it proves the request shape is accepted or that the
/// reply parses. Compiling proves the SDK's type names exist; only a real call proves the model ID,
/// the effort level, and the response handling are right.
///
/// Skipped unless ANTHROPIC_API_KEY is set, so the default suite stays offline and deterministic:
///
///     $env:ANTHROPIC_API_KEY = "sk-ant-..."
///     dotnet test ResumeBuilder.sln --filter "FullyQualifiedName~AnthropicLiveTests"
///
/// Costs a fraction of a cent per run.
/// </summary>
public class AnthropicLiveTests
{
    private static string? ApiKey => Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");

    [SkippableFact]
    public async Task GenerateSummary_ReturnsUsableProse()
    {
        Skip.If(string.IsNullOrWhiteSpace(ApiKey), "ANTHROPIC_API_KEY is not set.");

        var service = new AnthropicAiService();
        service.Configure(ApiKey!);

        var result = await service.GenerateSummaryAsync(
            "Senior Software Engineer",
            ["Cut data ingestion time from 18 hours to 5 minutes", "Led a team of four"],
            ["C#", ".NET", "Azure"]);

        result.ErrorMessage.Should().BeNull("the request should be accepted as constructed");
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNullOrWhiteSpace();

        // Proves the response is prose we can drop into the summary field, not a raw block dump or
        // leaked reasoning. The second assertion is the one that catches thinking bleeding through.
        result.Data!.Length.Should().BeGreaterThan(40);
        result.Data.Should().NotContain("<thinking>");
    }

    [SkippableFact]
    public async Task SuggestSkills_ParsesTheBulletFormat()
    {
        Skip.If(string.IsNullOrWhiteSpace(ApiKey), "ANTHROPIC_API_KEY is not set.");

        var service = new AnthropicAiService();
        service.Configure(ApiKey!);

        var result = await service.SuggestSkillsAsync("Data Engineer", ["SQL", "Python"]);

        result.Success.Should().BeTrue(result.ErrorMessage);

        // The prompt asks for "- one per line"; if the model answers in prose the shared parser
        // yields nothing, and the user sees an empty suggestion list rather than an error. That
        // silent-empty case is exactly what this pins.
        result.Data.Should().NotBeNull();
        result.Data!.Should().NotBeEmpty("the reply should match the bullet format the parser expects");
    }

    [SkippableFact]
    public async Task BadKey_FailsWithTheProvidersOwnWording()
    {
        Skip.If(string.IsNullOrWhiteSpace(ApiKey), "ANTHROPIC_API_KEY is not set.");

        var service = new AnthropicAiService();
        service.Configure("sk-ant-obviously-not-valid");

        var result = await service.GenerateSummaryAsync("Engineer", ["Did work"], ["C#"]);

        // A wrong key must surface as a message the user can act on, not an unhandled exception.
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
    }
}
