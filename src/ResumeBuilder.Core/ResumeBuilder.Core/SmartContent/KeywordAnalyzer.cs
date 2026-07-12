using System.Text.RegularExpressions;

namespace ResumeBuilder.Core.SmartContent;

/// <summary>
/// Result of keyword analysis comparing a resume against a job description.
/// </summary>
public class KeywordAnalysisResult
{
    /// <summary>
    /// Overall match score (0-100).
    /// </summary>
    public int MatchScore { get; init; }

    /// <summary>
    /// Keywords found in both resume and job description.
    /// </summary>
    public List<KeywordMatch> MatchedKeywords { get; init; } = new();

    /// <summary>
    /// Important keywords from job description missing in resume.
    /// </summary>
    public List<string> MissingKeywords { get; init; } = new();

    /// <summary>
    /// Suggestions for improving the resume.
    /// </summary>
    public List<string> Suggestions { get; init; } = new();

    /// <summary>
    /// ATS compatibility warnings.
    /// </summary>
    public List<string> Warnings { get; init; } = new();
}

/// <summary>
/// Represents a matched keyword with context.
/// </summary>
public class KeywordMatch
{
    public required string Keyword { get; init; }
    public int JobDescriptionCount { get; init; }
    public int ResumeCount { get; init; }
    public string Category { get; init; } = "General";
}

