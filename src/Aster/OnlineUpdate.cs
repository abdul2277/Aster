using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Aster;

internal static class OnlineUpdate
{
	public const string Feed = "https://github.com/abdul2277/Aster/releases/latest/download/aster-update.json";

	public const long Limit = 262144000L;

	internal static Func<HttpClient> TestClientFactory;

	public static Uri Https(string value)
	{
		if (!Uri.TryCreate(value, UriKind.Absolute, out var result) || result.Scheme != "https" || result.UserInfo.Length > 0 || !result.IsDefaultPort)
		{
			throw new InvalidDataException("The update address must use HTTPS.");
		}
		return result;
	}

	public static OnlineRelease Verify(string json)
	{
		if (json.Length > 65536)
		{
			throw new InvalidDataException("Update information is too large.");
		}
		JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
		javaScriptSerializer.MaxJsonLength = 65536;
		JavaScriptSerializer javaScriptSerializer2 = javaScriptSerializer;
		SignedUpdate signedUpdate = javaScriptSerializer2.Deserialize<SignedUpdate>(json);
		if (signedUpdate == null || signedUpdate.Payload == null || signedUpdate.Signature == null)
		{
			throw new InvalidDataException("Missing release signature.");
		}
		byte[] array = Convert.FromBase64String(signedUpdate.Payload);
		byte[] signature = Convert.FromBase64String(signedUpdate.Signature);
		using (RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider())
		{
			rSACryptoServiceProvider.PersistKeyInCsp = false;
			using (StreamReader streamReader = new StreamReader(typeof(OnlineUpdate).Assembly.GetManifestResourceStream("AsterUpdateKey")))
			{
				rSACryptoServiceProvider.FromXmlString(streamReader.ReadToEnd());
			}
			if (!rSACryptoServiceProvider.VerifyData(array, CryptoConfig.MapNameToOID("SHA256"), signature))
			{
				throw new InvalidDataException("Release signature is not trusted.");
			}
		}
		OnlineRelease onlineRelease = javaScriptSerializer2.Deserialize<OnlineRelease>(new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(array));
		if (onlineRelease == null || onlineRelease.AppId != "aster.desktop" || !Version.TryParse(onlineRelease.Version, out var _) || onlineRelease.Size <= 0 || onlineRelease.Size > 262144000 || onlineRelease.Sha256 == null || !Regex.IsMatch(onlineRelease.Sha256, "^[a-fA-F0-9]{64}$") || (onlineRelease.Notes ?? "").Length > 4000)
		{
			throw new InvalidDataException("Invalid release information.");
		}
		Uri uri = Https(onlineRelease.Url);
		if (uri.Host != "github.com" || !uri.AbsolutePath.StartsWith("/abdul2277/Aster/releases/download/", StringComparison.Ordinal))
		{
			throw new InvalidDataException("Release must belong to the configured Aster repository.");
		}
		return onlineRelease;
	}

	private static HttpClient Client()
	{
		if (TestClientFactory != null)
		{
			return TestClientFactory();
		}
		ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
		HttpClientHandler httpClientHandler = new HttpClientHandler();
		httpClientHandler.AllowAutoRedirect = false;
		HttpClient httpClient = new HttpClient(httpClientHandler);
		httpClient.Timeout = TimeSpan.FromSeconds(45.0);
		httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Aster/2.6");
		return httpClient;
	}

	private static async Task<HttpResponseMessage> Get(HttpClient client, Uri uri, CancellationToken token)
	{
		for (int i = 0; i < 6; i++)
		{
			HttpResponseMessage response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, token);
			int code = (int)response.StatusCode;
			if (code >= 300 && code < 400)
			{
				Uri location = response.Headers.Location;
				response.Dispose();
				if (location == null)
				{
					throw new IOException("Update redirect is missing its destination.");
				}
				uri = Https(new Uri(uri, location).AbsoluteUri);
				continue;
			}
			try
			{
				if (response.StatusCode == HttpStatusCode.NotFound)
				{
					throw new UpdateFeedMissingException();
				}
				response.EnsureSuccessStatusCode();
				return response;
			}
			catch
			{
				response.Dispose();
				throw;
			}
		}
		throw new IOException("Too many update redirects.");
	}

	public static async Task<OnlineRelease> Check(CancellationToken token)
	{
		using HttpClient client = Client();
		using HttpResponseMessage response = await Get(client, Https("https://github.com/abdul2277/Aster/releases/latest/download/aster-update.json"), token);
		using Stream input = await response.Content.ReadAsStreamAsync();
		using MemoryStream output = new MemoryStream();
		byte[] buffer = new byte[8192];
		while (true)
		{
			int num;
			int n = (num = await input.ReadAsync(buffer, 0, buffer.Length, token));
			if (num <= 0)
			{
				break;
			}
			if (output.Length + n > 65536)
			{
				throw new IOException("Update information exceeds its size limit.");
			}
			output.Write(buffer, 0, n);
		}
		return Verify(new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(output.ToArray()));
	}

	public static async Task<string> Download(OnlineRelease release, string folder, IProgress<int> progress, CancellationToken token)
	{
		Directory.CreateDirectory(folder);
		string path = Path.Combine(folder, "Aster-" + Guid.NewGuid().ToString("N") + ".zip");
		try
		{
			using (HttpClient client = Client())
			{
				using HttpResponseMessage response = await Get(client, Https(release.Url), token);
				if (response.Content.Headers.ContentLength.HasValue && response.Content.Headers.ContentLength.Value != release.Size)
				{
					throw new IOException("Download size does not match the signed release.");
				}
				using Stream input = await response.Content.ReadAsStreamAsync();
				using FileStream output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536, useAsync: true);
				long total = 0L;
				byte[] buffer = new byte[65536];
				while (true)
				{
					int num;
					int n = (num = await input.ReadAsync(buffer, 0, buffer.Length, token));
					if (num <= 0)
					{
						break;
					}
					total += n;
					if (total > release.Size)
					{
						throw new IOException("Download exceeds its signed size.");
					}
					await output.WriteAsync(buffer, 0, n, token);
					progress?.Report((int)(total * 100 / release.Size));
				}
				if (total != release.Size)
				{
					throw new IOException("Download was incomplete.");
				}
			}
			token.ThrowIfCancellationRequested();
			using FileStream stream = File.OpenRead(path);
			if (!string.Equals(AsterUpdate.Hash(stream), release.Sha256, StringComparison.OrdinalIgnoreCase))
			{
				throw new IOException("Downloaded update failed its integrity check.");
			}
			return path;
		}
		catch
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
			throw;
		}
	}
}
