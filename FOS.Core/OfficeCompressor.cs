using System.IO.Compression;

namespace FOS.Core;

internal static class OfficeCompressor
{
    private static readonly string[] MediaFolders = { "word/media/", "ppt/media/", "xl/media/" };

    public static (int Changed, int Total) Compress(
        string inputPath, string outputPath, Func<byte[], string, byte[]?> recompress)
    {
        int changed = 0;
        int total = 0;

        using var inputStream = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var input = new ZipArchive(inputStream, ZipArchiveMode.Read);
        using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
        using var output = new ZipArchive(outputStream, ZipArchiveMode.Create);

        foreach (var entry in input.Entries)
        {
            var target = output.CreateEntry(entry.FullName, CompressionLevel.SmallestSize);
            target.LastWriteTime = entry.LastWriteTime;

            using var source = entry.Open();
            using var destination = target.Open();

            // selain gambar, salin apa adanya
            if (!IsMedia(entry.FullName))
            {
                source.CopyTo(destination);
                continue;
            }

            total++;
            using var buffer = new MemoryStream();
            source.CopyTo(buffer);
            var original = buffer.ToArray();
            var smaller = recompress(original, Path.GetExtension(entry.FullName));

            if (smaller != null)
            {
                destination.Write(smaller);
                changed++;
            }
            else
            {
                destination.Write(original);
            }
        }

        return (changed, total);
    }

    private static bool IsMedia(string name) =>
        !name.EndsWith('/') && MediaFolders.Any(f => name.StartsWith(f, StringComparison.OrdinalIgnoreCase));
}