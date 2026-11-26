namespace FOS.Core.Actions;

public sealed class CompressImageAction : IFileAction
{
    private const double MinSavingRatio = 0.05;

    private static readonly HashSet<string> Extensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

    private readonly Func<int> _targetPercent;

    public CompressImageAction(Func<int> targetPercent)
    {
        _targetPercent = targetPercent;
    }

    public string Label => "Kompres";

    public bool CanHandle(IReadOnlyList<string> paths) =>
        paths.Count > 0 && paths.All(p => Extensions.Contains(Path.GetExtension(p)));

    private sealed record Outcome(bool Saved, long Before, long After, string Name);

    public async Task<ActionResult> RunAsync(IReadOnlyList<string> paths)
    {
        int target = _targetPercent();
        var outcomes = new List<Outcome>();

        foreach (var path in paths)
        {
            try
            {
                outcomes.Add(await Task.Run(() => CompressOne(path, target)));
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

        string sizes = CompressReport.Sizes(saved.Sum(o => o.Before), saved.Sum(o => o.After), target);

        if (paths.Count == 1)
            return new ActionResult(true, $"{saved[0].Name} ({sizes})");

        string tail = skipped > 0 ? $", {skipped} dilewati" : "";
        return new ActionResult(true, $"{saved.Count} gambar dikompres ({sizes}){tail}");
    }

    private static Outcome CompressOne(string path, int target)
    {
        var ext = Path.GetExtension(path);
        var original = File.ReadAllBytes(path);
        long goal = (long)(original.Length * (1 - target / 100.0));

        var cache = new Dictionary<int, byte[]?>();
        byte[]? DataAt(int level)
        {
            if (!cache.TryGetValue(level, out var data))
            {
                data = ImageRecompressor.Recompress(original, ext, CompressLevels.All[level], false);
                cache[level] = data;
            }
            return data;
        }

        int best = TargetSearch.FindLevel(CompressLevels.All.Count, goal,
            level => DataAt(level)?.Length ?? original.Length);

        var result = DataAt(best);

        // hemat kurang dari 5% dibuang
        if (result == null || result.Length > original.Length * (1 - MinSavingRatio))
            return new Outcome(false, original.Length, original.Length, Path.GetFileName(path));

        var dir = Path.GetDirectoryName(path) ?? "";
        var name = Path.GetFileNameWithoutExtension(path) + "-compressed" + ext;
        var outPath = FileNaming.UniquePath(Path.Combine(dir, name));
        File.WriteAllBytes(outPath, result);

        return new Outcome(true, original.Length, result.Length, Path.GetFileName(outPath));
    }
}