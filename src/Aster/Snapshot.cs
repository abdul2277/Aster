using System;
using System.Collections.Generic;

namespace Aster;

internal class Snapshot
{
	public double Cpu;

	public double MemoryPercent;

	public double Receive;

	public double Send;

	public double DiskRead;

	public double DiskWrite;

	public double Gpu;

	public ulong Total;

	public ulong Available;

	public List<Proc> Processes = new List<Proc>();

	public DateTime Time = DateTime.Now;

	public string Warning = "";
}
