namespace FOS.Core;

public sealed record ActionResult(bool Success, string Message);

public interface IFileAction
{
    // Teks yang tampil di tombol
    string Label { get; }

    // true = hanya ditampilkan kalau tidak ada aksi utama untuk file terpilih
    bool IsFallback => false;

    // Apakah aksi ini cocok untuk file yang sedang terpilih
    bool CanHandle(IReadOnlyList<string> paths);

    Task<ActionResult> RunAsync(IReadOnlyList<string> paths);
}