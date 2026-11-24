namespace FOS.Core;

internal static class CompressReport
{
    public static string Sizes(long before, long after, int target)
    {
        int percent = (int)Math.Round(100.0 * (before - after) / before);
        string text = $"{FileNaming.FormatSize(before)} → {FileNaming.FormatSize(after)}, hemat {percent}%";
        return percent < target - 3 ? text + $", target {target}% tidak tercapai" : text;
    }
}