using System.Runtime.InteropServices;

namespace FOS.App;

public static class ExplorerSelection
{
    private const uint GA_ROOT = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(POINT pt);

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);

    // Mengembalikan path file/folder yang terpilih di jendela Explorer
    // yang ada di bawah titik (x, y). Kosong kalau bukan jendela Explorer.
    public static string[] GetSelectedPaths(int x, int y)
    {
        var result = new List<string>();
        try
        {
            IntPtr hwnd = WindowFromPoint(new POINT { X = x, Y = y });
            if (hwnd == IntPtr.Zero) return Array.Empty<string>();
            hwnd = GetAncestor(hwnd, GA_ROOT);

            var shellType = Type.GetTypeFromProgID("Shell.Application");
            if (shellType == null) return Array.Empty<string>();

            dynamic shell = Activator.CreateInstance(shellType)!;
            dynamic windows = shell.Windows();
            int count = windows.Count;

            for (int i = 0; i < count; i++)
            {
                try
                {
                    dynamic window = windows.Item(i);
                    if (window == null) continue;
                    if ((long)window.HWND != hwnd.ToInt64()) continue;

                    dynamic items = window.Document.SelectedItems();
                    int n = items.Count;
                    for (int j = 0; j < n; j++)
                    {
                        result.Add((string)items.Item(j).Path);
                    }
                    if (result.Count > 0) break;
                }
                catch
                {
                    // Abaikan jendela yang bukan jendela folder
                }
            }
        }
        catch
        {
            // Kalau gagal, anggap tidak ada file terpilih
        }
        return result.ToArray();
    }
}