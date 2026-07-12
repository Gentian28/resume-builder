using FluentAssertions;
using ResumeBuilder.Core.Models;
using ResumeBuilder.Core.SmartContent;

namespace ResumeBuilder.Tests;

public class JobTailoringServiceTests
{
    [Fact]
    public async Task Tailor_WithoutAi_StillReturnsKeywordAnalysis()
    {
        var service = new JobTailoringService(new StubAiService { Configured = false });
        var resume = SampleResume();

        var result = await service.TailorAsync(resume, "We need a Python engineer with Kubernetes experience.");

        result.Analysis.Should().NotBeNull();
        result.HasAiEdits.Should().BeFalse();
        result.AiError.Should().Contain("not configured");
        result.SuggestedSkills.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Tailor_ProposesEditsButDoesNotMutateResume()
    {
        var service = new JobTailoringService(new StubAiService());
        var resume = SampleResume();
        var originalSummary = resume.Summary;

        var result = await service.TailorAsync(resume, "Backend role. C#, AWS.");

        result.Edits.Should().NotBeEmpty();
        resume.Summary.Should().Be(originalSummary, "tailoring proposes edits for review and applies nothing itself");
    }

    [Fact]
    public async Task Apply_OnlyAppliesAcceptedEdits()
    {
        var service = new JobTailoringService(new StubAiService());
        var resume = SampleResume();

        var result = await service.TailorAsync(resume, "Backend role.");
        foreach (var edit in result.Edits)
        {
            edit.Accepted = edit.Target == TailoredEditTarget.Summary;
        }

        var applied = JobTailoringService.Apply(resume, result.Edits);

        applied.Should().Be(1);
        resume.Summary.Should().StartWith("[optimized]");
        resume.Experiences[0].Achievements[0].Should().NotStartWith("[improved]");
    }

    private static Resume SampleResume() => new()
    {
        PersonalInfo = new PersonalInfo { FirstName = "Jane", LastName = "Doe", JobTitle = "Engineer" },
        Summary = "Engineer with backend experience.",
        Experiences =
        {
            new Experience
            {
                JobTitle = "Engineer",
                Company = "Acme",
                Description = "Built services.",
                Achievements = { "Shipped a thing" }
            }
        },
        Skills = { new Skill { Name = "C#" } }
    };
}

public class CoverLetterServiceTests
{
    [Fact]
    public async Task Draft_WithoutAi_StillProducesAUsableLetter()
    {
        var service = new CoverLetterService(new StubAiService { Configured = false });
        var resume = new Resume
        {
            PersonalInfo = new PersonalInfo { FirstName = "Jane", LastName = "Doe", JobTitle = "Engineer" },
            Summary = "Backend engineer with eight years of experience.",
            Experiences =
            {
                new Experience { JobTitle = "Engineer", Company = "Acme", Achievements = { "Cut latency in half" } }
            },
            Skills = { new Skill { Name = "C#" }, new Skill { Name = "AWS" } }
        };

        var result = await service.DraftAsync(resume, "Northwind", "Senior Engineer", "job text");

        result.Success.Should().BeTrue();
        var letter = result.Data!;
        letter.Paragraphs.Should().NotBeEmpty();
        letter.Body.Should().Contain("Northwind");
        letter.CompanyName.Should().Be("Northwind");
        letter.Subject.Should().Be("Application for Senior Engineer");
    }

    [Fact]
    public void FromResume_InheritsSenderAndStyling()
    {
        var resume = new Resume
        {
            Id = 5,
            PersonalInfo = new PersonalInfo { FirstName = "Jane", LastName = "Doe", Email = "jane@example.com" }
        };
        resume.TemplateSettings.AccentColor = "#123456";

        var letter = CoverLetter.FromResume(resume, "Northwind", "Engineer");

        letter.ResumeId.Should().Be(5);
        letter.PersonalInfo.Email.Should().Be("jane@example.com");
        letter.TemplateSettings.AccentColor.Should().Be("#123456");

        // The sender block is a copy, not a shared reference back into the resume.
        letter.PersonalInfo.FirstName = "Changed";
        resume.PersonalInfo.FirstName.Should().Be("Jane");
    }

    [Fact]
    public void EffectiveSalutation_FallsBackWhenNoRecipient()
    {
        new CoverLetter().EffectiveSalutation.Should().Be("Dear Hiring Manager,");
        new CoverLetter { RecipientName = "Ada Lovelace" }.EffectiveSalutation.Should().Be("Dear Ada Lovelace,");
        new CoverLetter { Salutation = "To whom it may concern," }.EffectiveSalutation.Should().Be("To whom it may concern,");
    }
}

internal class StubAiService : IAiService
{
    public bool Configured { get; init; } = true;
    public bool IsConfigured => Configured;

    public void Configure(string apiKey, string? model = null) { }

    public Task<AiResult<string>> GenerateSummaryAsync(string jobTitle, IEnumerable<string> experiences, IEnumerable<string> skills, CancellationToken cancellationToken = default)
        => Task.FromResult(AiResult<string>.Succeeded("[summary]"));

    public Task<AiResult<IEnumerable<string>>> ImproveAchievementAsync(string achievement, string? jobContext = null, CancellationToken cancellationToken = default)
        => Task.FromResult(AiResult<IEnumerable<string>>.Succeeded(new[] { $"[improved] {achievement}" }));

    public Task<AiResult<IEnumerable<string>>> SuggestSkillsAsync(string jobTitle, IEnumerable<string> currentSkills, IEnumerable<string>? experiences = null, CancellationToken cancellationToken = default)
        => Task.FromResult(AiResult<IEnumerable<string>>.Succeeded(new[] { "Kubernetes" }));

    public Task<AiResult<string>> OptimizeForJobAsync(string content, string jobDescription, CancellationToken cancellationToken = default)
        => Task.FromResult(AiResult<string>.Succeeded($"[optimized] {content}"));

    public Task<AiResult<IEnumerable<AiSuggestion>>> GetImprovementSuggestionsAsync(string resumeContent, CancellationToken cancellationToken = default)
        => Task.FromResult(AiResult<IEnumerable<AiSuggestion>>.Succeeded(Array.Empty<AiSuggestion>()));
}
