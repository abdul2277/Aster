using System;
using System.IO;
using System.Linq;

namespace Aster;

internal class RecoveryJob
{
	public string Id;

	public string Title;

	public string Description;

	public string Exe;

	public string Arguments;

	public bool Admin;

	public static RecoveryJob[] All = new RecoveryJob[8]
	{
		new RecoveryJob
		{
			Id = "image-check",
			Title = "Check Windows image",
			Description = "Quickly checks whether the Windows component store is already marked as damaged. Does not repair files.",
			Exe = "dism.exe",
			Arguments = "/Online /Cleanup-Image /CheckHealth",
			Admin = true
		},
		new RecoveryJob
		{
			Id = "image-scan",
			Title = "Scan Windows image",
			Description = "Performs a deeper component-store scan without repairing it. May take several minutes.",
			Exe = "dism.exe",
			Arguments = "/Online /Cleanup-Image /ScanHealth",
			Admin = true
		},
		new RecoveryJob
		{
			Id = "image-repair",
			Title = "Repair Windows image",
			Description = "Repairs the Windows component store. May download files from Windows Update. Keep your PC powered on until it finishes.",
			Exe = "dism.exe",
			Arguments = "/Online /Cleanup-Image /RestoreHealth",
			Admin = true
		},
		new RecoveryJob
		{
			Id = "files-check",
			Title = "Verify system files",
			Description = "Checks protected Windows system files without replacing them. Use the log to see whether corruption was found.",
			Exe = "sfc.exe",
			Arguments = "/verifyonly",
			Admin = true
		},
		new RecoveryJob
		{
			Id = "files-repair",
			Title = "Repair system files",
			Description = "Scans and replaces damaged protected system files. If image corruption is suspected, run Repair Windows image first. A restart may be needed afterward.",
			Exe = "sfc.exe",
			Arguments = "/scannow",
			Admin = true
		},
		new RecoveryJob
		{
			Id = "disk-check",
			Title = "Check system-drive filesystem",
			Description = "Runs CHKDSK without repair switches. Does not schedule a reboot or attempt repairs. Files in use can affect the report.",
			Exe = "chkdsk.exe",
			Arguments = Path.GetPathRoot(Environment.SystemDirectory).TrimEnd('\\'),
			Admin = true
		},
		new RecoveryJob
		{
			Id = "dns-flush",
			Title = "Flush DNS cache",
			Description = "Clears cached DNS answers to help troubleshoot name-resolution problems. The cache rebuilds as you browse.",
			Exe = "ipconfig.exe",
			Arguments = "/flushdns",
			Admin = true
		},
		new RecoveryJob
		{
			Id = "winsock-reset",
			Title = "Reset Winsock",
			Description = "Resets the Winsock catalog. Some VPN or networking software may need repair afterward. Save your work and restart Windows manually after reviewing the result.",
			Exe = "netsh.exe",
			Arguments = "winsock reset",
			Admin = true
		}
	};

	public string FileName => Path.Combine(Environment.SystemDirectory, Exe);

	public static RecoveryJob Find(string id)
	{
		return All.FirstOrDefault((RecoveryJob j) => j.Id == id);
	}
}
