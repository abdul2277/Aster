using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace Aster {

// v1.5 — UI-free logic for Space finder, Ping monitor and Power timer.
// Kept separate from the WinForms pages so it can be tested on its own.

public sealed class SpaceItem {
    public string Path;
    public long Bytes;
    public int Files;
    public DateTime Modified;
}

public sealed class SpaceReport {
    public string Root;
    public long TotalBytes;
    public int Files;
    public int Folders;
    public int Skipped;
    public bool Canceled;
    public List<SpaceItem> LargestFiles = new List<SpaceItem>();
    public List<SpaceItem> TopFolders = new List<SpaceItem>();
    public Dictionary<string, long> ByExtension = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
}

public static class SpaceScanner {
    public const int MaxLargest = 100;

    // Walks a folder tree without following reparse points (junctions/symlinks),
    // so it never loops or double counts. Unreadable folders are counted as skipped.
    public static SpaceReport Scan(string root, CancellationToken token, Action<int> progress = null) {
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            throw new DirectoryNotFoundException("Folder not found: " + root);
        var report = new SpaceReport { Root = Path.GetFullPath(root) };
        var largest = new List<SpaceItem>();
        var topFolders = new Dictionary<string, SpaceItem>(StringComparer.OrdinalIgnoreCase);
        var pending = new Stack<KeyValuePair<string, string>>(); // folder, top-level bucket
        pending.Push(new KeyValuePair<string, string>(report.Root, null));
        while (pending.Count > 0) {
            if (token.IsCancellationRequested) { report.Canceled = true; break; }
            var next = pending.Pop();
            string folder = next.Key, bucket = next.Value;
            string[] files, folders;
            try {
                files = Directory.GetFiles(folder);
                folders = Directory.GetDirectories(folder);
            } catch (Exception) { report.Skipped++; continue; }
            report.Folders++;
            foreach (string file in files) {
                FileInfo info;
                try { info = new FileInfo(file); if ((info.Attributes & FileAttributes.ReparsePoint) != 0) continue; }
                catch (Exception) { report.Skipped++; continue; }
                long size = info.Length;
                report.TotalBytes += size;
                report.Files++;
                string ext = info.Extension.Length == 0 ? "(none)" : info.Extension.ToLowerInvariant();
                long existing; report.ByExtension.TryGetValue(ext, out existing); report.ByExtension[ext] = existing + size;
                if (bucket != null) { var t = topFolders[bucket]; t.Bytes += size; t.Files++; }
                Offer(largest, new SpaceItem { Path = info.FullName, Bytes = size, Files = 1, Modified = info.LastWriteTime });
                if (progress != null && report.Files % 500 == 0) progress(report.Files);
            }
            foreach (string child in folders) {
                try { if ((File.GetAttributes(child) & FileAttributes.ReparsePoint) != 0) continue; }
                catch (Exception) { report.Skipped++; continue; }
                string childBucket = bucket;
                if (childBucket == null) {
                    childBucket = child;
                    topFolders[child] = new SpaceItem { Path = child, Modified = SafeTime(child) };
                }
                pending.Push(new KeyValuePair<string, string>(child, childBucket));
            }
        }
        report.LargestFiles = largest.OrderByDescending(x => x.Bytes).ToList();
        report.TopFolders = topFolders.Values.OrderByDescending(x => x.Bytes).ToList();
        return report;
    }

    static DateTime SafeTime(string path) { try { return Directory.GetLastWriteTime(path); } catch { return DateTime.MinValue; } }

    // Keeps only the MaxLargest biggest files without sorting everything.
    static void Offer(List<SpaceItem> list, SpaceItem item) {
        if (list.Count < MaxLargest) { list.Add(item); return; }
        int smallest = 0;
        for (int i = 1; i < list.Count; i++) if (list[i].Bytes < list[smallest].Bytes) smallest = i;
        if (item.Bytes > list[smallest].Bytes) list[smallest] = item;
    }

    // CSV for spreadsheets. Cells starting with = + - @ (or a tab/CR) are prefixed with an apostrophe so a file named "=cmd|..." cannot run as a formula.
    public static string CsvCell(string v) {
        v = v ?? "";
        if (v.Length > 0 && "=+-@\t\r".IndexOf(v[0]) >= 0) v = "'" + v;
        return (v.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0) ? "\"" + v.Replace("\"", "\"\"") + "\"" : v;
    }
    public static string ToCsv(SpaceReport r) {
        var b = new System.Text.StringBuilder("Kind,Size (bytes),Share %,Files,Modified,Path\r\n");
        foreach (var f in r.TopFolders) b.Append("Folder," + f.Bytes + "," + (r.TotalBytes > 0 ? (100.0 * f.Bytes / r.TotalBytes).ToString("F2", CultureInfo.InvariantCulture) : "0") + "," + f.Files + ",," + CsvCell(f.Path) + "\r\n");
        foreach (var f in r.LargestFiles) b.Append("File," + f.Bytes + "," + (r.TotalBytes > 0 ? (100.0 * f.Bytes / r.TotalBytes).ToString("F2", CultureInfo.InvariantCulture) : "0") + ",1," + f.Modified.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "," + CsvCell(f.Path) + "\r\n");
        return b.ToString();
    }
    // A short, shareable summary (no file names beyond the top few) for chat or email.
    public static string Summary(SpaceReport r, int top = 5) {
        var b = new System.Text.StringBuilder("Space report for " + r.Root + ": " + Bytes(r.TotalBytes) + " in " + r.Files + " files" + (r.Canceled ? " (partial)" : "") + "\r\n");
        int n = 0; foreach (var f in r.TopFolders) { if (n++ >= top) break; b.AppendLine("  " + Bytes(f.Bytes).PadLeft(9) + "  " + f.Path); }
        return b.ToString();
    }

    public static string Bytes(long n) {
        if (n < 0) return "Unavailable";
        string[] units = { "B", "KB", "MB", "GB", "TB", "PB" };
        double v = n; int u = 0;
        while (v >= 1024 && u < units.Length - 1) { v /= 1024; u++; }
        return u == 0 ? n + " B" : v.ToString(v >= 100 ? "F0" : "F1", CultureInfo.InvariantCulture) + " " + units[u];
    }

    public static string Share(long part, long total) {
        if (total <= 0) return "0%";
        return (100.0 * part / total).ToString("F1", CultureInfo.InvariantCulture) + "%";
    }
}

public sealed class PingStats {
    readonly List<long> window = new List<long>();
    public int Sent { get; private set; }
    public int Received { get; private set; }
    public int Window { get; private set; }
    public long? Last { get; private set; }

    public PingStats(int window = 60) { Window = Math.Max(2, window); }

    public void Add(long? milliseconds) {
        Sent++;
        Last = milliseconds;
        if (milliseconds.HasValue) {
            Received++;
            window.Add(Math.Max(0, milliseconds.Value));
            if (window.Count > Window) window.RemoveAt(0);
        }
    }

    public IList<long> Recent { get { return window.AsReadOnly(); } }
    public double LossPercent { get { return Sent == 0 ? 0 : 100.0 * (Sent - Received) / Sent; } }
    public long Min { get { return window.Count == 0 ? 0 : window.Min(); } }
    public long Max { get { return window.Count == 0 ? 0 : window.Max(); } }
    public double Average { get { return window.Count == 0 ? 0 : window.Average(); } }

    // Mean absolute difference between consecutive replies (RFC 3550 style, unsmoothed).
    public double Jitter {
        get {
            if (window.Count < 2) return 0;
            double sum = 0;
            for (int i = 1; i < window.Count; i++) sum += Math.Abs(window[i] - window[i - 1]);
            return sum / (window.Count - 1);
        }
    }

    public string Verdict {
        get {
            if (Sent == 0) return "Waiting for the first reply…";
            if (Received == 0) return "No replies — the host may be down, blocking ping, or you are offline.";
            if (LossPercent >= 5) return "Unstable — noticeable packet loss.";
            if (Average >= 150 || Jitter >= 30) return "Slow or jittery — calls and games may stutter.";
            if (Average >= 60) return "Okay — fine for browsing and streaming.";
            return "Excellent — low latency and stable.";
        }
    }

    public void Reset() { window.Clear(); Sent = Received = 0; Last = null; }
}

public static class PingReport {
    public static string Text(string host, PingStats st, DateTime now, DateTime? started = null) {
        var b = new System.Text.StringBuilder("ASTER PING REPORT\r\nHost: " + host + "\r\nCreated: " + now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + "\r\n");
        if (started.HasValue) b.AppendLine("Monitored for: " + (now - started.Value).ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture));
        b.AppendLine("Sent: " + st.Sent + "   Received: " + st.Received + "   Loss: " + st.LossPercent.ToString("F1", CultureInfo.InvariantCulture) + "%");
        b.AppendLine("Latency ms  avg " + st.Average.ToString("F1", CultureInfo.InvariantCulture) + "  min " + st.Min + "  max " + st.Max + "  jitter " + st.Jitter.ToString("F1", CultureInfo.InvariantCulture));
        b.AppendLine("Verdict: " + st.Verdict);
        b.AppendLine("Last " + st.Recent.Count + " replies (ms): " + string.Join(" ", st.Recent.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToArray()));
        b.AppendLine("\r\nA snapshot of one connection at one time, not a full network test.");
        return b.ToString();
    }
}

