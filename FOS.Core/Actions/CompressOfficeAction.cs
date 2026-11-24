namespace FOS.Core.Actions;

public sealed class CompressOfficeAction : IFileAction
{
    private const double MinSavingRatio = 0.05;

    private static readonly HashSet<string> Extensions =
        new(StringComparer.OrdinalIgnoreCase) { ".docx", ".pptx", ".xlsx" };

    private readonly Func<int> _targetPercent;

    public CompressOfficeAction(Func<int> targetPercent)
    {
        _targetPercent = targetPercent;
    }

    public string Label => "Kompres";

    public bool CanHandle(IReadOnlyList<string> paths) =>
        paths.Count > 0 && paths.All(p => Extensions.Contains(Path.GetExtension(p)));

    private sealed record Outcome(long Before, long After, string Name);

    public async Task<ActionResult> RunAsync(IReadOnlyList<string> paths)
    {
        int target = _targetPercent();
        var saved = new List<Outcome>();

        foreach (var path in paths)
        {
            try
            {
                var outcome = await Task.Run(() => CompressOne(path, target));
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

        string sizes = CompressReport.Sizes(saved.Sum(o => o.Before), saved.Sum(o => o.After), target);

        return paths.Count == 1
            ? new ActionResult(true, $"{saved[0].Name} ({sizes})")
            : new ActionResult(true, $"{saved.Count} dokumen dikompres ({sizes})");
    }

    private static Outcome? CompressOne(string path, int target)
    {
        long before = new FileInfo(path).Length;
        long goal = (long)(before * (1 - target / 100.0));
        var trials = new Dictionary<int, string>();

        long SizeAt(int level)
        {
            var temp = Path.GetTempFileName();
            trials[level] = temp;
            OfficeCompressor.Compress(path, temp, (bytes, ext) =>
                ImageRecompressor.Recompress(bytes, ext, CompressLevels.All[level], true));
            return new FileInfo(temp).Length;
        }

        try
        {
            int best = TargetSearch.FindLevel(CompressLevels.All.Count, goal, SizeAt);
            if (!trials.ContainsKey(best)) SizeAt(best);

            var chosen = trials[best];
            long after = new FileInfo(chosen).Length;

            // hemat kurang dari 5% dibuang
            if (after > before * (1 - MinSavingRatio)) return null;

            var dir = Path.GetDirectoryName(path) ?? "";
            var name = Path.GetFileNameWithoutExtension(path) + "-compressed" + Path.GetExtension(path);
            var outPath = FileNaming.UniquePath(Path.Combine(dir, name));

            File.Move(chosen, outPath, true);
            trials.Remove(best);

            return new Outcome(before, after, Path.GetFileName(outPath));
        }
        finally
        {
            foreach (var temp in trials.Values)
            {
                if (File.Exists(temp)) File.Delete(temp);
            }
        }
    }
}