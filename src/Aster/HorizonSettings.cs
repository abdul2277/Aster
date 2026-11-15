using System.Collections.Generic;

namespace Aster;

public class HorizonSettings
{
	public int Version = 1;

	public int Theme;

	public int Opacity = 96;

	public int Size = 1;

	public string Display = "";

	public bool AutoHide = true;

	public bool HideFullscreen = true;

	public bool Clock24 = true;

	public bool ShowMeters = true;

	public List<HorizonPin> Pins = new List<HorizonPin>();
}
