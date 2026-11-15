using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Aster;

internal static class Nvidia
{
	public static double Number(string text)
	{
		if (!double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
		{
			return double.NaN;
		}
		return result;
	}

	public static List<GpuSensor> Parse(string output)
	{
		List<GpuSensor> list = new List<GpuSensor>();
		string[] array = output.Split(new char[2] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
		foreach (string text in array)
		{
			string[] array2 = (from v in text.Split(',')
				select v.Trim()).ToArray();
			if (array2.Length == 10)
			{
				list.Add(new GpuSensor
				{
					Index = array2[0],
					Name = array2[1],
					Total = Number(array2[2]),
					Used = Number(array2[3]),
					Load = Number(array2[4]),
					Temp = Number(array2[5]),
					Power = Number(array2[6]),
					Limit = Number(array2[7]),
					Clock = Number(array2[8]),
					Driver = array2[9]
				});
			}
		}
		return list;
	}

	public static List<GpuSensor> Read()
	{
		string text = new string[2]
		{
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32\\nvidia-smi.exe"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "NVIDIA Corporation\\NVSMI\\nvidia-smi.exe")
		}.FirstOrDefault(File.Exists);
		if (text == null)
		{
			return new List<GpuSensor>();
		}
		try
		{
			using Process process = new Process();
			process.StartInfo = new ProcessStartInfo(text, "--query-gpu=index,name,memory.total,memory.used,utilization.gpu,temperature.gpu,power.draw,power.limit,clocks.current.graphics,driver_version --format=csv,noheader,nounits")
			{
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			};
			process.Start();
			Task<string> task = process.StandardOutput.ReadToEndAsync();
			process.StandardError.ReadToEndAsync();
			if (!process.WaitForExit(3500))
			{
				try
				{
					process.Kill();
				}
				catch
				{
				}
				return new List<GpuSensor>();
			}
			if (process.ExitCode != 0)
			{
				return new List<GpuSensor>();
			}
			return Parse(task.GetAwaiter().GetResult());
		}
		catch
		{
			return new List<GpuSensor>();
		}
	}
}
