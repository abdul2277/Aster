using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Aster;

internal class RepairWindow : Form
{
	private TextBox output;

	private Label state;

	private Process process;

	private bool running;

	private StringBuilder log = new StringBuilder();

	private string path;

	private RecoveryJob job;

	public RepairWindow(RecoveryJob value)
	{
		job = value;
		Text = "Aster Recovery · " + job.Title;
		base.Size = new Size(850, 600);
		base.StartPosition = FormStartPosition.CenterScreen;
		BackColor = Color.FromArgb(20, 26, 34);
		ForeColor = Color.White;
		Font = new Font("Segoe UI", 10f);
		state = new Label
		{
			Dock = DockStyle.Top,
			Height = 80,
			Padding = new Padding(16),
			Text = job.Title + "\r\n" + job.Exe + " " + job.Arguments
		};
		base.Controls.Add(state);
		output = new TextBox
		{
			Multiline = true,
			ScrollBars = ScrollBars.Vertical,
			Dock = DockStyle.Fill,
			ReadOnly = true,
			BackColor = Color.FromArgb(13, 18, 25),
			ForeColor = Color.FromArgb(182, 221, 219),
			Font = new Font("Consolas", 10f),
			BorderStyle = BorderStyle.None
		};
		base.Controls.Add(output);
		output.BringToFront();
		EventHandler value2 = delegate
		{
			Run();
		};
		base.Shown += value2;
		base.FormClosing += delegate(object s, FormClosingEventArgs e)
		{
			if (running)
			{
				e.Cancel = true;
				MessageBox.Show(this, "This Windows tool is still running. Wait for it to finish before closing this window.");
			}
		};
	}

	private void Append(string text)
	{
		if (!base.IsDisposed)
		{
			if (base.InvokeRequired)
			{
				BeginInvoke(new Action<string>(Append), text);
			}
			else
			{
				text = (text ?? "").Replace("\0", "");
				log.AppendLine(text);
				output.AppendText(text + Environment.NewLine);
			}
		}
	}

	private async void Run()
	{
		running = true;
		try
		{
			int num = default(int);
			_ = num;
			_ = 0;
			try
			{
				string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Pulse", "RecoveryLogs");
				Directory.CreateDirectory(dir);
				path = Path.Combine(dir, DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + job.Id + ".txt");
				Append(string.Concat(DateTime.Now, " · ", job.FileName, " ", job.Arguments));
				process = new Process
				{
					StartInfo = new ProcessStartInfo(job.FileName, job.Arguments)
					{
						UseShellExecute = false,
						CreateNoWindow = true,
						RedirectStandardOutput = true,
						RedirectStandardError = true
					}
				};
				if (job.Exe == "sfc.exe")
				{
					process.StartInfo.StandardOutputEncoding = Encoding.Unicode;
				}
				process.OutputDataReceived += delegate(object s, DataReceivedEventArgs e)
				{
					if (e.Data != null)
					{
						Append(e.Data);
					}
				};
				process.ErrorDataReceived += delegate(object s, DataReceivedEventArgs e)
				{
					if (e.Data != null)
					{
						Append(e.Data);
					}
				};
				process.Start();
				process.BeginOutputReadLine();
				process.BeginErrorReadLine();
				await Task.Run(delegate
				{
					process.WaitForExit();
				});
				Append("Tool finished. Exit code: " + process.ExitCode + ". Review the output; completion does not by itself prove Windows is healthy.");
				state.Text = "Finished · exit code " + process.ExitCode + "\r\nLog: " + path;
			}
			catch (Exception ex)
			{
				Append("Could not complete: " + ex.Message);
				state.Text = "Could not complete · review details below";
			}
		}
		finally
		{
			running = false;
			if (path != null)
			{
				try
				{
					File.WriteAllText(path, log.ToString(), Encoding.UTF8);
				}
				catch (Exception ex2)
				{
					Append("Could not save log: " + ex2.Message);
				}
			}
			if (process != null)
			{
				process.Dispose();
			}
		}
	}
}
