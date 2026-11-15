using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Windows.Threading;
using FOS.Core;
using FOS.Core.Actions;

namespace FOS.App;

public partial class App : System.Windows.Application
{
    private NotifyIcon? _tray;
    private MouseHook? _hook;
    private OverlayWindow? _overlay;
    private DispatcherTimer? _focusWatch;

    private readonly ExplorerReader _reader = new();
    private readonly ActionRegistry _registry = new();
    private IReadOnlyList<IFileAction> _currentActions = Array.Empty<IFileAction>();
    private IReadOnlyList<string> _currentPaths = Array.Empty<string>();
    private IntPtr _targetWindow = IntPtr.Zero;
    private int _clickVersion;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        _registry.Register(new CompressImageAction());
        _registry.Register(new CompressOfficeAction());
        _registry.Register(new ConvertImageAction("png"));
        _registry.Register(new ConvertImageAction("jpg"));
        _registry.Register(new ConvertImageAction("webp"));
        _registry.Register(new ImageToPdfAction());
        _registry.Register(new CopyPathAction());
        _registry.Register(new OpenFolderAction());

        var menu = new ContextMenuStrip();
        menu.Items.Add("Keluar", null, (_, _) => ExitApp());

        _tray = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "FOS",
            Visible = true,
            ContextMenuStrip = menu
        };

        _overlay = new OverlayWindow();
        _overlay.ItemClicked += RunAction;

        _hook = new MouseHook();

        // Alt + klik hanya ditangani di jendela Explorer, atau di dalam lingkaran FOS
        // sendiri (supaya bisa dipindahkan). Di aplikasi lain dibiarkan seperti biasa.
        _hook.AltClickFilter = (x, y) =>
            (_overlay is { IsVisible: true } && _overlay.ContainsScreenPoint(x, y))
            || ExplorerSelection.IsExplorerAt(x, y);

        _hook.LeftClick += (x, y) => Dispatcher.InvokeAsync(() =>
        {
            if (_overlay is { IsVisible: true } && !_overlay.ContainsScreenPoint(x, y))
            {
                _overlay.HideOverlay();
            }
        });
        _hook.AltClick += (x, y) => Dispatcher.InvokeAsync(() => OnAltClick(x, y));
        _hook.Start();

        // Tutup lingkaran kalau jendela Explorer tidak lagi aktif
        _focusWatch = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _focusWatch.Tick += (_, _) =>
        {
            if (_overlay is { IsVisible: true }
                && _targetWindow != IntPtr.Zero
                && ExplorerSelection.GetForegroundRoot() != _targetWindow)
            {
                _overlay.HideOverlay();
            }
        };
        _focusWatch.Start();
    }

    // Alt + klik pada file yang sudah ada di dalam pilihan banyak file: Explorer
    // menciutkan pilihan menjadi satu file saat tombol mouse dilepas.
    // Kalau bacaan akhir hanyalah salah satu file dari bacaan awal, pakai bacaan awal.
    private static string[] Choose(string[] early, string[] late)
    {
        if (late.Length == 0) return early;

        bool lateInsideEarly = late.All(p => early.Contains(p, StringComparer.OrdinalIgnoreCase));
        return early.Length > late.Length && lateInsideEarly ? early : late;
    }

    private async void OnAltClick(int x, int y)
    {
        int version = ++_clickVersion;

        // Tutup overlay lama, lalu tangkap jendela di bawah kursor
        // SEBELUM overlay baru muncul
        _overlay?.HideOverlay();
        var target = ExplorerSelection.GetRootWindowAt(x, y);

        // Bacaan awal dimulai SEKARANG, sebelum Explorer sempat menciutkan
        // pilihan banyak file menjadi satu
        var earlyTask = _reader.ReadAsync(target);

        _targetWindow = IntPtr.Zero;
        _currentActions = Array.Empty<IFileAction>();
        _currentPaths = Array.Empty<string>();
        _overlay?.ShowAt(x, y);

        // Beri waktu Explorer memperbarui pilihan, lalu baca lagi
        await Task.Delay(150);
        if (version != _clickVersion) return;

        var lateTask = _reader.ReadAsync(target);
        var early = await earlyTask;
        var late = await lateTask;
        if (version != _clickVersion) return;

        var paths = Choose(early.Paths, late.Paths);

        // Hanya jumlah dan waktu yang dicatat, bukan nama file (menjaga janji privasi)
        Log($"Alt+Klik di {x},{y}: bacaan awal {early.Paths.Length} item ({early.Ms} ms), " +
            $"bacaan akhir {late.Paths.Length} item ({late.Ms} ms), dipakai {paths.Length}");

        // Tidak ada file terpilih (misalnya klik di ruang kosong): tutup lingkaran
        if (paths.Length == 0)
        {
            _overlay?.HideOverlay();
            return;
        }

        _targetWindow = target;
        _currentPaths = paths;
        _currentActions = _registry.GetFor(paths);
        _overlay?.SetFile(paths[0], paths.Length);
        _overlay?.SetItems(_currentActions.Select(a => a.Label).ToList());
    }

    private async void RunAction(int index)
    {
        if (index < 0 || index >= _currentActions.Count) return;

        var action = _currentActions[index];
        var paths = _currentPaths;
        Log($"Aksi dijalankan: {action.Label} ({paths.Count} item)");

        try
        {
            var result = await action.RunAsync(paths);
            _tray?.ShowBalloonTip(1500, "FOS", result.Message,
                result.Success ? ToolTipIcon.Info : ToolTipIcon.Warning);
        }
        catch (Exception ex)
        {
            _tray?.ShowBalloonTip(2000, "FOS", "Gagal: " + ex.Message, ToolTipIcon.Error);
        }
    }

    private static void Log(string text)
    {
        File.AppendAllText(
            Path.Combine(Path.GetTempPath(), "fos.log"),
            $"{DateTime.Now:HH:mm:ss} {text}{Environment.NewLine}");
    }

    private void ExitApp()
    {
        _focusWatch?.Stop();
        _hook?.Dispose();
        _reader.Dispose();
        _overlay?.Close();
        if (_tray != null)
        {
            _tray.Visible = false;
            _tray.Dispose();
        }
        Shutdown();
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        _hook?.Dispose();
        _tray?.Dispose();
        base.OnExit(e);
    }
}