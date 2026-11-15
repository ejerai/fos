namespace FOS.Core;

public sealed class FosSettings
{
    public int Strength { get; set; } = CompressPresets.Balanced;
    public bool FirstRunDone { get; set; }
}