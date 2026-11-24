using SkiaSharp;

namespace FOS.Core;

internal static class ImageRecompressor
{
    public static byte[]? Recompress(byte[] data, string extension, CompressLevel level, bool skipRotated)
    {
        SKEncodedImageFormat format;
        switch (extension.ToLowerInvariant())
        {
            case ".jpg":
            case ".jpeg":
                format = SKEncodedImageFormat.Jpeg;
                break;
            case ".png":
                format = SKEncodedImageFormat.Png;
                break;
            case ".webp":
                format = SKEncodedImageFormat.Webp;
                break;
            default:
                return null;
        }

        // dokumen office: lewati gambar yang bergantung pada rotasi exif
        if (skipRotated)
        {
            using var probe = SKCodec.Create(new MemoryStream(data));
            if (probe == null || probe.EncodedOrigin != SKEncodedOrigin.TopLeft) return null;
        }

        SKColor? background = format == SKEncodedImageFormat.Jpeg ? SKColors.White : null;
        using var bitmap = ImageLoader.LoadScaled(new MemoryStream(data), (float)level.Scale, background);
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(format, format == SKEncodedImageFormat.Png ? 100 : level.Quality);

        if (encoded == null || encoded.Size > data.Length * 0.95) return null;
        return encoded.ToArray();
    }
}