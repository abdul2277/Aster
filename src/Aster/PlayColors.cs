using System.Drawing;
using System.Drawing.Drawing2D;

namespace Aster;

internal static class PlayColors
{
	public static Color Primary(string theme)
	{
		if (!(theme == "Tide"))
		{
			if (!(theme == "Sunset"))
			{
				return Color.FromArgb(182, 147, 255);
			}
			return Color.FromArgb(255, 172, 123);
		}
		return Color.FromArgb(64, 223, 193);
	}

	public static Color Deep(string theme)
	{
		if (!(theme == "Tide"))
		{
			if (!(theme == "Sunset"))
			{
				return Color.FromArgb(50, 37, 94);
			}
			return Color.FromArgb(89, 40, 73);
		}
		return Color.FromArgb(14, 65, 75);
	}

	public static GraphicsPath Round(RectangleF r, float radius)
	{
		GraphicsPath graphicsPath = new GraphicsPath();
		float num = radius * 2f;
		graphicsPath.AddArc(r.X, r.Y, num, num, 180f, 90f);
		graphicsPath.AddArc(r.Right - num, r.Y, num, num, 270f, 90f);
		graphicsPath.AddArc(r.Right - num, r.Bottom - num, num, num, 0f, 90f);
		graphicsPath.AddArc(r.X, r.Bottom - num, num, num, 90f, 90f);
		graphicsPath.CloseFigure();
		return graphicsPath;
	}
}
