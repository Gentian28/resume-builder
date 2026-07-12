using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Core.SmartContent;

/// <summary>A proposed rewrite the user can accept or reject, scoped to one field.</summary>
public class TailoredEdit
{
    public required TailoredEditTarget Target { get; init; }

    /// <summary>Index into the target collection (experience index), or -1 for the summary.</summary>
    public int ItemIndex { get; init; } = -1;

    /// <summary>Index of the achievement within the experience, or -1 for the whole item.</summary>
    public int SubIndex { get; init; } = -1;

    public required string Original { get; init; }
    public required string Proposed { get; init; }

    /// <summary>Why the rewrite was proposed - shown next to the diff.</summary>
    public string Rationale { get; init; } = string.Empty;

    public bool Accepted { get; set; }
}

public enum TailoredEditTarget
{
    Summary,
    ExperienceDescription,
    ExperienceAchievement
}

/// <summary>The result of tailoring a resume to one job description.</summary>
public class TailoringResult
{
    public required KeywordAnalysisResult Analysis { get; init; }
    public List<TailoredEdit> Edits { get; init; } = new();
    public List<string> SuggestedSkills { get; init; } = new();

    /// <summary>Populated when AI was unavailable; the keyword analysis is still valid.</summary>
    public string? AiError { get; init; }

    public bool HasAiEdits => Edits.Count > 0;
}

/// <summary>
/// Tailors a resume to a specific job posting. The keyword analyzer and the AI service already
/// existed but were never connected to each other: this runs the analysis, feeds the gaps into the
/// rewrite prompts, and returns edits the user reviews before anything is applied.
/// </summary>
public class JobTailoringService
{
    private readonly KeywordAnalyzer _analyzer;
    private readonly IAiService _ai;

    public JobTailoringService(IAiService ai, KeywordAnalyzer? analyzer = null)
    {
        _ai = ai;
        _analyzer = analyzer ?? new KeywordAnalyzer();
    }

    /// <summary>
    /// Analyzes the resume against the posting and, if AI is configured, proposes rewrites.
    /// Nothing is mutated - apply the accepted edits with <see cref="Apply"/>.
    /// </summary>
    public async Task<TailoringResult> TailorAsync(
        Resume resume,
        string jobDescription,
        CancellationToken cancellationToken = default)
    {
        var resumeText = ResumeTextExtractor.ToPlainText(resume);
        var analysis = _analyzer.Analyze(resumeText, jobDescription);

        if (!_ai.IsConfigured)
        {
            // Without AI there are no rewrites, but the gaps the analysis found are still the
            // most useful thing we can hand back.
            return new TailoringResult
            {
                Analysis = analysis,
                SuggestedSkills = analysis.MissingKeywords.Take(15).ToList(),
                AiError = "AI is not configured, so only the keyword analysis is available."
            };
        }

        var edits = new List<TailoredEdit>();
        var errors = new List<string>();

        if (!string.IsNullOrWhiteSpace(resume.Summary))
        {
            var rewritten = await _ai.OptimizeForJobAsync(resume.Summary, jobDescription, cancellationToken);
            if (rewritten.Success && !string.IsNullOrWhiteSpace(rewritten.Data))
            {
                AddIfChanged(edits, new TailoredEdit
                {
                    Target = TailoredEditTarget.Summary,
                    Original = resume.Summary,
                    Proposed = rewritten.Data!.Trim(),
                    Rationale = "Aligned the summary with the posting's language."
                });
            }
            else if (rewritten.ErrorMessage != null)
            {
                errors.Add(rewritten.ErrorMessage);
            }
        }

        // Only the most recent roles are worth rewriting; older ones rarely move the needle and
        // each one is a round trip.
        var recentExperiences = resume.Experiences
            .OrderBy(e => e.Order)
            .Take(3)
            .ToList();

        foreach (var exp in recentExperiences)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var expIndex = resume.Experiences.IndexOf(exp);
            var context = $"{exp.JobTitle} at {exp.Company}";

            for (var i = 0; i < exp.Achievements.Count; i++)
            {
                var achievement = exp.Achievements[i];
                if (string.IsNullOrWhiteSpace(achievement))
                    continue;

                var improved = await _ai.ImproveAchievementAsync(achievement, context, cancellationToken);
                var best = improved.Success ? improved.Data?.FirstOrDefault() : null;

                if (!string.IsNullOrWhiteSpace(best))
                {
                    AddIfChanged(edits, new TailoredEdit
                    {
                        Target = TailoredEditTarget.ExperienceAchievement,
                        ItemIndex = expIndex,
                        SubIndex = i,
                        Original = achievement,
                        Proposed = best!.Trim(),
                        Rationale = $"Strengthened a bullet under {context}."
                    });
                }
                else if (improved.ErrorMessage != null)
                {
                    errors.Add(improved.ErrorMessage);
                }
            }
        }

        var skills = await _ai.SuggestSkillsAsync(
            resume.PersonalInfo.JobTitle,
            resume.Skills.Select(s => s.Name),
            resume.Experiences.Select(e => e.Description),
            cancellationToken);

        var suggestedSkills = skills.Success && skills.Data != null
            ? skills.Data.Concat(analysis.MissingKeywords).Distinct(StringComparer.OrdinalIgnoreCase).Take(15).ToList()
            : analysis.MissingKeywords.Take(15).ToList();

        return new TailoringResult
        {
            Analysis = analysis,
            Edits = edits,
            SuggestedSkills = suggestedSkills,
            AiError = errors.Count > 0 ? string.Join("; ", errors.Distinct()) : null
        };
    }

    private static void AddIfChanged(List<TailoredEdit> edits, TailoredEdit edit)
    {
        if (!string.Equals(edit.Original.Trim(), edit.Proposed.Trim(), StringComparison.Ordinal))
        {
            edits.Add(edit);
        }
    }

    /// <summary>Applies only the edits the user accepted. Returns the number applied.</summary>
    public static int Apply(Resume resume, IEnumerable<TailoredEdit> edits)
    {
        var applied = 0;

        foreach (var edit in edits.Where(e => e.Accepted))
        {
            switch (edit.Target)
            {
                case TailoredEditTarget.Summary:
                    resume.Summary = edit.Proposed;
                    applied++;
                    break;

                case TailoredEditTarget.ExperienceDescription
                    when IsInRange(resume.Experiences, edit.ItemIndex):
                    resume.Experiences[edit.ItemIndex].Description = edit.Proposed;
                    applied++;
                    break;

                case TailoredEditTarget.ExperienceAchievement
                    when IsInRange(resume.Experiences, edit.ItemIndex) &&
                         IsInRange(resume.Experiences[edit.ItemIndex].Achievements, edit.SubIndex):
                    resume.Experiences[edit.ItemIndex].Achievements[edit.SubIndex] = edit.Proposed;
                    applied++;
                    break;
            }
        }

        return applied;
    }

    private static bool IsInRange<T>(IList<T> list, int index) => index >= 0 && index < list.Count;
}
