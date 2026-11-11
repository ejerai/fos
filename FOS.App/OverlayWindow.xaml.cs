using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace FOS.App;

public partial class OverlayWindow : Window
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOACTIVATE = 0x0010;
    private static readonly IntPtr HWND_TOPMOST = new(-1);

    private const double Size = 300;
    private const double DiscSize = 270;
    private const double RingRadius = 95;
    private const double ItemSize = 68;
    private const double CenterSize = 70;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    private readonly SolidColorBrush _normal = MakeBrush(0xFF, 0x2C, 0x2C, 0x34);
    private readonly SolidColorBrush _hover = MakeBrush(0xFF, 0x4A, 0x3A, 0xB8);
    private readonly SolidColorBrush _accent = MakeBrush(0xFF, 0x7C, 0x6C, 0xFF);
    private readonly SolidColorBrush _white = MakeBrush(0xFF, 0xFF, 0xFF, 0xFF);

    private readonly DispatcherTimer _autoClose;
    private readonly List<UIElement> _itemElements = new();
    private TextBlock _centerText = null!;

    // Mengirim nomor urut tombol yang diklik
    public event Action<int>? ItemClicked;

    public OverlayWindow()
    {
        InitializeComponent();
        BuildStatic();

        _autoClose = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _autoClose.Tick += (_, _) => HideOverlay();

        // Supaya overlay tidak mencuri fokus dari Explorer
        var hwnd = new WindowInteropHelper(this).EnsureHandle();
        int style = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, style | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW);
    }

    private static SolidColorBrush MakeBrush(byte a, byte r, byte g, byte b) =>
        new(System.Windows.Media.Color.FromArgb(a, r, g, b));

    // Bagian yang selalu ada: lingkaran latar dan lingkaran tengah
    private void BuildStatic()
    {
        double c = Size / 2;

        var disc = new Ellipse
        {
            Width = DiscSize,
            Height = DiscSize,
            Fill = MakeBrush(0xC8, 0x12, 0x12, 0x18),
            Stroke = MakeBrush(0x40, 0xFF, 0xFF, 0xFF),
            StrokeThickness = 1
        };
        Canvas.SetLeft(disc, c - DiscSize / 2);
        Canvas.SetTop(disc, c - DiscSize / 2);
        Ring.Children.Add(disc);

        _centerText = new TextBlock
        {
            Text = "FOS",
            Foreground = _white,
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            VerticalAlignment = System.Windows.VerticalAlignment.Center
        };
        var center = new Border
        {
            Width = CenterSize,
            Height = CenterSize,
            CornerRadius = new CornerRadius(CenterSize / 2),
            Background = _accent,
            Child = _centerText
        };
        Canvas.SetLeft(center, c - CenterSize / 2);
        Canvas.SetTop(center, c - CenterSize / 2);
        Ring.Children.Add(center);
    }

    // Mengganti tombol-tombol di sekeliling sesuai aksi yang tersedia
    public void SetItems(IReadOnlyList<string> labels)
    {
        foreach (var element in _itemElements) Ring.Children.Remove(element);
        _itemElements.Clear();

        double c = Size / 2;

        for (int i = 0; i < labels.Count; i++)
        {
            int index = i;
            double angle = (-90 + i * 360.0 / labels.Count) * Math.PI / 180.0;
            double x = c + RingRadius * Math.Cos(angle) - ItemSize / 2;
            double y = c + RingRadius * Math.Sin(angle) - ItemSize / 2;

            var item = new Border
            {
                Width = ItemSize,
                Height = ItemSize,
                CornerRadius = new CornerRadius(ItemSize / 2),
                Background = _normal,
                BorderBrush = _accent,
                BorderThickness = new Thickness(1.5),
                Cursor = System.Windows.Input.Cursors.Hand,
                Opacity = 0,
                Child = new TextBlock
                {
                    Text = labels[i],
                    Foreground = _white,
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    VerticalAlignment = System.Windows.VerticalAlignment.Center
                }
            };

            item.MouseEnter += (_, _) => item.Background = _hover;
            item.MouseLeave += (_, _) => item.Background = _normal;
            item.MouseLeftButtonUp += (_, _) =>
            {
                ItemClicked?.Invoke(index);
                HideOverlay();
            };

            Canvas.SetLeft(item, x);
            Canvas.SetTop(item, y);
            Ring.Children.Add(item);
            _itemElements.Add(item);

            // Muncul satu per satu
            item.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180))
            {
                BeginTime = TimeSpan.FromMilliseconds(40 * i)
            });
        }
    }

    // Mengubah tulisan di lingkaran tengah sesuai file yang terpilih
    public void SetFile(string? path, int count)
    {
        if (path == null) { _centerText.Text = "FOS"; return; }
        if (count > 1) { _centerText.Text = count + " file"; return; }

        var ext = System.IO.Path.GetExtension(path).TrimStart('.').ToUpperInvariant();
        _centerText.Text = ext.Length > 0 ? ext : "DIR";
    }

    // x, y = posisi kursor di layar (piksel)
    public void ShowAt(int x, int y)
    {
        SetFile(null, 0);
        SetItems(Array.Empty<string>());

        BeginAnimation(OpacityProperty, null);
        Opacity = 0;

        if (!IsVisible) Show();

        var hwnd = new WindowInteropHelper(this).Handle;
        var dpi = VisualTreeHelper.GetDpi(this);
        int sizePx = (int)Math.Round(Width * dpi.DpiScaleX);

        // Jaga agar lingkaran tidak terpotong di tepi layar
        var area = System.Windows.Forms.Screen
            .FromPoint(new System.Drawing.Point(x, y)).WorkingArea;
        int left = Math.Clamp(x - sizePx / 2, area.Left, Math.Max(area.Left, area.Right - sizePx));
        int top = Math.Clamp(y - sizePx / 2, area.Top, Math.Max(area.Top, area.Bottom - sizePx));
        SetWindowPos(hwnd, HWND_TOPMOST, left, top, 0, 0, SWP_NOSIZE | SWP_NOACTIVATE);

        var scale = new DoubleAnimation(0.6, 1.0, TimeSpan.FromMilliseconds(260))
        {
            EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.4 }
        };
        RootScale.BeginAnimation(ScaleTransform.ScaleXProperty, scale);
        RootScale.BeginAnimation(ScaleTransform.ScaleYProperty, scale);
        BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(160)));

        _autoClose.Stop();
        _autoClose.Start();
    }

    public void HideOverlay()
    {
        _autoClose.Stop();
        if (IsVisible) Hide();
    }

    public bool ContainsScreenPoint(int x, int y)
    {
        if (!IsVisible) return false;
        var hwnd = new WindowInteropHelper(this).Handle;
        if (!GetWindowRect(hwnd, out var r)) return false;
        return x >= r.Left && x < r.Right && y >= r.Top && y < r.Bottom;
    }
}