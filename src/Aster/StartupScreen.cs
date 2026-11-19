using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Aster;

internal sealed class StartupScreen : Form
{
	private readonly Timer frames = new Timer
	{
		Interval = 25
	};

	private readonly Stopwatch elapsed = new Stopwatch();

	private readonly ThemePalette palette;

	private readonly string style;

	private readonly int duration;

	private readonly bool motion;

	private readonly Image badge = SuiteArt.Badge(124);

	private readonly PointF[] stars = new PointF[100];

	private bool finished;

	public StartupScreen(WorkspaceOptions options)
	{
		style = StartupLauncher.Style(options.StartupStyle);
		duration = StartupLauncher.Duration(options.StartupDuration);
		motion = options.Motion;
		palette = ThemePalette.Find(options.ThemeId);
		if (options.ThemeId == "custom")
		{
			try
			{
				palette = CustomTheme.Create(new ThemeRecipe
				{
					Background = options.CustomBase,
					Accent = options.CustomAccent
				});
			}
			catch
			{
			}
		}
		Text = "Welcome to Aster";
		base.Icon = SuiteArt.AppIcon();
		base.ClientSize = new Size(820, 510);
		base.FormBorderStyle = FormBorderStyle.FixedDialog;
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		base.StartPosition = FormStartPosition.CenterScreen;
		DoubleBuffered = true;
		BackColor = palette.Background;
		base.KeyPreview = true;
		base.AutoScaleMode = AutoScaleMode.None;
		Random random = new Random(74);
		for (int i = 0; i < stars.Length; i++)
		{
			ref PointF reference = ref stars[i];
			reference = new PointF((float)random.NextDouble(), (float)random.NextDouble());
		}
		Button button = new Button
		{
			Text = "Open Aster  →",
			AccessibleName = "Skip introduction and open Aster",
			Size = new Size(152, 38),
			Location = new Point(640, 445),
			FlatStyle = FlatStyle.Flat,
			BackColor = palette.Raised,
			ForeColor = palette.Text,
			TabIndex = 0
		};
		button.FlatAppearance.BorderColor = palette.Border;
		button.Click += delegate
		{
			Complete();
		};
		base.Controls.Add(button);
		base.AcceptButton = button;
		base.KeyDown += delegate(object s, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)
			{
				e.Handled = true;
				Complete();
			}
		};
		frames.Tick += delegate
		{
			if (elapsed.ElapsedMilliseconds >= duration)
			{
				Complete();
			}
			else
			{
				Invalidate();
			}
		};
		base.Shown += delegate
		{
			elapsed.Start();
			frames.Start();
		};
	}

	private void Complete()
	{
		if (!finished)
		{
			finished = true;
			frames.Stop();
			base.DialogResult = DialogResult.OK;
			Close();
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			frames.Dispose();
			badge.Dispose();
		}
		base.Dispose(disposing);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		Render(e.Graphics, base.ClientRectangle, elapsed.Elapsed.TotalSeconds);
	}

	internal void Render(Graphics g, Rectangle bounds, double seconds)
	{
		g.SmoothingMode = SmoothingMode.AntiAlias;
		GraphicsState gstate = g.Save();
		g.ScaleTransform((float)bounds.Width / 820f, (float)bounds.Height / 510f);
		using (LinearGradientBrush brush = new LinearGradientBrush(new Rectangle(0, 0, 820, 510), palette.Background, palette.Hero, 35f))
		{
			g.FillRectangle(brush, 0, 0, 820, 510);
		}
		double num = (motion ? seconds : 1.5);
		for (int i = 0; i < stars.Length; i++)
		{
			float x = stars[i].X * 820f;
			float y = stars[i].Y * 510f;
			int alpha = 100 + (int)(55.0 * Math.Sin(num * 1.2 + (double)i));
			using SolidBrush brush2 = new SolidBrush(Color.FromArgb(alpha, palette.Accent));
			if (style == "Warp")
			{
				double num2 = (num * 0.35 + (double)stars[i].X) % 1.0;
				double num3 = (double)stars[i].Y * Math.PI * 2.0;
				float num4 = (float)(25.0 + num2 * 540.0);
				x = 410f + (float)Math.Cos(num3) * num4;
				y = 215f + (float)Math.Sin(num3) * num4 * 0.65f;
				using Pen pen = new Pen(Color.FromArgb(alpha, palette.Accent), 1.2f);
				g.DrawLine(pen, x, y, x + (x - 410f) * 0.09f, y + (y - 215f) * 0.09f);
			}
			else
			{
				g.FillEllipse(brush2, x, y, (i % 5 == 0) ? 3f : 1.5f, (i % 5 == 0) ? 3f : 1.5f);
			}
		}
		if (style == "Aurora")
		{
			for (int j = 0; j < 5; j++)
			{
				PointF[] array = new PointF[42];
				for (int k = 0; k < array.Length; k++)
				{
					float x2 = k * 20;
					ref PointF reference = ref array[k];
					reference = new PointF(x2, (float)(210 + j * 13) + (float)Math.Sin((double)k * 0.14 + num * 0.8 + (double)j * 0.35) * 65f);
				}
				using Pen pen2 = new Pen(Color.FromArgb(20 + j * 5, palette.Accent), 20f);
				g.DrawCurve(pen2, array);
			}
		}
		for (int l = 0; l < 3; l++)
		{
			float num5 = 95 + l * 28;
			using (Pen pen3 = new Pen(Color.FromArgb(40 + l * 12, palette.Accent), 1f))
			{
				g.DrawEllipse(pen3, 410f - num5, 205f - num5 * 0.58f, num5 * 2f, num5 * 1.16f);
			}
			double num6 = num * (0.7 + (double)l * 0.2) + (double)(l * 2);
			float num7 = 410f + (float)Math.Cos(num6) * num5;
			float num8 = 205f + (float)Math.Sin(num6) * num5 * 0.58f;
			using (SolidBrush brush3 = new SolidBrush(Color.FromArgb(28, palette.Accent)))
			{
				g.FillEllipse(brush3, num7 - 12f, num8 - 12f, 24f, 24f);
			}
			using SolidBrush brush4 = new SolidBrush(palette.Accent);
			g.FillEllipse(brush4, num7 - 3f, num8 - 3f, 6f, 6f);
		}
		float num9 = (float)Math.Min(1.0, num / 0.7);
		float num10 = 90f + 34f * num9;
		g.DrawImage(badge, 410f - num10 / 2f, 205f - num10 / 2f, num10, num10);
		using (Font font = new Font("Segoe UI", 32f, FontStyle.Bold))
		{
			using SolidBrush brush5 = new SolidBrush(palette.Text);
			StringFormat stringFormat = new StringFormat();
			stringFormat.Alignment = StringAlignment.Center;
			using StringFormat format = stringFormat;
			g.DrawString("A S T E R", font, brush5, new RectangleF(0f, 310f, 820f, 62f), format);
		}
		using (Font font2 = new Font("Segoe UI", 11f))
		{
			using SolidBrush brush6 = new SolidBrush(palette.Muted);
			StringFormat stringFormat2 = new StringFormat();
			stringFormat2.Alignment = StringAlignment.Center;
			using StringFormat format2 = stringFormat2;
			g.DrawString("A little wonder. A clearer workspace.", font2, brush6, new RectangleF(0f, 375f, 820f, 35f), format2);
		}
		using (Font font3 = new Font("Segoe UI", 9f))
		{
			using SolidBrush brush7 = new SolidBrush(palette.Muted);
			g.DrawString(style.ToUpperInvariant() + "  /  WELCOME HOME", font3, brush7, 28f, 458f);
		}
		using (Pen pen4 = new Pen(palette.Border))
		{
			g.DrawLine(pen4, 28, 428, 792, 428);
		}
		g.Restore(gstate);
	}
}
