using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Aster {

// v1.5 pages: Space finder, Ping monitor, Power timer.
partial class MainWindow {
    public static readonly string[] NextPages = { "Space finder", "Ping monitor", "Power timer" };
    static bool IsNextPage(string name) { return Array.IndexOf(NextPages, name) >= 0; }

    bool BuildNextPage(string name) {
        if (name == "Space finder") { BuildSpaceFinder(); return true; }
        if (name == "Ping monitor") { BuildPingMonitor(); return true; }
        if (name == "Power timer") { BuildPowerTimer(); return true; }
        return false;
    }

    TableLayoutPanel NextLayout(params float[] rows) {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = rows.Length };
        foreach (float r in rows) layout.RowStyles.Add(r > 0 ? new RowStyle(SizeType.Absolute, r) : new RowStyle(SizeType.Percent, 100));
        content.Controls.Add(layout);
        return layout;
    }

    static ListView NextList(params string[] columns) {
        var list = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, HideSelection = false, Font = new Font("Segoe UI", 10) };
        foreach (string c in columns) list.Columns.Add(c, c == "Path" || c == "Folder" ? 520 : 120);
        return list;
    }

    // ---------------- Space finder ----------------
    CancellationTokenSource spaceScan;

    void BuildSpaceFinder() {
        sub.Text = "Find what fills your disk · read-only · nothing is deleted";
        var layout = NextLayout(50, 34, 0);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill };
        var folder = new TextBox { Width = 360, Font = new Font("Segoe UI", 11), AccessibleName = "Folder to scan",
            Text = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) };
        var browse = Button("Browse…"); var scan = Button("Scan"); var stop = Button("Stop"); var open = Button("Show in Explorer"); var export = Button("Save report");
        stop.Enabled = open.Enabled = export.Enabled = false;
        actions.Controls.AddRange(new Control[] { folder, browse, scan, stop, open, export });
        var status = new Label { Dock = DockStyle.Fill, Text = "Choose a folder or drive and select Scan.", TextAlign = ContentAlignment.MiddleLeft };
        var tabs = new TabControl { Dock = DockStyle.Fill };
        var files = NextList("Size", "Share", "Modified", "Path");
        var folders = NextList("Size", "Share", "Files", "Folder");
        var types = NextList("Size", "Share", "Type");
        foreach (var pair in new[] { Tuple.Create("Largest files", files), Tuple.Create("Folders", folders), Tuple.Create("File types", types) }) {
            var tab = new TabPage(pair.Item1); tab.Controls.Add(pair.Item2); tabs.TabPages.Add(tab);
        }
        layout.Controls.Add(actions, 0, 0); layout.Controls.Add(status, 0, 1); layout.Controls.Add(tabs, 0, 2);
        SpaceReport last = null;

        browse.Click += (s, e) => {
            using (var dialog = new FolderBrowserDialog { Description = "Choose a folder or drive to scan", SelectedPath = folder.Text })
                if (dialog.ShowDialog(this) == DialogResult.OK) folder.Text = dialog.SelectedPath;
        };
        stop.Click += (s, e) => { if (spaceScan != null) spaceScan.Cancel(); };
        Func<string> selected = () => {
            var list = tabs.SelectedIndex == 0 ? files : tabs.SelectedIndex == 1 ? folders : null;
            return list != null && list.SelectedItems.Count > 0 ? list.SelectedItems[0].Tag as string : null;
        };
        open.Click += (s, e) => {
            string path = selected() ?? (last != null ? last.Root : null);
            if (path == null) return;
            try { Process.Start("explorer.exe", (File.Exists(path) ? "/select,\"" : "\"") + path + "\""); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Space finder"); }
        };
        export.Click += (s, e) => {
            if (last == null) return;
            using (var dialog = new SaveFileDialog { Filter = "Text report|*.txt", FileName = "Aster-space-report.txt" })
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    try { File.WriteAllText(dialog.FileName, SpaceText(last), Encoding.UTF8); }
                    catch (Exception ex) { MessageBox.Show(this, ex.Message, "Space finder"); }
        };
        scan.Click += async (s, e) => {
            string root = folder.Text.Trim();
            if (!Directory.Exists(root)) { status.Text = "That folder does not exist."; return; }
            if (spaceScan != null) spaceScan.Cancel();
            var cts = new CancellationTokenSource(); spaceScan = cts;
            scan.Enabled = open.Enabled = export.Enabled = false; stop.Enabled = true;
            files.Items.Clear(); folders.Items.Clear(); types.Items.Clear();
            status.Text = "Scanning " + root + " …";
            var started = DateTime.Now;
            try {
                var report = await Task.Run(() => SpaceScanner.Scan(root, cts.Token, n => {
                    try { BeginInvoke((Action)(() => { if (!status.IsDisposed) status.Text = "Scanning… " + n.ToString("N0") + " files so far"; })); } catch { }
                }));
                if (status.IsDisposed) return;
                last = report;
                FillSpace(report, files, folders, types);
                status.Text = (report.Canceled ? "Stopped early · partial results · " : "Done · ") + SpaceScanner.Bytes(report.TotalBytes) + " in "
                    + report.Files.ToString("N0") + " files, " + report.Folders.ToString("N0") + " folders"
                    + (report.Skipped > 0 ? " · " + report.Skipped + " items skipped (no access)" : "")
                    + " · " + (DateTime.Now - started).TotalSeconds.ToString("F1") + " s";
                open.Enabled = export.Enabled = true;
                AddSessionEvent("Space finder scanned " + root);
            } catch (Exception ex) {
                if (!status.IsDisposed) status.Text = "Scan failed: " + ex.Message;
            } finally {
                if (spaceScan == cts) spaceScan = null;
                if (!scan.IsDisposed) { scan.Enabled = true; stop.Enabled = false; }
            }
        };
        layout.Disposed += (s, e) => { if (spaceScan != null) spaceScan.Cancel(); };
        ApplyAppTheme();
    }

    static void FillSpace(SpaceReport r, ListView files, ListView folders, ListView types) {
        files.BeginUpdate(); folders.BeginUpdate(); types.BeginUpdate();
        foreach (var f in r.LargestFiles)
            files.Items.Add(new ListViewItem(new[] { SpaceScanner.Bytes(f.Bytes), SpaceScanner.Share(f.Bytes, r.TotalBytes), f.Modified.ToString("yyyy-MM-dd"), f.Path }) { Tag = f.Path });
        foreach (var f in r.TopFolders)
            folders.Items.Add(new ListViewItem(new[] { SpaceScanner.Bytes(f.Bytes), SpaceScanner.Share(f.Bytes, r.TotalBytes), f.Files.ToString("N0"), f.Path }) { Tag = f.Path });
        foreach (var t in r.ByExtension.OrderByDescending(x => x.Value).Take(40))
            types.Items.Add(new ListViewItem(new[] { SpaceScanner.Bytes(t.Value), SpaceScanner.Share(t.Value, r.TotalBytes), t.Key }));
        files.EndUpdate(); folders.EndUpdate(); types.EndUpdate();
    }

    static string SpaceText(SpaceReport r) {
        var b = new StringBuilder("ASTER SPACE REPORT\r\nFolder: " + r.Root + "\r\nCreated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + "\r\n");
        b.AppendLine("Total: " + SpaceScanner.Bytes(r.TotalBytes) + " in " + r.Files + " files" + (r.Canceled ? " (partial scan)" : ""));
        b.AppendLine("\r\nFOLDERS"); foreach (var f in r.TopFolders.Take(30)) b.AppendLine(SpaceScanner.Bytes(f.Bytes).PadLeft(10) + "  " + f.Path);
        b.AppendLine("\r\nLARGEST FILES"); foreach (var f in r.LargestFiles.Take(50)) b.AppendLine(SpaceScanner.Bytes(f.Bytes).PadLeft(10) + "  " + f.Path);
        b.AppendLine("\r\nReview carefully before deleting anything. Aster made no changes.");
        return b.ToString();
    }

    // ---------------- Ping monitor ----------------
    void BuildPingMonitor() {
        sub.Text = "Live latency, jitter and packet loss · one small ping per second";
        var layout = NextLayout(50, 84, 0);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill };
        var host = new ComboBox { Width = 230, Font = new Font("Segoe UI", 11), AccessibleName = "Host to ping" };
        host.Items.AddRange(new object[] { "1.1.1.1", "8.8.8.8", "9.9.9.9", "bing.com", "github.com" });
        string gateway = DefaultGateway(); if (gateway != null) host.Items.Insert(0, gateway + "  (router)");
        host.SelectedIndex = 0;
        var start = Button("Start"); var stop = Button("Stop"); var reset = Button("Reset"); stop.Enabled = false;
        actions.Controls.AddRange(new Control[] { new Label { Text = "Host", AutoSize = true, Margin = new Padding(0, 8, 8, 0) }, host, start, stop, reset });
        var readout = new Label { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12), Text = "Select Start to measure your connection." };
        var chart = new PingChart { Dock = DockStyle.Fill };
        layout.Controls.Add(actions, 0, 0); layout.Controls.Add(readout, 0, 1); layout.Controls.Add(chart, 0, 2);
        var stats = new PingStats(90); chart.Stats = stats;
        var timer = new System.Windows.Forms.Timer { Interval = 1000 };
        var ping = new Ping(); bool busy = false;
        Action render = () => {
            readout.Text = (stats.Last.HasValue ? "Now " + stats.Last + " ms" : stats.Sent > 0 ? "Now: timed out" : "Now: —")
                + "   ·   avg " + stats.Average.ToString("F0") + " ms   ·   min " + stats.Min + " / max " + stats.Max
                + "   ·   jitter " + stats.Jitter.ToString("F1") + " ms   ·   loss " + stats.LossPercent.ToString("F1") + "% (" + stats.Sent + " sent)\r\n" + stats.Verdict;
            chart.Invalidate();
        };
        timer.Tick += async (s, e) => {
            if (busy) return; busy = true;
            string target = (host.Text ?? "").Split(' ')[0].Trim();
            long? ms = null;
            try { var reply = await ping.SendPingAsync(target, 2000); if (reply.Status == IPStatus.Success) ms = reply.RoundtripTime; }
            catch (Exception ex) { if (!readout.IsDisposed && stats.Sent == 0) readout.Text = "Cannot ping: " + (ex.InnerException ?? ex).Message; }
            finally { busy = false; }
            if (readout.IsDisposed || !timer.Enabled) return;
            stats.Add(ms); render();
        };
        start.Click += (s, e) => { if (string.IsNullOrWhiteSpace(host.Text)) return; timer.Start(); start.Enabled = false; stop.Enabled = true; host.Enabled = false; AddSessionEvent("Ping monitor started: " + host.Text); };
        stop.Click += (s, e) => { timer.Stop(); start.Enabled = true; stop.Enabled = false; host.Enabled = true; };
        reset.Click += (s, e) => { stats.Reset(); readout.Text = "Statistics cleared."; chart.Invalidate(); };
        layout.Disposed += (s, e) => { timer.Stop(); timer.Dispose(); ping.Dispose(); };
        ApplyAppTheme();
    }

    static string DefaultGateway() {
        try {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces()) {
                if (nic.OperationalStatus != OperationalStatus.Up || nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                foreach (var g in nic.GetIPProperties().GatewayAddresses)
                    if (g.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !g.Address.ToString().StartsWith("0.")) return g.Address.ToString();
            }
        } catch { }
        return null;
    }

    sealed class PingChart : Control {
        public PingStats Stats;
        public PingChart() { DoubleBuffered = true; ResizeRedraw = true; }
        protected override void OnPaint(PaintEventArgs e) {
            var g = e.Graphics; g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(BackColor);
            var area = new Rectangle(46, 12, Math.Max(10, Width - 60), Math.Max(10, Height - 40));
            using (var grid = new Pen(Color.FromArgb(70, ForeColor))) using (var text = new SolidBrush(Color.FromArgb(170, ForeColor))) {
                long top = Math.Max(40, Stats == null ? 40 : (long)(Math.Ceiling(Stats.Max / 40.0) * 40));
                for (int i = 0; i <= 4; i++) {
                    int y = area.Bottom - area.Height * i / 4;
                    g.DrawLine(grid, area.Left, y, area.Right, y);
                    g.DrawString((top * i / 4) + " ms", Font, text, 2, y - 8);
                }
                if (Stats == null || Stats.Recent.Count < 2) { g.DrawString("Waiting for replies…", Font, text, area.Left + 8, area.Top + 8); return; }
                var pts = new List<PointF>(); var values = Stats.Recent; int n = Stats.Window;
                for (int i = 0; i < values.Count; i++)
                    pts.Add(new PointF(area.Right - (values.Count - 1 - i) * (float)area.Width / (n - 1), area.Bottom - area.Height * Math.Min(1f, values[i] / (float)top)));
                using (var line = new Pen(Color.FromArgb(94, 196, 214), 2.2f)) g.DrawLines(line, pts.ToArray());
            }
        }
    }

    // ---------------- Power timer ----------------
    DateTime? powerDeadline; PowerAction powerAction;

    void BuildPowerTimer() {
        sub.Text = "Schedule shutdown or restart · cancel any time · uses Windows' own shutdown tool";
        var layout = NextLayout(52, 52, 120, 0);
        var row1 = new FlowLayoutPanel { Dock = DockStyle.Fill };
        var action = new ComboBox { Width = 130, DropDownStyle = ComboBoxStyle.DropDownList, AccessibleName = "Power action" };
        action.Items.AddRange(new object[] { "Shut down", "Restart" }); action.SelectedIndex = 0;
        var minutes = new NumericUpDown { Width = 80, Minimum = 1, Maximum = 1440, Value = 30, AccessibleName = "Minutes" };
        var inMinutes = Button("Schedule in minutes");
        row1.Controls.AddRange(new Control[] { action, new Label { Text = "after", AutoSize = true, Margin = new Padding(4, 8, 4, 0) }, minutes, new Label { Text = "min", AutoSize = true, Margin = new Padding(4, 8, 10, 0) }, inMinutes });
        foreach (int quick in new[] { 15, 60, 120 }) { int q = quick; var b = Button(q < 60 ? q + " min" : q / 60 + " h"); b.Click += (s, e) => Schedule(action.SelectedIndex == 1 ? PowerAction.Restart : PowerAction.Shutdown, DateTime.Now.AddMinutes(q)); row1.Controls.Add(b); }
        var row2 = new FlowLayoutPanel { Dock = DockStyle.Fill };
        var clock = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "HH:mm", ShowUpDown = true, Width = 90, Value = DateTime.Now.AddHours(1), AccessibleName = "Clock time" };
        var atTime = Button("Schedule at this time"); var cancel = Button("Cancel scheduled action"); var lockNow = Button("Lock now"); var sleepNow = Button("Sleep now");
        row2.Controls.AddRange(new Control[] { new Label { Text = "or at", AutoSize = true, Margin = new Padding(0, 8, 6, 0) }, clock, atTime, cancel, lockNow, sleepNow });
        var countdown = new Label { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 30, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
        var note = new Label { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10), Text = "Save your work before the deadline — Windows closes apps when it shuts down. Windows also shows its own notice, and \"shutdown /a\" in any terminal cancels the plan even if Aster is closed." };
        layout.Controls.Add(row1, 0, 0); layout.Controls.Add(row2, 0, 1); layout.Controls.Add(countdown, 0, 2); layout.Controls.Add(note, 0, 3);
        var tick = new System.Windows.Forms.Timer { Interval = 500 };
        Action refresh = () => {
            if (powerDeadline.HasValue && powerDeadline.Value <= DateTime.Now.AddSeconds(-5)) powerDeadline = null;
            countdown.Text = powerDeadline.HasValue
                ? (powerAction == PowerAction.Restart ? "Restart in " : "Shutdown in ") + PowerPlan.Countdown(powerDeadline.Value - DateTime.Now)
                : "Nothing scheduled";
            cancel.Enabled = powerDeadline.HasValue;
        };
        tick.Tick += (s, e) => refresh();
        inMinutes.Click += (s, e) => { Schedule(action.SelectedIndex == 1 ? PowerAction.Restart : PowerAction.Shutdown, DateTime.Now.AddMinutes((double)minutes.Value)); refresh(); };
        atTime.Click += (s, e) => { Schedule(action.SelectedIndex == 1 ? PowerAction.Restart : PowerAction.Shutdown, PowerPlan.NextAt(clock.Value.Hour, clock.Value.Minute, DateTime.Now)); refresh(); };
        cancel.Click += (s, e) => { if (RunShutdown(PowerPlan.AbortArguments)) { powerDeadline = null; AddSessionEvent("Power timer canceled"); } refresh(); };
        lockNow.Click += (s, e) => { try { Process.Start(Path.Combine(Environment.SystemDirectory, "rundll32.exe"), "user32.dll,LockWorkStation"); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Power timer"); } };
        sleepNow.Click += (s, e) => {
            if (MessageBox.Show(this, "Put this PC to sleep now?", "Power timer", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                Application.SetSuspendState(PowerState.Suspend, false, false);
        };
        layout.Disposed += (s, e) => { tick.Stop(); tick.Dispose(); };
        refresh(); tick.Start();
        ApplyAppTheme();
    }

    void Schedule(PowerAction what, DateTime when) {
        int seconds = PowerPlan.SecondsUntil(when, DateTime.Now);
        string verb = what == PowerAction.Restart ? "restart" : "shut down";
        if (MessageBox.Show(this, "Windows will " + verb + " at " + when.ToString("HH:mm") + " (" + PowerPlan.Countdown(TimeSpan.FromSeconds(seconds)) + " from now).\r\n\r\nContinue?",
            "Power timer", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK) return;
        if (powerDeadline.HasValue) RunShutdown(PowerPlan.AbortArguments);
        if (RunShutdown(PowerPlan.Arguments(what, seconds))) {
            powerDeadline = DateTime.Now.AddSeconds(seconds); powerAction = what;
            AddSessionEvent("Power timer: " + verb + " at " + when.ToString("HH:mm"));
        }
    }

    bool RunShutdown(string arguments) {
        try {
            var info = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "shutdown.exe"), arguments) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true };
            using (var p = Process.Start(info)) {
                string error = p.StandardError.ReadToEnd(); p.WaitForExit(10000);
                if (p.ExitCode == 0 || (arguments == PowerPlan.AbortArguments && p.ExitCode == 1116)) return true; // 1116 = nothing to abort
                MessageBox.Show(this, "Windows refused the request (code " + p.ExitCode + "). " + error.Trim(), "Power timer");
            }
        } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Power timer"); }
        return false;
    }
}
}
