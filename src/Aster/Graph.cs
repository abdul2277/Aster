using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace Aster;

internal class Graph : Control
{
	public List<double> Values = new List<double>();

	public Color Accent = Color.FromArgb(55, 197, 231);

	public string Caption = "CPU";

	public string Value = "Waiting for sample";

	public string Hint = "";

	public double Ceiling = 100.0;

	public bool AutoScale;

	public Graph()
	{
		DoubleBuffered = true;
		BackColor = Color.FromArgb(24, 29, 37);
		ForeColor = Color.White;
		MinimumSize = new Size(150, 100);
	}

	public void Add(double v)
	{
		Values.Add(v);
		if (Values.Count > 60)
		{
			Values.RemoveAt(0);
		}
		Invalidate();
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		Rectangle r = new Rectangle(18, 64, Math.Max(1, base.Width - 36), Math.Max(1, base.Height - 91));
		using (Font font = new Font("Segoe UI", 10f, FontStyle.Bold))
		{
			using Font font2 = new Font("Segoe UI", 19f);
			using SolidBrush brush = new SolidBrush(ThemePalette.Current.Muted);
			using SolidBrush brush2 = new SolidBrush(ForeColor);
			graphics.DrawString(Caption, font, brush2, 18f, 13f);
			SizeF sizeF = graphics.MeasureString(Value, font2);
			using (SolidBrush brush3 = new SolidBrush(Accent))
			{
				graphics.DrawString(Value, font2, brush3, (float)base.Width - sizeF.Width - 18f, 12f);
			}
			graphics.DrawString(Hint, Font, brush, 18f, base.Height - 23);
		}
		using (Pen pen = new Pen(ThemePalette.Current.Border))
		{
			for (int i = 0; i <= 10; i++)
			{
				graphics.DrawLine(pen, r.X + r.Width * i / 10, r.Y, r.X + r.Width * i / 10, r.Bottom);
			}
			for (int j = 0; j <= 4; j++)
			{
				graphics.DrawLine(pen, r.X, r.Y + r.Height * j / 4, r.Right, r.Y + r.Height * j / 4);
			}
		}
		double num = (AutoScale ? Math.Max(1024.0, Values.Where((double v) => !double.IsNaN(v)).DefaultIfEmpty(0.0).Max() * 1.15) : Ceiling);
		List<PointF> list = new List<PointF>();
		for (int num2 = 0; num2 < Values.Count; num2++)
		{
			double num3 = Values[num2];
			if (double.IsNaN(num3))
			{
				DrawSegment(graphics, list, r);
				list.Clear();
			}
			else
			{
				list.Add(new PointF((float)r.Right - (float)((Values.Count - 1 - num2) * r.Width) / 59f, (float)((double)r.Bottom - (double)r.Height * Math.Max(0.0, Math.Min(1.0, num3 / num)))));
			}
		}
		DrawSegment(graphics, list, r);
	}

	private void DrawSegment(Graphics g, List<PointF> points, Rectangle r)
	{
		if (points.Count < 2)
		{
			return;
		}
		List<PointF> list = new List<PointF>(points);
		list.Add(new PointF(points[points.Count - 1].X, r.Bottom));
		list.Add(new PointF(points[0].X, r.Bottom));
		using (SolidBrush brush = new SolidBrush(Color.FromArgb(35, Accent)))
		{
			g.FillPolygon(brush, list.ToArray());
		}
		using Pen pen = new Pen(Accent, 1.8f);
		g.DrawLines(pen, points.ToArray());
	}
}
