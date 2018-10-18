using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HttpStreamingClient
{
	class Program
	{
		static void Main(string[] args)
		{
			var x = ReadUrl();
			x.Wait();
		}

		private async static Task ReadUrl()
		{
			try
			{
				Uri uri = new Uri("https://www.example.com");

				HttpClient httpClient = new HttpClient();
				string content;

				// Streaming method - read a partial string but wait for the headers to be read before completion
				using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri))
				using (var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead))
				{
					content = await httpResponseMessage.ReadContentAsStreamAsync().ConfigureAwait(false);
				}

				Console.WriteLine(content);
				Console.ReadLine();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
		}

	}

	public static class ReadContentAsStreamExtension
	{
		private const int MaxContentSize = 1 * 1024;

		public static async Task<string> ReadContentAsStreamAsync(this HttpResponseMessage httpResponseMessage)
		{
			return httpResponseMessage.Content == null ?
				String.Empty :
				await StreamContentAsync(httpResponseMessage);
		}

		private static async Task<string> StreamContentAsync(HttpResponseMessage httpResponseMessage)
		{
			string content = String.Empty;

			using (var stream = await httpResponseMessage.Content.ReadAsStreamAsync())
			{
				content = await ReadDataFromStream(stream);

				await CheckForDataRemainingOnTheStream(stream);

				stream.Close();
			}

			return content;
		}

		private static async Task<string> ReadDataFromStream(System.IO.Stream stream)
		{
			const int bufferOffset = 0;
			byte[] bytes = new byte[MaxContentSize];

			int bytesCount = await stream.ReadAsync(bytes, bufferOffset, MaxContentSize);
			return Encoding.UTF8.GetString(bytes, 0, bytesCount);
		}

		private static async Task CheckForDataRemainingOnTheStream(System.IO.Stream stream)
		{
			int bufferOffset = 0;
			byte[] remainingData = new byte[1];
			int bytesStillToRead = await stream.ReadAsync(remainingData, bufferOffset, 1);

			if (bytesStillToRead > 0)
			{
				// Log error
				Console.WriteLine($"Size of http response body exceeds {MaxContentSize} bytes, the response has been truncated");
			}
		}
	}
}
