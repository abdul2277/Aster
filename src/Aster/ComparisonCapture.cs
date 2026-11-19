using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aster;

internal class ComparisonCapture
{
	public ComparisonWindow Before;

	public ComparisonWindow After;

	private ComparisonWindow pending;

	private bool captureBefore;

	public string State = "Capture a before window to begin.";

	public bool Active => pending != null;

	public void Start(bool before)
	{
		if (!before && Before == null)
		{
			throw new InvalidOperationException("Capture Before first.");
		}
		if (Active)
		{
			throw new InvalidOperationException("A capture is already running.");
		}
		captureBefore = before;
		pending = new ComparisonWindow();
		State = "Collecting 0 / 20 samples…";
	}

	public void Cancel(string reason)
	{
		pending = null;
		State = reason;
	}

	public bool Add(Snapshot s, double maxGap)
	{
		if (pending == null)
		{
			return false;
		}
		List<ComparisonReading> readings = pending.Readings;
		if (readings.Count > 0 && (s.Time <= readings.Last().Time || (s.Time - readings.Last().Time).TotalSeconds > maxGap))
		{
			Cancel("Capture stopped: samples were interrupted. Retry when monitoring is steady.");
			return false;
		}
		if (double.IsNaN(s.Cpu) || double.IsInfinity(s.Cpu) || double.IsNaN(s.MemoryPercent) || double.IsInfinity(s.MemoryPercent))
		{
			Cancel("Capture stopped: a resource reading is unavailable.");
			return false;
		}
		readings.Add(new ComparisonReading
		{
			Time = s.Time,
			Cpu = s.Cpu,
			Ram = s.MemoryPercent,
			Processes = s.Processes.Count
		});
		State = "Collecting " + readings.Count + " / 20 samples…";
		if (readings.Count < 20)
		{
			return false;
		}
		if (captureBefore)
		{
			Before = pending;
			After = null;
		}
		else
		{
			After = pending;
		}
		pending = null;
		State = (captureBefore ? "Before" : "After") + " window captured · 20 samples.";
		return true;
	}

	public string Report()
	{
		StringBuilder stringBuilder = new StringBuilder("ASTER PERFORMANCE COMPARISON\r\n\r\nBEFORE\r\n" + ((Before == null) ? "Not captured" : Before.Describe()) + "\r\n\r\nAFTER\r\n" + ((After == null) ? "Not captured" : After.Describe()));
		if (Before != null && After != null)
		{
			stringBuilder.AppendLine("\r\n\r\nAFTER MINUS BEFORE");
			stringBuilder.AppendLine("Average CPU: " + (After.Readings.Average((ComparisonReading x) => x.Cpu) - Before.Readings.Average((ComparisonReading x) => x.Cpu)).ToString("+0.0;-0.0;0.0") + " percentage points");
			stringBuilder.AppendLine("Average RAM: " + (After.Readings.Average((ComparisonReading x) => x.Ram) - Before.Readings.Average((ComparisonReading x) => x.Ram)).ToString("+0.0;-0.0;0.0") + " percentage points");
		}
		stringBuilder.AppendLine("\r\nCompare similar workloads. Differences do not prove that a change caused an improvement. Aster does not modify Windows during capture.");
		return stringBuilder.ToString();
	}
}
