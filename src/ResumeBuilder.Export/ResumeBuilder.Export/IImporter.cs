using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Export;

public interface IImporter
{
    string Format { get; }
    string[] SupportedExtensions { get; }
    Task<ImportResult<Resume>> ImportAsync(Stream stream);
    Task<ImportResult<Resume>> ImportFromFileAsync(string filePath);
}

public class ImportResult<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Warnings { get; set; } = new();

    public static ImportResult<T> Succeeded(T data, List<string>? warnings = null) => new()
    {
        Success = true,
        Data = data,
        Warnings = warnings ?? new()
    };

    public static ImportResult<T> Failed(string error) => new()
    {
        Success = false,
        ErrorMessage = error
    };
}
