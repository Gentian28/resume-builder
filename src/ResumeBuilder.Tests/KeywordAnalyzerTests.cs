using FluentAssertions;
using ResumeBuilder.Core.SmartContent;

namespace ResumeBuilder.Tests;

public class KeywordAnalyzerTests
{
    private readonly KeywordAnalyzer _analyzer = new();

    [Fact]
    public void Analyze_WithMatchingKeywords_ReturnsPositiveScore()
    {
        // Arrange
        var resume = @"
            Software Engineer with 5 years of experience in Python and JavaScript.
            Skilled in React, Node.js, and cloud computing with AWS.
            Strong problem-solving and communication skills.
        ";

        var jobDescription = @"
            We are looking for a Software Engineer with experience in Python and JavaScript.
            Must have knowledge of React and AWS cloud services.
            Excellent communication skills required.
        ";

        // Act
        var result = _analyzer.Analyze(resume, jobDescription);

        // Assert
        result.MatchScore.Should().BeGreaterThan(0);
        result.MatchedKeywords.Should().NotBeEmpty();
        result.MatchedKeywords.Should().Contain(k => k.Keyword.Contains("python", StringComparison.OrdinalIgnoreCase));
        result.MatchedKeywords.Should().Contain(k => k.Keyword.Contains("javascript", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Analyze_WithNoMatchingKeywords_ReturnsLowScore()
    {
        // Arrange
        var resume = "Experienced chef with culinary arts background. Skilled in French cuisine.";
        var jobDescription = "Looking for a software developer with Python and machine learning experience.";

        // Act
        var result = _analyzer.Analyze(resume, jobDescription);

        // Assert
        result.MatchScore.Should().BeLessThan(50);
        result.MissingKeywords.Should().NotBeEmpty();
    }

    [Fact]
    public void Analyze_WithEmptyResume_ReturnsZeroScore()
    {
        // Arrange
        var resume = "";
        var jobDescription = "Software Engineer with Python experience required.";

        // Act
        var result = _analyzer.Analyze(resume, jobDescription);

        // Assert
        result.MatchScore.Should().Be(0);
    }

    [Fact]
    public void Analyze_WithEmptyJobDescription_ReturnsZeroScore()
    {
        // Arrange
        var resume = "Experienced software developer with Python skills.";
        var jobDescription = "";

        // Act
        var result = _analyzer.Analyze(resume, jobDescription);

        // Assert
        result.MatchScore.Should().Be(0);
    }

    [Fact]
    public void Analyze_IdentifiesMissingKeywords()
    {
        // Arrange
        var resume = "Software developer with Java experience.";
        var jobDescription = @"
            Looking for a developer with Python, Python, Python experience.
            Must know Docker and Kubernetes.
            Docker experience is essential.
        ";

        // Act
        var result = _analyzer.Analyze(resume, jobDescription);

        // Assert
        result.MissingKeywords.Should().NotBeEmpty();
        // Python appears 3 times, so it should be flagged as missing
        result.MissingKeywords.Should().Contain(k => k.Contains("python", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Analyze_DetectsTechnicalSkills()
    {
        // Arrange
        var resume = @"
            Full stack developer with React, Node.js, and PostgreSQL experience.
            Deployed applications on AWS using Docker and Kubernetes.
        ";

        var jobDescription = @"
            We need a full stack developer familiar with React and Node.js.
            Experience with AWS, Docker, and database systems required.
        ";

        // Act
        var result = _analyzer.Analyze(resume, jobDescription);

        // Assert
        result.MatchedKeywords.Should().Contain(k => k.Category == "Web Technologies" || k.Category == "Cloud & DevOps" || k.Category == "Databases");
    }

    [Fact]
    public void Analyze_GeneratesSuggestions_WhenMissingKeywords()
    {
        // Arrange
        var resume = "Junior developer.";
        var jobDescription = @"
            Senior software engineer with Python, Java, and C# required.
            Must have AWS, Docker, and Kubernetes experience.
            Machine learning knowledge is a plus.
        ";

        // Act
        var result = _analyzer.Analyze(resume, jobDescription);

        // Assert
        result.Suggestions.Should().NotBeEmpty();
    }

    [Fact]
    public void Analyze_GeneratesWarnings_ForShortResume()
    {
        // Arrange
        var resume = "Developer.";
        var jobDescription = "Software Engineer needed.";

        // Act
        var result = _analyzer.Analyze(resume, jobDescription);

        // Assert
        result.Warnings.Should().Contain(w => w.Contains("short", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Analyze_GeneratesWarnings_ForMissingEmail()
    {
        // Arrange
        var resume = "John Doe, Software Developer with Python experience. Phone: 555-1234";
        var jobDescription = "Software Developer needed.";

        // Act
        var result = _analyzer.Analyze(resume, jobDescription);

        // Assert
        result.Warnings.Should().Contain(w => w.Contains("email", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Analyze_NoEmailWarning_WhenEmailPresent()
    {
        // Arrange
        var resume = "John Doe, john.doe@email.com, Software Developer with Python experience.";
        var jobDescription = "Software Developer needed.";

        // Act
        var result = _analyzer.Analyze(resume, jobDescription);

        // Assert
        result.Warnings.Should().NotContain(w => w.Contains("email", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Analyze_ScoreNeverExceeds100()
    {
        // Arrange - resume contains all keywords from job description
        var jobDescription = "Python developer";
        var resume = "Python Python Python Python Python developer developer developer";

        // Act
        var result = _analyzer.Analyze(resume, jobDescription);

        // Assert
        result.MatchScore.Should().BeLessThanOrEqualTo(100);
    }
}
