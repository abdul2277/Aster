using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Aster;

internal sealed class HorizonSession : IDisposable
{
	private struct Rect
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;
	}

	private readonly HorizonSettings options;

	private readonly Action<string> navigate;

	private readonly Func<string> meters;

	private readonly Action stopped;

	private readonly List<HorizonAction> actions = new List<HorizonAction>();

	internal HorizonSurface Bar;

	internal HorizonSurface Dock;

	internal Form Launcher;

	internal Timer Ticker;

	private NotifyIcon tray;

	private ContextMenuStrip trayMenu;

	private Label readout;

	private Rectangle lastArea;

	private bool disposed;

	private int ticks;

	public bool IsDisposed => disposed;

	[DllImport("user32.dll")]
	private static extern IntPtr GetForegroundWindow();

	[DllImport("user32.dll")]
	private static extern bool GetWindowRect(IntPtr handle, out Rect rect);

	[DllImport("user32.dll")]
	private static extern IntPtr GetShellWindow();

	[DllImport("user32.dll")]
	private static extern IntPtr GetDesktopWindow();

	public HorizonSession(HorizonSettings settings, Action<string> open, Func<string> live, Action onStop)
	{
		HorizonStore.Validate(settings);
		options = HorizonStore.Clone(settings);
		navigate = open;
		meters = live;
		stopped = onStop;
	}

	internal Screen SelectedScreen()
	{
		return Screen.AllScreens.FirstOrDefault((Screen s) => s.DeviceName == options.Display) ?? Screen.PrimaryScreen;
	}

	private Button MakeButton(string text, Action click, int width)
	{
		Button button = new Button();
		button.Text = text;
		button.AccessibleName = text;
		button.Width = width;
		button.Height = 34;
		button.Margin = new Padding(4);
		button.FlatStyle = FlatStyle.Flat;
		button.BackColor = Color.FromArgb(32, 41, 59);
		button.ForeColor = HorizonStore.Accents[options.Theme];
		button.UseMnemonic = false;
		Button button2 = button;
		button2.FlatAppearance.BorderSize = 0;
		button2.Click += delegate
		{
			try
			{
				click();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Could not complete this action: " + ex.Message, "Aster Horizon");
			}
		};
		return button2;
	}

	private void Add(string name, Action run)
	{
		actions.Add(new HorizonAction
		{
			Name = name,
			Run = run
		});
	}

	private static void Launch(string target)
	{
		ProcessStartInfo processStartInfo = new ProcessStartInfo(target);
		processStartInfo.UseShellExecute = true;
		Process.Start(processStartInfo);
	}

	public void Start()
	{
		if (disposed)
		{
			throw new ObjectDisposedException("Horizon");
		}
		if (Bar != null)
		{
			return;
		}
		try
		{
			string[] array = new string[10] { "Summary", "Processes", "Performance", "Wallpaper studio", "Themes", "Focus timer", "Windows advisor", "Desktop mods", "Horizon desktop", "Tools hub" };
			foreach (string text in array)
			{
				string page = text;
				Add(page, delegate
				{
					navigate(page);
				});
			}
			Add("File Explorer", delegate
			{
				Launch("explorer.exe");
			});
			Add("Calculator", delegate
			{
				Launch("calc.exe");
			});
			Add("Notepad", delegate
			{
				Launch("notepad.exe");
			});
			Add("Windows sound settings", delegate
			{
				Launch("ms-settings:sound");
			});
			Add("Windows network settings", delegate
			{
				Launch("ms-settings:network");
			});
			Add("Windows display settings", delegate
			{
				Launch("ms-settings:display");
			});
			foreach (HorizonPin pin2 in options.Pins)
			{
				HorizonPin pin = pin2;
				Add(pin.Name, delegate
				{
					if (!File.Exists(pin.Path))
					{
						throw new FileNotFoundException("This pinned app was moved or removed. Edit your pins in Horizon desktop.");
					}
					Launch(pin.Path);
				});
			}
			Bar = new HorizonSurface
			{
				Name = "HorizonBar",
				Height = 44,
				Opacity = (double)options.Opacity / 100.0
			};
			Button button = MakeButton("✦ ASTER", ShowLauncher, 110);
			button.Dock = DockStyle.Left;
			Bar.Controls.Add(button);
			Button button2 = MakeButton("Stop", delegate
			{
				Dispose();
				stopped();
			}, 65);
			button2.Dock = DockStyle.Right;
			Bar.Controls.Add(button2);
			Button button3 = MakeButton("Customize", delegate
			{
				navigate("Horizon desktop");
			}, 110);
			button3.Dock = DockStyle.Right;
			Bar.Controls.Add(button3);
			readout = new Label
			{
				Dock = DockStyle.Fill,
				TextAlign = ContentAlignment.MiddleCenter,
				ForeColor = HorizonStore.Accents[options.Theme],
				AutoEllipsis = true
			};
			Bar.Controls.Add(readout);
			readout.BringToFront();
			Dock = new HorizonSurface
			{
				Name = "HorizonDock",
				Height = ((options.Size == 0) ? 52 : ((options.Size == 1) ? 64 : 76)),
				Opacity = (double)options.Opacity / 100.0
			};
			FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel();
			flowLayoutPanel.Dock = DockStyle.Fill;
			flowLayoutPanel.AutoScroll = true;
			flowLayoutPanel.WrapContents = false;
			flowLayoutPanel.Padding = new Padding(6);
			FlowLayoutPanel flowLayoutPanel2 = flowLayoutPanel;
			Dock.Controls.Add(flowLayoutPanel2);
			int width = ((options.Size == 0) ? 84 : ((options.Size == 1) ? 104 : 126));
			flowLayoutPanel2.Controls.Add(MakeButton("✦ Launch", ShowLauncher, width));
			string[] array2 = new string[3] { "Summary", "Wallpaper studio", "Focus timer" };
			foreach (string text2 in array2)
			{
				string page2 = text2;
				flowLayoutPanel2.Controls.Add(MakeButton(page2, delegate
				{
					navigate(page2);
				}, width));
			}
			foreach (HorizonPin pin3 in options.Pins)
			{
				HorizonPin selected = pin3;
				flowLayoutPanel2.Controls.Add(MakeButton(pin3.Name, delegate
				{
					if (!File.Exists(selected.Path))
					{
						throw new FileNotFoundException("This pinned app is missing. Update it in Horizon desktop.");
					}
					Launch(selected.Path);
				}, width));
			}
			flowLayoutPanel2.Controls.Add(MakeButton("Customize", delegate
			{
				navigate("Horizon desktop");
			}, width));
			foreach (Control control in flowLayoutPanel2.Controls)
			{
				control.Height = Dock.Height - 20;
			}
			trayMenu = new ContextMenuStrip();
			trayMenu.Items.Add("Open launcher", null, delegate
			{
				ShowLauncher();
			});
			trayMenu.Items.Add("Customize Horizon", null, delegate
			{
				navigate("Horizon desktop");
			});
			trayMenu.Items.Add("Stop Horizon", null, delegate
			{
				Dispose();
				stopped();
			});
			tray = new NotifyIcon
			{
				Text = "Aster Horizon — desktop controls",
				Icon = SystemIcons.Application,
				ContextMenuStrip = trayMenu,
				Visible = true
			};
			tray.DoubleClick += delegate
			{
				ShowLauncher();
			};
			Bar.FormClosed += delegate
			{
				Dispose();
			};
			Dock.FormClosed += delegate
			{
				Dispose();
			};
			Arrange();
			Bar.Show();
			Dock.Show();
			UpdateReadout();
			Ticker = new Timer
			{
				Interval = 200
			};
			Ticker.Tick += delegate
			{
				Tick();
			};
			Ticker.Start();
		}
		catch
		{
			Dispose();
			throw;
		}
	}

	internal void Arrange()
	{
		Rectangle rectangle = (lastArea = SelectedScreen().WorkingArea);
		Bar.Bounds = new Rectangle(rectangle.Left + 8, rectangle.Top + 8, Math.Max(200, rectangle.Width - 16), 44);
		int num = Math.Min(rectangle.Width - 24, (5 + options.Pins.Count) * ((options.Size == 0) ? 92 : ((options.Size == 1) ? 112 : 134)) + 24);
		Dock.Width = num;
		Dock.Location = new Point(rectangle.Left + (rectangle.Width - num) / 2, rectangle.Bottom - Dock.Height - 8);
	}

	private void UpdateReadout()
	{
		if (!disposed)
		{
			readout.Text = (options.ShowMeters ? (meters() + "    •    ") : "") + DateTime.Now.ToString(options.Clock24 ? "ddd, dd MMM yyyy   HH:mm:ss" : "ddd, dd MMM yyyy   h:mm:ss tt");
		}
	}

	internal static bool ShouldReveal(Rectangle area, Rectangle dock, Point cursor)
	{
		if (!dock.Contains(cursor))
		{
			return new Rectangle(area.Left, area.Bottom - 8, area.Width, 8).Contains(cursor);
		}
		return true;
	}

	private bool Fullscreen()
	{
		if (!options.HideFullscreen)
		{
			return false;
		}
		IntPtr foregroundWindow = GetForegroundWindow();
		if (foregroundWindow == IntPtr.Zero || foregroundWindow == GetShellWindow() || foregroundWindow == GetDesktopWindow() || foregroundWindow == Bar.Handle || foregroundWindow == Dock.Handle || (Launcher != null && !Launcher.IsDisposed && foregroundWindow == Launcher.Handle))
		{
			return false;
		}
		if (!GetWindowRect(foregroundWindow, out var rect))
		{
			return false;
		}
		Rectangle bounds = SelectedScreen().Bounds;
		if (rect.Left <= bounds.Left && rect.Top <= bounds.Top && rect.Right >= bounds.Right)
		{
			return rect.Bottom >= bounds.Bottom;
		}
		return false;
	}

	internal void Tick()
	{
		if (disposed)
		{
			return;
		}
		try
		{
			if (lastArea != SelectedScreen().WorkingArea)
			{
				Arrange();
			}
			bool flag = Fullscreen();
			Bar.Visible = !flag;
			Dock.Visible = !flag && (!options.AutoHide || ShouldReveal(lastArea, Dock.Bounds, Cursor.Position) || Dock.ContainsFocus);
			if (++ticks % 5 == 0)
			{
				UpdateReadout();
			}
		}
		catch (Exception ex)
		{
			Dispose();
			stopped();
			MessageBox.Show("Horizon stopped safely: " + ex.Message, "Aster Horizon");
		}
	}

	internal IEnumerable<HorizonAction> Search(string query)
	{
		return actions.Where((HorizonAction a) => a.Name.IndexOf(query.Trim(), StringComparison.OrdinalIgnoreCase) >= 0);
	}

	public void ShowLauncher()
	{
		if (disposed)
		{
			return;
		}
		if (Launcher != null && !Launcher.IsDisposed)
		{
			Launcher.Activate();
			return;
		}
		Rectangle workingArea = SelectedScreen().WorkingArea;
		Form f = new Form
		{
			Text = "Aster Horizon · Launcher",
			Width = Math.Min(600, workingArea.Width),
			Height = Math.Min(520, workingArea.Height),
			StartPosition = FormStartPosition.Manual,
			BackColor = Color.FromArgb(20, 25, 38),
			ForeColor = Color.White,
			TopMost = true,
			ShowInTaskbar = false,
			Font = new Font("Segoe UI", 12f),
			MinimizeBox = false,
			MaximizeBox = false,
			KeyPreview = true
		};
		Launcher = f;
		f.Location = new Point(workingArea.Left + (workingArea.Width - f.Width) / 2, workingArea.Top + (workingArea.Height - f.Height) / 2);
		TextBox query = new TextBox
		{
			Dock = DockStyle.Top,
			AccessibleName = "Search apps and Aster tools",
			BackColor = Color.FromArgb(35, 45, 63),
			ForeColor = Color.White,
			BorderStyle = BorderStyle.FixedSingle,
			Font = new Font("Segoe UI", 15f)
		};
		ListBox list = new ListBox
		{
			Dock = DockStyle.Fill,
			BackColor = Color.FromArgb(27, 34, 49),
			ForeColor = Color.White,
			BorderStyle = BorderStyle.None,
			ItemHeight = 32,
			DrawMode = DrawMode.OwnerDrawFixed
		};
		list.DrawItem += delegate(object sender, DrawItemEventArgs e)
		{
			if (e.Index >= 0)
			{
				bool flag = (e.State & DrawItemState.Selected) != 0;
				using (SolidBrush brush = new SolidBrush(flag ? Color.FromArgb(48, 61, 84) : list.BackColor))
				{
					e.Graphics.FillRectangle(brush, e.Bounds);
				}
				TextRenderer.DrawText(e.Graphics, list.Items[e.Index].ToString(), list.Font, new Rectangle(e.Bounds.X + 12, e.Bounds.Y, e.Bounds.Width - 24, e.Bounds.Height), flag ? HorizonStore.Accents[options.Theme] : Color.White, TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
				e.DrawFocusRectangle();
			}
		};
		Label label = new Label();
		label.Dock = DockStyle.Bottom;
		label.Height = 32;
		label.Text = "Type to search · Enter to open · Esc to close";
		label.TextAlign = ContentAlignment.MiddleCenter;
		Label value = label;
		f.Controls.Add(list);
		f.Controls.Add(query);
		f.Controls.Add(value);
		Label label2 = new Label();
		label2.Text = "✦  FIND YOUR NEXT ACTION";
		label2.Height = 44;
		label2.Dock = DockStyle.Top;
		label2.TextAlign = ContentAlignment.MiddleLeft;
		label2.ForeColor = HorizonStore.Accents[options.Theme];
		label2.Padding = new Padding(10, 0, 0, 0);
		Label value2 = label2;
		f.Controls.Add(value2);
		Action filter = delegate
		{
			list.Items.Clear();
			list.Items.AddRange(Search(query.Text).Cast<object>().ToArray());
			if (list.Items.Count > 0)
			{
				list.SelectedIndex = 0;
			}
		};
		query.TextChanged += delegate
		{
			filter();
		};
		Action run = delegate
		{
			if (!(list.SelectedItem is HorizonAction horizonAction))
			{
				return;
			}
			try
			{
				horizonAction.Run();
				f.Close();
			}
			catch (Exception ex)
			{
				MessageBox.Show(f, ex.Message, "Unable to open");
			}
		};
		list.DoubleClick += delegate
		{
			run();
		};
		f.KeyDown += delegate(object s, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)
			{
				e.SuppressKeyPress = true;
				f.Close();
			}
			else if (e.KeyCode == Keys.Return)
			{
				e.SuppressKeyPress = true;
				run();
			}
			else if (e.KeyCode == Keys.Down && query.Focused && list.Items.Count > 0)
			{
				e.SuppressKeyPress = true;
				list.Focus();
			}
		};
		filter();
		f.Show();
		query.Focus();
	}

	public void Dispose()
	{
		if (!disposed)
		{
			disposed = true;
			if (Ticker != null)
			{
				Ticker.Stop();
				Ticker.Dispose();
			}
			if (tray != null)
			{
				tray.Visible = false;
				tray.Dispose();
			}
			if (trayMenu != null)
			{
				trayMenu.Dispose();
			}
			if (Launcher != null && !Launcher.IsDisposed)
			{
				Launcher.Dispose();
			}
			if (Bar != null && !Bar.IsDisposed)
			{
				Bar.Dispose();
			}
			if (Dock != null && !Dock.IsDisposed)
			{
				Dock.Dispose();
			}
		}
	}
}
