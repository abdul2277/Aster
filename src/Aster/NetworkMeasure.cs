using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Aster;

internal static class NetworkMeasure
{
	public static double Median(IEnumerable<double> values)
	{
		double[] array = values.OrderBy((double x) => x).ToArray();
		if (array.Length == 0)
		{
			throw new Exception("No samples.");
		}
		if (array.Length % 2 != 1)
		{
			return (array[array.Length / 2 - 1] + array[array.Length / 2]) / 2.0;
		}
		return array[array.Length / 2];
	}

	public static double Mbps(long bytes, double seconds)
	{
		if (bytes < 0 || seconds <= 0.0)
		{
			throw new ArgumentException("Invalid throughput sample.");
		}
		return (double)bytes * 8.0 / seconds / 1000000.0;
	}

	public static double Variation(double[] values)
	{
		if (values.Length >= 2)
		{
			return Enumerable.Range(1, values.Length - 1).Average((int i) => Math.Abs(values[i] - values[i - 1]));
		}
		return 0.0;
	}

	public static HttpClient Client()
	{
		ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
		HttpClientHandler httpClientHandler = new HttpClientHandler();
		httpClientHandler.AllowAutoRedirect = false;
		httpClientHandler.AutomaticDecompression = DecompressionMethods.None;
		httpClientHandler.UseCookies = false;
		HttpClient httpClient = new HttpClient(httpClientHandler);
		httpClient.BaseAddress = new Uri("https://speed.cloudflare.com/");
		httpClient.Timeout = Timeout.InfiniteTimeSpan;
		httpClient.MaxResponseContentBufferSize = 65536L;
		HttpClient httpClient2 = httpClient;
		httpClient2.DefaultRequestHeaders.AcceptEncoding.ParseAdd("identity");
		httpClient2.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
		{
			NoCache = true,
			NoStore = true
		};
		return httpClient2;
	}

	public static async Task<NetworkResult> Run(HttpClient client, NetworkProfile profile, IProgress<string> progress, CancellationToken cancel)
	{
		using CancellationTokenSource total = CancellationTokenSource.CreateLinkedTokenSource(cancel);
		total.CancelAfter(TimeSpan.FromMinutes(3.0));
		CancellationToken ct = total.Token;
		List<double> times = new List<double>();
		for (int i = 0; i < 6; i++)
		{
			progress.Report((i == 0) ? "Warming the HTTPS connection…" : ("HTTPS latency sample " + i + " / 5"));
			double duration = await Download(client, 0, ct);
			if (i > 0)
			{
				times.Add(duration * 1000.0);
			}
		}
		NetworkResult result = new NetworkResult
		{
			Profile = profile.Name,
			TimeUtc = DateTimeOffset.UtcNow.ToString("o"),
			LatencyMs = Median(times),
			VariationMs = Variation(times.ToArray())
		};
		if (profile.DownloadBytes > 0)
		{
			List<double> down = new List<double>();
			for (int j = 0; j < 3; j++)
			{
				ct.ThrowIfCancellationRequested();
				progress.Report("Download sample " + (j + 1) + " / 3 · " + profile.DownloadBytes / 1048576 + " MiB");
				down.Add(Mbps(profile.DownloadBytes, await Download(client, profile.DownloadBytes, ct)));
				result.PayloadBytes += profile.DownloadBytes;
			}
			result.DownloadMbps = Median(down);
		}
		if (profile.UploadBytes > 0)
		{
			byte[] payload = new byte[profile.UploadBytes];
			using (RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create())
			{
				randomNumberGenerator.GetBytes(payload);
			}
			List<double> up = new List<double>();
			for (int k = 0; k < 3; k++)
			{
				ct.ThrowIfCancellationRequested();
				progress.Report("Upload sample " + (k + 1) + " / 3 · " + profile.UploadBytes / 1048576 + " MiB of generated test data");
				up.Add(Mbps(payload.Length, await Upload(client, payload, ct)));
				result.PayloadBytes += payload.Length;
			}
			result.UploadMbps = Median(up);
		}
		ct.ThrowIfCancellationRequested();
		return result;
	}

	private static async Task<double> Download(HttpClient client, int bytes, CancellationToken token)
	{
		using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
		timeout.CancelAfter(TimeSpan.FromSeconds(30.0));
		CancellationToken ct = timeout.Token;
		Stopwatch watch = Stopwatch.StartNew();
		using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "__down?bytes=" + bytes + "&aster=" + Guid.NewGuid().ToString("N"));
		HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
		try
		{
			response.EnsureSuccessStatusCode();
			if (response.Content.Headers.ContentEncoding.Any())
			{
				throw new Exception("Unexpected compressed response; throughput was not measured.");
			}
			using (ct.Register(delegate
			{
				response.Dispose();
			}))
			{
				using Stream stream = await response.Content.ReadAsStreamAsync();
				byte[] buffer = new byte[65536];
				long received = 0L;
				while (true)
				{
					int num;
					int read = (num = await stream.ReadAsync(buffer, 0, buffer.Length, ct));
					if (num <= 0)
					{
						break;
					}
					received += read;
					if (received > bytes)
					{
						throw new Exception("Unexpected response size; test stopped.");
					}
				}
				if (received != bytes)
				{
					throw new Exception("Incomplete download; no throughput result recorded.");
				}
			}
			watch.Stop();
			ct.ThrowIfCancellationRequested();
			return Math.Max(1E-06, watch.Elapsed.TotalSeconds);
		}
		finally
		{
			if (response != null)
			{
				((IDisposable)response).Dispose();
			}
		}
	}

	private static async Task<double> Upload(HttpClient client, byte[] payload, CancellationToken token)
	{
		using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
		timeout.CancelAfter(TimeSpan.FromSeconds(30.0));
		CancellationToken ct = timeout.Token;
		Stopwatch watch = Stopwatch.StartNew();
		using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "__up");
		request.Content = new ByteArrayContent(payload);
		request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
		HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
		try
		{
			response.EnsureSuccessStatusCode();
			using (ct.Register(delegate
			{
				response.Dispose();
			}))
			{
				using Stream stream = await response.Content.ReadAsStreamAsync();
				byte[] buffer = new byte[4096];
				int total = 0;
				while (true)
				{
					int num;
					int read = (num = await stream.ReadAsync(buffer, 0, buffer.Length, ct));
					if (num > 0)
					{
						total += read;
						if (total > 65536)
						{
							throw new Exception("Unexpected upload response size.");
						}
						continue;
					}
					break;
				}
			}
			watch.Stop();
			ct.ThrowIfCancellationRequested();
			return Math.Max(1E-06, watch.Elapsed.TotalSeconds);
		}
		finally
		{
			if (response != null)
			{
				((IDisposable)response).Dispose();
			}
		}
	}
}
