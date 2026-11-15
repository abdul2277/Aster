using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace Aster;

internal static class HorizonStore
{
	public static string FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Aster", "Horizon.json");

	public static readonly string[] Themes = new string[8] { "Aurora", "Sakura", "Emerald", "Solar", "Amethyst", "Glacier", "Coral", "Monochrome" };

	public static readonly Color[] Accents = new Color[8]
	{
		Color.FromArgb(100, 210, 255),
		Color.FromArgb(255, 151, 207),
		Color.FromArgb(109, 235, 182),
		Color.FromArgb(255, 210, 100),
		Color.FromArgb(194, 163, 255),
		Color.FromArgb(160, 229, 239),
		Color.FromArgb(255, 158, 137),
		Color.FromArgb(230, 235, 245)
	};

	public static void Validate(HorizonSettings s)
	{
		if (s == null || s.Version != 1 || s.Theme < 0 || s.Theme >= Themes.Length || s.Opacity < 85 || s.Opacity > 100 || s.Size < 0 || s.Size > 2 || s.Display == null || s.Display.Length > 200 || s.Pins == null || s.Pins.Count > 24)
		{
			throw new InvalidDataException("Invalid Horizon settings.");
		}
		foreach (HorizonPin pin in s.Pins)
		{
			if (pin == null || string.IsNullOrWhiteSpace(pin.Name) || pin.Name.Length > 60 || string.IsNullOrWhiteSpace(pin.Path) || pin.Path.Length > 1024 || !Path.IsPathRooted(pin.Path) || !new string[2] { ".exe", ".lnk" }.Contains(Path.GetExtension(pin.Path).ToLowerInvariant()))
			{
				throw new InvalidDataException("Pins must be local app or shortcut paths with a name of 1–60 characters.");
			}
		}
	}

	public static HorizonSettings Read()
	{
		if (!File.Exists(FileName))
		{
			return new HorizonSettings();
		}
		if (new FileInfo(FileName).Length > 65536)
		{
			throw new InvalidDataException("Horizon settings file is too large.");
		}
		HorizonSettings horizonSettings = new JavaScriptSerializer().Deserialize<HorizonSettings>(File.ReadAllText(FileName));
		Validate(horizonSettings);
		return horizonSettings;
	}

	public static HorizonSettings Clone(HorizonSettings s)
	{
		return new JavaScriptSerializer().Deserialize<HorizonSettings>(new JavaScriptSerializer().Serialize(s));
	}

	public static void Save(HorizonSettings s)
	{
		Validate(s);
		Directory.CreateDirectory(Path.GetDirectoryName(FileName));
		string text = FileName + "." + Guid.NewGuid().ToString("N") + ".tmp";
		try
		{
			File.WriteAllText(text, new JavaScriptSerializer().Serialize(s));
			if (File.Exists(FileName))
			{
				File.Replace(text, FileName, FileName + ".backup");
			}
			else
			{
				File.Move(text, FileName);
			}
		}
		finally
		{
			if (File.Exists(text))
			{
				File.Delete(text);
			}
		}
	}
}
