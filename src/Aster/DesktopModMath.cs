using System;

namespace Aster;

internal static class DesktopModMath
{
	public static int UsedPercent(long total, long free)
	{
		if (total <= 0)
		{
			return 0;
		}
		return (int)Math.Max(0.0, Math.Min(100.0, 100.0 * (1.0 - (double)Math.Max(0L, Math.Min(total, free)) / (double)total)));
	}
}
