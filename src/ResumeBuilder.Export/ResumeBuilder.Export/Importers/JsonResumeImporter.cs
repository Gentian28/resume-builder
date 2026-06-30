using System.Text.Json;
using ResumeBuilder.Core.Models;
using ResumeBuilder.Export.Mappers;
using ResumeBuilder.Export.Models;

namespace ResumeBuilder.Export.Importers;

public class JsonResumeImporter : IImporter
{
    public string Format => "JSON Resume";
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
            var jsonResume = await JsonSerializer.DeserializeAsync<JsonResumeSchema>(stream, Options);

            if (jsonResume == null)
            {
                return ImportResult<Resume>.Failed("Failed to parse JSON file. The file may be empty or malformed.");
            }

            var warnings = new List<string>();

            // Validate basic structure
            if (jsonResume.Basics == null)
            {
                warnings.Add("Missing 'basics' section. Personal information will be empty.");
            }

            if (string.IsNullOrWhiteSpace(jsonResume.Basics?.Name))
            {
                warnings.Add("Missing name in basics section.");
            }

            var resume = JsonResumeMapper.FromJsonResume(jsonResume);

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

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (!SupportedExtensions.Contains(extension))
        {
            return ImportResult<Resume>.Failed($"Unsupported file extension: {extension}. Expected: {string.Join(", ", SupportedExtensions)}");
        }

        await using var stream = File.OpenRead(filePath);
        return await ImportAsync(stream);
    }
}
