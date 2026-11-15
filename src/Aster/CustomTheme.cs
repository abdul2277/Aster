using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Web.Script.Serialization;

namespace Aster;

internal static class CustomTheme
{
	public static Color Parse(string value)
	{
		if (value == null || value.Length != 7 || value[0] != '#')
		{
			throw new Exception("Use six-digit colors such as #171925.");
		}
		if (!int.TryParse(value.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var result))
		{
			throw new Exception("Invalid hexadecimal color.");
		}
		return Color.FromArgb(result >> 16, (result >> 8) & 0xFF, result & 0xFF);
	}

	private static double Channel(byte x)
	{
		double num = (double)(int)x / 255.0;
		if (!(num <= 0.04045))
		{
			return Math.Pow((num + 0.055) / 1.055, 2.4);
		}
		return num / 12.92;
	}

	public static double Luminance(Color c)
	{
		return 0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);
	}

	public static double Contrast(Color a, Color b)
	{
		double val = Luminance(a);
		double val2 = Luminance(b);
		return (Math.Max(val, val2) + 0.05) / (Math.Min(val, val2) + 0.05);
	}

	public static ThemePalette Create(ThemeRecipe r)
	{
		if (r == null || r.Version != 1)
		{
			throw new Exception("Unsupported theme recipe.");
		}
		Color color = Parse(r.Background);
		Color color2 = Parse(r.Accent);
		ThemePalette themePalette = new ThemePalette();
		themePalette.Id = "custom";
		themePalette.Name = "Your custom theme";
		themePalette.Description = "Made in Aster Theme studio";
		themePalette.Background = color;
		themePalette.Surface = ThemePalette.Mix(color, Color.White, 0.035);
		themePalette.Raised = ThemePalette.Mix(color, Color.White, 0.07);
		themePalette.Sidebar = ThemePalette.Mix(color, Color.Black, 0.12);
		themePalette.Border = ThemePalette.Mix(color, Color.White, 0.2);
		themePalette.Hero = ThemePalette.Mix(color, color2, 0.18);
		themePalette.Accent = color2;
		ThemePalette themePalette2 = themePalette;
		if (Contrast(themePalette2.Text, themePalette2.Raised) < 4.5 || Contrast(themePalette2.Muted, themePalette2.Raised) < 4.5)
		{
			throw new Exception("Choose a darker background so the text stays readable.");
		}
		if (Contrast(color2, themePalette2.Raised) < 3.0 || Contrast(color2, themePalette2.Hero) < 3.0)
		{
			throw new Exception("Choose a brighter accent so highlights stay visible.");
		}
		return themePalette2;
	}

	public static ThemeRecipe Read(string path)
	{
		if (new FileInfo(path).Length > 8192)
		{
			throw new Exception("Theme recipes must be smaller than 8 KB.");
		}
		JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
		javaScriptSerializer.MaxJsonLength = 8192;
		ThemeRecipe themeRecipe = javaScriptSerializer.Deserialize<ThemeRecipe>(File.ReadAllText(path));
		Create(themeRecipe);
		return themeRecipe;
	}
}
