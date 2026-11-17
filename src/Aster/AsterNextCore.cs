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
}
