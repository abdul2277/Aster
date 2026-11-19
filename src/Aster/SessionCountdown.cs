using System;

namespace Aster;

internal class SessionCountdown
{
	private TimeSpan anchor;

	public TimeSpan Remaining { get; private set; }

	public bool Running { get; private set; }

	public int Completed { get; private set; }

	public TimeSpan Duration { get; private set; }

	public SessionCountdown()
	{
		Reset(25);
	}

	public void Reset(int minutes)
	{
		Running = false;
		TimeSpan duration = (Remaining = TimeSpan.FromMinutes(Math.Max(1, Math.Min(180, minutes))));
		Duration = duration;
	}

	public void Start(TimeSpan now)
	{
		if (!(Remaining <= TimeSpan.Zero))
		{
			anchor = now;
			Running = true;
		}
	}

	public bool Update(TimeSpan now)
	{
		if (!Running)
		{
			return false;
		}
		TimeSpan timeSpan = now - anchor;
		anchor = now;
		if (timeSpan < TimeSpan.Zero)
		{
			timeSpan = TimeSpan.Zero;
		}
		Remaining -= timeSpan;
		if (Remaining <= TimeSpan.Zero)
		{
			Remaining = TimeSpan.Zero;
			Running = false;
			Completed++;
			return true;
		}
		return false;
	}

	public bool Pause(TimeSpan now)
	{
		bool result = Update(now);
		Running = false;
		return result;
	}
}
