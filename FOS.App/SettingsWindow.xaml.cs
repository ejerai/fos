using System.Windows;
using System.Windows.Media;
using FOS.Core;

namespace FOS.App;

public partial class SettingsWindow : Window
{
    private static readonly SolidColorBrush Normal = MakeBrush(0x2C, 0x2C, 0x34);
    private static readonly SolidColorBrush Selected = MakeBrush(0x4A, 0x3A, 0xB8);

    private readonly SettingsStore _store;
    private bool _loading = true;

    public SettingsWindow(SettingsStore store, bool firstRun)
    {
        InitializeComponent();
        _store = store;

        TargetSlider.Minimum = SettingsStore.MinTarget;
        TargetSlider.Maximum = SettingsStore.MaxTarget;

        LightButton.Tag = CompressPresets.Light;
        BalancedButton.Tag = CompressPresets.Balanced;
        StrongButton.Tag = CompressPresets.Strong;
        LightButton.Content = $"Ringan {CompressPresets.Light}%";
        BalancedButton.Content = $"Seimbang {CompressPresets.Balanced}%";
        StrongButton.Content = $"Hemat {CompressPresets.Strong}%";

        if (firstRun)
        {
            TitleText.Text = "Selamat datang di FOS";
            SubtitleText.Text = "Tahan Alt lalu klik file di Explorer untuk membuka menu. Pilih dulu berapa persen file dikecilkan.";
        }

        TargetSlider.Value = store.Current.TargetPercent;
        _loading = false;
        Refresh();
    }

    private static SolidColorBrush MakeBrush(byte r, byte g, byte b) =>
        new(System.Windows.Media.Color.FromRgb(r, g, b));

    private void Refresh()
    {
        int target = (int)TargetSlider.Value;

        TargetText.Text = $"Target hemat: {target}%";
        DetailText.Text = $"FOS memilih kualitas dan ukuran gambar sendiri supaya file mengecil sekitar {target}%.";

        Mark(LightButton, target == CompressPresets.Light);
        Mark(BalancedButton, target == CompressPresets.Balanced);
        Mark(StrongButton, target == CompressPresets.Strong);
    }

    private static void Mark(System.Windows.Controls.Button button, bool on) =>
        button.Background = on ? Selected : Normal;

    private void OnPresetClick(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button { Tag: int value })
            TargetSlider.Value = value;
    }

    private void OnTargetChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_loading) return;
        Refresh();
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        _store.Update(s => s.TargetPercent = (int)TargetSlider.Value);
        Close();
    }
}