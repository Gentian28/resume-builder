using System.Text.Json;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Export.Importers;

/// <summary>
/// Owns the ".json" extension, which the native and JSON Resume formats both use, and dispatches on
/// the file's content. Without this, a file written by <see cref="JsonExporter"/> could not be
/// re-imported because extension lookup would hand it to whichever importer happened to be found first.
/// </summary>
public class JsonImporter : IImporter
{
    private readonly JsonResumeImporter _jsonResume = new();
    private readonly NativeJsonImporter _native = new();

    public string Format => "JSON";
    public string[] SupportedExtensions => new[] { ".json" };
    public bool IsDefaultForExtension => true;

    public async Task<ImportResult<Resume>> ImportAsync(Stream stream)
    {
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);
        buffer.Position = 0;

        if (buffer.Length == 0)
        {
            return ImportResult<Resume>.Failed("Failed to parse JSON file. The file may be empty or malformed.");
        }

        bool isJsonResume;
        try
        {
            using var document = JsonDocument.Parse(buffer);
            isJsonResume = IsJsonResume(document.RootElement);
        }
        catch (JsonException ex)
        {
            return ImportResult<Resume>.Failed($"Invalid JSON format: {ex.Message}");
        }

        buffer.Position = 0;

        return isJsonResume
            ? await _jsonResume.ImportAsync(buffer)
            : await _native.ImportAsync(buffer);
    }

    public async Task<ImportResult<Resume>> ImportFromFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return ImportResult<Resume>.Failed($"File not found: {filePath}");
        }

        await using var stream = File.OpenRead(filePath);
        return await ImportAsync(stream);
    }

    private static bool IsJsonResume(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
            return false;

        // The native format is identified by its own root property; anything else is treated as
        // JSON Resume, which stays the default for third-party files.
        if (root.TryGetProperty("personalInfo", out _) || root.TryGetProperty("PersonalInfo", out _))
            return false;

        if (root.TryGetProperty("basics", out _))
            return true;

        if (root.TryGetProperty("$schema", out var schema) &&
            schema.ValueKind == JsonValueKind.String &&
            (schema.GetString() ?? "").Contains("resume", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return true;
    }
}
