using System.Diagnostics;

namespace FOS.Core.Actions;

public sealed class OpenFolderAction : IFileAction
{
    public string Label => "Buka Folder";

    public bool CanHandle(IReadOnlyList<string> paths) => paths.Count == 1;

    public Task<ActionResult> RunAsync(IReadOnlyList<string> paths)
    {
        var path = paths[0];
        Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"")
        {
            UseShellExecute = true
        });
        return Task.FromResult(new ActionResult(true, "Folder dibuka"));
    }
}