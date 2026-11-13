using SkiaSharp;

namespace FOS.Core.Actions;

public sealed class CompressImageAction : IFileAction
{
    // Sisi terpanjang maksimal (piksel) dan kualitas untuk JPG dan WebP.
    // Ubah angka ini kalau mau kompresi lebih ringan atau lebih kuat.
    private const int MaxSide = 2000;
    private const int Quality = 70;

    // Hasil yang hematnya kurang dari ini dianggap tidak berarti dan tidak disimpan
    private const double MinSavingRatio = 0.05;

    private static readonly HashSet<string> Extensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

    public string Label => "Kompres";

    public bool CanHandle(IReadOnlyList<string> paths) =>
        paths.Count > 0 && paths.All(p => Extensions.Contains(Path.GetExtension(p)));

    private sealed record Outcome(bool Saved, long Before, long After, string Name);

    public async Task<ActionResult> RunAsync(IReadOnlyList<string> paths)
    {
        var outcomes = new List<Outcome>();

        foreach (var path in paths)
        {
            try
            {
                outcomes.Add(await Task.Run(() => CompressOne(path)));
            }
            catch (Exception ex)
            {
                return new ActionResult(false, "Gagal: " + ex.Message);
            }
        }

        var saved = outcomes.Where(o => o.Saved).ToList();
        int skipped = outcomes.Count - saved.Count;

        if (saved.Count == 0)
        {
            return new ActionResult(false,
                paths.Count == 1
                    ? "Sudah cukup kecil, tidak ada yang dihemat"
                    : "Semua gambar sudah cukup kecil, tidak ada yang dihemat");
        }

        long before = saved.Sum(o => o.Before);
        long after = saved.Sum(o => o.After);
        int percent = (int)Math.Round(100.0 * (before - after) / before);
        string sizes = $"{FileNaming.FormatSize(before)} → {FileNaming.FormatSize(after)}, hemat {percent}%";

        if (paths.Count == 1)
            return new ActionResult(true, $"{saved[0].Name} ({sizes})");

        string tail = skipped > 0 ? $", {skipped} dilewati" : "";
        return new ActionResult(true, $"{saved.Count} gambar dikompres ({sizes}){tail}");
    }

    private static Outcome CompressOne(string path)
    {
        var ext = Path.GetExtension(path);
        var (format, quality) = ext.ToLowerInvariant() switch
        {
            ".png" => (SKEncodedImageFormat.Png, 100),
            ".webp" => (SKEncodedImageFormat.Webp, Quality),
            _ => (SKEncodedImageFormat.Jpeg, Quality)
        };

        // JPG tidak mendukung transparansi, PNG dan WebP tetap mempertahankannya
        SKColor? background = format == SKEncodedImageFormat.Jpeg ? SKColors.White : null;

        using var bitmap = ImageLoader.Load(path, MaxSide, background);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, quality);
        if (data == null)
            throw new InvalidOperationException("Gagal menyimpan gambar");

        long before = new FileInfo(path).Length;

        // Jangan menyimpan hasil yang hematnya kurang dari 5%
        if (data.Size > before * (1 - MinSavingRatio))
            return new Outcome(false, before, before, Path.GetFileName(path));

        var dir = Path.GetDirectoryName(path) ?? "";
        var name = Path.GetFileNameWithoutExtension(path) + "-compressed" + ext;
        var outPath = FileNaming.UniquePath(Path.Combine(dir, name));
        File.WriteAllBytes(outPath, data.ToArray());

        return new Outcome(true, before, new FileInfo(outPath).Length, Path.GetFileName(outPath));
    }
}