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

    // Muncul kalau semua file adalah gambar yang didukung dan minimal satu
    // belum berformat tujuan. File yang sudah berformat tujuan dilewati.
    public bool CanHandle(IReadOnlyList<string> paths) =>
        paths.Count > 0 && paths.All(IsSupported) && paths.Any(p => !IsAlreadyTarget(p));

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
        var todo = paths.Where(p => !IsAlreadyTarget(p)).ToList();
        int skipped = paths.Count - todo.Count;

        int done = 0;
        string lastMessage = "";

        foreach (var path in todo)
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

        if (todo.Count == 1 && skipped == 0)
            return new ActionResult(true, lastMessage);

        string message = $"{done} gambar dikonversi";
        if (skipped > 0)
            message += $", {skipped} dilewati (sudah {_target.ToUpperInvariant()})";

        return new ActionResult(true, message);
    }

    private string ConvertOne(string path)
    {
        // JPG tidak mendukung transparansi: gambar di atas latar putih
        SKColor? background = _target == "jpg" ? SKColors.White : null;

        using var bitmap = ImageLoader.Load(path, 0, background);
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
}