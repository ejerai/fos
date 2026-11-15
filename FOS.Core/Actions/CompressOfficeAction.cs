namespace FOS.Core.Actions;

public sealed class CompressOfficeAction : IFileAction
{
    private const double MinSavingRatio = 0.05;

    private static readonly HashSet<string> Extensions =
        new(StringComparer.OrdinalIgnoreCase) { ".docx", ".pptx", ".xlsx" };

    private readonly Func<CompressProfile> _profile;

    public CompressOfficeAction(Func<CompressProfile> profile)
    {
        _profile = profile;
    }

    public string Label => "Kompres";

    public bool CanHandle(IReadOnlyList<string> paths) =>
        paths.Count > 0 && paths.All(p => Extensions.Contains(Path.GetExtension(p)));

    private sealed record Outcome(long Before, long After, string Name);

    public async Task<ActionResult> RunAsync(IReadOnlyList<string> paths)
    {
        var profile = _profile();
        var saved = new List<Outcome>();

        foreach (var path in paths)
        {
            try
            {
                var outcome = await Task.Run(() => CompressOne(path, profile));
                if (outcome != null) saved.Add(outcome);
            }
            catch (Exception ex)
            {
                return new ActionResult(false, "Gagal: " + ex.Message);
            }
        }

        if (saved.Count == 0)
        {
            return new ActionResult(false,
                paths.Count == 1
                    ? "Sudah cukup kecil, tidak ada yang dihemat"
                    : "Semua dokumen sudah cukup kecil, tidak ada yang dihemat");
        }

        long before = saved.Sum(o => o.Before);
        long after = saved.Sum(o => o.After);
        int percent = (int)Math.Round(100.0 * (before - after) / before);
        string sizes = $"{FileNaming.FormatSize(before)} → {FileNaming.FormatSize(after)}, hemat {percent}%";

        return paths.Count == 1
            ? new ActionResult(true, $"{saved[0].Name} ({sizes})")
            : new ActionResult(true, $"{saved.Count} dokumen dikompres ({sizes})");
    }

    private static Outcome? CompressOne(string path, CompressProfile profile)
    {
        var dir = Path.GetDirectoryName(path) ?? "";
        var name = Path.GetFileNameWithoutExtension(path) + "-compressed" + Path.GetExtension(path);
        var outPath = FileNaming.UniquePath(Path.Combine(dir, name));

        try
        {
            OfficeCompressor.Compress(path, outPath,
                (bytes, ext) => ImageRecompressor.Recompress(bytes, ext, profile.MaxSide, profile.Quality));

            long before = new FileInfo(path).Length;
            long after = new FileInfo(outPath).Length;

            // hemat kurang dari 5% dibuang
            if (after > before * (1 - MinSavingRatio))
            {
                File.Delete(outPath);
                return null;
            }

            return new Outcome(before, after, Path.GetFileName(outPath));
        }
        catch
        {
            if (File.Exists(outPath)) File.Delete(outPath);
            throw;
        }
    }
}