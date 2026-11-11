namespace FOS.Core;

public sealed class ActionRegistry
{
    private readonly List<IFileAction> _actions = new();

    public void Register(IFileAction action) => _actions.Add(action);

    // Aksi yang cocok untuk file terpilih (maksimal 6 supaya lingkaran tetap rapi)
    public IReadOnlyList<IFileAction> GetFor(IReadOnlyList<string> paths, int max = 6) =>
        _actions.Where(a => a.CanHandle(paths)).Take(max).ToList();
}