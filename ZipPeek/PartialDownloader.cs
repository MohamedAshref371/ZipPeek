using System;
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
    }
}
