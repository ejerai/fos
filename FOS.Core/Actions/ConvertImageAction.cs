using SkiaSharp;

namespace FOS.Core.Actions;

public sealed class ConvertImageAction : IFileAction
{
    private static readonly HashSet<string> InputExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".gif" };

    private readonly string _target; // "png", "jpg", atau "webp"

    public ConvertImageAction(string target)
    {
        _target = target.ToLowerInvariant();
    }

    public string Label => _target switch
    {
        "png" => "Ke PNG",
        "jpg" => "Ke JPG",
        "webp" => "Ke WebP",
        _ => "Ke " + _target
    };

    // Muncul hanya kalau semua file adalah gambar yang didukung
    // dan belum berformat tujuan
    public bool CanHandle(IReadOnlyList<string> paths) =>
        paths.Count > 0 && paths.All(p => IsSupported(p) && !IsAlreadyTarget(p));

    private static bool IsSupported(string path) =>
        InputExtensions.Contains(Path.GetExtension(path));

    private bool IsAlreadyTarget(string path)
    {
        var ext = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
        if (ext == "jpeg") ext = "jpg";
        return ext == _target;
    }

    public async Task<ActionResult> RunAsync(IReadOnlyList<string> paths)
    {
        int done = 0;
        string lastMessage = "";

        foreach (var path in paths)
        {
            try
            {
                lastMessage = await Task.Run(() => ConvertOne(path));
                done++;
            }
            catch (Exception ex)
            {
                return new ActionResult(false, "Gagal: " + ex.Message);
            }
        }

        return new ActionResult(true, done == 1 ? lastMessage : $"{done} gambar dikonversi");
    }

    private string ConvertOne(string path)
    {
        using var source = SKBitmap.Decode(path);
        if (source == null)
            throw new InvalidOperationException("Format gambar tidak didukung");

        SKBitmap bitmap = source;
        SKBitmap? flattened = null;

        if (_target == "jpg")
        {
            // JPG tidak mendukung transparansi: gambar di atas latar putih
            flattened = new SKBitmap(source.Width, source.Height, SKColorType.Rgba8888, SKAlphaType.Opaque);
            using (var canvas = new SKCanvas(flattened))
            {
                canvas.Clear(SKColors.White);
                canvas.DrawBitmap(source, 0, 0, new SKSamplingOptions());
            }
            bitmap = flattened;
        }

        try
        {
            using var image = SKImage.FromBitmap(bitmap);

            var (format, quality) = _target switch
            {
                "png" => (SKEncodedImageFormat.Png, 100),
                "jpg" => (SKEncodedImageFormat.Jpeg, 90),
                _ => (SKEncodedImageFormat.Webp, 85)
            };

            using var data = image.Encode(format, quality);
            if (data == null)
                throw new InvalidOperationException("Gagal menyimpan gambar");

            var outPath = FileNaming.UniquePath(Path.ChangeExtension(path, _target));
            File.WriteAllBytes(outPath, data.ToArray());

            long before = new FileInfo(path).Length;
            long after = new FileInfo(outPath).Length;
            return $"{Path.GetFileName(outPath)} ({FileNaming.FormatSize(before)} → {FileNaming.FormatSize(after)})";
        }
        finally
        {
            flattened?.Dispose();
        }
    }
}