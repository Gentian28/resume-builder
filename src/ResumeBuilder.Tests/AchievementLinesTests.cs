using AwesomeAssertions;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Tests;

/// <summary>
/// A tailored edit addresses an achievement by its index in the model, and the editor writes it back
/// by counting lines in a text box. If those two disagree by even one, an accepted AI rewrite lands
/// on the wrong bullet — so the round-trip is pinned here.
/// </summary>
public class AchievementLinesTests
{
    [Fact]
    public void Parse_DropsBlankLinesAndTrims()
    {
        var parsed = AchievementLines.Parse("  First  \n\n Second\n   \nThird\n");

        parsed.Should().Equal("First", "Second", "Third");
    }

    [Theory]
    [InlineData("a\nb")]
    [InlineData("a\r\nb")]
    [InlineData("a\rb")]
    public void Parse_HandlesEveryLineEnding(string text)
    {
        AchievementLines.Parse(text).Should().Equal("a", "b");
    }

    [Fact]
    public void Parse_EmptyText_ReturnsNoAchievements()
    {
        AchievementLines.Parse(null).Should().BeEmpty();
        AchievementLines.Parse("").Should().BeEmpty();
        AchievementLines.Parse("   \n  ").Should().BeEmpty();
    }

    [Fact]
    public void ReplaceAt_TargetsTheSameEntryParseWouldProduce()
    {
        // Blank lines the user left in the box must not shift the index.
        const string text = "First\n\nSecond\n\n\nThird";

        for (var index = 0; index < 3; index++)
        {
            var updated = AchievementLines.ReplaceAt(text, index, "REWRITTEN");
            var parsed = AchievementLines.Parse(updated);

            parsed[index].Should().Be("REWRITTEN", "the index used to write must address the entry that index reads");
            parsed.Should().HaveCount(3);
        }
    }

    [Fact]
    public void ReplaceAt_PreservesTheBlankLinesTheUserTyped()
    {
        var updated = AchievementLines.ReplaceAt("First\n\nSecond", 1, "Changed");

        updated.Should().Be("First\n\nChanged");
    }

    [Fact]
    public void ReplaceAt_IndexOutOfRange_LeavesTextAlone()
    {
        AchievementLines.ReplaceAt("First\nSecond", 5, "x").Should().Be("First\nSecond");
        AchievementLines.ReplaceAt("First\nSecond", -1, "x").Should().Be("First\nSecond");
    }

    [Fact]
    public void FormatThenParse_RoundTrips()
    {
        var achievements = new List<string> { "Cut latency in half", "Mentored three engineers" };

        AchievementLines.Parse(AchievementLines.Format(achievements)).Should().Equal(achievements);
    }
}
