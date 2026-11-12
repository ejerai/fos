using FOS.Core;

namespace FOS.App;

public sealed class CopyPathAction : IFileAction
{
    public string Label => "Salin Path";
    public bool IsFallback => true;
    public bool CanHandle(IReadOnlyList<string> paths) => paths.Count > 0;

    public Task<ActionResult> RunAsync(IReadOnlyList<string> paths)
    {
        var text = string.Join(Environment.NewLine, paths);
        try
        {
            System.Windows.Clipboard.SetDataObject(text, true);
            var message = paths.Count == 1 ? "Path disalin" : $"{paths.Count} path disalin";
            return Task.FromResult(new ActionResult(true, message));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ActionResult(false, "Gagal menyalin: " + ex.Message));
        }
    }
}