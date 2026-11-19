using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Aster;

internal class OrbitCanvas : Control
{
	private class Spark
	{
		public float X;

		public float Y;

		public float Vx;

		public float Vy;

		public int Life;
	}

	public string ThemeName = "Nebula";

	public string Scene = "Orbit";

	public double Cpu;

	public double Ram;

	public bool HasData;

	public double Phase;

	public string CenterTitle = "ASTER";

	public string CenterValue = "SYSTEM PLAYGROUND";

	public bool TimerMode;

	public double Progress;

	private List<Spark> sparks = new List<Spark>();

	private Random random = new Random(47);

	public bool Motion = true;

	public OrbitCanvas()
	{
		DoubleBuffered = true;
		Dock = DockStyle.Fill;
		BackColor = Color.FromArgb(14, 19, 31);
		Cursor = Cursors.Cross;
		SetStyle(ControlStyles.ResizeRedraw, value: true);
	}

	public void Celebrate()
	{
		if (Motion)
		{
			Burst(base.Width / 2, base.Height / 3);
		}
	}

	private void Burst(int x, int y)
	{
		for (int i = 0; i < 22; i++)
		{
			double num = random.NextDouble() * Math.PI * 2.0;
			sparks.Add(new Spark
			{
				X = x,
				Y = y,
				Vx = (float)Math.Cos(num) * 4f,
				Vy = (float)Math.Sin(num) * 4f,
				Life = 45
			});
		}
		if (sparks.Count > 88)
		{
			sparks.RemoveRange(0, sparks.Count - 88);
		}
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		if (Motion)
		{
			Burst(e.X, e.Y);
			return;
		}
		Phase += 0.4;
		Invalidate();
	}

