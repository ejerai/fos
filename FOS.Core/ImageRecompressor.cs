using SkiaSharp;

namespace FOS.Core;

internal static class ImageRecompressor
{
    public static byte[]? Recompress(byte[] data, string extension, int maxSide, int quality)
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
            default:
                return null;
        }

        // lewati gambar yang bergantung pada rotasi exif
        using (var probe = SKCodec.Create(new MemoryStream(data)))
        {
            if (probe == null || probe.EncodedOrigin != SKEncodedOrigin.TopLeft) return null;
        }

        SKColor? background = format == SKEncodedImageFormat.Jpeg ? SKColors.White : null;
        using var bitmap = ImageLoader.Load(new MemoryStream(data), maxSide, background);
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(format, format == SKEncodedImageFormat.Png ? 100 : quality);

        if (encoded == null || encoded.Size > data.Length * 0.95) return null;
        return encoded.ToArray();
    }
}