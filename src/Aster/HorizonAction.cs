using System;

namespace Aster;

internal sealed class HorizonAction
{
	public string Name;

	public Action Run;

	public override string ToString()
	{
		return Name;
	}
}
