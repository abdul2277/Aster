using System.Collections.Generic;

namespace Aster;

internal class Table
{
	public string[] Columns;

	public List<string[]> Rows = new List<string[]>();

	public string Note = "";

	public Table(params string[] columns)
	{
		Columns = columns;
	}
}
