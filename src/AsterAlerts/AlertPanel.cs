using System;
using System.Collections;
using System.Collections.Generic;
using System.Device.Location;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace AsterAlerts;

public sealed class AlertPanel : UserControl
{
	private readonly TextBox latitude = new TextBox();

	private readonly TextBox longitude = new TextBox();

	private readonly TextBox token = new TextBox
	{
		UseSystemPasswordChar = true
	};

	private readonly TextBox chat = new TextBox();

	private readonly NumericUpDown radius = new NumericUpDown
	{
		Minimum = 1m,
		Maximum = 1000m,
		Value = 20m
	};

	private readonly NumericUpDown poll = new NumericUpDown
	{
		Minimum = 5m,
		Maximum = 3600m,
		Value = 30m
	};

	private readonly NumericUpDown age = new NumericUpDown
	{
		Minimum = 1m,
		Maximum = 1440m,
		Value = 15m
	};

	private readonly DataGridView feeds = new DataGridView();

	private readonly TextBox log = new TextBox
	{
		Multiline = true,
		ReadOnly = true,
		ScrollBars = ScrollBars.Vertical,
		Dock = DockStyle.Fill,
		Font = new Font("Consolas", 10f)
	};

	private readonly Button start = new Button
	{
		Text = "Start alerts"
	};

	private readonly Button check = new Button
	{
		Text = "Check feeds (no sends)"
	};

	private readonly Button stop = new Button
	{
		Text = "Stop",
		Enabled = false
	};

	private readonly Button save = new Button
	{
		Text = "Save feed settings"
	};

	private readonly Label status = new Label
	{
		Text = "Stopped · enter your details to begin",
		Dock = DockStyle.Fill
	};

	private Process worker;

	private bool closing;

	private readonly string stateFolder;

	private readonly Button locate = new Button
	{
		Text = "Use my location",
		AutoSize = true,
		Dock = DockStyle.Fill
	};

	private readonly Label locationStatus = new Label
	{
		Text = "Choose Use my location, or enter coordinates above. The selected point stays fixed until you change it.",
		Dock = DockStyle.Fill,
		Font = new Font("Segoe UI", 9f)
	};

	private GeoCoordinateWatcher watcher;

	private readonly Timer locationTimer = new Timer
	{
		Interval = 30000
	};

	public bool IsMonitoring => worker != null;

	public event EventHandler WatchVideoRequested;

	public AlertPanel()
		: this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Aster", "AirDefense"))
	{
	}

