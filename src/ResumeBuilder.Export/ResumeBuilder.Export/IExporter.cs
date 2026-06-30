using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Export;

public interface IExporter
{
    string Format { get; }
    string FileExtension { get; }
    string MimeType { get; }
    Task<byte[]> ExportAsync(Resume resume, string templateId);
    Task ExportToFileAsync(Resume resume, string templateId, string filePath);
}
