using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace Aster;

internal static class AuraProfile
{
	public static string PathName => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Pulse", "Aura.xml");

	public static void Eco()
	{
		EcoAt(PathName);
	}

	public static void EcoAt(string p)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(p));
		XDocument xDocument;
		if (File.Exists(p))
		{
			using (XmlReader reader = XmlReader.Create(p, new XmlReaderSettings
			{
				DtdProcessing = DtdProcessing.Prohibit,
				XmlResolver = null
			}))
			{
				xDocument = XDocument.Load(reader);
			}
			File.Copy(p, p + ".before-eco", overwrite: true);
		}
		else
		{
			xDocument = new XDocument(new XElement("Settings"));
			xDocument.Save(p + ".before-eco");
		}
		string[][] array = new string[5][]
		{
			new string[2] { "Fps", "15" },
			new string[2] { "Density", "40" },
			new string[2] { "Volume", "0" },
			new string[2] { "PauseBattery", "true" },
			new string[2] { "PauseFullscreen", "true" }
		};
		foreach (string[] array2 in array)
		{
			xDocument.Root.SetElementValue(array2[0], array2[1]);
		}
		Write(xDocument, p);
	}

	public static void Undo()
	{
		UndoAt(PathName);
	}

	public static void UndoAt(string p)
	{
		if (!File.Exists(p + ".before-eco"))
		{
			throw new Exception("No saved pre-Eco settings were found.");
		}
		XDocument doc;
		using (XmlReader reader = XmlReader.Create(p + ".before-eco", new XmlReaderSettings
		{
			DtdProcessing = DtdProcessing.Prohibit,
			XmlResolver = null
		}))
		{
			doc = XDocument.Load(reader);
		}
		Write(doc, p);
	}

	private static void Write(XDocument doc, string p)
	{
		string text = p + ".profile.tmp";
		doc.Save(text);
		if (File.Exists(p))
		{
			File.Replace(text, p, null);
		}
		else
		{
			File.Move(text, p);
		}
	}
}
