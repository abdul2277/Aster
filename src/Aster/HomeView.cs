using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace Aster {

// The Aster artwork that is already embedded in the program (resource "AsterArt"). Missing or unreadable art never breaks a screen.
static class ArtImage {
    public static Image Override; static Image cached;
    public static Image Get() {
        if (Override != null) return Override; if (cached != null) return cached;
        try { using (var st = typeof(ArtImage).Assembly.GetManifestResourceStream("AsterArt")) { if (st != null) { using (var img = Image.FromStream(st)) cached = new Bitmap(img); } } } catch { cached = null; }
        return cached;
    }
}

// v2.7 — a calm Home screen: three live meters, one sentence about the PC, six quick tools and your recent tools.
sealed class HomeView : UserControl {
    sealed class Meter : Control {
        public string Title = ""; public double Value; public string Text = "–"; public Color Fill = Color.Gray; public Color Card, Fore, Muted;
        public Meter() { DoubleBuffered = true; Width = 236; Height = 96; Margin = new Padding(0, 0, 12, 12); SetStyle(ControlStyles.ResizeRedraw, true); }
        protected override void OnPaint(PaintEventArgs e) {
            var g = e.Graphics; g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; g.Clear(Card);
            using (var f1 = new Font("Segoe UI", 9, FontStyle.Bold)) using (var f2 = new Font("Segoe UI", 20, FontStyle.Bold)) using (var m = new SolidBrush(Muted)) using (var f = new SolidBrush(Fore)) {
                g.DrawString(Title.ToUpperInvariant(), f1, m, 12, 8); g.DrawString(Text, f2, f, 10, 26);
                var track = new Rectangle(12, Height - 22, Width - 24, 8); using (var tb = new SolidBrush(Color.FromArgb(60, Muted))) g.FillRectangle(tb, track);
                int w = (int)Math.Round(track.Width * Math.Max(0, Math.Min(100, Value)) / 100.0); if (w > 0) using (var fb = new SolidBrush(Fill)) g.FillRectangle(fb, track.X, track.Y, w, track.Height);
            }
        }
    }
    readonly Color tileBack, tileHover, fore, muted, accent; readonly Func<IList<string>> recent; readonly Func<string, bool> exists; readonly Action<string> open;
    readonly Label greeting, verdict; readonly Meter cpu = new Meter { Title = "Processor" }, ram = new Meter { Title = "Memory" }, disk = new Meter { Title = "System drive free" };
    readonly FlowLayoutPanel quick = new FlowLayoutPanel { AutoSize = true, WrapContents = true, Margin = new Padding(0) }, recentRow = new FlowLayoutPanel { AutoSize = true, WrapContents = true, Margin = new Padding(0) };
    public HomeLevel LastLevel { get; private set; } public int QuickCount { get { return quick.Controls.Count; } } public int RecentCount { get { return recentRow.Controls.Count; } }
    public string VerdictText { get { return verdict.Text; } } public PictureBox Picture;

    readonly Func<DateTime> clock; readonly string who; readonly Panel nowCard = new Panel { Height = 70, Width = 540, Margin = new Padding(0, 0, 0, 4), Visible = false }; readonly Label nowIcon = new Label(), nowText = new Label(); readonly Button nowGo = new Button();
    int shownHour = -1; string nowTool = ""; bool busyNow;
    public string GreetingText { get { return greeting.Text; } } public string SuggestionText { get { return nowText.Text; } } public string SuggestionTool { get { return nowTool; } } public bool SuggestionVisible { get { return nowCard.Visible; } }
    public HomeView(Color tileBack, Color fore, Color muted, Color accent, string name, Func<IList<string>> recent, Func<string, bool> exists, Action<string> open, Control extra, Func<DateTime> clock = null) {
        this.clock = clock ?? (() => DateTime.Now);
        this.tileBack = tileBack; this.fore = fore; this.muted = muted; this.accent = accent; this.recent = recent; this.exists = exists; this.open = open; tileHover = ControlPaint.Light(tileBack, .25f);
        Dock = DockStyle.Fill; AutoScroll = true; ForeColor = fore; DoubleBuffered = true;
        var col = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, Dock = DockStyle.Top, Padding = new Padding(4, 4, 4, 16) };
        who = string.IsNullOrWhiteSpace(name) ? "" : ", " + name.Trim();
        greeting = new Label { Text = HomeModel.Emoji(this.clock().Hour) + "  " + HomeModel.Greeting(this.clock().Hour) + who, AutoSize = true, Font = new Font("Segoe UI", 22, FontStyle.Bold), ForeColor = fore, UseMnemonic = false };
        verdict = new Label { Text = "Reading your PC…", AutoSize = true, Font = new Font("Segoe UI", 12), ForeColor = muted, Margin = new Padding(3, 0, 0, 14) };
        foreach (var m in new[] { cpu, ram, disk }) { m.Card = tileBack; m.Fore = fore; m.Muted = muted; m.Fill = Color.FromArgb(76, 217, 100); }
        var meters = new FlowLayoutPanel { AutoSize = true, WrapContents = true, Margin = new Padding(0) }; meters.Controls.AddRange(new Control[] { cpu, ram, disk });
        var head = new FlowLayoutPanel { AutoSize = true, WrapContents = false, FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(0) };
        var art = ArtImage.Get(); if (art != null) { Picture = new PictureBox { Width = 84, Height = 84, SizeMode = PictureBoxSizeMode.Zoom, Image = art, Margin = new Padding(0, 0, 14, 8), BackColor = Color.Transparent, AccessibleName = "Aster logo", TabStop = false }; head.Controls.Add(Picture); }
        var words = new FlowLayoutPanel { AutoSize = true, WrapContents = false, FlowDirection = FlowDirection.TopDown, Margin = new Padding(0, 6, 0, 0) }; words.Controls.Add(greeting); words.Controls.Add(verdict); head.Controls.Add(words);
        col.Controls.Add(head); col.Controls.Add(meters);
        nowCard.BackColor = tileBack; nowIcon.SetBounds(0, 0, 54, 70); nowIcon.Font = new Font("Segoe UI Emoji", 20); nowIcon.TextAlign = ContentAlignment.MiddleCenter; nowIcon.ForeColor = fore;
        nowText.SetBounds(54, 0, 330, 70); nowText.Font = new Font("Segoe UI", 10.5f); nowText.TextAlign = ContentAlignment.MiddleLeft; nowText.ForeColor = fore; nowText.UseMnemonic = false;
        nowGo.SetBounds(392, 19, 136, 32); nowGo.FlatStyle = FlatStyle.Flat; nowGo.FlatAppearance.BorderSize = 0; nowGo.BackColor = accent; nowGo.ForeColor = Color.FromArgb(16, 19, 26); nowGo.UseMnemonic = false; nowGo.AccessibleName = "Open the suggested tool";
        nowGo.Click += (s, e) => { if (nowTool.Length > 0) this.open(nowTool); }; nowCard.Controls.AddRange(new Control[] { nowIcon, nowText, nowGo });
        col.Controls.Add(Header("RIGHT NOW")); col.Controls.Add(nowCard);
        col.Controls.Add(Header("QUICK TOOLS")); col.Controls.Add(quick); col.Controls.Add(Header("RECENT")); col.Controls.Add(recentRow);
        if (extra != null) { extra.Margin = new Padding(0, 18, 0, 0); col.Controls.Add(extra); }
        Controls.Add(col); BuildTiles(); RefreshTime(true);
    }
    Label Header(string t) { return new Label { Text = t, AutoSize = true, ForeColor = muted, Font = new Font("Segoe UI", 8, FontStyle.Bold), Margin = new Padding(2, 12, 0, 6), UseMnemonic = false }; }
    Control Tile(string name) {
        var t = new Panel { Width = 176, Height = 62, Margin = new Padding(0, 0, 10, 10), BackColor = tileBack, Cursor = Cursors.Hand, TabStop = true, AccessibleName = name, AccessibleRole = AccessibleRole.PushButton };
        var icon = new Label { Text = HubModel.Icon(name), Font = new Font("Segoe UI Emoji", 15), AutoSize = false, Width = 40, Height = 62, TextAlign = ContentAlignment.MiddleCenter, ForeColor = fore };
        var label = new Label { Text = name, Font = new Font("Segoe UI", 10, FontStyle.Bold), AutoSize = false, Left = 40, Width = 130, Height = 62, TextAlign = ContentAlignment.MiddleLeft, ForeColor = fore, UseMnemonic = false, AutoEllipsis = true };
        t.Controls.AddRange(new Control[] { label, icon }); EventHandler go = (s, e) => open(name); icon.Click += go; label.Click += go; t.Click += go;
        EventHandler enter = (s, e) => t.BackColor = tileHover; EventHandler leave = (s, e) => { if (!t.ClientRectangle.Contains(t.PointToClient(Cursor.Position))) t.BackColor = tileBack; };
        foreach (Control c in new Control[] { t, icon, label }) { c.MouseEnter += enter; c.MouseLeave += leave; }
        t.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space) { e.Handled = true; open(name); } };
        return t;
    }
    public void BuildTiles() {
        foreach (Control c in quick.Controls.Cast<Control>().ToArray()) c.Dispose(); quick.Controls.Clear(); foreach (Control c in recentRow.Controls.Cast<Control>().ToArray()) c.Dispose(); recentRow.Controls.Clear();
        foreach (string n in HomeModel.QuickTools) if (exists(n)) quick.Controls.Add(Tile(n));
        var r = HubModel.QuickAccess(new string[0], recent(), exists, false, 6).Where(n => !HomeModel.QuickTools.Contains(n)).ToList();
        foreach (string n in r) recentRow.Controls.Add(Tile(n));
        if (r.Count == 0) recentRow.Controls.Add(new Label { Text = "Tools you open will show up here.", AutoSize = true, ForeColor = muted, Margin = new Padding(2, 4, 0, 0) });
    }
    static Color ForLevel(double pct, double busy, double bad, bool invert) { double v = invert ? 100 - pct : pct; return v >= bad ? Color.FromArgb(255, 107, 107) : v >= busy ? Color.FromArgb(255, 179, 71) : Color.FromArgb(76, 217, 100); }
    // cpu / ram in percent; freeDiskPct < 0 means unknown.
    // Re-reads the clock. Cheap enough to call every second: it only changes anything when the hour changes or the PC becomes busy / calm.
    public void RefreshTime(bool force) {
        var now = clock(); bool busy = busyNow; if (!force && now.Hour == shownHour && busy == lastBusyShown) return;
        shownHour = now.Hour; lastBusyShown = busy; greeting.Text = HomeModel.Emoji(now.Hour) + "  " + HomeModel.Greeting(now.Hour) + who;
        var sg = HomeModel.Suggestion(now.Hour, now.DayOfYear, busy); nowTool = exists(sg.Value) ? sg.Value : ""; nowCard.Visible = nowTool.Length > 0;
        nowIcon.Text = HubModel.Icon(nowTool); nowText.Text = sg.Key; nowGo.Text = "Open " + nowTool;
    }
    bool lastBusyShown;
    public void SetReadings(double cpuPct, double ramPct, double freeDiskPct) {
        busyNow = cpuPct >= 70 || ramPct >= 80;
        cpu.Value = cpuPct; cpu.Text = cpuPct.ToString("F0", CultureInfo.InvariantCulture) + "%"; cpu.Fill = ForLevel(cpuPct, 70, 90, false);
        ram.Value = ramPct; ram.Text = ramPct.ToString("F0", CultureInfo.InvariantCulture) + "%"; ram.Fill = ForLevel(ramPct, 80, 92, false);
        disk.Value = Math.Max(0, freeDiskPct); disk.Text = freeDiskPct < 0 ? "unknown" : freeDiskPct.ToString("F0", CultureInfo.InvariantCulture) + "%"; disk.Fill = freeDiskPct < 0 ? Color.Gray : ForLevel(freeDiskPct, 85, 92, true);
        LastLevel = HomeModel.Level(cpuPct, ramPct, freeDiskPct); verdict.Text = HomeModel.Verdict(cpuPct, ramPct, freeDiskPct);
        verdict.ForeColor = LastLevel == HomeLevel.Good ? muted : LastLevel == HomeLevel.Busy ? Color.FromArgb(255, 179, 71) : Color.FromArgb(255, 107, 107);
        RefreshTime(false); cpu.Invalidate(); ram.Invalidate(); disk.Invalidate();
    }
}

