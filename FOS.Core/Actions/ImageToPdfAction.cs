using SkiaSharp;

namespace FOS.Core.Actions;

public sealed class ImageToPdfAction : IFileAction
{
    // Sisi terpanjang gambar di dalam PDF (piksel) dan kualitas JPEG
    private const int MaxSide = 2400;
    private const int Quality = 85;

    // WebP sengaja tidak termasuk. Tambahkan ".webp" di daftar ini kalau mau mendukungnya.
    private static readonly HashSet<string> Extensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };

    public string Label => "Ke PDF";

    public bool CanHandle(IReadOnlyList<string> paths) =>
        paths.Count > 0 && paths.All(p => Extensions.Contains(Path.GetExtension(p)));

    public async Task<ActionResult> RunAsync(IReadOnlyList<string> paths)
    {
        try
        {
            return await Task.Run(() => Build(paths));
        }
        catch (Exception ex)
        {
            return new ActionResult(false, "Gagal: " + ex.Message);
        }
    }

    private static ActionResult Build(IReadOnlyList<string> paths)
    {
        // Urutan halaman mengikuti nama file
        var ordered = paths
        .OrderBy(p => Path.GetFileName(p), new NaturalFileNameComparer())
        .ToList();

        var pdf = new SimplePdfBuilder();

        foreach (var path in ordered)
        {
            // Gambar transparan disusun di atas latar putih
            using var bitmap = ImageLoader.Load(path, MaxSide, SKColors.White);
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, Quality);
            if (data == null)
                throw new InvalidOperationException("Gagal memproses gambar");

            pdf.AddJpegPage(data.ToArray(), bitmap.Width, bitmap.Height);
        }

        var first = ordered[0];
        var dir = Path.GetDirectoryName(first) ?? "";
        var baseName = Path.GetFileNameWithoutExtension(first);
        if (ordered.Count > 1) baseName += "-gabungan";

        var outPath = FileNaming.UniquePath(Path.Combine(dir, baseName + ".pdf"));
        pdf.Save(outPath);

        long size = new FileInfo(outPath).Length;
        string pages = ordered.Count == 1 ? "1 halaman" : $"{ordered.Count} halaman";
        return new ActionResult(true, $"{Path.GetFileName(outPath)} ({pages}, {FileNaming.FormatSize(size)})");
    }
}