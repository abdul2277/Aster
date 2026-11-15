using System;
using System.Globalization;
using System.Linq;

namespace Aster;

internal static class GreetingClock
{
	public static string Salutation(int hour)
	{
		if (hour < 5 || hour >= 12)
		{
			if (hour < 12 || hour >= 17)
			{
				if (hour < 17 || hour >= 22)
				{
					return "A peaceful night";
				}
				return "Good evening";
			}
			return "Good afternoon";
		}
		return "Good morning";
	}

	public static string Name(string configured, string fallback)
	{
		string text = (string.IsNullOrWhiteSpace(configured) ? fallback : configured);
		text = new string((text ?? "").Where((char c) => !char.IsControl(c)).ToArray()).Trim();
		if (text.Length != 0)
		{
			return text.Substring(0, Math.Min(40, text.Length));
		}
		return "friend";
	}

	public static GreetingData Format(DateTimeOffset utc, WorkspaceOptions options, string fallback)
	{
		DateTimeOffset dateTimeOffset;
		string text;
		if (options.RiyadhTime)
		{
			dateTimeOffset = utc.ToOffset(TimeSpan.FromHours(3.0));
			text = "Riyadh · UTC+03:00";
		}
		else
		{
			dateTimeOffset = TimeZoneInfo.ConvertTime(utc, TimeZoneInfo.Local);
			text = "Windows local · UTC" + dateTimeOffset.ToString("zzz", CultureInfo.InvariantCulture);
		}
		GreetingData greetingData = new GreetingData();
		greetingData.Title = Salutation(dateTimeOffset.Hour) + ", " + Name(options.DisplayName, fallback) + ".";
		greetingData.DateLine = dateTimeOffset.ToString("dddd, dd MMMM yyyy", CultureInfo.InvariantCulture) + "  ·  " + dateTimeOffset.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + "  ·  " + dateTimeOffset.ToString(options.Clock24 ? "HH:mm:ss" : "hh:mm:ss tt", CultureInfo.InvariantCulture) + "  ·  " + text;
		greetingData.Hijri = "";
		greetingData.Occasion = "Make a little room for something good today.";
		return greetingData;
	}
}
