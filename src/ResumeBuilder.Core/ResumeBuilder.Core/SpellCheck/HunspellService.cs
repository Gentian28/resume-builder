using System.Text.RegularExpressions;
using WeCantSpell.Hunspell;

namespace ResumeBuilder.Core.SpellCheck;

public class HunspellService : ISpellChecker, IDisposable
{
    private WordList? _dictionary;
    private HashSet<string> _personalDictionary = new(StringComparer.OrdinalIgnoreCase);
    private string _dictionaryPath;
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

    public async Task InitializeAsync(string language = "en_US")
    {
        if (_disposed) throw new ObjectDisposedException(nameof(HunspellService));

        try
        {
            Directory.CreateDirectory(_dictionaryPath);

            var dicPath = Path.Combine(_dictionaryPath, $"{language}.dic");
            var affPath = Path.Combine(_dictionaryPath, $"{language}.aff");

            // Download dictionaries if not present
            if (!File.Exists(dicPath) || !File.Exists(affPath))
            {
                await DownloadDictionaryAsync(language);
            }

            if (File.Exists(dicPath) && File.Exists(affPath))
            {
                _dictionary = WordList.CreateFromFiles(dicPath, affPath);
                CurrentLanguage = language;
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
        // Dictionary URLs from LibreOffice dictionaries
        var baseUrl = language switch
        {
            "en_US" => "https://raw.githubusercontent.com/LibreOffice/dictionaries/master/en/en_US",
            "en_GB" => "https://raw.githubusercontent.com/LibreOffice/dictionaries/master/en/en_GB",
            "de_DE" => "https://raw.githubusercontent.com/LibreOffice/dictionaries/master/de/de_DE_frami",
            "fr_FR" => "https://raw.githubusercontent.com/LibreOffice/dictionaries/master/fr_FR/fr",
            "es_ES" => "https://raw.githubusercontent.com/LibreOffice/dictionaries/master/es/es_ES",
            _ => null
        };

        if (baseUrl == null)
        {
            // Fall back to en_US
            baseUrl = "https://raw.githubusercontent.com/LibreOffice/dictionaries/master/en/en_US";
            language = "en_US";
        }

        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(30);

        try
        {
            // Download .dic file
            var dicContent = await httpClient.GetByteArrayAsync($"{baseUrl}.dic");
            await File.WriteAllBytesAsync(Path.Combine(_dictionaryPath, $"{language}.dic"), dicContent);

            // Download .aff file
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
        if (_personalDictionary.Contains(word))
            return true;

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
            var word = match.Value;

            // Skip words with apostrophes at weird positions
            word = word.Trim('\'');

            if (!Check(word))
            {
                result.MisspelledWords.Add(new MisspelledWord
                {
                    Word = word,
                    StartIndex = match.Index,
                    Length = match.Length,
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

        _personalDictionary.Add(word);
        SavePersonalDictionary();
    }

    private void LoadPersonalDictionary()
    {
        var personalDicPath = Path.Combine(_dictionaryPath, "personal.dic");
        if (File.Exists(personalDicPath))
        {
            var words = File.ReadAllLines(personalDicPath);
            _personalDictionary = new HashSet<string>(words, StringComparer.OrdinalIgnoreCase);
        }
    }

    private void SavePersonalDictionary()
    {
        try
        {
            var personalDicPath = Path.Combine(_dictionaryPath, "personal.dic");
            File.WriteAllLines(personalDicPath, _personalDictionary);
        }
        catch
        {
            // Ignore save errors
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _dictionary = null;
    }
}
