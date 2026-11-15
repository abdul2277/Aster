using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace Aster;

internal static class PersonalStore
{
	public static string FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Pulse", "PersonalWorkspace.json");

	public static void Validate(PersonalData data)
	{
		if (data == null || data.Version != 1 || data.Note == null || data.Note.Length > 50000 || data.Tasks == null || data.Tasks.Count > 200 || data.Tasks.Any((PersonalTask t) => t == null || string.IsNullOrWhiteSpace(t.Title) || t.Title.Length > 300))
		{
			throw new Exception("Invalid personal workspace file. The original file has been left untouched.");
		}
	}

	public static PersonalData Read()
	{
		if (!File.Exists(FileName))
		{
			return new PersonalData();
		}
		if (new FileInfo(FileName).Length > 1048576)
		{
			throw new Exception("Personal workspace file exceeds 1 MB.");
		}
		JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
		javaScriptSerializer.MaxJsonLength = 1048576;
		PersonalData personalData = javaScriptSerializer.Deserialize<PersonalData>(File.ReadAllText(FileName));
		Validate(personalData);
		return personalData;
	}

	public static void Save(PersonalData data)
	{
		Validate(data);
		Directory.CreateDirectory(Path.GetDirectoryName(FileName));
		string text = FileName + ".tmp";
		File.WriteAllText(text, new JavaScriptSerializer
		{
			MaxJsonLength = 1048576
		}.Serialize(data), Encoding.UTF8);
		if (File.Exists(FileName))
		{
			File.Replace(text, FileName, FileName + ".backup");
		}
		else
		{
			File.Move(text, FileName);
		}
	}
}