public enum PowerAction { Shutdown, Restart }

public static class PowerPlan {
    public const int MaxSeconds = 315359999; // shutdown.exe /t limit (10 years)

    public static int SecondsUntil(DateTime target, DateTime now) {
        double s = Math.Ceiling((target - now).TotalSeconds);
        if (s < 0) s = 0;
        return (int)Math.Min(s, MaxSeconds);
    }

    // Next occurrence of a clock time (today, or tomorrow if it has passed).
    public static DateTime NextAt(int hour, int minute, DateTime now) {
        if (hour < 0 || hour > 23 || minute < 0 || minute > 59) throw new ArgumentOutOfRangeException("hour");
        var at = now.Date.AddHours(hour).AddMinutes(minute);
        return at <= now ? at.AddDays(1) : at;
    }

    // Arguments for %WINDIR%\System32\shutdown.exe. Windows shows its own warning
    // and the plan can always be aborted with "shutdown /a".
    public static string Arguments(PowerAction action, int seconds) {
        if (seconds < 0 || seconds > MaxSeconds) throw new ArgumentOutOfRangeException("seconds");
        return (action == PowerAction.Restart ? "/r" : "/s") + " /t " + seconds.ToString(CultureInfo.InvariantCulture)
            + " /c \"Scheduled by Aster Power timer. Run 'shutdown /a' or use Aster to cancel.\"";
    }

