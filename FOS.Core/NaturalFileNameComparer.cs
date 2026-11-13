using System.Runtime.InteropServices;

namespace FOS.Core;

// Urutan nama file yang sama dengan Windows Explorer
// (angka dibaca sebagai angka: 2 sebelum 10, 23 sebelum 227)
internal sealed class NaturalFileNameComparer : IComparer<string>
{
    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int StrCmpLogicalW(string x, string y);

    public int Compare(string? x, string? y) => StrCmpLogicalW(x ?? "", y ?? "");
}