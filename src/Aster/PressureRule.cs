using System;

namespace Aster;

internal class PressureRule
{
	private DateTime? since;

	private DateTime? previous;

	private DateTime? lastAlert;

	public void Reset()
	{
		since = null;
		previous = null;
		lastAlert = null;
	}

	public bool Observe(double value, double threshold, int seconds, int cooldown, DateTime now, double maxGap)
	{
		if (previous.HasValue && (now <= previous.Value || (now - previous.Value).TotalSeconds > maxGap))
		{
			since = null;
		}
		if (previous.HasValue && now < previous.Value)
		{
			lastAlert = null;
		}
		previous = now;
		if (double.IsNaN(value) || double.IsInfinity(value) || value < threshold)
		{
			since = null;
			return false;
		}
		if (!since.HasValue)
		{
			since = now;
		}
		if ((now - since.Value).TotalSeconds < (double)seconds)
		{
			return false;
		}
		if (lastAlert.HasValue && (now - lastAlert.Value).TotalSeconds < (double)cooldown)
		{
			return false;
		}
		lastAlert = now;
		since = now;
		return true;
	}
}