    public const string AbortArguments = "/a";

    // ---- recurring plans use the Windows Task Scheduler (schtasks.exe), so they still run when Aster is closed ----
    public const string TaskName = "AsterPowerTimer";
    static readonly string[] DayCodes = { "SUN", "MON", "TUE", "WED", "THU", "FRI", "SAT" };
    // days: bit 0 = Sunday ... bit 6 = Saturday. 127 = every day.
    public static string DaysText(int days) {
        if (days <= 0 || days > 127) throw new ArgumentOutOfRangeException("days");
        var l = new List<string>(); for (int i = 0; i < 7; i++) if ((days & (1 << i)) != 0) l.Add(DayCodes[i]); return string.Join(",", l.ToArray());
    }
    public static string CreateTaskArguments(PowerAction action, int hour, int minute, int days) {
        if (hour < 0 || hour > 23 || minute < 0 || minute > 59) throw new ArgumentOutOfRangeException("hour");
        string time = hour.ToString("00", CultureInfo.InvariantCulture) + ":" + minute.ToString("00", CultureInfo.InvariantCulture);
        string run = "shutdown.exe " + (action == PowerAction.Restart ? "/r" : "/s") + " /t 60 /c \"Scheduled by Aster. Run shutdown /a within 60 seconds to cancel.\"";
        string sched = days == 127 ? "/sc DAILY" : "/sc WEEKLY /d " + DaysText(days);
        return "/create " + sched + " /st " + time + " /tn " + TaskName + " /tr \"" + run.Replace("\"", "\\\"") + "\" /f";
    }
    public static string DeleteTaskArguments() { return "/delete /tn " + TaskName + " /f"; }
    public static string QueryTaskArguments() { return "/query /tn " + TaskName + " /fo LIST"; }
    public static string Describe(PowerAction action, int hour, int minute, int days) {
        return (action == PowerAction.Restart ? "Restart" : "Shut down") + " at " + hour.ToString("00", CultureInfo.InvariantCulture) + ":" + minute.ToString("00", CultureInfo.InvariantCulture) + " " + (days == 127 ? "every day" : "on " + DaysText(days).Replace(",", ", "));
    }

