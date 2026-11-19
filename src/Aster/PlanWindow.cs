using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Aster;

internal class PlanWindow : Form
{
	private readonly RepairPlan plan;

	private TextBox text;

	private Label title;

	private bool running;

	private bool stopPending;

	private StringBuilder transcript = new StringBuilder();

	public PlanWindow(RepairPlan p)
	{
		plan = p;
		Text = "Aster · Approved repair plan";
		base.Size = new Size(940, 680);
		BackColor = Color.FromArgb(16, 23, 32);
		ForeColor = Color.White;
		Font = new Font("Segoe UI", 10f);
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			RowCount = 3,
			ColumnCount = 1,
			Padding = new Padding(15)
		};
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 65f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
		base.Controls.Add(tableLayoutPanel);
		title = new Label
		{
			Text = p.Title + "\r\n" + p.Description,
			Dock = DockStyle.Fill
		};
		tableLayoutPanel.Controls.Add(title, 0, 0);
		text = new TextBox
		{
			Multiline = true,
			ScrollBars = ScrollBars.Vertical,
			Dock = DockStyle.Fill,
			ReadOnly = true,
			BackColor = Color.FromArgb(23, 33, 45),
			ForeColor = Color.FromArgb(210, 232, 238),
			Font = new Font("Consolas", 10f)
		};
		tableLayoutPanel.Controls.Add(text, 0, 1);
		Button stop = new Button
		{
			Text = "Stop after the current Windows tool finishes",
			Dock = DockStyle.Fill
		};
		stop.Click += delegate
		{
			stopPending = true;
			stop.Enabled = false;
			Append("Remaining steps canceled. The current tool will be allowed to finish.");
		};
		tableLayoutPanel.Controls.Add(stop, 0, 2);
		EventHandler value = async delegate
		{
			await Run();
		};
		base.Shown += value;
		base.FormClosing += delegate(object s, FormClosingEventArgs e)
		{
			if (running)
			{
				e.Cancel = true;
				MessageBox.Show(this, "A Windows tool is running. Use Stop after current tool, then wait for completion.");
			}
		};
	}

	private void Append(string line)
	{
		if (!base.IsDisposed)
		{
			if (base.InvokeRequired)
			{
				BeginInvoke(new Action<string>(Append), line);
			}
			else
			{
				line = (line ?? "").Replace("\0", "");
				transcript.AppendLine(line);
				text.AppendText(line + Environment.NewLine);
			}
		}
	}

	public static bool ContinueAfter(int exitCode)
	{
		return exitCode == 0;
	}

	private async Task Run()
	{
		running = true;
		string outcome = "Plan completed; review the Windows tool output. No automatic claim of repair success is made.";
		try
		{
			int num = default(int);
			_ = num;
			_ = 0;
			try
			{
				Append("APPROVED PLAN: " + plan.Title + " · " + DateTime.Now);
				for (int i = 0; i < plan.Jobs.Length; i++)
				{
					if (stopPending)
					{
						outcome = "Stopped before remaining steps.";
						break;
					}
					RecoveryJob job = RecoveryJob.Find(plan.Jobs[i]);
					if (job == null)
					{
						throw new Exception("Plan contains an unsupported operation.");
					}
					title.Text = "Step " + (i + 1) + " / " + plan.Jobs.Length + " · " + job.Title + "\r\nNo automatic reboot. Remaining steps stop if this tool fails.";
					Append("\r\n> " + job.FileName + " " + job.Arguments);
					Process process = new Process
					{
						StartInfo = new ProcessStartInfo(job.FileName, job.Arguments)
						{
							UseShellExecute = false,
							CreateNoWindow = true,
							RedirectStandardOutput = true,
							RedirectStandardError = true
						}
					};
					try
					{
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
						Append("Exit code: " + process.ExitCode);
						if (!ContinueAfter(process.ExitCode))
						{
							outcome = ((process.ExitCode == 3010) ? "Windows reports a restart is required. Remaining steps were not run. Save work and restart manually." : "This step returned a nonzero exit code. Remaining steps were not run. Review the output.");
							break;
						}
					}
					finally
					{
						if (process != null)
						{
							((IDisposable)process).Dispose();
						}
					}
				}
				Append("\r\n" + outcome);
				Append("After addressing any reported issue, run the advisor scan again and test your original symptom.");
			}
			catch (Exception ex)
			{
				outcome = "Plan stopped: " + ex.Message;
				Append(outcome);
			}
		}
		finally
		{
			running = false;
			try
			{
				string text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Pulse", "RecoveryLogs");
				Directory.CreateDirectory(text);
				string text2 = Path.Combine(text, DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-plan-" + plan.Id + ".txt");
				File.WriteAllText(text2, transcript.ToString(), Encoding.UTF8);
				title.Text = outcome + "\r\nLog: " + text2;
			}
			catch (Exception ex2)
			{
				title.Text = outcome + "\r\nLog could not be saved: " + ex2.Message;
			}
		}
	}
}