	public AlertPanel(string folder)
	{
		stateFolder = folder;
		AutoScroll = true;
		BackColor = Color.FromArgb(16, 23, 34);
		ForeColor = Color.FromArgb(232, 240, 250);
		Font = new Font("Segoe UI", 10f);
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			Padding = new Padding(16),
			MinimumSize = new Size(850, 730),
			ColumnCount = 1,
			RowCount = 8
		};
		int[] array = new int[7] { 55, 62, 195, 30, 170, 48, 32 };
		foreach (int num in array)
		{
			tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, num));
		}
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		base.Controls.Add(tableLayoutPanel);
		tableLayoutPanel.Controls.Add(new Label
		{
			Text = "Your nearby alerts",
			Font = new Font("Segoe UI", 25f, FontStyle.Bold),
			AutoSize = true
		}, 0, 0);
		tableLayoutPanel.Controls.Add(new Label
		{
			Text = "Nearby public advisories, delivered to your phone. No feeds are active until you start.\r\nPublic reports may be delayed or incomplete. Keep using official emergency alerts.",
			Dock = DockStyle.Fill
		}, 0, 1);
		TableLayoutPanel tableLayoutPanel2 = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			ColumnCount = 4,
			RowCount = 4
		};
		for (int j = 0; j < 4; j++)
		{
			tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
		}
		tableLayoutPanel.Controls.Add(tableLayoutPanel2, 0, 2);
		AddField(tableLayoutPanel2, "Home latitude", latitude, 0, 0);
		AddField(tableLayoutPanel2, "Home longitude", longitude, 1, 0);
		AddField(tableLayoutPanel2, "Radius (km)", radius, 2, 0);
		AddField(tableLayoutPanel2, "Poll interval (seconds)", poll, 3, 0);
		AddField(tableLayoutPanel2, "Telegram bot token", token, 0, 1);
		AddField(tableLayoutPanel2, "Telegram chat ID", chat, 1, 1);
		AddField(tableLayoutPanel2, "Maximum report age (minutes)", age, 2, 1);
		Label control = new Label
		{
			Text = "Your home preset is stored locally in home-location.json. Telegram details are session-only.",
			Dock = DockStyle.Fill,
			Font = new Font("Segoe UI", 9f)
		};
		tableLayoutPanel2.Controls.Add(control, 0, 3);
		tableLayoutPanel2.SetColumnSpan(control, 4);
		SetupLocation(tableLayoutPanel2);
		tableLayoutPanel.Controls.Add(new Label
		{
			Text = "FEEDS · HTTPS URLs or Telegram @channel · choose telegram format for channels",
			Dock = DockStyle.Fill
		}, 0, 3);
		feeds.Dock = DockStyle.Fill;
		feeds.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
		feeds.BackgroundColor = Color.FromArgb(24, 36, 53);
		feeds.RowHeadersWidth = 30;
		feeds.AllowUserToAddRows = true;
		feeds.AllowUserToDeleteRows = true;
		feeds.DefaultCellStyle.BackColor = Color.FromArgb(24, 36, 53);
		feeds.DefaultCellStyle.ForeColor = ForeColor;
		feeds.DefaultCellStyle.SelectionBackColor = Color.FromArgb(40, 74, 93);
		feeds.Columns.Add("name", "Source name");
		DataGridViewComboBoxColumn dataGridViewComboBoxColumn = new DataGridViewComboBoxColumn
		{
			Name = "format",
			HeaderText = "Format",
			FillWeight = 45f
		};
		dataGridViewComboBoxColumn.Items.AddRange("rss", "geojson", "json", "telegram");
		feeds.Columns.Add(dataGridViewComboBoxColumn);
		feeds.Columns.Add("url", "HTTPS feed URL / Telegram @channel");
		feeds.Columns[2].FillWeight = 230f;
		feeds.DefaultValuesNeeded += delegate(object s, DataGridViewRowEventArgs e)
		{
			e.Row.Cells[1].Value = "rss";
		};
		feeds.DataError += delegate(object s, DataGridViewDataErrorEventArgs e)
		{
			e.ThrowException = false;
		};
		tableLayoutPanel.Controls.Add(feeds, 0, 4);
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
		{
			Dock = DockStyle.Fill,
			WrapContents = false
		};
		Button[] array2 = new Button[4] { save, check, start, stop };
		foreach (Button button in array2)
		{
			button.AutoSize = true;
			button.Height = 34;
			button.BackColor = Color.FromArgb(32, 49, 72);
			button.ForeColor = ForeColor;
			button.FlatStyle = FlatStyle.Flat;
			flowLayoutPanel.Controls.Add(button);
		}
		Button button2 = new Button
		{
			Text = "YouTube player",
			AccessibleName = "Watch your linked video inside the Air Defense feed",
			AutoSize = true,
			Height = 34,
			BackColor = Color.FromArgb(32, 49, 72),
			ForeColor = ForeColor,
			FlatStyle = FlatStyle.Flat
		};
		button2.Click += delegate
		{
			this.WatchVideoRequested?.Invoke(this, EventArgs.Empty);
		};
		flowLayoutPanel.Controls.Add(button2);
		tableLayoutPanel.Controls.Add(flowLayoutPanel, 0, 5);
		tableLayoutPanel.Controls.Add(status, 0, 6);
		log.BackColor = Color.FromArgb(12, 18, 28);
		log.ForeColor = ForeColor;
		tableLayoutPanel.Controls.Add(log, 0, 7);
		save.Click += delegate
		{
			Guard(delegate
			{
				SaveConfiguration();
				Write("Feed settings saved. Location and Telegram credentials were not saved.");
			});
		};
		check.Click += delegate
		{
			Guard(delegate
			{
				Launch(dry: true);
			});
		};
		start.Click += delegate
		{
			Guard(delegate
			{
				Launch(dry: false);
			});
		};
		stop.Click += delegate
		{
			StopWorker();
		};
		LoadSettings();
		LoadHomeLocation(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "home-location.json"));
		Write("Choose real providers. Supported: GeoRSS, GeoJSON, JSON, or public Telegram channels. No alerts are active.");
	}

	private void AddField(TableLayoutPanel grid, string caption, Control input, int col, int row)
	{
		Panel panel = new Panel();
		panel.Dock = DockStyle.Fill;
		panel.Height = 48;
		Panel panel2 = panel;
		panel2.Controls.Add(new Label
		{
			Text = caption,
			Dock = DockStyle.Top,
			Height = 22,
			Font = new Font("Segoe UI", 9f)
		});
		input.Dock = DockStyle.Bottom;
		input.AccessibleName = caption;
		panel2.Controls.Add(input);
		grid.Controls.Add(panel2, col, row);
	}

	private void Guard(Action action)
	{
		try
		{
			action();
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, (ex is ArgumentException) ? ex.Message : "Could not complete this action. Check file access and the bundled runtime.", "Aster alerts");
		}
	}

	private void Write(string message)
	{
		if (log.TextLength > 60000)
		{
			log.Text = log.Text.Substring(log.TextLength - 30000);
		}
		log.AppendText(message + Environment.NewLine);
	}

	private void Dispatch(Action action)
	{
		if (closing || base.IsDisposed || !base.IsHandleCreated)
		{
			return;
		}
		try
		{
			BeginInvoke(action);
		}
		catch (InvalidOperationException)
		{
		}
	}

	public static double Coordinate(string text, double min, double max)
	{
		if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) || double.IsNaN(result) || double.IsInfinity(result) || result < min || result > max)
		{
			throw new ArgumentException("Enter valid coordinates using a decimal point. Latitude: -90 to 90. Longitude: -180 to 180.");
		}
		return result;
	}

	public static string NormalizeChannel(string input)
	{
		Match match = Regex.Match(input.Trim(), "^(?:https://t\\.me/(?:s/)?)?@?([A-Za-z][A-Za-z0-9_]{3,31})/?$", RegexOptions.IgnoreCase);
		if (!match.Success)
		{
			throw new ArgumentException("Enter a public channel username such as @channelname or its https://t.me/s/channelname URL. Private invites and post links are not supported.");
		}
		return "https://t.me/s/" + match.Groups[1].Value.ToLowerInvariant();
	}

	private Config ReadForm()
	{
		feeds.EndEdit();
		Config config = new Config();
		config.air_defense_feed.radius_km = (double)radius.Value;
		config.air_defense_feed.poll_seconds = (double)poll.Value;
		config.air_defense_feed.max_age_seconds = (double)age.Value * 60.0;
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		foreach (DataGridViewRow item in (IEnumerable)feeds.Rows)
		{
			if (!item.IsNewRow)
			{
				string text = Convert.ToString(item.Cells[0].Value).Trim();
				string text2 = Convert.ToString(item.Cells[1].Value);
				string text3 = Convert.ToString(item.Cells[2].Value).Trim();
				if (text2 == "telegram")
				{
					text3 = NormalizeChannel(text3);
				}
				if (!Regex.IsMatch(text, "^[A-Za-z0-9 _.-]{1,60}$") || !hashSet.Add(text))
				{
					throw new ArgumentException("Give each feed a unique simple source name (letters, numbers, spaces, periods, hyphens).");
				}
				if (!Uri.TryCreate(text3, UriKind.Absolute, out var result) || result.Scheme != "https" || result.UserInfo != "" || result.Fragment != "" || result.Host.EndsWith(".invalid"))
				{
					throw new ArgumentException("Enter a real HTTPS feed URL without a username, password or fragment.");
				}
				if (!new string[4] { "rss", "geojson", "json", "telegram" }.Contains(text2))
				{
					throw new ArgumentException("Choose rss, geojson, json or telegram for each feed.");
				}
				config.air_defense_feed.sources.Add(new Feed
				{
					name = text,
					format = text2,
					url = text3
				});
			}
		}
		if (config.air_defense_feed.sources.Count < 1 || config.air_defense_feed.sources.Count > 20)
		{
			throw new ArgumentException("Add between 1 and 20 real feed URLs.");
		}
		if (config.air_defense_feed.sources.Any((Feed f) => f.format == "telegram") && poll.Value < 30m)
		{
			throw new ArgumentException("Telegram previews require a polling interval of at least 30 seconds.");
		}
		return config;
	}

	private void SaveConfiguration()
	{
		Config obj = ReadForm();
		Directory.CreateDirectory(stateFolder);
		string text = Path.Combine(stateFolder, "config.json");
		string text2 = text + ".tmp";
		File.WriteAllText(text2, new JavaScriptSerializer().Serialize(obj));
		if (File.Exists(text))
		{
			File.Replace(text2, text, null);
		}
		else
		{
			File.Move(text2, text);
		}
	}

	private void LoadSettings()
	{
		string path = Path.Combine(stateFolder, "config.json");
		if (!File.Exists(path))
		{
			return;
		}
		try
		{
			Config config = new JavaScriptSerializer().Deserialize<Config>(File.ReadAllText(path));
			Settings air_defense_feed = config.air_defense_feed;
			radius.Value = Math.Max(1m, Math.Min(1000m, (decimal)air_defense_feed.radius_km));
			poll.Value = Math.Max(5m, Math.Min(3600m, (decimal)air_defense_feed.poll_seconds));
			age.Value = Math.Max(1m, Math.Min(1440m, (decimal)air_defense_feed.max_age_seconds / 60m));
			foreach (Feed item in air_defense_feed.sources.Take(20))
			{
				feeds.Rows.Add(item.name, item.format, item.url);
			}
		}
		catch
		{
			Write("Saved feed settings could not be read. Enter them again to replace the configuration.");
		}
	}

	private void Launch(bool dry)
	{
		if (worker != null)
		{
			throw new ArgumentException("Stop the current monitor first.");
		}
		CancelLocation();
		double num = Coordinate(latitude.Text, -90.0, 90.0);
		double num2 = Coordinate(longitude.Text, -180.0, 180.0);
		if (!dry && (!Regex.IsMatch(token.Text.Trim(), "^[0-9]+:[A-Za-z0-9_-]+$") || string.IsNullOrWhiteSpace(chat.Text)))
		{
			throw new ArgumentException("Enter your Telegram bot token and chat ID before starting alerts.");
		}
		string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
		string text = Path.Combine(baseDirectory, "runtime", "python.exe");
		string text2 = Path.Combine(baseDirectory, "AirDefense", "air_defense.py");
		if (!File.Exists(text) || !File.Exists(text2))
		{
			throw new ArgumentException("Extract the entire Aster folder first. Keep runtime and AirDefense next to Aster.exe.");
		}
		SaveConfiguration();
		ProcessStartInfo processStartInfo = new ProcessStartInfo();
		processStartInfo.FileName = text;
		processStartInfo.Arguments = "-I -B -u \"" + text2 + "\" --config \"" + Path.Combine(stateFolder, "config.json") + "\"" + (dry ? " --dry-run --once" : "");
		processStartInfo.WorkingDirectory = baseDirectory;
		processStartInfo.UseShellExecute = false;
		processStartInfo.CreateNoWindow = true;
		processStartInfo.RedirectStandardOutput = true;
		processStartInfo.RedirectStandardError = true;
		ProcessStartInfo processStartInfo2 = processStartInfo;
		processStartInfo2.EnvironmentVariables["ASTER_HOME_LAT"] = num.ToString("R", CultureInfo.InvariantCulture);
		processStartInfo2.EnvironmentVariables["ASTER_HOME_LON"] = num2.ToString("R", CultureInfo.InvariantCulture);
		processStartInfo2.EnvironmentVariables["ASTER_TELEGRAM_TOKEN"] = token.Text.Trim();
		processStartInfo2.EnvironmentVariables["ASTER_TELEGRAM_CHAT_ID"] = chat.Text.Trim();
		Process process = new Process
		{
			StartInfo = processStartInfo2,
			EnableRaisingEvents = true
		};
		process.OutputDataReceived += delegate(object s, DataReceivedEventArgs e)
		{
			if (e.Data != null)
			{
				Dispatch(delegate
				{
					Write(e.Data);
				});
			}
		};
		process.ErrorDataReceived += delegate(object s, DataReceivedEventArgs e)
		{
			if (e.Data != null)
			{
				Dispatch(delegate
				{
					Write(e.Data);
				});
			}
		};
		process.Exited += delegate
		{
			Dispatch(delegate
			{
				if (worker == process)
				{
					int exitCode = process.ExitCode;
					worker = null;
					process.Dispose();
					Running(running: false);
					status.Text = ((exitCode != 0) ? "Stopped with an error · review results below" : (dry ? "Feed check finished · review results below" : "Stopped"));
				}
			});
		};
		worker = process;
		try
		{
			process.Start();
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();
			Running(running: true);
			status.Text = (dry ? "Checking feeds · no messages will be sent" : "Monitoring · coverage and delivery status appear below");
		}
		catch
		{
			worker = null;
			process.Dispose();
			throw;
		}
	}

	private void Running(bool running)
	{
		Button button = start;
		Button button2 = check;
		Button button3 = save;
		DataGridView dataGridView = feeds;
		TextBox textBox = latitude;
		TextBox textBox2 = longitude;
		TextBox textBox3 = token;
		TextBox textBox4 = chat;
		NumericUpDown numericUpDown = radius;
		NumericUpDown numericUpDown2 = poll;
		bool flag = (age.Enabled = !running);
		bool flag3 = (numericUpDown2.Enabled = flag);
		bool flag5 = (numericUpDown.Enabled = flag3);
		bool flag7 = (textBox4.Enabled = flag5);
		bool flag9 = (textBox3.Enabled = flag7);
		bool flag11 = (textBox2.Enabled = flag9);
		bool flag13 = (textBox.Enabled = flag11);
		bool flag15 = (dataGridView.Enabled = flag13);
		bool flag17 = (button3.Enabled = flag15);
		bool enabled = (button2.Enabled = flag17);
		button.Enabled = enabled;
		stop.Enabled = running;
		locate.Enabled = !running;
	}

	private void StopWorker()
	{
		Process process = worker;
		worker = null;
		if (process != null)
		{
			try
			{
				if (!process.HasExited)
				{
					process.Kill();
				}
			}
			catch (InvalidOperationException)
			{
			}
			finally
			{
				process.Dispose();
			}
		}
		if (!closing)
		{
			Running(running: false);
			status.Text = "Stopped · monitoring is not active";
		}
	}

	internal void LoadHomeLocation(string path)
	{
		if (!File.Exists(path))
		{
			return;
		}
		try
		{
			Dictionary<string, object> dictionary = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(File.ReadAllText(path));
			double num = Coordinate(Convert.ToString(dictionary["latitude"], CultureInfo.InvariantCulture), -90.0, 90.0);
			double num2 = Coordinate(Convert.ToString(dictionary["longitude"], CultureInfo.InvariantCulture), -180.0, 180.0);
			latitude.Text = num.ToString("0.######", CultureInfo.InvariantCulture);
			longitude.Text = num2.ToString("0.######", CultureInfo.InvariantCulture);
			locationStatus.Text = "Your home coordinates are selected from the local preset. Use my location only to replace them for this session.";
		}
		catch
		{
			locationStatus.Text = "The home location preset could not be read. Enter coordinates or use Windows location.";
		}
	}

	private void SetupLocation(TableLayoutPanel fields)
	{
		fields.Controls.Add(locate, 3, 1);
		fields.Controls.Add(locationStatus, 0, 2);
		fields.SetColumnSpan(locationStatus, 4);
		locate.BackColor = Color.FromArgb(32, 49, 72);
		locate.ForeColor = ForeColor;
		locate.FlatStyle = FlatStyle.Flat;
		locate.Click += delegate
		{
			BeginLocation();
		};
		locationTimer.Tick += delegate
		{
			CancelLocation();
			locationStatus.Text = "No fresh location available. Enable Windows Location services or enter coordinates manually.";
		};
	}

	private void BeginLocation()
	{
		if (IsMonitoring)
		{
			return;
		}
		CancelLocation();
		locate.Enabled = false;
		locationStatus.Text = "Requesting your location from Windows… (up to 30 seconds)";
		try
		{
			GeoCoordinateWatcher current = new GeoCoordinateWatcher(GeoPositionAccuracy.High);
			watcher = current;
			current.PositionChanged += delegate(object s, GeoPositionChangedEventArgs<GeoCoordinate> e)
			{
				Dispatch(delegate
				{
					if (watcher == current)
					{
						ApplyLocation(e.Position, DateTimeOffset.UtcNow);
					}
				});
			};
			current.StatusChanged += delegate(object s, GeoPositionStatusChangedEventArgs e)
			{
				Dispatch(delegate
				{
					if (watcher == current)
					{
						if (current.Permission == GeoPositionPermission.Denied || e.Status == GeoPositionStatus.Disabled)
						{
							CancelLocation();
							locationStatus.Text = "Location is blocked or unavailable. Check Windows Settings > Privacy & security > Location, or enter coordinates.";
						}
						else if (e.Status == GeoPositionStatus.NoData)
						{
							locationStatus.Text = "Windows has no location fix yet. Waiting up to 30 seconds…";
						}
					}
				});
			};
			locationTimer.Start();
			current.Start(suppressPermissionPrompt: false);
		}
		catch
		{
			CancelLocation();
			locationStatus.Text = "Windows location could not start. Enable a location provider or enter coordinates manually.";
		}
	}

	public static bool FreshPosition(GeoPosition<GeoCoordinate> position, DateTimeOffset now)
	{
		if (position == null || position.Location == null || position.Location.IsUnknown)
		{
			return false;
		}
		double totalSeconds = (now - position.Timestamp).TotalSeconds;
		if (totalSeconds >= -30.0)
		{
			return totalSeconds <= 120.0;
		}
		return false;
	}

	internal bool ApplyLocation(GeoPosition<GeoCoordinate> position, DateTimeOffset now)
	{
		if (!FreshPosition(position, now))
		{
			locationStatus.Text = "Waiting for a fresh Windows location; an old or unknown fix was ignored.";
			return false;
		}
		GeoCoordinate location = position.Location;
		latitude.Text = location.Latitude.ToString("F6", CultureInfo.InvariantCulture);
		longitude.Text = location.Longitude.ToString("F6", CultureInfo.InvariantCulture);
		string text = (double.IsNaN(location.HorizontalAccuracy) ? "accuracy unavailable" : ("estimated accuracy ±" + location.HorizontalAccuracy.ToString("F0", CultureInfo.InvariantCulture) + " m"));
		locationStatus.Text = "Windows location selected · " + text + ". This is your current position; use it as home only when you are home.";
		CancelLocation();
		return true;
	}

	private void CancelLocation()
	{
		locationTimer.Stop();
		GeoCoordinateWatcher geoCoordinateWatcher = watcher;
		watcher = null;
		if (geoCoordinateWatcher != null)
		{
			geoCoordinateWatcher.Stop();
			geoCoordinateWatcher.Dispose();
		}
		if (!closing && !base.IsDisposed)
		{
			locate.Enabled = !IsMonitoring;
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && !closing)
		{
			closing = true;
			CancelLocation();
			locationTimer.Dispose();
			StopWorker();
			token.Clear();
			chat.Clear();
			latitude.Clear();
			longitude.Clear();
		}
		base.Dispose(disposing);
	}
}
