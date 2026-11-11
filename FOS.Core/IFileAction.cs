namespace FOS.Core;

public sealed record ActionResult(bool Success, string Message);

public interface IFileAction
{
    // Teks yang tampil di tombol
    string Label { get; }

    // Apakah aksi ini cocok untuk file yang sedang terpilih
    bool CanHandle(IReadOnlyList<string> paths);

    Task<ActionResult> RunAsync(IReadOnlyList<string> paths);
}