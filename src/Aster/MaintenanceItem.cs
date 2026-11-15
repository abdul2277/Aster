using System;

namespace Aster;

internal class MaintenanceItem
{
	public string Title = "";

	public DateTime Due = DateTime.Today;

	public int RepeatDays;

	public bool Done;

	public void Complete(DateTime today)
	{
		if (RepeatDays > 0)
		{
			Due = today.Date.AddDays(Math.Min(365, RepeatDays));
			Done = false;
		}
		else
		{
			Done = true;
		}
	}

	public override string ToString()
	{
		return (Done ? "DONE" : ((Due.Date <= DateTime.Today) ? "DUE" : "PLANNED")) + " · " + Title + " · " + Due.ToString("yyyy-MM-dd") + ((RepeatDays > 0) ? (" · every " + RepeatDays + " days") : "");
	}
}
