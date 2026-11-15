using System.Drawing;
using System.Windows.Forms;

namespace Aster;

internal sealed class HorizonSurface : Form
{
	protected override bool ShowWithoutActivation => true;

	protected override CreateParams CreateParams
	{
		get
		{
			CreateParams createParams = base.CreateParams;
			createParams.ExStyle |= 128;
			return createParams;
		}
	}

	public HorizonSurface()
	{
		base.FormBorderStyle = FormBorderStyle.None;
		base.ShowInTaskbar = false;
		base.StartPosition = FormStartPosition.Manual;
		base.TopMost = true;
		BackColor = Color.FromArgb(20, 25, 38);
		ForeColor = Color.White;
		Font = new Font("Segoe UI", 10f);
		DoubleBuffered = true;
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		using Pen pen = new Pen(Color.FromArgb(64, 78, 101));
		e.Graphics.DrawRectangle(pen, 0, 0, base.Width - 1, base.Height - 1);
	}
}
