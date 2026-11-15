namespace FOS.Core;

public static class CompressPresets
{
    public const int Light = 20;
    public const int Balanced = 40;
    public const int Strong = 70;
}

public sealed record CompressProfile(int Quality, int MaxSide)
{
    public static CompressProfile FromStrength(int strength)
    {
        int s = Math.Clamp(strength, 0, 100);
        int quality = (int)Math.Round(92 - s * 0.6);
        int maxSide = (int)Math.Round(2800 - s * 18.0);
        return new CompressProfile(quality, maxSide);
    }
}