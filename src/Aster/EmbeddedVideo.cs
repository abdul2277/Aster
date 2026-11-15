using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Aster;

internal sealed class EmbeddedVideo : UserControl
{
	internal const string PlayerUrl = "https://aster-player.example/index.html";

	private readonly WebView2 web = new WebView2
	{
		Dock = DockStyle.Fill
	};

	private readonly Label notice = new Label
	{
		Dock = DockStyle.Fill,
		Text = "Preparing the in-app player…",
		TextAlign = ContentAlignment.MiddleCenter,
		ForeColor = Color.White,
		BackColor = Color.FromArgb(16, 23, 34)
	};

	private readonly Button reload = new Button
	{
		Text = "Reload player",
		AutoSize = true
	};

	private bool initializing;

	private bool initialized;

	private readonly string profileDirectory;

	public EmbeddedVideo(Action back, string profileFolder = null)
	{
		EmbeddedVideo embeddedVideo = this;
		profileDirectory = profileFolder ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Aster", "VideoBrowser");
		Dock = DockStyle.Fill;
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			RowCount = 2,
			ColumnCount = 1
		};
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		base.Controls.Add(tableLayoutPanel);
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
		{
			Dock = DockStyle.Fill
		};
		Button button = new Button
		{
			Text = "Back to alerts",
			AutoSize = true
		};
		button.Click += delegate
		{
			back();
		};
		reload.Click += async delegate
		{
			if (initialized)
			{
				web.CoreWebView2.Navigate("https://aster-player.example/index.html");
			}
			else
			{
				await InitializePlayer();
			}
		};
		flowLayoutPanel.Controls.Add(button);
		flowLayoutPanel.Controls.Add(reload);
		flowLayoutPanel.Controls.Add(new Label
		{
			Text = "YouTube video · plays inside Aster · stops when you leave this page",
			AutoSize = true,
			Margin = new Padding(16, 9, 0, 0)
		});
		tableLayoutPanel.Controls.Add(flowLayoutPanel, 0, 0);
		Panel panel = new Panel
		{
			Dock = DockStyle.Fill
		};
		panel.Controls.Add(web);
		panel.Controls.Add(notice);
		tableLayoutPanel.Controls.Add(panel, 0, 1);
		base.Load += async delegate
		{
			await InitializePlayer();
		};
	}

	private async Task InitializePlayer()
	{
		if (initializing || initialized || base.IsDisposed)
		{
			return;
		}
		initializing = true;
		reload.Enabled = false;
		try
		{
			string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Video");
			if (!File.Exists(Path.Combine(folder, "index.html")))
			{
				throw new FileNotFoundException();
			}
			string profile = profileDirectory;
			CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync(null, profile);
			if (base.IsDisposed)
			{
				return;
			}
			await web.EnsureCoreWebView2Async(environment);
			if (base.IsDisposed)
			{
				return;
			}
			CoreWebView2 core = web.CoreWebView2;
			core.Settings.AreDevToolsEnabled = false;
			core.Settings.AreDefaultContextMenusEnabled = false;
			core.Settings.IsStatusBarEnabled = false;
			core.Settings.AreDefaultScriptDialogsEnabled = false;
			core.Settings.IsWebMessageEnabled = false;
			core.PermissionRequested += delegate(object s, CoreWebView2PermissionRequestedEventArgs e)
			{
				e.State = CoreWebView2PermissionState.Deny;
			};
			core.NewWindowRequested += delegate(object s, CoreWebView2NewWindowRequestedEventArgs e)
			{
				e.Handled = true;
			};
			core.DownloadStarting += delegate(object s, CoreWebView2DownloadStartingEventArgs e)
			{
				e.Cancel = true;
			};
			core.NavigationStarting += delegate(object s, CoreWebView2NavigationStartingEventArgs e)
			{
				if (!string.Equals(e.Uri, "https://aster-player.example/index.html", StringComparison.OrdinalIgnoreCase))
				{
					e.Cancel = true;
					notice.Text = "This player stays inside Aster. Use Back to alerts to return.";
				}
			};
			core.NavigationCompleted += delegate(object s, CoreWebView2NavigationCompletedEventArgs e)
			{
				if (e.IsSuccess)
				{
					notice.Visible = false;
				}
				else
				{
					notice.Text = "The player page could not load. Select Reload player to try again.";
					notice.Visible = true;
				}
			};
			core.SetVirtualHostNameToFolderMapping("aster-player.example", folder, CoreWebView2HostResourceAccessKind.DenyCors);
			initialized = true;
			notice.Visible = false;
			core.Navigate("https://aster-player.example/index.html");
		}
		catch (WebView2RuntimeNotFoundException)
		{
			if (!base.IsDisposed)
			{
				notice.Text = "Microsoft Edge WebView2 Runtime is required for the in-app player.\r\nInstall it from Microsoft's WebView2 website, then reload.";
			}
		}
		catch (Exception)
		{
			if (!base.IsDisposed)
			{
				notice.Text = "The in-app player could not start. Extract the whole Aster folder\r\nand check access to the local video-browser profile, then reload.";
			}
		}
		finally
		{
			initializing = false;
			if (!base.IsDisposed)
			{
				reload.Enabled = true;
			}
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			web.Dispose();
		}
		base.Dispose(disposing);
	}
}
