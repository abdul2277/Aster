using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Aster;

internal sealed class DesktopTrail : Form
{
	private readonly Timer ticker = new Timer
	{
		Interval = 33
	};

	private readonly Queue<Point> points = new Queue<Point>();

	private readonly Color accent;

	private readonly string style;

	private Point previous = new Point(int.MinValue, int.MinValue);

	protected override bool ShowWithoutActivation => true;

	protected override CreateParams CreateParams
	{
		get
		{
			CreateParams createParams = base.CreateParams;
			createParams.ExStyle |= 134742176;
			return createParams;
		}
	}

	public DesktopTrail(Rectangle bounds, Color color, string selected, int interval)
	{
		ticker.Interval = interval;
		accent = color;
		style = selected;
		base.FormBorderStyle = FormBorderStyle.None;
		base.ShowInTaskbar = false;
		base.StartPosition = FormStartPosition.Manual;
		base.Bounds = bounds;
		BackColor = Color.Black;
		base.TransparencyKey = Color.Black;
		base.TopMost = true;
		DoubleBuffered = true;
		Timer timer = ticker;
		EventHandler value = delegate
		{
			Point point = PointToClient(Cursor.Position);
			if (point != previous && base.ClientRectangle.Contains(point))
			{
				points.Enqueue(point);
				previous = point;
			}
			else if (points.Count > 0)
			{
				points.Dequeue();
			}
			while (points.Count > 18)
			{
				points.Dequeue();
			}
			Invalidate();
		};
		timer.Tick += value;
		ticker.Start();
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
		Point[] array = points.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			float num = 2f + (float)i / 3f;
			Color color = Color.FromArgb(255, accent.R * (i + 1) / array.Length, accent.G * (i + 1) / array.Length, accent.B * (i + 1) / array.Length);
			using Pen pen = new Pen(color, 2f);
			using SolidBrush brush = new SolidBrush(color);
			Point point = array[i];
			if (style == "Starlight")
			{
				e.Graphics.DrawLine(pen, (float)point.X - num, point.Y, (float)point.X + num, point.Y);
				e.Graphics.DrawLine(pen, point.X, (float)point.Y - num, point.X, (float)point.Y + num);
			}
			else if (style == "Comet")
			{
				e.Graphics.FillEllipse(brush, (float)point.X - num / 2f, (float)point.Y - num / 2f, num, num);
			}
			else
			{
				e.Graphics.DrawEllipse(pen, (float)point.X - num, (float)point.Y - num, num * 2f, num * 2f);
			}
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			ticker.Dispose();
		}
		base.Dispose(disposing);
	}
}
