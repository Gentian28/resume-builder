using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using ResumeBuilder.Core.Models;
using ResumeBuilder.Templates;

namespace ResumeBuilder.Export;

public class PdfExporter : IExporter
{
    private readonly TemplateRegistry _templateRegistry;

    public string Format => "PDF";
    public string FileExtension => ".pdf";
    public string MimeType => "application/pdf";

    public PdfExporter(TemplateRegistry templateRegistry)
    {
        _templateRegistry = templateRegistry;

        // Configure QuestPDF license
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> ExportAsync(Resume resume, string templateId)
    {
        var template = _templateRegistry.GetTemplateOrDefault(templateId);
        var document = template.CreateDocument(resume);

        var bytes = document.GeneratePdf();
        return Task.FromResult(bytes);
    }

    public async Task ExportToFileAsync(Resume resume, string templateId, string filePath)
    {
        var bytes = await ExportAsync(resume, templateId);
        await File.WriteAllBytesAsync(filePath, bytes);
    }
}
