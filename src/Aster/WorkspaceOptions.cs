using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace Aster;

internal class WorkspaceOptions
{
	public List<MaintenanceItem> Maintenance = new List<MaintenanceItem>();

	public bool PressureEnabled;

	public int CpuLimit = 90;

	public int RamLimit = 85;

	public int PressureSeconds = 15;

	public int PressureCooldown = 300;

	public bool StartupEnabled = true;

	public string StartupStyle = "Orbit";

	public int StartupDuration = 2800;

	public bool[] HomeSections = new bool[7] { true, true, true, true, true, true, true };

	public string CustomBase = "#171925";

	public string CustomAccent = "#BCA4FF";

	public bool DailyPicture;

	public int PictureChoice;

	public int PictureShift;

	public string ThemeId = "midnight";

	public string DisplayName = "";

	public bool RiyadhTime = true;

	public bool ShowHijri = true;

	public bool Clock24 = true;

	public int HijriOffset;

	public string PlayTheme = "Nebula";

	public bool Motion = true;

	public int Interval = 1000;

	public string Accent = "Ocean";

	public bool AlwaysOnTop;

	public List<string> Pins = new List<string> { "Summary", "Windows advisor", "Processes" };

	public static string FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Pulse", "Workspace.json");

	public static WorkspaceOptions Read()
	{
		try
		{
			if (new FileInfo(FileName).Length > 16384)
			{
				return new WorkspaceOptions();
			}
			WorkspaceOptions workspaceOptions = new JavaScriptSerializer().Deserialize<WorkspaceOptions>(File.ReadAllText(FileName));
			if (workspaceOptions == null)
			{
				return new WorkspaceOptions();
			}
			if (!new int[4] { 1000, 2000, 3000, 5000 }.Contains(workspaceOptions.Interval))
			{
				workspaceOptions.Interval = 1000;
			}
			if (workspaceOptions.Pins == null)
			{
				workspaceOptions.Pins = new List<string>();
			}
			workspaceOptions.Pins = workspaceOptions.Pins.Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct().Take(100)
				.ToList();
			if (workspaceOptions.Maintenance == null)
			{
				workspaceOptions.Maintenance = new List<MaintenanceItem>();
			}
			workspaceOptions.Maintenance = workspaceOptions.Maintenance.Where((MaintenanceItem x) => x != null && !string.IsNullOrWhiteSpace(x.Title) && x.Title.Length <= 80 && x.RepeatDays >= 0 && x.RepeatDays <= 365).Take(24).ToList();
			return workspaceOptions;
		}
		catch
		{
			return new WorkspaceOptions();
		}
	}

	public void Save()
	{
		Directory.CreateDirectory(Path.GetDirectoryName(FileName));
		string text = FileName + ".tmp";
		File.WriteAllText(text, new JavaScriptSerializer().Serialize(this));
		if (File.Exists(FileName))
		{
			File.Replace(text, FileName, null);
		}
		else
		{
			File.Move(text, FileName);
		}
	}
}
