using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

namespace ResumeBuilder.App.Services;

public class ImageService
{
    private const int MaxPhotoWidth = 400;
    private const int MaxPhotoHeight = 400;
    private const int MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB

    public async Task<byte[]?> LoadAndResizeImageAsync(IStorageFile file)
    {
        await using var stream = await file.OpenReadAsync();

        // Check file size
        if (stream.Length > MaxFileSizeBytes)
        {
            throw new InvalidOperationException("Image file is too large. Maximum size is 5MB.");
        }

        // Read the image
        using var memStream = new MemoryStream();
        await stream.CopyToAsync(memStream);
        var originalBytes = memStream.ToArray();

        // Validate it's a valid image by trying to decode it
        memStream.Position = 0;
        try
        {
            var bitmap = new Bitmap(memStream);

            // Resize if needed
            if (bitmap.PixelSize.Width > MaxPhotoWidth || bitmap.PixelSize.Height > MaxPhotoHeight)
            {
                var scale = Math.Min(
                    (double)MaxPhotoWidth / bitmap.PixelSize.Width,
                    (double)MaxPhotoHeight / bitmap.PixelSize.Height);

                var newWidth = (int)(bitmap.PixelSize.Width * scale);
                var newHeight = (int)(bitmap.PixelSize.Height * scale);

                // Create resized bitmap
                using var resizedStream = new MemoryStream();
                var resizedBitmap = bitmap.CreateScaledBitmap(
                    new Avalonia.PixelSize(newWidth, newHeight),
                    Avalonia.Media.Imaging.BitmapInterpolationMode.HighQuality);

                resizedBitmap.Save(resizedStream);
                return resizedStream.ToArray();
            }

            return originalBytes;
        }
        catch (Exception)
        {
            throw new InvalidOperationException("Invalid image format. Please use JPEG, PNG, or BMP.");
        }
    }

    public Bitmap? BytesToBitmap(byte[]? imageData)
    {
        if (imageData == null || imageData.Length == 0)
            return null;

        try
        {
            using var stream = new MemoryStream(imageData);
            return new Bitmap(stream);
        }
        catch
        {
            return null;
        }
    }

    public static FilePickerFileType ImageFileTypes { get; } = new("Images")
    {
        Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif" },
        MimeTypes = new[] { "image/png", "image/jpeg", "image/bmp", "image/gif" }
    };
}