    public static string Countdown(TimeSpan left) {
        if (left < TimeSpan.Zero) left = TimeSpan.Zero;
        return left.TotalHours >= 1
            ? string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}:{2:00}", (int)left.TotalHours, left.Minutes, left.Seconds)
            : string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}", left.Minutes, left.Seconds);
    }
}

// v2.6 — Tools hub model: what to show, in which order, with a calmer "Simple mode".
public static class HubModel {
    // Specialist tools that Simple mode hides from the hub and the sidebar search. Nothing is removed: turn Simple mode off to see them again.
    public static readonly HashSet<string> Advanced = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
        "Air Defense feed", "Recovery toolbox", "Services", "Drivers", "Users", "Connections", "Flight recorder", "Session timeline", "Performance compare",
        "Diagnostics bundle", "Motherboard & BIOS", "Hardware compare", "Theme studio", "Playground", "Home layout", "Wallpaper studio", "Compact monitor",
        "Insights", "App history", "Horizon desktop", "Desktop mods", "Network adapters", "Memory specs" };
    public static bool IsAdvanced(string name) { return Advanced.Contains(name ?? ""); }

    static readonly Dictionary<string, string> Icons = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
        {"Summary","📊"},{"Performance","⚡"},{"Processes","🧮"},{"CPU specs","🧠"},{"GPU","🎮"},{"Storage specs","💽"},{"System info","ℹ"},{"Disk space","📦"},{"Space finder","🔍"},
        {"Network lab","🌐"},{"Ping monitor","📶"},{"Hardware report","📝"},{"Update Aster","⬆"},{"System check","🩺"},{"Windows advisor","🤖"},{"Backup center","💾"},{"Help center","❓"},
        {"Startup apps","🚀"},{"Installed apps","🗂"},{"Maintenance planner","🗓"},{"Command Center","🧭"},{"Notebook","📓"},{"Checklist","✅"},{"Focus","🎯"},{"Focus timer","⏱"},
        {"Power timer","⏻"},{"Video player","🎬"},{"Themes","🎨"},{"Preferences","⚙"},{"Performance alerts","🔔"},{"Memory specs","🧩"},{"Services","🛠"},{"Drivers","🔧"},{"Tools hub","🧰"},{"Session timeline","🕘"},{"Performance compare","⚖"},{"Air Defense feed","🛡"},{"Compact monitor","🔲"},{"Insights","💡"},{"App history","📜"},{"Flight recorder","✈"},{"Motherboard & BIOS","🧱"},{"Network adapters","🔌"},{"Hardware compare","🆚"},{"Diagnostics bundle","📦"},{"Recovery toolbox","🧰"},{"Connections","🔗"},{"Users","👤"},{"Horizon desktop","🪟"},{"Desktop mods","🧩"},{"Theme studio","🖌"},{"Home layout","🏠"},{"Wallpaper studio","🖼"},{"Playground","🎲"} };
    public static string Icon(string name) { string i; return Icons.TryGetValue(name ?? "", out i) ? i : "▫"; }

    // Matches every word of the query against the tool name and its description. Name matches rank first.
    public static int Score(string name, string hint, string query) {
        if (string.IsNullOrWhiteSpace(query)) return 1;
        int total = 0;
        foreach (string w in query.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)) {
            int n = (name ?? "").IndexOf(w, StringComparison.OrdinalIgnoreCase), h = (hint ?? "").IndexOf(w, StringComparison.OrdinalIgnoreCase);
            if (n < 0 && h < 0) return 0;
            total += n == 0 ? 40 : n > 0 ? 25 : 8;
        }
        return total;
    }

    // groups: [groupTitle, tool, tool, ...]. Returns the sections to show, best matches first inside each section.
    public static List<KeyValuePair<string, List<string>>> Sections(IList<string[]> groups, string query, string category, bool simple, Func<string, string> hint) {
        var result = new List<KeyValuePair<string, List<string>>>(); var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var g in groups) {
            if (!string.IsNullOrEmpty(category) && !string.Equals(category, g[0], StringComparison.OrdinalIgnoreCase)) continue;
            var scored = new List<KeyValuePair<string, int>>();
            for (int i = 1; i < g.Length; i++) {
                string name = g[i]; if (simple && IsAdvanced(name)) continue; if (!seen.Add(name)) continue; // a tool listed in two groups shows once
                int sc = Score(name, hint == null ? "" : hint(name), query); if (sc > 0) scored.Add(new KeyValuePair<string, int>(name, sc));
            }
            if (scored.Count == 0) continue;
            if (!string.IsNullOrWhiteSpace(query)) scored = scored.OrderByDescending(x => x.Value).ToList();
            result.Add(new KeyValuePair<string, List<string>>(g[0], scored.Select(x => x.Key).ToList()));
        }
        return result;
    }

    // The quick-access row: pinned tools first, then what you opened recently, no duplicates, only tools that exist.
    public static List<string> QuickAccess(IEnumerable<string> pins, IEnumerable<string> recent, Func<string, bool> exists, bool simple, int max) {
        var o = new List<string>(); var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string n in (pins ?? new string[0]).Concat(recent ?? new string[0])) {
            if (o.Count >= max) break; if (string.IsNullOrWhiteSpace(n) || n == "Tools hub" || !exists(n) || (simple && IsAdvanced(n)) || !seen.Add(n)) continue; o.Add(n);
        }
        return o;
    }
}

