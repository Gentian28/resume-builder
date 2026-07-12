using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using ResumeBuilder.Core.Models;
using ResumeBuilder.Templates;

namespace ResumeBuilder.Export;

public class CoverLetterPdfExporter : ICoverLetterExporter
{
    private readonly TemplateRegistry _templateRegistry;

    public string Format => "PDF";
    public string FileExtension => ".pdf";
    public string MimeType => "application/pdf";

    public CoverLetterPdfExporter(TemplateRegistry templateRegistry)
    {
        _templateRegistry = templateRegistry;

        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> ExportAsync(CoverLetter letter, string templateId)
    {
        var template = _templateRegistry.GetCoverLetterTemplateOrDefault(templateId);
        var document = template.CreateDocument(letter);

        return Task.FromResult(document.GeneratePdf());
    }

    public async Task ExportToFileAsync(CoverLetter letter, string templateId, string filePath)
    {
        var bytes = await ExportAsync(letter, templateId);
        await File.WriteAllBytesAsync(filePath, bytes);
    }
}
