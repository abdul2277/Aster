using System.Collections.Generic;

namespace Aster;

public class PersonalData
{
	public int Version = 1;

	public string Note = "";

	public List<PersonalTask> Tasks = new List<PersonalTask>();
}
