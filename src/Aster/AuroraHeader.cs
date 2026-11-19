using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Aster;

internal class AuroraHeader : Panel
{
	public string ThemeName = "Nebula";

	public AuroraHeader()
	{
		DoubleBuffered = true;
	}

	protected override void OnPaintBackground(PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		if (base.Width < 2 || base.Height < 2)
		{
			return;
		}
		using (GraphicsPath path = PlayColors.Round(new RectangleF(0f, 0f, base.Width - 1, base.Height - 1), 16f))
		{
			using LinearGradientBrush brush = new LinearGradientBrush(base.ClientRectangle, ThemePalette.Current.Hero, ThemePalette.Current.Surface, 15f);
			graphics.FillPath(brush, path);
		}
		for (int i = 0; i < 4; i++)
		{
			using Pen pen = new Pen(Color.FromArgb(22, ThemePalette.Current.Accent), 12f);
			graphics.DrawBezier(pen, (float)base.Width * 0.5f + (float)(i * 20), base.Height + 20, (float)base.Width * 0.68f, -70f, (float)base.Width * 0.78f, base.Height + 50, base.Width + 30, -10f);
		}
	}
}
