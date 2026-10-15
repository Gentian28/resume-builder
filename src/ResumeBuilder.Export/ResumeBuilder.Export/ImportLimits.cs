using System.IO.Compression;

namespace ResumeBuilder.Export;

public static class ImportLimits
{
    public const int MaximumBytes = 64 * 1024 * 1024;
    public static async Task<byte[]> ReadAsync(Stream stream, int maximumBytes = MaximumBytes, CancellationToken cancellationToken = default)
    {
        using var output = new MemoryStream();
        var chunk = new byte[81920];
        int count;
        while ((count = await stream.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (output.Length + count > maximumBytes) throw new InvalidDataException("The import exceeds its supported size limit.");
            await output.WriteAsync(chunk.AsMemory(0, count), cancellationToken);
        }
        return output.ToArray();
    }
    public static void ValidateArchive(ZipArchive archive)
    {
        if (archive.Entries.Count > 2000 || archive.Entries.Any(e => e.Length > 16 * 1024 * 1024) || archive.Entries.Sum(e => e.Length) > MaximumBytes)
            throw new InvalidDataException("The archive has too many entries or expands beyond the import limit.");
    }
}
