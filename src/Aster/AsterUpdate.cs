using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace Aster;

internal static class AsterUpdate
{
	public const string CurrentVersion = "2.7";

	private const long MaxTotal = 262144000L;

	private const long MaxFile = 67108864L;

	public static string Hash(Stream stream)
	{
		using SHA256 sHA = SHA256.Create();
		return BitConverter.ToString(sHA.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
	}

	private static string FileHash(Stream stream, long expected)
	{
		using SHA256 sHA = SHA256.Create();
		byte[] array = new byte[65536];
		long num = 0L;
		int num2;
		while ((num2 = stream.Read(array, 0, array.Length)) > 0)
		{
			num += num2;
			if (num > expected)
			{
				throw new InvalidDataException("Expanded file exceeds its declared size.");
			}
			sHA.TransformBlock(array, 0, num2, array, 0);
		}
		if (num != expected)
		{
			throw new InvalidDataException("Expanded file size mismatch.");
		}
		sHA.TransformFinalBlock(new byte[0], 0, 0);
		return BitConverter.ToString(sHA.Hash).Replace("-", "").ToLowerInvariant();
	}

	private static string ManifestText(ZipArchiveEntry entry)
	{
		using Stream stream = entry.Open();
		using MemoryStream memoryStream = new MemoryStream();
		byte[] array = new byte[8192];
		int num;
		while ((num = stream.Read(array, 0, array.Length)) > 0)
		{
			if (memoryStream.Length + num > 2097152)
			{
				throw new InvalidDataException("Manifest too large.");
			}
			memoryStream.Write(array, 0, num);
		}
		memoryStream.Position = 0L;
		using StreamReader streamReader = new StreamReader(memoryStream);
		return streamReader.ReadToEnd();
	}

	public static bool SafeName(string name)
	{
		if (string.IsNullOrEmpty(name) || name.Length > 240 || name.Contains('\\') || name.StartsWith("/") || name.EndsWith("/"))
		{
			return false;
		}
		string[] array = name.Split('/');
		foreach (string text in array)
		{
			if (text == "." || text == ".." || text.Length == 0 || text.EndsWith(".") || text.EndsWith(" ") || !Regex.IsMatch(text, "^[A-Za-z0-9_. -]+$"))
			{
				return false;
			}
			string text2 = text.Split('.')[0].ToUpperInvariant();
			if (new string[6] { "CON", "PRN", "AUX", "NUL", "CONIN$", "CONOUT$" }.Contains(text2) || Regex.IsMatch(text2, "^(COM|LPT)[0-9]$"))
			{
				return false;
			}
		}
		return true;
	}

	private static void NoLinks(string path)
	{
		for (DirectoryInfo directoryInfo = new DirectoryInfo(Path.GetFullPath(path)); directoryInfo != null; directoryInfo = directoryInfo.Parent)
		{
			if (directoryInfo.Exists && (directoryInfo.Attributes & FileAttributes.ReparsePoint) != 0)
			{
				throw new InvalidDataException("Linked update folders are not supported.");
			}
		}
	}

	private static FileStream OpenPackage(string path)
	{
		FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
		if (fileStream.Length > 262144000)
		{
			fileStream.Dispose();
			throw new InvalidDataException("Update ZIP exceeds 250 MB.");
		}
		return fileStream;
	}

	private static UpdatePackage Read(ZipArchive zip, string hash)
	{
		if (zip.Entries.Count > 5000)
		{
			throw new InvalidDataException("Too many ZIP entries.");
		}
		Dictionary<string, ZipArchiveEntry> dictionary = new Dictionary<string, ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
		long num = 0L;
		foreach (ZipArchiveEntry entry in zip.Entries)
		{
			string fullName = entry.FullName;
			if (fullName.EndsWith("/"))
			{
				if (!SafeName(fullName.TrimEnd('/')))
				{
					throw new InvalidDataException("Unsafe ZIP directory.");
				}
				continue;
			}
			if (!SafeName(fullName) || entry.Length > 67108864 || ((entry.ExternalAttributes >> 16) & 0xF000) == 40960)
			{
				throw new InvalidDataException("Unsafe or oversized ZIP entry.");
			}
			if (dictionary.ContainsKey(fullName))
			{
				throw new InvalidDataException("Duplicate ZIP paths.");
			}
			dictionary.Add(fullName, entry);
			num = checked(num + entry.Length);
			if (num > 262144000)
			{
				throw new InvalidDataException("Expanded update exceeds 250 MB.");
			}
		}
		KeyValuePair<string, ZipArchiveEntry>[] array = dictionary.Where((KeyValuePair<string, ZipArchiveEntry> p) => p.Key.Equals("release.json", StringComparison.OrdinalIgnoreCase) || p.Key.EndsWith("/release.json", StringComparison.OrdinalIgnoreCase)).ToArray();
		if (array.Length != 1)
		{
			throw new InvalidDataException("This ZIP needs one Aster release.json manifest. Use an update-ready Aster package.");
		}
		ZipArchiveEntry value = array[0].Value;
		if (value.Length > 2097152)
		{
			throw new InvalidDataException("Manifest too large.");
		}
		JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
		javaScriptSerializer.MaxJsonLength = 2097152;
		UpdateManifest updateManifest = javaScriptSerializer.Deserialize<UpdateManifest>(ManifestText(value));
		if (updateManifest == null || updateManifest.Schema != 1 || updateManifest.AppId != "aster.desktop" || !Version.TryParse(updateManifest.Version, out var result) || result <= new Version(AsterUpdate.CurrentVersion) || updateManifest.Files == null || updateManifest.Files.Count == 0 || updateManifest.Files.Count > 4999)
		{
			throw new InvalidDataException("Not a compatible newer Aster release. Installed version: 2.7.");
		}
		string text = array[0].Key.Substring(0, array[0].Key.Length - "release.json".Length);
		if (text.TrimEnd('/').Contains('/'))
		{
			throw new InvalidDataException("Use a package with at most one enclosing folder.");
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (UpdateFile file in updateManifest.Files)
		{
			if (file == null || !SafeName(file.Name) || file.Name.Equals("release.json", StringComparison.OrdinalIgnoreCase) || !hashSet.Add(file.Name) || file.Size < 0 || file.Size > 67108864 || file.Sha256 == null || !Regex.IsMatch(file.Sha256, "^[a-fA-F0-9]{64}$"))
			{
				throw new InvalidDataException("Invalid release file list.");
			}
			if (!dictionary.TryGetValue(text + file.Name, out var value2) || value2.Length != file.Size)
			{
				throw new InvalidDataException("Missing or mismatched update file: " + file.Name);
			}
			using Stream stream = value2.Open();
			if (!string.Equals(FileHash(stream, file.Size), file.Sha256, StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidDataException("File integrity check failed: " + file.Name);
			}
		}
		if (dictionary.Count != updateManifest.Files.Count + 1)
		{
			throw new InvalidDataException("ZIP contains files not listed in its manifest.");
		}
		string[] array2 = new string[8] { "Aster.exe", "runtime/python.exe", "runtime/python312.zip", "AirDefense/air_defense.py", "Video/index.html", "Microsoft.Web.WebView2.Core.dll", "Microsoft.Web.WebView2.WinForms.dll", "WebView2Loader.dll" };
		foreach (string text2 in array2)
		{
			if (!hashSet.Contains(text2))
			{
				throw new InvalidDataException("Incomplete Aster package: " + text2);
			}
		}
		foreach (string item in hashSet)
		{
			string text3 = item;
			while (text3.Contains('/'))
			{
				text3 = text3.Substring(0, text3.LastIndexOf('/'));
				if (hashSet.Contains(text3))
				{
					throw new InvalidDataException("Conflicting file and directory paths.");
				}
			}
		}
		using (Stream stream2 = dictionary[text + "Aster.exe"].Open())
		{
			if (stream2.ReadByte() != 77 || stream2.ReadByte() != 90)
			{
				throw new InvalidDataException("Aster.exe is not a Windows executable.");
			}
		}
		UpdatePackage updatePackage = new UpdatePackage();
		updatePackage.Manifest = updateManifest;
		updatePackage.Prefix = text;
		updatePackage.PackageHash = hash;
		updatePackage.Bytes = num;
		return updatePackage;
	}

	public static UpdatePackage Inspect(string path)
	{
		using FileStream fileStream = OpenPackage(path);
		string hash = Hash(fileStream);
		fileStream.Position = 0L;
		using ZipArchive zip = new ZipArchive(fileStream, ZipArchiveMode.Read);
		return Read(zip, hash);
	}

	public static string Install(string path, string expectedHash, string baseFolder, string currentFolder)
	{
		using FileStream fileStream = OpenPackage(path);
		string text = Hash(fileStream);
		if (!string.Equals(text, expectedHash, StringComparison.Ordinal))
		{
			throw new InvalidDataException("The ZIP changed after review. Select it again.");
		}
		fileStream.Position = 0L;
		using ZipArchive zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read);
		UpdatePackage package = Read(zipArchive, text);
		NoLinks(baseFolder);
		Directory.CreateDirectory(baseFolder);
		string fullPath = Path.GetFullPath(Path.Combine(baseFolder, "Aster-" + new Version(package.Manifest.Version).ToString() + "-" + Guid.NewGuid().ToString("N")));
		string value = Path.GetFullPath(baseFolder).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
		if (!fullPath.StartsWith(value, StringComparison.OrdinalIgnoreCase) || Directory.Exists(fullPath))
		{
			throw new IOException("Invalid update destination.");
		}
		Directory.CreateDirectory(fullPath);
		foreach (UpdateFile item in package.Manifest.Files)
		{
			if (item.Name.Equals("home-location.json", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			string fullPath2 = Path.GetFullPath(Path.Combine(fullPath, item.Name.Replace('/', Path.DirectorySeparatorChar)));
			if (!fullPath2.StartsWith(fullPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidDataException("Invalid extraction path.");
			}
			Directory.CreateDirectory(Path.GetDirectoryName(fullPath2));
			using Stream stream = zipArchive.Entries.Single((ZipArchiveEntry e) => string.Equals(e.FullName, package.Prefix + item.Name, StringComparison.OrdinalIgnoreCase)).Open();
			using FileStream destination = new FileStream(fullPath2, FileMode.CreateNew, FileAccess.Write);
			stream.CopyTo(destination);
		}
		string[] array = new string[2] { "home-location.json", "AirDefense/places.json" };
		foreach (string text2 in array)
		{
			string text3 = Path.Combine(currentFolder, text2.Replace('/', Path.DirectorySeparatorChar));
			if (File.Exists(text3))
			{
				NoLinks(Path.GetDirectoryName(text3));
				if ((File.GetAttributes(text3) & FileAttributes.ReparsePoint) != 0 || new FileInfo(text3).Length > 262144)
				{
					throw new IOException("Cannot preserve the local location file safely.");
				}
				string text4 = Path.Combine(fullPath, text2.Replace('/', Path.DirectorySeparatorChar));
				Directory.CreateDirectory(Path.GetDirectoryName(text4));
				File.Copy(text3, text4, overwrite: true);
			}
		}
		File.WriteAllText(Path.Combine(fullPath, "installed-version.txt"), package.Manifest.Version + Environment.NewLine + "Source ZIP SHA-256: " + text);
		return fullPath;
	}
}