	public void Advance()
	{
		if (Motion)
		{
			Phase += 0.022;
			foreach (Spark spark in sparks)
			{
				spark.X += spark.Vx;
				spark.Y += spark.Vy;
				spark.Vy += 0.045f;
				spark.Life--;
			}
			sparks.RemoveAll((Spark s) => s.Life <= 0);
		}
		else
		{
			sparks.Clear();
		}
		Invalidate();
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		if (base.Width < 10 || base.Height < 10)
		{
			return;
		}
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		Color color = PlayColors.Primary(ThemeName);
		using (LinearGradientBrush brush = new LinearGradientBrush(base.ClientRectangle, Color.FromArgb(14, 19, 31), PlayColors.Deep(ThemeName), 60f))
		{
			graphics.FillRectangle(brush, base.ClientRectangle);
		}
		for (int i = 0; i < 55; i++)
		{
			float x = (float)(i * 173 % 997) / 997f * (float)base.Width;
			float y = (float)(i * 97 % 509) / 509f * (float)base.Height;
			int alpha = 60 + (int)(35.0 * (1.0 + Math.Sin(Phase + (double)i)));
			using SolidBrush brush2 = new SolidBrush(Color.FromArgb(alpha, 195, 215, 250));
			graphics.FillEllipse(brush2, x, y, (i % 4 == 0) ? 3f : 1.5f, (i % 4 == 0) ? 3f : 1.5f);
		}
		float num = (float)base.Width / 2f;
		float num2 = (float)base.Height * 0.46f;
		float num3 = (float)Math.Min(base.Width, base.Height) * 0.24f;
		if (Scene == "Aurora")
		{
			for (int j = 0; j < 5; j++)
			{
				List<PointF> list = new List<PointF>();
				for (int k = 0; k <= base.Width; k += 8)
				{
					list.Add(new PointF(k, (float)((double)base.Height * 0.5 + Math.Sin((double)k * 0.009 + Phase + (double)j * 0.4) * (40.0 + Cpu * 0.5) + (double)(j * 23))));
				}
				using Pen pen = new Pen(Color.FromArgb(45 + j * 12, color), 12f);
				graphics.DrawLines(pen, list.ToArray());
			}
		}
		else if (Scene == "Pixel garden")
		{
			for (int l = 0; l < 20; l++)
			{
				float num4 = (float)(base.Width * (l + 1)) / 21f;
				float num5 = (float)(35.0 + (1.0 + Math.Sin((double)l * 0.7 + Phase)) * (40.0 + Cpu * 0.65));
				for (int m = 0; (float)m < num5 / 12f; m++)
				{
					using SolidBrush brush3 = new SolidBrush(Color.FromArgb(70 + Math.Min(150, m * 8), color));
					graphics.FillRectangle(brush3, num4 - 7f, base.Height - 40 - m * 12, 9f, 9f);
				}
			}
		}
		else
		{
			for (int n = 0; n < 3; n++)
			{
				float num6 = num3 + (float)(n * 24);
				using (Pen pen2 = new Pen(Color.FromArgb(35 + n * 12, color), 1f))
				{
					graphics.DrawEllipse(pen2, num - num6, num2 - num6 * 0.67f, num6 * 2f, num6 * 1.34f);
				}
				double num7 = Phase * (0.45 + Cpu / 160.0) + (double)(n * 2);
				float num8 = num + (float)Math.Cos(num7) * num6;
				float num9 = num2 + (float)Math.Sin(num7) * num6 * 0.67f;
				using SolidBrush brush4 = new SolidBrush((n == 1) ? Color.FromArgb(103, 227, 221) : color);
				graphics.FillEllipse(brush4, num8 - 6f, num9 - 6f, 12f, 12f);
			}
		}
		using (SolidBrush brush5 = new SolidBrush(Color.FromArgb(210, 18, 25, 42)))
		{
			graphics.FillEllipse(brush5, num - num3 * 0.68f, num2 - num3 * 0.68f, num3 * 1.36f, num3 * 1.36f);
		}
		if (TimerMode)
		{
			using (Pen pen3 = new Pen(Color.FromArgb(55, color), 5f))
			{
				graphics.DrawEllipse(pen3, num - num3 * 0.68f, num2 - num3 * 0.68f, num3 * 1.36f, num3 * 1.36f);
			}
			if (Progress > 0.0)
			{
				using Pen pen4 = new Pen(color, 5f);
				graphics.DrawArc(pen4, num - num3 * 0.68f, num2 - num3 * 0.68f, num3 * 1.36f, num3 * 1.36f, -90f, (float)(360.0 * Math.Min(1.0, Progress)));
			}
		}
		using (Font font = new Font("Segoe UI", TimerMode ? 30 : 23, FontStyle.Bold))
		{
			TextRenderer.DrawText(graphics, CenterTitle, font, new Rectangle((int)(num - num3), (int)num2 - 35, (int)(num3 * 2f), 52), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter);
		}
		using (Font font2 = new Font("Segoe UI", 9f))
		{
			TextRenderer.DrawText(graphics, CenterValue, font2, new Rectangle((int)(num - num3 - 60f), (int)num2 + 21, (int)(num3 * 2f + 120f), 30), color, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix);
		}
		if (!TimerMode)
		{
			string text = (HasData ? ("CPU " + Cpu.ToString("F1") + "%     •     RAM " + Ram.ToString("F1") + "%") : "Waiting for a real system sample");
			using (Font font3 = new Font("Segoe UI", 13f))
			{
				TextRenderer.DrawText(graphics, text, font3, new Rectangle(12, base.Height - 75, base.Width - 24, 30), Color.White, TextFormatFlags.HorizontalCenter);
			}
			using Font font4 = new Font("Segoe UI", 9f);
			TextRenderer.DrawText(graphics, "Decorative motion follows CPU load · not a benchmark · click for stardust", font4, new Rectangle(12, base.Height - 38, base.Width - 24, 25), Color.FromArgb(170, 190, 215), TextFormatFlags.EndEllipsis | TextFormatFlags.HorizontalCenter);
		}
		foreach (Spark spark in sparks)
		{
			using SolidBrush brush6 = new SolidBrush(Color.FromArgb(Math.Min(255, spark.Life * 5), color));
			graphics.FillEllipse(brush6, spark.X, spark.Y, 5f, 5f);
		}
	}
}
