using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using AsterAlerts;

namespace Aster;

internal partial class MainWindow : Form
{
	private Color bg = Color.FromArgb(16, 20, 27);

	private Color panel = Color.FromArgb(24, 29, 37);

	private Color muted = Color.FromArgb(139, 153, 171);

	private Color cyan = Color.FromArgb(55, 197, 231);

	private Panel content;

	private Label heading;

	private Label sub;

	private Label status;

	private FlowLayoutPanel nav;

	private FlowLayoutPanel tools;

	private TextBox search;

	private Button pause;

	private System.Windows.Forms.Timer timer;

	private Collector collector = new Collector();

	private BackgroundWorker sampler = new BackgroundWorker();

	private Snapshot current;

	private string page = "Summary";

	private bool paused;

	private DataGridView grid;

	private Graph cpu;

	private Graph ram;

	private Graph net;

	private Graph disk;

	private Dictionary<string, Button> buttons = new Dictionary<string, Button>();

	private List<Snapshot> recording = new List<Snapshot>();

	private bool record;

	private Table inventory;

	private string cpuName = "";

	private string gpuInfo = "GPU · checking Windows counters";

	private Label gpuLabel;

	private bool inventoryBusy;

	private int inventoryGeneration;

	private int count;

	private bool gpuBusy;

	private double gpuValue = double.NaN;

	private string gpuNote = "Waiting for GPU counters";

	private List<Snapshot> history = new List<Snapshot>();

	private Snapshot lastView;

	private Dictionary<string, Tuple<double, long>> appHistory = new Dictionary<string, Tuple<double, long>>();

	private DateTime lastHistory;

	private Label recordingState;

	private Button recordingButton;

	private HorizonSession horizon;

	private bool horizonCloseHook;

	private int onlineProgress;

	private OnlineRelease onlineRelease;

	private string onlineStatus = "Check online for a newer version. No update file is needed.";

	private CancellationTokenSource updateCancellation;

	private bool pendingLoaded;

	private string installedOnlineVersion;

	private bool restartAfterUpdate = true;

	internal Func<string, bool> ConfirmOnlineUpdate;

	internal Action<string> LaunchUpdatedForTest;

	private readonly ComparisonCapture comparisonCapture = new ComparisonCapture();

	private readonly List<string> sessionTimeline = new List<string>();

	private ListBox timelineList;

	private TextBox comparisonText;

	private Label comparisonState;

	private Label maintenanceState;

	private bool? lastPaused;

	private readonly PressureRule cpuPressure = new PressureRule();

	private readonly PressureRule ramPressure = new PressureRule();

	private readonly List<string> pressureEvents = new List<string>();

	private int unreadPressure;

	private Label commandStatus;

	private ListBox pressureList;

	private string installedUpdateFolder;

	private bool updateBusy;

	private bool updateCloseHook;

	private AlertPanel airDefensePanel;

	private TabControl airDefenseTabs;

	private EmbeddedVideo airDefenseVideo;

	private readonly List<string> recentTools = new List<string>();

	private Graph summaryCpu;

	private Graph summaryRam;

	private DataGridView topProcesses;

	private Label liveOverview;

	private Label liveHardware;

	private Label sensorStrip;

	private List<GpuSensor> sensors = new List<GpuSensor>();

	private DateTime sensorTime;

	private string storageOverview = "Storage: reading device inventory…";

	private Form compact;

	private WorkspaceOptions preferences = WorkspaceOptions.Read();

	private InsightTracker insightTracker = new InsightTracker();

	private Snapshot baseline;

	private Label insightText;

	private ListBox insightEvents;

	private MetricTile tileCpu;

	private MetricTile tileRam;

	private MetricTile tileGpu;

	private MetricTile tileNet;

	private TextBox navFilter;

	private Button pinButton;

	private Panel sidebar;

	private string lastInsightText = "";

	private bool distractionFree;

	private MetricTile focusCpu;

	private MetricTile focusRam;

	private MetricTile focusProcesses;

	private MetricTile focusUptime;

	private Label focusReadings;

	private System.Windows.Forms.Timer funClock;

	private Stopwatch funTime = new Stopwatch();

	private SessionCountdown countdown = new SessionCountdown();

	private OrbitCanvas funCanvas;

	private Label timerState;

	private Button timerStart;

	private NumericUpDown timerMinutes;

	private bool timerDone;

	private Label greetingTitle;

	private Label greetingDate;

	private Label greetingHijri;

	private Label greetingOccasion;

	private System.Windows.Forms.Timer greetingTimer;

	private Label dailyLogo;

	private Label pictureDescription;

	private PictureBox bannerPicture;

	private PictureBox picturePreview;

	private CheckBox dailyPictureCheck;

	private bool changingPictureControls;

	private int shownPicture = -1;

	private Icon dynamicPictureIcon;

	private PersonalData personal = new PersonalData();

	private bool personalDirty;

	private string personalLoadError;

	private PersonalTask removedTask;

	private int removedTaskIndex;

	private static readonly string[] HomeLabels = new string[7] { "Personal welcome & calendar", "Live CPU / RAM / GPU / network cards", "Quick action buttons", "CPU and memory history charts", "Busy processes and hardware overview", "GPU sensor readings", "Network and disk activity details" };

	private readonly List<Form> desktopMods = new List<Form>();

	private Color modAccent = Color.DeepSkyBlue;

	private string modTrail = "Orbit rings";

	private bool modCloseHook;

	private int modPalette;

	private int modScreen;

	private bool modEco = true;

