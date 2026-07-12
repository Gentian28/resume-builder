using System.Text.Json;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Export.Importers;

/// <summary>
/// Reads back the native format written by <see cref="JsonExporter"/>.
/// </summary>
public class NativeJsonImporter : IImporter
{
    public string Format => "Native JSON";
    public string[] SupportedExtensions => new[] { ".json" };

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public async Task<ImportResult<Resume>> ImportAsync(Stream stream)
    {
        try
        {
            var resume = await JsonSerializer.DeserializeAsync<Resume>(stream, Options);

            if (resume == null)
            {
                return ImportResult<Resume>.Failed("Failed to parse JSON file. The file may be empty or malformed.");
            }

            var warnings = new List<string>();

            if (string.IsNullOrWhiteSpace(resume.PersonalInfo.FullName))
            {
                warnings.Add("Missing name in personal information.");
            }

            // An older export can predate a section, so repair the order rather than dropping it.
            resume.SectionOrder ??= SectionOrder.Default;
            resume.SectionOrder.EnsureAllSectionsPresent();

            // A re-imported resume is a new local row but keeps its cross-machine identity.
            resume.Id = 0;

            return ImportResult<Resume>.Succeeded(resume, warnings);
        }
        catch (JsonException ex)
        {
            return ImportResult<Resume>.Failed($"Invalid JSON format: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ImportResult<Resume>.Failed($"Error importing file: {ex.Message}");
        }
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
}
