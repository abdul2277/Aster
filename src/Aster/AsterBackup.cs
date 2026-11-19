using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace Aster;

internal static class AsterBackup
{
	public const long MaxTotal = 20971520L;

	public const int MaxFile = 1048576;

	public const int MaxEntries = 600;

	public static bool Allowed(string name)
	{
		switch (name)
		{
		case "Workspace.json":
		case "PersonalWorkspace.json":
		case "NetworkHistory.json":
			return true;
		default:
			if (name.StartsWith("Cases/", StringComparison.Ordinal) && name.EndsWith(".json", StringComparison.OrdinalIgnoreCase) && name.Length > 11 && name.Substring(6).All((char c) => char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.'))
			{
				return !name.Contains("..");
			}
			return false;
		}
	}

	private static void SafeExisting(string path)
	{
		if ((File.Exists(path) || Directory.Exists(path)) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
		{
			throw new Exception("Linked files or folders are not supported: " + Path.GetFileName(path));
		}
	}

	private static string Target(string root, string name)
	{
		if (!Allowed(name))
		{
			throw new Exception("Unsupported archive entry: " + name);
		}
		root = Path.GetFullPath(root);
		SafeExisting(root);
		if (name.StartsWith("Cases/"))
		{
			SafeExisting(Path.Combine(root, "Cases"));
		}
		string fullPath = Path.GetFullPath(Path.Combine(root, name.Replace('/', Path.DirectorySeparatorChar)));
		if (!fullPath.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
		{
			throw new Exception("Invalid target path.");
		}
		SafeExisting(fullPath);
		return fullPath;
	}

	private static void Validate(string name, byte[] bytes)
	{
		if (!Allowed(name) || bytes.Length > 1048576)
		{
			throw new Exception("Unsupported or oversized entry: " + name);
		}
		string input = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(bytes).TrimStart('\ufeff');
		JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
		javaScriptSerializer.MaxJsonLength = 1048576;
		JavaScriptSerializer javaScriptSerializer2 = javaScriptSerializer;
		switch (name)
		{
		case "PersonalWorkspace.json":
			PersonalStore.Validate(javaScriptSerializer2.Deserialize<PersonalData>(input));
			break;
		case "Workspace.json":
		{
			WorkspaceOptions workspaceOptions = javaScriptSerializer2.Deserialize<WorkspaceOptions>(input);
			if (workspaceOptions == null || !new int[4] { 1000, 2000, 3000, 5000 }.Contains(workspaceOptions.Interval) || workspaceOptions.Pins == null || workspaceOptions.Pins.Count > 100 || workspaceOptions.Pins.Any((string n) => string.IsNullOrWhiteSpace(n) || n.Length > 100))
			{
				throw new Exception("Invalid settings file.");
			}
			if (workspaceOptions.ThemeId == "custom")
			{
				ThemeRecipe themeRecipe = new ThemeRecipe();
				themeRecipe.Background = workspaceOptions.CustomBase;
				themeRecipe.Accent = workspaceOptions.CustomAccent;
				CustomTheme.Create(themeRecipe);
			}
			break;
		}
		case "NetworkHistory.json":
		{
			List<NetworkResult> list = javaScriptSerializer2.Deserialize<List<NetworkResult>>(input);
			if (list == null || list.Count > 50 || list.Any((NetworkResult r) => r == null))
			{
				throw new Exception("Invalid network history.");
			}
			break;
		}
		default:
		{
			SupportCase supportCase = javaScriptSerializer2.Deserialize<SupportCase>(input);
			if (supportCase == null || supportCase.Version != 1 || string.IsNullOrWhiteSpace(supportCase.Title))
			{
				throw new Exception("Invalid saved case: " + name);
			}
			break;
		}
		}
	}

	public static Dictionary<string, byte[]> Read(string path)
	{
		if (new FileInfo(path).Length > 22020096)
		{
			throw new Exception("Archive exceeds 21 MiB.");
		}
		Dictionary<string, byte[]> dictionary = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
		long num = 0L;
		using ZipArchive zipArchive = ZipFile.OpenRead(path);
		if (zipArchive.Entries.Count == 0 || zipArchive.Entries.Count > 600)
		{
			throw new Exception("Archive must contain 1–600 supported files.");
		}
		foreach (ZipArchiveEntry entry in zipArchive.Entries)
		{
			if (!Allowed(entry.FullName) || dictionary.ContainsKey(entry.FullName) || entry.Length > 1048576 || (num += entry.Length) > 20971520)
			{
				throw new Exception("Unsafe, duplicate or oversized archive entry: " + entry.FullName);
			}
			using Stream stream = entry.Open();
			using MemoryStream memoryStream = new MemoryStream();
			byte[] array = new byte[8192];
			int num2;
			while ((num2 = stream.Read(array, 0, array.Length)) > 0)
			{
				if (memoryStream.Length + num2 > 1048576)
				{
					throw new Exception("Expanded entry too large.");
				}
				memoryStream.Write(array, 0, num2);
			}
			byte[] array2 = memoryStream.ToArray();
			if (array2.LongLength != entry.Length)
			{
				throw new Exception("Incomplete archive entry.");
			}
			Validate(entry.FullName, array2);
			dictionary.Add(entry.FullName, array2);
		}
		return dictionary;
	}

	private static Dictionary<string, byte[]> Collect(string root)
	{
		Dictionary<string, byte[]> dictionary = new Dictionary<string, byte[]>();
		SafeExisting(root);
		List<string> list = new List<string>();
		list.Add("Workspace.json");
		list.Add("PersonalWorkspace.json");
		list.Add("NetworkHistory.json");
		List<string> list2 = list;
		string path = Path.Combine(root, "Cases");
		SafeExisting(path);
		if (Directory.Exists(path))
		{
			list2.AddRange(from p in Directory.GetFiles(path, "*.json", SearchOption.TopDirectoryOnly)
				select "Cases/" + Path.GetFileName(p));
		}
		long num = 0L;
		foreach (string item in list2)
		{
			string text = Target(root, item);
			if (File.Exists(text))
			{
				FileInfo fileInfo = new FileInfo(text);
				if (fileInfo.Length > 1048576 || (num += fileInfo.Length) > 20971520 || dictionary.Count >= 600)
				{
					throw new Exception("Backup limit exceeded: 600 files, 1 MiB each, 20 MiB total.");
				}
				byte[] array = File.ReadAllBytes(text);
				Validate(item, array);
				dictionary.Add(item, array);
			}
		}
		return dictionary;
	}

	private static void Write(string path, Dictionary<string, byte[]> entries)
	{
		string text = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
		try
		{
			using (ZipArchive zipArchive = ZipFile.Open(text, ZipArchiveMode.Create))
			{
				foreach (KeyValuePair<string, byte[]> entry in entries)
				{
					ZipArchiveEntry zipArchiveEntry = zipArchive.CreateEntry(entry.Key, CompressionLevel.Optimal);
					using Stream stream = zipArchiveEntry.Open();
					stream.Write(entry.Value, 0, entry.Value.Length);
				}
			}
			if (File.Exists(path))
			{
				File.Replace(text, path, null);
			}
			else
			{
				File.Move(text, path);
			}
		}
		finally
		{
			if (File.Exists(text))
			{
				File.Delete(text);
			}
		}
	}

	public static int Create(string root, string destination)
	{
		Dictionary<string, byte[]> dictionary = Collect(root);
		if (dictionary.Count == 0)
		{
			throw new Exception("No saved Aster settings, notes, history or cases were found yet.");
		}
		Write(destination, dictionary);
		return dictionary.Count;
	}

	private static void WriteTarget(string path, byte[] bytes)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(path));
		string text = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
		try
		{
			File.WriteAllBytes(text, bytes);
			if (File.Exists(path))
			{
				File.Replace(text, path, null);
			}
			else
			{
				File.Move(text, path);
			}
		}
		finally
		{
			if (File.Exists(text))
			{
				File.Delete(text);
			}
		}
	}

	public static string Restore(string root, Dictionary<string, byte[]> entries)
	{
		if (entries.Count == 0 || entries.Count > 600 || ((IEnumerable<KeyValuePair<string, byte[]>>)entries).Sum((Func<KeyValuePair<string, byte[]>, long>)((KeyValuePair<string, byte[]> x) => x.Value.Length)) > 20971520)
		{
			throw new Exception("Invalid restore size.");
		}
		Dictionary<string, byte[]> dictionary = new Dictionary<string, byte[]>();
		foreach (KeyValuePair<string, byte[]> entry in entries)
		{
			Validate(entry.Key, entry.Value);
			string text = Target(root, entry.Key);
			if (File.Exists(text))
			{
				if (new FileInfo(text).Length > 1048576)
				{
					throw new Exception("Existing destination file is too large to back up.");
				}
				dictionary[entry.Key] = File.ReadAllBytes(text);
			}
		}
		Directory.CreateDirectory(root);
		string text2 = Path.Combine(root, "Backups");
		SafeExisting(text2);
		Directory.CreateDirectory(text2);
		string text3 = Path.Combine(text2, "before-restore-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N") + ".zip");
		Write(text3, dictionary);
		List<string> list = new List<string>();
		try
		{
			foreach (KeyValuePair<string, byte[]> entry2 in entries)
			{
				WriteTarget(Target(root, entry2.Key), entry2.Value);
				list.Add(entry2.Key);
			}
			return text3;
		}
		catch (Exception ex)
		{
			List<string> list2 = new List<string>();
			foreach (string item in list.AsEnumerable().Reverse())
			{
				try
				{
					string path = Target(root, item);
					if (dictionary.ContainsKey(item))
					{
						WriteTarget(path, dictionary[item]);
					}
					else if (File.Exists(path))
					{
						File.Delete(path);
					}
				}
				catch (Exception ex2)
				{
					list2.Add(ex2.Message);
				}
			}
			throw new Exception("Restore failed: " + ex.Message + "\r\n" + ((list2.Count == 0) ? "Written files rolled back." : ("Some rollback steps failed: " + string.Join("; ", list2))) + "\r\nPre-restore backup: " + text3);
		}
	}
}
