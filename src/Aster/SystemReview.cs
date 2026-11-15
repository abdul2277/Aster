using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Aster;

internal static class SystemReview
{
	public static string SpaceStatus(long free, long total)
	{
		if (total <= 0)
		{
			return "Unavailable";
		}
		if (!((double)free / (double)total < 0.1))
		{
			return "At least 10% free.";
		}
		return "Low space — below 10%; review files before removing anything.";
	}

	public static string Capture(Snapshot sample)
	{
		StringBuilder stringBuilder = new StringBuilder("ASTER SYSTEM CHECK\r\nCaptured: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\r\n\r\n");
		if (sample == null)
		{
			stringBuilder.AppendLine("Live readings not yet available.");
		}
		else
		{
			stringBuilder.AppendLine("Sample time: " + sample.Time.ToString("yyyy-MM-dd HH:mm:ss"));
			stringBuilder.AppendLine("CPU: " + sample.Cpu.ToString("F1") + "%" + ((sample.Cpu >= 90.0) ? " — high in this sample; check Processes." : ""));
			stringBuilder.AppendLine("Memory: " + sample.MemoryPercent.ToString("F1") + "%" + ((sample.MemoryPercent >= 85.0) ? " — review unneeded apps after saving work." : ""));
			stringBuilder.AppendLine("Available memory: " + Inventory.Bytes(sample.Available));
		}
		stringBuilder.AppendLine("\r\nFIXED STORAGE");
		try
		{
			foreach (DriveInfo item in from d in DriveInfo.GetDrives()
				where d.DriveType == DriveType.Fixed
				select d)
			{
				try
				{
					if (!item.IsReady)
					{
						stringBuilder.AppendLine(item.Name + " unavailable");
						continue;
					}
					long availableFreeSpace = item.AvailableFreeSpace;
					long totalSize = item.TotalSize;
					stringBuilder.AppendLine(item.Name + "  " + Inventory.Bytes((ulong)availableFreeSpace) + " available / " + Inventory.Bytes((ulong)totalSize) + "  · " + SpaceStatus(availableFreeSpace, totalSize));
				}
				catch (Exception ex)
				{
					stringBuilder.AppendLine(item.Name + " unavailable: " + ex.Message);
				}
			}
		}
		catch (Exception ex2)
		{
			stringBuilder.AppendLine("Storage unavailable: " + ex2.Message);
		}
		stringBuilder.AppendLine("\r\nPOWER");
		try
		{
			PowerStatus powerStatus = SystemInformation.PowerStatus;
			stringBuilder.AppendLine("Power source: " + powerStatus.PowerLineStatus);
			BatteryChargeStatus batteryChargeStatus = powerStatus.BatteryChargeStatus;
			if (batteryChargeStatus == BatteryChargeStatus.Unknown)
			{
				stringBuilder.AppendLine("Battery status unavailable.");
			}
			else if ((batteryChargeStatus & BatteryChargeStatus.NoSystemBattery) != 0)
			{
				stringBuilder.AppendLine("No system battery reported.");
			}
			else
			{
				float batteryLifePercent = powerStatus.BatteryLifePercent;
				stringBuilder.AppendLine((batteryLifePercent >= 0f && batteryLifePercent <= 1f) ? ("Battery: " + (batteryLifePercent * 100f).ToString("F0") + "%") : "Battery level unavailable.");
			}
		}
		catch
		{
			stringBuilder.AppendLine("Power information unavailable.");
		}
		stringBuilder.AppendLine("\r\nA snapshot, not a health certification. This does not scan Windows files, disk health, malware or updates. One high sample does not prove a fault. No changes were made.");
		return stringBuilder.ToString();
	}
}
