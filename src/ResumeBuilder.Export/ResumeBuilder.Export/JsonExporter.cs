using System.Text.Json;
using System.Text.Json.Serialization;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Export;

public class JsonExporter : IExporter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string Format => "JSON";
    public string FileExtension => ".json";
    public string MimeType => "application/json";

    public Task<byte[]> ExportAsync(Resume resume, string templateId)
    {
        var json = JsonSerializer.Serialize(resume, Options);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        return Task.FromResult(bytes);
    }

    public async Task ExportToFileAsync(Resume resume, string templateId, string filePath)
    {
        var bytes = await ExportAsync(resume, templateId);
        await File.WriteAllBytesAsync(filePath, bytes);
    }

    public static Resume? Import(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Resume>(json, Options);
        }
        catch
        {
            return null;
        }
    }

    public static Resume? ImportFromFile(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            return Import(json);
        }
        catch
        {
            return null;
        }
    }

    public static async Task<Resume?> ImportFromFileAsync(string filePath)
    {
        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            return Import(json);
        }
        catch
        {
            return null;
        }
    }
}
