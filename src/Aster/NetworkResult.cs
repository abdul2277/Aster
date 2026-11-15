namespace Aster;

public class NetworkResult
{
	public string TimeUtc;

	public string Profile;

	public string Provider = "Cloudflare HTTPS";

	public string Method = "Sequential requests; full request duration";

	public double LatencyMs;

	public double VariationMs;

	public double? DownloadMbps;

	public double? UploadMbps;

	public long PayloadBytes;

	public string Summary()
	{
		return "Test completed: " + TimeUtc + "\r\n" + Profile + " · " + Provider + "\r\nHTTPS latency: " + LatencyMs.ToString("F1") + " ms · variation: " + VariationMs.ToString("F1") + " ms\r\nDownload: " + (DownloadMbps.HasValue ? (DownloadMbps.Value.ToString("F2") + " Mbps") : "not tested") + "\r\nUpload: " + (UploadMbps.HasValue ? (UploadMbps.Value.ToString("F2") + " Mbps") : "not tested") + "\r\nPayload transferred: " + ((double)PayloadBytes / 1048576.0).ToString("F1") + " MiB (excludes protocol overhead)\r\n\r\nSingle-connection HTTPS estimates to this provider. Request timing includes network/server overhead; short tests may underestimate fast links. VPNs, Wi-Fi, congestion and competing apps can affect results. Variation is successive HTTPS timing differences, not packet loss or an ICMP ping.";
	}
}
