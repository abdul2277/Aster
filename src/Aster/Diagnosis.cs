using System;
using System.Collections.Generic;
using System.Linq;

namespace Aster;

internal class Diagnosis
{
	public DateTime Time = DateTime.Now;

	public double Cpu;

	public double Ram;

	public double FreeGb;

	public double FreePercent;

	public bool Network;

	public bool NetworkKnown;

	public bool Reboot;

	public int Errors;

	public List<string> Facts = new List<string>();

	public List<string> Limits = new List<string>();

	public List<string> Advice = new List<string>();

	public string Report()
	{
		return "LOCAL DIAGNOSTIC REPORT · " + Time.ToString("g") + "\r\nThis report uses measured checks and explicit rules, not an AI model.\r\n\r\nOBSERVATIONS\r\n" + string.Join("\r\n", Facts.Select((string f) => "• " + f)) + "\r\n\r\nRECOMMENDED NEXT STEPS\r\n" + string.Join("\r\n", Advice.Select((string f) => "• " + f)) + "\r\n\r\nLIMITS\r\n" + string.Join("\r\n", Limits.Select((string f) => "• " + f)) + "\r\nA short sample is not a complete diagnosis. Recent error events do not establish a root cause.";
	}
}
