using System;
using System.IO;
using System.Windows.Forms;

namespace Aster {

// v2.7 — a safety net: an unexpected error is written to a report (Windows user name removed) and the person is offered a way to copy it.
static class CrashLog {
    public static string Page = "";
    public static string Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Pulse", "Errors");
    static bool showing; static DateTime lastShown = DateTime.MinValue;

    public static void Install() {
        try {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) => Handle(e.Exception, false);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => Handle(e.ExceptionObject as Exception ?? new Exception(Convert.ToString(e.ExceptionObject)), e.IsTerminating);
        } catch { }
    }
    // Returns the saved file path, or null if it could not be saved. Never throws.
    public static string Save(Exception ex, string version) {
        try {
            Directory.CreateDirectory(Folder); string text = CrashReport.Build(ex, version, DateTime.Now, Environment.OSVersion.VersionString, Page);
            string file = Path.Combine(Folder, "crash-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Environment.TickCount.ToString("x") + ".txt"); File.WriteAllText(file, text); CrashReport.Trim(Folder, 20); return file;
        } catch { return null; }
    }
    static void Handle(Exception ex, bool fatal) {
        string file = Save(ex, AsterUpdate.CurrentVersion);
        if (showing || (DateTime.Now - lastShown).TotalSeconds < 5) return; // one friendly message, not a storm of them
        showing = true;
        try {
            string text = "Aster hit an unexpected problem" + (fatal ? " and needs to close" : " but is still running") + ".\r\n\r\n" + (file != null ? "A report was saved (your user name is removed from it):\r\n" + file + "\r\n\r\n" : "") + "Copy the report so you can send it to whoever is helping you?";
            if (MessageBox.Show(text, "Aster", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes && file != null) { try { Clipboard.SetText(File.ReadAllText(file)); } catch { } }
        } catch { } finally { showing = false; lastShown = DateTime.Now; }
    }
}
}
