namespace FOS.Core;

public static class CompressPresets
{
    public const int Light = 25;
    public const int Balanced = 50;
    public const int Strong = 75;
}

public sealed class FosSettings
{
    public int TargetPercent { get; set; } = CompressPresets.Balanced;
    public bool FirstRunDone { get; set; }
}