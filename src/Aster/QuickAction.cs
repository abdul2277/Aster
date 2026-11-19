using System;

namespace Aster;

internal class QuickAction
{
	public string Label;

	public Action Run;

	public override string ToString()
	{
		return Label;
	}
}
