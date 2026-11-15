using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace Aster;

internal static class NetworkHistory
{
	public static string FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Pulse", "NetworkHistory.json");

	public static List<NetworkResult> Read()
	{
		if (!File.Exists(FileName))
		{
			return new List<NetworkResult>();
		}
		if (new FileInfo(FileName).Length > 1048576)
		{
			throw new Exception("History file exceeds 1 MB.");
		}
		JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
		javaScriptSerializer.MaxJsonLength = 1048576;
		List<NetworkResult> list = javaScriptSerializer.Deserialize<List<NetworkResult>>(File.ReadAllText(FileName));
		if (list == null || list.Count > 50 || list.Any((NetworkResult x) => x == null))
		{
			throw new Exception("Invalid history file.");
		}
		return list;
	}

	public static void Add(NetworkResult value)
	{
		List<NetworkResult> list = Read();
		list.Insert(0, value);
		list = list.Take(50).ToList();
		Directory.CreateDirectory(Path.GetDirectoryName(FileName));
		string text = FileName + ".tmp";
		File.WriteAllText(text, new JavaScriptSerializer().Serialize(list), Encoding.UTF8);
		if (File.Exists(FileName))
		{
			File.Replace(text, FileName, null);
		}
		else
		{
			File.Move(text, FileName);
		}
	}
}
