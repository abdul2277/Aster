using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using Microsoft.Win32;

namespace Aster;

internal static class Diagnose
{
	public static Diagnosis Collect(CancellationToken token)
	{
		Diagnosis diagnosis = new Diagnosis();
		Collector collector = new Collector();
		collector.Sample();
		List<Snapshot> list = new List<Snapshot>();
		for (int i = 0; i < 3; i++)
		{
			token.ThrowIfCancellationRequested();
			Thread.Sleep(650);
			list.Add(collector.Sample());
		}
		diagnosis.Cpu = list.Average((Snapshot s) => s.Cpu);
		diagnosis.Ram = list.Average((Snapshot s) => s.MemoryPercent);
		Snapshot snapshot = list.Last();
		diagnosis.Facts.Add("CPU average " + diagnosis.Cpu.ToString("F1") + "%; memory " + diagnosis.Ram.ToString("F1") + "% across 3 samples.");
		diagnosis.Facts.Add("RAM available " + Inventory.Bytes(snapshot.Available) + " / " + Inventory.Bytes(snapshot.Total) + "; " + snapshot.Processes.Count + " visible processes.");
		diagnosis.Facts.Add("Top sampled CPU processes: " + string.Join(", ", from p in snapshot.Processes.OrderByDescending((Proc p) => p.Cpu).Take(5)
			select p.Name + " " + p.Cpu.ToString("F1") + "%"));
		try
		{
			DriveInfo driveInfo = new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory));
			diagnosis.FreeGb = (double)driveInfo.AvailableFreeSpace / 1073741824.0;
			diagnosis.FreePercent = (double)driveInfo.AvailableFreeSpace * 100.0 / (double)driveInfo.TotalSize;
			diagnosis.Facts.Add("Windows drive free: " + diagnosis.FreeGb.ToString("F1") + " GB (" + diagnosis.FreePercent.ToString("F1") + "%).");
		}
		catch (Exception ex)
		{
			diagnosis.FreeGb = (diagnosis.FreePercent = double.NaN);
			diagnosis.Limits.Add("Drive check unavailable: " + ex.Message);
		}
		try
		{
			diagnosis.Network = NetworkInterface.GetIsNetworkAvailable();
			diagnosis.NetworkKnown = true;
			NetworkInterface[] array = (from n in NetworkInterface.GetAllNetworkInterfaces()
				where n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback
				select n).ToArray();
			diagnosis.Facts.Add(array.Length + " active network adapters. This is not an internet or DNS connectivity test.");
		}
		catch (Exception ex2)
		{
			diagnosis.Limits.Add("Network inventory unavailable: " + ex2.Message);
		}
		try
		{
			using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Component Based Servicing\\RebootPending"))
			{
				diagnosis.Reboot = registryKey != null;
			}
			using (RegistryKey registryKey2 = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\WindowsUpdate\\Auto Update\\RebootRequired"))
			{
				diagnosis.Reboot |= registryKey2 != null;
			}
			diagnosis.Facts.Add("Servicing / Windows Update restart flag: " + (diagnosis.Reboot ? "present" : "not present") + ". Other restart reasons are not checked.");
		}
		catch (Exception ex3)
		{
			diagnosis.Limits.Add("Restart-state check unavailable: " + ex3.Message);
		}
		string[] array2 = new string[2] { "System", "Application" };
		foreach (string text in array2)
		{
			token.ThrowIfCancellationRequested();
			try
			{
				EventLogQuery eventLogQuery = new EventLogQuery(text, PathType.LogName, "*[System[(Level=1 or Level=2) and TimeCreated[timediff(@SystemTime) <= 86400000]]]");
				eventLogQuery.ReverseDirection = true;
				EventLogQuery eventQuery = eventLogQuery;
				using EventLogReader eventLogReader = new EventLogReader(eventQuery);
				int num2 = 0;
				EventRecord eventRecord;
				while (num2 < 15 && (eventRecord = eventLogReader.ReadEvent()) != null)
				{
					using (eventRecord)
					{
						diagnosis.Facts.Add("Event: " + text + " / " + eventRecord.ProviderName + " / ID " + eventRecord.Id + " / " + eventRecord.TimeCreated);
						num2++;
						diagnosis.Errors++;
					}
				}
				diagnosis.Facts.Add(text + ": " + num2 + " recent critical/error events shown (maximum 15; last 24 hours).");
			}
			catch (Exception ex4)
			{
				diagnosis.Limits.Add(text + " events unavailable: " + ex4.Message);
			}
		}
		try
		{
			List<GpuSensor> list2 = Nvidia.Read();
			foreach (GpuSensor item in list2)
			{
				diagnosis.Facts.Add(item.Name + ": GPU " + Metric(item.Load, "%") + ", temperature " + Metric(item.Temp, " °C") + ", VRAM " + Metric(item.Used / 1024.0, " GB") + " / " + Metric(item.Total / 1024.0, " GB"));
			}
			if (list2.Count == 0)
			{
				diagnosis.Limits.Add("NVIDIA vendor readings unavailable or no supported NVIDIA adapter.");
			}
		}
		catch
		{
			diagnosis.Limits.Add("GPU sensors unavailable.");
		}
		diagnosis.Advice = Recommend(diagnosis);
		diagnosis.Limits.Add("No system files, registry settings, services or startup entries were changed.");
		diagnosis.Limits.Add("Hardware sensors, event logs and protected processes may be restricted by Windows.");
		return diagnosis;
	}

	private static string Metric(double n, string unit)
	{
		if (!double.IsNaN(n))
		{
			return n.ToString("F1") + unit;
		}
		return "unavailable";
	}

	public static List<string> Recommend(Diagnosis d)
	{
		List<string> list = new List<string>();
		if (d.Cpu >= 85.0)
		{
			list.Add("High CPU was observed. Inspect the busiest processes and retest after background work completes; do not terminate unfamiliar Windows processes.");
		}
		if (d.Ram >= 85.0)
		{
			list.Add("High memory use was observed. Save work, close unneeded apps, and use Aura Eco mode. Disabling the page file is not recommended.");
		}
		if (!double.IsNaN(d.FreeGb) && (d.FreeGb < 15.0 || d.FreePercent < 10.0))
		{
			list.Add("System-drive free space is low. Review Windows Storage / Cleanup manually before deleting anything.");
		}
		if (d.NetworkKnown && !d.Network)
		{
			list.Add("No active network was detected. Check your adapter/cable/Wi-Fi before considering a network repair.");
		}
		if (d.Reboot)
		{
			list.Add("A servicing restart is pending. Save your work and restart manually before further repair attempts.");
		}
		if (d.Errors > 0)
		{
			list.Add("Review recent event IDs alongside when the symptom happened. Error events alone do not justify repairs or driver removal.");
		}
		if (list.Count == 0)
		{
			list.Add("These short checks did not reveal the tracked pressure indicators. Describe the symptom and choose a targeted diagnostic plan.");
		}
		list.Add("For suspected Windows corruption, start with a read-only scan plan. Use repair only after reviewing evidence.");
		return list;
	}

	public static string Guide(string category, string issue)
	{
		string text = "LOCAL GUIDANCE · predefined troubleshooting rules\r\n\r\n";
		return category switch
		{
			"Network" => text + "Check Wi-Fi/cable and whether other devices work. Adapter presence does not prove internet access. For name-resolution symptoms, review the DNS cache plan. Winsock reset is a separate advanced option in Recovery toolbox and can affect VPN software.", 
			"Gaming / graphics" => text + "Compare CPU/GPU load during the problem. Use Aura Eco or Pause while fullscreen, then retest. Check driver version in GPU specs. High utilization by itself is expected in many games; do not infer overheating from utilization.", 
			"Storage" => text + "Review free capacity and Storage specs. Back up important files if there are disk errors. The filesystem check plan does not repair the disk. Avoid running repeated benchmarks against a potentially failing drive.", 
			"Crashes / blue screens" => text + "Record the stop code and timing. Compare recent System/Application events, Reliability History and driver changes. Memory Diagnostic may require a restart. Do not delete drivers or restore Windows based only on an event count.", 
			"Windows files / updates" => text + "Check pending restart first. Start with the Windows scan plan. For supported corruption evidence, the approved repair plan runs DISM RestoreHealth, then SFC repair and verification; it stops on failures. It does not guarantee every update issue will be fixed.", 
			_ => text + "Describe when the slowdown started and whether it affects one app or everything. Compare sampled CPU, RAM, disk activity and free space. Try Aster Efficient monitoring and Aura Eco. These optimize these apps; they do not apply registry tweaks, disable security, or close your work.", 
		};
	}
}
