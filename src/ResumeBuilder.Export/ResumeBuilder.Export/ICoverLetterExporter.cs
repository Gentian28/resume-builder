using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Export;

public interface ICoverLetterExporter
{
    string Format { get; }
    string FileExtension { get; }
    string MimeType { get; }
    Task<byte[]> ExportAsync(CoverLetter letter, string templateId);
    Task ExportToFileAsync(CoverLetter letter, string templateId, string filePath);
}
