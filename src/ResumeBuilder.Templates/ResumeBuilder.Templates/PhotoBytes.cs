using System.Diagnostics.CodeAnalysis;
using QuestPDF.Infrastructure;

namespace ResumeBuilder.Templates;

/// <summary>
/// Whether a byte array is an image the PDF renderer can draw.
///
/// Photo bytes arrive from JSON imports and from the database, and neither path proves they are an
/// image. QuestPDF throws while composing when they are not, which takes down every render of that
/// resume, preview included, with nothing on screen to say why. Checking here lets the templates
/// fall back to initials and the importers drop the photo with a warning instead.
/// </summary>
public static class PhotoBytes
{
    public static bool IsRenderable([NotNullWhen(true)] byte[]? bytes)
    {
        if (bytes is null || bytes.Length == 0)
            return false;

        try
        {
            if (bytes.Length > 10 * 1024 * 1024) return false;
            using var data = SkiaSharp.SKData.CreateCopy(bytes);
            using var codec = SkiaSharp.SKCodec.Create(data);
            if (codec is null || codec.Info.Width <= 0 || codec.Info.Height <= 0 || (long)codec.Info.Width * codec.Info.Height > 25_000_000) return false;
            Image.FromBinaryData(bytes);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
