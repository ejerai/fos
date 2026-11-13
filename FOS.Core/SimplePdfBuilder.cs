using System.Globalization;
using System.Text;

namespace FOS.Core;

// Pembuat PDF sederhana: satu gambar JPEG per halaman A4, tanpa library tambahan.
internal sealed class SimplePdfBuilder
{
    private sealed record PageImage(byte[] Jpeg, int WidthPx, int HeightPx);

    private readonly List<PageImage> _pages = new();

    public int PageCount => _pages.Count;

    public void AddJpegPage(byte[] jpeg, int widthPx, int heightPx) =>
        _pages.Add(new PageImage(jpeg, widthPx, heightPx));

    public void Save(string path)
    {
        if (_pages.Count == 0)
            throw new InvalidOperationException("Tidak ada halaman untuk disimpan");

        using var stream = File.Create(path);
        Write(stream);
    }

    private static string F(double value) =>
        value.ToString("0.####", CultureInfo.InvariantCulture);

    private void Write(Stream s)
    {
        var offsets = new List<long>();

        void Raw(string text)
        {
            var bytes = Encoding.ASCII.GetBytes(text);
            s.Write(bytes, 0, bytes.Length);
        }

        void BeginObject(int number)
        {
            while (offsets.Count < number) offsets.Add(0);
            offsets[number - 1] = s.Position;
            Raw($"{number} 0 obj\n");
        }

        void EndObject() => Raw("endobj\n");

        Raw("%PDF-1.4\n");
        s.Write(new byte[] { 0x25, 0xE2, 0xE3, 0xCF, 0xD3, 0x0A }); // komentar biner

        int count = _pages.Count;

        // Objek 1: Catalog, objek 2: daftar halaman
        BeginObject(1);
        Raw("<< /Type /Catalog /Pages 2 0 R >>\n");
        EndObject();

        BeginObject(2);
        var kids = string.Join(" ", Enumerable.Range(0, count).Select(i => $"{3 + i * 3} 0 R"));
        Raw($"<< /Type /Pages /Count {count} /Kids [{kids}] >>\n");
        EndObject();

        // Tiap halaman memakai 3 objek: halaman, isi, gambar
        for (int i = 0; i < count; i++)
        {
            var img = _pages[i];
            int pageObj = 3 + i * 3;
            int contentObj = 4 + i * 3;
            int imageObj = 5 + i * 3;

            // A4 dalam poin; orientasi mengikuti bentuk gambar
            double pageW = 595.28;
            double pageH = 841.89;
            if (img.WidthPx > img.HeightPx) (pageW, pageH) = (pageH, pageW);

            const double margin = 20;
            double scale = Math.Min((pageW - 2 * margin) / img.WidthPx,
                                    (pageH - 2 * margin) / img.HeightPx);
            double drawW = img.WidthPx * scale;
            double drawH = img.HeightPx * scale;
            double x = (pageW - drawW) / 2;
            double y = (pageH - drawH) / 2;

            BeginObject(pageObj);
            Raw($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {F(pageW)} {F(pageH)}] " +
                $"/Resources << /XObject << /Im0 {imageObj} 0 R >> >> /Contents {contentObj} 0 R >>\n");
            EndObject();

            string content = $"q {F(drawW)} 0 0 {F(drawH)} {F(x)} {F(y)} cm /Im0 Do Q\n";
            BeginObject(contentObj);
            Raw($"<< /Length {content.Length} >>\nstream\n{content}endstream\n");
            EndObject();

            BeginObject(imageObj);
            Raw($"<< /Type /XObject /Subtype /Image /Width {img.WidthPx} /Height {img.HeightPx} " +
                $"/ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length {img.Jpeg.Length} >>\nstream\n");
            s.Write(img.Jpeg, 0, img.Jpeg.Length);
            Raw("\nendstream\n");
            EndObject();
        }

        long xrefPosition = s.Position;
        int total = 2 + count * 3;
        Raw($"xref\n0 {total + 1}\n");
        Raw("0000000000 65535 f \n");
        for (int n = 1; n <= total; n++)
        {
            Raw($"{offsets[n - 1]:D10} 00000 n \n");
        }
        Raw($"trailer\n<< /Size {total + 1} /Root 1 0 R >>\nstartxref\n{xrefPosition}\n%%EOF\n");
    }
}