using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace Aster;

internal static class DailyArt
{
	public static readonly string[] Names = new string[7] { "Aster star", "Orbit", "Crescent", "Sunrise", "Bloom", "Comet", "Constellation" };

	public static int Wrap(int n)
	{
		return (n % Names.Length + Names.Length) % Names.Length;
	}

	public static DateTime Day(DateTimeOffset utc, bool riyadh)
	{
		if (!riyadh)
		{
			return TimeZoneInfo.ConvertTime(utc, TimeZoneInfo.Local).Date;
		}
		return utc.ToOffset(TimeSpan.FromHours(3.0)).Date;
	}

	public static int Index(DateTime date, bool daily, int choice, int shift)
	{
		if (!daily)
		{
			return Wrap(choice);
		}
		return Wrap((int)(date.Date - new DateTime(2000, 1, 1)).TotalDays + Wrap(shift));
	}

	public static int ShiftFor(DateTime date, int desired)
	{
		return Wrap(desired - (int)(date.Date - new DateTime(2000, 1, 1)).TotalDays);
	}

	public static Bitmap Render(int size, int choice)
	{
		choice = Wrap(choice);
		if (choice == 0)
		{
			return (Bitmap)SuiteArt.Badge(size);
		}
		Bitmap bitmap = new Bitmap(size, size);
		Color[] array = new Color[7]
		{
			Color.Aqua,
			Color.FromArgb(165, 145, 255),
			Color.FromArgb(137, 197, 255),
			Color.FromArgb(255, 181, 109),
			Color.FromArgb(255, 155, 203),
			Color.FromArgb(106, 227, 204),
			Color.FromArgb(163, 190, 255)
		};
		Color color = array[choice];
		using (Graphics graphics = Graphics.FromImage(bitmap))
		{
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			graphics.ScaleTransform((float)size / 128f, (float)size / 128f);
			graphics.Clear(Color.Transparent);
			using (GraphicsPath path = PlayColors.Round(new RectangleF(4f, 4f, 120f, 120f), 20f))
			{
				using (LinearGradientBrush brush = new LinearGradientBrush(new Point(0, 0), new Point(128, 128), Color.FromArgb(36, 49, 76), Color.FromArgb(15, 24, 40)))
				{
					graphics.FillPath(brush, path);
				}
				using Pen pen = new Pen(Color.FromArgb(110, color), 2f);
				graphics.DrawPath(pen, path);
			}
			using SolidBrush brush2 = new SolidBrush(color);
			using SolidBrush brush3 = new SolidBrush(Color.FromArgb(235, 245, 255));
			using Pen pen2 = new Pen(color, 4f);
			switch (choice)
			{
			case 1:
				graphics.FillEllipse(brush2, 43, 43, 42, 42);
				graphics.TranslateTransform(64f, 64f);
				graphics.RotateTransform(-25f);
				graphics.DrawEllipse(pen2, -48, -17, 96, 34);
				graphics.ResetTransform();
				graphics.ScaleTransform((float)size / 128f, (float)size / 128f);
				graphics.FillEllipse(brush3, 91, 26, 7, 7);
				break;
			case 2:
			{
				graphics.FillEllipse(brush2, 28, 24, 70, 78);
				using (SolidBrush brush5 = new SolidBrush(Color.FromArgb(24, 35, 55)))
				{
					graphics.FillEllipse(brush5, 50, 16, 64, 70);
				}
				graphics.FillPolygon(brush3, new Point[8]
				{
					new Point(95, 77),
					new Point(99, 87),
					new Point(110, 91),
					new Point(99, 95),
					new Point(95, 106),
					new Point(91, 95),
					new Point(81, 91),
					new Point(91, 87)
				});
				break;
			}
			case 3:
			{
				graphics.FillEllipse(brush2, 40, 39, 48, 48);
				using (SolidBrush brush4 = new SolidBrush(Color.FromArgb(22, 31, 48)))
				{
					graphics.FillRectangle(brush4, 20, 69, 88, 39);
				}
				graphics.DrawLine(pen2, 22, 72, 106, 72);
				graphics.DrawLine(pen2, 35, 85, 93, 85);
				graphics.DrawLine(pen2, 47, 98, 81, 98);
				for (int j = 0; j < 5; j++)
				{
					double num = (double)(j * 35 + 20) * Math.PI / 180.0;
					graphics.DrawLine(pen2, 64f + (float)Math.Cos(num) * 33f, 63f - (float)Math.Sin(num) * 33f, 64f + (float)Math.Cos(num) * 45f, 63f - (float)Math.Sin(num) * 45f);
				}
				break;
			}
			case 4:
			{
				for (int k = 0; k < 6; k++)
				{
					graphics.TranslateTransform(64f, 64f);
					graphics.RotateTransform(k * 60);
					graphics.FillEllipse(brush2, -12, -43, 24, 44);
					graphics.ResetTransform();
					graphics.ScaleTransform((float)size / 128f, (float)size / 128f);
				}
				graphics.FillEllipse(brush3, 51, 51, 26, 26);
				break;
			}
			case 5:
				graphics.FillPolygon(brush2, new Point[3]
				{
					new Point(22, 24),
					new Point(95, 55),
					new Point(72, 87)
				});
				graphics.FillEllipse(brush2, 57, 52, 43, 43);
				graphics.DrawLine(pen2, 21, 47, 44, 68);
				graphics.DrawLine(pen2, 46, 21, 66, 39);
				graphics.FillEllipse(brush3, 70, 64, 16, 16);
				break;
			default:
			{
				Point[] array2 = new Point[5]
				{
					new Point(29, 89),
					new Point(47, 41),
					new Point(80, 60),
					new Point(101, 28),
					new Point(91, 96)
				};
				graphics.DrawLines(pen2, array2);
				Point[] array3 = array2;
				for (int i = 0; i < array3.Length; i++)
				{
					Point point = array3[i];
					graphics.FillEllipse(brush3, point.X - 5, point.Y - 5, 10, 10);
				}
				graphics.FillEllipse(brush2, 25, 24, 4, 4);
				graphics.FillEllipse(brush2, 55, 103, 4, 4);
				break;
			}
			}
		}
		return bitmap;
	}

	[DllImport("user32.dll")]
	private static extern bool DestroyIcon(IntPtr icon);

	public static Icon MakeIcon(int choice)
	{
		using Bitmap bitmap = Render(64, choice);
		IntPtr hicon = bitmap.GetHicon();
		try
		{
			using Icon icon = Icon.FromHandle(hicon);
			return (Icon)icon.Clone();
		}
		finally
		{
			DestroyIcon(hicon);
		}
	}
}
