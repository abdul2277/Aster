using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Aster;

internal static class DiagnosticsBundle
{
	public static string Csv(IEnumerable<Snapshot> samples)
	{
		StringBuilder stringBuilder = new StringBuilder("Time UTC,CPU percent,Memory percent,Available RAM bytes,Total RAM bytes,Process count\r\n");
		foreach (Snapshot sample in samples)
		{
			stringBuilder.AppendLine(string.Join(",", sample.Time.ToUniversalTime().ToString("o"), sample.Cpu.ToString("F2", CultureInfo.InvariantCulture), sample.MemoryPercent.ToString("F2", CultureInfo.InvariantCulture), sample.Available.ToString(CultureInfo.InvariantCulture), sample.Total.ToString(CultureInfo.InvariantCulture), sample.Processes.Count.ToString(CultureInfo.InvariantCulture)));
		}
		return stringBuilder.ToString();
	}

	public static string Report(IEnumerable<Snapshot> samples, string version, bool paused)
	{
		Snapshot[] array = samples.ToArray();
		StringBuilder stringBuilder = new StringBuilder("ASTER DIAGNOSTICS\r\nVersion: " + version + "\r\nCaptured UTC: " + DateTime.UtcNow.ToString("o") + "\r\nMonitoring: " + (paused ? "paused" : "running") + "\r\nSamples: " + array.Length + " (up to 60 most recent samples)\r\n");
		if (array.Length > 0)
		{
			stringBuilder.AppendLine("Average CPU: " + array.Average((Snapshot s) => s.Cpu).ToString("F1", CultureInfo.InvariantCulture) + "%");
			stringBuilder.AppendLine("Peak CPU: " + array.Max((Snapshot s) => s.Cpu).ToString("F1", CultureInfo.InvariantCulture) + "%");
			stringBuilder.AppendLine("Peak memory: " + array.Max((Snapshot s) => s.MemoryPercent).ToString("F1", CultureInfo.InvariantCulture) + "%");
		}
		stringBuilder.AppendLine("\r\nThis is a resource snapshot, not a Windows health or malware scan. Paused or delayed collection can leave gaps.");
		stringBuilder.AppendLine("Contains aggregate measurements and timestamps only. No process names, computer name, serial numbers, location, notes, Telegram credentials or feed messages are included. Nothing is uploaded.");
		return stringBuilder.ToString();
	}

	public static void Write(string path, string report, string csv)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using (ZipArchive zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
		{
			string[][] array = new string[2][]
			{
				new string[2] { "report.txt", report },
				new string[2] { "performance.csv", csv }
			};
			foreach (string[] array2 in array)
			{
				ZipArchiveEntry zipArchiveEntry = zipArchive.CreateEntry(array2[0]);
				using StreamWriter streamWriter = new StreamWriter(zipArchiveEntry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
				streamWriter.Write(array2[1]);
			}
		}
		File.WriteAllBytes(path, memoryStream.ToArray());
	}
}
