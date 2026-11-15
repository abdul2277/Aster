using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Aster;

internal class ThemeCard : Button
{
	public ThemePalette Palette;

	public bool Selected;

	public ThemeCard()
	{
		base.Size = new Size(300, 206);
		base.Margin = new Padding(0, 0, 18, 18);
		base.FlatStyle = FlatStyle.Flat;
		base.AccessibleRole = AccessibleRole.PushButton;
		SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		ThemePalette palette = Palette;
		using (GraphicsPath path = PlayColors.Round(new RectangleF(1f, 1f, base.Width - 3, base.Height - 3), 14f))
		{
			using (SolidBrush brush = new SolidBrush(palette.Surface))
			{
				graphics.FillPath(brush, path);
			}
			using Pen pen = new Pen((Selected || Focused) ? palette.Accent : palette.Border, (!Selected) ? 1 : 2);
			graphics.DrawPath(pen, path);
		}
		using (SolidBrush brush2 = new SolidBrush(palette.Sidebar))
		{
			graphics.FillRectangle(brush2, 17, 17, 35, 88);
		}
		using (LinearGradientBrush brush3 = new LinearGradientBrush(new Rectangle(60, 17, base.Width - 80, 36), palette.Hero, palette.Raised, 0f))
		{
			graphics.FillRectangle(brush3, 60, 17, base.Width - 80, 36);
		}
		for (int i = 0; i < 3; i++)
		{
			using (SolidBrush brush4 = new SolidBrush((i == 0) ? palette.Accent : palette.Raised))
			{
				graphics.FillRectangle(brush4, 23, 28 + i * 22, 23, 5);
			}
			using SolidBrush brush5 = new SolidBrush(palette.Raised);
			graphics.FillRectangle(brush5, 60 + i * 72, 63, 64, 40);
		}
		using (Font font = new Font("Segoe UI", 15f, FontStyle.Bold))
		{
			TextRenderer.DrawText(graphics, palette.Name + (Selected ? "  ✓" : ""), font, new Rectangle(17, 118, base.Width - 30, 32), palette.Text, TextFormatFlags.NoPrefix);
		}
		using (Font font2 = new Font("Segoe UI", 9f))
		{
			TextRenderer.DrawText(graphics, palette.Description, font2, new Rectangle(17, 154, base.Width - 30, 24), palette.Muted, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
		}
		using Font font3 = new Font("Segoe UI", 8f);
		TextRenderer.DrawText(graphics, (!base.Enabled) ? "PREVIEW ONLY" : (Selected ? "ACTIVE THEME" : "CLICK TO APPLY"), font3, new Rectangle(17, 181, base.Width - 30, 18), palette.Accent, TextFormatFlags.NoPrefix);
	}
}
