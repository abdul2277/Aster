using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Aster {

// v2.6 — the Tools hub as a calm tile grid: a quick-access row, category chips and compact tiles. Descriptions appear on hover.
sealed class ToolsHubView : UserControl {
    readonly IList<string[]> groups; readonly Func<string, string> hint; readonly ISet<string> pins; readonly Func<IList<string>> recent;
    readonly Action<string> open, togglePin; readonly Action<bool> simpleChanged; readonly Func<string, bool> exists;
    readonly Color tileBack, tileHover, fore, muted, accent; readonly ToolTip tip = new ToolTip { InitialDelay = 350, ReshowDelay = 100 };
    readonly TextBox find = new TextBox { Width = 280, Font = new Font("Segoe UI", 11), AccessibleName = "Search tools" };
    readonly CheckBox simpleBox = new CheckBox { Text = "Simple mode", AutoSize = true, Margin = new Padding(16, 7, 0, 0), AccessibleName = "Simple mode: hide advanced tools" };
    readonly FlowLayoutPanel chips = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = false };
    readonly FlowLayoutPanel quick = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
    readonly FlowLayoutPanel body = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
    string category = ""; bool simple;
    public int TileCount { get; private set; }
    public int SectionCount { get; private set; }

    public ToolsHubView(IList<string[]> groups, Func<string, string> hint, ISet<string> pins, Func<IList<string>> recent, bool simple, Func<string, bool> exists,
        Action<string> open, Action<string> togglePin, Action<bool> simpleChanged, Color tileBack, Color fore, Color muted, Color accent) {
        this.groups = groups; this.hint = hint; this.pins = pins; this.recent = recent; this.simple = simple; this.exists = exists; this.open = open; this.togglePin = togglePin; this.simpleChanged = simpleChanged;
        this.tileBack = tileBack; this.fore = fore; this.muted = muted; this.accent = accent; tileHover = ControlPaint.Light(tileBack, .25f);
        Dock = DockStyle.Fill; DoubleBuffered = true; ForeColor = fore;
        var top = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
        top.RowStyles.Add(new RowStyle(SizeType.Absolute, 42)); top.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); top.RowStyles.Add(new RowStyle(SizeType.Absolute, 38)); top.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var bar = new FlowLayoutPanel { Dock = DockStyle.Fill }; simpleBox.Checked = simple; simpleBox.ForeColor = fore;
        bar.Controls.Add(find); bar.Controls.Add(simpleBox); bar.Controls.Add(new Label { Text = "Hides specialist tools until you want them", AutoSize = true, ForeColor = muted, Margin = new Padding(6, 9, 0, 0) });
        top.Controls.Add(bar, 0, 0); top.Controls.Add(quick, 0, 1); top.Controls.Add(chips, 0, 2); top.Controls.Add(body, 0, 3); Controls.Add(top);
        find.TextChanged += (s, e) => Render(); simpleBox.CheckedChanged += (s, e) => { this.simple = simpleBox.Checked; if (this.simpleChanged != null) this.simpleChanged(this.simple); if (category != "" && !Visible) category = ""; Render(); };
        body.SizeChanged += (s, e) => { if (body.Width > 0) foreach (Control c in body.Controls) if (c is FlowLayoutPanel) c.Width = Math.Max(200, body.ClientSize.Width - 24); };
        Render();
    }
    public void SetSimple(bool value) { simple = value; simpleBox.Checked = value; }
    public void SearchFor(string text) { find.Text = text; }
    public void ChooseCategory(string name) { category = name ?? ""; Render(); }
    protected override void Dispose(bool disposing) { if (disposing) tip.Dispose(); base.Dispose(disposing); }

    Button Pill(string text, bool on, Action click) {
        var b = new Button { Text = text, AutoSize = true, FlatStyle = FlatStyle.Flat, Height = 28, Padding = new Padding(8, 0, 8, 0), Margin = new Padding(0, 2, 6, 2), Font = new Font("Segoe UI", 9), UseMnemonic = false,
            BackColor = on ? accent : tileBack, ForeColor = on ? Color.FromArgb(16, 19, 26) : fore, FlatAppearance = { BorderSize = 0 } };
        b.Click += (s, e) => click(); return b;
    }
    Control Tile(string name) {
        var t = new Panel { Width = 176, Height = 66, Margin = new Padding(0, 0, 10, 10), BackColor = tileBack, Cursor = Cursors.Hand, TabStop = true, AccessibleName = name, AccessibleRole = AccessibleRole.PushButton };
        var icon = new Label { Text = HubModel.Icon(name), Font = new Font("Segoe UI Emoji", 16), AutoSize = false, Width = 40, Height = 66, TextAlign = ContentAlignment.MiddleCenter, ForeColor = fore, Left = 0, Top = 0 };
        var label = new Label { Text = name, Font = new Font("Segoe UI", 10, FontStyle.Bold), AutoSize = false, Left = 40, Top = 0, Width = 106, Height = 66, TextAlign = ContentAlignment.MiddleLeft, ForeColor = fore, UseMnemonic = false, AutoEllipsis = true };
        var star = new Label { Text = pins.Contains(name) ? "★" : "☆", Font = new Font("Segoe UI Symbol", 12), AutoSize = false, Width = 28, Height = 28, Left = 146, Top = 2, TextAlign = ContentAlignment.MiddleCenter, ForeColor = pins.Contains(name) ? accent : muted, Cursor = Cursors.Hand };
        string h = hint(name) ?? ""; tip.SetToolTip(t, name + "\n" + h); tip.SetToolTip(label, name + "\n" + h); tip.SetToolTip(icon, name + "\n" + h); tip.SetToolTip(star, pins.Contains(name) ? "Unpin from the sidebar" : "Pin to the sidebar");
        t.Controls.AddRange(new Control[] { star, label, icon });
        EventHandler go = (s, e) => open(name); icon.Click += go; label.Click += go; t.Click += go;
        star.Click += (s, e) => { togglePin(name); Render(); };
        EventHandler enter = (s, e) => t.BackColor = tileHover; EventHandler leave = (s, e) => { if (!t.ClientRectangle.Contains(t.PointToClient(Cursor.Position))) t.BackColor = tileBack; };
        foreach (Control c in new Control[] { t, icon, label, star }) { c.MouseEnter += enter; c.MouseLeave += leave; }
        t.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space) { e.Handled = true; open(name); } };
        return t;
    }

    void Render() {
        SuspendLayout(); body.SuspendLayout();
        foreach (Control c in body.Controls.Cast<Control>().ToArray()) c.Dispose(); body.Controls.Clear(); quick.Controls.Clear(); chips.Controls.Clear();
        string q = find.Text.Trim(); var sections = HubModel.Sections(groups, q, category, simple, hint); TileCount = 0; SectionCount = sections.Count;
        // quick access (hidden while searching)
        quick.Visible = q.Length == 0;
        if (q.Length == 0) {
            var qa = HubModel.QuickAccess(pins, recent(), exists, simple, 8); quick.Controls.Add(new Label { Text = "QUICK ACCESS", AutoSize = true, ForeColor = muted, Font = new Font("Segoe UI", 8, FontStyle.Bold), Margin = new Padding(0, 4, 10, 0) });
            if (qa.Count == 0) quick.Controls.Add(new Label { Text = "Pin tools with ☆ and the ones you open appear here.", AutoSize = true, ForeColor = muted, Margin = new Padding(0, 4, 0, 0) });
            foreach (string n in qa) { string name = n; quick.Controls.Add(Pill(HubModel.Icon(name) + "  " + name, false, () => open(name))); }
        }
        chips.Controls.Add(Pill("All", category == "", () => { category = ""; Render(); }));
        foreach (var g in groups) { string cat = g[0]; if (simple && g.Skip(1).All(HubModel.IsAdvanced)) continue; chips.Controls.Add(Pill(Title(cat), string.Equals(category, cat, StringComparison.OrdinalIgnoreCase), () => { category = cat; Render(); })); }
        foreach (var sec in sections) {
            body.Controls.Add(new Label { Text = sec.Key, UseMnemonic = false, AutoSize = true, ForeColor = muted, Font = new Font("Segoe UI", 8, FontStyle.Bold), Margin = new Padding(2, 10, 0, 6) });
            var grid = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = true, Width = Math.Max(200, body.ClientSize.Width - 24), Margin = new Padding(0, 0, 0, 4) };
            foreach (string n in sec.Value) { grid.Controls.Add(Tile(n)); TileCount++; } body.Controls.Add(grid);
        }
        if (sections.Count == 0) body.Controls.Add(new Label { Text = q.Length > 0 ? "No tools match \"" + q + "\". Try fewer words." : "Nothing to show.", AutoSize = true, ForeColor = muted, Margin = new Padding(6, 18, 0, 0) });
        body.ResumeLayout(); ResumeLayout();
    }
    static string Title(string upper) { if (string.IsNullOrEmpty(upper)) return upper; var t = upper.ToLowerInvariant().Replace(" ai ", " AI ").Replace(" ai", " AI"); return char.ToUpperInvariant(t[0]) + t.Substring(1); }
}
}
