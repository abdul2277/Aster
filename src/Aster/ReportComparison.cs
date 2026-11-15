using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Aster;

internal static class ReportComparison
{
	public static Dictionary<string, string> Parse(string text)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
		string[] array = text.Split('\n');
		foreach (string text2 in array)
		{
			string text3 = text2.Trim();
			int num = text3.IndexOf(": ", StringComparison.Ordinal);
			if (num > 0 && text3.Substring(0, num).Contains(" · "))
			{
				string text4 = text3.Substring(0, num);
				if (dictionary.ContainsKey(text4))
				{
					throw new Exception("Report contains duplicate specification keys: " + text4);
				}
				dictionary[text4] = text3.Substring(num + 2);
			}
		}
		if (dictionary.Count == 0)
		{
			throw new Exception("No Aster hardware fields were found. Choose an exported Hardware report.");
		}
		return dictionary;
	}

	public static List<SpecDifference> Compare(string before, string after)
	{
		Dictionary<string, string> a = Parse(before);
		Dictionary<string, string> b = Parse(after);
		return (from k in a.Keys.Union(b.Keys)
			orderby k
			select k).Select(delegate(string k)
		{
			string value;
			bool flag = a.TryGetValue(k, out value);
			string value2;
			bool flag2 = b.TryGetValue(k, out value2);
			return new SpecDifference
			{
				Field = k,
				Before = (flag ? value : "—"),
				After = (flag2 ? value2 : "—"),
				Change = ((!flag) ? "Added" : ((!flag2) ? "Removed" : ((value == value2) ? "Unchanged" : "Changed")))
			};
		}).ToList();
	}

	public static string Read(string path)
	{
		if (new FileInfo(path).Length > 1048576)
		{
			throw new Exception("Hardware reports must be smaller than 1 MB.");
		}
		string text = File.ReadAllText(path);
		Parse(text);
		return text;
	}
}
