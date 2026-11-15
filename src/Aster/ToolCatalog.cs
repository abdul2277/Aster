using System;

namespace Aster;

internal static class ToolCatalog
{
	public static readonly string[][] Groups = new string[5][]
	{
		new string[12]
		{
			"MONITORING", "Session timeline", "Performance compare", "Performance alerts", "Air Defense feed", "Summary", "Performance", "Processes", "Compact monitor", "Insights",
			"App history", "Flight recorder"
		},
		new string[14]
		{
			"HARDWARE & NETWORK", "CPU specs", "GPU", "Memory specs", "Storage specs", "Motherboard & BIOS", "Network adapters", "System info", "Disk space", "Space finder", "Network lab", "Ping monitor",
			"Hardware report", "Hardware compare"
		},
		new string[14]
		{
			"REPAIR & AI HELP", "Diagnostics bundle", "Update Aster", "System check", "Windows advisor", "Recovery toolbox", "Backup center", "Help center", "Startup apps", "Services",
			"Connections", "Installed apps", "Drivers", "Users"
		},
		new string[8] { "PRODUCTIVITY", "Maintenance planner", "Command Center", "Notebook", "Checklist", "Focus", "Focus timer", "Power timer" },
		new string[10] { "APPEARANCE & PLAY", "Horizon desktop", "Desktop mods", "Video player", "Themes", "Theme studio", "Home layout", "Wallpaper studio", "Playground", "Preferences" }
	};

	public static string Hint(string name)
	{
		return name switch
		{
			"Horizon desktop" => "Custom desktop toolbar, dock, launcher and saved app pins", 
			"Session timeline" => "Session activity, personal markers and text export", 
			"Performance compare" => "Measure before and after using 20-sample windows", 
			"Maintenance planner" => "Recurring local reminders, completion and snooze", 
			"Command Center" => "Live status and shortcuts for your day", 
			"Performance alerts" => "Custom CPU and memory limits, sustained duration and cooldown", 
			"Diagnostics bundle" => "Preview and export a private aggregate support snapshot", 
			"Air Defense feed" => "Public advisories near you · Telegram alert monitor", 
			"Preferences" => "Startup animations, daily picture, greeting & settings", 
			"System check" => "Review resource pressure, storage space and power", 
			"Windows advisor" => "Investigate issues and prepare optional AI help", 
			"Backup center" => "Save and restore your Aster workspace", 
			"Network lab" => "Speed test, adapter details and saved results", 
			"Space finder" => "See which folders and files fill your disk · read-only", 
			"Ping monitor" => "Live latency, jitter and packet-loss graph", 
			"Power timer" => "Schedule shutdown or restart, lock or sleep", 
			_ => "Open " + name.ToLowerInvariant(), 
		};
	}

	public static bool Matches(string name, string query)
	{
		return (name + " " + Hint(name)).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
	}
}
