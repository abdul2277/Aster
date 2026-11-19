using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Aster;

internal class MetricTile : Control
{
	public string Caption;

	public string Value = "—";

	public string Detail = "Waiting for sample";

	public Color Accent;

	public double Level;

	public MetricTile()
	{
		DoubleBuffered = true;
		base.Margin = new Padding(0, 0, 12, 12);
		Dock = DockStyle.Fill;
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		if (base.Width < 2 || base.Height < 2)
		{
			return;
		}
		using (GraphicsPath path = PlayColors.Round(new RectangleF(0f, 0f, base.Width - 1, base.Height - 1), 14f))
		{
			using (LinearGradientBrush brush = new LinearGradientBrush(base.ClientRectangle, ThemePalette.Current.Raised, ThemePalette.Current.Surface, 70f))
			{
				graphics.FillPath(brush, path);
			}
			using Pen pen = new Pen(ThemePalette.Current.Border);
			graphics.DrawPath(pen, path);
		}
		using (Font font = new Font("Segoe UI", 9f, FontStyle.Bold))
		{
			TextRenderer.DrawText(graphics, Caption, font, new Rectangle(18, 16, base.Width - 32, 20), ThemePalette.Current.Muted, TextFormatFlags.EndEllipsis);
		}
		using (Font font2 = new Font("Segoe UI", 26f, FontStyle.Bold))
		{
			TextRenderer.DrawText(graphics, Value, font2, new Rectangle(15, 39, base.Width - 28, 49), Color.White, TextFormatFlags.EndEllipsis);
		}
		using (Font font3 = new Font("Segoe UI", 9f))
		{
			TextRenderer.DrawText(graphics, Detail, font3, new Rectangle(18, 94, base.Width - 32, 23), ThemePalette.Current.Muted, TextFormatFlags.EndEllipsis);
		}
		using SolidBrush brush2 = new SolidBrush(Accent);
		graphics.FillRectangle(brush2, 0f, base.Height - 4, (float)((double)base.Width * Math.Max(0.0, Math.Min(1.0, Level))), 4f);
	}
}
