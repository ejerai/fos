namespace FOS.Core;

public sealed class ActionRegistry
{
    private readonly List<IFileAction> _actions = new();

    public void Register(IFileAction action) => _actions.Add(action);

    // Aksi utama dulu (maksimal 6 supaya lingkaran tetap rapi).
    // Aksi cadangan hanya muncul kalau tidak ada aksi utama.
    public IReadOnlyList<IFileAction> GetFor(IReadOnlyList<string> paths, int max = 6)
    {
        var matching = _actions.Where(a => a.CanHandle(paths)).ToList();

        var primary = matching.Where(a => !a.IsFallback).Take(max).ToList();
        return primary.Count > 0 ? primary : matching.Take(max).ToList();
    }
}