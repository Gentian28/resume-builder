using System.Text.RegularExpressions;
using WeCantSpell.Hunspell;

namespace ResumeBuilder.Core.SpellCheck;

public class HunspellService : ISpellChecker, IDisposable
{
    private readonly HashSet<string> _personalDictionary = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _personalDictionaryLock = new();
    private readonly string _dictionaryPath;
    private WordList? _dictionary;
    private bool _disposed;

    public bool IsInitialized => _dictionary != null;
    public string CurrentLanguage { get; private set; } = "";

    public HunspellService()
    {
        _dictionaryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ResumeBuilder",
            "Dictionaries");
    }

    private const string FallbackLanguage = "en_US";

    /// <summary>Languages with a known LibreOffice dictionary. Anything else falls back to en_US.</summary>
    public static IReadOnlyDictionary<string, string> SupportedLanguages { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["en_US"] = "https://raw.githubusercontent.com/LibreOffice/dictionaries/master/en/en_US",
            ["en_GB"] = "https://raw.githubusercontent.com/LibreOffice/dictionaries/master/en/en_GB",
            ["de_DE"] = "https://raw.githubusercontent.com/LibreOffice/dictionaries/master/de/de_DE_frami",
            ["fr_FR"] = "https://raw.githubusercontent.com/LibreOffice/dictionaries/master/fr_FR/fr",
            ["es_ES"] = "https://raw.githubusercontent.com/LibreOffice/dictionaries/master/es/es_ES",
            ["it_IT"] = "https://raw.githubusercontent.com/LibreOffice/dictionaries/master/it_IT/it_IT",
            ["pt_PT"] = "https://raw.githubusercontent.com/LibreOffice/dictionaries/master/pt_PT/pt_PT",
            ["nl_NL"] = "https://raw.githubusercontent.com/LibreOffice/dictionaries/master/nl_NL/nl_NL"
        };

    public async Task InitializeAsync(string language = FallbackLanguage)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(HunspellService));

        try
        {
            Directory.CreateDirectory(_dictionaryPath);

            // Resolve the fallback up front. Previously the download silently fell back to en_US
            // filenames while this method kept looking for the requested language's files, so an
            // unsupported language downloaded a dictionary and then never loaded it.
            var resolved = SupportedLanguages.ContainsKey(language) ? language : FallbackLanguage;

            var dicPath = Path.Combine(_dictionaryPath, $"{resolved}.dic");
            var affPath = Path.Combine(_dictionaryPath, $"{resolved}.aff");

            if (!File.Exists(dicPath) || !File.Exists(affPath))
            {
                await DownloadDictionaryAsync(resolved);
            }

            if (File.Exists(dicPath) && File.Exists(affPath))
            {
                _dictionary = WordList.CreateFromFiles(dicPath, affPath);
                CurrentLanguage = resolved;
                LoadPersonalDictionary();
            }
        }
        catch (Exception ex)
        {
            // Log error but don't crash - spell check is optional
            Console.WriteLine($"Failed to initialize spell checker: {ex.Message}");
        }
    }

    private async Task DownloadDictionaryAsync(string language)
    {
        var baseUrl = SupportedLanguages.TryGetValue(language, out var url)
            ? url
            : SupportedLanguages[FallbackLanguage];

        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(30);

        try
        {
            var dicContent = await httpClient.GetByteArrayAsync($"{baseUrl}.dic");
            await File.WriteAllBytesAsync(Path.Combine(_dictionaryPath, $"{language}.dic"), dicContent);

            var affContent = await httpClient.GetByteArrayAsync($"{baseUrl}.aff");
            await File.WriteAllBytesAsync(Path.Combine(_dictionaryPath, $"{language}.aff"), affContent);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to download dictionary: {ex.Message}");
            throw;
        }
    }

    public bool Check(string word)
    {
        if (!IsInitialized || string.IsNullOrWhiteSpace(word))
            return true;

        // Skip if in personal dictionary
        lock (_personalDictionaryLock)
        {
            if (_personalDictionary.Contains(word))
                return true;
        }

        // Skip numbers, emails, URLs
        if (IsSpecialWord(word))
            return true;

        return _dictionary!.Check(word);
    }

    private static bool IsSpecialWord(string word)
    {
        // Skip numbers
        if (Regex.IsMatch(word, @"^\d+$"))
            return true;

        // Skip words with numbers (like "C#", "3D")
        if (Regex.IsMatch(word, @"\d"))
            return true;

        // Skip email-like patterns
        if (word.Contains('@'))
            return true;

        // Skip URL-like patterns
        if (word.Contains("://") || word.StartsWith("www."))
            return true;

        // Skip very short words
        if (word.Length < 2)
            return true;

        // Skip words that are all caps (acronyms)
        if (word.All(char.IsUpper) && word.Length <= 5)
            return true;

        return false;
    }

    public IEnumerable<string> Suggest(string word, int maxSuggestions = 5)
    {
        if (!IsInitialized || string.IsNullOrWhiteSpace(word))
            return Enumerable.Empty<string>();

        return _dictionary!.Suggest(word).Take(maxSuggestions);
    }

    public SpellCheckResult CheckText(string text)
    {
        var result = new SpellCheckResult { OriginalText = text };

        if (!IsInitialized || string.IsNullOrWhiteSpace(text))
            return result;

        // Split text into words, preserving positions
        var wordPattern = new Regex(@"\b[\w']+\b");
        var matches = wordPattern.Matches(text);

        foreach (Match match in matches)
        {
            // Trimming apostrophes shifts the word, so the reported offset has to shift with it —
            // otherwise the caller underlines (or replaces) the wrong span of text.
            var word = match.Value;
            var leadingQuotes = word.Length - word.TrimStart('\'').Length;
            word = word.Trim('\'');

            if (word.Length == 0)
                continue;

            if (!Check(word))
            {
                result.MisspelledWords.Add(new MisspelledWord
                {
                    Word = word,
                    StartIndex = match.Index + leadingQuotes,
                    Length = word.Length,
                    Suggestions = Suggest(word).ToList()
                });
            }
        }

        return result;
    }

    public void AddToPersonalDictionary(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
            return;

        lock (_personalDictionaryLock)
        {
            // Merge with whatever is on disk first, so a second instance's additions aren't lost.
            foreach (var existing in ReadPersonalDictionary())
            {
                _personalDictionary.Add(existing);
            }

            _personalDictionary.Add(word.Trim());
            SavePersonalDictionary();
        }
    }

    private void LoadPersonalDictionary()
    {
        lock (_personalDictionaryLock)
        {
            foreach (var word in ReadPersonalDictionary())
            {
                _personalDictionary.Add(word);
            }
        }
    }

    private IEnumerable<string> ReadPersonalDictionary()
    {
        var personalDicPath = Path.Combine(_dictionaryPath, "personal.dic");
        if (!File.Exists(personalDicPath))
            return Enumerable.Empty<string>();

        try
        {
            return File.ReadAllLines(personalDicPath).Where(w => !string.IsNullOrWhiteSpace(w));
        }
        catch (IOException)
        {
            return Enumerable.Empty<string>();
        }
    }

    private void SavePersonalDictionary()
    {
        try
        {
            Directory.CreateDirectory(_dictionaryPath);
            var personalDicPath = Path.Combine(_dictionaryPath, "personal.dic");
            File.WriteAllLines(personalDicPath, _personalDictionary.OrderBy(w => w, StringComparer.OrdinalIgnoreCase));
        }
        catch (IOException)
        {
            // Ignore save errors - the personal dictionary is a convenience, not required state.
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _dictionary = null;
    }
}