	public MainWindow()
	{
		Text = "Aster · System workspace";
		base.Size = new Size(1480, 960);
		MinimumSize = new Size(1050, 700);
		base.StartPosition = FormStartPosition.CenterScreen;
		BackColor = bg;
		ForeColor = Color.White;
		Font = new Font("Segoe UI", 10f);
		base.Icon = SuiteArt.AppIcon();
		Panel panel = new Panel
		{
			Dock = DockStyle.Left,
			Width = 238,
			BackColor = Color.FromArgb(21, 26, 34),
			Padding = new Padding(16)
		};
		base.Controls.Add(panel);
		Label label = new Label
		{
			Text = "ASTER",
			Dock = DockStyle.Top,
			Height = 72,
			Font = new Font("Segoe UI", 22f, FontStyle.Bold),
			ForeColor = cyan,
			TextAlign = ContentAlignment.MiddleLeft
		};
		label.Image = SuiteArt.Badge(46);
		label.ImageAlign = ContentAlignment.MiddleLeft;
		label.TextAlign = ContentAlignment.MiddleRight;
		panel.Controls.Add(label);
		nav = new FlowLayoutPanel
		{
			Dock = DockStyle.Fill,
			FlowDirection = FlowDirection.TopDown,
			WrapContents = false,
			AutoScroll = true,
			Padding = new Padding(0, 14, 0, 0)
		};
		panel.Controls.Add(nav);
		nav.BringToFront();
		Label value = new Label
		{
			Dock = DockStyle.Bottom,
			Height = 60,
			Text = "LOCAL SYSTEM MONITOR\r\nYour data stays on this PC",
			ForeColor = muted,
			Font = new Font("Segoe UI", 8f),
			TextAlign = ContentAlignment.BottomLeft
		};
		panel.Controls.Add(value);
		string[] array = new string[54]
		{
			"Space finder", "Ping monitor", "Power timer", "Horizon desktop", "Desktop mods", "Session timeline", "Performance compare", "Maintenance planner", "Command Center", "Performance alerts", "Diagnostics bundle", "Update Aster", "Video player",
			"Air Defense feed", "Tools hub", "System check", "Summary", "Home layout", "Backup center", "Hardware compare", "Network lab", "Theme studio", "Notebook",
			"Checklist", "Themes", "Playground", "Focus timer", "Focus", "Help center", "Hardware report", "Insights", "Preferences", "Windows advisor",
			"Compact monitor", "Recovery toolbox", "Wallpaper studio", "Performance", "Processes", "CPU specs", "GPU", "Memory specs", "Storage specs", "Motherboard & BIOS",
			"Network adapters", "System info", "App history", "Startup apps", "Users", "Services", "Connections", "Installed apps", "Drivers", "Disk space",
			"Flight recorder"
		};
		foreach (string text in array)
		{
			string target = text;
			Button button = Button(text);
			button.Width = 177;
			button.Height = 36;
			button.TextAlign = ContentAlignment.MiddleLeft;
			button.Click += delegate
			{
				Navigate(target);
			};
			nav.Controls.Add(button);
			buttons[text] = button;
		}
		Panel panel2 = new Panel
		{
			Dock = DockStyle.Fill,
			Padding = new Padding(28, 20, 28, 14)
		};
		base.Controls.Add(panel2);
		panel2.BringToFront();
		Panel panel3 = new Panel
		{
			Dock = DockStyle.Top,
			Height = 80
		};
		heading = new Label
		{
			Text = "Summary",
			Location = new Point(0, 0),
			AutoSize = true,
			Font = new Font("Segoe UI", 27f)
		};
		sub = new Label
		{
			Text = "A clear view of what your computer is doing.",
			Location = new Point(2, 49),
			AutoSize = true,
			ForeColor = muted,
			Font = new Font("Segoe UI", 9f)
		};
		panel3.Controls.Add(heading);
		panel3.Controls.Add(sub);
		panel2.Controls.Add(panel3);
		tools = new FlowLayoutPanel
		{
			Dock = DockStyle.Top,
			Height = 47,
			WrapContents = false
		};
		search = new TextBox
		{
			Width = 250,
			BackColor = this.panel,
			ForeColor = Color.White,
			BorderStyle = BorderStyle.FixedSingle,
			Margin = new Padding(0, 6, 12, 0)
		};
		search.TextChanged += delegate
		{
			RefreshTable();
		};
		tools.Controls.Add(search);
		pause = Button("Pause");
		pause.Click += delegate
		{
			paused = !paused;
			pause.Text = (paused ? "Resume" : "Pause");
			UpdateView();
		};
		tools.Controls.Add(pause);
		Button button2 = Button("Refresh");
		button2.Click += delegate
		{
			if (IsInventory())
			{
				LoadInventory();
			}
			else
			{
				StartSample();
			}
		};
		tools.Controls.Add(button2);
		Button button3 = Button("Run new task");
		button3.Click += delegate
		{
			RunTask();
		};
		tools.Controls.Add(button3);
		Button button4 = Button("Export CSV");
		button4.Click += delegate
		{
			Export();
		};
		tools.Controls.Add(button4);
		Button button5 = Button("End process");
		button5.Name = "End";
		button5.ForeColor = Color.FromArgb(244, 145, 136);
		button5.Click += delegate
		{
			EndProcess();
		};
		tools.Controls.Add(button5);
		panel2.Controls.Add(tools);
		tools.BringToFront();
		status = new Label
		{
			Dock = DockStyle.Bottom,
			Height = 28,
			ForeColor = muted,
			Font = new Font("Segoe UI", 9f),
			Text = "Connecting to local Windows providers…",
			TextAlign = ContentAlignment.MiddleLeft
		};
		panel2.Controls.Add(status);
		content = new Panel
		{
			Dock = DockStyle.Fill,
			Padding = new Padding(0, 10, 0, 0)
		};
		panel2.Controls.Add(content);
		content.BringToFront();
		tools.BringToFront();
		panel3.BringToFront();
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			ColumnCount = 1,
			RowCount = 4,
			Margin = new Padding(0),
			Padding = new Padding(0)
		};
		tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 80f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 47f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));
		panel2.Controls.Clear();
		panel2.Controls.Add(tableLayoutPanel);
		panel3.Dock = (tools.Dock = (content.Dock = (status.Dock = DockStyle.Fill)));
		panel3.Margin = (tools.Margin = (content.Margin = (status.Margin = new Padding(0))));
		tableLayoutPanel.Controls.Add(panel3, 0, 0);
		tableLayoutPanel.Controls.Add(tools, 0, 1);
		tableLayoutPanel.Controls.Add(content, 0, 2);
		tableLayoutPanel.Controls.Add(status, 0, 3);
		foreach (Button value2 in buttons.Values)
		{
			value2.AutoSize = false;
			value2.Width = 160;
			value2.Margin = new Padding(0, 3, 0, 3);
		}
		nav.HorizontalScroll.Enabled = false;
		nav.HorizontalScroll.Visible = false;
		sampler.DoWork += delegate(object s, DoWorkEventArgs e)
		{
			e.Result = collector.Sample();
		};
		sampler.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs e)
		{
			if (!base.IsDisposed)
			{
				if (e.Error != null)
				{
					status.Text = "Sampling error: " + e.Error.Message;
				}
				else
				{
					current = (Snapshot)e.Result;
					TrackInsights(current);
					count++;
					if (record && !paused)
					{
						recording.Add(current);
						if (recording.Count >= 1800)
						{
							record = false;
							MessageBox.Show(this, "Recording stopped at 1,800 samples. Export it from Flight recorder.");
						}
					}
					UpdateView();
					if (count % 5 == 1)
					{
						SampleGpu();
					}
				}
			}
		};
		timer = new System.Windows.Forms.Timer
		{
			Interval = 1000
		};
		timer.Tick += delegate
		{
			TickSessionTools();
			if (!paused)
			{
				StartSample();
			}
		};
		timer.Start();
		SetupWorkspace(panel, panel3);
		SetupFun();
		SetupGreeting();
		SetupPersonalWorkspace();
		Navigate("Command Center");
		base.Shown += delegate
		{
			if (!preferences.TourSeen)
			{
				preferences.TourSeen = true;
				SavePreferences();
				using WelcomeTour welcomeTour = new WelcomeTour(this.panel, Color.FromArgb(204, 215, 229), muted, Color.FromArgb(94, 196, 214));
				welcomeTour.ShowDialog(this);
			}
		};
		SetupShortcuts();
		StartSample();
		LoadHardware();
		ReadStorageOverview();
		base.FormClosed += delegate
		{
			timer.Stop();
		};
	}

	private Button Button(string text)
	{
		ThemeButton themeButton = new ThemeButton();
		themeButton.Text = text;
		themeButton.UseMnemonic = false;
		themeButton.FlatStyle = FlatStyle.Flat;
		themeButton.BackColor = panel;
		themeButton.ForeColor = Color.FromArgb(204, 215, 229);
		themeButton.Height = 32;
		themeButton.AutoSize = true;
		themeButton.Padding = new Padding(9, 0, 9, 0);
		themeButton.Margin = new Padding(0, 3, 8, 3);
		themeButton.FlatAppearance.BorderSize = 0;
		return themeButton;
	}

	private bool IsInventory()
	{
		if (IsNextPage(page))
		{
			return false;
		}
		if (!(page == "Horizon desktop") && !(page == "Desktop mods") && !(page == "Session timeline") && !(page == "Performance compare") && !(page == "Maintenance planner") && !(page == "Command Center") && !(page == "Performance alerts") && !(page == "Diagnostics bundle") && !(page == "Update Aster") && !(page == "Video player") && !(page == "Air Defense feed") && !(page == "Tools hub") && !(page == "System check") && !(page == "Summary") && !(page == "Performance") && !(page == "Processes") && !(page == "App history") && !(page == "Flight recorder") && !(page == "Recovery toolbox") && !(page == "Wallpaper studio") && !(page == "Windows advisor") && !(page == "Compact monitor") && !(page == "Insights") && !(page == "Preferences") && !(page == "Focus") && !(page == "Help center") && !(page == "Hardware report") && !(page == "Playground") && !(page == "Focus timer") && !(page == "Themes") && !(page == "Theme studio") && !(page == "Notebook") && !(page == "Checklist") && !(page == "Network lab") && !(page == "Home layout") && !(page == "Backup center"))
		{
			return !(page == "Hardware compare");
		}
		return false;
	}

	private void StartSample()
	{
		if (!sampler.IsBusy)
		{
			sampler.RunWorkerAsync();
		}
	}

	private void Navigate(string name)
	{
		CrashLog.Page = name;
		DetachAirDefense();
		RememberTool(name);
		page = name;
		inventoryGeneration++;
		heading.Text = name;
		grid = null;
		search.Text = "";
		foreach (KeyValuePair<string, Button> button in buttons)
		{
			button.Value.BackColor = ((button.Key == name) ? Color.FromArgb(31, 65, 80) : panel);
		}
		Control[] array = content.Controls.Cast<Control>().ToArray();
		foreach (Control control in array)
		{
			control.Dispose();
		}
		content.Controls.Clear();
		grid = null;
		summaryCpu = (summaryRam = null);
		topProcesses = null;
		liveOverview = (liveHardware = (sensorStrip = null));
		cpu = (ram = (net = (disk = null)));
		gpuLabel = null;
		inventory = null;
		inventoryBusy = false;
		TextBox textBox = search;
		int visible;
		switch (IsNextPage(name) ? "Summary" : name)
		{
		default:
			visible = ((!(name == "Hardware compare")) ? 1 : 0);
			break;
		case "Horizon desktop":
		case "Desktop mods":
		case "Summary":
		case "Performance":
		case "Flight recorder":
		case "Recovery toolbox":
		case "Wallpaper studio":
		case "Windows advisor":
		case "Compact monitor":
		case "Insights":
		case "Preferences":
		case "Focus":
		case "Help center":
		case "Hardware report":
		case "Playground":
		case "Focus timer":
		case "Themes":
		case "Theme studio":
		case "Notebook":
		case "Checklist":
		case "Network lab":
		case "Home layout":
		case "Backup center":
			visible = 0;
			break;
		}
		textBox.Visible = (byte)visible != 0;
		tools.Controls[tools.Controls.Count - 1].Visible = name == "Processes";
		sub.Text = "Live local data · refresh approximately every second";
		if (BuildNextPage(name))
		{
			StyleView();
			UpdateView();
			return;
		}
		switch (name)
		{
		case "Horizon desktop":
			BuildHorizon();
			break;
		case "Desktop mods":
			BuildDesktopMods();
			break;
		case "Session timeline":
			BuildSessionTimeline();
			break;
		case "Performance compare":
			BuildPerformanceCompare();
			break;
		case "Maintenance planner":
			BuildMaintenancePlanner();
			break;
		case "Command Center":
			BuildCommandCenter();
			break;
		case "Performance alerts":
			BuildPressureAlerts();
			break;
		case "Diagnostics bundle":
			BuildDiagnosticsBundle();
			break;
		case "Update Aster":
			BuildUpdates();
			break;
		case "Video player":
			BuildVideoPlayer();
			break;
		case "Air Defense feed":
			BuildAirDefense();
			break;
		case "Tools hub":
			BuildToolsHub();
			break;
		case "System check":
			BuildSystemCheck();
			break;
		case "Home layout":
			BuildHomeLayout();
			break;
		case "Backup center":
			BuildBackupCenter();
			break;
		case "Hardware compare":
			BuildHardwareCompare();
			break;
		case "Network lab":
			BuildNetworkLab();
			break;
		case "Theme studio":
			BuildThemeStudio();
			break;
		case "Notebook":
			BuildNotebook();
			break;
		case "Checklist":
			BuildChecklist();
			break;
		case "Themes":
			BuildThemes();
			break;
		case "Playground":
			BuildPlayground();
			break;
		case "Focus timer":
			BuildFocusTimer();
			break;
		case "Help center":
			BuildHelpCenter();
			break;
		case "Hardware report":
			BuildHardwareReport();
			break;
		case "Focus":
			BuildFocus();
			break;
		case "Insights":
			BuildInsights();
			break;
		case "Preferences":
			BuildPreferences();
			break;
		case "Compact monitor":
			BuildCompact();
			break;
		case "Windows advisor":
			BuildAdvisor();
			break;
		case "Recovery toolbox":
			BuildRecovery();
			break;
		case "Wallpaper studio":
			BuildWallpaperLauncher();
			break;
		case "Summary":
			BuildSummary();
			break;
		case "Performance":
			BuildDashboard();
			break;
		case "Flight recorder":
			BuildRecorder();
			break;
		default:
			grid = MakeGrid();
			content.Controls.Add(grid);
			if (IsInventory())
			{
				LoadInventory();
			}
			else
			{
				RefreshTable();
			}
			break;
		}
		StyleView();
		UpdateView();
	}

	private Graph NewGraph(string caption, Color color, bool scale)
	{
		Graph graph = new Graph();
		graph.Dock = DockStyle.Fill;
		graph.Caption = caption;
		graph.Accent = color;
		graph.AutoScale = scale;
		graph.Margin = new Padding(0, 0, 14, 14);
		return graph;
	}

	private void BuildDashboard()
	{
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel();
		tableLayoutPanel.Dock = DockStyle.Fill;
		tableLayoutPanel.ColumnCount = 2;
		tableLayoutPanel.RowCount = 3;
		tableLayoutPanel.BackColor = bg;
		TableLayoutPanel tableLayoutPanel2 = tableLayoutPanel;
		tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58f));
		tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 85f));
		content.Controls.Add(tableLayoutPanel2);
		cpu = NewGraph("CPU", cyan, scale: false);
		ram = NewGraph("MEMORY", Color.FromArgb(188, 133, 245), scale: false);
		net = NewGraph("NETWORK · receive", Color.FromArgb(79, 216, 166), scale: true);
		disk = NewGraph("DISK · read", Color.FromArgb(238, 174, 93), scale: true);
		tableLayoutPanel2.Controls.Add(cpu, 0, 0);
		tableLayoutPanel2.Controls.Add(ram, 1, 0);
		tableLayoutPanel2.Controls.Add(net, 0, 1);
		tableLayoutPanel2.Controls.Add(disk, 1, 1);
		gpuLabel = new Label
		{
			Dock = DockStyle.Fill,
			ForeColor = muted,
			Font = new Font("Segoe UI", 9f),
			Text = "GPU · querying Windows counters",
			Padding = new Padding(10),
			BackColor = panel
		};
		tableLayoutPanel2.Controls.Add(gpuLabel, 0, 2);
		tableLayoutPanel2.SetColumnSpan(gpuLabel, 2);
		foreach (Snapshot item in history)
		{
			AddGraphs(item);
		}
	}

	private void AddGraphs(Snapshot s)
	{
		cpu.Add(s.Cpu);
		ram.Add(s.MemoryPercent);
		net.Add(s.Receive);
		disk.Add(s.DiskRead);
	}

	private void UpdateView()
	{
		RefreshRecorder();
		if (current == null)
		{
			return;
		}
		bool flag = current != lastView;
		if (flag)
		{
			history.Add(current);
			if (history.Count > 60)
			{
				history.RemoveAt(0);
			}
			lastView = current;
		}
		status.Text = ((unreadPressure > 0) ? ("[" + unreadPressure + " performance alerts — open Command Center]    ") : "") + (paused ? "PAUSED" : "● LIVE") + "    " + current.Processes.Count + " processes    " + current.Processes.Sum((Proc p) => p.Threads) + " threads    Up " + TimeSpan.FromMilliseconds(Native.GetTickCount64()).ToString("d\\.hh\\:mm\\:ss") + "    " + current.Time.ToString("HH:mm:ss") + (record ? "    ● RECORDING" : "") + ((current.Warning != "") ? ("    " + current.Warning) : "");
		if (page == "Focus")
		{
			UpdateFocus();
		}
		else if (page == "Summary")
		{
			UpdateSummary(flag);
			UpdateTiles();
		}
		else if (page == "Insights")
		{
			RefreshInsights();
		}
		else if (cpu != null)
		{
			if (flag)
			{
				AddGraphs(current);
			}
			cpu.Value = current.Cpu.ToString("F1") + "%";
			cpu.Hint = Environment.ProcessorCount + " logical processors · last 60 samples";
			ram.Value = current.MemoryPercent.ToString("F1") + "%";
			ram.Hint = Inventory.Bytes(current.Total - current.Available) + " / " + Inventory.Bytes(current.Total) + " · " + Inventory.Bytes(current.Available) + " available";
			net.Value = Inventory.Bytes(current.Receive) + "/s";
			net.Hint = "Send " + Inventory.Bytes(current.Send) + "/s · active adapters combined";
			disk.Value = Inventory.Bytes(current.DiskRead) + (double.IsNaN(current.DiskRead) ? "" : "/s");
			disk.Hint = "Write " + Inventory.Bytes(current.DiskWrite) + "/s · physical disks combined";
			gpuLabel.Text = SensorText();
			cpu.Invalidate();
			ram.Invalidate();
			net.Invalidate();
			disk.Invalidate();
			sub.Text = ((cpuName.Length > 0) ? cpuName : "Live system performance");
		}
		else if (page == "Processes" || page == "App history")
		{
			RefreshTable();
		}
	}

	private DataGridView MakeGrid()
	{
		DataGridView dataGridView = new DataGridView();
		dataGridView.Dock = DockStyle.Fill;
		dataGridView.BackgroundColor = panel;
		dataGridView.BorderStyle = BorderStyle.None;
		dataGridView.ReadOnly = true;
		dataGridView.AllowUserToAddRows = false;
		dataGridView.AllowUserToDeleteRows = false;
		dataGridView.AllowUserToResizeRows = false;
		dataGridView.RowHeadersVisible = false;
		dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
		dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
		dataGridView.MultiSelect = false;
		dataGridView.EnableHeadersVisualStyles = false;
		dataGridView.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
		dataGridView.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
		dataGridView.GridColor = Color.FromArgb(38, 47, 58);
		DataGridView dataGridView2 = dataGridView;
		dataGridView2.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
		{
			BackColor = Color.FromArgb(30, 38, 48),
			ForeColor = muted,
			Padding = new Padding(8),
			Font = new Font("Segoe UI", 9f, FontStyle.Bold)
		};
		dataGridView2.DefaultCellStyle = new DataGridViewCellStyle
		{
			BackColor = panel,
			ForeColor = Color.FromArgb(218, 225, 237),
			SelectionBackColor = Color.FromArgb(32, 66, 83),
			SelectionForeColor = Color.White,
			Padding = new Padding(7),
			Font = new Font("Segoe UI", 9f)
		};
		dataGridView2.RowTemplate.Height = 34;
		dataGridView2.ColumnHeadersHeight = 40;
		dataGridView2.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(21, 27, 35);
		return dataGridView2;
	}

	private void SetColumns(string[] columns)
	{
		if (grid.Columns.Count != columns.Length || grid.Columns.Count <= 0 || !(grid.Columns[0].HeaderText == columns[0]))
		{
			grid.Columns.Clear();
			foreach (string text in columns)
			{
				grid.Columns.Add(text, text);
			}
		}
	}

	private void RefreshTable()
	{
		if (grid == null)
		{
			return;
		}
		if (page == "Processes" || page == "App history")
		{
			if (current == null)
			{
				return;
			}
			if (page == "App history")
			{
				SetColumns(new string[3] { "Application", "Observed CPU time (s)", "Peak working set" });
				if (lastHistory != current.Time)
				{
					double num = ((lastHistory == default(DateTime)) ? 0.0 : Math.Min(10.0, (current.Time - lastHistory).TotalSeconds));
					foreach (Proc process in current.Processes)
					{
						if (!appHistory.TryGetValue(process.Name, out var value))
						{
							value = Tuple.Create(0.0, 0L);
						}
						appHistory[process.Name] = Tuple.Create(value.Item1 + process.Cpu / 100.0 * (double)Environment.ProcessorCount * num, Math.Max(value.Item2, process.Memory));
					}
					lastHistory = current.Time;
				}
				grid.Rows.Clear();
				foreach (KeyValuePair<string, Tuple<double, long>> item in from x in appHistory
					where x.Key.IndexOf(search.Text, StringComparison.OrdinalIgnoreCase) >= 0
					orderby x.Value.Item1 descending
					select x)
				{
					grid.Rows.Add(item.Key, item.Value.Item1.ToString("F1"), Inventory.Bytes(item.Value.Item2));
				}
				sub.Text = "Observed while this view is open in this session; not Windows historical usage.";
				return;
			}
			int num2 = ((grid.SelectedRows.Count > 0 && grid.SelectedRows[0].Tag is Proc) ? ((Proc)grid.SelectedRows[0].Tag).Id : (-1));
			int firstDisplayedScrollingRowIndex = grid.FirstDisplayedScrollingRowIndex;
			string columnName = ((grid.SortedColumn == null) ? "CPU %" : grid.SortedColumn.Name);
			ListSortDirection direction = ((grid.SortOrder != SortOrder.Ascending) ? ListSortDirection.Descending : ListSortDirection.Ascending);
			SetColumns(new string[7] { "Name", "PID", "CPU %", "Memory (MB)", "Threads", "Handles", "Session" });
			grid.Rows.Clear();
			foreach (Proc item2 in current.Processes.Where((Proc p) => p.Name.IndexOf(search.Text, StringComparison.OrdinalIgnoreCase) >= 0 || p.Id.ToString() == search.Text))
			{
				int index = grid.Rows.Add(item2.Name, item2.Id, Math.Round(item2.Cpu, 1), Math.Round((double)item2.Memory / 1048576.0, 1), item2.Threads, item2.Handles, item2.Session);
				grid.Rows[index].Tag = item2;
			}
			grid.Sort(grid.Columns[columnName], direction);
			grid.ClearSelection();
			foreach (DataGridViewRow item3 in (IEnumerable)grid.Rows)
			{
				if (((Proc)item3.Tag).Id == num2)
				{
					item3.Selected = true;
					break;
				}
			}
			if (firstDisplayedScrollingRowIndex >= 0 && firstDisplayedScrollingRowIndex < grid.Rows.Count)
			{
				grid.FirstDisplayedScrollingRowIndex = firstDisplayedScrollingRowIndex;
			}
			sub.Text = "Click a column to sort · search by name or PID · CPU is a share of the entire machine";
		}
		else
		{
			if (inventory == null)
			{
				return;
			}
			SetColumns(inventory.Columns);
			grid.Rows.Clear();
			foreach (string[] item4 in ((page == "GPU") ? GpuRows() : inventory.Rows).Where((string[] r) => string.Join(" ", r).IndexOf(search.Text, StringComparison.OrdinalIgnoreCase) >= 0))
			{
				grid.Rows.Add(item4);
			}
			sub.Text = ((inventory.Note.Length > 0) ? inventory.Note : (inventory.Rows.Count + " entries · click Refresh to update"));
		}
	}

	private void LoadInventory()
	{
		if (inventoryBusy)
		{
			return;
		}
		inventoryBusy = true;
		int generation = inventoryGeneration;
		string target = page;
		sub.Text = "Reading Windows inventory…";
		BackgroundWorker worker = new BackgroundWorker();
		worker.DoWork += delegate(object s, DoWorkEventArgs e)
		{
			e.Result = Inventory.Read(target);
		};
		worker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs e)
		{
			worker.Dispose();
			if (!base.IsDisposed && generation == inventoryGeneration)
			{
				inventoryBusy = false;
				if (e.Error != null)
				{
					sub.Text = e.Error.Message;
				}
				else
				{
					inventory = (Table)e.Result;
					RefreshTable();
				}
			}
		};
		worker.RunWorkerAsync();
	}

	private void LoadHardware()
	{
		BackgroundWorker w = new BackgroundWorker();
		w.DoWork += delegate(object s, DoWorkEventArgs e)
		{
			string text = "";
			string text2 = "";
			try
			{
				using (ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor"))
				{
					foreach (ManagementObject item in managementObjectSearcher.Get())
					{
						using (item)
						{
							text = Convert.ToString(item["Name"]);
						}
					}
				}
				Table table = Inventory.Read("GPU");
				text2 = string.Join("  ·  ", from r in table.Rows
					where r.Length == 3 && r[1] == "Model"
					select r[2]);
			}
			catch
			{
			}
			e.Result = new string[2] { text, text2 };
		};
		w.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs e)
		{
			w.Dispose();
			if (!base.IsDisposed && e.Error == null)
			{
				string[] array = (string[])e.Result;
				cpuName = array[0];
				gpuInfo = "GPU · " + array[1];
			}
		};
		w.RunWorkerAsync();
	}

	private void SampleGpu()
	{
		if (gpuBusy)
		{
			return;
		}
		gpuBusy = true;
		BackgroundWorker w = new BackgroundWorker();
		w.DoWork += delegate(object s, DoWorkEventArgs e)
		{
			List<GpuSensor> item = Nvidia.Read();
			double num = double.NaN;
			try
			{
				using ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT UtilizationPercentage FROM Win32_PerfFormattedData_GPUPerformanceCounters_GPUEngine");
				managementObjectSearcher.Options.Timeout = TimeSpan.FromSeconds(5.0);
				foreach (ManagementObject item2 in managementObjectSearcher.Get())
				{
					using (item2)
					{
						double num2 = Convert.ToDouble(item2["UtilizationPercentage"]);
						num = (double.IsNaN(num) ? num2 : Math.Max(num, num2));
					}
				}
			}
			catch
			{
			}
			e.Result = Tuple.Create(num, item);
		};
		w.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs e)
		{
			w.Dispose();
			if (!base.IsDisposed)
			{
				gpuBusy = false;
				if (e.Error == null)
				{
					Tuple<double, List<GpuSensor>> tuple = (Tuple<double, List<GpuSensor>>)e.Result;
					gpuValue = tuple.Item1;
					sensors = tuple.Item2;
					sensorTime = DateTime.Now;
				}
				else
				{
					sensors.Clear();
					gpuValue = double.NaN;
				}
				gpuNote = (double.IsNaN(gpuValue) ? "Windows GPU engine usage unavailable" : ("Busiest Windows GPU engine: " + Math.Min(100.0, gpuValue).ToString("F1") + "% · sampled every five cycles"));
				if (page == "GPU")
				{
					RefreshTable();
				}
				UpdateView();
			}
		};
		w.RunWorkerAsync();
	}

	private void RefreshRecorder()
	{
		if (recordingButton != null && !recordingButton.IsDisposed)
		{
			recordingButton.Text = (record ? "Stop recording" : "Start recording");
		}
		if (recordingState != null && !recordingState.IsDisposed)
		{
			recordingState.Text = ((!record) ? "Stopped" : (paused ? "Paused with monitoring" : "Recording")) + " · " + recording.Count + " / 1,800 samples captured";
		}
	}

	private void BuildRecorder()
	{
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel();
		flowLayoutPanel.Dock = DockStyle.Fill;
		flowLayoutPanel.FlowDirection = FlowDirection.TopDown;
		flowLayoutPanel.WrapContents = false;
		FlowLayoutPanel flowLayoutPanel2 = flowLayoutPanel;
		flowLayoutPanel2.Controls.Add(new Label
		{
			Text = "Record system performance",
			AutoSize = true,
			Font = new Font("Segoe UI", 20f),
			Margin = new Padding(0, 25, 0, 15)
		});
		flowLayoutPanel2.Controls.Add(new Label
		{
			Text = "Capture CPU, memory, network and disk samples locally.\r\nRecordings stay in memory until you export them. Maximum: 1,800 samples.\r\nPausing live updates also pauses recording. No process names are exported.",
			AutoSize = true,
			ForeColor = muted,
			Margin = new Padding(0, 0, 0, 25)
		});
		Button button = (recordingButton = Button(record ? "Stop recording" : "Start recording"));
		recordingState = new Label
		{
			AutoSize = true,
			Margin = new Padding(0, 12, 0, 12)
		};
		flowLayoutPanel2.Controls.Add(recordingState);
		RefreshRecorder();
		button.Click += delegate
		{
			if (!record && recording.Count > 0)
			{
				if (MessageBox.Show(this, "Start a new recording? Export the existing recording first if you want to keep it.", "New recording", MessageBoxButtons.OKCancel) != DialogResult.OK)
				{
					return;
				}
				recording.Clear();
			}
			record = !record;
			RefreshRecorder();
		};
		flowLayoutPanel2.Controls.Add(button);
		Button button2 = Button("Export recording CSV");
		button2.Click += delegate
		{
			ExportRecording();
		};
		flowLayoutPanel2.Controls.Add(button2);
		content.Controls.Add(flowLayoutPanel2);
		sub.Text = "A local performance recorder for troubleshooting";
	}

	private void RunTask()
	{
		OpenFileDialog openFileDialog = new OpenFileDialog();
		openFileDialog.Title = "Choose a program to run";
		openFileDialog.Filter = "Windows programs (*.exe)|*.exe";
		openFileDialog.CheckFileExists = true;
		using OpenFileDialog openFileDialog2 = openFileDialog;
		if (openFileDialog2.ShowDialog(this) != DialogResult.OK)
		{
			return;
		}
		try
		{
			ProcessStartInfo processStartInfo = new ProcessStartInfo(openFileDialog2.FileName);
			processStartInfo.UseShellExecute = true;
			processStartInfo.WorkingDirectory = Path.GetDirectoryName(openFileDialog2.FileName);
			Process.Start(processStartInfo);
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, ex.Message, "Could not start program");
		}
	}

	private void EndProcess()
	{
		if (grid == null || grid.SelectedRows.Count == 0 || !(grid.SelectedRows[0].Tag is Proc))
		{
			MessageBox.Show(this, "Select a process first.");
			return;
		}
		Proc proc = (Proc)grid.SelectedRows[0].Tag;
		string[] source = new string[10] { "system", "registry", "smss", "csrss", "wininit", "winlogon", "lsass", "services", "secure system", "memory compression" };
		if (proc.Id <= 4 || proc.Id == Process.GetCurrentProcess().Id || proc.Session == 0 || source.Contains(proc.Name.ToLowerInvariant()) || proc.Start == 0)
		{
			MessageBox.Show(this, "This process is protected by Aster or its identity could not be verified.", "Cannot end process");
			return;
		}
		try
		{
			using (Process process = Process.GetProcessById(proc.Id))
			{
				if (process.StartTime.ToUniversalTime().Ticks != proc.Start)
				{
					throw new Exception("The selected process has exited. Refresh the list.");
				}
				if (!Native.IsProcessCritical(process.Handle, out var critical) || critical)
				{
					throw new Exception("Windows could not confirm that this process is safe to terminate.");
				}
				if (MessageBox.Show(this, "End " + proc.Name + " (PID " + proc.Id + ")?\r\nUnsaved work in this program will be lost.", "End process", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
				{
					return;
				}
				process.Kill();
			}
			StartSample();
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, ex.Message, "Could not end process");
		}
	}

	private static string Csv(string x)
	{
		x = x ?? "";
		if (x.Length > 0 && "=+-@\t\r".IndexOf(x[0]) >= 0)
		{
			x = "'" + x;
		}
		return "\"" + x.Replace("\"", "\"\"") + "\"";
	}

	private void SaveCsv(string name, IEnumerable<string[]> rows)
	{
		SaveFileDialog saveFileDialog = new SaveFileDialog();
		saveFileDialog.Filter = "CSV file (*.csv)|*.csv";
		saveFileDialog.FileName = "Aster-" + name + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".csv";
		using SaveFileDialog saveFileDialog2 = saveFileDialog;
		if (saveFileDialog2.ShowDialog(this) != DialogResult.OK)
		{
			return;
		}
		try
		{
			File.WriteAllLines(saveFileDialog2.FileName, rows.Select((string[] row) => string.Join(",", row.Select(Csv))), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
			MessageBox.Show(this, "Saved to " + saveFileDialog2.FileName, "Export complete");
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, ex.Message, "Export failed");
		}
	}

	private void Export()
	{
		if (page == "Flight recorder")
		{
			ExportRecording();
		}
		else if (grid != null)
		{
			List<string[]> list = new List<string[]>();
			list.Add((from DataGridViewColumn c in grid.Columns
				select c.HeaderText).ToArray());
			foreach (DataGridViewRow item in (IEnumerable)grid.Rows)
			{
				list.Add((from DataGridViewCell c in item.Cells
					select Convert.ToString(c.Value)).ToArray());
			}
			SaveCsv(page, list);
		}
		else
		{
			SaveCsv("performance", SampleRows(history));
		}
	}

	private IEnumerable<string[]> SampleRows(IEnumerable<Snapshot> samples)
	{
		yield return new string[8] { "Local time", "CPU percent", "Memory percent", "Memory used bytes", "Receive bytes per second", "Send bytes per second", "Disk read bytes per second", "Disk write bytes per second" };
		foreach (Snapshot s in samples)
		{
			yield return new string[8]
			{
				s.Time.ToString("o"),
				F(s.Cpu),
				F(s.MemoryPercent),
				(s.Total - s.Available).ToString(),
				F(s.Receive),
				F(s.Send),
				F(s.DiskRead),
				F(s.DiskWrite)
			};
		}
	}

	private string F(double v)
	{
		if (!double.IsNaN(v))
		{
			return v.ToString("F2", CultureInfo.InvariantCulture);
		}
		return "Unavailable";
	}

	private void ExportRecording()
	{
		if (recording.Count == 0)
		{
			MessageBox.Show(this, "Start a recording first.");
		}
		else
		{
			SaveCsv("recording", SampleRows(recording));
		}
	}

	private void BuildHorizon()
	{
		if (!horizonCloseHook)
		{
			horizonCloseHook = true;
			base.FormClosed += delegate
			{
				if (horizon != null)
				{
					horizon.Dispose();
				}
			};
		}
		sub.Text = "Your personal desktop · saved styles, shortcuts and a searchable launcher";
		string text = "";
		HorizonSettings draft;
		try
		{
			draft = HorizonStore.Read();
		}
		catch (Exception ex)
		{
			draft = new HorizonSettings();
			text = "Could not load saved settings: " + ex.Message + " Defaults are shown; your file stays unchanged until you save.";
		}
		FlowLayoutPanel flow = new FlowLayoutPanel
		{
			Dock = DockStyle.Fill,
			AutoScroll = true,
			FlowDirection = FlowDirection.TopDown,
			WrapContents = false,
			Padding = new Padding(20)
		};
		content.Controls.Add(flow);
		flow.Controls.Add(new Label
		{
			Text = "✦  HORIZON",
			AutoSize = true,
			Font = new Font("Segoe UI", 28f, FontStyle.Bold),
			ForeColor = HorizonStore.Accents[draft.Theme]
		});
		flow.Controls.Add(new Label
		{
			Text = "A desktop that feels like yours.\r\nFloating toolbar, app dock and instant launcher. Your Windows taskbar stays available.",
			AutoSize = true,
			Margin = new Padding(0, 8, 0, 18)
		});
		Label status = new Label
		{
			AutoSize = true,
			MaximumSize = new Size(850, 0),
			Text = ((text.Length > 0) ? text : ((horizon != null && !horizon.IsDisposed) ? "Horizon is running." : "Horizon is off. Start when you are ready."))
		};
		flow.Controls.Add(status);
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel();
		flowLayoutPanel.AutoSize = true;
		FlowLayoutPanel flowLayoutPanel2 = flowLayoutPanel;
		flow.Controls.Add(flowLayoutPanel2);
		Button button = Button("Save & start Horizon");
		Button button2 = Button("Stop Horizon");
		Button button3 = Button("Open launcher");
		flowLayoutPanel2.Controls.AddRange(new Control[3] { button, button2, button3 });
		Action<string, Control> action = delegate(string caption, Control control)
		{
			FlowLayoutPanel flowLayoutPanel7 = new FlowLayoutPanel
			{
				AutoSize = true,
				WrapContents = false
			};
			flowLayoutPanel7.Controls.Add(new Label
			{
				Text = caption,
				Width = 170,
				Height = 32,
				TextAlign = ContentAlignment.MiddleLeft
			});
			flowLayoutPanel7.Controls.Add(control);
			flow.Controls.Add(flowLayoutPanel7);
		};
		ComboBox theme = new ComboBox
		{
			DropDownStyle = ComboBoxStyle.DropDownList,
			Width = 240
		};
		theme.Items.AddRange(HorizonStore.Themes);
		theme.SelectedIndex = draft.Theme;
		action("Color palette", theme);
		ComboBox screen = new ComboBox
		{
			DropDownStyle = ComboBoxStyle.DropDownList,
			Width = 340
		};
		screen.Items.AddRange(Screen.AllScreens.Select((Screen s) => s.DeviceName).ToArray());
		screen.SelectedItem = draft.Display;
		if (screen.SelectedIndex < 0)
		{
			screen.SelectedItem = Screen.PrimaryScreen.DeviceName;
		}
		action("Display", screen);
		ComboBox size = new ComboBox
		{
			DropDownStyle = ComboBoxStyle.DropDownList,
			Width = 240
		};
		size.Items.AddRange(new string[3] { "Compact", "Comfortable", "Large" });
		size.SelectedIndex = draft.Size;
		action("Dock size", size);
		NumericUpDown opacity = new NumericUpDown
		{
			Minimum = 85m,
			Maximum = 100m,
			Value = draft.Opacity,
			Width = 90
		};
		action("Opacity (%)", opacity);
		Func<string, bool, CheckBox> func = delegate(string title, bool value)
		{
			CheckBox checkBox = new CheckBox
			{
				Text = title,
				Checked = value,
				AutoSize = true,
				Margin = new Padding(0, 7, 0, 7)
			};
			flow.Controls.Add(checkBox);
			return checkBox;
		};
		CheckBox auto = func("Auto-hide dock — move to the bottom edge to reveal", draft.AutoHide);
		CheckBox full = func("Hide toolbar and dock during fullscreen apps", draft.HideFullscreen);
		CheckBox clock = func("24-hour clock", draft.Clock24);
		CheckBox metrics = func("Show live CPU and RAM in toolbar", draft.ShowMeters);
		FlowLayoutPanel flowLayoutPanel3 = new FlowLayoutPanel();
		flowLayoutPanel3.AutoSize = true;
		FlowLayoutPanel flowLayoutPanel4 = flowLayoutPanel3;
		flow.Controls.Add(flowLayoutPanel4);
		string[] array = new string[3] { "Minimal", "Creator", "Neon" };
		foreach (string text2 in array)
		{
			string preset = text2;
			Button button4 = Button(text2);
			flowLayoutPanel4.Controls.Add(button4);
			button4.Click += delegate
			{
				theme.SelectedIndex = ((preset == "Neon") ? 4 : 0);
				size.SelectedIndex = ((!(preset == "Minimal")) ? 1 : 0);
				auto.Checked = preset != "Creator";
				metrics.Checked = preset != "Minimal";
				opacity.Value = ((preset == "Neon") ? 92 : 100);
				status.Text = preset + " preset selected. Save & start to apply.";
			};
		}
		flow.Controls.Add(new Label
		{
			Text = "PINNED APPS  ·  up to 24 local apps or shortcuts",
			AutoSize = true,
			Margin = new Padding(0, 14, 0, 6)
		});
		ListBox pins = new ListBox
		{
			Width = 650,
			Height = 100
		};
		foreach (HorizonPin pin in draft.Pins)
		{
			pins.Items.Add(pin);
		}
		flow.Controls.Add(pins);
		FlowLayoutPanel flowLayoutPanel5 = new FlowLayoutPanel();
		flowLayoutPanel5.AutoSize = true;
		FlowLayoutPanel flowLayoutPanel6 = flowLayoutPanel5;
		flow.Controls.Add(flowLayoutPanel6);
		Button button5 = Button("Add app…");
		Button button6 = Button("Remove");
		Button button7 = Button("Move up");
		Button button8 = Button("Move down");
		flowLayoutPanel6.Controls.AddRange(new Control[4] { button5, button6, button7, button8 });
		button5.Click += delegate
		{
			if (pins.Items.Count >= 24)
			{
				status.Text = "The dock supports up to 24 custom pins.";
				return;
			}
			OpenFileDialog pick = new OpenFileDialog
			{
				Filter = "Apps and shortcuts|*.exe;*.lnk",
				CheckFileExists = true,
				DereferenceLinks = false
			};
			try
			{
				if (pick.ShowDialog(this) == DialogResult.OK)
				{
					if (pins.Items.Cast<HorizonPin>().Any((HorizonPin p) => string.Equals(p.Path, pick.FileName, StringComparison.OrdinalIgnoreCase)))
					{
						status.Text = "That app is already pinned.";
					}
					else
					{
						string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(pick.FileName);
						pins.Items.Add(new HorizonPin
						{
							Name = fileNameWithoutExtension.Substring(0, Math.Min(60, fileNameWithoutExtension.Length)),
							Path = pick.FileName
						});
					}
				}
			}
			finally
			{
				if (pick != null)
				{
					((IDisposable)pick).Dispose();
				}
			}
		};
		button6.Click += delegate
		{
			if (pins.SelectedIndex >= 0)
			{
				pins.Items.RemoveAt(pins.SelectedIndex);
			}
		};
		Action<int> move = delegate(int delta)
		{
			int selectedIndex = pins.SelectedIndex;
			int num2 = selectedIndex + delta;
			if (selectedIndex >= 0 && num2 >= 0 && num2 < pins.Items.Count)
			{
				object item2 = pins.Items[selectedIndex];
				pins.Items.RemoveAt(selectedIndex);
				pins.Items.Insert(num2, item2);
				pins.SelectedIndex = num2;
			}
		};
		button7.Click += delegate
		{
			move(-1);
		};
		button8.Click += delegate
		{
			move(1);
		};
		button.Click += delegate
		{
			try
			{
				draft.Theme = theme.SelectedIndex;
				draft.Display = screen.Text;
				draft.Size = size.SelectedIndex;
				draft.Opacity = (int)opacity.Value;
				draft.AutoHide = auto.Checked;
				draft.HideFullscreen = full.Checked;
				draft.Clock24 = clock.Checked;
				draft.ShowMeters = metrics.Checked;
				draft.Pins = pins.Items.Cast<HorizonPin>().ToList();
				HorizonStore.Save(draft);
				if (horizon != null)
				{
					horizon.Dispose();
				}
				horizon = new HorizonSession(draft, delegate(string page)
				{
					Show();
					if (base.WindowState == FormWindowState.Minimized)
					{
						base.WindowState = FormWindowState.Normal;
					}
					Activate();
					Navigate(page);
				}, () => (current != null) ? ((paused ? "PAUSED  " : "") + "CPU " + current.Cpu.ToString("F0") + "%  ·  RAM " + current.MemoryPercent.ToString("F0") + "%") : "Waiting for system data", delegate
				{
					if (!status.IsDisposed)
					{
						status.Text = "Horizon is off.";
					}
				});
				horizon.Start();
				status.Text = "Saved. Horizon is running. Use ASTER on the toolbar or Launch on the dock.";
			}
			catch (Exception ex2)
			{
				status.Text = "Could not start Horizon: " + ex2.Message;
			}
		};
		button2.Click += delegate
		{
			if (horizon != null)
			{
				horizon.Dispose();
			}
			status.Text = "Horizon is off. Your saved style is kept.";
		};
		button3.Click += delegate
		{
			if (horizon == null || horizon.IsDisposed)
			{
				status.Text = "Start Horizon first to use its launcher.";
			}
			else
			{
				horizon.ShowLauncher();
			}
		};
		flow.Controls.Add(new Label
		{
			AutoSize = true,
			MaximumSize = new Size(850, 0),
			Margin = new Padding(0, 16, 0, 8),
			Text = "Horizon runs while Aster is open and starts off each session. Stop from here, the toolbar, or the tray icon.\r\nEsc closes the launcher. Saved preferences apply with Save & start. Missing monitors fall back to the primary display.\r\nThis is Aster’s own desktop companion, inspired by Seelen UI. It does not include automatic window tiling or replace the Windows shell."
		});
	}

	private void LoadPendingUpdate()
	{
		if (pendingLoaded)
		{
			return;
		}
		pendingLoaded = true;
		try
		{
			InstalledUpdate installedUpdate = UpdateReceipt.Read(active: false);
			if (installedUpdate != null)
			{
				installedUpdateFolder = installedUpdate.Folder;
				installedOnlineVersion = installedUpdate.Version;
				onlineStatus = "Aster " + installedUpdate.Version + " is installed and ready. Restart to finish updating.";
			}
		}
		catch (Exception ex)
		{
			onlineStatus = "A previous update could not be restored. You can check and download again.\r\n" + ex.Message;
		}
	}

	private async Task CheckOnlineUpdate()
	{
		if (updateBusy)
		{
			return;
		}
		updateBusy = true;
		onlineRelease = null;
		onlineProgress = 0;
		onlineStatus = "Checking for updates…";
		updateCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(60.0));
		bool offer = false;
		try
		{
			int num = default(int);
			_ = num;
			_ = 0;
			try
			{
				OnlineRelease release = await OnlineUpdate.Check(updateCancellation.Token);
				if (new Version(release.Version) > new Version(AsterUpdate.CurrentVersion))
				{
					onlineRelease = release;
					onlineStatus = "Update available: Aster " + release.Version + " (" + ((double)release.Size / 1048576.0).ToString("F1") + " MB)\r\n\r\n" + release.Notes;
					offer = true;
				}
				else
				{
					onlineStatus = "You're up to date. Installed Aster 2.8.\r\nLast checked: " + DateTime.Now.ToString("g");
				}
			}
			catch (UpdateFeedMissingException)
			{
				onlineStatus = "Updates are not published yet.\r\nThe release needs both aster-update.json and the Aster ZIP on GitHub. This does not mean your app is up to date.";
			}
			catch (OperationCanceledException)
			{
				onlineStatus = "Update check canceled or timed out. Try again when you are ready.";
			}
			catch (Exception ex3)
			{
				onlineStatus = "Could not check for updates. Check your connection and try again.\r\n" + ex3.Message;
			}
		}
		finally
		{
			updateCancellation.Dispose();
			updateCancellation = null;
			updateBusy = false;
		}
		if (offer && !base.IsDisposed)
		{
			await OfferOnlineUpdate();
		}
	}

	private async Task OfferOnlineUpdate()
	{
		if (!updateBusy && onlineRelease != null && installedUpdateFolder == null)
		{
			string prompt = "Update available: Aster " + onlineRelease.Version + "\r\n\r\nDo you want to update now? Aster will download, verify and install it automatically.\r\n" + (restartAfterUpdate ? "Aster will restart when installation finishes. Save any work first." : "You can restart later to finish.") + "\r\n\r\nYour saved preferences stay on this PC.";
			if ((ConfirmOnlineUpdate != null) ? ConfirmOnlineUpdate(prompt) : (MessageBox.Show(this, prompt, "Update available", MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk) == DialogResult.Yes))
			{
				await InstallOnlineUpdate();
			}
		}
	}

	private async Task InstallOnlineUpdate()
	{
		if (updateBusy || onlineRelease == null)
		{
			return;
		}
		updateBusy = true;
		onlineProgress = 0;
		OnlineRelease release = onlineRelease;
		bool restart = restartAfterUpdate;
		string zip = null;
		bool success = false;
		updateCancellation = new CancellationTokenSource(TimeSpan.FromMinutes(15.0));
		onlineStatus = "Downloading Aster " + release.Version + "…";
		try
		{
			zip = await OnlineUpdate.Download(release, UpdateReceipt.Downloads, new Progress<int>(delegate(int value)
			{
				onlineProgress = value;
			}), updateCancellation.Token);
			updateCancellation.Token.ThrowIfCancellationRequested();
			updateCancellation.Dispose();
			updateCancellation = null;
			onlineStatus = "Verifying and installing Aster " + release.Version + "… Keep Aster open.";
			string file = zip;
			string folder = await Task.Run(delegate
			{
				UpdatePackage updatePackage = AsterUpdate.Inspect(file);
				if (updatePackage.Manifest.Version != release.Version || !string.Equals(updatePackage.PackageHash, release.Sha256, StringComparison.OrdinalIgnoreCase))
				{
					throw new IOException("The package does not match the signed release.");
				}
				return AsterUpdate.Install(file, updatePackage.PackageHash, UpdateReceipt.Versions, AppDomain.CurrentDomain.BaseDirectory);
			});
			UpdateReceipt.Save(folder, release.Version, active: false);
			installedUpdateFolder = folder;
			installedOnlineVersion = release.Version;
			onlineProgress = 100;
			success = true;
			onlineStatus = "Aster " + release.Version + " installed successfully. Restart to finish updating.";
		}
		catch (OperationCanceledException)
		{
			onlineStatus = "Update canceled or timed out. Your current Aster is unchanged.";
		}
		catch (Exception ex2)
		{
			onlineStatus = "Update failed. Your current Aster is unchanged. You can try again.\r\n" + ex2.Message;
		}
		finally
		{
			if (updateCancellation != null)
			{
				updateCancellation.Dispose();
				updateCancellation = null;
			}
			if (zip != null)
			{
				try
				{
					File.Delete(zip);
				}
				catch
				{
				}
			}
			updateBusy = false;
		}
		if (success && restart && !base.IsDisposed)
		{
			RestartOnlineUpdate();
		}
	}

	private void RestartOnlineUpdate()
	{
		if (updateBusy || installedUpdateFolder == null)
		{
			return;
		}
		try
		{
			InstalledUpdate installedUpdate = UpdateReceipt.Read(active: false);
			if (installedUpdate == null)
			{
				throw new IOException("No verified newer update is ready.");
			}
			if (LaunchUpdatedForTest != null)
			{
				LaunchUpdatedForTest(installedUpdate.Folder);
				return;
			}
			string folder = installedUpdate.Folder;
			string version = installedUpdate.Version;
			Close();
			if (!base.IsDisposed)
			{
				onlineStatus = "Restart postponed. Your update is installed and ready.";
				return;
			}
			try
			{
				UpdateReceipt.Save(folder, version, active: true);
			}
			catch
			{
			}
			ProcessStartInfo processStartInfo = new ProcessStartInfo(Path.Combine(folder, "Aster.exe"));
			processStartInfo.UseShellExecute = true;
			processStartInfo.WorkingDirectory = folder;
			Process.Start(processStartInfo);
		}
		catch (Exception ex)
		{
			if (!base.IsDisposed)
			{
				onlineStatus = "Could not restart into the update: " + ex.Message;
			}
			else
			{
				MessageBox.Show("Aster could not open the update. Your previous version is still available.\r\n" + ex.Message, "Aster update");
			}
		}
	}

	private void BuildUpdates()
	{
		LoadPendingUpdate();
		if (!updateCloseHook)
		{
			updateCloseHook = true;
			base.FormClosing += delegate(object s, FormClosingEventArgs e)
			{
				if (updateBusy)
				{
					e.Cancel = true;
					MessageBox.Show(this, "An update is running. Cancel it or wait for installation to finish before closing.");
				}
			};
		}
		sub.Text = "Installed Aster 2.8 · automatic download and installation";
		FlowLayoutPanel flowLayoutPanel = FeatureFlow("Updates made simple.", "Check for a new version, choose Update now, and Aster handles the rest. No ZIP to select or files to copy.");
		TextBox state = new TextBox
		{
			Text = onlineStatus,
			ReadOnly = true,
			Multiline = true,
			Width = 740,
			Height = 180,
			ScrollBars = ScrollBars.Vertical
		};
		ProgressBar progress = new ProgressBar
		{
			Width = 740,
			Height = 22
		};
		FlowLayoutPanel flowLayoutPanel2 = new FlowLayoutPanel();
		flowLayoutPanel2.Width = 760;
		flowLayoutPanel2.Height = 48;
		FlowLayoutPanel flowLayoutPanel3 = flowLayoutPanel2;
		Button primary = Button("Check for updates");
		Button recheck = Button("Check again");
		Button cancel = Button("Cancel");
		flowLayoutPanel3.Controls.AddRange(new Control[3] { primary, recheck, cancel });
		flowLayoutPanel.Controls.Add(flowLayoutPanel3);
		flowLayoutPanel.Controls.Add(progress);
		flowLayoutPanel.Controls.Add(state);
		CheckBox automatic = new CheckBox
		{
			Text = "Restart automatically when the update finishes",
			Checked = restartAfterUpdate,
			AutoSize = true,
			Margin = new Padding(0, 16, 0, 8)
		};
		automatic.CheckedChanged += delegate
		{
			restartAfterUpdate = automatic.Checked;
		};
		flowLayoutPanel.Controls.Add(automatic);
		Action refresh = delegate
		{
			if (!state.IsDisposed)
			{
				state.Text = onlineStatus;
				progress.Value = Math.Max(0, Math.Min(100, onlineProgress));
				primary.Text = (updateBusy ? "Updating…" : ((installedUpdateFolder != null) ? "Restart to finish update" : ((onlineRelease != null) ? "Update available · Update now" : "Check for updates")));
				primary.Enabled = !updateBusy;
				recheck.Enabled = !updateBusy && installedUpdateFolder == null;
				recheck.Visible = onlineRelease != null;
				cancel.Enabled = updateCancellation != null;
				automatic.Enabled = !updateBusy;
			}
		};
		primary.Click += async delegate
		{
			if (!updateBusy)
			{
				if (installedUpdateFolder != null)
				{
					RestartOnlineUpdate();
				}
				else if (onlineRelease == null)
				{
					await CheckOnlineUpdate();
				}
				else
				{
					await OfferOnlineUpdate();
				}
				refresh();
			}
		};
		recheck.Click += async delegate
		{
			await CheckOnlineUpdate();
			refresh();
		};
		cancel.Click += delegate
		{
			if (updateCancellation != null)
			{
				updateCancellation.Cancel();
			}
		};
		System.Windows.Forms.Timer uiTimer = new System.Windows.Forms.Timer
		{
			Interval = 200
		};
		uiTimer.Tick += delegate
		{
			refresh();
		};
		flowLayoutPanel.Disposed += delegate
		{
			uiTimer.Dispose();
		};
		uiTimer.Start();
		refresh();
		flowLayoutPanel.Controls.Add(new Label
		{
			Text = "Official source: github.com/abdul2277/Aster\r\nPublisher signature, download hash and package contents are checked before installation.\r\nYour old version stays available. Aster 2.3+ launchers forward to your activated newer version.\r\nUpdates become available after the publisher uploads a signed release.",
			AutoSize = true,
			Margin = new Padding(0, 18, 0, 0)
		});
	}

	private void AddSessionEvent(string message)
	{
		sessionTimeline.Insert(0, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " · " + message);
		if (sessionTimeline.Count > 200)
		{
			sessionTimeline.RemoveAt(200);
		}
		if (timelineList != null && !timelineList.IsDisposed)
		{
			timelineList.Items.Clear();
			timelineList.Items.AddRange(sessionTimeline.Cast<object>().ToArray());
		}
	}

	private void TickSessionTools()
	{
		if (lastPaused != paused)
		{
			lastPaused = paused;
			AddSessionEvent(paused ? "Monitoring paused" : "Monitoring resumed");
		}
		if (paused && comparisonCapture.Active)
		{
			comparisonCapture.Cancel("Capture canceled because monitoring was paused.");
			AddSessionEvent("Performance capture canceled: monitoring paused");
			RefreshComparison();
		}
		if (maintenanceState != null && !maintenanceState.IsDisposed)
		{
			maintenanceState.Text = MaintenanceSummary();
		}
	}

	private void TrackSessionTools(Snapshot s)
	{
		if (paused && comparisonCapture.Active)
		{
			comparisonCapture.Cancel("Capture canceled because monitoring was paused.");
		}
		else if (comparisonCapture.Add(s, Math.Max(10.0, (double)timer.Interval / 1000.0 * 3.0)))
		{
			AddSessionEvent(comparisonCapture.State);
		}
		RefreshComparison();
	}

	private string MaintenanceSummary()
	{
		int num = preferences.Maintenance.Count((MaintenanceItem x) => !x.Done && x.Due.Date <= DateTime.Today);
		if (num != 0)
		{
			return num + " maintenance reminder(s) due · open Maintenance planner";
		}
		return "No maintenance reminders due today.";
	}

	private void RefreshComparison()
	{
		if (comparisonText != null && !comparisonText.IsDisposed)
		{
			comparisonText.Text = comparisonCapture.Report();
		}
		if (comparisonState != null && !comparisonState.IsDisposed)
		{
			comparisonState.Text = comparisonCapture.State;
		}
	}

	private void BuildPerformanceCompare()
	{
		sub.Text = "Compare two 20-sample windows · local measurements · no automatic tuning";
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel();
		tableLayoutPanel.Dock = DockStyle.Fill;
		tableLayoutPanel.ColumnCount = 1;
		tableLayoutPanel.RowCount = 4;
		tableLayoutPanel.Padding = new Padding(16);
		TableLayoutPanel tableLayoutPanel2 = tableLayoutPanel;
		int[] array = new int[3] { 80, 50, 35 };
		foreach (int num in array)
		{
			tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, num));
		}
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		content.Controls.Add(tableLayoutPanel2);
		tableLayoutPanel2.Controls.Add(new Label
		{
			Dock = DockStyle.Fill,
			Text = "1. Capture Before during your usual workload.\r\n2. Make one change yourself, then capture After under a similar workload.\r\nCaptures use the selected refresh interval and can continue while you visit another page."
		}, 0, 0);
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel();
		flowLayoutPanel.Dock = DockStyle.Fill;
		FlowLayoutPanel flowLayoutPanel2 = flowLayoutPanel;
		tableLayoutPanel2.Controls.Add(flowLayoutPanel2, 0, 1);
		bool[] array2 = new bool[2] { true, false };
		foreach (bool flag in array2)
		{
			bool which = flag;
			Button button = Button(flag ? "Capture Before" : "Capture After");
			button.Click += delegate
			{
				try
				{
					if (paused)
					{
						throw new InvalidOperationException("Resume monitoring before starting a capture.");
					}
					comparisonCapture.Start(which);
					AddSessionEvent("Started " + (which ? "Before" : "After") + " performance capture");
					RefreshComparison();
				}
				catch (Exception ex)
				{
					MessageBox.Show(this, ex.Message);
				}
			};
			flowLayoutPanel2.Controls.Add(button);
		}
		Button button2 = Button("Cancel capture");
		button2.Click += delegate
		{
			if (comparisonCapture.Active)
			{
				comparisonCapture.Cancel("Capture canceled. Completed windows kept.");
				AddSessionEvent("Performance capture canceled");
				RefreshComparison();
			}
		};
		flowLayoutPanel2.Controls.Add(button2);
		Button button3 = Button("Export comparison");
		button3.Click += delegate
		{
			ExportPersonalText("performance-comparison", comparisonCapture.Report());
		};
		flowLayoutPanel2.Controls.Add(button3);
		comparisonState = new Label
		{
			Dock = DockStyle.Fill
		};
		tableLayoutPanel2.Controls.Add(comparisonState, 0, 2);
		comparisonText = new TextBox
		{
			Dock = DockStyle.Fill,
			ReadOnly = true,
			Multiline = true,
			ScrollBars = ScrollBars.Vertical
		};
		tableLayoutPanel2.Controls.Add(comparisonText, 0, 3);
		RefreshComparison();
	}

	private void BuildSessionTimeline()
	{
		sub.Text = "This Aster session · latest 200 events · stored in memory until you export";
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel();
		tableLayoutPanel.Dock = DockStyle.Fill;
		tableLayoutPanel.ColumnCount = 1;
		tableLayoutPanel.RowCount = 3;
		tableLayoutPanel.Padding = new Padding(16);
		TableLayoutPanel tableLayoutPanel2 = tableLayoutPanel;
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		content.Controls.Add(tableLayoutPanel2);
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel();
		flowLayoutPanel.Dock = DockStyle.Fill;
		FlowLayoutPanel flowLayoutPanel2 = flowLayoutPanel;
		TextBox note = new TextBox
		{
			Width = 410,
			MaxLength = 120,
			AccessibleName = "Timeline marker"
		};
		flowLayoutPanel2.Controls.Add(note);
		Button button = Button("Add marker");
		button.Click += delegate
		{
			if (note.Text.Trim().Length > 0)
			{
				AddSessionEvent("Marker: " + note.Text.Trim());
				note.Clear();
			}
		};
		flowLayoutPanel2.Controls.Add(button);
		tableLayoutPanel2.Controls.Add(flowLayoutPanel2, 0, 0);
		FlowLayoutPanel flowLayoutPanel3 = new FlowLayoutPanel();
		flowLayoutPanel3.Dock = DockStyle.Fill;
		FlowLayoutPanel flowLayoutPanel4 = flowLayoutPanel3;
		Button button2 = Button("Export timeline");
		button2.Click += delegate
		{
			ExportPersonalText("session-timeline", string.Join(Environment.NewLine, sessionTimeline));
		};
		flowLayoutPanel4.Controls.Add(button2);
		Button button3 = Button("Clear timeline");
		button3.Click += delegate
		{
			sessionTimeline.Clear();
			timelineList.Items.Clear();
		};
		flowLayoutPanel4.Controls.Add(button3);
		flowLayoutPanel4.Controls.Add(new Label
		{
			Text = "Tracks page visits, monitoring, captures and performance alerts.",
			AutoSize = true,
			Margin = new Padding(12, 8, 0, 0)
		});
		tableLayoutPanel2.Controls.Add(flowLayoutPanel4, 0, 1);
		timelineList = new ListBox
		{
			Dock = DockStyle.Fill,
			HorizontalScrollbar = true
		};
		timelineList.Items.AddRange(sessionTimeline.Cast<object>().ToArray());
		tableLayoutPanel2.Controls.Add(timelineList, 0, 2);
	}

	private void BuildMaintenancePlanner()
	{
		sub.Text = "Local reminders while Aster is open · you choose when to take action";
		FlowLayoutPanel flowLayoutPanel = FeatureFlow("A little upkeep. On your terms.", "Plan a backup, update review or storage check. Reminders appear here and in Command Center; Aster never runs maintenance automatically.");
		maintenanceState = new Label
		{
			Text = MaintenanceSummary(),
			AutoSize = true,
			Margin = new Padding(0, 0, 0, 14)
		};
		flowLayoutPanel.Controls.Add(maintenanceState);
		FlowLayoutPanel flowLayoutPanel2 = new FlowLayoutPanel();
		flowLayoutPanel2.Width = 760;
		flowLayoutPanel2.Height = 42;
		FlowLayoutPanel flowLayoutPanel3 = flowLayoutPanel2;
		flowLayoutPanel.Controls.Add(flowLayoutPanel3);
		TextBox title = new TextBox
		{
			Width = 470,
			MaxLength = 80,
			AccessibleName = "Maintenance title"
		};
		flowLayoutPanel.Controls.Add(new Label
		{
			Text = "Reminder title",
			AutoSize = true
		});
		flowLayoutPanel.Controls.Add(title);
		string[] array = new string[3] { "Review backups", "Check Windows updates", "Review storage space" };
		foreach (string text in array)
		{
			string preset = text;
			Button button = Button(text);
			button.Click += delegate
			{
				title.Text = preset;
			};
			flowLayoutPanel3.Controls.Add(button);
		}
		FlowLayoutPanel flowLayoutPanel4 = new FlowLayoutPanel();
		flowLayoutPanel4.Width = 760;
		flowLayoutPanel4.Height = 42;
		FlowLayoutPanel flowLayoutPanel5 = flowLayoutPanel4;
		flowLayoutPanel.Controls.Add(flowLayoutPanel5);
		flowLayoutPanel5.Controls.Add(new Label
		{
			Text = "Due",
			Width = 40,
			Margin = new Padding(0, 7, 0, 0)
		});
		DateTimePicker date = new DateTimePicker
		{
			Format = DateTimePickerFormat.Short,
			Width = 155
		};
		flowLayoutPanel5.Controls.Add(date);
		flowLayoutPanel5.Controls.Add(new Label
		{
			Text = "Repeat days (0 = once)",
			Width = 190,
			Margin = new Padding(12, 7, 0, 0)
		});
		NumericUpDown repeat = new NumericUpDown
		{
			Minimum = 0m,
			Maximum = 365m,
			Width = 75
		};
		flowLayoutPanel5.Controls.Add(repeat);
		ListBox list = new ListBox
		{
			Width = 740,
			Height = 220,
			HorizontalScrollbar = true
		};
		FlowLayoutPanel flowLayoutPanel6 = new FlowLayoutPanel();
		flowLayoutPanel6.Width = 760;
		flowLayoutPanel6.Height = 48;
		FlowLayoutPanel flowLayoutPanel7 = flowLayoutPanel6;
		flowLayoutPanel.Controls.Add(flowLayoutPanel7);
		flowLayoutPanel.Controls.Add(list);
		Action render = delegate
		{
			list.Items.Clear();
			list.Items.AddRange((from x in preferences.Maintenance
				orderby x.Done, x.Due
				select x).Cast<object>().ToArray());
			maintenanceState.Text = MaintenanceSummary();
		};
		Action<Action> persist = delegate(Action change)
		{
			List<MaintenanceItem> maintenance = preferences.Maintenance.Select((MaintenanceItem x) => new MaintenanceItem
			{
				Title = x.Title,
				Due = x.Due,
				RepeatDays = x.RepeatDays,
				Done = x.Done
			}).ToList();
			try
			{
				change();
				preferences.Save();
				render();
			}
			catch (Exception ex)
			{
				preferences.Maintenance = maintenance;
				render();
				MessageBox.Show(this, "Could not save reminder: " + ex.Message);
			}
		};
		Button button2 = Button("Add reminder");
		button2.Click += delegate
		{
			if (title.Text.Trim().Length != 0)
			{
				if (preferences.Maintenance.Count >= 24)
				{
					MessageBox.Show(this, "Keep up to 24 reminders. Remove old reminders to add more.");
				}
				else
				{
					persist(delegate
					{
						preferences.Maintenance.Add(new MaintenanceItem
						{
							Title = title.Text.Trim(),
							Due = date.Value.Date,
							RepeatDays = (int)repeat.Value
						});
					});
				}
			}
		};
		flowLayoutPanel7.Controls.Add(button2);
		Button button3 = Button("Mark done");
		button3.Click += delegate
		{
			MaintenanceItem selected = list.SelectedItem as MaintenanceItem;
			if (selected != null)
			{
				persist(delegate
				{
					selected.Complete(DateTime.Today);
				});
			}
		};
		flowLayoutPanel7.Controls.Add(button3);
		Button button4 = Button("Snooze 1 day");
		button4.Click += delegate
		{
			MaintenanceItem selected = list.SelectedItem as MaintenanceItem;
			if (selected != null)
			{
				persist(delegate
				{
					selected.Due = DateTime.Today.AddDays(1.0);
					selected.Done = false;
				});
			}
		};
		flowLayoutPanel7.Controls.Add(button4);
		Button button5 = Button("Remove selected");
		button5.Click += delegate
		{
			MaintenanceItem selected = list.SelectedItem as MaintenanceItem;
			if (selected != null)
			{
				persist(delegate
				{
					preferences.Maintenance.Remove(selected);
				});
			}
		};
		flowLayoutPanel7.Controls.Add(button5);
		FlowLayoutPanel flowLayoutPanel8 = new FlowLayoutPanel();
		flowLayoutPanel8.Width = 760;
		flowLayoutPanel8.Height = 48;
		FlowLayoutPanel flowLayoutPanel9 = flowLayoutPanel8;
		flowLayoutPanel.Controls.Add(flowLayoutPanel9);
		string[] array2 = new string[4] { "Backup center", "Update Aster", "Disk space", "Recovery toolbox" };
		foreach (string text2 in array2)
		{
			string target = text2;
			Button button6 = Button(text2 + " →");
			button6.Click += delegate
			{
				Navigate(target);
			};
			flowLayoutPanel9.Controls.Add(button6);
		}
		render();
	}

	private void TrackPressure(Snapshot s)
	{
		if (preferences.PressureEnabled)
		{
			double maxGap = Math.Max(10.0, (double)timer.Interval / 1000.0 * 3.0);
			if (cpuPressure.Observe(s.Cpu, Math.Max(1, Math.Min(100, preferences.CpuLimit)), Math.Max(5, preferences.PressureSeconds), Math.Max(60, preferences.PressureCooldown), s.Time, maxGap))
			{
				AddPressure(s.Time, "CPU", s.Cpu, "Open Processes to review busy apps. Heavy work may explain this.");
			}
			if (ramPressure.Observe(s.MemoryPercent, Math.Max(1, Math.Min(100, preferences.RamLimit)), Math.Max(5, preferences.PressureSeconds), Math.Max(60, preferences.PressureCooldown), s.Time, maxGap))
			{
				AddPressure(s.Time, "Memory", s.MemoryPercent, "Save work before closing apps you no longer need.");
			}
		}
		else
		{
			cpuPressure.Reset();
			ramPressure.Reset();
		}
		if (commandStatus != null && !commandStatus.IsDisposed)
		{
			commandStatus.Text = CommandStatus();
		}
		if (homeView != null && !homeView.IsDisposed && current != null)
		{
			homeView.SetReadings(current.Cpu, current.MemoryPercent, SystemDriveFreePct());
		}
	}

	private void AddPressure(DateTime time, string metric, double value, string advice)
	{
		AddSessionEvent(metric + " performance alert: " + value.ToString("F1") + "%");
		pressureEvents.Insert(0, time.ToString("HH:mm:ss") + "  ·  " + metric + " " + value.ToString("F1") + "%  ·  " + advice);
		if (pressureEvents.Count > 100)
		{
			pressureEvents.RemoveAt(100);
		}
		if (page != "Performance alerts")
		{
			unreadPressure++;
		}
		RefreshPressureList();
	}

	private void RefreshPressureList()
	{
		if (pressureList != null && !pressureList.IsDisposed)
		{
			pressureList.Items.Clear();
			pressureList.Items.AddRange(pressureEvents.Cast<object>().ToArray());
		}
	}

	private string CommandStatus()
	{
		return (paused ? "Monitoring paused" : "Monitoring live") + "  •  " + ((current == null) ? "Waiting for readings" : ("CPU " + current.Cpu.ToString("F0") + "%  /  RAM " + current.MemoryPercent.ToString("F0") + "%")) + "\r\n" + (preferences.PressureEnabled ? "Performance alerts on" : "Performance alerts off") + "  •  " + unreadPressure + " unread alerts  •  Aster 2.8\r\n" + MaintenanceSummary();
	}

	private FlowLayoutPanel FeatureFlow(string title, string description)
	{
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel();
		flowLayoutPanel.Dock = DockStyle.Fill;
		flowLayoutPanel.FlowDirection = FlowDirection.TopDown;
		flowLayoutPanel.WrapContents = false;
		flowLayoutPanel.AutoScroll = true;
		flowLayoutPanel.Padding = new Padding(18);
		FlowLayoutPanel flowLayoutPanel2 = flowLayoutPanel;
		content.Controls.Add(flowLayoutPanel2);
		flowLayoutPanel2.Controls.Add(new Label
		{
			Text = title,
			AutoSize = true,
			Font = new Font("Segoe UI", 26f, FontStyle.Bold),
			Margin = new Padding(0, 0, 0, 12)
		});
		flowLayoutPanel2.Controls.Add(new Label
		{
			Text = description,
			AutoSize = true,
			MaximumSize = new Size(720, 0),
			Margin = new Padding(0, 0, 0, 18)
		});
		return flowLayoutPanel2;
	}

	private HomeView homeView;

	private double sysDriveFree = -1.0;

	private DateTime sysDriveAt = DateTime.MinValue;

	private double SystemDriveFreePct()
	{
		if ((DateTime.Now - sysDriveAt).TotalSeconds < 30.0)
		{
			return sysDriveFree;
		}
		sysDriveAt = DateTime.Now;
		try
		{
			DriveInfo driveInfo = new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory));
			sysDriveFree = ((driveInfo.IsReady && driveInfo.TotalSize > 0) ? (100.0 * (double)driveInfo.AvailableFreeSpace / (double)driveInfo.TotalSize) : (-1.0));
		}
		catch
		{
			sysDriveFree = -1.0;
		}
		return sysDriveFree;
	}

	private void BuildCommandCenter()
	{
		sub.Text = "How your PC is doing, and the tools you reach for most";
		Button toggle = Button(paused ? "Resume live monitoring" : "Pause live monitoring");
		toggle.Click += delegate
		{
			paused = !paused;
			pause.Text = (paused ? "Resume" : "Pause");
			toggle.Text = (paused ? "Resume live monitoring" : "Pause live monitoring");
			cpuPressure.Reset();
			ramPressure.Reset();
			UpdateView();
		};
		FlowLayoutPanel extra = new FlowLayoutPanel();
		extra.AutoSize = true;
		extra.Controls.Add(toggle);
		extra.Controls.Add(new Label
		{
			Text = "Ctrl+K  Find any tool     •     F11  Distraction-free view",
			AutoSize = true,
			ForeColor = muted,
			Margin = new Padding(10, 10, 0, 0)
		});
		homeView = new HomeView(panel, Color.FromArgb(204, 215, 229), muted, Color.FromArgb(94, 196, 214), preferences.DisplayName, () => recentTools, (string n) => buttons.ContainsKey(n), delegate(string n)
		{
			Navigate(n);
		}, extra);
		content.Controls.Add(homeView);
		if (current != null)
		{
			homeView.SetReadings(current.Cpu, current.MemoryPercent, SystemDriveFreePct());
		}
		ApplyAppTheme();
	}

	private NumericUpDown PressureNumber(FlowLayoutPanel flow, string label, int min, int max, int value)
	{
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel();
		flowLayoutPanel.Width = 710;
		flowLayoutPanel.Height = 38;
		FlowLayoutPanel flowLayoutPanel2 = flowLayoutPanel;
		flowLayoutPanel2.Controls.Add(new Label
		{
			Text = label,
			Width = 400,
			Height = 30,
			TextAlign = ContentAlignment.MiddleLeft
		});
		NumericUpDown numericUpDown = new NumericUpDown();
		numericUpDown.Minimum = min;
		numericUpDown.Maximum = max;
		numericUpDown.Value = Math.Max(min, Math.Min(max, value));
		numericUpDown.Width = 110;
		numericUpDown.AccessibleName = label;
		NumericUpDown numericUpDown2 = numericUpDown;
		flowLayoutPanel2.Controls.Add(numericUpDown2);
		flow.Controls.Add(flowLayoutPanel2);
		return numericUpDown2;
	}

	private void BuildPressureAlerts()
	{
		unreadPressure = 0;
		sub.Text = "Optional local alerts · no changes to Windows or running apps";
		FlowLayoutPanel flowLayoutPanel = FeatureFlow("Catch resource pressure early.", "Alerts appear inside Aster while it is open and monitoring. Choose how long CPU or memory must stay above a limit. A gap in samples resets the waiting period.");
		CheckBox enabled = new CheckBox
		{
			Text = "Enable performance alerts",
			Checked = preferences.PressureEnabled,
			AutoSize = true,
			Margin = new Padding(0, 0, 0, 16)
		};
		flowLayoutPanel.Controls.Add(enabled);
		NumericUpDown cpuLimit = PressureNumber(flowLayoutPanel, "CPU threshold (%)", 1, 100, preferences.CpuLimit);
		NumericUpDown ramLimit = PressureNumber(flowLayoutPanel, "Memory threshold (%)", 1, 100, preferences.RamLimit);
		NumericUpDown duration = PressureNumber(flowLayoutPanel, "Sustained for (seconds)", 5, 600, preferences.PressureSeconds);
		NumericUpDown quiet = PressureNumber(flowLayoutPanel, "Minimum time between alerts per metric (seconds)", 60, 3600, preferences.PressureCooldown);
		Label saved = new Label
		{
			Text = "Changes take effect when you save.",
			AutoSize = true
		};
		Button button = Button("Save alert rules");
		button.Click += delegate
		{
			bool pressureEnabled = preferences.PressureEnabled;
			int cpuLimit2 = preferences.CpuLimit;
			int ramLimit2 = preferences.RamLimit;
			int pressureSeconds = preferences.PressureSeconds;
			int pressureCooldown = preferences.PressureCooldown;
			preferences.PressureEnabled = enabled.Checked;
			preferences.CpuLimit = (int)cpuLimit.Value;
			preferences.RamLimit = (int)ramLimit.Value;
			preferences.PressureSeconds = (int)duration.Value;
			preferences.PressureCooldown = (int)quiet.Value;
			try
			{
				preferences.Save();
				cpuPressure.Reset();
				ramPressure.Reset();
				saved.Text = "Saved · " + (enabled.Checked ? "watching new samples" : "alerts disabled");
			}
			catch (Exception ex)
			{
				preferences.PressureEnabled = pressureEnabled;
				preferences.CpuLimit = cpuLimit2;
				preferences.RamLimit = ramLimit2;
				preferences.PressureSeconds = pressureSeconds;
				preferences.PressureCooldown = pressureCooldown;
				saved.Text = "Could not save: " + ex.Message;
			}
		};
		flowLayoutPanel.Controls.Add(button);
		flowLayoutPanel.Controls.Add(saved);
		flowLayoutPanel.Controls.Add(new Label
		{
			Text = "THIS SESSION · latest 100 alerts",
			AutoSize = true,
			Margin = new Padding(0, 20, 0, 8)
		});
		pressureList = new ListBox
		{
			Width = 740,
			Height = 185,
			HorizontalScrollbar = true
		};
		flowLayoutPanel.Controls.Add(pressureList);
		RefreshPressureList();
		Button button2 = Button("Clear session alerts");
		button2.Click += delegate
		{
			pressureEvents.Clear();
			unreadPressure = 0;
			RefreshPressureList();
		};
		flowLayoutPanel.Controls.Add(button2);
	}

	private void BuildDiagnosticsBundle()
	{
		sub.Text = "Review every exported byte · saved locally · no automatic sharing";
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel();
		tableLayoutPanel.Dock = DockStyle.Fill;
		tableLayoutPanel.ColumnCount = 1;
		tableLayoutPanel.RowCount = 3;
		tableLayoutPanel.Padding = new Padding(12);
		TableLayoutPanel tableLayoutPanel2 = tableLayoutPanel;
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 62f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));
		content.Controls.Add(tableLayoutPanel2);
		tableLayoutPanel2.Controls.Add(new Label
		{
			Text = "A small support bundle with aggregate resource readings.\r\nReview both tabs, then save the ZIP wherever you choose.",
			Dock = DockStyle.Fill,
			AutoSize = true
		}, 0, 0);
		TabControl tabControl = new TabControl();
		tabControl.Dock = DockStyle.Fill;
		TabControl tabControl2 = tabControl;
		tableLayoutPanel2.Controls.Add(tabControl2, 0, 1);
		TextBox report = new TextBox
		{
			Multiline = true,
			ReadOnly = true,
			Dock = DockStyle.Fill,
			ScrollBars = ScrollBars.Both,
			WordWrap = false
		};
		TextBox csv = new TextBox
		{
			Multiline = true,
			ReadOnly = true,
			Dock = DockStyle.Fill,
			ScrollBars = ScrollBars.Both,
			WordWrap = false
		};
		TabPage tabPage = new TabPage("report.txt");
		tabPage.Controls.Add(report);
		tabControl2.TabPages.Add(tabPage);
		TabPage tabPage2 = new TabPage("performance.csv");
		tabPage2.Controls.Add(csv);
		tabControl2.TabPages.Add(tabPage2);
		Action capture = delegate
		{
			Snapshot[] samples = history.ToArray();
			report.Text = DiagnosticsBundle.Report(samples, AsterUpdate.CurrentVersion, paused);
			csv.Text = DiagnosticsBundle.Csv(samples);
		};
		capture();
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel();
		flowLayoutPanel.Dock = DockStyle.Fill;
		FlowLayoutPanel flowLayoutPanel2 = flowLayoutPanel;
		tableLayoutPanel2.Controls.Add(flowLayoutPanel2, 0, 2);
		Button button = Button("Capture fresh preview");
		button.Click += delegate
		{
			capture();
		};
		flowLayoutPanel2.Controls.Add(button);
		Button button2 = Button("Save reviewed ZIP…");
		flowLayoutPanel2.Controls.Add(button2);
		button2.Click += delegate
		{
			using SaveFileDialog saveFileDialog = new SaveFileDialog
			{
				Filter = "Diagnostics ZIP|*.zip",
				FileName = "Aster-diagnostics-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".zip"
			};
			if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
			{
				try
				{
					DiagnosticsBundle.Write(saveFileDialog.FileName, report.Text, csv.Text);
					MessageBox.Show(this, "Saved the two files exactly as previewed. Nothing was uploaded.", "Diagnostics saved");
					return;
				}
				catch (Exception ex)
				{
					MessageBox.Show(this, "Could not save diagnostics: " + ex.Message);
					return;
				}
			}
		};
	}

	private void BuildOfflineUpdates()
	{
		if (!updateCloseHook)
		{
			updateCloseHook = true;
			base.FormClosing += delegate(object s, FormClosingEventArgs e)
			{
				if (updateBusy)
				{
					e.Cancel = true;
					MessageBox.Show(this, "Wait for the update check or installation to finish before closing Aster.");
				}
			};
		}
		if (updateBusy)
		{
			Button button = new Button();
			button.Text = "An update operation is in progress. Click to refresh.";
			button.Dock = DockStyle.Top;
			button.Height = 50;
			Button button2 = button;
			button2.Click += delegate
			{
				Navigate("Update Aster");
			};
			content.Controls.Add(button2);
			return;
		}
		sub.Text = "Installed: Aster 2.8 · install a newer downloaded package";
		string selected = null;
		string installed = installedUpdateFolder;
		UpdatePackage reviewed = null;
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel();
		flowLayoutPanel.Dock = DockStyle.Fill;
		flowLayoutPanel.FlowDirection = FlowDirection.TopDown;
		flowLayoutPanel.WrapContents = false;
		flowLayoutPanel.AutoScroll = true;
		flowLayoutPanel.Padding = new Padding(20);
		FlowLayoutPanel flowLayoutPanel2 = flowLayoutPanel;
		content.Controls.Add(flowLayoutPanel2);
		flowLayoutPanel2.Controls.Add(new Label
		{
			Text = "Update Aster",
			AutoSize = true,
			Font = new Font("Segoe UI", 25f, FontStyle.Bold)
		});
		flowLayoutPanel2.Controls.Add(new Label
		{
			Text = "Choose a newer Aster ZIP to review and install. Your current copy stays intact.\r\nThere is no online update server connected yet. Updates use downloaded packages.",
			AutoSize = true,
			Margin = new Padding(0, 16, 0, 20)
		});
		Button choose = Button("Choose update ZIP…");
		Button install = Button("Install reviewed update");
		install.Enabled = false;
		Button launch = Button("Switch to updated Aster");
		launch.Enabled = installed != null;
		flowLayoutPanel2.Controls.Add(choose);
		flowLayoutPanel2.Controls.Add(install);
		flowLayoutPanel2.Controls.Add(launch);
		TextBox details = new TextBox
		{
			ReadOnly = true,
			Multiline = true,
			ScrollBars = ScrollBars.Vertical,
			Width = 740,
			Height = 230,
			Text = "Current version: 2.8\r\nChoose an update package to begin."
		};
		if (installed != null)
		{
			details.Text = "Previously installed update ready to open:\r\n" + installed;
		}
		flowLayoutPanel2.Controls.Add(details);
		flowLayoutPanel2.Controls.Add(new Label
		{
			Text = "Packages must contain release.json with a newer version and file hashes.\r\nHashes detect damage, not publisher identity. Only install a package you trust.\r\nYour home preset, place dictionary and shared workspace settings are preserved.\r\nThe old folder remains available if you need to return to the previous version.",
			AutoSize = true,
			Margin = new Padding(0, 16, 0, 0)
		});
		choose.Click += async delegate
		{
			using (OpenFileDialog openFileDialog = new OpenFileDialog
			{
				Filter = "Aster update package|*.zip",
				Title = "Choose a newer Aster release"
			})
			{
				if (openFileDialog.ShowDialog(this) != DialogResult.OK)
				{
					return;
				}
				selected = openFileDialog.FileName;
			}
			reviewed = null;
			installed = null;
			Button button3 = install;
			Button button4 = launch;
			bool flag = (choose.Enabled = false);
			bool enabled = (button4.Enabled = flag);
			button3.Enabled = enabled;
			updateBusy = true;
			details.Text = "Checking version, paths and file integrity…";
			try
			{
				int num = default(int);
				_ = num;
				_ = 0;
				try
				{
					UpdatePackage result = await Task.Run(() => AsterUpdate.Inspect(selected));
					if (!details.IsDisposed)
					{
						reviewed = result;
						details.Text = "Update version: " + result.Manifest.Version + "\r\nFiles: " + result.Manifest.Files.Count + "\r\nExpanded size: " + ((double)result.Bytes / 1048576.0).ToString("F1") + " MB\r\n\r\nSHA-256 of package:\r\n" + result.PackageHash + "\r\n\r\nReady for your review. Installation does not launch the package.";
						install.Enabled = true;
					}
				}
				catch (Exception ex)
				{
					if (!details.IsDisposed)
					{
						details.Text = "Package rejected: " + ex.Message;
					}
				}
			}
			finally
			{
				updateBusy = false;
				if (!choose.IsDisposed)
				{
					choose.Enabled = true;
				}
			}
		};
		install.Click += async delegate
		{
			if (reviewed == null || MessageBox.Show(this, "Install Aster " + reviewed.Manifest.Version + " from this ZIP?\r\nFile hashes passed, but the publisher is not authenticated. Continue only if you trust this package.\r\nYour current program folder will remain intact.", "Install update", MessageBoxButtons.OKCancel, MessageBoxIcon.Asterisk) != DialogResult.OK)
			{
				return;
			}
			Button button3 = install;
			bool enabled = (choose.Enabled = false);
			button3.Enabled = enabled;
			updateBusy = true;
			details.Text = "Installing into a new version folder…";
			try
			{
				int num = default(int);
				_ = num;
				_ = 0;
				try
				{
					string path = selected, hash = reviewed.PackageHash;
					string destination = await Task.Run(() => AsterUpdate.Install(path, hash, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Aster", "Versions"), AppDomain.CurrentDomain.BaseDirectory));
					installedUpdateFolder = destination;
					if (!details.IsDisposed)
					{
						installed = destination;
						details.Text = "Installed successfully.\r\n" + destination + "\r\n\r\nSwitch to updated Aster when ready. Your existing save/stop prompts still apply.";
						launch.Enabled = true;
					}
				}
				catch (Exception ex)
				{
					if (!details.IsDisposed)
					{
						details.Text = "Update could not finish: " + ex.Message + "\r\nThe current version was not replaced. Any incomplete new folder is not launched.";
					}
				}
			}
			finally
			{
				updateBusy = false;
				if (!choose.IsDisposed)
				{
					choose.Enabled = true;
				}
			}
		};
		launch.Click += delegate
		{
			if (installed != null)
			{
				string fileName = Path.Combine(installed, "Aster.exe");
				Close();
				if (base.IsDisposed)
				{
					try
					{
						Process.Start(new ProcessStartInfo(fileName)
						{
							UseShellExecute = true,
							WorkingDirectory = installed
						});
					}
					catch
					{
						MessageBox.Show("The updated app could not launch. Open Aster.exe in:\r\n" + installed, "Aster update");
					}
				}
			}
		};
	}

	private void BuildVideoPlayer()
	{
		sub.Text = "Your linked video · embedded YouTube player";
		content.Controls.Add(new EmbeddedVideo(delegate
		{
			Navigate("Air Defense feed");
		}));
	}

	private void AddStartupPreferences(FlowLayoutPanel flow)
	{
		flow.Controls.Add(new Label
		{
			Text = "Make an entrance",
			AutoSize = true,
			Font = new Font("Segoe UI", 24f, FontStyle.Bold)
		});
		flow.Controls.Add(new Label
		{
			Text = "Theme-matched stars, orbital lights, and a smooth workspace reveal.",
			AutoSize = true,
			Margin = new Padding(0, 8, 0, 14)
		});
		CheckBox enabled = new CheckBox
		{
			Text = "Show the startup introduction",
			AutoSize = true,
			Checked = preferences.StartupEnabled
		};
		enabled.CheckedChanged += delegate
		{
			preferences.StartupEnabled = enabled.Checked;
			SavePreferences();
		};
		flow.Controls.Add(enabled);
		ComboBox styles = new ComboBox
		{
			Width = 280,
			DropDownStyle = ComboBoxStyle.DropDownList,
			AccessibleName = "Startup animation style"
		};
		styles.Items.AddRange(new object[3] { "Orbit", "Warp", "Aurora" });
		styles.SelectedItem = StartupLauncher.Style(preferences.StartupStyle);
		styles.SelectedIndexChanged += delegate
		{
			preferences.StartupStyle = (string)styles.SelectedItem;
			SavePreferences();
		};
		flow.Controls.Add(styles);
		ComboBox length = new ComboBox
		{
			Width = 280,
			DropDownStyle = ComboBoxStyle.DropDownList,
			AccessibleName = "Startup animation length"
		};
		length.Items.AddRange(new object[3] { "Quick · 1.6 seconds", "Cinematic · 2.8 seconds", "Extended · 4.2 seconds" });
		int num = StartupLauncher.Duration(preferences.StartupDuration);
		length.SelectedIndex = num switch
		{
			4200 => 2, 
			1600 => 0, 
			_ => 1, 
		};
		length.SelectedIndexChanged += delegate
		{
			preferences.StartupDuration = (new int[3] { 1600, 2800, 4200 })[length.SelectedIndex];
			SavePreferences();
		};
		flow.Controls.Add(length);
		CheckBox reduced = new CheckBox
		{
			Text = "Reduce motion throughout Aster (skip startup animation)",
			AutoSize = true,
			Checked = !preferences.Motion
		};
		reduced.CheckedChanged += delegate
		{
			preferences.Motion = !reduced.Checked;
			SavePreferences();
			ConfigureFun();
		};
		flow.Controls.Add(reduced);
		Button button = Button("Preview introduction ↗");
		button.Click += delegate
		{
			using StartupScreen startupScreen = new StartupScreen(preferences);
			startupScreen.ShowDialog(this);
		};
		flow.Controls.Add(button);
		flow.Controls.Add(new Label
		{
			Text = "Enter or Escape skips the intro. Close exits startup. Reduced-motion previews are still.\r\nThis is a visual welcome, not a system scan. Repair tools open directly.",
			AutoSize = true,
			Margin = new Padding(0, 8, 0, 28)
		});
	}

	private void StopAirDefenseVideo()
	{
		if (airDefenseVideo != null)
		{
			airDefenseVideo.Dispose();
			airDefenseVideo = null;
		}
	}

	private void DetachAirDefense()
	{
		StopAirDefenseVideo();
		if (airDefensePanel != null && airDefensePanel.Parent != null)
		{
			airDefensePanel.Parent.Controls.Remove(airDefensePanel);
		}
		airDefenseTabs = null;
	}

	private void BuildAirDefense()
	{
		sub.Text = "Nearby public advisories and your embedded YouTube player";
		if (airDefensePanel == null)
		{
			airDefensePanel = new AlertPanel
			{
				Dock = DockStyle.Fill
			};
			airDefensePanel.WatchVideoRequested += delegate
			{
				if (airDefenseTabs != null && !airDefenseTabs.IsDisposed)
				{
					airDefenseTabs.SelectedIndex = 1;
				}
			};
			base.FormClosing += delegate(object s, FormClosingEventArgs e)
			{
				if (!e.Cancel && airDefensePanel.IsMonitoring && MessageBox.Show(this, "Close Aster and stop the alert monitor?", "Stop monitoring", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
				{
					e.Cancel = true;
				}
			};
			base.FormClosed += delegate
			{
				StopAirDefenseVideo();
				airDefensePanel.Dispose();
			};
		}
		airDefenseTabs = new TabControl
		{
			Dock = DockStyle.Fill,
			AccessibleName = "Air Defense feed sections",
			Padding = new Point(18, 9)
		};
		TabPage tabPage = new TabPage("Alerts & settings");
		TabPage videoTab = new TabPage("YouTube player");
		tabPage.Controls.Add(airDefensePanel);
		airDefenseTabs.TabPages.Add(tabPage);
		airDefenseTabs.TabPages.Add(videoTab);
		content.Controls.Add(airDefenseTabs);
		airDefenseTabs.SelectedIndexChanged += delegate
		{
			if (airDefenseTabs != null && !airDefenseTabs.IsDisposed && !airDefenseTabs.Disposing)
			{
				StopAirDefenseVideo();
				if (airDefenseTabs.SelectedIndex == 1)
				{
					while (videoTab.Controls.Count > 0)
					{
						videoTab.Controls[0].Dispose();
					}
					TableLayoutPanel tableLayoutPanel = new TableLayoutPanel
					{
						Dock = DockStyle.Fill,
						ColumnCount = 1,
						RowCount = 2
					};
					tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 58f));
					tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
					videoTab.Controls.Add(tableLayoutPanel);
					tableLayoutPanel.Controls.Add(new Label
					{
						Text = "Your linked video plays here. Started alert monitoring continues while you watch.\r\nVideo is not processed for proximity alerts. YouTube may show ads or restrict playback.",
						Dock = DockStyle.Fill,
						Padding = new Padding(12, 8, 12, 0)
					}, 0, 0);
					airDefenseVideo = new EmbeddedVideo(delegate
					{
						if (airDefenseTabs != null)
						{
							airDefenseTabs.SelectedIndex = 0;
						}
					});
					tableLayoutPanel.Controls.Add(airDefenseVideo, 0, 1);
					ApplyAppTheme();
				}
			}
		};
	}

	private void RememberTool(string name)
	{
		AddSessionEvent("Opened " + name);
		if (!(name == "Tools hub"))
		{
			recentTools.Remove(name);
			recentTools.Insert(0, name);
			if (recentTools.Count > 6)
			{
				recentTools.RemoveAt(6);
			}
		}
	}

	private string[][] NavigationGroups(string query)
	{
		if (query.Length > 0)
		{
			return new string[1][] { new string[1] { "SEARCH ALL TOOLS" }.Concat(buttons.Keys.Where((string n) => ToolCatalog.Matches(n, query) && !(preferences.SimpleMode && HubModel.IsAdvanced(n)))).ToArray() };
		}
		return new string[3][]
		{
			new string[1] { "PINNED" }.Concat(preferences.Pins.Where(buttons.ContainsKey)).ToArray(),
			new string[7] { "MAIN", "Command Center", "Summary", "Performance", "Processes", "Tools hub", "System check" },
			new string[3] { "PERSONALIZE", "Preferences", "Update Aster" }
		};
	}

	private void BuildToolsHub()
	{
		sub.Text = "Everything Aster can do · search, pin ☆ your favourites, or turn on Simple mode";
		var view = new ToolsHubView(ToolCatalog.Groups, ToolCatalog.Hint, new HashSet<string>(preferences.Pins),
			() => recentTools, preferences.SimpleMode, (string n) => buttons.ContainsKey(n),
			(string n) => Navigate(n),
			(string n) => { if (preferences.Pins.Contains(n)) preferences.Pins.Remove(n); else preferences.Pins.Add(n); SavePreferences(); RebuildNavigation(); },
			(bool on) => { preferences.SimpleMode = on; SavePreferences(); },
			panel, Color.FromArgb(204, 215, 229), muted, Color.FromArgb(94, 196, 214));
		content.Controls.Add(view);
		ApplyAppTheme();
	}

	private void BuildSystemCheck()
	{
		sub.Text = "Read-only review · refresh when needed · export or prepare help";
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel();
		tableLayoutPanel.Dock = DockStyle.Fill;
		tableLayoutPanel.RowCount = 2;
		tableLayoutPanel.ColumnCount = 1;
		TableLayoutPanel tableLayoutPanel2 = tableLayoutPanel;
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		content.Controls.Add(tableLayoutPanel2);
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel();
		flowLayoutPanel.Dock = DockStyle.Fill;
		FlowLayoutPanel flowLayoutPanel2 = flowLayoutPanel;
		TextBox report = new TextBox
		{
			Multiline = true,
			ReadOnly = true,
			ScrollBars = ScrollBars.Vertical,
			Dock = DockStyle.Fill,
			Font = new Font("Segoe UI", 11f),
			Text = "Reading storage and power…"
		};
		tableLayoutPanel2.Controls.Add(flowLayoutPanel2, 0, 0);
		tableLayoutPanel2.Controls.Add(report, 0, 1);
		Button refresh = Button("Refresh check");
		Button copy = Button("Copy report");
		Button save = Button("Save report");
		Button ai = Button("Prepare AI help");
		flowLayoutPanel2.Controls.AddRange(new Control[4] { refresh, copy, save, ai });
		Action<bool> ready = delegate(bool value)
		{
			Button button = copy;
			Button button2 = save;
			bool flag = (ai.Enabled = value);
			bool enabled = (button2.Enabled = flag);
			button.Enabled = enabled;
		};
		Action capture = async delegate
		{
			ready(obj: false);
			refresh.Enabled = false;
			try
			{
				int num = default(int);
				_ = num;
				_ = 0;
				try
				{
					Snapshot snapshot = current;
					string text = await Task.Run(() => SystemReview.Capture(snapshot));
					if (!report.IsDisposed)
					{
						report.Text = text;
						ready(obj: true);
					}
				}
				catch (Exception ex)
				{
					if (!report.IsDisposed)
					{
						report.Text = "Could not complete review: " + ex.Message;
					}
				}
			}
			finally
			{
				if (!refresh.IsDisposed)
				{
					refresh.Enabled = true;
				}
			}
		};
		refresh.Click += delegate
		{
			capture();
		};
		copy.Click += delegate
		{
			try
			{
				Clipboard.SetText(report.Text);
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, ex.Message);
			}
		};
		save.Click += delegate
		{
			using SaveFileDialog saveFileDialog = new SaveFileDialog
			{
				Filter = "Text report|*.txt",
				FileName = "Aster-system-check.txt"
			};
			if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
			{
				try
				{
					File.WriteAllText(saveFileDialog.FileName, report.Text);
					return;
				}
				catch (Exception ex)
				{
					MessageBox.Show(this, ex.Message);
					return;
				}
			}
		};
		ai.Click += delegate
		{
			string text = report.Text;
			Navigate("Windows advisor");
			content.Controls.OfType<AdvisorPanel>().FirstOrDefault()?.PrepareEvidence(text);
		};
		capture();
	}

	private static string Metric(double n, string unit)
	{
		if (!double.IsNaN(n))
		{
			return n.ToString("F1") + unit;
		}
		return "Unavailable";
	}

	private void BuildLegacySummary()
	{
		Panel panel = new Panel();
		panel.Dock = DockStyle.Fill;
		panel.AutoScroll = true;
		Panel panel2 = panel;
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel();
		tableLayoutPanel.Dock = DockStyle.Top;
		tableLayoutPanel.Height = 660;
		tableLayoutPanel.ColumnCount = 2;
		tableLayoutPanel.RowCount = 4;
		TableLayoutPanel tableLayoutPanel2 = tableLayoutPanel;
		tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
		tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 88f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 245f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 225f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 102f));
		panel2.Controls.Add(tableLayoutPanel2);
		content.Controls.Add(panel2);
		liveOverview = new Label
		{
			Dock = DockStyle.Fill,
			BackColor = this.panel,
			ForeColor = cyan,
			Font = new Font("Segoe UI", 12f),
			Padding = new Padding(14),
			Margin = new Padding(0, 0, 0, 12),
			Text = "Waiting for live samples…"
		};
		tableLayoutPanel2.Controls.Add(liveOverview, 0, 0);
		tableLayoutPanel2.SetColumnSpan(liveOverview, 2);
		summaryCpu = NewGraph("CPU", cyan, scale: false);
		summaryRam = NewGraph("MEMORY", Color.FromArgb(188, 133, 245), scale: false);
		tableLayoutPanel2.Controls.Add(summaryCpu, 0, 1);
		tableLayoutPanel2.Controls.Add(summaryRam, 1, 1);
		foreach (Snapshot item in history)
		{
			summaryCpu.Add(item.Cpu);
			summaryRam.Add(item.MemoryPercent);
		}
		TableLayoutPanel tableLayoutPanel3 = new TableLayoutPanel();
		tableLayoutPanel3.Dock = DockStyle.Fill;
		tableLayoutPanel3.RowCount = 2;
		tableLayoutPanel3.ColumnCount = 1;
		tableLayoutPanel3.Margin = new Padding(0, 0, 14, 12);
		TableLayoutPanel tableLayoutPanel4 = tableLayoutPanel3;
		tableLayoutPanel4.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));
		tableLayoutPanel4.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		tableLayoutPanel4.Controls.Add(new Label
		{
			Text = "BUSIEST PROCESSES · CPU",
			Dock = DockStyle.Fill,
			ForeColor = muted
		}, 0, 0);
		topProcesses = MakeGrid();
		topProcesses.ColumnHeadersHeight = 30;
		topProcesses.RowTemplate.Height = 27;
		string[] array = new string[4] { "Process", "PID", "CPU %", "RAM MB" };
		foreach (string text in array)
		{
			topProcesses.Columns.Add(text, text);
		}
		topProcesses.Columns[0].FillWeight = 180f;
		tableLayoutPanel4.Controls.Add(topProcesses, 0, 1);
		tableLayoutPanel2.Controls.Add(tableLayoutPanel4, 0, 2);
		liveHardware = new Label
		{
			Dock = DockStyle.Fill,
			BackColor = this.panel,
			ForeColor = Color.FromArgb(215, 226, 239),
			Padding = new Padding(12),
			Margin = new Padding(0, 0, 0, 12),
			Font = new Font("Segoe UI", 10f)
		};
		tableLayoutPanel2.Controls.Add(liveHardware, 1, 2);
		sensorStrip = new Label
		{
			Dock = DockStyle.Fill,
			BackColor = this.panel,
			ForeColor = muted,
			Padding = new Padding(12),
			Text = "GPU sensors: reading…",
			Font = new Font("Segoe UI", 9f)
		};
		tableLayoutPanel2.Controls.Add(sensorStrip, 0, 3);
		tableLayoutPanel2.SetColumnSpan(sensorStrip, 2);
	}

	private void UpdateSummary(bool fresh)
	{
		if (summaryCpu == null || current == null)
		{
			return;
		}
		if (fresh)
		{
			summaryCpu.Add(current.Cpu);
			summaryRam.Add(current.MemoryPercent);
		}
		summaryCpu.Value = Metric(current.Cpu, "%");
		summaryCpu.Hint = Environment.ProcessorCount + " logical processors · 60 samples";
		summaryRam.Value = Metric(current.MemoryPercent, "%");
		summaryRam.Hint = Inventory.Bytes(current.Total - current.Available) + " / " + Inventory.Bytes(current.Total);
		summaryCpu.Invalidate();
		summaryRam.Invalidate();
		liveOverview.Text = "CPU  " + Metric(current.Cpu, "%") + "       RAM  " + Metric(current.MemoryPercent, "%") + "       PROCESSES  " + current.Processes.Count + "\r\nNETWORK  ↓ " + Inventory.Bytes(current.Receive) + "/s   ↑ " + Inventory.Bytes(current.Send) + "/s       DISK  R " + Inventory.Bytes(current.DiskRead) + "/s   W " + Inventory.Bytes(current.DiskWrite) + "/s";
		topProcesses.Rows.Clear();
		foreach (Proc item in current.Processes.OrderByDescending((Proc p) => p.Cpu).Take(6))
		{
			topProcesses.Rows.Add(item.Name, item.Id, Math.Round(item.Cpu, 1), Math.Round((double)item.Memory / 1048576.0, 1));
		}
		topProcesses.ClearSelection();
		liveHardware.Text = "HARDWARE AT A GLANCE\r\n\r\n" + cpuName + "\r\n" + Inventory.Bytes(current.Total) + " usable RAM · " + Inventory.Bytes(current.Available) + " available\r\n" + storageOverview + "\r\n\r\n" + ((current.MemoryPercent >= 85.0) ? "Memory use is above 85%." : "Memory use is below 85%.") + "\r\n" + ((current.Cpu >= 90.0) ? "CPU use is above 90%." : "CPU use is below 90%.");
		sensorStrip.Text = SensorText();
		sub.Text = "Live overview · " + current.Time.ToString("HH:mm:ss") + " · detailed hardware specs in the sidebar";
	}

	private string SensorText()
	{
		if (sensors.Count == 0)
		{
			return gpuInfo + "\r\n" + gpuNote;
		}
		return string.Join("\r\n", sensors.Select((GpuSensor g) => g.Name + "   " + Metric(g.Load, "%") + "   VRAM " + Metric(g.Used / 1024.0, " GB") + " / " + Metric(g.Total / 1024.0, " GB") + "   " + Metric(g.Temp, " °C") + "   " + Metric(g.Power, " W") + "   " + Metric(g.Clock, " MHz"))) + "\r\nNVIDIA sensors · " + sensorTime.ToString("HH:mm:ss") + " · other adapters: GPU specs";
	}

	private List<string[]> GpuRows()
	{
		Table table = new Table("Component", "Specification", "Value");
		AppendSensors(table);
		table.Rows.AddRange(inventory.Rows);
		return table.Rows;
	}

	private void AppendSensors(Table t)
	{
		foreach (GpuSensor sensor in sensors)
		{
			string text = "LIVE · NVIDIA GPU " + sensor.Index + " · " + sensor.Name;
			string[][] array = new string[9][]
			{
				new string[2]
				{
					"GPU utilization",
					Metric(sensor.Load, "%")
				},
				new string[2]
				{
					"Dedicated memory total",
					Metric(sensor.Total, " MiB")
				},
				new string[2]
				{
					"Dedicated memory used",
					Metric(sensor.Used, " MiB")
				},
				new string[2]
				{
					"Temperature",
					Metric(sensor.Temp, " °C")
				},
				new string[2]
				{
					"Board power",
					Metric(sensor.Power, " W")
				},
				new string[2]
				{
					"Power limit",
					Metric(sensor.Limit, " W")
				},
				new string[2]
				{
					"Graphics clock",
					Metric(sensor.Clock, " MHz")
				},
				new string[2] { "Driver", sensor.Driver },
				new string[2]
				{
					"Sampled at",
					sensorTime.ToString("HH:mm:ss")
				}
			};
			foreach (string[] array2 in array)
			{
				t.Rows.Add(new string[3]
				{
					text,
					array2[0],
					array2[1]
				});
			}
		}
	}

	private void ReadStorageOverview()
	{
		BackgroundWorker worker = new BackgroundWorker();
		worker.DoWork += delegate(object s, DoWorkEventArgs e)
		{
			Table table = Inventory.Hardware("Storage specs");
			string[] array = (from r in table.Rows
				where r[0].StartsWith("Disk ") && r[1] == "Model"
				select r[2]).ToArray();
			e.Result = ((array.Length == 0) ? "Storage specs unavailable" : (array.Length + " physical drive(s)\r\n" + string.Join("\r\n", array.Take(3))));
		};
		worker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs e)
		{
			worker.Dispose();
			if (!base.IsDisposed)
			{
				storageOverview = ((e.Error == null) ? ((string)e.Result) : "Storage specs unavailable");
			}
		};
		worker.RunWorkerAsync();
	}

	private void BuildRecovery()
	{
		sub.Text = "Diagnose first. Repair only the part that needs it. Each tool explains its changes before it runs.";
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel();
		flowLayoutPanel.Dock = DockStyle.Fill;
		flowLayoutPanel.AutoScroll = true;
		flowLayoutPanel.FlowDirection = FlowDirection.TopDown;
		flowLayoutPanel.WrapContents = false;
		flowLayoutPanel.Padding = new Padding(0, 8, 0, 8);
		FlowLayoutPanel flowLayoutPanel2 = flowLayoutPanel;
		content.Controls.Add(flowLayoutPanel2);
		Label label = new Label();
		label.Text = "WINDOWS REPAIR TOOLS\r\nAdministrator tools open in a separate progress window. Windows asks for permission.\r\nNo automatic reboot, file cleanup or registry tuning is performed.";
		label.ForeColor = muted;
		label.AutoSize = true;
		label.Margin = new Padding(0, 0, 0, 20);
		Label value = label;
		flowLayoutPanel2.Controls.Add(value);
		RecoveryJob[] all = RecoveryJob.All;
		foreach (RecoveryJob recoveryJob in all)
		{
			RecoveryJob j = recoveryJob;
			Panel panel = new Panel();
			panel.Width = 850;
			panel.Height = 104;
			panel.BackColor = this.panel;
			panel.Margin = new Padding(0, 0, 0, 12);
			Panel panel2 = panel;
			Label label2 = new Label();
			label2.Text = j.Title;
			label2.Location = new Point(14, 10);
			label2.AutoSize = true;
			label2.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
			Label label3 = label2;
			Label label4 = new Label();
			label4.Text = j.Description;
			label4.Location = new Point(14, 38);
			label4.Width = 660;
			label4.Height = 62;
			label4.ForeColor = muted;
			Label label5 = label4;
			Button button = Button("Open tool");
			button.Location = new Point(720, 31);
			button.Click += delegate
			{
				LaunchRepair(j);
			};
			panel2.Controls.AddRange(new Control[3] { label3, label5, button });
			flowLayoutPanel2.Controls.Add(panel2);
		}
		flowLayoutPanel2.Controls.Add(new Label
		{
			Text = "WINDOWS RECOVERY SHORTCUTS",
			AutoSize = true,
			Margin = new Padding(0, 18, 0, 12),
			ForeColor = cyan
		});
		AddShortcut(flowLayoutPanel2, "Restore points / protection", "SystemPropertiesProtection.exe", "");
		AddShortcut(flowLayoutPanel2, "System Restore wizard", "rstrui.exe", "");
		AddShortcut(flowLayoutPanel2, "Memory diagnostic (offers a restart)", "mdsched.exe", "");
		AddShortcut(flowLayoutPanel2, "Reliability history", "perfmon.exe", "/rel");
		AddShortcut(flowLayoutPanel2, "Event Viewer", "eventvwr.exe", "");
		AddShortcut(flowLayoutPanel2, "Disk Cleanup (choose what to remove)", "cleanmgr.exe", "");
		AddShortcut(flowLayoutPanel2, "Windows Update", "ms-settings:windowsupdate", "");
		AddShortcut(flowLayoutPanel2, "Windows troubleshooters", "ms-settings:troubleshoot", "");
		AddShortcut(flowLayoutPanel2, "Recovery settings", "ms-settings:recovery", "");
		Button button2 = Button("Open recovery logs");
		button2.Click += delegate
		{
			string text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Pulse", "RecoveryLogs");
			Directory.CreateDirectory(text);
			Process.Start(new ProcessStartInfo(text)
			{
				UseShellExecute = true
			});
		};
		flowLayoutPanel2.Controls.Add(button2);
	}

	private void AddShortcut(FlowLayoutPanel p, string title, string exe, string args)
	{
		Button button = Button(title);
		button.Click += delegate
		{
			try
			{
				Process.Start(new ProcessStartInfo(exe.StartsWith("ms-settings:") ? exe : Path.Combine(Environment.SystemDirectory, exe), args)
				{
					UseShellExecute = true
				});
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, ex.Message, "Could not open Windows tool");
			}
		};
		p.Controls.Add(button);
	}

	private void LaunchRepair(RecoveryJob job)
	{
		if (MessageBox.Show(this, job.Description + "\r\n\r\nCommand: " + job.Exe + " " + job.Arguments + "\r\n\r\nContinue and request administrator access?", job.Title, MessageBoxButtons.OKCancel, MessageBoxIcon.Asterisk, MessageBoxDefaultButton.Button2) != DialogResult.OK)
		{
			return;
		}
		try
		{
			ProcessStartInfo processStartInfo = new ProcessStartInfo(Application.ExecutablePath, "--repair " + job.Id);
			processStartInfo.UseShellExecute = true;
			processStartInfo.Verb = "runas";
			Process.Start(processStartInfo);
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, ex.Message, "Tool not started");
		}
	}

	private void BuildWallpaperLauncher()
	{
		sub.Text = "Aura · a separate live wallpaper studio";
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel();
		flowLayoutPanel.Dock = DockStyle.Fill;
		flowLayoutPanel.FlowDirection = FlowDirection.TopDown;
		flowLayoutPanel.Padding = new Padding(10);
		FlowLayoutPanel flowLayoutPanel2 = flowLayoutPanel;
		flowLayoutPanel2.Controls.Add(new Label
		{
			Text = "Make the desktop yours.",
			AutoSize = true,
			Font = new Font("Segoe UI", 24f),
			Margin = new Padding(0, 20, 0, 15)
		});
		flowLayoutPanel2.Controls.Add(new Label
		{
			Text = "Twelve anime character artworks, a motion editor and looping GIF creation,\r\nGIF / video import, playlists and automatic pause while gaming or on battery.\r\n\r\nPreview first, then choose Apply to desktop in Aura.",
			ForeColor = muted,
			AutoSize = true,
			Margin = new Padding(0, 0, 0, 20)
		});
		Button button = Button("Open Aura Wallpaper");
		button.Click += delegate
		{
			try
			{
				string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
				string text = Path.Combine(baseDirectory, "AuraWallpaper.exe");
				if (!File.Exists(text))
				{
					throw new FileNotFoundException("AuraWallpaper.exe is missing. Extract the entire Aster ZIP, then try again.");
				}
				Process.Start(new ProcessStartInfo(text)
				{
					WorkingDirectory = baseDirectory,
					UseShellExecute = true
				});
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, ex.Message, "Could not open Aura");
			}
		};
		flowLayoutPanel2.Controls.Add(button);
		content.Controls.Add(flowLayoutPanel2);
	}

	private void BuildAdvisor()
	{
		sub.Text = "Read-only diagnosis · optional local AI · reviewed automatic repair plans";
		content.Controls.Add(new AdvisorPanel(LaunchPlan, delegate(bool value)
		{
			timer.Interval = (value ? 3000 : 1000);
		}));
	}

	private void LaunchPlan(RepairPlan plan)
	{
		if (MessageBox.Show(this, plan.Review(), "Review automatic plan", MessageBoxButtons.OKCancel, MessageBoxIcon.Asterisk, MessageBoxDefaultButton.Button2) != DialogResult.OK)
		{
			return;
		}
		try
		{
			ProcessStartInfo processStartInfo = new ProcessStartInfo(Application.ExecutablePath, "--plan " + plan.Id);
			processStartInfo.UseShellExecute = true;
			processStartInfo.Verb = "runas";
			Process.Start(processStartInfo);
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, ex.Message, "Plan not started");
		}
	}

	private void SetupShortcuts()
	{
		base.KeyPreview = true;
		base.KeyDown += delegate(object s, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.F11)
			{
				e.Handled = true;
				e.SuppressKeyPress = true;
				ToggleFocusMode();
			}
			else if (e.Control && e.KeyCode == Keys.K)
			{
				e.Handled = true;
				e.SuppressKeyPress = true;
				ShowPalette();
			}
			else if (e.KeyCode == Keys.F5)
			{
				e.Handled = true;
				if (IsInventory())
				{
					LoadInventory();
				}
				else
				{
					StartSample();
				}
			}
		};
	}

	private void ShowPalette()
	{
		ShowActionLauncher();
	}

	private void BuildCompact()
	{
		sub.Text = "A small always-on-top display using the existing monitor samples · Ctrl+K opens quick navigation";
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel();
		flowLayoutPanel.Dock = DockStyle.Fill;
		flowLayoutPanel.FlowDirection = FlowDirection.TopDown;
		flowLayoutPanel.Padding = new Padding(14);
		FlowLayoutPanel flowLayoutPanel2 = flowLayoutPanel;
		flowLayoutPanel2.Controls.Add(new Label
		{
			Text = "Keep the essentials in view.",
			AutoSize = true,
			Font = new Font("Segoe UI", 22f)
		});
		flowLayoutPanel2.Controls.Add(new Label
		{
			Text = "CPU, RAM, network and process counts in a compact floating panel.\r\nIt reuses Aster's readings, so it does not add a second system sampler.\r\nClose the panel at any time. Aster must stay open.",
			AutoSize = true,
			ForeColor = muted,
			Margin = new Padding(0, 20, 0, 20)
		});
		Button button = Button("Open floating monitor");
		button.Click += delegate
		{
			if (compact != null && !compact.IsDisposed)
			{
				compact.Activate();
			}
			else
			{
				compact = new Form
				{
					Text = "Aster · Mini monitor",
					Size = new Size(370, 250),
					MinimumSize = new Size(300, 220),
					TopMost = true,
					BackColor = panel,
					ForeColor = Color.White,
					Icon = SuiteArt.AppIcon()
				};
				Label text = new Label
				{
					Dock = DockStyle.Fill,
					Font = new Font("Segoe UI", 15f),
					Padding = new Padding(18)
				};
				compact.Controls.Add(text);
				System.Windows.Forms.Timer clock = new System.Windows.Forms.Timer
				{
					Interval = 1000
				};
				clock.Tick += delegate
				{
					if (current != null)
					{
						text.Text = "CPU  " + current.Cpu.ToString("F1") + "%\r\nRAM  " + current.MemoryPercent.ToString("F1") + "%\r\n↓ " + Inventory.Bytes(current.Receive) + "/s  ↑ " + Inventory.Bytes(current.Send) + "/s\r\n" + current.Processes.Count + " processes · " + current.Time.ToString("T");
					}
				};
				compact.FormClosed += delegate
				{
					clock.Dispose();
				};
				clock.Start();
				compact.Show(this);
			}
		};
		flowLayoutPanel2.Controls.Add(button);
		content.Controls.Add(flowLayoutPanel2);
	}

	private Color AccentColor()
	{
		if (!(preferences.Accent == "Sunset"))
		{
			if (!(preferences.Accent == "Violet"))
			{
				if (!(preferences.Accent == "Mint"))
				{
					return Color.FromArgb(69, 204, 238);
				}
				return Color.FromArgb(81, 220, 175);
			}
			return Color.FromArgb(177, 145, 255);
		}
		return Color.FromArgb(255, 172, 123);
	}

	private bool SavePreferences()
	{
		try
		{
			preferences.Save();
			return true;
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, "Could not save preferences: " + ex.Message);
			return false;
		}
	}

	private void SetupWorkspace(Panel side, Panel head)
	{
		sidebar = side;
		cyan = AccentColor();
		timer.Interval = preferences.Interval;
		base.TopMost = preferences.AlwaysOnTop;
		side.Padding = new Padding(14);
		Control[] array = side.Controls.Cast<Control>().ToArray();
		foreach (Control control in array)
		{
			if (control != nav)
			{
				control.Dispose();
			}
		}
		nav.Controls.Clear();
		nav.Padding = new Padding(0);
		Label label = new Label();
		label.Text = "ASTER";
		label.Font = new Font("Segoe UI", 20f, FontStyle.Bold);
		label.ForeColor = Color.White;
		label.Dock = DockStyle.Top;
		label.Height = 76;
		label.Image = SuiteArt.Badge(44);
		label.ImageAlign = ContentAlignment.MiddleLeft;
		label.TextAlign = ContentAlignment.MiddleRight;
		label.Padding = new Padding(0, 0, 20, 0);
		Label label2 = (dailyLogo = label);
		side.Controls.Add(label2);
		Panel panel = new Panel();
		panel.Dock = DockStyle.Top;
		panel.Height = 55;
		panel.Padding = new Padding(0, 6, 0, 10);
		Panel panel2 = panel;
		navFilter = new TextBox
		{
			Dock = DockStyle.Fill,
			BackColor = this.panel,
			ForeColor = Color.White,
			BorderStyle = BorderStyle.FixedSingle,
			Font = new Font("Segoe UI", 11f)
		};
		panel2.Controls.Add(navFilter);
		side.Controls.Add(panel2);
		Label label3 = new Label();
		label3.Dock = DockStyle.Bottom;
		label3.Height = 48;
		label3.Text = "YOUR SYSTEM, AT A GLANCE\r\nCtrl+K to jump anywhere";
		label3.ForeColor = muted;
		label3.Font = new Font("Segoe UI", 8f);
		label3.TextAlign = ContentAlignment.MiddleLeft;
		Label label4 = label3;
		side.Controls.Add(label4);
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel();
		tableLayoutPanel.Dock = DockStyle.Fill;
		tableLayoutPanel.ColumnCount = 1;
		tableLayoutPanel.RowCount = 4;
		tableLayoutPanel.Margin = new Padding(0);
		TableLayoutPanel tableLayoutPanel2 = tableLayoutPanel;
		tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 76f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));
		side.Controls.Clear();
		side.Controls.Add(tableLayoutPanel2);
		FlowLayoutPanel flowLayoutPanel = nav;
		DockStyle dockStyle = (label4.Dock = DockStyle.Fill);
		DockStyle dockStyle3 = (flowLayoutPanel.Dock = dockStyle);
		DockStyle dock = (panel2.Dock = dockStyle3);
		label2.Dock = dock;
		FlowLayoutPanel flowLayoutPanel2 = nav;
		Padding padding = (label4.Margin = new Padding(0));
		Padding padding3 = (flowLayoutPanel2.Margin = padding);
		Padding margin = (panel2.Margin = padding3);
		label2.Margin = margin;
		panel2.Padding = new Padding(0, 23, 0, 8);
		Label label5 = new Label();
		label5.Text = "Search pages";
		label5.AutoSize = true;
		label5.Location = new Point(0, 0);
		label5.ForeColor = muted;
		label5.Font = new Font("Segoe UI", 8f);
		Label value = label5;
		panel2.Controls.Add(value);
		tableLayoutPanel2.Controls.Add(label2, 0, 0);
		tableLayoutPanel2.Controls.Add(panel2, 0, 1);
		tableLayoutPanel2.Controls.Add(nav, 0, 2);
		tableLayoutPanel2.Controls.Add(label4, 0, 3);
		navFilter.TextChanged += delegate
		{
			RebuildNavigation();
		};
		FlowLayoutPanel flowLayoutPanel3 = new FlowLayoutPanel();
		flowLayoutPanel3.Dock = DockStyle.Right;
		flowLayoutPanel3.Width = 295;
		flowLayoutPanel3.FlowDirection = FlowDirection.RightToLeft;
		FlowLayoutPanel flowLayoutPanel4 = flowLayoutPanel3;
		head.Controls.Add(flowLayoutPanel4);
		pinButton = Button("☆ Pin page");
		pinButton.Click += delegate
		{
			if (preferences.Pins.Contains(page))
			{
				preferences.Pins.Remove(page);
			}
			else
			{
				preferences.Pins.Add(page);
			}
			SavePreferences();
			RebuildNavigation();
			StyleView();
		};
		flowLayoutPanel4.Controls.Add(pinButton);
		Button button = Button("⌕  Jump to…");
		button.Click += delegate
		{
			ShowPalette();
		};
		flowLayoutPanel4.Controls.Add(button);
		RebuildNavigation();
	}

	private void RebuildNavigation()
	{
		nav.SuspendLayout();
		foreach (Button value in buttons.Values)
		{
			nav.Controls.Remove(value);
		}
		Control[] array = nav.Controls.Cast<Control>().ToArray();
		foreach (Control control in array)
		{
			control.Dispose();
		}
		nav.Controls.Clear();
		string query = navFilter.Text.Trim();
		string[][] array2 = NavigationGroups(query);
		HashSet<string> seen = new HashSet<string>();
		string[][] array3 = array2;
		foreach (string[] array4 in array3)
		{
			string[] array5 = (from n in array4.Skip(1)
				where !seen.Contains(n) && ToolCatalog.Matches(n, query)
				select n).ToArray();
			if (array5.Length != 0)
			{
				nav.Controls.Add(new Label
				{
					Text = array4[0],
					ForeColor = muted,
					Width = 184,
					Height = 28,
					Font = new Font("Segoe UI", 8f, FontStyle.Bold),
					Padding = new Padding(8, 9, 0, 0),
					Margin = new Padding(0, 7, 0, 0)
				});
				string[] array6 = array5;
				foreach (string text in array6)
				{
					seen.Add(text);
					Button button = buttons[text];
					button.Width = 184;
					button.Height = 37;
					button.Margin = new Padding(0, 2, 0, 2);
					button.Padding = new Padding(12, 0, 0, 0);
					button.BackColor = ((text == page) ? Color.FromArgb(35, 62, 79) : sidebar.BackColor);
					button.ForeColor = ((text == page) ? cyan : Color.FromArgb(185, 201, 222));
					nav.Controls.Add(button);
				}
			}
		}
		nav.ResumeLayout();
		if (content != null)
		{
			ApplyAppTheme();
		}
	}

	private void StyleView()
	{
		ConfigureFun();
		CleanToolbar();
		ApplyAppTheme();
		if (pinButton != null)
		{
			pinButton.Text = (preferences.Pins.Contains(page) ? "★ Pinned" : "☆ Pin page");
		}
	}

	private Label Block(string text, int size)
	{
		Label label = new Label();
		label.Text = text;
		label.Dock = DockStyle.Fill;
		label.ForeColor = Color.FromArgb(215, 229, 244);
		label.Font = new Font("Segoe UI", size);
		label.Padding = new Padding(18);
		label.BackColor = panel;
		label.Margin = new Padding(0, 0, 12, 12);
		return label;
	}

	private void BuildSummary()
	{
		Panel panel = new Panel();
		panel.Dock = DockStyle.Fill;
		panel.AutoScroll = true;
		Panel panel2 = panel;
		content.Controls.Add(panel2);
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel();
		tableLayoutPanel.Dock = DockStyle.Top;
		tableLayoutPanel.Height = 979;
		tableLayoutPanel.ColumnCount = 2;
		tableLayoutPanel.RowCount = 7;
		TableLayoutPanel tableLayoutPanel2 = tableLayoutPanel;
		tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58f));
		tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f));
		int[] array = new int[7] { 150, 134, 50, 220, 225, 105, 95 };
		foreach (int num in array)
		{
			tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, num));
		}
		panel2.Controls.Add(tableLayoutPanel2);
		Control control = BuildGreetingHero();
		tableLayoutPanel2.Controls.Add(control, 0, 0);
		tableLayoutPanel2.SetColumnSpan(control, 2);
		TableLayoutPanel tableLayoutPanel3 = new TableLayoutPanel();
		tableLayoutPanel3.Dock = DockStyle.Fill;
		tableLayoutPanel3.ColumnCount = 4;
		tableLayoutPanel3.RowCount = 1;
		tableLayoutPanel3.Margin = new Padding(0);
		TableLayoutPanel tableLayoutPanel4 = tableLayoutPanel3;
		for (int j = 0; j < 4; j++)
		{
			tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
		}
		tileCpu = new MetricTile
		{
			Caption = "PROCESSOR",
			Accent = cyan
		};
		tileRam = new MetricTile
		{
			Caption = "MEMORY",
			Accent = Color.FromArgb(180, 145, 255)
		};
		tileGpu = new MetricTile
		{
			Caption = "GPU · WINDOWS",
			Accent = Color.FromArgb(92, 218, 176)
		};
		tileNet = new MetricTile
		{
			Caption = "NETWORK · RECEIVE",
			Accent = Color.FromArgb(244, 184, 99)
		};
		tableLayoutPanel4.Controls.Add(tileCpu, 0, 0);
		tableLayoutPanel4.Controls.Add(tileRam, 1, 0);
		tableLayoutPanel4.Controls.Add(tileGpu, 2, 0);
		tableLayoutPanel4.Controls.Add(tileNet, 3, 0);
		tableLayoutPanel2.Controls.Add(tableLayoutPanel4, 0, 1);
		tableLayoutPanel2.SetColumnSpan(tableLayoutPanel4, 2);
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel();
		flowLayoutPanel.Dock = DockStyle.Fill;
		flowLayoutPanel.WrapContents = false;
		FlowLayoutPanel flowLayoutPanel2 = flowLayoutPanel;
		string[][] array2 = new string[4][]
		{
			new string[2] { "Investigate an issue", "Windows advisor" },
			new string[2] { "View insights", "Insights" },
			new string[2] { "Personalize", "Preferences" },
			new string[2] { "Live wallpapers", "Wallpaper studio" }
		};
		foreach (string[] array3 in array2)
		{
			string target = array3[1];
			Button button = Button(array3[0] + "  →");
			button.Click += delegate
			{
				Navigate(target);
			};
			flowLayoutPanel2.Controls.Add(button);
		}
		tableLayoutPanel2.Controls.Add(flowLayoutPanel2, 0, 2);
		tableLayoutPanel2.SetColumnSpan(flowLayoutPanel2, 2);
		summaryCpu = NewGraph("PROCESSOR HISTORY", cyan, scale: false);
		summaryRam = NewGraph("MEMORY HISTORY", Color.FromArgb(180, 145, 255), scale: false);
		tableLayoutPanel2.Controls.Add(summaryCpu, 0, 3);
		tableLayoutPanel2.Controls.Add(summaryRam, 1, 3);
		foreach (Snapshot item in history)
		{
			summaryCpu.Add(item.Cpu);
			summaryRam.Add(item.MemoryPercent);
		}
		TableLayoutPanel tableLayoutPanel5 = new TableLayoutPanel();
		tableLayoutPanel5.Dock = DockStyle.Fill;
		tableLayoutPanel5.RowCount = 2;
		tableLayoutPanel5.ColumnCount = 1;
		tableLayoutPanel5.Margin = new Padding(0, 0, 12, 12);
		TableLayoutPanel tableLayoutPanel6 = tableLayoutPanel5;
		tableLayoutPanel6.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
		tableLayoutPanel6.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		tableLayoutPanel6.Controls.Add(new Label
		{
			Text = "BUSIEST RIGHT NOW",
			ForeColor = muted,
			Dock = DockStyle.Fill,
			Font = new Font("Segoe UI", 9f, FontStyle.Bold)
		});
		topProcesses = MakeGrid();
		topProcesses.RowTemplate.Height = 27;
		string[] array4 = new string[4] { "Process", "PID", "CPU %", "RAM MB" };
		foreach (string text in array4)
		{
			topProcesses.Columns.Add(text, text);
		}
		topProcesses.Columns[0].FillWeight = 190f;
		tableLayoutPanel6.Controls.Add(topProcesses, 0, 1);
		tableLayoutPanel2.Controls.Add(tableLayoutPanel6, 0, 4);
		liveHardware = Block("Reading your hardware…", 10);
		tableLayoutPanel2.Controls.Add(liveHardware, 1, 4);
		sensorStrip = Block("Reading sensors…", 10);
		tableLayoutPanel2.Controls.Add(sensorStrip, 0, 5);
		tableLayoutPanel2.SetColumnSpan(sensorStrip, 2);
		liveOverview = Block("Waiting for measurements…", 10);
		tableLayoutPanel2.Controls.Add(liveOverview, 0, 6);
		tableLayoutPanel2.SetColumnSpan(liveOverview, 2);
		ApplyHomeLayout(tableLayoutPanel2);
	}

	private void UpdateTiles()
	{
		if (tileCpu != null && !tileCpu.IsDisposed && current != null)
		{
			tileCpu.Value = current.Cpu.ToString("F1") + "%";
			tileCpu.Detail = Environment.ProcessorCount + " logical processors";
			tileCpu.Level = current.Cpu / 100.0;
			tileRam.Value = current.MemoryPercent.ToString("F1") + "%";
			tileRam.Detail = Inventory.Bytes(current.Available) + " available";
			tileRam.Level = current.MemoryPercent / 100.0;
			tileGpu.Value = (double.IsNaN(gpuValue) ? "—" : (gpuValue.ToString("F1") + "%"));
			tileGpu.Detail = (double.IsNaN(gpuValue) ? "Provider unavailable" : "Windows GPU engine reading");
			tileGpu.Level = (double.IsNaN(gpuValue) ? 0.0 : (gpuValue / 100.0));
			tileNet.Value = Inventory.Bytes(current.Receive) + "/s";
			tileNet.Detail = "↑ " + Inventory.Bytes(current.Send) + "/s sent";
			tileNet.Level = 1.0;
			MetricTile[] array = new MetricTile[4] { tileCpu, tileRam, tileGpu, tileNet };
			foreach (MetricTile metricTile in array)
			{
				metricTile.Invalidate();
			}
		}
	}

	private void TrackInsights(Snapshot s)
	{
		insightTracker.Add(s);
		TrackPressure(s);
		TrackSessionTools(s);
	}

	private string InsightsReport()
	{
		if (current == null)
		{
			return "Waiting for measurements…";
		}
		string text = "SESSION INSIGHTS · " + current.Time.ToString("g") + "\r\n\r\nCPU " + current.Cpu.ToString("F1") + "%  ·  Memory " + current.MemoryPercent.ToString("F1") + "%  ·  " + current.Processes.Count + " processes\r\n";
		if (baseline != null)
		{
			string text2 = text;
			text = text2 + "\r\nCompared with baseline at " + baseline.Time.ToString("T") + ":\r\nCPU " + (current.Cpu - baseline.Cpu).ToString("+0.0;-0.0;0.0") + " percentage points\r\nMemory " + (current.MemoryPercent - baseline.MemoryPercent).ToString("+0.0;-0.0;0.0") + " percentage points\r\nProcesses " + (current.Processes.Count - baseline.Processes.Count).ToString("+0;-0;0") + "\r\n";
		}
		else
		{
			text += "\r\nCapture a baseline before making one change, then compare readings here.\r\n";
		}
		return text + "\r\nShort samples fluctuate. A difference does not prove a change caused it.\r\nLocal rules flag sustained CPU/memory pressure; this is not model-generated advice.\r\n";
	}

	private void BuildInsights()
	{
		sub.Text = "Before / after readings · quiet session alerts · no extra sampler";
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel();
		tableLayoutPanel.Dock = DockStyle.Fill;
		tableLayoutPanel.ColumnCount = 1;
		tableLayoutPanel.RowCount = 4;
		TableLayoutPanel tableLayoutPanel2 = tableLayoutPanel;
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 270f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		content.Controls.Add(tableLayoutPanel2);
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel();
		flowLayoutPanel.Dock = DockStyle.Fill;
		FlowLayoutPanel flowLayoutPanel2 = flowLayoutPanel;
		Button button = Button("Capture baseline");
		button.Click += delegate
		{
			if (current != null)
			{
				baseline = current;
			}
			RefreshInsights();
		};
		flowLayoutPanel2.Controls.Add(button);
		Button button2 = Button("Copy evidence for AI / support");
		button2.Click += delegate
		{
			try
			{
				Clipboard.SetText(InsightsReport() + "\r\n" + string.Join("\r\n", insightTracker.Events));
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, ex.Message);
			}
		};
		flowLayoutPanel2.Controls.Add(button2);
		Button button3 = Button("Prepare AI comparison");
		button3.Click += delegate
		{
			string evidence = InsightsReport();
			Navigate("Windows advisor");
			content.Controls.OfType<AdvisorPanel>().FirstOrDefault()?.PrepareEvidence(evidence);
		};
		flowLayoutPanel2.Controls.Add(button3);
		Button button4 = Button("Clear session alerts");
		button4.Click += delegate
		{
			insightTracker.Events.Clear();
			RefreshInsights();
		};
		flowLayoutPanel2.Controls.Add(button4);
		tableLayoutPanel2.Controls.Add(flowLayoutPanel2, 0, 0);
		insightText = Block("Waiting for readings…", 11);
		tableLayoutPanel2.Controls.Add(insightText, 0, 1);
		tableLayoutPanel2.Controls.Add(new Label
		{
			Text = "ACTIVITY · 5 consecutive high samples · 5-minute cooldown per metric · this session only",
			Dock = DockStyle.Fill,
			ForeColor = muted
		}, 0, 2);
		insightEvents = new ListBox
		{
			Dock = DockStyle.Fill,
			BackColor = panel,
			ForeColor = Color.White,
			HorizontalScrollbar = true,
			BorderStyle = BorderStyle.None
		};
		tableLayoutPanel2.Controls.Add(insightEvents, 0, 3);
		lastInsightText = "";
		RefreshInsights();
	}

	private void RefreshInsights()
	{
		if (insightText != null && !insightText.IsDisposed)
		{
			insightText.Text = InsightsReport();
			string text = string.Join("\n", insightTracker.Events);
			if (text != lastInsightText || insightEvents.Items.Count == 0)
			{
				lastInsightText = text;
				insightEvents.Items.Clear();
				insightEvents.Items.AddRange((insightTracker.Events.Count == 0) ? new string[1] { "No sustained CPU or memory pressure alerts recorded in this session." } : insightTracker.Events.ToArray());
			}
		}
	}

	private void BuildPreferences()
	{
		sub.Text = "Make this workspace yours · preferences saved locally";
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel();
		flowLayoutPanel.Dock = DockStyle.Fill;
		flowLayoutPanel.FlowDirection = FlowDirection.TopDown;
		flowLayoutPanel.WrapContents = false;
		flowLayoutPanel.AutoScroll = true;
		flowLayoutPanel.Padding = new Padding(20);
		FlowLayoutPanel flowLayoutPanel2 = flowLayoutPanel;
		content.Controls.Add(flowLayoutPanel2);
		AddStartupPreferences(flowLayoutPanel2);
		AddPicturePreferences(flowLayoutPanel2);
		AddGreetingPreferences(flowLayoutPanel2);
		flowLayoutPanel2.Controls.Add(new Label
		{
			Text = "A calmer workspace",
			AutoSize = true,
			Font = new Font("Segoe UI", 24f, FontStyle.Bold)
		});
		flowLayoutPanel2.Controls.Add(new Label
		{
			Text = "Choose an accent, adjust monitoring, and pin the pages you use most.",
			AutoSize = true,
			ForeColor = muted,
			Margin = new Padding(0, 12, 0, 25)
		});
		Button button = Button("Open theme gallery →");
		button.Click += delegate
		{
			Navigate("Themes");
		};
		flowLayoutPanel2.Controls.Add(button);
		flowLayoutPanel2.Controls.Add(new Label
		{
			Text = "Sampling interval (longer intervals reduce monitoring work)",
			AutoSize = true,
			Margin = new Padding(0, 20, 0, 8)
		});
		ComboBox interval = new ComboBox
		{
			Width = 280,
			DropDownStyle = ComboBoxStyle.DropDownList
		};
		interval.Items.AddRange(new object[4] { 1000, 2000, 3000, 5000 });
		interval.SelectedItem = preferences.Interval;
		interval.SelectedIndexChanged += delegate
		{
			preferences.Interval = (int)interval.SelectedItem;
			timer.Interval = preferences.Interval;
			SavePreferences();
		};
		flowLayoutPanel2.Controls.Add(interval);
		flowLayoutPanel2.Controls.Add(new Label
		{
			Text = "Values are milliseconds: 1000 = one second. History graphs show 60 samples.",
			AutoSize = true,
			ForeColor = muted
		});
		CheckBox top = new CheckBox
		{
			Text = "Keep Aster above other windows",
			AutoSize = true,
			Checked = preferences.AlwaysOnTop,
			Margin = new Padding(0, 22, 0, 15)
		};
		top.CheckedChanged += delegate
		{
			base.TopMost = (preferences.AlwaysOnTop = top.Checked);
			SavePreferences();
		};
		flowLayoutPanel2.Controls.Add(top);
		CheckBox simpleMode = new CheckBox
		{
			Text = "Simple mode: hide specialist tools in the Tools hub and in search",
			AutoSize = true,
			Checked = preferences.SimpleMode,
			Margin = new Padding(0, 0, 0, 15)
		};
		simpleMode.CheckedChanged += delegate
		{
			preferences.SimpleMode = simpleMode.Checked;
			SavePreferences();
			RebuildNavigation();
		};
		flowLayoutPanel2.Controls.Add(simpleMode);
		flowLayoutPanel2.Controls.Add(new Label
		{
			Text = "Use ☆ Pin page in the header to add or remove sidebar favorites.\r\nSearch the sidebar to filter pages. Ctrl+K jumps to any page.\r\nF5 refreshes the current view. Saved cases and AI tools are in Windows advisor.",
			AutoSize = true,
			ForeColor = muted,
			Margin = new Padding(0, 20, 0, 0)
		});
	}

	private TextBox ReadableText()
	{
		TextBox textBox = new TextBox();
		textBox.Dock = DockStyle.Fill;
		textBox.Multiline = true;
		textBox.ReadOnly = true;
		textBox.ScrollBars = ScrollBars.Vertical;
		textBox.BorderStyle = BorderStyle.None;
		textBox.BackColor = panel;
		textBox.ForeColor = Color.FromArgb(211, 224, 240);
		textBox.Font = new Font("Segoe UI", 11f);
		return textBox;
	}

	private void CleanToolbar()
	{
		if (tools == null)
		{
			return;
		}
		bool flag = new string[7] { "Summary", "Focus", "Insights", "Performance", "Processes", "App history", "Flight recorder" }.Contains(page);
		foreach (Button item in tools.Controls.OfType<Button>())
		{
			if (item == pause)
			{
				item.Visible = flag;
			}
			else if (item.Text == "Run new task")
			{
				item.Visible = page == "Processes";
			}
			else if (item.Text == "Export CSV")
			{
				item.Visible = grid != null || page == "Performance" || page == "Summary" || page == "Flight recorder";
			}
			else if (item.Text == "Refresh")
			{
				item.Visible = flag || IsInventory();
			}
		}
		bool flag2 = flag || IsInventory();
		tools.Visible = flag2 && !distractionFree;
		if (tools.Parent is TableLayoutPanel tableLayoutPanel)
		{
			tableLayoutPanel.RowStyles[1].Height = (tools.Visible ? 47 : 0);
		}
	}

	private void ToggleFocusMode()
	{
		distractionFree = !distractionFree;
		sidebar.Visible = !distractionFree;
		CleanToolbar();
	}

	private void BuildFocus()
	{
		sub.Text = "Essential readings · F11 hides or restores navigation · Ctrl+K jumps to any page";
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel();
		tableLayoutPanel.Dock = DockStyle.Fill;
		tableLayoutPanel.ColumnCount = 2;
		tableLayoutPanel.RowCount = 4;
		tableLayoutPanel.Padding = new Padding(0, 10, 0, 0);
		TableLayoutPanel tableLayoutPanel2 = tableLayoutPanel;
		tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
		tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 150f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 150f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		content.Controls.Add(tableLayoutPanel2);
		Button button = Button("Toggle distraction-free view  ·  F11");
		button.Click += delegate
		{
			ToggleFocusMode();
		};
		tableLayoutPanel2.Controls.Add(button, 0, 0);
		tableLayoutPanel2.SetColumnSpan(button, 2);
		focusCpu = new MetricTile
		{
			Caption = "PROCESSOR",
			Accent = cyan
		};
		focusRam = new MetricTile
		{
			Caption = "MEMORY",
			Accent = Color.FromArgb(178, 145, 255)
		};
		focusProcesses = new MetricTile
		{
			Caption = "PROCESSES",
			Accent = Color.FromArgb(95, 220, 179)
		};
		focusUptime = new MetricTile
		{
			Caption = "WINDOWS UPTIME",
			Accent = Color.FromArgb(239, 184, 111)
		};
		tableLayoutPanel2.Controls.Add(focusCpu, 0, 1);
		tableLayoutPanel2.Controls.Add(focusRam, 1, 1);
		tableLayoutPanel2.Controls.Add(focusProcesses, 0, 2);
		tableLayoutPanel2.Controls.Add(focusUptime, 1, 2);
		focusReadings = Block("Waiting for measurements…", 12);
		tableLayoutPanel2.Controls.Add(focusReadings, 0, 3);
		tableLayoutPanel2.SetColumnSpan(focusReadings, 2);
		UpdateFocus();
	}

	private void UpdateFocus()
	{
		if (current != null && focusCpu != null && !focusCpu.IsDisposed)
		{
			focusCpu.Value = current.Cpu.ToString("F1") + "%";
			focusCpu.Detail = Environment.ProcessorCount + " logical processors";
			focusCpu.Level = current.Cpu / 100.0;
			focusRam.Value = current.MemoryPercent.ToString("F1") + "%";
			focusRam.Detail = Inventory.Bytes(current.Available) + " available";
			focusRam.Level = current.MemoryPercent / 100.0;
			focusProcesses.Value = current.Processes.Count.ToString();
			focusProcesses.Detail = "Visible in the latest sample";
			focusProcesses.Level = 0.0;
			focusUptime.Value = TimeSpan.FromMilliseconds(Native.GetTickCount64()).ToString("d\\d\\ hh\\h\\ mm\\m");
			focusUptime.Detail = "Windows-reported time since boot";
			focusUptime.Level = 0.0;
			focusReadings.Text = (paused ? "PAUSED" : "LIVE") + " · sampled " + current.Time.ToString("T") + "\r\n\r\n" + ((history.Count == 0) ? "" : (history.Count + " recent samples\r\nCPU range " + history.Min((Snapshot x) => x.Cpu).ToString("F1") + "–" + history.Max((Snapshot x) => x.Cpu).ToString("F1") + "%\r\nMemory range " + history.Min((Snapshot x) => x.MemoryPercent).ToString("F1") + "–" + history.Max((Snapshot x) => x.MemoryPercent).ToString("F1") + "%")) + "\r\n\r\nReadings reuse the existing sampler. No additional system monitoring runs for this view.";
			MetricTile[] array = new MetricTile[4] { focusCpu, focusRam, focusProcesses, focusUptime };
			foreach (MetricTile metricTile in array)
			{
				metricTile.Invalidate();
			}
		}
	}

	private void BuildHelpCenter()
	{
		sub.Text = "Practical, searchable guides · available without an AI model";
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel();
		tableLayoutPanel.Dock = DockStyle.Fill;
		tableLayoutPanel.ColumnCount = 2;
		tableLayoutPanel.RowCount = 3;
		TableLayoutPanel tableLayoutPanel2 = tableLayoutPanel;
		tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300f));
		tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
		content.Controls.Add(tableLayoutPanel2);
		TextBox searchHelp = new TextBox
		{
			Dock = DockStyle.Top,
			Font = new Font("Segoe UI", 12f),
			Margin = new Padding(0, 5, 18, 10)
		};
		tableLayoutPanel2.Controls.Add(searchHelp, 0, 0);
		tableLayoutPanel2.Controls.Add(new Label
		{
			Text = "Search a symptom or feature, such as “slow”, “AI”, or “wallpaper”.",
			Dock = DockStyle.Fill,
			ForeColor = muted,
			Padding = new Padding(12, 8, 0, 0)
		}, 1, 0);
		ListBox list = new ListBox
		{
			Dock = DockStyle.Fill,
			BorderStyle = BorderStyle.None,
			BackColor = panel,
			ForeColor = Color.White,
			Font = new Font("Segoe UI", 11f),
			ItemHeight = 34,
			Margin = new Padding(0, 0, 18, 10)
		};
		list.DrawMode = DrawMode.OwnerDrawFixed;
		list.DrawItem += delegate(object s, DrawItemEventArgs e)
		{
			if (e.Index >= 0)
			{
				e.DrawBackground();
				TextRenderer.DrawText(e.Graphics, list.Items[e.Index].ToString(), list.Font, e.Bounds, Color.White, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter);
			}
		};
		tableLayoutPanel2.Controls.Add(list, 0, 1);
		TextBox detail = ReadableText();
		detail.Margin = new Padding(18, 8, 18, 12);
		tableLayoutPanel2.Controls.Add(detail, 1, 1);
		Button open = Button("Open related tool →");
		tableLayoutPanel2.Controls.Add(open, 1, 2);
		Action refreshHelp = delegate
		{
			list.Items.Clear();
			list.Items.AddRange(HelpTopic.Search(searchHelp.Text));
			open.Enabled = list.Items.Count > 0;
			if (list.Items.Count > 0)
			{
				list.SelectedIndex = 0;
			}
			else
			{
				detail.Text = "No matching guide. Try a shorter search or open Windows advisor to describe your issue.";
			}
		};
		list.SelectedIndexChanged += delegate
		{
			if (list.SelectedItem is HelpTopic helpTopic)
			{
				detail.Text = helpTopic.Title.ToUpperInvariant() + "\r\n\r\n" + helpTopic.Body;
			}
		};
		open.Click += delegate
		{
			if (list.SelectedItem is HelpTopic helpTopic)
			{
				Navigate(helpTopic.Page);
			}
		};
		searchHelp.TextChanged += delegate
		{
			refreshHelp();
		};
		refreshHelp();
	}

	private void BuildHardwareReport()
	{
		sub.Text = "CPU, graphics, memory, storage and motherboard · review before sharing";
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel();
		tableLayoutPanel.Dock = DockStyle.Fill;
		tableLayoutPanel.ColumnCount = 1;
		tableLayoutPanel.RowCount = 3;
		TableLayoutPanel tableLayoutPanel2 = tableLayoutPanel;
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 56f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		content.Controls.Add(tableLayoutPanel2);
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel();
		flowLayoutPanel.Dock = DockStyle.Fill;
		FlowLayoutPanel flowLayoutPanel2 = flowLayoutPanel;
		tableLayoutPanel2.Controls.Add(flowLayoutPanel2, 0, 0);
		Button generate = Button("Generate hardware report");
		Button copy = Button("Copy report");
		Button save = Button("Save as text");
		Button button = copy;
		bool enabled = (save.Enabled = false);
		button.Enabled = enabled;
		flowLayoutPanel2.Controls.Add(generate);
		flowLayoutPanel2.Controls.Add(copy);
		flowLayoutPanel2.Controls.Add(save);
		Label state = new Label
		{
			Dock = DockStyle.Fill,
			ForeColor = muted,
			Text = "Only generated when requested. Nothing is uploaded."
		};
		tableLayoutPanel2.Controls.Add(state, 0, 1);
		TextBox text = ReadableText();
		text.Text = "A useful inventory for support or your own records.\r\n\r\nThe report excludes serial numbers and device/processor identifiers. Provider failures are included as unavailable readings. Review the report before sharing it.\r\n\r\nGenerating may take time while Windows reads hardware information.";
		tableLayoutPanel2.Controls.Add(text, 0, 2);
		generate.Click += async delegate
		{
			generate.Enabled = false;
			state.Text = "Reading Windows hardware providers…";
			try
			{
				int num = default(int);
				_ = num;
				_ = 0;
				try
				{
					string data = await Task.Run(() => HardwareReport.Collect());
					if (!text.IsDisposed)
					{
						text.Text = data;
						Button button2 = copy;
						bool enabled2 = (save.Enabled = true);
						button2.Enabled = enabled2;
						state.Text = "Report ready · review before copying or saving.";
					}
				}
				catch (Exception ex)
				{
					if (!state.IsDisposed)
					{
						state.Text = "Could not generate report: " + ex.Message;
					}
				}
			}
			finally
			{
				if (!generate.IsDisposed)
				{
					generate.Enabled = true;
				}
			}
		};
		copy.Click += delegate
		{
			try
			{
				Clipboard.SetText(text.Text);
				state.Text = "Copied. Nothing has been sent to another person.";
			}
			catch (Exception ex)
			{
				state.Text = ex.Message;
			}
		};
		save.Click += delegate
		{
			using SaveFileDialog saveFileDialog = new SaveFileDialog
			{
				Filter = "Text report|*.txt",
				FileName = "Aster-hardware-" + DateTime.Now.ToString("yyyyMMdd") + ".txt"
			};
			if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
			{
				try
				{
					File.WriteAllText(saveFileDialog.FileName, text.Text, Encoding.UTF8);
					state.Text = "Saved report.";
					return;
				}
				catch (Exception ex)
				{
					state.Text = ex.Message;
					return;
				}
			}
		};
	}

	private void SetupFun()
	{
		funTime.Start();
		funClock = new System.Windows.Forms.Timer
		{
			Interval = 1000
		};
		funClock.Tick += delegate
		{
			TickFun();
		};
		funClock.Start();
		base.Resize += delegate
		{
			ConfigureFun();
		};
		base.FormClosed += delegate
		{
			funClock.Dispose();
			funTime.Stop();
		};
	}

	private void ConfigureFun()
	{
		if (funClock != null)
		{
			funClock.Interval = (((page == "Playground" || page == "Focus timer") && preferences.Motion && base.WindowState != FormWindowState.Minimized) ? 50 : 1000);
		}
	}

	private void TickFun()
	{
		if (countdown.Update(funTime.Elapsed))
		{
			timerDone = true;
			if (funCanvas != null && !funCanvas.IsDisposed && page == "Focus timer")
			{
				funCanvas.Celebrate();
			}
		}
		if (base.WindowState != FormWindowState.Minimized && funCanvas != null && !funCanvas.IsDisposed && (page == "Playground" || page == "Focus timer"))
		{
			funCanvas.Motion = preferences.Motion;
			funCanvas.ThemeName = preferences.PlayTheme;
			if (current != null)
			{
				funCanvas.Cpu = current.Cpu;
				funCanvas.Ram = current.MemoryPercent;
				funCanvas.HasData = true;
			}
			if (page == "Focus timer")
			{
				double num = Math.Ceiling(countdown.Remaining.TotalSeconds);
				funCanvas.CenterTitle = ((int)num / 60).ToString("00") + ":" + ((int)num % 60).ToString("00");
				funCanvas.CenterValue = (timerDone ? "NICE WORK. TAKE A BREATH." : (countdown.Running ? "ONE THING AT A TIME" : "YOUR MOMENT TO FOCUS"));
				funCanvas.Progress = 1.0 - countdown.Remaining.TotalSeconds / countdown.Duration.TotalSeconds;
				timerState.Text = (timerDone ? "Session complete! " : (countdown.Running ? "Running · " : "Ready / paused · ")) + countdown.Completed + " completed this session. Timer continues while you browse Aster.";
				timerStart.Text = (countdown.Running ? "Pause" : "Start / resume");
				timerMinutes.Enabled = !countdown.Running;
			}
			funCanvas.Advance();
		}
	}

	private FlowLayoutPanel PlayControls()
	{
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel();
		flowLayoutPanel.Dock = DockStyle.Fill;
		flowLayoutPanel.WrapContents = true;
		FlowLayoutPanel flowLayoutPanel2 = flowLayoutPanel;
		Label label = new Label();
		label.Text = "PALETTE";
		label.AutoSize = true;
		label.ForeColor = muted;
		label.Margin = new Padding(0, 10, 10, 0);
		Label value = label;
		flowLayoutPanel2.Controls.Add(value);
		ComboBox theme = new ComboBox
		{
			DropDownStyle = ComboBoxStyle.DropDownList,
			Width = 145
		};
		theme.Items.AddRange(new string[3] { "Nebula", "Tide", "Sunset" });
		theme.SelectedItem = (theme.Items.Contains(preferences.PlayTheme) ? preferences.PlayTheme : "Nebula");
		theme.SelectedIndexChanged += delegate
		{
			preferences.PlayTheme = (string)theme.SelectedItem;
			preferences.Accent = ((preferences.PlayTheme == "Tide") ? "Mint" : ((preferences.PlayTheme == "Nebula") ? "Violet" : "Sunset"));
			SavePreferences();
			StyleView();
			TickFun();
		};
		flowLayoutPanel2.Controls.Add(theme);
		CheckBox motion = new CheckBox
		{
			Text = "Animate scenes",
			AutoSize = true,
			Checked = preferences.Motion,
			Margin = new Padding(16, 8, 12, 0)
		};
		motion.CheckedChanged += delegate
		{
			preferences.Motion = motion.Checked;
			SavePreferences();
			ConfigureFun();
			TickFun();
		};
		flowLayoutPanel2.Controls.Add(motion);
		return flowLayoutPanel2;
	}

	private void BuildPlayground()
	{
		sub.Text = "A playful window into your system · powered by your existing readings";
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel();
		tableLayoutPanel.Dock = DockStyle.Fill;
		tableLayoutPanel.ColumnCount = 1;
		tableLayoutPanel.RowCount = 2;
		TableLayoutPanel tableLayoutPanel2 = tableLayoutPanel;
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 82f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		content.Controls.Add(tableLayoutPanel2);
		FlowLayoutPanel flowLayoutPanel = PlayControls();
		tableLayoutPanel2.Controls.Add(flowLayoutPanel, 0, 0);
		ComboBox scenes = new ComboBox
		{
			DropDownStyle = ComboBoxStyle.DropDownList,
			Width = 150
		};
		scenes.Items.AddRange(new string[3] { "Orbit", "Aurora", "Pixel garden" });
		scenes.SelectedIndex = 0;
		flowLayoutPanel.Controls.Add(scenes);
		Button button = Button("Start a focus session →");
		button.Click += delegate
		{
			Navigate("Focus timer");
		};
		flowLayoutPanel.Controls.Add(button);
		funCanvas = new OrbitCanvas
		{
			ThemeName = preferences.PlayTheme,
			Motion = preferences.Motion
		};
		tableLayoutPanel2.Controls.Add(funCanvas, 0, 1);
		scenes.SelectedIndexChanged += delegate
		{
			funCanvas.Scene = (string)scenes.SelectedItem;
			funCanvas.Invalidate();
		};
		TickFun();
	}

	private void BuildFocusTimer()
	{
		sub.Text = "A gentle countdown · no blocked apps, automatic settings changes, or sounds";
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel();
		tableLayoutPanel.Dock = DockStyle.Fill;
		tableLayoutPanel.ColumnCount = 1;
		tableLayoutPanel.RowCount = 4;
		TableLayoutPanel tableLayoutPanel2 = tableLayoutPanel;
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 58f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 58f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 55f));
		content.Controls.Add(tableLayoutPanel2);
		tableLayoutPanel2.Controls.Add(PlayControls(), 0, 0);
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel();
		flowLayoutPanel.Dock = DockStyle.Fill;
		FlowLayoutPanel flowLayoutPanel2 = flowLayoutPanel;
		tableLayoutPanel2.Controls.Add(flowLayoutPanel2, 0, 1);
		flowLayoutPanel2.Controls.Add(new Label
		{
			Text = "Minutes",
			AutoSize = true,
			Margin = new Padding(0, 8, 8, 0)
		});
		timerMinutes = new NumericUpDown
		{
			Minimum = 1m,
			Maximum = 180m,
			Value = (decimal)countdown.Duration.TotalMinutes,
			Width = 72
		};
		flowLayoutPanel2.Controls.Add(timerMinutes);
		timerStart = Button("Start / resume");
		timerStart.Click += delegate
		{
			if (countdown.Running)
			{
				if (countdown.Pause(funTime.Elapsed))
				{
					timerDone = true;
				}
			}
			else
			{
				if (countdown.Remaining == TimeSpan.Zero || countdown.Duration.TotalMinutes != (double)timerMinutes.Value)
				{
					countdown.Reset((int)timerMinutes.Value);
				}
				timerDone = false;
				countdown.Start(funTime.Elapsed);
			}
			TickFun();
		};
		flowLayoutPanel2.Controls.Add(timerStart);
		Button button = Button("Reset");
		button.Click += delegate
		{
			countdown.Reset((int)timerMinutes.Value);
			timerDone = false;
			TickFun();
		};
		flowLayoutPanel2.Controls.Add(button);
		int[] array = new int[3] { 5, 25, 50 };
		foreach (int num2 in array)
		{
			int duration = num2;
			Button button2 = Button((num2 == 5) ? "5m break" : (num2 + "m focus"));
			button2.Click += delegate
			{
				countdown.Reset(duration);
				timerMinutes.Value = duration;
				timerDone = false;
				TickFun();
			};
			flowLayoutPanel2.Controls.Add(button2);
		}
		funCanvas = new OrbitCanvas
		{
			TimerMode = true,
			ThemeName = preferences.PlayTheme,
			Motion = preferences.Motion
		};
		tableLayoutPanel2.Controls.Add(funCanvas, 0, 2);
		timerState = new Label
		{
			Dock = DockStyle.Fill,
			ForeColor = muted,
			Padding = new Padding(0, 12, 0, 0)
		};
		tableLayoutPanel2.Controls.Add(timerState, 0, 3);
		TickFun();
	}

	private void SetupGreeting()
	{
		greetingTimer = new System.Windows.Forms.Timer
		{
			Interval = 1000
		};
		greetingTimer.Tick += delegate
		{
			RefreshDailyPicture();
			RefreshGreeting();
		};
		greetingTimer.Start();
		RefreshDailyPicture();
		base.FormClosed += delegate
		{
			greetingTimer.Dispose();
		};
	}

	private void RefreshGreeting()
	{
		if (greetingTitle != null && !greetingTitle.IsDisposed)
		{
			GreetingData greetingData = GreetingClock.Format(DateTimeOffset.UtcNow, preferences, Environment.UserName);
			greetingTitle.Text = greetingData.Title;
			greetingDate.Text = greetingData.DateLine;
			greetingHijri.Text = greetingData.Hijri;
			greetingOccasion.Text = greetingData.Occasion;
		}
	}

	private Control BuildGreetingHero()
	{
		AuroraHeader auroraHeader = new AuroraHeader();
		auroraHeader.ThemeName = preferences.PlayTheme;
		auroraHeader.Dock = DockStyle.Fill;
		auroraHeader.Margin = new Padding(0, 0, 12, 14);
		auroraHeader.Padding = new Padding(20, 8, 14, 8);
		AuroraHeader auroraHeader2 = auroraHeader;
		PictureBox pictureBox = new PictureBox();
		pictureBox.Dock = DockStyle.Right;
		pictureBox.Width = 76;
		pictureBox.Image = DailyArt.Render(66, CurrentPictureNumber());
		pictureBox.SizeMode = PictureBoxSizeMode.CenterImage;
		pictureBox.BackColor = Color.Transparent;
		PictureBox value = (bannerPicture = pictureBox);
		auroraHeader2.Controls.Add(value);
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel();
		tableLayoutPanel.Dock = DockStyle.Fill;
		tableLayoutPanel.ColumnCount = 1;
		tableLayoutPanel.RowCount = 4;
		tableLayoutPanel.BackColor = Color.Transparent;
		TableLayoutPanel tableLayoutPanel2 = tableLayoutPanel;
		int[] array = new int[4] { 42, 27, 24, 25 };
		foreach (int num in array)
		{
			tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, num));
		}
		auroraHeader2.Controls.Add(tableLayoutPanel2);
		tableLayoutPanel2.BringToFront();
		greetingTitle = GreetingLabel(20, Color.White);
		greetingTitle.Font = new Font("Segoe UI", 20f, FontStyle.Bold);
		greetingDate = GreetingLabel(9, Color.FromArgb(203, 217, 237));
		greetingHijri = GreetingLabel(10, Color.FromArgb(206, 187, 255));
		greetingOccasion = GreetingLabel(10, Color.FromArgb(123, 230, 208));
		tableLayoutPanel2.Controls.Add(greetingTitle, 0, 0);
		tableLayoutPanel2.Controls.Add(greetingDate, 0, 1);
		tableLayoutPanel2.Controls.Add(greetingHijri, 0, 2);
		tableLayoutPanel2.Controls.Add(greetingOccasion, 0, 3);
		RefreshGreeting();
		return auroraHeader2;
	}

	private Label GreetingLabel(int size, Color color)
	{
		Label label = new Label();
		label.Dock = DockStyle.Fill;
		label.BackColor = Color.Transparent;
		label.ForeColor = color;
		label.Font = new Font("Segoe UI", size);
		label.AutoEllipsis = true;
		label.UseMnemonic = false;
		label.TextAlign = ContentAlignment.MiddleLeft;
		label.Margin = new Padding(0);
		return label;
	}

	private void AddGreetingPreferences(FlowLayoutPanel flow)
	{
		flow.Controls.Add(new Label
		{
			Text = "Your daily welcome",
			AutoSize = true,
			Font = new Font("Segoe UI", 22f, FontStyle.Bold)
		});
		flow.Controls.Add(new Label
		{
			Text = "Choose your name and clock for the Summary greeting.",
			AutoSize = true,
			ForeColor = muted,
			Margin = new Padding(0, 8, 0, 12)
		});
		TextBox name = new TextBox
		{
			Width = 310,
			MaxLength = 40,
			Text = (preferences.DisplayName ?? "")
		};
		flow.Controls.Add(new Label
		{
			Text = "Display name (leave blank to use your Windows username)",
			AutoSize = true
		});
		flow.Controls.Add(name);
		CheckBox riyadh = new CheckBox
		{
			Text = "Use Riyadh time (UTC+03:00); otherwise use Windows local time",
			AutoSize = true,
			Checked = preferences.RiyadhTime
		};
		flow.Controls.Add(riyadh);
		CheckBox clock24 = new CheckBox
		{
			Text = "24-hour clock",
			AutoSize = true,
			Checked = preferences.Clock24
		};
		flow.Controls.Add(clock24);
		Button save = Button("Save greeting preferences");
		save.Click += delegate
		{
			preferences.DisplayName = name.Text.Trim();
			preferences.RiyadhTime = riyadh.Checked;
			preferences.Clock24 = clock24.Checked;
			SavePreferences();
			RefreshGreeting();
			save.Text = "Saved · see Summary";
		};
		flow.Controls.Add(save);
		flow.Controls.Add(new Label
		{
			Text = "",
			Height = 20
		});
	}

	private void BuildThemes()
	{
		BuildThemeGallery();
	}

	private void ApplyAppTheme()
	{
		ThemePalette themePalette = (ThemePalette.Current = ResolveTheme());
		bg = themePalette.Background;
		panel = themePalette.Surface;
		muted = themePalette.Muted;
		cyan = themePalette.Accent;
		BackColor = bg;
		ForeColor = themePalette.Text;
		ApplyThemeTree(this, themePalette);
		sidebar.BackColor = themePalette.Sidebar;
		foreach (Control control in sidebar.Controls)
		{
			SetSidebar(control, themePalette);
		}
		foreach (KeyValuePair<string, Button> button in buttons)
		{
			button.Value.BackColor = ((button.Key == page) ? themePalette.Hero : themePalette.Sidebar);
			button.Value.ForeColor = ((button.Key == page) ? themePalette.Accent : themePalette.Text);
			button.Value.Invalidate();
		}
		heading.ForeColor = themePalette.Text;
		Label label = sub;
		Color foreColor = (status.ForeColor = themePalette.Muted);
		label.ForeColor = foreColor;
		if (greetingTitle != null && !greetingTitle.IsDisposed)
		{
			greetingTitle.ForeColor = themePalette.Text;
			greetingDate.ForeColor = themePalette.Muted;
			Label label2 = greetingHijri;
			Color foreColor2 = (greetingOccasion.ForeColor = themePalette.Accent);
			label2.ForeColor = foreColor2;
		}
		if (summaryCpu != null && !summaryCpu.IsDisposed)
		{
			summaryCpu.Accent = themePalette.Accent;
		}
		if (tileCpu != null && !tileCpu.IsDisposed)
		{
			tileCpu.Accent = themePalette.Accent;
		}
		if (cpu != null && !cpu.IsDisposed)
		{
			cpu.Accent = themePalette.Accent;
		}
		Invalidate(invalidateChildren: true);
	}

	private void SetSidebar(Control c, ThemePalette t)
	{
		if (!(c is TextBox) && !(c is Button))
		{
			c.BackColor = t.Sidebar;
		}
		if (c is Label)
		{
			c.ForeColor = t.Muted;
		}
		foreach (Control control in c.Controls)
		{
			SetSidebar(control, t);
		}
	}

	private void ApplyThemeTree(Control parent, ThemePalette t)
	{
		foreach (Control control in parent.Controls)
		{
			if (!(control is ThemeCard) && !(control is OrbitCanvas))
			{
				bool flag = control.BackColor == Color.Transparent;
				bool flag2 = control is Label && control.Dock == DockStyle.Fill && control.Padding.All > 0;
				bool flag3 = control is TextBoxBase || control is ComboBox || control is ListBox || control is NumericUpDown || control is Graph || flag2;
				if (!flag)
				{
					control.BackColor = (flag3 ? t.Surface : t.Background);
				}
				control.ForeColor = t.Text;
				if (control is Button button)
				{
					button.BackColor = t.Raised;
					button.ForeColor = ((button.Name == "End") ? Color.FromArgb(255, 161, 161) : t.Text);
				}
				if (control is DataGridView dataGridView)
				{
					dataGridView.EnableHeadersVisualStyles = false;
					dataGridView.BackgroundColor = t.Surface;
					dataGridView.GridColor = t.Border;
					dataGridView.DefaultCellStyle.BackColor = t.Surface;
					dataGridView.RowsDefaultCellStyle.BackColor = t.Surface;
					dataGridView.DefaultCellStyle.ForeColor = t.Text;
					dataGridView.DefaultCellStyle.SelectionBackColor = t.Hero;
					dataGridView.DefaultCellStyle.SelectionForeColor = t.Text;
					dataGridView.AlternatingRowsDefaultCellStyle.BackColor = t.Raised;
					dataGridView.ColumnHeadersDefaultCellStyle.BackColor = t.Raised;
					dataGridView.ColumnHeadersDefaultCellStyle.ForeColor = t.Muted;
				}
				if (control is TabControl tabControl)
				{
					tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
					tabControl.Padding = new Point(16, 9);
					tabControl.DrawItem -= PaintThemeTab;
					tabControl.DrawItem += PaintThemeTab;
				}
				ApplyThemeTree(control, t);
				control.Invalidate();
			}
		}
	}

	private void PaintThemeTab(object sender, DrawItemEventArgs e)
	{
		TabControl tabControl = (TabControl)sender;
		ThemePalette themePalette = ThemePalette.Current;
		bool flag = e.Index == tabControl.SelectedIndex;
		using (SolidBrush brush = new SolidBrush(flag ? themePalette.Hero : themePalette.Surface))
		{
			e.Graphics.FillRectangle(brush, e.Bounds);
		}
		TextRenderer.DrawText(e.Graphics, tabControl.TabPages[e.Index].Text, Font, e.Bounds, flag ? themePalette.Accent : themePalette.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter);
	}

	private int CurrentPictureNumber()
	{
		return DailyArt.Index(DailyArt.Day(DateTimeOffset.UtcNow, preferences.RiyadhTime), preferences.DailyPicture, preferences.PictureChoice, preferences.PictureShift);
	}

	private static void ReplacePicture(PictureBox box, Image image)
	{
		if (box == null || box.IsDisposed)
		{
			image.Dispose();
			return;
		}
		Image image2 = box.Image;
		box.Image = image;
		image2?.Dispose();
	}

	private void RefreshDailyPicture()
	{
		int num = CurrentPictureNumber();
		if (num != shownPicture)
		{
			shownPicture = num;
			if (dailyLogo != null && !dailyLogo.IsDisposed)
			{
				Image image = dailyLogo.Image;
				dailyLogo.Image = DailyArt.Render(44, num);
				image?.Dispose();
			}
			if (bannerPicture != null && !bannerPicture.IsDisposed)
			{
				ReplacePicture(bannerPicture, DailyArt.Render(66, num));
			}
			Icon icon = (base.Icon = DailyArt.MakeIcon(num));
			Icon icon3 = dynamicPictureIcon;
			dynamicPictureIcon = icon;
			icon3?.Dispose();
		}
		if (picturePreview != null && !picturePreview.IsDisposed && !object.Equals(picturePreview.Tag, num))
		{
			picturePreview.Tag = num;
			ReplacePicture(picturePreview, DailyArt.Render(88, num));
		}
		if (pictureDescription != null && !pictureDescription.IsDisposed)
		{
			pictureDescription.Text = DailyArt.Names[num] + (preferences.DailyPicture ? " · changes at midnight in your greeting time zone" : " · fixed picture");
		}
	}

	private void SelectPicture(int number)
	{
		DateTime date = DailyArt.Day(DateTimeOffset.UtcNow, preferences.RiyadhTime);
		preferences.PictureChoice = DailyArt.Wrap(number);
		preferences.PictureShift = DailyArt.ShiftFor(date, number);
		SavePreferences();
		RefreshDailyPicture();
	}

	private void AddPicturePreferences(FlowLayoutPanel flow)
	{
		flow.Controls.Add(new Label
		{
			Text = "A new look each day",
			AutoSize = true,
			Font = new Font("Segoe UI", 22f, FontStyle.Bold)
		});
		flow.Controls.Add(new Label
		{
			Text = "Seven built-in pictures for the sidebar, welcome banner and running window icon.",
			AutoSize = true,
			ForeColor = muted,
			Margin = new Padding(0, 8, 0, 12)
		});
		picturePreview = new PictureBox
		{
			Size = new Size(100, 100),
			SizeMode = PictureBoxSizeMode.CenterImage
		};
		picturePreview.Disposed += delegate(object s, EventArgs e)
		{
			PictureBox pictureBox = (PictureBox)s;
			if (pictureBox.Image != null)
			{
				pictureBox.Image.Dispose();
			}
		};
		flow.Controls.Add(picturePreview);
		pictureDescription = new Label
		{
			AutoSize = true
		};
		flow.Controls.Add(pictureDescription);
		dailyPictureCheck = new CheckBox
		{
			Text = "Change my app picture every day",
			AutoSize = true,
			Checked = preferences.DailyPicture,
			Margin = new Padding(0, 12, 0, 10)
		};
		dailyPictureCheck.CheckedChanged += delegate
		{
			if (!changingPictureControls)
			{
				int number = CurrentPictureNumber();
				preferences.DailyPicture = dailyPictureCheck.Checked;
				SelectPicture(number);
			}
		};
		flow.Controls.Add(dailyPictureCheck);
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel();
		flowLayoutPanel.Width = 680;
		flowLayoutPanel.Height = 90;
		flowLayoutPanel.WrapContents = true;
		FlowLayoutPanel flowLayoutPanel2 = flowLayoutPanel;
		for (int num = 0; num < DailyArt.Names.Length; num++)
		{
			int choice = num;
			Button button = Button(DailyArt.Names[num]);
			button.Click += delegate
			{
				SelectPicture(choice);
			};
			flowLayoutPanel2.Controls.Add(button);
		}
		flow.Controls.Add(flowLayoutPanel2);
		FlowLayoutPanel flowLayoutPanel3 = new FlowLayoutPanel();
		flowLayoutPanel3.Width = 680;
		flowLayoutPanel3.Height = 48;
		FlowLayoutPanel flowLayoutPanel4 = flowLayoutPanel3;
		Button button2 = Button("Change now");
		button2.Click += delegate
		{
			SelectPicture(CurrentPictureNumber() + 1);
		};
		flowLayoutPanel4.Controls.Add(button2);
		Button button3 = Button("Keep this picture");
		button3.Click += delegate
		{
			int number = CurrentPictureNumber();
			preferences.DailyPicture = false;
			changingPictureControls = true;
			dailyPictureCheck.Checked = false;
			changingPictureControls = false;
			SelectPicture(number);
		};
		flowLayoutPanel4.Controls.Add(button3);
		flow.Controls.Add(flowLayoutPanel4);
		flow.Controls.Add(new Label
		{
			Text = "Rotation works offline. If Aster is closed at midnight, it catches up when reopened.\r\nThe executable and desktop-shortcut icons keep their installed design; Windows may\r\nalso keep a cached taskbar icon. No wallpaper or account picture is changed.",
			AutoSize = true,
			ForeColor = muted,
			Margin = new Padding(0, 8, 0, 24)
		});
		RefreshDailyPicture();
	}

	private ThemePalette ResolveTheme()
	{
		if (preferences.ThemeId == "custom")
		{
			try
			{
				ThemeRecipe themeRecipe = new ThemeRecipe();
				themeRecipe.Background = preferences.CustomBase;
				themeRecipe.Accent = preferences.CustomAccent;
				return CustomTheme.Create(themeRecipe);
			}
			catch
			{
				return ThemePalette.Find("midnight");
			}
		}
		return ThemePalette.Find(preferences.ThemeId);
	}

	private void BuildThemeGallery()
	{
		sub.Text = ThemePalette.All.Length + " ready-made themes · search, apply, or create your own";
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel();
		tableLayoutPanel.Dock = DockStyle.Fill;
		tableLayoutPanel.ColumnCount = 1;
		tableLayoutPanel.RowCount = 2;
		TableLayoutPanel tableLayoutPanel2 = tableLayoutPanel;
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 55f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		content.Controls.Add(tableLayoutPanel2);
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel();
		flowLayoutPanel.Dock = DockStyle.Fill;
		FlowLayoutPanel flowLayoutPanel2 = flowLayoutPanel;
		TextBox filter = new TextBox
		{
			Width = 260,
			Margin = new Padding(0, 6, 12, 0)
		};
		flowLayoutPanel2.Controls.Add(new Label
		{
			Text = "Find a theme",
			AutoSize = true,
			Margin = new Padding(0, 9, 10, 0)
		});
		flowLayoutPanel2.Controls.Add(filter);
		Button button = Button("Open Theme studio →");
		button.Click += delegate
		{
			Navigate("Theme studio");
		};
		flowLayoutPanel2.Controls.Add(button);
		tableLayoutPanel2.Controls.Add(flowLayoutPanel2, 0, 0);
		Panel panel = new Panel();
		panel.Dock = DockStyle.Fill;
		panel.AutoScroll = true;
		Panel panel2 = panel;
		tableLayoutPanel2.Controls.Add(panel2, 0, 1);
		FlowLayoutPanel flow = new FlowLayoutPanel
		{
			Dock = DockStyle.Top,
			AutoSize = true,
			WrapContents = true
		};
		panel2.Controls.Add(flow);
		ThemePalette[] all = ThemePalette.All;
		foreach (ThemePalette themePalette in all)
		{
			ThemePalette p = themePalette;
			ThemeCard themeCard = new ThemeCard();
			themeCard.Palette = p;
			themeCard.Selected = p.Id == preferences.ThemeId;
			themeCard.AccessibleName = "Apply " + p.Name;
			themeCard.Text = p.Name;
			ThemeCard themeCard2 = themeCard;
			themeCard2.Click += delegate
			{
				string themeId = preferences.ThemeId;
				preferences.ThemeId = p.Id;
				if (!SavePreferences())
				{
					preferences.ThemeId = themeId;
					return;
				}
				ApplyAppTheme();
				foreach (ThemeCard item in flow.Controls.OfType<ThemeCard>())
				{
					item.Selected = item.Palette.Id == preferences.ThemeId;
					item.Invalidate();
				}
			};
			flow.Controls.Add(themeCard2);
		}
		Label noMatch = new Label
		{
			Text = "No matching theme. Try a shorter name.",
			AutoSize = true,
			Visible = false
		};
		flow.Controls.Add(noMatch);
		filter.TextChanged += delegate
		{
			int num2 = 0;
			foreach (ThemeCard item2 in flow.Controls.OfType<ThemeCard>())
			{
				item2.Visible = item2.Palette.Name.IndexOf(filter.Text.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
				if (item2.Visible)
				{
					num2++;
				}
			}
			noMatch.Visible = num2 == 0;
		};
	}

	private void BuildThemeStudio()
	{
		sub.Text = "Create a coordinated dark theme · preview first · import or export a color recipe";
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel();
		flowLayoutPanel.Dock = DockStyle.Fill;
		flowLayoutPanel.AutoScroll = true;
		flowLayoutPanel.WrapContents = false;
		flowLayoutPanel.FlowDirection = FlowDirection.TopDown;
		flowLayoutPanel.Padding = new Padding(12);
		FlowLayoutPanel flowLayoutPanel2 = flowLayoutPanel;
		content.Controls.Add(flowLayoutPanel2);
		flowLayoutPanel2.Controls.Add(new Label
		{
			Text = "Make it yours.",
			AutoSize = true,
			Font = new Font("Segoe UI", 25f, FontStyle.Bold)
		});
		TextBox bgBox = new TextBox
		{
			Text = preferences.CustomBase,
			Width = 150
		};
		TextBox accentBox = new TextBox
		{
			Text = preferences.CustomAccent,
			Width = 150
		};
		Tuple<string, TextBox>[] array = new Tuple<string, TextBox>[2]
		{
			Tuple.Create("Background", bgBox),
			Tuple.Create("Accent", accentBox)
		};
		foreach (Tuple<string, TextBox> tuple in array)
		{
			FlowLayoutPanel flowLayoutPanel3 = new FlowLayoutPanel();
			flowLayoutPanel3.Width = 580;
			flowLayoutPanel3.Height = 44;
			FlowLayoutPanel flowLayoutPanel4 = flowLayoutPanel3;
			flowLayoutPanel4.Controls.Add(new Label
			{
				Text = tuple.Item1,
				Width = 120,
				Margin = new Padding(0, 8, 0, 0)
			});
			flowLayoutPanel4.Controls.Add(tuple.Item2);
			TextBox edit = tuple.Item2;
			Button button = Button("Pick color");
			button.Click += delegate
			{
				using ColorDialog colorDialog = new ColorDialog
				{
					FullOpen = true
				};
				try
				{
					colorDialog.Color = CustomTheme.Parse(edit.Text);
				}
				catch
				{
				}
				if (colorDialog.ShowDialog(this) == DialogResult.OK)
				{
					edit.Text = "#" + colorDialog.Color.R.ToString("X2") + colorDialog.Color.G.ToString("X2") + colorDialog.Color.B.ToString("X2");
				}
			};
			flowLayoutPanel4.Controls.Add(button);
			flowLayoutPanel2.Controls.Add(flowLayoutPanel4);
		}
		ThemeCard preview = new ThemeCard
		{
			Palette = ResolveTheme(),
			Enabled = false
		};
		flowLayoutPanel2.Controls.Add(preview);
		Label result = new Label
		{
			AutoSize = true,
			MaximumSize = new Size(720, 0),
			Text = "Enter a dark background and bright accent. Preview checks text/highlight contrast."
		};
		flowLayoutPanel2.Controls.Add(result);
		Func<ThemeRecipe> recipe = () => new ThemeRecipe
		{
			Background = bgBox.Text.Trim(),
			Accent = accentBox.Text.Trim()
		};
		Action<Action> guard = delegate(Action action)
		{
			try
			{
				action();
			}
			catch (Exception ex)
			{
				result.Text = ex.Message;
			}
		};
		FlowLayoutPanel flowLayoutPanel5 = new FlowLayoutPanel();
		flowLayoutPanel5.Width = 760;
		flowLayoutPanel5.Height = 90;
		FlowLayoutPanel flowLayoutPanel6 = flowLayoutPanel5;
		flowLayoutPanel2.Controls.Add(flowLayoutPanel6);
		Button button2 = Button("Preview");
		button2.Click += delegate
		{
			guard(delegate
			{
				preview.Palette = CustomTheme.Create(recipe());
				preview.Invalidate();
				result.Text = "Preview ready. Your current theme has not changed.";
			});
		};
		flowLayoutPanel6.Controls.Add(button2);
		Button button3 = Button("Apply & save");
		button3.Click += delegate
		{
			guard(delegate
			{
				ThemeRecipe themeRecipe = recipe();
				preview.Palette = CustomTheme.Create(themeRecipe);
				string customBase = preferences.CustomBase;
				string customAccent = preferences.CustomAccent;
				string themeId = preferences.ThemeId;
				preferences.CustomBase = themeRecipe.Background;
				preferences.CustomAccent = themeRecipe.Accent;
				preferences.ThemeId = "custom";
				if (!SavePreferences())
				{
					preferences.CustomBase = customBase;
					preferences.CustomAccent = customAccent;
					preferences.ThemeId = themeId;
					result.Text = "Theme was not saved. Your previous theme is still active.";
				}
				else
				{
					ApplyAppTheme();
					result.Text = "Custom theme applied and saved.";
					preview.Invalidate();
				}
			});
		};
		flowLayoutPanel6.Controls.Add(button3);
		Button button4 = Button("Export recipe");
		button4.Click += delegate
		{
			guard(delegate
			{
				ThemeRecipe themeRecipe = recipe();
				CustomTheme.Create(themeRecipe);
				using SaveFileDialog saveFileDialog = new SaveFileDialog
				{
					Filter = "Theme recipe|*.json",
					FileName = "Aster-theme.json"
				};
				if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
				{
					File.WriteAllText(saveFileDialog.FileName, new JavaScriptSerializer().Serialize(themeRecipe));
					result.Text = "Theme recipe exported.";
				}
			});
		};
		flowLayoutPanel6.Controls.Add(button4);
		Button button5 = Button("Import recipe");
		button5.Click += delegate
		{
			guard(delegate
			{
				using OpenFileDialog openFileDialog = new OpenFileDialog
				{
					Filter = "Theme recipe|*.json"
				};
				if (openFileDialog.ShowDialog(this) == DialogResult.OK)
				{
					ThemeRecipe themeRecipe = CustomTheme.Read(openFileDialog.FileName);
					bgBox.Text = themeRecipe.Background;
					accentBox.Text = themeRecipe.Accent;
					preview.Palette = CustomTheme.Create(themeRecipe);
					preview.Invalidate();
					result.Text = "Recipe loaded for preview. Click Apply to use it.";
				}
			});
		};
		flowLayoutPanel6.Controls.Add(button5);
	}

	private void SetupPersonalWorkspace()
	{
		try
		{
			personal = PersonalStore.Read();
		}
		catch (Exception ex)
		{
			personalLoadError = ex.Message;
		}
		base.FormClosing += delegate(object s, FormClosingEventArgs e)
		{
			if (personalDirty)
			{
				switch (MessageBox.Show(this, "Save your notebook and checklist changes before closing?", "Unsaved workspace", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question))
				{
				case DialogResult.Cancel:
					e.Cancel = true;
					break;
				case DialogResult.Yes:
					if (!SavePersonal())
					{
						e.Cancel = true;
					}
					break;
				}
			}
		};
	}

	private bool SavePersonal()
	{
		try
		{
			if (personalLoadError != null)
			{
				throw new Exception(personalLoadError);
			}
			PersonalStore.Save(personal);
			personalDirty = false;
			return true;
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, "Could not save: " + ex.Message);
			return false;
		}
	}

	private void ExportPersonalText(string title, string data)
	{
		SaveFileDialog saveFileDialog = new SaveFileDialog();
		saveFileDialog.Filter = "Text document|*.txt";
		saveFileDialog.FileName = "Aster-" + title + ".txt";
		using SaveFileDialog saveFileDialog2 = saveFileDialog;
		if (saveFileDialog2.ShowDialog(this) == DialogResult.OK)
		{
			try
			{
				File.WriteAllText(saveFileDialog2.FileName, data, Encoding.UTF8);
				return;
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, ex.Message);
				return;
			}
		}
	}

	private void BuildNotebook()
	{
		sub.Text = "Your local scratchpad · keep troubleshooting notes, ideas and reminders";
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel();
		tableLayoutPanel.Dock = DockStyle.Fill;
		tableLayoutPanel.ColumnCount = 1;
		tableLayoutPanel.RowCount = 3;
		TableLayoutPanel tableLayoutPanel2 = tableLayoutPanel;
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));
		content.Controls.Add(tableLayoutPanel2);
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel();
		flowLayoutPanel.Dock = DockStyle.Fill;
		FlowLayoutPanel flowLayoutPanel2 = flowLayoutPanel;
		tableLayoutPanel2.Controls.Add(flowLayoutPanel2, 0, 0);
		TextBox editor = new TextBox
		{
			Multiline = true,
			AcceptsReturn = true,
			AcceptsTab = true,
			Dock = DockStyle.Fill,
			ScrollBars = ScrollBars.Vertical,
			MaxLength = 50000,
			Font = new Font("Segoe UI", 12f),
			Text = personal.Note,
			ReadOnly = (personalLoadError != null)
		};
		tableLayoutPanel2.Controls.Add(editor, 0, 1);
		Label state = new Label
		{
			Dock = DockStyle.Fill,
			Text = (personalLoadError ?? "Local only · Save writes your notebook and checklist together.")
		};
		tableLayoutPanel2.Controls.Add(state, 0, 2);
		editor.TextChanged += delegate
		{
			personal.Note = editor.Text;
			personalDirty = true;
			state.Text = "Unsaved · " + editor.TextLength.ToString("N0") + " / 50,000 characters. Text stays while switching pages.";
		};
		Button button = Button("Save workspace");
		button.Enabled = personalLoadError == null;
		button.Click += delegate
		{
			if (SavePersonal())
			{
				state.Text = "Saved locally · " + DateTime.Now.ToString("T");
			}
		};
		flowLayoutPanel2.Controls.Add(button);
		Button button2 = Button("Export text");
		button2.Click += delegate
		{
			ExportPersonalText("notes", editor.Text);
		};
		flowLayoutPanel2.Controls.Add(button2);
		Button button3 = Button("Insert date & time");
		button3.Enabled = personalLoadError == null;
		button3.Click += delegate
		{
			DateTimeOffset dateTimeOffset = (preferences.RiyadhTime ? DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(3.0)) : DateTimeOffset.Now);
			editor.SelectedText = "\r\n[" + dateTimeOffset.ToString("yyyy-MM-dd HH:mm zzz") + "] ";
			editor.Focus();
		};
		flowLayoutPanel2.Controls.Add(button3);
	}

	private void BuildChecklist()
	{
		sub.Text = "A simple local checklist · changes survive page switches · save when ready";
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel();
		tableLayoutPanel.Dock = DockStyle.Fill;
		tableLayoutPanel.ColumnCount = 1;
		tableLayoutPanel.RowCount = 4;
		TableLayoutPanel tableLayoutPanel2 = tableLayoutPanel;
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));
		content.Controls.Add(tableLayoutPanel2);
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel();
		flowLayoutPanel.Dock = DockStyle.Fill;
		FlowLayoutPanel flowLayoutPanel2 = flowLayoutPanel;
		tableLayoutPanel2.Controls.Add(flowLayoutPanel2, 0, 0);
		TextBox input = new TextBox
		{
			Width = 480,
			MaxLength = 300
		};
		flowLayoutPanel2.Controls.Add(input);
		Button add = Button("Add task");
		add.Enabled = personalLoadError == null;
		flowLayoutPanel2.Controls.Add(add);
		FlowLayoutPanel flowLayoutPanel3 = new FlowLayoutPanel();
		flowLayoutPanel3.Dock = DockStyle.Fill;
		FlowLayoutPanel flowLayoutPanel4 = flowLayoutPanel3;
		tableLayoutPanel2.Controls.Add(flowLayoutPanel4, 0, 1);
		CheckedListBox list = new CheckedListBox
		{
			Dock = DockStyle.Fill,
			CheckOnClick = true,
			BorderStyle = BorderStyle.None,
			Font = new Font("Segoe UI", 12f),
			Enabled = (personalLoadError == null)
		};
		tableLayoutPanel2.Controls.Add(list, 0, 2);
		Label state = new Label
		{
			Dock = DockStyle.Fill
		};
		tableLayoutPanel2.Controls.Add(state, 0, 3);
		CheckBox hide = new CheckBox
		{
			Text = "Hide completed",
			AutoSize = true,
			Margin = new Padding(12, 8, 8, 0)
		};
		bool filling = false;
		Action status = delegate
		{
			state.Text = personalLoadError ?? (personal.Tasks.Count((PersonalTask t) => t.Done) + " / " + personal.Tasks.Count + " completed · " + (personalDirty ? "unsaved changes" : "saved") + " · up to 200 tasks");
		};
		Action refresh = delegate
		{
			filling = true;
			list.Items.Clear();
			foreach (PersonalTask item2 in personal.Tasks.Where((PersonalTask t) => !hide.Checked || !t.Done))
			{
				list.Items.Add(item2, item2.Done);
			}
			filling = false;
			status();
		};
		Action addTask = delegate
		{
			string text = input.Text.Trim();
			if (text.Length != 0)
			{
				if (personal.Tasks.Count >= 200)
				{
					MessageBox.Show(this, "This checklist supports up to 200 tasks.");
				}
				else
				{
					personal.Tasks.Add(new PersonalTask
					{
						Title = text
					});
					personalDirty = true;
					input.Clear();
					refresh();
				}
			}
		};
		add.Click += delegate
		{
			addTask();
		};
		input.KeyDown += delegate(object s, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Return && add.Enabled)
			{
				e.SuppressKeyPress = true;
				addTask();
			}
		};
		list.ItemCheck += delegate(object s, ItemCheckEventArgs e)
		{
			if (!filling)
			{
				((PersonalTask)list.Items[e.Index]).Done = e.NewValue == CheckState.Checked;
				personalDirty = true;
				BeginInvoke((Action)delegate
				{
					if (!list.IsDisposed)
					{
						if (hide.Checked)
						{
							refresh();
						}
						else
						{
							status();
						}
					}
				});
			}
		};
		Button button = Button("Save workspace");
		button.Enabled = personalLoadError == null;
		button.Click += delegate
		{
			SavePersonal();
			status();
		};
		flowLayoutPanel4.Controls.Add(button);
		Button button2 = Button("Remove selected");
		button2.Click += delegate
		{
			if (list.SelectedItem is PersonalTask item)
			{
				removedTask = item;
				removedTaskIndex = personal.Tasks.IndexOf(item);
				personal.Tasks.Remove(item);
				personalDirty = true;
				refresh();
			}
		};
		flowLayoutPanel4.Controls.Add(button2);
		Button button3 = Button("Undo last removal");
		button3.Click += delegate
		{
			if (removedTask != null && personal.Tasks.Count < 200)
			{
				personal.Tasks.Insert(Math.Min(removedTaskIndex, personal.Tasks.Count), removedTask);
				removedTask = null;
				personalDirty = true;
				refresh();
			}
		};
		flowLayoutPanel4.Controls.Add(button3);
		flowLayoutPanel4.Controls.Add(hide);
		hide.CheckedChanged += delegate
		{
			refresh();
		};
		Button button4 = Button("Export text");
		button4.Click += delegate
		{
			ExportPersonalText("checklist", string.Join(Environment.NewLine, personal.Tasks.Select((PersonalTask t) => (t.Done ? "[x] " : "[ ] ") + t.Title)));
		};
		flowLayoutPanel2.Controls.Add(button4);
		refresh();
	}

	private void ShowActionLauncher()
	{
		List<QuickAction> choices = new List<QuickAction>();
		foreach (string key in buttons.Keys)
		{
			string target = key;
			choices.Add(new QuickAction
			{
				Label = "Open · " + key,
				Run = delegate
				{
					Navigate(target);
				}
			});
		}
		choices.Add(new QuickAction
		{
			Label = "Action · Pause / resume monitoring",
			Run = delegate
			{
				paused = !paused;
				pause.Text = (paused ? "Resume" : "Pause");
				UpdateView();
			}
		});
		choices.Add(new QuickAction
		{
			Label = "Action · Toggle distraction-free view",
			Run = ToggleFocusMode
		});
		choices.Add(new QuickAction
		{
			Label = "Action · Change app picture now",
			Run = delegate
			{
				SelectPicture(CurrentPictureNumber() + 1);
			}
		});
		choices.Add(new QuickAction
		{
			Label = "Action · Save notebook and checklist",
			Run = delegate
			{
				SavePersonal();
			}
		});
		QuickAction chosen = null;
		Form dialog = new Form
		{
			Text = "Aster · Find a page or action",
			Size = new Size(590, 520),
			StartPosition = FormStartPosition.CenterParent,
			BackColor = panel,
			ForeColor = Color.White,
			Font = Font,
			KeyPreview = true
		};
		try
		{
			TextBox searchBox = new TextBox
			{
				Dock = DockStyle.Top,
				Font = new Font("Segoe UI", 14f),
				BackColor = panel,
				ForeColor = Color.White
			};
			ListBox list = new ListBox
			{
				Dock = DockStyle.Fill,
				BackColor = panel,
				ForeColor = Color.White,
				BorderStyle = BorderStyle.None
			};
			dialog.Controls.Add(list);
			dialog.Controls.Add(searchBox);
			Action refresh = delegate
			{
				list.Items.Clear();
				foreach (QuickAction item in choices.Where((QuickAction a) => a.Label.IndexOf(searchBox.Text.Trim(), StringComparison.OrdinalIgnoreCase) >= 0))
				{
					list.Items.Add(item);
				}
				if (list.Items.Count > 0)
				{
					list.SelectedIndex = 0;
				}
			};
			Action select = delegate
			{
				chosen = list.SelectedItem as QuickAction;
				if (chosen != null)
				{
					dialog.Close();
				}
			};
			searchBox.TextChanged += delegate
			{
				refresh();
			};
			searchBox.KeyDown += delegate(object s, KeyEventArgs e)
			{
				if (e.KeyCode == Keys.Return)
				{
					e.SuppressKeyPress = true;
					select();
				}
				else if (e.KeyCode == Keys.Down)
				{
					list.Focus();
				}
			};
			list.DoubleClick += delegate
			{
				select();
			};
			list.KeyDown += delegate(object s, KeyEventArgs e)
			{
				if (e.KeyCode == Keys.Return)
				{
					e.SuppressKeyPress = true;
					select();
				}
			};
			dialog.KeyDown += delegate(object s, KeyEventArgs e)
			{
				if (e.KeyCode == Keys.Escape)
				{
					dialog.Close();
				}
			};
			refresh();
			dialog.ShowDialog(this);
		}
		finally
		{
			if (dialog != null)
			{
				((IDisposable)dialog).Dispose();
			}
		}
		if (chosen != null)
		{
			try
			{
				chosen.Run();
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, ex.Message);
			}
		}
	}

	private void BuildNetworkLab()
	{
		sub.Text = "On-demand HTTPS speed estimates · local connection details · result history";
		content.Controls.Add(new NetworkPanel());
	}

	private bool[] HomeSelection()
	{
		if (preferences.HomeSections == null || preferences.HomeSections.Length != 7 || !preferences.HomeSections.Any((bool x) => x))
		{
			return new bool[7] { true, true, true, true, true, true, true };
		}
		return preferences.HomeSections;
	}

	private void ApplyHomeLayout(TableLayoutPanel layout)
	{
		int[] heights = new int[7] { 150, 134, 50, 220, 225, 105, 95 };
		bool[] selected = HomeSelection();
		for (int i = 0; i < 7; i++)
		{
			layout.RowStyles[i].Height = (selected[i] ? heights[i] : 0);
			foreach (Control control in layout.Controls)
			{
				if (layout.GetRow(control) == i)
				{
					control.Visible = selected[i];
				}
			}
		}
		layout.Height = (from num in Enumerable.Range(0, 7)
			where selected[num]
			select num).Sum((int num) => heights[num]) + 12;
	}

	private void BuildHomeLayout()
	{
		sub.Text = "Choose what belongs on Summary · presets or your own selection";
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel();
		flowLayoutPanel.Dock = DockStyle.Fill;
		flowLayoutPanel.FlowDirection = FlowDirection.TopDown;
		flowLayoutPanel.WrapContents = false;
		flowLayoutPanel.AutoScroll = true;
		flowLayoutPanel.Padding = new Padding(16);
		FlowLayoutPanel flowLayoutPanel2 = flowLayoutPanel;
		content.Controls.Add(flowLayoutPanel2);
		flowLayoutPanel2.Controls.Add(new Label
		{
			Text = "Your home, your way.",
			AutoSize = true,
			Font = new Font("Segoe UI", 24f, FontStyle.Bold)
		});
		flowLayoutPanel2.Controls.Add(new Label
		{
			Text = "Hide sections you do not need. Your data and monitoring history stay intact.",
			AutoSize = true,
			ForeColor = muted,
			Margin = new Padding(0, 12, 0, 18)
		});
		CheckedListBox list = new CheckedListBox
		{
			Width = 680,
			Height = 245,
			CheckOnClick = true,
			BorderStyle = BorderStyle.None,
			Font = new Font("Segoe UI", 12f)
		};
		bool[] array = HomeSelection();
		for (int i = 0; i < 7; i++)
		{
			list.Items.Add(HomeLabels[i], array[i]);
		}
		flowLayoutPanel2.Controls.Add(list);
		FlowLayoutPanel flowLayoutPanel3 = new FlowLayoutPanel();
		flowLayoutPanel3.Width = 700;
		flowLayoutPanel3.Height = 50;
		FlowLayoutPanel flowLayoutPanel4 = flowLayoutPanel3;
		flowLayoutPanel2.Controls.Add(flowLayoutPanel4);
		Action<bool[]> set = delegate(bool[] values)
		{
			for (int j = 0; j < 7; j++)
			{
				list.SetItemChecked(j, values[j]);
			}
		};
		string[] array2 = new string[3] { "Everything", "Essentials", "Quiet dashboard" };
		foreach (string text in array2)
		{
			string name = text;
			Button button = Button(text);
			button.Click += delegate
			{
				set((!(name == "Everything")) ? ((!(name == "Essentials")) ? new bool[7] { true, true, false, false, false, false, false } : new bool[7] { true, true, true, true, false, false, false }) : new bool[7] { true, true, true, true, true, true, true });
			};
			flowLayoutPanel4.Controls.Add(button);
		}
		Button button2 = Button("Save layout & open Summary");
		button2.Click += delegate
		{
			bool[] array3 = (from index in Enumerable.Range(0, 7)
				select list.GetItemChecked(index)).ToArray();
			if (!array3.Any((bool x) => x))
			{
				MessageBox.Show(this, "Keep at least one Summary section.");
			}
			else
			{
				preferences.HomeSections = array3;
				SavePreferences();
				Navigate("Summary");
			}
		};
		flowLayoutPanel2.Controls.Add(button2);
	}

	private void BuildHardwareCompare()
	{
		sub.Text = "Compare two Aster hardware reports · changes are observations, not fault diagnoses";
		string before = null;
		string after = null;
		List<SpecDifference> differences = null;
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel();
		tableLayoutPanel.Dock = DockStyle.Fill;
		tableLayoutPanel.ColumnCount = 1;
		tableLayoutPanel.RowCount = 3;
		TableLayoutPanel tableLayoutPanel2 = tableLayoutPanel;
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 90f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		content.Controls.Add(tableLayoutPanel2);
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel();
		flowLayoutPanel.Dock = DockStyle.Fill;
		flowLayoutPanel.WrapContents = true;
		FlowLayoutPanel flowLayoutPanel2 = flowLayoutPanel;
		tableLayoutPanel2.Controls.Add(flowLayoutPanel2, 0, 0);
		Label state = new Label
		{
			Dock = DockStyle.Fill,
			Text = "Load two saved Hardware reports, or load an older report and capture current specs."
		};
		tableLayoutPanel2.Controls.Add(state, 0, 1);
		DataGridView table = MakeGrid();
		string[] array = new string[4] { "Specification", "Before", "After", "Change" };
		foreach (string text in array)
		{
			table.Columns.Add(text, text);
		}
		table.Columns[0].FillWeight = 140f;
		tableLayoutPanel2.Controls.Add(table, 0, 2);
		CheckBox only = new CheckBox
		{
			Text = "Changes only",
			Checked = true,
			AutoSize = true,
			Margin = new Padding(8, 8, 8, 0)
		};
		Action render = delegate
		{
			table.Rows.Clear();
			if (differences != null)
			{
				foreach (SpecDifference item in differences.Where((SpecDifference d) => !only.Checked || d.Change != "Unchanged"))
				{
					table.Rows.Add(item.Field, item.Before, item.After, item.Change);
				}
			}
		};
		Action compare = delegate
		{
			if (before != null && after != null)
			{
				differences = ReportComparison.Compare(before, after);
				state.Text = differences.Count((SpecDifference d) => d.Change != "Unchanged") + " changed / added / removed fields · " + differences.Count + " total. Provider availability can also change.";
				render();
			}
		};
		Action<Action> guard = delegate(Action a)
		{
			try
			{
				a();
			}
			catch (Exception ex)
			{
				state.Text = ex.Message;
			}
		};
		string[] array2 = new string[2] { "before", "after" };
		foreach (string text2 in array2)
		{
			string which = text2;
			Button button = Button("Load " + text2 + " report");
			button.Click += delegate
			{
				guard(delegate
				{
					using OpenFileDialog openFileDialog = new OpenFileDialog
					{
						Filter = "Hardware report|*.txt"
					};
					if (openFileDialog.ShowDialog(this) == DialogResult.OK)
					{
						string text3 = ReportComparison.Read(openFileDialog.FileName);
						if (which == "before")
						{
							before = text3;
						}
						else
						{
							after = text3;
						}
						state.Text = "Loaded " + which + ": " + Path.GetFileName(openFileDialog.FileName);
						compare();
					}
				});
			};
			flowLayoutPanel2.Controls.Add(button);
		}
		Button capture = Button("Capture current as after");
		capture.Click += async delegate
		{
			capture.Enabled = false;
			state.Text = "Reading hardware providers…";
			try
			{
				int num2 = default(int);
				_ = num2;
				_ = 0;
				try
				{
					string value = await Task.Run(() => HardwareReport.Collect());
					if (!table.IsDisposed)
					{
						ReportComparison.Parse(value);
						after = value;
						state.Text = "Current report captured.";
						compare();
					}
				}
				catch (Exception ex)
				{
					if (!state.IsDisposed)
					{
						state.Text = ex.Message;
					}
				}
			}
			finally
			{
				if (!capture.IsDisposed)
				{
					capture.Enabled = true;
				}
			}
		};
		flowLayoutPanel2.Controls.Add(capture);
		flowLayoutPanel2.Controls.Add(only);
		only.CheckedChanged += delegate
		{
			render();
		};
		Button button2 = Button("Export comparison");
		button2.Click += delegate
		{
			guard(delegate
			{
				if (differences == null)
				{
					throw new Exception("Load both reports first.");
				}
				SaveCsv("hardware-comparison", new string[1][] { new string[4] { "Specification", "Before", "After", "Change" } }.Concat(differences.Select((SpecDifference d) => new string[4] { d.Field, d.Before, d.After, d.Change })));
			});
		};
		flowLayoutPanel2.Controls.Add(button2);
		Button button3 = Button("Prepare AI explanation");
		button3.Click += delegate
		{
			if (differences == null)
			{
				state.Text = "Load both reports first.";
			}
			else
			{
				string text3 = string.Join("\r\n", from d in differences.Where((SpecDifference d) => d.Change != "Unchanged").Take(35)
					select d.Field + ": " + d.Before + " → " + d.After);
				if (text3.Length > 4800)
				{
					text3 = text3.Substring(0, 4800) + "\r\n[truncated]";
				}
				Navigate("Windows advisor");
				content.Controls.OfType<AdvisorPanel>().FirstOrDefault()?.PrepareEvidence("Hardware report comparison (up to 35 differing fields). These are not proven faults.\r\n" + ((text3.Length == 0) ? "No differences found." : text3));
			}
		};
		flowLayoutPanel2.Controls.Add(button3);
	}

	private void BuildBackupCenter()
	{
		sub.Text = "Local backups · review before restoring · no Windows system files";
		string rootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Pulse");
		Dictionary<string, byte[]> selected = null;
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel();
		tableLayoutPanel.Dock = DockStyle.Fill;
		tableLayoutPanel.ColumnCount = 1;
		tableLayoutPanel.RowCount = 3;
		tableLayoutPanel.Padding = new Padding(12);
		TableLayoutPanel tableLayoutPanel2 = tableLayoutPanel;
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 100f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 55f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		content.Controls.Add(tableLayoutPanel2);
		tableLayoutPanel2.Controls.Add(new Label
		{
			Dock = DockStyle.Fill,
			Text = "Back up saved Aster preferences, notebook/checklist, network history and saved cases. Backups are ordinary ZIP files, not encrypted. They may contain personal notes and diagnostic data.\r\nRestore merges the archive: matching files are replaced; unrelated files stay. Current matching files are backed up automatically before replacement. Close other Aster copies first. Wallpaper files and Windows repairs are not included."
		}, 0, 0);
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel();
		flowLayoutPanel.Dock = DockStyle.Fill;
		FlowLayoutPanel flowLayoutPanel2 = flowLayoutPanel;
		tableLayoutPanel2.Controls.Add(flowLayoutPanel2, 0, 1);
		TextBox review = ReadableText();
		review.Text = "Create an archive, or choose one to validate and review its file list.";
		tableLayoutPanel2.Controls.Add(review, 0, 2);
		Button button = Button("Create backup ZIP");
		button.Click += delegate
		{
			try
			{
				if (personalDirty && (MessageBox.Show(this, "Save current notebook/checklist edits before creating the backup?", "Save edits", MessageBoxButtons.OKCancel) != DialogResult.OK || !SavePersonal()))
				{
					return;
				}
				using SaveFileDialog saveFileDialog = new SaveFileDialog
				{
					Filter = "Aster backup|*.zip",
					FileName = "Aster-backup-" + DateTime.Now.ToString("yyyyMMdd-HHmm") + ".zip"
				};
				if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
				{
					int num = AsterBackup.Create(rootPath, saveFileDialog.FileName);
					review.Text = "Backed up " + num + " files to:\r\n" + saveFileDialog.FileName;
				}
			}
			catch (Exception ex)
			{
				review.Text = ex.Message;
			}
		};
		flowLayoutPanel2.Controls.Add(button);
		Button button2 = Button("Choose backup to review");
		Button restore = Button("Restore reviewed files");
		restore.Enabled = false;
		button2.Click += delegate
		{
			using OpenFileDialog openFileDialog = new OpenFileDialog
			{
				Filter = "Aster backup|*.zip"
			};
			if (openFileDialog.ShowDialog(this) == DialogResult.OK)
			{
				selected = null;
				restore.Enabled = false;
				try
				{
					selected = AsterBackup.Read(openFileDialog.FileName);
					review.Text = "VALIDATED ARCHIVE · " + selected.Count + " files\r\n\r\n" + string.Join("\r\n", selected.Select((KeyValuePair<string, byte[]> pair) => (File.Exists(Path.Combine(rootPath, pair.Key.Replace('/', Path.DirectorySeparatorChar))) ? "REPLACE  " : "ADD      ") + pair.Key + " · " + pair.Value.Length + " bytes")) + "\r\n\r\nOnly the listed files will be written. Saved settings apply immediately. Notes/checklist are reloaded only if they are included in this archive.";
					restore.Enabled = true;
					return;
				}
				catch (Exception ex)
				{
					review.Text = "Archive rejected: " + ex.Message;
					return;
				}
			}
		};
		flowLayoutPanel2.Controls.Add(button2);
		restore.Click += delegate
		{
			if (selected == null || MessageBox.Show(this, "Restore the reviewed " + selected.Count + " files? Matching files will be replaced and backed up first." + ((personalDirty && selected.ContainsKey("PersonalWorkspace.json")) ? "\r\nYou have unsaved notebook/checklist changes. Cancel and save them first to include them in the pre-restore backup." : ""), "Confirm reviewed restore", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2) != DialogResult.OK)
			{
				return;
			}
			try
			{
				string text = AsterBackup.Restore(rootPath, selected);
				ApplyRestoredWorkspace(selected.Keys);
				review.Text = "Restore completed. Prior matching files are in:\r\n" + text + "\r\n\r\nUnrelated files were left in place.";
				selected = null;
				restore.Enabled = false;
			}
			catch (Exception ex)
			{
				review.Text = ex.Message;
			}
		};
		flowLayoutPanel2.Controls.Add(restore);
	}

	private void ApplyRestoredWorkspace(IEnumerable<string> files)
	{
		HashSet<string> hashSet = new HashSet<string>(files, StringComparer.OrdinalIgnoreCase);
		if (hashSet.Contains("Workspace.json"))
		{
			preferences = WorkspaceOptions.Read();
			timer.Interval = preferences.Interval;
			base.TopMost = preferences.AlwaysOnTop;
		}
		if (hashSet.Contains("PersonalWorkspace.json"))
		{
			try
			{
				personal = PersonalStore.Read();
				personalLoadError = null;
			}
			catch (Exception ex)
			{
				personalLoadError = ex.Message;
			}
			personalDirty = false;
		}
		RebuildNavigation();
		RefreshDailyPicture();
		ApplyAppTheme();
	}

	private void StopDesktopMods()
	{
		Form[] array = desktopMods.ToArray();
		foreach (Form form in array)
		{
			if (!form.IsDisposed)
			{
				form.Close();
			}
		}
		desktopMods.Clear();
	}

	private void ShowDesktopMod(string key, Func<Form> create)
	{
		Form form = desktopMods.FirstOrDefault((Form f) => !f.IsDisposed && f.Name == key);
		if (form != null)
		{
			form.Activate();
			return;
		}
		Form form2 = null;
		try
		{
			form2 = create();
			form2.Name = key;
			desktopMods.Add(form2);
			form2.FormClosed += delegate
			{
				desktopMods.Remove(form2);
			};
			form2.Show(this);
		}
		catch (Exception ex)
		{
			if (form2 != null)
			{
				desktopMods.Remove(form2);
				form2.Dispose();
			}
			MessageBox.Show(this, "Could not open this widget: " + ex.Message, "Desktop mods");
		}
	}

	private Form ModWindow(string title, int width, int height)
	{
		Form form = new Form();
		form.Text = "Aster · " + title;
		form.Width = width;
		form.Height = height;
		form.MinimumSize = new Size(280, 110);
		form.TopMost = true;
		form.BackColor = Color.FromArgb(18, 22, 32);
		form.ForeColor = Color.White;
		form.StartPosition = FormStartPosition.CenterScreen;
		form.ShowInTaskbar = false;
		return form;
	}

	private void BuildDesktopMods()
	{
		if (!modCloseHook)
		{
			modCloseHook = true;
			base.FormClosed += delegate
			{
				StopDesktopMods();
			};
		}
		sub.Text = "Your desktop, your style · optional floating widgets · close all at any time";
		FlowLayoutPanel flow = new FlowLayoutPanel
		{
			Dock = DockStyle.Fill,
			AutoScroll = true,
			FlowDirection = FlowDirection.TopDown,
			WrapContents = false,
			Padding = new Padding(20)
		};
		content.Controls.Add(flow);
		flow.Controls.Add(new Label
		{
			Text = "DESKTOP MODS",
			AutoSize = true,
			Font = new Font("Segoe UI", 26f, FontStyle.Bold)
		});
		flow.Controls.Add(new Label
		{
			Text = "Aster widgets inspired by your reference. Drag their title bars to arrange them.\r\nThey leave Windows Explorer and your Start button unchanged. Mods start only when you open them.",
			AutoSize = true,
			Margin = new Padding(0, 12, 0, 18)
		});
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel();
		flowLayoutPanel.AutoSize = true;
		flowLayoutPanel.WrapContents = false;
		FlowLayoutPanel flowLayoutPanel2 = flowLayoutPanel;
		flow.Controls.Add(flowLayoutPanel2);
		ComboBox preset = new ComboBox
		{
			DropDownStyle = ComboBoxStyle.DropDownList,
			Width = 200
		};
		preset.Items.AddRange(new object[6] { "Neon blue", "Sakura pink", "Emerald", "Solar gold", "Violet night", "Ice white" });
		preset.SelectedIndex = modPalette;
		flowLayoutPanel2.Controls.Add(preset);
		ComboBox styles = new ComboBox
		{
			DropDownStyle = ComboBoxStyle.DropDownList,
			Width = 180
		};
		styles.Items.AddRange(new object[3] { "Orbit rings", "Starlight", "Comet" });
		styles.SelectedItem = modTrail;
		flowLayoutPanel2.Controls.Add(styles);
		Button button = Button("Use style");
		flowLayoutPanel2.Controls.Add(button);
		Label status = new Label
		{
			AutoSize = true,
			Text = "Styles affect newly opened widgets. Close and reopen a widget to restyle it."
		};
		flow.Controls.Add(status);
		button.Click += delegate
		{
			modPalette = preset.SelectedIndex;
			modAccent = (new Color[6]
			{
				Color.DeepSkyBlue,
				Color.HotPink,
				Color.MediumSpringGreen,
				Color.Gold,
				Color.MediumPurple,
				Color.AliceBlue
			})[preset.SelectedIndex];
			modTrail = styles.Text;
			status.Text = "Style selected: " + preset.Text + " / " + modTrail + ". Reopen widgets to use it.";
		};
		ComboBox display = new ComboBox
		{
			DropDownStyle = ComboBoxStyle.DropDownList,
			Width = 300
		};
		Screen[] allScreens = Screen.AllScreens;
		foreach (Screen screen in allScreens)
		{
			display.Items.Add(screen.DeviceName + (screen.Primary ? " (primary)" : ""));
		}
		display.SelectedIndex = Math.Min(modScreen, display.Items.Count - 1);
		display.SelectedIndexChanged += delegate
		{
			modScreen = display.SelectedIndex;
		};
		flow.Controls.Add(display);
		CheckBox eco = new CheckBox
		{
			Text = "Low-power cursor effects (15 fps)",
			AutoSize = true,
			Checked = modEco
		};
		eco.CheckedChanged += delegate
		{
			modEco = eco.Checked;
		};
		flow.Controls.Add(eco);
		FlowLayoutPanel flowLayoutPanel3 = new FlowLayoutPanel();
		flowLayoutPanel3.AutoSize = true;
		FlowLayoutPanel flowLayoutPanel4 = flowLayoutPanel3;
		flow.Controls.Add(flowLayoutPanel4);
		Action<string, EventHandler> action = delegate(string name, EventHandler handler)
		{
			Button button3 = Button(name);
			button3.Width = 300;
			button3.Height = 40;
			button3.Click += handler;
			flow.Controls.Add(button3);
		};
		action("Open live system bar", delegate
		{
			ShowDesktopMod("bar", delegate
			{
				Form form = ModWindow("System bar", 720, 145);
				Label label = new Label
				{
					Dock = DockStyle.Fill,
					Padding = new Padding(12),
					Font = new Font("Segoe UI", 12f),
					ForeColor = modAccent
				};
				form.Controls.Add(label);
				Button button3 = new Button
				{
					Text = "Close all mods",
					Dock = DockStyle.Bottom,
					Height = 28
				};
				button3.Click += delegate
				{
					StopDesktopMods();
				};
				form.Controls.Add(button3);
				System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer
				{
					Interval = 1000
				};
				Action update = delegate
				{
					label.Text = ((current == null) ? "Waiting for a system sample…" : ((paused ? "PAUSED · " : "") + "CPU " + current.Cpu.ToString("F0") + "%   RAM " + current.MemoryPercent.ToString("F0") + "%   ↓ " + Inventory.Bytes(current.Receive) + "/s   ↑ " + Inventory.Bytes(current.Send) + "/s\r\n" + DateTime.Now.ToString("dddd, dd MMMM yyyy   HH:mm:ss")));
				};
				timer.Tick += delegate
				{
					update();
				};
				form.Disposed += delegate
				{
					timer.Dispose();
				};
				update();
				timer.Start();
				return form;
			});
		});
		action("Open quick-launch dock", delegate
		{
			ShowDesktopMod("dock", delegate
			{
				Form f = ModWindow("Quick launch", 590, 210);
				FlowLayoutPanel flowLayoutPanel5 = new FlowLayoutPanel
				{
					Dock = DockStyle.Fill,
					Padding = new Padding(10),
					AutoScroll = true
				};
				f.Controls.Add(flowLayoutPanel5);
				string[] array2 = new string[7] { "Tools hub", "Summary", "Wallpaper studio", "Themes", "Focus timer", "Windows advisor", "Desktop mods" };
				foreach (string text2 in array2)
				{
					string target = text2;
					Button button3 = new Button
					{
						Text = target,
						Width = 165,
						Height = 32,
						BackColor = Color.FromArgb(30, 40, 55),
						FlatStyle = FlatStyle.Flat,
						ForeColor = modAccent
					};
					button3.Click += delegate
					{
						if (base.WindowState == FormWindowState.Minimized)
						{
							base.WindowState = FormWindowState.Normal;
						}
						Show();
						Activate();
						Navigate(target);
					};
					flowLayoutPanel5.Controls.Add(button3);
				}
				string[] array3 = new string[3] { "explorer.exe", "notepad.exe", "calc.exe" };
				foreach (string text3 in array3)
				{
					string file = text3;
					Button button4 = new Button
					{
						Text = Path.GetFileNameWithoutExtension(file),
						Width = 165,
						Height = 32,
						BackColor = Color.FromArgb(30, 40, 55),
						FlatStyle = FlatStyle.Flat,
						ForeColor = modAccent
					};
					button4.Click += delegate
					{
						try
						{
							Process.Start(new ProcessStartInfo(file)
							{
								UseShellExecute = true
							});
						}
						catch (Exception ex)
						{
							MessageBox.Show(f, "Unable to open app: " + ex.Message);
						}
					};
					flowLayoutPanel5.Controls.Add(button4);
				}
				return f;
			});
		});
		action("Open storage cards", delegate
		{
			ShowDesktopMod("disks", delegate
			{
				Form form = ModWindow("Storage cards", 480, 370);
				FlowLayoutPanel flowLayoutPanel5 = new FlowLayoutPanel
				{
					Dock = DockStyle.Fill,
					AutoScroll = true,
					FlowDirection = FlowDirection.TopDown,
					WrapContents = false,
					Padding = new Padding(12)
				};
				form.Controls.Add(flowLayoutPanel5);
				flowLayoutPanel5.Controls.Add(new Label
				{
					Text = "Local fixed drives · snapshot when opened",
					AutoSize = true,
					ForeColor = modAccent
				});
				DriveInfo[] drives = DriveInfo.GetDrives();
				foreach (DriveInfo driveInfo in drives)
				{
					try
					{
						if (driveInfo.DriveType == DriveType.Fixed && driveInfo.IsReady)
						{
							long totalSize = driveInfo.TotalSize;
							long availableFreeSpace = driveInfo.AvailableFreeSpace;
							flowLayoutPanel5.Controls.Add(new Label
							{
								Text = driveInfo.Name + "   " + Inventory.Bytes((ulong)availableFreeSpace) + " free of " + Inventory.Bytes((ulong)totalSize),
								AutoSize = true,
								Margin = new Padding(0, 14, 0, 4)
							});
							flowLayoutPanel5.Controls.Add(new ProgressBar
							{
								Width = 405,
								Height = 18,
								Value = DesktopModMath.UsedPercent(totalSize, availableFreeSpace)
							});
						}
					}
					catch (IOException)
					{
						flowLayoutPanel5.Controls.Add(new Label
						{
							Text = driveInfo.Name + " currently unavailable",
							AutoSize = true
						});
					}
					catch (UnauthorizedAccessException)
					{
						flowLayoutPanel5.Controls.Add(new Label
						{
							Text = driveInfo.Name + " access unavailable",
							AutoSize = true
						});
					}
				}
				return form;
			});
		});
		action("Start cursor effects", delegate
		{
			ShowDesktopMod("trail", () => new DesktopTrail(Screen.AllScreens[Math.Min(modScreen, Screen.AllScreens.Length - 1)].Bounds, modAccent, modTrail, modEco ? 66 : 33));
		});
		action("Open ambient clock", delegate
		{
			ShowDesktopMod("clock", delegate
			{
				Form form = ModWindow("Ambient clock", 480, 180);
				Label clock = new Label
				{
					Dock = DockStyle.Fill,
					TextAlign = ContentAlignment.MiddleCenter,
					ForeColor = modAccent,
					Font = new Font("Segoe UI", 23f)
				};
				form.Controls.Add(clock);
				System.Windows.Forms.Timer tick = new System.Windows.Forms.Timer
				{
					Interval = 1000
				};
				Action refresh = delegate
				{
					clock.Text = DateTime.Now.ToString("HH:mm:ss\r\ndddd, dd MMMM yyyy");
				};
				tick.Tick += delegate
				{
					refresh();
				};
				form.Disposed += delegate
				{
					tick.Dispose();
				};
				refresh();
				tick.Start();
				return form;
			});
		});
		string[] array = new string[3] { "Focus layout", "Creator layout", "Neon layout" };
		foreach (string text in array)
		{
			string mode = text;
			Button button2 = Button(mode);
			flowLayoutPanel4.Controls.Add(button2);
			button2.Click += delegate
			{
				StopDesktopMods();
				string[] array2 = ((mode == "Focus layout") ? new string[1] { "Open ambient clock" } : ((mode == "Creator layout") ? new string[3] { "Open live system bar", "Open quick-launch dock", "Open storage cards" } : new string[3] { "Open live system bar", "Open quick-launch dock", "Start cursor effects" }));
				string[] array3 = array2;
				foreach (string title in array3)
				{
					IEnumerable<Button> source = flow.Controls.OfType<Button>();
					Func<Button, bool> predicate = (Button x) => x.Text == title;
					source.FirstOrDefault(predicate)?.PerformClick();
				}
				ArrangeDesktopMods();
				status.Text = mode + " enabled. You can close any widget separately.";
			};
		}
		action("Arrange on selected screen", delegate
		{
			ArrangeDesktopMods();
		});
		action("Close all desktop mods", delegate
		{
			StopDesktopMods();
			status.Text = "All desktop mods are off.";
		});
		flow.Controls.Add(new Label
		{
			Text = "Cursor effects follow the pointer on the selected screen and let clicks pass through.\r\nSwitch them off here or from the system bar. They close when Aster closes.\r\nStorage cards show a snapshot; reopen to refresh. No system performance boost is claimed.",
			AutoSize = true,
			Margin = new Padding(0, 20, 0, 12)
		});
		action("Open Wallpaper Studio", delegate
		{
			Navigate("Wallpaper studio");
		});
		action("Browse Aster themes", delegate
		{
			Navigate("Themes");
		});
	}

	private void ArrangeDesktopMods()
	{
		Rectangle workingArea = Screen.AllScreens[Math.Min(modScreen, Screen.AllScreens.Length - 1)].WorkingArea;
		int num = workingArea.Top + 12;
		foreach (Form item in desktopMods.Where((Form x) => !x.IsDisposed && x.Name != "trail"))
		{
			item.Width = Math.Min(item.Width, workingArea.Width - 24);
			item.Height = Math.Min(item.Height, workingArea.Height - 24);
			item.Location = new Point(workingArea.Left + 12, Math.Min(num, workingArea.Bottom - item.Height - 12));
			num += item.Height + 12;
		}
	}
}
