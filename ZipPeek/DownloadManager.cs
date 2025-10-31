using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ZipPeek
{
    public static class DownloadManager
    {
        private static readonly HttpClient _httpClient = new HttpClient() { Timeout = Timeout.InfiniteTimeSpan };
        private static CancellationTokenSource _cts;

        public static async Task<byte[]> FetchRangeAsync(string url, long start, long end, IProgress<long> progress)
        {
            _cts = new CancellationTokenSource();

            _httpClient.DefaultRequestHeaders.Range = new System.Net.Http.Headers.RangeHeaderValue(start, end);

            using (var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, _cts.Token))
            {
                response.EnsureSuccessStatusCode();

                //var totalBytes = response.Content.Headers.ContentLength ?? -1L;

                using (var ms = new MemoryStream())
                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    var buffer = new byte[8192];
                    long totalRead = 0;
                    int bytesRead;

                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, _cts.Token)) > 0)
                    {
                        await ms.WriteAsync(buffer, 0, bytesRead, _cts.Token);
                        totalRead += bytesRead;

                        progress?.Report(totalRead);
                    }

                    return ms.ToArray();
                }
            }
        }

        public static async Task DownloadWithResumeAsync(string url, string filePath, long start, long end, IProgress<double> progress = null)
        {
            _cts = new CancellationTokenSource();
            
            long existingLength = 0;
            if (File.Exists(filePath))
                existingLength = new FileInfo(filePath).Length;

            // طلب التحميل من البايت اللي بعد الموجود
            _httpClient.DefaultRequestHeaders.Range = new System.Net.Http.Headers.RangeHeaderValue(start + existingLength, end);

            using (var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, _cts.Token))
            {
                response.EnsureSuccessStatusCode();

                long totalRead = existingLength;
                byte[] buffer = new byte[8192];
                int bytesRead;

                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var fs = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.None, 8192, true))
                {
                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, _cts.Token)) > 0)
                    {
                        await fs.WriteAsync(buffer, 0, bytesRead, _cts.Token);
                        totalRead += bytesRead;

                        progress?.Report(totalRead);
                    }
                }
            }
        }

        public static void CancelFetch()
        {
            _cts?.Cancel();
        }
    }
}
