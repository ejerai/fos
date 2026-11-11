namespace FOS.Core;

public static class FileNaming
{
    // Kalau file sudah ada, tambahkan " (1)", " (2)", dan seterusnya.
    // File asli tidak pernah ditimpa.
    public static string UniquePath(string path)
    {
        if (!File.Exists(path)) return path;

        var dir = Path.GetDirectoryName(path) ?? "";
        var name = Path.GetFileNameWithoutExtension(path);
        var ext = Path.GetExtension(path);

        for (int i = 1; ; i++)
        {
            var candidate = Path.Combine(dir, $"{name} ({i}){ext}");
            if (!File.Exists(candidate)) return candidate;
        }
    }

    public static string FormatSize(long bytes) =>
        bytes >= 1024 * 1024
            ? $"{bytes / 1024.0 / 1024.0:0.0} MB"
            : $"{bytes / 1024.0:0} KB";
}