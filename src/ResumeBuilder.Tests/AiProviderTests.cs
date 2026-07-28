using AwesomeAssertions;
using ResumeBuilder.Core.SmartContent;

namespace ResumeBuilder.Tests;

/// <summary>
/// Two providers now sit behind one <see cref="IAiService"/>. What matters is that picking one
/// never leaks into the other — a key typed for Anthropic must not end up on an OpenAI request —
/// and that an unconfigured provider degrades instead of throwing, which is what keeps the rest of
/// the app working without any AI set up at all.
/// </summary>
public class AiProviderTests
{
    [Fact]
    public void Router_DefaultsToTheOpenAiCompatiblePath()
    {
        var router = new AiProviderRouter();

        router.Active.Should().Be(AiProvider.OpenAiCompatible);
    }

    [Fact]
    public void Router_ConfigureAppliesToTheActiveProviderOnly()
    {
        var router = new AiProviderRouter { Active = AiProvider.Anthropic };

        router.Configure("sk-ant-test", "claude-opus-5");

        router.Anthropic.IsConfigured.Should().BeTrue();
        router.Anthropic.Model.Should().Be("claude-opus-5");

        // The OpenAI-compatible service must not have received the Anthropic key. It reports
        // configured only because its default endpoint is remote-with-no-key... which is false.
        router.OpenAiCompatible.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void Router_KeepsBothProvidersConfiguredAcrossASwitch()
    {
        var router = new AiProviderRouter();

        router.OpenAiCompatible.ConfigureLocal("http://localhost:11434/v1", "llama3");
        router.Anthropic.Configure("sk-ant-test", "claude-opus-5");

        router.Active = AiProvider.Anthropic;
        router.IsConfigured.Should().BeTrue();

        // Switching back must not have cost the user their local endpoint settings.
        router.Active = AiProvider.OpenAiCompatible;
        router.IsConfigured.Should().BeTrue();
        router.OpenAiCompatible.BaseUrl.Should().Be("http://localhost:11434/v1");
        router.OpenAiCompatible.Model.Should().Be("llama3");
    }

    [Fact]
    public void Router_IsConfiguredReflectsTheActiveProviderNotEither()
    {
        var router = new AiProviderRouter { Active = AiProvider.Anthropic };

        // A configured local endpoint must not make Anthropic look ready — otherwise the panel
        // would offer features that fail on the first call.
        router.OpenAiCompatible.ConfigureLocal("http://localhost:11434/v1", "llama3");

        router.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public async Task Anthropic_WithoutAKey_FailsRatherThanThrows()
    {
        var service = new AnthropicAiService();

        service.IsConfigured.Should().BeFalse();

        var result = await service.GenerateSummaryAsync("Engineer", ["Built things"], ["C#"]);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Anthropic_DefaultsToTheCurrentFlagshipModel()
    {
        var service = new AnthropicAiService();
        service.Configure("sk-ant-test");

        service.Model.Should().Be(AnthropicAiService.DefaultModel);
        AnthropicAiService.DefaultModel.Should().Be("claude-opus-5");
    }

    [Fact]
    public async Task Router_UnconfiguredProviderStillReturnsAResult()
    {
        // The whole "degrade, don't gate" contract in one assertion: every entry point returns a
        // failed result rather than throwing, so the editor stays usable with no AI configured.
        var router = new AiProviderRouter { Active = AiProvider.Anthropic };

        (await router.GenerateSummaryAsync("Engineer", [], [])).Success.Should().BeFalse();
        (await router.SuggestSkillsAsync("Engineer", [])).Success.Should().BeFalse();
        (await router.ImproveAchievementAsync("Did a thing")).Success.Should().BeFalse();
        (await router.OptimizeForJobAsync("content", "job")).Success.Should().BeFalse();
        (await router.GetImprovementSuggestionsAsync("resume")).Success.Should().BeFalse();
    }
}
