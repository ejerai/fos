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

    private readonly DispatcherTimer _autoClose;

    public event Action<string>? ItemClicked;

    public OverlayWindow(IReadOnlyList<string> labels)
    {
        InitializeComponent();
        BuildRing(labels);

        _autoClose = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _autoClose.Tick += (_, _) => HideOverlay();

        // Supaya overlay tidak mencuri fokus dari Explorer
        var hwnd = new WindowInteropHelper(this).EnsureHandle();
        int style = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, style | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW);
    }

    private static SolidColorBrush MakeBrush(byte a, byte r, byte g, byte b) =>
        new(System.Windows.Media.Color.FromArgb(a, r, g, b));

    private void BuildRing(IReadOnlyList<string> labels)
    {
        const double size = 300;
        const double discSize = 270;
        const double ringRadius = 95;
        const double itemSize = 68;
        const double centerSize = 70;
        double c = size / 2;

        var normal = MakeBrush(0xFF, 0x2C, 0x2C, 0x34);
        var hover = MakeBrush(0xFF, 0x4A, 0x3A, 0xB8);
        var accent = MakeBrush(0xFF, 0x7C, 0x6C, 0xFF);
        var white = MakeBrush(0xFF, 0xFF, 0xFF, 0xFF);

        // Lingkaran latar
        var disc = new Ellipse
        {
            Width = discSize,
            Height = discSize,
            Fill = MakeBrush(0xC8, 0x12, 0x12, 0x18),
            Stroke = MakeBrush(0x40, 0xFF, 0xFF, 0xFF),
            StrokeThickness = 1
        };
        Canvas.SetLeft(disc, c - discSize / 2);
        Canvas.SetTop(disc, c - discSize / 2);
        Ring.Children.Add(disc);

        // Lingkaran tengah
        var center = new Border
        {
            Width = centerSize,
            Height = centerSize,
            CornerRadius = new CornerRadius(centerSize / 2),
            Background = accent,
            Child = new TextBlock
            {
                Text = "FOS",
                Foreground = white,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            }
        };
        Canvas.SetLeft(center, c - centerSize / 2);
        Canvas.SetTop(center, c - centerSize / 2);
        Ring.Children.Add(center);

        // Item di sekeliling, mulai dari atas, searah jarum jam
        for (int i = 0; i < labels.Count; i++)
        {
            double angle = (-90 + i * 360.0 / labels.Count) * Math.PI / 180.0;
            double x = c + ringRadius * Math.Cos(angle) - itemSize / 2;
            double y = c + ringRadius * Math.Sin(angle) - itemSize / 2;
            string label = labels[i];

            var item = new Border
            {
                Width = itemSize,
                Height = itemSize,
                CornerRadius = new CornerRadius(itemSize / 2),
                Background = normal,
                BorderBrush = accent,
                BorderThickness = new Thickness(1.5),
                Cursor = System.Windows.Input.Cursors.Hand,
                Child = new TextBlock
                {
                    Text = label,
                    Foreground = white,
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    VerticalAlignment = System.Windows.VerticalAlignment.Center
                }
            };

            item.MouseEnter += (_, _) => item.Background = hover;
            item.MouseLeave += (_, _) => item.Background = normal;
            item.MouseLeftButtonUp += (_, _) =>
            {
                ItemClicked?.Invoke(label);
                HideOverlay();
            };

            Canvas.SetLeft(item, x);
            Canvas.SetTop(item, y);
            Ring.Children.Add(item);
        }
    }

    // x, y = posisi kursor di layar (piksel)
    public void ShowAt(int x, int y)
    {
        BeginAnimation(OpacityProperty, null);
        Opacity = 0;

        if (!IsVisible) Show();

        var hwnd = new WindowInteropHelper(this).Handle;
        var dpi = VisualTreeHelper.GetDpi(this);
        int sizePx = (int)Math.Round(Width * dpi.DpiScaleX);
        SetWindowPos(hwnd, HWND_TOPMOST, x - sizePx / 2, y - sizePx / 2, 0, 0, SWP_NOSIZE | SWP_NOACTIVATE);

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