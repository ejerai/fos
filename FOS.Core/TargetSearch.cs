namespace FOS.Core;

public static class TargetSearch
{
    // level paling ringan yang ukurannya mencapai target
    public static int FindLevel(int levels, long goalSize, Func<int, long> sizeAt)
    {
        int low = 0;
        int high = levels - 1;
        int best = high;

        while (low <= high)
        {
            int mid = (low + high) / 2;
            if (sizeAt(mid) <= goalSize)
            {
                best = mid;
                high = mid - 1;
            }
            else
            {
                low = mid + 1;
            }
        }

        return best;
    }
}