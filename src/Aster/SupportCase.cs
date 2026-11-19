using System;

namespace Aster;

public class SupportCase
{
	public int Version = 1;

	public string Title;

	public string Category;

	public string Symptom;

	public string Report;

	public string Question;

	public string Answer;

	public string Notes;

	public DateTime Saved;

	public override string ToString()
	{
		return Saved.ToString("g") + " · " + Title;
	}
}
