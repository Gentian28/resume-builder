using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using ResumeBuilder.Core.Models;
using ResumeBuilder.Templates;
using SkiaSharp;

namespace ResumeBuilder.Export;

public class PngExporter : IExporter
{
    private readonly TemplateRegistry _templateRegistry;

    public string Format => "PNG";
    public string FileExtension => ".png";
    public string MimeType => "image/png";

    public PngExporter(TemplateRegistry templateRegistry)
    {
        _templateRegistry = templateRegistry;

        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> ExportAsync(Resume resume, string templateId)
    {
        var template = _templateRegistry.GetTemplateOrDefault(templateId);
        var document = template.CreateDocument(resume);

        // Generate images from the document (one per page)
        var images = document.GenerateImages(new ImageGenerationSettings
        {
            ImageFormat = ImageFormat.Png,
            RasterDpi = 150
        });

        // Get the first page as PNG
        var firstPage = images.FirstOrDefault();
        if (firstPage == null)
        {
            return Task.FromResult(Array.Empty<byte>());
        }

        return Task.FromResult(firstPage);
    }

    public async Task<byte[][]> ExportAllPagesAsync(Resume resume, string templateId)
    {
        var template = _templateRegistry.GetTemplateOrDefault(templateId);
        var document = template.CreateDocument(resume);

        var images = document.GenerateImages(new ImageGenerationSettings
        {
            ImageFormat = ImageFormat.Png,
            RasterDpi = 150
        });

        return await Task.FromResult(images.ToArray());
    }

    public async Task ExportToFileAsync(Resume resume, string templateId, string filePath)
    {
        var bytes = await ExportAsync(resume, templateId);
        await File.WriteAllBytesAsync(filePath, bytes);
    }

    public async Task ExportAllPagesToFilesAsync(Resume resume, string templateId, string baseFilePath)
    {
        var pages = await ExportAllPagesAsync(resume, templateId);

        for (int i = 0; i < pages.Length; i++)
        {
            var fileName = pages.Length == 1
                ? baseFilePath
                : Path.Combine(
                    Path.GetDirectoryName(baseFilePath) ?? "",
                    $"{Path.GetFileNameWithoutExtension(baseFilePath)}_page{i + 1}{Path.GetExtension(baseFilePath)}");

            await File.WriteAllBytesAsync(fileName, pages[i]);
        }
    }
}