/// <summary>
/// Analyzes resume content against job descriptions for keyword optimization.
/// </summary>
public class KeywordAnalyzer
{
    // Common words to exclude from analysis
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with",
        "by", "from", "as", "is", "was", "are", "were", "been", "be", "have", "has", "had",
        "do", "does", "did", "will", "would", "could", "should", "may", "might", "must",
        "shall", "can", "need", "dare", "ought", "used", "this", "that", "these", "those",
        "i", "you", "he", "she", "it", "we", "they", "what", "which", "who", "whom",
        "our", "your", "their", "its", "my", "his", "her", "all", "each", "every", "both",
        "few", "more", "most", "other", "some", "such", "no", "nor", "not", "only", "own",
        "same", "so", "than", "too", "very", "just", "also", "now", "here", "there",
        "when", "where", "why", "how", "any", "if", "because", "while", "about", "into",
        "through", "during", "before", "after", "above", "below", "between", "under",
        "work", "working", "experience", "job", "position", "role", "team", "company",
        "including", "within", "using", "provide", "ensure", "able", "well", "good",
        "new", "time", "year", "years", "day", "week", "month"
    };

    // Technical skill categories for better organization
    private static readonly Dictionary<string, string[]> SkillCategories = new()
    {
        ["Programming Languages"] = new[] { "python", "java", "javascript", "c#", "c++", "ruby", "go", "rust", "php", "swift", "kotlin", "typescript", "scala" },
        ["Web Technologies"] = new[] { "html", "css", "react", "angular", "vue", "node", "express", "django", "flask", "asp.net", "blazor", "nextjs", "gatsby" },
        ["Databases"] = new[] { "sql", "mysql", "postgresql", "mongodb", "redis", "oracle", "sqlite", "dynamodb", "cassandra", "elasticsearch" },
        ["Cloud & DevOps"] = new[] { "aws", "azure", "gcp", "docker", "kubernetes", "jenkins", "terraform", "ansible", "ci/cd", "devops" },
        ["Data & AI"] = new[] { "machine learning", "ai", "data science", "tensorflow", "pytorch", "pandas", "numpy", "spark", "hadoop", "tableau", "power bi" },
        ["Soft Skills"] = new[] { "leadership", "communication", "teamwork", "problem-solving", "analytical", "collaborative", "management", "agile", "scrum" }
    };

    /// <summary>
    /// Analyze resume content against a job description.
    /// </summary>
    public KeywordAnalysisResult Analyze(string resumeContent, string jobDescription)
    {
        var resumeWords = ExtractKeywords(resumeContent);
        var jobWords = ExtractKeywords(jobDescription);

        var matchedKeywords = new List<KeywordMatch>();
        var missingKeywords = new List<string>();

        // Find common important keywords
        foreach (var (keyword, jobCount) in jobWords)
        {
            if (resumeWords.TryGetValue(keyword, out var resumeCount))
            {
                matchedKeywords.Add(new KeywordMatch
                {
                    Keyword = keyword,
                    JobDescriptionCount = jobCount,
                    ResumeCount = resumeCount,
                    Category = GetKeywordCategory(keyword)
                });
            }
            else if (jobCount >= 2 || IsImportantKeyword(keyword))
            {
                missingKeywords.Add(keyword);
            }
        }

        // Calculate match score
        var totalJobKeywords = jobWords.Count;
        var matchedCount = matchedKeywords.Count;
        var matchScore = totalJobKeywords > 0
            ? (int)((matchedCount / (double)totalJobKeywords) * 100)
            : 0;

        // Generate suggestions
        var suggestions = GenerateSuggestions(matchedKeywords, missingKeywords);
        var warnings = GenerateWarnings(resumeContent);

        return new KeywordAnalysisResult
        {
            MatchScore = Math.Min(matchScore, 100),
            MatchedKeywords = matchedKeywords.OrderByDescending(k => k.JobDescriptionCount).Take(20).ToList(),
            MissingKeywords = missingKeywords.Take(15).ToList(),
            Suggestions = suggestions,
            Warnings = warnings
        };
    }

    private Dictionary<string, int> ExtractKeywords(string text)
    {
        var words = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // ToLowerInvariant, not ToLower: the lookups below are ordinal, and a Turkish locale would
        // lowercase "I" to a dotless "ı" that never matches them.
        var cleanText = Regex.Replace(text.ToLowerInvariant(), @"[^\w\s\-/+#.]", " ");
        var tokens = cleanText.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var token in tokens)
        {
            var word = token.Trim('.', ',', ';', ':');

            // Skip short words and stop words
            if (word.Length < 2 || StopWords.Contains(word))
                continue;

            // Skip pure numbers
            if (int.TryParse(word, out _))
                continue;

            if (words.ContainsKey(word))
                words[word]++;
            else
                words[word] = 1;
        }

        // Also extract multi-word phrases (bigrams for technical terms)
        for (int i = 0; i < tokens.Length - 1; i++)
        {
            var phrase = $"{tokens[i]} {tokens[i + 1]}".ToLowerInvariant();
            if (IsKnownPhrase(phrase))
            {
                if (words.ContainsKey(phrase))
                    words[phrase]++;
                else
                    words[phrase] = 1;
            }
        }

        return words;
    }

    private bool IsKnownPhrase(string phrase)
    {
        // Common multi-word technical phrases
        var knownPhrases = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "machine learning", "deep learning", "data science", "data analysis",
            "project management", "product management", "software development",
            "full stack", "front end", "back end", "mobile development",
            "cloud computing", "big data", "artificial intelligence",
            "user experience", "user interface", "quality assurance",
            "business intelligence", "data engineering", "devops engineer",
            "software engineer", "technical lead", "team lead"
        };

        return knownPhrases.Contains(phrase);
    }

    /// <summary>
    /// Skill terms indexed for exact lookup. Substring matching was used here before and produced
    /// nonsense: "email" contains "ai", "goal" contains "go", "scalable" contains "scala".
    /// </summary>
    private static readonly Dictionary<string, string> SkillToCategory = SkillCategories
        .SelectMany(pair => pair.Value.Select(skill => (Skill: skill, Category: pair.Key)))
        .ToDictionary(x => x.Skill, x => x.Category, StringComparer.OrdinalIgnoreCase);

    private string GetKeywordCategory(string keyword) =>
        SkillToCategory.TryGetValue(keyword, out var category) ? category : "General";

    private bool IsImportantKeyword(string keyword) => SkillToCategory.ContainsKey(keyword);

    private List<string> GenerateSuggestions(List<KeywordMatch> matched, List<string> missing)
    {
        var suggestions = new List<string>();

        if (missing.Count > 5)
        {
            suggestions.Add($"Consider adding {missing.Count} missing keywords from the job description to improve ATS matching.");
        }

        var topMissing = missing.Take(5).ToList();
        if (topMissing.Any())
        {
            suggestions.Add($"Key skills to add: {string.Join(", ", topMissing)}");
        }

        var lowCount = matched.Where(m => m.ResumeCount < m.JobDescriptionCount).Take(3).ToList();
        if (lowCount.Any())
        {
            var terms = string.Join(", ", lowCount.Select(k => k.Keyword));
            suggestions.Add($"Use these terms more frequently: {terms}");
        }

        return suggestions;
    }

    private List<string> GenerateWarnings(string resumeContent)
    {
        var warnings = new List<string>();

        // Check for common ATS issues
        if (resumeContent.Contains("●") || resumeContent.Contains("■") || resumeContent.Contains("►"))
        {
            warnings.Add("Special characters/bullets may not parse correctly in ATS systems.");
        }

        if (resumeContent.Length < 500)
        {
            warnings.Add("Resume content appears short. Consider adding more detail about achievements.");
        }

        // Check for contact info patterns
        if (!Regex.IsMatch(resumeContent, @"[\w\.-]+@[\w\.-]+\.\w+", RegexOptions.IgnoreCase))
        {
            warnings.Add("No email address detected. Ensure contact info is included.");
        }

        return warnings;
    }
}
