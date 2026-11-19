using System;
using System.Diagnostics;
using System.IO;
using System.Web.Script.Serialization;

namespace Aster;

internal static class UpdateReceipt
{
	internal static string Root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Aster");

	public static string Versions => Path.Combine(Root, "Versions");

	public static string Downloads => Path.Combine(Root, "Downloads");

	private static string FileName(bool active)
	{
		return Path.Combine(Root, active ? "active-update.json" : "pending-update.json");
	}

	private static void NoLinks(string path)
	{
		for (DirectoryInfo directoryInfo = new DirectoryInfo(Path.GetFullPath(path)); directoryInfo != null; directoryInfo = directoryInfo.Parent)
		{
			if (directoryInfo.Exists && (directoryInfo.Attributes & FileAttributes.ReparsePoint) != 0)
			{
				throw new IOException("Linked update folders are not supported.");
			}
		}
	}

	public static InstalledUpdate Read(bool active)
	{
		string text = FileName(active);
		if (!File.Exists(text))
		{
			return null;
		}
		if (new FileInfo(text).Length > 8192)
		{
			throw new IOException("Update record is too large.");
		}
		InstalledUpdate installedUpdate = new JavaScriptSerializer().Deserialize<InstalledUpdate>(File.ReadAllText(text));
		if (installedUpdate == null || !Version.TryParse(installedUpdate.Version, out var result) || result <= new Version(AsterUpdate.CurrentVersion))
		{
			return null;
		}
		string fullPath = Path.GetFullPath(installedUpdate.Folder);
		string value = Path.GetFullPath(Versions).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
		if (!fullPath.StartsWith(value, StringComparison.OrdinalIgnoreCase))
		{
			throw new IOException("Update record is outside Aster's version folder.");
		}
		NoLinks(fullPath);
		string path = Path.Combine(fullPath, "Aster.exe");
		if (!File.Exists(path) || (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
		{
			throw new IOException("The installed update is missing or linked.");
		}
		using (FileStream stream = File.OpenRead(path))
		{
			if (!string.Equals(AsterUpdate.Hash(stream), installedUpdate.ExeHash, StringComparison.OrdinalIgnoreCase))
			{
				throw new IOException("The installed update has changed. Check and download it again.");
			}
		}
		installedUpdate.Folder = fullPath;
		return installedUpdate;
	}

	public static void Save(string folder, string version, bool active)
	{
		string fullPath = Path.GetFullPath(folder);
		string value = Path.GetFullPath(Versions).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
		if (!fullPath.StartsWith(value, StringComparison.OrdinalIgnoreCase))
		{
			throw new IOException("Invalid update destination.");
		}
		NoLinks(fullPath);
		Directory.CreateDirectory(Root);
		string exeHash;
		using (FileStream stream = File.OpenRead(Path.Combine(fullPath, "Aster.exe")))
		{
			exeHash = AsterUpdate.Hash(stream);
		}
		InstalledUpdate installedUpdate = new InstalledUpdate();
		installedUpdate.Folder = fullPath;
		installedUpdate.Version = version;
		installedUpdate.ExeHash = exeHash;
		InstalledUpdate obj = installedUpdate;
		string text = FileName(active);
		string text2 = text + "." + Guid.NewGuid().ToString("N") + ".tmp";
		try
		{
			File.WriteAllText(text2, new JavaScriptSerializer().Serialize(obj));
			if (File.Exists(text))
			{
				File.Replace(text2, text, null);
			}
			else
			{
				File.Move(text2, text);
			}
		}
		finally
		{
			if (File.Exists(text2))
			{
				File.Delete(text2);
			}
		}
	}

	public static bool TryForward()
	{
		try
		{
			InstalledUpdate installedUpdate = Read(active: true);
			if (installedUpdate == null)
			{
				return false;
			}
			ProcessStartInfo processStartInfo = new ProcessStartInfo(Path.Combine(installedUpdate.Folder, "Aster.exe"));
			processStartInfo.UseShellExecute = true;
			processStartInfo.WorkingDirectory = installedUpdate.Folder;
			Process.Start(processStartInfo);
			return true;
		}
		catch
		{
			return false;
		}
	}
}
