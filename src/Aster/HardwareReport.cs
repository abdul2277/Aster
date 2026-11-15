using System;
using System.Linq;
using System.Text;

namespace Aster;

internal static class HardwareReport
{
	public static bool Shareable(string key)
	{
		string text = key.ToLowerInvariant();
		return !new string[6] { "serial", "identifier", "device id", "processor id", "mac address", "ip address" }.Any(text.Contains);
	}

	public static string Format(string title, Table data)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(title.ToUpperInvariant());
		foreach (string[] row in data.Rows)
		{
			if (row.Length >= 3 && Shareable(row[1]))
			{
				stringBuilder.AppendLine(row[0] + " · " + row[1] + ": " + row[2]);
			}
		}
		if (!string.IsNullOrWhiteSpace(data.Note))
		{
			stringBuilder.AppendLine("Note: " + data.Note);
		}
		return stringBuilder.ToString();
	}

	public static string Collect()
	{
		StringBuilder stringBuilder = new StringBuilder("ASTER · HARDWARE REPORT\r\nGenerated " + DateTime.Now.ToString("g") + "\r\n\r\nHardware inventory, not a health certificate. Serial/device identifiers are excluded. Review before sharing.\r\n\r\n");
		string[] array = new string[5] { "CPU specs", "GPU", "Memory specs", "Storage specs", "Motherboard & BIOS" };
		foreach (string text in array)
		{
			stringBuilder.AppendLine(Format(text, Inventory.Hardware(text)));
			stringBuilder.AppendLine();
		}
		return stringBuilder.ToString();
	}
}