// v2.7 — Home screen verdicts and the crash safety net.
public enum HomeLevel { Good, Busy, Attention }
public static class HomeModel {
    // One plain sentence about the PC, from three numbers. freeDiskPct < 0 means "unknown".
    public static HomeLevel Level(double cpu, double ram, double freeDiskPct) {
        if (cpu >= 90 || ram >= 92 || (freeDiskPct >= 0 && freeDiskPct < 8)) return HomeLevel.Attention;
        if (cpu >= 70 || ram >= 80 || (freeDiskPct >= 0 && freeDiskPct < 15)) return HomeLevel.Busy;
        return HomeLevel.Good;
    }
    public static string Verdict(double cpu, double ram, double freeDiskPct) {
        var l = Level(cpu, ram, freeDiskPct); var why = new List<string>();
        if (cpu >= 70) why.Add("the processor is working hard"); if (ram >= 80) why.Add("memory is getting full"); if (freeDiskPct >= 0 && freeDiskPct < 15) why.Add("the system drive is running low on space");
        if (l == HomeLevel.Good) return "Everything looks healthy.";
        string s = char.ToUpperInvariant(why[0][0]) + why[0].Substring(1); for (int i = 1; i < why.Count; i++) s += (i == why.Count - 1 ? " and " : ", ") + why[i];
        return s + (l == HomeLevel.Attention ? ". Worth a look." : ".");
    }
    public enum DayPart { Night, Dawn, Morning, Afternoon, Evening }
    // 22-04 night, 05-07 dawn, 08-11 morning, 12-17 afternoon, 18-21 evening. Hours outside 0-23 wrap safely.
    public static DayPart Part(int hour) { hour = ((hour % 24) + 24) % 24; return hour >= 22 || hour < 5 ? DayPart.Night : hour < 8 ? DayPart.Dawn : hour < 12 ? DayPart.Morning : hour < 18 ? DayPart.Afternoon : DayPart.Evening; }
    public static string Greeting(int hour) { switch (Part(hour)) { case DayPart.Night: return hour % 24 >= 22 ? "Good night" : "Working late"; case DayPart.Dawn: return "Early start"; case DayPart.Morning: return "Good morning"; case DayPart.Afternoon: return "Good afternoon"; default: return "Good evening"; } }
    public static string Emoji(int hour) { switch (Part(hour)) { case DayPart.Night: return "🌙"; case DayPart.Dawn: return "🌅"; case DayPart.Morning: return "☀"; case DayPart.Afternoon: return "🌤"; default: return "🌆"; } }
    // One time-aware idea: what to do right now, and the tool that does it. Rotates with the day of the year so it does not nag with the same line.
    public static KeyValuePair<string, string> Suggestion(int hour, int dayOfYear, bool pcBusy) {
        string[][] pool;
        switch (Part(hour)) {
            case DayPart.Dawn: case DayPart.Morning: pool = new[] { new[] { "Start the day with a quick health check.", "System check" }, new[] { "Plan today: set a reminder or two.", "Maintenance planner" }, new[] { "Make sure the connection is solid before your first call.", "Ping monitor" } }; break;
            case DayPart.Afternoon: pool = new[] { new[] { "Find what is filling your drive.", "Space finder" }, new[] { "Clear your head with a focus session.", "Focus timer" }, new[] { "Jot down what you have learned today.", "Notebook" } }; break;
            case DayPart.Evening: pool = new[] { new[] { "Wind down: schedule the PC to shut itself off later.", "Power timer" }, new[] { "Save a report of today's connection quality.", "Ping monitor" }, new[] { "Review what happened today.", "Session timeline" } }; break;
            default: pool = new[] { new[] { "Late night? Set the PC to shut down so you can stop worrying about it.", "Power timer" }, new[] { "Pause live monitoring to keep things quiet.", "Preferences" } }; break;
        }
        if (pcBusy && Part(hour) != DayPart.Night) return new KeyValuePair<string, string>("Your PC is busy right now: see what is using it.", "Processes");
        var pick = pool[((dayOfYear % pool.Length) + pool.Length) % pool.Length]; return new KeyValuePair<string, string>(pick[0], pick[1]);
    }
    public static readonly string[] QuickTools = { "Space finder", "Ping monitor", "Power timer", "System check", "Windows advisor", "Tools hub" };
}

