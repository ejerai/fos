using System.Text.Json;

namespace FOS.Core;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _path;
    private readonly object _lock = new();

    public FosSettings Current { get; private set; }

    public bool IsFirstRun { get; }

    public CompressProfile Profile => CompressProfile.FromStrength(Current.Strength);

    public SettingsStore(string? path = null)
    {
        _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FOS", "settings.json");
        Current = Load();
        IsFirstRun = !Current.FirstRunDone;
    }

    public void Update(Action<FosSettings> change)
    {
        lock (_lock)
        {
            change(Current);
            Current.Strength = Math.Clamp(Current.Strength, 0, 100);
            Save();
        }
    }

    private FosSettings Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                var loaded = JsonSerializer.Deserialize<FosSettings>(File.ReadAllText(_path));
                if (loaded != null)
                {
                    loaded.Strength = Math.Clamp(loaded.Strength, 0, 100);
                    return loaded;
                }
            }
        }
        catch { /* file rusak, pakai bawaan */ }

        return new FosSettings();
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, JsonSerializer.Serialize(Current, JsonOptions));
        }
        catch { /* gagal simpan, abaikan */ }
    }
}