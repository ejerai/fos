using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace FOS.App;

public partial class App : System.Windows.Application
{
    private NotifyIcon? _tray;
    private MouseHook? _hook;
    private OverlayWindow? _overlay;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        var menu = new ContextMenuStrip();
        menu.Items.Add("Keluar", null, (_, _) => ExitApp());

        _tray = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "FOS",
            Visible = true,
            ContextMenuStrip = menu
        };

        _overlay = new OverlayWindow(new[] { "Kompres", "Zip", "Salin Path", "Buka Folder" });
        _overlay.ItemClicked += label =>
        {
            Log($"Menu dipilih: {label}");
            _tray?.ShowBalloonTip(1000, "FOS", $"Dipilih: {label}", ToolTipIcon.Info);
        };

        _hook = new MouseHook();
        _hook.LeftClick += (x, y) => Dispatcher.InvokeAsync(() =>
        {
            if (_overlay is { IsVisible: true } && !_overlay.ContainsScreenPoint(x, y))
            {
                _overlay.HideOverlay();
            }
        });
        _hook.AltClick += (x, y) => Dispatcher.InvokeAsync(() => OnAltClick(x, y));
        _hook.Start();
    }

    private void OnAltClick(int x, int y)
    {
        Log($"Alt+Klik di {x},{y}");
        _overlay?.ShowAt(x, y);
    }

    private static void Log(string text)
    {
        File.AppendAllText(
            Path.Combine(Path.GetTempPath(), "fos.log"),
            $"{DateTime.Now:HH:mm:ss} {text}{Environment.NewLine}");
    }

    private void ExitApp()
    {
        _hook?.Dispose();
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