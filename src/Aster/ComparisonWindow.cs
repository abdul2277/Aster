using System.Collections.Generic;
using System.Linq;

namespace Aster;

internal class ComparisonWindow
{
	public List<ComparisonReading> Readings = new List<ComparisonReading>();

	public string Describe()
	{
		if (Readings.Count == 0)
		{
			return "Not captured";
		}
		return Readings[0].Time.ToString("HH:mm:ss") + " – " + Readings.Last().Time.ToString("HH:mm:ss") + "\r\nCPU average " + Readings.Average((ComparisonReading x) => x.Cpu).ToString("F1") + "% · peak " + Readings.Max((ComparisonReading x) => x.Cpu).ToString("F1") + "%\r\nRAM average " + Readings.Average((ComparisonReading x) => x.Ram).ToString("F1") + "% · peak " + Readings.Max((ComparisonReading x) => x.Ram).ToString("F1") + "%\r\nAverage process count " + Readings.Average((ComparisonReading x) => x.Processes).ToString("F1");
	}
}