// A short, skippable welcome shown once.
sealed class WelcomeTour : Form {
    static readonly string[][] Steps = {
        new[] { "👋", "Welcome to Aster", "A calm toolbox for your PC. This takes 20 seconds, and you can skip it any time." },
        new[] { "🏠", "Home shows how your PC is doing", "Three live meters and one plain sentence. The six quick tools underneath are the things people reach for most." },
        new[] { "🧰", "Tools hub keeps everything tidy", "Every tool lives in the hub as a tile. Click the ☆ to pin favourites to the sidebar, and turn on Simple mode to hide the specialist ones." },
        new[] { "⌨", "Ctrl+K finds anything", "Press Ctrl+K and type a word like \"disk\" or \"ping\". It jumps straight to the right tool." },
        new[] { "📤", "Share what you find", "Space finder, Ping monitor and others can save or copy a report, so it is easy to send to someone who can help." }
    };
    PictureBox pic; int index; readonly Label icon = new Label(), title = new Label(), body = new Label(), count = new Label(); readonly Button next = new Button(), back = new Button(), skip = new Button();
    public bool ShowsPicture { get { return pic != null && pic.Visible; } } public int StepCount { get { return Steps.Length; } } public int Index { get { return index; } } public bool Finished { get; private set; }
    public WelcomeTour(Color back0, Color fore, Color muted, Color accent) {
        Text = "Welcome"; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; StartPosition = FormStartPosition.CenterParent; ClientSize = new Size(520, 300); BackColor = back0; ForeColor = fore; ShowInTaskbar = false;
        icon.SetBounds(0, 22, 520, 70); icon.Font = new Font("Segoe UI Emoji", 36); icon.TextAlign = ContentAlignment.MiddleCenter; icon.ForeColor = fore;
        title.SetBounds(20, 100, 480, 36); title.Font = new Font("Segoe UI", 16, FontStyle.Bold); title.TextAlign = ContentAlignment.MiddleCenter; title.UseMnemonic = false;
        body.SetBounds(40, 140, 440, 80); body.Font = new Font("Segoe UI", 11); body.TextAlign = ContentAlignment.TopCenter; body.ForeColor = muted; body.UseMnemonic = false;
        count.SetBounds(0, 228, 520, 18); count.TextAlign = ContentAlignment.MiddleCenter; count.ForeColor = muted; count.Font = new Font("Segoe UI", 9);
        foreach (var b in new[] { next, back, skip }) { b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderSize = 0; b.Height = 32; b.Width = 96; b.Top = 256; b.UseMnemonic = false; b.BackColor = Color.FromArgb(32, 44, 62); b.ForeColor = fore; }
        next.BackColor = accent; next.ForeColor = Color.FromArgb(16, 19, 26); next.Left = 404; back.Text = "Back"; back.Left = 300; skip.Text = "Skip"; skip.Left = 20;
        next.Click += (s, e) => { if (index >= Steps.Length - 1) { Finished = true; Close(); } else { index++; Show1(); } }; back.Click += (s, e) => { if (index > 0) { index--; Show1(); } }; skip.Click += (s, e) => Close();
        var art = ArtImage.Get(); if (art != null) { pic = new PictureBox { Image = art, SizeMode = PictureBoxSizeMode.Zoom, Left = 220, Top = 10, Width = 80, Height = 80, BackColor = Color.Transparent, TabStop = false, AccessibleName = "Aster logo" }; }
        AcceptButton = next; CancelButton = skip; Controls.AddRange(new Control[] { icon, title, body, count, back, next, skip }); if (pic != null) { Controls.Add(pic); pic.BringToFront(); } Show1();
    }
    void Show1() { var s = Steps[index]; icon.Text = s[0]; icon.Visible = !(pic != null && index == 0); if (pic != null) pic.Visible = index == 0; title.Text = s[1]; body.Text = s[2]; count.Text = (index + 1) + " of " + Steps.Length; back.Visible = index > 0; next.Text = index == Steps.Length - 1 ? "Get started" : "Next"; skip.Visible = index < Steps.Length - 1; }
    public void Advance() { next.PerformClick(); } public void GoBack() { back.PerformClick(); } public void SkipAll() { skip.PerformClick(); }
}
}
