using System.Drawing;
using System.Windows.Forms;

namespace FOS.App;

public partial class App : System.Windows.Application
{
    private NotifyIcon? _tray;

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
    }

    private void ExitApp()
    {
        if (_tray != null)
        {
            _tray.Visible = false;
            _tray.Dispose();
        }
        Shutdown();
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        _tray?.Dispose();
        base.OnExit(e);
    }
}