namespace Aster;

internal class NetworkProfile
{
	public string Name;

	public int DownloadBytes;

	public int UploadBytes;

	public static NetworkProfile[] All = new NetworkProfile[3]
	{
		new NetworkProfile
		{
			Name = "Quick · about 18 MiB",
			DownloadBytes = 4194304,
			UploadBytes = 2097152
		},
		new NetworkProfile
		{
			Name = "Standard · about 72 MiB",
			DownloadBytes = 16777216,
			UploadBytes = 8388608
		},
		new NetworkProfile
		{
			Name = "Latency only · minimal data"
		}
	};

	public long Budget => 3L * (long)(DownloadBytes + UploadBytes);

	public override string ToString()
	{
		return Name;
	}
}
