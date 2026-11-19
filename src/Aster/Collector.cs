using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Management;
using System.Net.NetworkInformation;

namespace Aster;

internal class Collector
{
	private ulong idle;

	private ulong kernel;

	private ulong user;

	private bool primed;

	private long stamp;

	private Dictionary<int, Tuple<double, long>> previous = new Dictionary<int, Tuple<double, long>>();

	private Dictionary<string, Tuple<long, long>> network = new Dictionary<string, Tuple<long, long>>();

	public Snapshot Sample()
	{
		Snapshot snapshot = new Snapshot();
		long timestamp = Stopwatch.GetTimestamp();
		double num = ((stamp == 0) ? 0.0 : ((double)(timestamp - stamp) / (double)Stopwatch.Frequency));
		stamp = timestamp;
		if (Native.GetSystemTimes(out var times, out var times2, out var times3))
		{
			ulong num2 = times2.Value + times3.Value;
			ulong num3 = kernel + user;
			if (primed && num2 > num3)
			{
				snapshot.Cpu = Math.Max(0.0, Math.Min(100.0, 100.0 * (1.0 - (double)(times.Value - idle) / (double)(num2 - num3))));
			}
			idle = times.Value;
			kernel = times2.Value;
			user = times3.Value;
			primed = true;
		}
		Native.Memory memory = new Native.Memory();
		if (!Native.GlobalMemoryStatusEx(memory))
		{
			throw new Win32Exception();
		}
		snapshot.Total = memory.Total;
		snapshot.Available = memory.Available;
		snapshot.MemoryPercent = 100.0 * (double)(memory.Total - memory.Available) / (double)memory.Total;
		Dictionary<int, Tuple<double, long>> dictionary = new Dictionary<int, Tuple<double, long>>();
		Process[] processes = Process.GetProcesses();
		foreach (Process process in processes)
		{
			using (process)
			{
				try
				{
					Proc proc = new Proc();
					proc.Id = process.Id;
					proc.Name = process.ProcessName;
					proc.Memory = process.WorkingSet64;
					proc.Session = process.SessionId;
					Proc proc2 = proc;
					double num4 = 0.0;
					try
					{
						num4 = process.TotalProcessorTime.TotalSeconds;
						proc2.Start = process.StartTime.ToUniversalTime().Ticks;
						if (num > 0.0 && previous.TryGetValue(process.Id, out var value) && value.Item2 == proc2.Start)
						{
							proc2.Cpu = Math.Max(0.0, Math.Min(100.0, (num4 - value.Item1) / num / (double)Environment.ProcessorCount * 100.0));
						}
						dictionary[process.Id] = Tuple.Create(num4, proc2.Start);
					}
					catch
					{
					}
					try
					{
						proc2.Threads = process.Threads.Count;
						proc2.Handles = process.HandleCount;
					}
					catch
					{
					}
					snapshot.Processes.Add(proc2);
				}
				catch
				{
				}
			}
		}
		previous = dictionary;
		Dictionary<string, Tuple<long, long>> dictionary2 = new Dictionary<string, Tuple<long, long>>();
		NetworkInterface[] allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
		foreach (NetworkInterface networkInterface in allNetworkInterfaces)
		{
			try
			{
				if (networkInterface.OperationalStatus == OperationalStatus.Up && networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
				{
					IPv4InterfaceStatistics iPv4Statistics = networkInterface.GetIPv4Statistics();
					if (num > 0.0 && network.TryGetValue(networkInterface.Id, out var value2))
					{
						snapshot.Receive += (double)Math.Max(0L, iPv4Statistics.BytesReceived - value2.Item1) / num;
						snapshot.Send += (double)Math.Max(0L, iPv4Statistics.BytesSent - value2.Item2) / num;
					}
					dictionary2[networkInterface.Id] = Tuple.Create(iPv4Statistics.BytesReceived, iPv4Statistics.BytesSent);
				}
			}
			catch
			{
			}
		}
		network = dictionary2;
		snapshot.DiskRead = double.NaN;
		snapshot.DiskWrite = double.NaN;
		snapshot.Gpu = double.NaN;
		try
		{
			using ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT DiskReadBytesPersec,DiskWriteBytesPersec FROM Win32_PerfFormattedData_PerfDisk_PhysicalDisk WHERE Name='_Total'");
			foreach (ManagementObject item in managementObjectSearcher.Get())
			{
				using (item)
				{
					snapshot.DiskRead = Convert.ToDouble(item["DiskReadBytesPersec"]);
					snapshot.DiskWrite = Convert.ToDouble(item["DiskWriteBytesPersec"]);
				}
			}
		}
		catch
		{
			snapshot.Warning = "Disk counters unavailable";
		}
		return snapshot;
	}
}
