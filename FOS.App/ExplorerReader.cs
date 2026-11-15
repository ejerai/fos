using System.Collections.Concurrent;
using System.Diagnostics;

namespace FOS.App;

public sealed record ReadResult(string[] Paths, long Ms);

// Membaca pilihan file di Explorer lewat satu thread STA tersendiri,
// supaya tampilan FOS tidak tersendat dan pembacaan bisa dimulai secepat mungkin.
public sealed class ExplorerReader : IDisposable
{
    private readonly BlockingCollection<(IntPtr Window, TaskCompletionSource<ReadResult> Result)> _queue = new();
    private readonly Thread _thread;

    public ExplorerReader()
    {
        _thread = new Thread(Loop) { IsBackground = true, Name = "FOS-ExplorerReader" };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
    }

    public Task<ReadResult> ReadAsync(IntPtr rootWindow)
    {
        var tcs = new TaskCompletionSource<ReadResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            _queue.Add((rootWindow, tcs));
        }
        catch (InvalidOperationException)
        {
            tcs.SetResult(new ReadResult(Array.Empty<string>(), 0));
        }
        return tcs.Task;
    }

    private void Loop()
    {
        foreach (var (window, result) in _queue.GetConsumingEnumerable())
        {
            var watch = Stopwatch.StartNew();
            string[] paths;
            try
            {
                paths = ExplorerSelection.GetSelectedPaths(window);
            }
            catch
            {
                paths = Array.Empty<string>();
            }
            result.SetResult(new ReadResult(paths, watch.ElapsedMilliseconds));
        }
    }

    public void Dispose() => _queue.CompleteAdding();
}