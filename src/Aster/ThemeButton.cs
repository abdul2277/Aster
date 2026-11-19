using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Aster;

internal class ThemeButton : Button
{
	private bool hover;

	public ThemeButton()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
	}

	protected override void OnMouseEnter(EventArgs e)
	{
		hover = true;
		Invalidate();
		base.OnMouseEnter(e);
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		hover = false;
		Invalidate();
		base.OnMouseLeave(e);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		if (base.Width < 3 || base.Height < 3)
		{
			return;
		}
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		ThemePalette current = ThemePalette.Current;
		using (GraphicsPath path = PlayColors.Round(new RectangleF(1f, 1f, base.Width - 3, base.Height - 3), 8f))
		{
			using (SolidBrush brush = new SolidBrush(hover ? current.Raised : BackColor))
			{
				graphics.FillPath(brush, path);
			}
			if (hover || Focused || TextAlign != ContentAlignment.MiddleLeft || BackColor != current.Sidebar)
			{
				using Pen pen = new Pen(Focused ? current.Accent : current.Border, 1f);
				graphics.DrawPath(pen, path);
			}
		}
		TextFormatFlags textFormatFlags = TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter;
		TextRenderer.DrawText(flags: (TextAlign != ContentAlignment.MiddleLeft) ? (textFormatFlags | TextFormatFlags.HorizontalCenter) : textFormatFlags, dc: graphics, text: Text, font: Font, bounds: new Rectangle(10, 1, Math.Max(1, base.Width - 20), base.Height - 2), foreColor: base.Enabled ? ForeColor : current.Muted);
	}
}
