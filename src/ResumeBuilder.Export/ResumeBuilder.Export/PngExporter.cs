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

    private const int RasterDpi = 150;
    private const int PageGap = 20;

    public PngExporter(TemplateRegistry templateRegistry)
    {
        _templateRegistry = templateRegistry;

        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>
    /// A multi-page resume is stitched into one tall image so a single-image caller never silently
    /// loses pages 2+.
    /// </summary>
    public Task<byte[]> ExportAsync(Resume resume, string templateId)
    {
        var pages = RenderPages(resume, templateId);

        return Task.FromResult(pages.Length switch
        {
            0 => Array.Empty<byte>(),
            1 => pages[0],
            _ => StitchVertically(pages)
        });
    }

    public Task<byte[][]> ExportAllPagesAsync(Resume resume, string templateId)
    {
        return Task.FromResult(RenderPages(resume, templateId));
    }

    public async Task ExportToFileAsync(Resume resume, string templateId, string filePath)
    {
        await ExportAllPagesToFilesAsync(resume, templateId, filePath);
    }

    /// <summary>
    /// Writes one file per page. A single-page resume keeps the requested name; multiple pages get a
    /// "-p1", "-p2" suffix.
    /// </summary>
    public async Task ExportAllPagesToFilesAsync(Resume resume, string templateId, string baseFilePath)
    {
        var pages = await ExportAllPagesAsync(resume, templateId);

        for (var i = 0; i < pages.Length; i++)
        {
            var fileName = pages.Length == 1
                ? baseFilePath
                : Path.Combine(
                    Path.GetDirectoryName(baseFilePath) ?? "",
                    $"{Path.GetFileNameWithoutExtension(baseFilePath)}-p{i + 1}{Path.GetExtension(baseFilePath)}");

            await File.WriteAllBytesAsync(fileName, pages[i]);
        }
    }

    private byte[][] RenderPages(Resume resume, string templateId)
    {
        var template = _templateRegistry.GetTemplateOrDefault(templateId);
        var document = template.CreateDocument(resume);

        return document.GenerateImages(new ImageGenerationSettings
        {
            ImageFormat = ImageFormat.Png,
            RasterDpi = RasterDpi
        }).ToArray();
    }

    private static byte[] StitchVertically(byte[][] pages)
    {
        var bitmaps = new List<SKBitmap>(pages.Length);

        try
        {
            foreach (var page in pages)
            {
                var bitmap = SKBitmap.Decode(page);
                if (bitmap != null)
                    bitmaps.Add(bitmap);
            }

            if (bitmaps.Count == 0)
                return Array.Empty<byte>();

            if (bitmaps.Count == 1)
                return pages[0];

            var width = bitmaps.Max(b => b.Width);
            var height = bitmaps.Sum(b => b.Height) + PageGap * (bitmaps.Count - 1);

            using var surface = SKSurface.Create(new SKImageInfo(width, height));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);

            var y = 0f;
            foreach (var bitmap in bitmaps)
            {
                // Center narrower pages so a mixed-width document does not look ragged.
                var x = (width - bitmap.Width) / 2f;
                canvas.DrawBitmap(bitmap, x, y);
                y += bitmap.Height + PageGap;
            }

            canvas.Flush();

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }
        finally
        {
            foreach (var bitmap in bitmaps)
                bitmap.Dispose();
        }
    }
}