public static class CrashReport {
    // Removes the Windows user name from paths so a report can be shared safely: C:\Users\alice\x -> C:\Users\<user>\x
    public static string Redact(string text) {
        if (string.IsNullOrEmpty(text)) return text ?? "";
        var t = System.Text.RegularExpressions.Regex.Replace(text, @"(?i)([A-Z]:\\Users\\)[^\\/\r\n:*?""<>|]+", "$1<user>");
        t = System.Text.RegularExpressions.Regex.Replace(t, @"(?i)(/home/|/Users/)[^/\r\n]+", "$1<user>");
        string name = Environment.UserName; if (!string.IsNullOrEmpty(name) && name.Length > 2) t = t.Replace(name, "<user>");
        return t;
    }
    public static string Build(Exception ex, string version, DateTime now, string os, string page) {
        var b = new System.Text.StringBuilder("ASTER CRASH REPORT\r\nVersion: " + version + "\r\nTime: " + now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + "\r\nWindows: " + os + "\r\nPage: " + (string.IsNullOrEmpty(page) ? "(unknown)" : page) + "\r\n\r\n");
        int depth = 0; for (var e = ex; e != null && depth < 5; e = e.InnerException, depth++) { b.AppendLine((depth == 0 ? "" : "Caused by: ") + e.GetType().FullName + ": " + e.Message); string st = e.StackTrace; if (st != null) b.AppendLine(st.Length > 3000 ? st.Substring(0, 3000) + " ..." : st); b.AppendLine(); }
        b.AppendLine("No personal files or settings are included. Your Windows user name was removed from paths.");
        return Redact(b.ToString());
    }
    // Keeps the newest N crash files so the folder never grows without limit.
    public static void Trim(string dir, int keep) {
        try { var files = new DirectoryInfo(dir).GetFiles("crash-*.txt").OrderByDescending(f => f.Name).ToList(); for (int i = keep; i < files.Count; i++) { try { files[i].Delete(); } catch { } } } catch { }
    }
}
}
