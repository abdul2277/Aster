using System.Drawing;
using System.Linq;

namespace Aster;

internal class ThemePalette
{
	public string Id;

	public string Name;

	public string Description;

	public Color Background;

	public Color Surface;

	public Color Raised;

	public Color Sidebar;

	public Color Border;

	public Color Accent;

	public Color Hero;

	public Color Text = Color.FromArgb(231, 238, 248);

	public Color Muted = Color.FromArgb(169, 187, 207);

	private static readonly ThemePalette[] Core = new ThemePalette[6]
	{
		Make("midnight", "Midnight", "Cool blue · clear and composed", 1054498, 1582133, 2109768, 1121579, 3426399, 6739955, 2113377),
		Make("amethyst", "Amethyst", "Soft violet · a little cosmic", 1578018, 2367029, 3287624, 2103597, 5325923, 12953087, 4796773),
		Make("lagoon", "Lagoon", "Deep teal · calm and refreshing", 793632, 1387312, 1850178, 1058090, 3430236, 7463886, 1528925),
		Make("rose", "Rosewood", "Warm rose · quietly expressive", 2299676, 3417133, 4731453, 2890788, 6440016, 16757446, 6698822),
		Make("ember", "Ember", "Amber glow · warm after dark", 2103829, 3156000, 4273706, 2694936, 6311997, 16764039, 6440490),
		Make("graphite", "Graphite", "Neutral charcoal · less distraction", 1513756, 2435374, 3291198, 1974822, 4804954, 12899812, 3885142)
	};

	public static readonly ThemePalette[] All = CreateCollection();

	public static ThemePalette Current = All[0];

	private static Color C(int hex)
	{
		return Color.FromArgb((hex >> 16) & 0xFF, (hex >> 8) & 0xFF, hex & 0xFF);
	}

	private static ThemePalette Make(string id, string name, string description, int bg, int surface, int raised, int side, int border, int accent, int hero)
	{
		ThemePalette themePalette = new ThemePalette();
		themePalette.Id = id;
		themePalette.Name = name;
		themePalette.Description = description;
		themePalette.Background = C(bg);
		themePalette.Surface = C(surface);
		themePalette.Raised = C(raised);
		themePalette.Sidebar = C(side);
		themePalette.Border = C(border);
		themePalette.Accent = C(accent);
		themePalette.Hero = C(hero);
		return themePalette;
	}

	public static ThemePalette Find(string id)
	{
		return All.FirstOrDefault((ThemePalette t) => t.Id == id) ?? All[0];
	}

	public static Color Mix(Color a, Color b, double amount)
	{
		return Color.FromArgb((int)((double)(int)a.R * (1.0 - amount) + (double)(int)b.R * amount), (int)((double)(int)a.G * (1.0 - amount) + (double)(int)b.G * amount), (int)((double)(int)a.B * (1.0 - amount) + (double)(int)b.B * amount));
	}

	private static ThemePalette[] CreateCollection()
	{
		string[] source = new string[18]
		{
			"Sapphire", "Emerald", "Cherry", "Lavender", "Copper", "Iceberg", "Moss", "Plum", "Cocoa", "Indigo",
			"Peach", "Slate", "Jade", "Orchid", "Sandstone", "Arctic", "Mulberry", "Electric"
		};
		int[] hues = new int[18]
		{
			8631551, 9167276, 16751515, 14006783, 16036749, 10545407, 12769171, 14987750, 15058336, 11121919,
			16762030, 11585756, 9429455, 15904739, 15324072, 12382185, 15115974, 15071118
		};
		return Core.Concat(source.Select(delegate(string name, int i)
		{
			Color color = C(hues[i]);
			return new ThemePalette
			{
				Id = name.ToLowerInvariant(),
				Name = name,
				Description = "Coordinated dark palette · " + name,
				Background = Mix(Color.FromArgb(14, 16, 22), color, 0.05),
				Surface = Mix(Color.FromArgb(19, 22, 29), color, 0.1),
				Raised = Mix(Color.FromArgb(22, 26, 34), color, 0.16),
				Sidebar = Mix(Color.FromArgb(15, 18, 24), color, 0.08),
				Border = Mix(Color.FromArgb(35, 41, 52), color, 0.25),
				Accent = color,
				Hero = Mix(Color.FromArgb(24, 28, 38), color, 0.25)
			};
		})).ToArray();
	}
}
