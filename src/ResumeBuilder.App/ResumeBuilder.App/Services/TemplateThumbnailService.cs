using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using ResumeBuilder.Export;

namespace ResumeBuilder.App.Services;

/// <summary>
/// Renders and caches a preview image of each résumé template.
///
/// The template gallery previously listed 25 designs as text alone, so people chose a visual
/// layout by reading a description of it. This turns that list into something you can look at.
///
/// Thumbnails are generated on demand rather than at start-up: 25 QuestPDF renders is measurable
/// work, and doing it eagerly would delay a launch for something most sessions never open. They
/// are cached to disk so it happens once per app version, not once per run.
/// </summary>
public sealed class TemplateThumbnailService
{
    private readonly PngExporter _pngExporter;
    private readonly string _cacheDirectory;

    // Guards against the same template being rendered twice concurrently, which is easy to trigger
    // when a gallery binds every card at once.
    private readonly ConcurrentDictionary<string, Task<Bitmap?>> _inFlight = new(StringComparer.Ordinal);

    public TemplateThumbnailService(PngExporter pngExporter)
    {
        _pngExporter = pngExporter;

        // Keyed by app version: a template's design can change between releases, and a stale
        // thumbnail is worse than a missing one because nothing signals it is wrong.
        var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "dev";

        _cacheDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ResumeBuilder",
            "thumbnails",
            version);
    }

    /// <summary>
    /// Returns the thumbnail for a template, rendering it if this is the first request.
    /// Never throws: a template that fails to render returns null so the gallery can fall back to
    /// its text card rather than taking the window down.
    /// </summary>
    public Task<Bitmap?> GetAsync(string templateId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(templateId))
            return Task.FromResult<Bitmap?>(null);

        return _inFlight.GetOrAdd(templateId, id => LoadOrRenderAsync(id, cancellationToken));
    }

    private async Task<Bitmap?> LoadOrRenderAsync(string templateId, CancellationToken cancellationToken)
    {
        try
        {
            var path = Path.Combine(_cacheDirectory, SafeFileName(templateId) + ".png");

            var bytes = await ReadCachedAsync(path, cancellationToken).ConfigureAwait(false)
                        ?? await RenderAndCacheAsync(templateId, path, cancellationToken).ConfigureAwait(false);

            if (bytes is null || bytes.Length == 0)
                return null;

            using var stream = new MemoryStream(bytes);
            return new Bitmap(stream);
        }
        catch (Exception)
        {
            // A missing thumbnail degrades the gallery; an exception here would break it entirely.
            return null;
        }
    }

    private static async Task<byte[]?> ReadCachedAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(path))
                return null;

            return await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Corrupt or locked cache file: fall through and re-render.
            return null;
        }
    }

    private async Task<byte[]?> RenderAndCacheAsync(string templateId, string path, CancellationToken cancellationToken)
    {
        // QuestPDF rendering is synchronous and CPU-bound, so keep it off the UI thread.
        var bytes = await Task.Run(
            () => _pngExporter.RenderThumbnail(ThumbnailSample.Create(), templateId),
            cancellationToken).ConfigureAwait(false);

        if (bytes.Length == 0)
            return null;

        try
        {
            Directory.CreateDirectory(_cacheDirectory);
            await File.WriteAllBytesAsync(path, bytes, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Caching is an optimisation. If the disk write fails the thumbnail still displays,
            // it just gets rendered again next time.
        }

        return bytes;
    }

    /// <summary>Template ids are slugs today, but they reach the filesystem, so don't trust that.</summary>
    private static string SafeFileName(string templateId)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            templateId = templateId.Replace(c, '_');

        return templateId;
    }
}
