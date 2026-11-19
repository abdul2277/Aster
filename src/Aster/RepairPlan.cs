using System.Linq;

namespace Aster;

internal class RepairPlan
{
	public string Id;

	public string Title;

	public string Description;

	public string[] Jobs;

	public static RepairPlan[] All = new RepairPlan[3]
	{
		new RepairPlan
		{
			Id = "scan",
			Title = "Windows scan · no repair",
			Description = "Checks image health, verifies protected files, and checks the filesystem without repair switches. May take time; administrator access is required.",
			Jobs = new string[3] { "image-check", "files-check", "disk-check" }
		},
		new RepairPlan
		{
			Id = "windows",
			Title = "Repair Windows files",
			Description = "Runs image repair, protected-file repair, then protected-file verification. Can download replacement files. Stops on a failed step or restart-required exit code. No automatic restart.",
			Jobs = new string[3] { "image-repair", "files-repair", "files-check" }
		},
		new RepairPlan
		{
			Id = "dns",
			Title = "Refresh DNS cache",
			Description = "Clears the DNS resolver cache. It does not reset adapters, change DNS servers or prove internet access is restored.",
			Jobs = new string[1] { "dns-flush" }
		}
	};

	public static RepairPlan Find(string id)
	{
		return All.FirstOrDefault((RepairPlan p) => p.Id == id);
	}

	public override string ToString()
	{
		return Title;
	}

	public string Review()
	{
		return Title + "\r\n\r\n" + Description + "\r\n\r\n" + string.Join("\r\n", Jobs.Select(delegate(string id, int i)
		{
			RecoveryJob recoveryJob = RecoveryJob.Find(id);
			return i + 1 + ". " + recoveryJob.Title + "\r\n   " + recoveryJob.Exe + " " + recoveryJob.Arguments;
		})) + "\r\n\r\nApprove this exact plan and request administrator access?";
	}
}
