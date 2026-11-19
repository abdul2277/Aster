using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

internal static class SuiteArt
{
	public static Icon AppIcon()
	{
		try
		{
			return Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
		}
		catch
		{
			return SystemIcons.Application;
		}
	}

	public static Image Badge(int size)
	{
		Bitmap bitmap = new Bitmap(size, size);
		using Graphics graphics = Graphics.FromImage(bitmap);
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		graphics.ScaleTransform((float)size / 128f, (float)size / 128f);
		graphics.Clear(Color.Transparent);
		using (GraphicsPath graphicsPath = new GraphicsPath())
		{
			graphicsPath.AddArc(4, 4, 36, 36, 180f, 90f);
			graphicsPath.AddArc(88, 4, 36, 36, 270f, 90f);
			graphicsPath.AddArc(88, 88, 36, 36, 0f, 90f);
			graphicsPath.AddArc(4, 88, 36, 36, 90f, 90f);
			graphicsPath.CloseFigure();
			using (LinearGradientBrush brush = new LinearGradientBrush(new Point(0, 0), new Point(128, 128), Color.FromArgb(34, 56, 87), Color.FromArgb(17, 24, 42)))
			{
				graphics.FillPath(brush, graphicsPath);
			}
			using Pen pen = new Pen(Color.FromArgb(83, 111, 151), 2f);
			graphics.DrawPath(pen, graphicsPath);
		}
		PointF[] points = new PointF[8]
		{
			new PointF(64f, 18f),
			new PointF(76f, 49f),
			new PointF(107f, 64f),
			new PointF(76f, 77f),
			new PointF(64f, 110f),
			new PointF(51f, 77f),
			new PointF(20f, 64f),
			new PointF(51f, 49f)
		};
		using (LinearGradientBrush brush2 = new LinearGradientBrush(new Point(25, 25), new Point(100, 105), Color.FromArgb(108, 230, 236), Color.FromArgb(171, 155, 255)))
		{
			graphics.FillPolygon(brush2, points);
		}
		using SolidBrush brush3 = new SolidBrush(Color.FromArgb(235, 247, 255));
		graphics.FillEllipse(brush3, 58, 58, 12, 12);
		return bitmap;
	}
}
