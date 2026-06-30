namespace ResumeBuilder.Core.SpellCheck;

public interface ISpellChecker
{
    bool IsInitialized { get; }
    string CurrentLanguage { get; }

    Task InitializeAsync(string language = "en_US");
    bool Check(string word);
    IEnumerable<string> Suggest(string word, int maxSuggestions = 5);
    SpellCheckResult CheckText(string text);
    void AddToPersonalDictionary(string word);
    void Dispose();
}

public class SpellCheckResult
{
    public string OriginalText { get; set; } = "";
    public List<MisspelledWord> MisspelledWords { get; set; } = new();
    public bool HasErrors => MisspelledWords.Any();
    public int ErrorCount => MisspelledWords.Count;
}

public class MisspelledWord
{
    public string Word { get; set; } = "";
    public int StartIndex { get; set; }
    public int Length { get; set; }
    public List<string> Suggestions { get; set; } = new();
}
