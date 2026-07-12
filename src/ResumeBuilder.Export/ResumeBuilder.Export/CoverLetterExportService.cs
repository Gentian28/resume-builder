using ResumeBuilder.Core.Models;
using ResumeBuilder.Templates;

namespace ResumeBuilder.Export;

/// <summary>
/// The cover-letter counterpart of <see cref="ExportService"/>. It is a separate service rather than
/// more formats on that one because <see cref="IExporter"/> is typed to <see cref="Resume"/>, and the
/// two document types share format names ("PDF", "DOCX") that would collide in one registry.
/// </summary>
public class CoverLetterExportService
{
    private readonly Dictionary<string, ICoverLetterExporter> _exporters;

    public CoverLetterExportService(TemplateRegistry templateRegistry)
    {
        _exporters = new Dictionary<string, ICoverLetterExporter>(StringComparer.OrdinalIgnoreCase);

        Register(new CoverLetterPdfExporter(templateRegistry));
        Register(new CoverLetterDocxExporter());
        Register(new CoverLetterTextExporter());
    }

    public void Register(ICoverLetterExporter exporter)
    {
        _exporters[exporter.Format] = exporter;
    }

    public ICoverLetterExporter? GetExporter(string format)
    {
        return _exporters.TryGetValue(format, out var exporter) ? exporter : null;
    }

    public IEnumerable<ExportFormat> GetAvailableFormats()
    {
        return _exporters.Values.Select(e => new ExportFormat
        {
            Name = e.Format,
            Extension = e.FileExtension,
            MimeType = e.MimeType
        });
    }

    public async Task<byte[]> ExportAsync(CoverLetter letter, string format)
    {
        var exporter = GetExporter(format)
            ?? throw new ArgumentException($"Unknown export format: {format}");

        return await exporter.ExportAsync(letter, letter.SelectedTemplateId);
    }

    public async Task ExportToFileAsync(CoverLetter letter, string format, string filePath)
    {
        var exporter = GetExporter(format)
            ?? throw new ArgumentException($"Unknown export format: {format}");

        await exporter.ExportToFileAsync(letter, letter.SelectedTemplateId, filePath);
    }
}
