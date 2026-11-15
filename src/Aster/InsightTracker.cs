using System;
using System.Collections.Generic;

namespace Aster;

internal class InsightTracker
{
	public List<string> Events = new List<string>();

	private int cpuRuns;

	private int ramRuns;

	private DateTime lastCpu = DateTime.MinValue;

	private DateTime lastRam = DateTime.MinValue;

	public void Add(Snapshot s)
	{
		cpuRuns = ((s.Cpu >= 90.0) ? (cpuRuns + 1) : 0);
		ramRuns = ((s.MemoryPercent >= 85.0) ? (ramRuns + 1) : 0);
		if (cpuRuns >= 5 && (s.Time - lastCpu).TotalMinutes >= 5.0)
		{
			Events.Insert(0, s.Time.ToString("T") + " · CPU ≥90% for 5 consecutive samples. Inspect busy processes; this can be expected during heavy work.");
			lastCpu = s.Time;
			cpuRuns = 0;
		}
		if (ramRuns >= 5 && (s.Time - lastRam).TotalMinutes >= 5.0)
		{
			Events.Insert(0, s.Time.ToString("T") + " · Memory ≥85% for 5 consecutive samples. Review unneeded apps after saving work.");
			lastRam = s.Time;
			ramRuns = 0;
		}
		if (Events.Count > 100)
		{
			Events.RemoveRange(100, Events.Count - 100);
		}
	}
}
