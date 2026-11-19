using System.Linq;
using System.Text.RegularExpressions;

namespace Aster;

internal class ErrorHelp
{
	private const string Root = "https://learn.microsoft.com/en-us/troubleshoot/";

	public string Code;

	public string Meaning;

	public string Next;

	public string Url;

	public static ErrorHelp[] All = new ErrorHelp[5]
	{
		new ErrorHelp
		{
			Code = "0x80070005",
			Meaning = "Access denied",
			Next = "Identify which app or update failed and the affected resource. Do not broadly change permissions or disable security.",
			Url = "https://learn.microsoft.com/en-us/troubleshoot/windows-client/installing-updates-features-roles/troubleshoot-windows-update-error-0x80070005"
		},
		new ErrorHelp
		{
			Code = "0x80070002",
			Meaning = "File not found",
			Next = "Record the failing operation and missing-file path from its log. The code alone does not identify which file or why it is missing.",
			Url = "https://learn.microsoft.com/en-us/troubleshoot/windows-server/installing-updates-features-roles/troubleshoot-windows-update-error-0x80070002"
		},
		new ErrorHelp
		{
			Code = "0x80070070",
			Meaning = "Insufficient disk space",
			Next = "Review free space on the affected volume and Windows Storage settings. Review files before deleting them.",
			Url = "https://learn.microsoft.com/en-us/troubleshoot/windows-server/installing-updates-features-roles/troubleshoot-windows-update-error-0x80070070"
		},
		new ErrorHelp
		{
			Code = "0x800f081f",
			Meaning = "Servicing source missing",
			Next = "Identify the feature or servicing operation and Windows version. A compatible repair source may be needed; do not use an arbitrary ISO.",
			Url = "https://learn.microsoft.com/en-us/troubleshoot/windows-client/application-management/dotnet-framework-35-installation-error"
		},
		new ErrorHelp
		{
			Code = "0x800f0906",
			Meaning = "Servicing source download failed",
			Next = "Check the operation, connectivity and managed update policies. On a managed PC, ask IT before changing its update source.",
			Url = "https://learn.microsoft.com/en-us/troubleshoot/windows-client/application-management/dotnet-framework-35-installation-error"
		}
	};

	public override string ToString()
	{
		return Code + " · " + Meaning;
	}

	public static string Summarize(string text)
	{
		string[] array = (from Match x in Regex.Matches(text, "\\b0x[0-9a-fA-F]{8}\\b", RegexOptions.IgnoreCase)
			select x.Value.ToLowerInvariant()).Distinct().ToArray();
		string[] array2 = (from Match x in Regex.Matches(text, "Exit code:\\s*(-?\\d+)", RegexOptions.IgnoreCase)
			select x.Groups[1].Value).ToArray();
		return "LOCAL LOG INDEX · pattern matching, not AI diagnosis\r\nRecorded exit codes: " + ((array2.Length == 0) ? "none found" : string.Join(", ", array2)) + "\r\nZero alone does not prove the original symptom is fixed. Missing codes do not prove success.\r\n\r\n" + ((array.Length == 0) ? "No hexadecimal error codes found." : string.Join("\r\n", array.Select(delegate(string c)
		{
			ErrorHelp errorHelp = All.FirstOrDefault((ErrorHelp x) => x.Code == c);
			return c + " · " + ((errorHelp == null) ? "not in the offline library" : errorHelp.Meaning);
		}))) + "\r\n\r\nORIGINAL LOG\r\n" + text;
	}
}
