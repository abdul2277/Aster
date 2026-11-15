using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Aster;

internal static class Program
{
	[STAThread]
	private static void Main(string[] args)
	{
		if (args.Length > 0 && args[0] == "--self-test")
		{
			if (args.Length < 2)
			{
				Environment.ExitCode = 2;
				return;
			}
			try
			{
				Collector collector = new Collector();
				collector.Sample();
				Thread.Sleep(1100);
				Snapshot snapshot = collector.Sample();
				if (snapshot.Total == 0 || snapshot.Processes.Count == 0 || snapshot.Cpu < 0.0 || snapshot.Cpu > 100.0)
				{
					throw new Exception("Invalid system sample");
				}
				string[] array = new string[14]
				{
					"System info", "CPU specs", "Memory specs", "Storage specs", "Motherboard & BIOS", "Network adapters", "Services", "Startup apps", "Disk space", "Connections",
					"Installed apps", "Drivers", "Users", "GPU"
				};
				foreach (string text in array)
				{
					Table table = Inventory.Read(text);
					if (table.Columns.Length == 0)
					{
						throw new Exception(text + " has no schema");
					}
				}
				File.WriteAllText(args[1], "PASS: CPU sampling, memory, process enumeration and inventory schemas (provider availability may be restricted).\r\nCPU: " + snapshot.Cpu.ToString("F1") + "%; RAM: " + ((double)snapshot.Total / 1073741824.0).ToString("F1") + " GB; processes: " + snapshot.Processes.Count + "\r\n");
				return;
			}
			catch (Exception ex)
			{
				File.WriteAllText(args[1], "FAIL: " + ex);
				Environment.ExitCode = 1;
				return;
			}
		}
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(defaultValue: false);
		if (args.Length == 2 && args[0] == "--repair")
		{
			RecoveryJob recoveryJob = RecoveryJob.Find(args[1]);
			if (recoveryJob == null)
			{
				MessageBox.Show("Unknown recovery tool.");
			}
			else
			{
				Application.Run(new RepairWindow(recoveryJob));
			}
		}
		else if (args.Length == 2 && args[0] == "--plan")
		{
			RepairPlan repairPlan = RepairPlan.Find(args[1]);
			if (repairPlan == null)
			{
				MessageBox.Show("Unknown repair plan.");
			}
			else
			{
				Application.Run(new PlanWindow(repairPlan));
			}
		}
		else if (args.Length != 0 || !UpdateReceipt.TryForward())
		{
			StartupLauncher.Run();
		}
	}
}
