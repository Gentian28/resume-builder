using AwesomeAssertions;
using ResumeBuilder.Core.SpellCheck;

namespace ResumeBuilder.Tests;

/// <summary>
/// The stock Hunspell dictionaries don't know industry vocabulary, so without the built-in list
/// the checker flags ordinary résumé words and suggests its nearest dictionary neighbour
/// (Kubernetes → "Rubbernecks"). These pin the words users actually hit.
/// </summary>
public class BuiltInVocabularyTests
{
    [Theory]
    [InlineData("Kubernetes")]
    [InlineData("SaaS")]
    [InlineData("microservice")]
    [InlineData("agentic")]
    [InlineData("architecting")]
    [InlineData("DevOps")]
    [InlineData("APIs")]
    public void Contains_KnowsCommonTechVocabulary(string word)
    {
        BuiltInVocabulary.Contains(word).Should().BeTrue();
    }

    [Theory]
    [InlineData("kubernetes")]
    [InlineData("saas")]
    [InlineData("KUBERNETES")]
    public void Contains_IsCaseInsensitive(string word)
    {
        BuiltInVocabulary.Contains(word).Should().BeTrue();
    }

    [Theory]
    [InlineData("Kubernetse")]
    [InlineData("recieve")]
    public void Contains_DoesNotSwallowActualTypos(string word)
    {
        BuiltInVocabulary.Contains(word).Should().BeFalse();
    }
}
