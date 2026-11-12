using SkiaSharp;

namespace FOS.Core;

internal static class ImageLoader
{
    // Membaca gambar, menerapkan rotasi EXIF (foto HP), dan memperkecil
    // kalau sisi terpanjang melebihi maxSide (0 = tidak diperkecil).
    // background != null: gambar disusun di atas warna itu (dipakai untuk JPG).
    public static SKBitmap Load(string path, int maxSide = 0, SKColor? background = null)
    {
        using var stream = File.OpenRead(path);
        using var codec = SKCodec.Create(stream)
            ?? throw new InvalidOperationException("Format gambar tidak didukung");

        var origin = codec.EncodedOrigin;
        using var source = SKBitmap.Decode(codec)
            ?? throw new InvalidOperationException("Gambar tidak bisa dibaca");

        int srcW = source.Width;
        int srcH = source.Height;

        bool swap = origin == SKEncodedOrigin.LeftTop
                 || origin == SKEncodedOrigin.RightTop
                 || origin == SKEncodedOrigin.RightBottom
                 || origin == SKEncodedOrigin.LeftBottom;
        int w = swap ? srcH : srcW;
        int h = swap ? srcW : srcH;

        float scale = 1f;
        int longest = Math.Max(w, h);
        if (maxSide > 0 && longest > maxSide)
        {
            scale = (float)maxSide / longest;
        }

        int outW = Math.Max(1, (int)Math.Round(w * scale));
        int outH = Math.Max(1, (int)Math.Round(h * scale));

        var dest = new SKBitmap(new SKImageInfo(outW, outH, SKColorType.Rgba8888, SKAlphaType.Premul));
        using (var canvas = new SKCanvas(dest))
        {
            canvas.Clear(background ?? SKColors.Transparent);

            // Skala dipasang pertama, jadi berlaku paling akhir
            if (scale != 1f) canvas.Scale(scale, scale);

            switch (origin)
            {
                case SKEncodedOrigin.TopRight:
                    canvas.Translate(srcW, 0);
                    canvas.Scale(-1, 1);
                    break;
                case SKEncodedOrigin.BottomRight:
                    canvas.Translate(srcW, srcH);
                    canvas.RotateDegrees(180);
                    break;
                case SKEncodedOrigin.BottomLeft:
                    canvas.Translate(0, srcH);
                    canvas.Scale(1, -1);
                    break;
                case SKEncodedOrigin.LeftTop:
                    canvas.Scale(-1, 1);
                    canvas.RotateDegrees(90);
                    break;
                case SKEncodedOrigin.RightTop:
                    canvas.Translate(srcH, 0);
                    canvas.RotateDegrees(90);
                    break;
                case SKEncodedOrigin.RightBottom:
                    canvas.Translate(srcH, srcW);
                    canvas.Scale(-1, 1);
                    canvas.RotateDegrees(-90);
                    break;
                case SKEncodedOrigin.LeftBottom:
                    canvas.Translate(0, srcW);
                    canvas.RotateDegrees(-90);
                    break;
            }

            var sampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);
            canvas.DrawBitmap(source, 0, 0, sampling);
        }

        return dest;
    }
}