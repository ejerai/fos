namespace FOS.Core;

public sealed record CompressLevel(int Quality, double Scale);

public static class CompressLevels
{
    // dari paling ringan ke paling kuat
    public static readonly IReadOnlyList<CompressLevel> All = new CompressLevel[]
    {
        new(94, 1.00), new(90, 1.00), new(86, 1.00), new(82, 1.00),
        new(78, 1.00), new(74, 1.00), new(70, 1.00), new(66, 1.00),
        new(62, 1.00), new(58, 1.00), new(54, 1.00), new(50, 1.00),
        new(50, 0.92), new(48, 0.85), new(46, 0.78), new(44, 0.72),
        new(42, 0.66), new(40, 0.60), new(38, 0.55), new(36, 0.50)
    };
}