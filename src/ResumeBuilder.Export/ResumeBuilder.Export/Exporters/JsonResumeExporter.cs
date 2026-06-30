using System.Text;
using System.Text.Json;
using ResumeBuilder.Core.Models;
using ResumeBuilder.Export.Mappers;

namespace ResumeBuilder.Export.Exporters;

public class JsonResumeExporter : IExporter
{
    public string Format => "JSONRESUME";
    public string FileExtension => ".json";
    public string MimeType => "application/json";

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<byte[]> ExportAsync(Resume resume, string templateId)
    {
        var jsonResume = JsonResumeMapper.ToJsonResume(resume);
        var json = JsonSerializer.Serialize(jsonResume, Options);
        return Encoding.UTF8.GetBytes(json);
    }

    public async Task ExportToFileAsync(Resume resume, string templateId, string filePath)
    {
        var bytes = await ExportAsync(resume, templateId);
        await File.WriteAllBytesAsync(filePath, bytes);
    }
}
