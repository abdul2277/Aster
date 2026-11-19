using System;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace Aster;

internal static class CaseStore
{
	public static string Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Pulse", "Cases");

	public static SupportCase Read(string path)
	{
		if (new FileInfo(path).Length > 1048576)
		{
			throw new Exception("Case exceeds 1 MB.");
		}
		JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
		javaScriptSerializer.MaxJsonLength = 1048576;
		SupportCase supportCase = javaScriptSerializer.Deserialize<SupportCase>(File.ReadAllText(path));
		if (supportCase == null || supportCase.Version != 1 || string.IsNullOrWhiteSpace(supportCase.Title))
		{
			throw new Exception("Unsupported or incomplete case file.");
		}
		return supportCase;
	}

	public static string Save(string folder, SupportCase value)
	{
		Directory.CreateDirectory(folder);
		value.Saved = DateTime.Now;
		string text = Path.Combine(folder, DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N") + ".json");
		JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
		javaScriptSerializer.MaxJsonLength = 1048576;
		string text2 = javaScriptSerializer.Serialize(value);
		if (Encoding.UTF8.GetByteCount(text2) > 1048576)
		{
			throw new Exception("Case exceeds 1 MB. Shorten its notes or report.");
		}
		File.WriteAllText(text + ".tmp", text2, Encoding.UTF8);
		File.Move(text + ".tmp", text);
		return text;
	}
}
