using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Aster;

internal class NetworkPanel : UserControl
{
	private class ResultItem
	{
		public NetworkResult Value;

		public override string ToString()
		{
			return Value.TimeUtc + " · " + Value.Profile + " · ↓ " + (Value.DownloadMbps.HasValue ? Value.DownloadMbps.Value.ToString("F1") : "—") + " / ↑ " + (Value.UploadMbps.HasValue ? Value.UploadMbps.Value.ToString("F1") : "—") + " Mbps";
		}
	}

	private CancellationTokenSource cancellation;

	private Button start;

	private TextBox output;

	private Label state;

	private ComboBox profiles;

	private ListBox history;

	private MetricTile down;

	private MetricTile up;

	private MetricTile latency;

	private NetworkResult latest;

	public NetworkPanel()
	{
		Dock = DockStyle.Fill;
		TabControl tabControl = new TabControl
		{
			Dock = DockStyle.Fill
		};
		base.Controls.Add(tabControl);
		TabPage tabPage = new TabPage("Speed test");
		TabPage tabPage2 = new TabPage("Connection details");
		TabPage tabPage3 = new TabPage("Saved results");
		tabControl.TabPages.AddRange(new TabPage[3] { tabPage, tabPage2, tabPage3 });
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			ColumnCount = 1,
			RowCount = 5,
			Padding = new Padding(12)
		};
		int[] array = new int[4] { 82, 50, 145, 36 };
		foreach (int num in array)
		{
			tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, num));
		}
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		tabPage.Controls.Add(tableLayoutPanel);
		tableLayoutPanel.Controls.Add(new Label
		{
			Dock = DockStyle.Fill,
			Text = "Connects to speed.cloudflare.com when you click Start. Cloudflare receives your network address and generated test traffic; no personal files are uploaded. Quick uses about 18 MiB; Standard about 72 MiB, plus protocol overhead. Latency-only sends small HTTPS requests.\r\nThese are sequential HTTPS estimates, not a guaranteed maximum line speed.",
			AutoEllipsis = true
		}, 0, 0);
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
		{
			Dock = DockStyle.Fill
		};
		profiles = new ComboBox
		{
			DropDownStyle = ComboBoxStyle.DropDownList,
			Width = 260
		};
		profiles.Items.AddRange(NetworkProfile.All);
		profiles.SelectedIndex = 0;
		flowLayoutPanel.Controls.Add(profiles);
		start = B("Start test");
		start.Click += async delegate
		{
			await RunTest();
		};
		flowLayoutPanel.Controls.Add(start);
		Button button = B("Cancel");
		button.Click += delegate
		{
			if (cancellation != null)
			{
				cancellation.Cancel();
			}
		};
		flowLayoutPanel.Controls.Add(button);
		Button button2 = B("Copy last result");
		button2.Click += delegate
		{
			if (latest != null)
			{
				try
				{
					Clipboard.SetText(latest.Summary());
				}
				catch (Exception ex)
				{
					state.Text = ex.Message;
				}
			}
		};
		flowLayoutPanel.Controls.Add(button2);
		tableLayoutPanel.Controls.Add(flowLayoutPanel, 0, 1);
		TableLayoutPanel tableLayoutPanel2 = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			ColumnCount = 3
		};
		for (int num2 = 0; num2 < 3; num2++)
		{
			tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f));
		}
		down = new MetricTile
		{
			Caption = "DOWNLOAD",
			Detail = "Mbps · median of 3 samples",
			Accent = Color.FromArgb(105, 212, 240)
		};
		up = new MetricTile
		{
			Caption = "UPLOAD",
			Detail = "Mbps · median of 3 samples",
			Accent = Color.FromArgb(186, 159, 253)
		};
		latency = new MetricTile
		{
			Caption = "HTTPS LATENCY",
			Detail = "ms · median of 5 samples",
			Accent = Color.FromArgb(113, 225, 187)
		};
		tableLayoutPanel2.Controls.Add(down, 0, 0);
		tableLayoutPanel2.Controls.Add(up, 1, 0);
		tableLayoutPanel2.Controls.Add(latency, 2, 0);
		tableLayoutPanel.Controls.Add(tableLayoutPanel2, 0, 2);
		state = new Label
		{
			Dock = DockStyle.Fill,
			Text = "Ready · no test traffic until Start"
		};
		tableLayoutPanel.Controls.Add(state, 0, 3);
		output = TextArea();
		output.Text = "For comparisons, use the same test profile and connection. Pause downloads yourself if you want a less busy measurement. Leaving this page or closing Aster cancels the test. Only completed results are saved locally.";
		tableLayoutPanel.Controls.Add(output, 0, 4);
		TableLayoutPanel tableLayoutPanel3 = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			ColumnCount = 1,
			RowCount = 2,
			Padding = new Padding(12)
		};
		tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));
		tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		tabPage2.Controls.Add(tableLayoutPanel3);
		TextBox details = TextArea();
		details.Text = "Click Refresh to read local adapter details. This sends no internet test traffic.";
		Button button3 = B("Refresh local connections");
		button3.Click += delegate
		{
			details.Text = ConnectionDetails();
		};
		tableLayoutPanel3.Controls.Add(button3, 0, 0);
		tableLayoutPanel3.Controls.Add(details, 0, 1);
		TableLayoutPanel tableLayoutPanel4 = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			ColumnCount = 1,
			RowCount = 3,
			Padding = new Padding(12)
		};
		tableLayoutPanel4.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));
		tableLayoutPanel4.RowStyles.Add(new RowStyle(SizeType.Absolute, 180f));
		tableLayoutPanel4.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		tabPage3.Controls.Add(tableLayoutPanel4);
		FlowLayoutPanel flowLayoutPanel2 = new FlowLayoutPanel
		{
			Dock = DockStyle.Fill
		};
		Button button4 = B("Refresh history");
		button4.Click += delegate
		{
			LoadHistory();
		};
		flowLayoutPanel2.Controls.Add(button4);
		Button button5 = B("Export history as CSV");
		button5.Click += delegate
		{
			ExportHistory();
		};
		flowLayoutPanel2.Controls.Add(button5);
		tableLayoutPanel4.Controls.Add(flowLayoutPanel2, 0, 0);
		history = new ListBox
		{
			Dock = DockStyle.Fill
		};
		tableLayoutPanel4.Controls.Add(history, 0, 1);
		TextBox detail = TextArea();
		tableLayoutPanel4.Controls.Add(detail, 0, 2);
		history.SelectedIndexChanged += delegate
		{
			if (history.SelectedItem is ResultItem resultItem)
			{
				detail.Text = resultItem.Value.Summary();
			}
		};
		LoadHistory();
	}

	private static Button B(string text)
	{
		ThemeButton themeButton = new ThemeButton();
		themeButton.Text = text;
		themeButton.AutoSize = true;
		themeButton.Height = 34;
		themeButton.FlatStyle = FlatStyle.Flat;
		themeButton.Padding = new Padding(10, 2, 10, 2);
		themeButton.Margin = new Padding(0, 2, 10, 2);
		return themeButton;
	}

	private static TextBox TextArea()
	{
		TextBox textBox = new TextBox();
		textBox.Dock = DockStyle.Fill;
		textBox.Multiline = true;
		textBox.ReadOnly = true;
		textBox.ScrollBars = ScrollBars.Vertical;
		textBox.Font = new Font("Segoe UI", 10f);
		textBox.BorderStyle = BorderStyle.None;
		return textBox;
	}

	private async Task RunTest()
	{
		start.Enabled = false;
		profiles.Enabled = false;
		latest = null;
		down.Value = (up.Value = (latency.Value = "—"));
		MetricTile[] array = new MetricTile[3] { down, up, latency };
		foreach (MetricTile metricTile in array)
		{
			metricTile.Invalidate();
		}
		cancellation = new CancellationTokenSource();
		output.Text = "Running… Values appear only after a complete test.";
		try
		{
			int num = default(int);
			_ = num;
			_ = 0;
			try
			{
				using (HttpClient client = NetworkMeasure.Client())
				{
					latest = await NetworkMeasure.Run(progress: new Progress<string>(delegate(string message)
					{
						if (!base.IsDisposed)
						{
							state.Text = message;
						}
					}), client: client, profile: (NetworkProfile)profiles.SelectedItem, cancel: cancellation.Token);
				}
				if (base.IsDisposed)
				{
					return;
				}
				down.Value = (latest.DownloadMbps.HasValue ? latest.DownloadMbps.Value.ToString("F1") : "—");
				up.Value = (latest.UploadMbps.HasValue ? latest.UploadMbps.Value.ToString("F1") : "—");
				latency.Value = latest.LatencyMs.ToString("F1");
				MetricTile[] array2 = new MetricTile[3] { down, up, latency };
				foreach (MetricTile metricTile2 in array2)
				{
					metricTile2.Invalidate();
				}
				output.Text = latest.Summary();
				try
				{
					NetworkHistory.Add(latest);
					state.Text = "Completed · saved locally · results are estimates";
					LoadHistory();
				}
				catch (Exception ex)
				{
					state.Text = "Completed, but history could not be saved: " + ex.Message;
				}
			}
			catch (Exception ex2)
			{
				if (!base.IsDisposed)
				{
					bool flag = cancellation.IsCancellationRequested || ex2 is OperationCanceledException;
					state.Text = (flag ? "Canceled or timed out · no completed result saved" : "Test failed · no completed result saved");
					output.Text = (flag ? "The test stopped. Partial traffic may already have been transferred." : (ex2.Message + "\r\nThe provider, firewall, proxy or connection may be unavailable. This does not prove your internet is disconnected."));
				}
			}
		}
		finally
		{
			if (!base.IsDisposed)
			{
				start.Enabled = true;
				profiles.Enabled = true;
			}
			cancellation.Dispose();
			cancellation = null;
		}
	}

	private void LoadHistory()
	{
		try
		{
			history.Items.Clear();
			foreach (NetworkResult item in NetworkHistory.Read())
			{
				history.Items.Add(new ResultItem
				{
					Value = item
				});
			}
			if (history.Items.Count > 0)
			{
				history.SelectedIndex = 0;
			}
		}
		catch (Exception ex)
		{
			history.Items.Add("History unavailable: " + ex.Message);
		}
	}

	private void ExportHistory()
	{
		try
		{
			List<NetworkResult> source = NetworkHistory.Read();
			SaveFileDialog saveFileDialog = new SaveFileDialog();
			saveFileDialog.Filter = "CSV file|*.csv";
			saveFileDialog.FileName = "Aster-network-history.csv";
			using SaveFileDialog saveFileDialog2 = saveFileDialog;
			if (saveFileDialog2.ShowDialog(this) == DialogResult.OK)
			{
				Func<string, string> csv = (string v) => "\"" + (v ?? "").Replace("\"", "\"\"") + "\"";
				List<string> list = new List<string>();
				list.Add("Time UTC,Profile,Download Mbps,Upload Mbps,HTTPS latency ms,Variation ms,Payload bytes");
				List<string> list2 = list;
				list2.AddRange(source.Select((NetworkResult v) => string.Join(",", new string[7]
				{
					v.TimeUtc,
					v.Profile,
					Convert.ToString(v.DownloadMbps, CultureInfo.InvariantCulture),
					Convert.ToString(v.UploadMbps, CultureInfo.InvariantCulture),
					v.LatencyMs.ToString(CultureInfo.InvariantCulture),
					v.VariationMs.ToString(CultureInfo.InvariantCulture),
					v.PayloadBytes.ToString()
				}.Select(csv))));
				File.WriteAllLines(saveFileDialog2.FileName, list2, Encoding.UTF8);
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, ex.Message);
		}
	}

	public static string ConnectionDetails()
	{
		StringBuilder stringBuilder = new StringBuilder("LOCAL ADAPTERS · not an internet connectivity test\r\nLink speed is the adapter's reported rate, not your measured internet speed.\r\n\r\n");
		try
		{
			NetworkInterface[] allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
			foreach (NetworkInterface networkInterface in allNetworkInterfaces)
			{
				stringBuilder.AppendLine(string.Concat(networkInterface.Name, " · ", networkInterface.NetworkInterfaceType, " · ", networkInterface.OperationalStatus));
				try
				{
					stringBuilder.AppendLine("Reported link: " + ((double)networkInterface.Speed / 1000000.0).ToString("F0") + " Mbps");
					IPInterfaceProperties iPProperties = networkInterface.GetIPProperties();
					stringBuilder.AppendLine("Local addresses: " + string.Join(", ", iPProperties.UnicastAddresses.Select((UnicastIPAddressInformation a) => a.Address.ToString())));
					stringBuilder.AppendLine("Gateways: " + string.Join(", ", iPProperties.GatewayAddresses.Select((GatewayIPAddressInformation a) => a.Address.ToString())));
					stringBuilder.AppendLine("DNS servers: " + string.Join(", ", iPProperties.DnsAddresses.Select((IPAddress a) => a.ToString())));
				}
				catch (Exception ex)
				{
					stringBuilder.AppendLine("Details unavailable: " + ex.Message);
				}
				stringBuilder.AppendLine();
			}
		}
		catch (Exception ex2)
		{
			stringBuilder.AppendLine("Adapter inventory unavailable: " + ex2.Message);
		}
		return stringBuilder.ToString();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && cancellation != null)
		{
			cancellation.Cancel();
		}
		base.Dispose(disposing);
	}
}
