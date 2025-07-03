using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace ZipPeek
{
    internal static class PartialDownloader
    {
        private static readonly HttpClient client = new HttpClient();
        public static async Task<long> GetRemoteFileSizeAsync(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Head, url);
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (response.IsSuccessStatusCode && response.Content.Headers.ContentLength.HasValue)
                return response.Content.Headers.ContentLength.Value;
            
            return 0;
        }

        public static async Task<bool> DownloadPartialAsync(string url, string outputFile, long startByte, long endByte)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(startByte, endByte);

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (response.IsSuccessStatusCode && response.Content.Headers.ContentLength > 0)
            {
                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var fs = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
                {
                    await stream.CopyToAsync(fs);
                }

                return true;
            }

            return false;
        }

    }
}
