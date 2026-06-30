using ResumeBuilder.Core.Models;
using ResumeBuilder.Export.Exporters;
using ResumeBuilder.Export.Importers;
using ResumeBuilder.Templates;

namespace ResumeBuilder.Export;

public class ExportService
{
    private readonly Dictionary<string, IExporter> _exporters;
    private readonly Dictionary<string, IImporter> _importers;
    private readonly TemplateRegistry _templateRegistry;

    public ExportService(TemplateRegistry templateRegistry)
    {
        _templateRegistry = templateRegistry;
        _exporters = new Dictionary<string, IExporter>(StringComparer.OrdinalIgnoreCase);
        _importers = new Dictionary<string, IImporter>(StringComparer.OrdinalIgnoreCase);

        RegisterDefaultExporters();
        RegisterDefaultImporters();
    }

    private void RegisterDefaultExporters()
    {
        Register(new PdfExporter(_templateRegistry));
        Register(new DocxExporter(_templateRegistry));
        Register(new HtmlExporter(_templateRegistry));
        Register(new PngExporter(_templateRegistry));
        Register(new JsonExporter());
        Register(new JsonResumeExporter());
    }

    private void RegisterDefaultImporters()
    {
        RegisterImporter(new JsonResumeImporter());
        RegisterImporter(new LinkedInImporter());
        RegisterImporter(new PdfImporter());
    }

    public void Register(IExporter exporter)
    {
        _exporters[exporter.Format] = exporter;
    }

    public IExporter? GetExporter(string format)
    {
        return _exporters.TryGetValue(format, out var exporter) ? exporter : null;
    }

    public IEnumerable<IExporter> GetAllExporters()
    {
        return _exporters.Values;
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

    public async Task<byte[]> ExportAsync(Resume resume, string format)
    {
        var exporter = GetExporter(format)
            ?? throw new ArgumentException($"Unknown export format: {format}");

        return await exporter.ExportAsync(resume, resume.SelectedTemplateId);
    }

    public async Task ExportToFileAsync(Resume resume, string format, string filePath)
    {
        var exporter = GetExporter(format)
            ?? throw new ArgumentException($"Unknown export format: {format}");

        await exporter.ExportToFileAsync(resume, resume.SelectedTemplateId, filePath);
    }

    // Import functionality
    public void RegisterImporter(IImporter importer)
    {
        _importers[importer.Format] = importer;
    }

    public IImporter? GetImporter(string format)
    {
        return _importers.TryGetValue(format, out var importer) ? importer : null;
    }

    public IImporter? GetImporterByExtension(string extension)
    {
        extension = extension.ToLowerInvariant();
        if (!extension.StartsWith("."))
            extension = "." + extension;

        return _importers.Values.FirstOrDefault(i =>
            i.SupportedExtensions.Any(e => e.Equals(extension, StringComparison.OrdinalIgnoreCase)));
    }

    public IEnumerable<IImporter> GetAllImporters()
    {
        return _importers.Values;
    }

    public async Task<ImportResult<Resume>> ImportAsync(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        var importer = GetImporterByExtension(extension);

        if (importer == null)
        {
            return ImportResult<Resume>.Failed($"No importer found for extension: {extension}");
        }

        return await importer.ImportFromFileAsync(filePath);
    }

    public async Task<ImportResult<Resume>> ImportAsync(Stream stream, string format)
    {
        var importer = GetImporter(format);

        if (importer == null)
        {
            return ImportResult<Resume>.Failed($"No importer found for format: {format}");
        }

        return await importer.ImportAsync(stream);
    }
}

public class ExportFormat
{
    public required string Name { get; init; }
    public required string Extension { get; init; }
    public required string MimeType { get; init; }
}
