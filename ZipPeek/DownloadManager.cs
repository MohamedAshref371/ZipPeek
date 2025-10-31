using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ZipPeek
{
    public static class DownloadManager
    {
        private static readonly HttpClient _httpClientT = new HttpClient() { Timeout = Timeout.InfiniteTimeSpan };
        private static CancellationTokenSource _cts;

        public static async Task<byte[]> FetchRangeAsync(string url, long start, long end, IProgress<long> progress)
        {
            _cts = new CancellationTokenSource();

            _httpClientT.DefaultRequestHeaders.Range = new System.Net.Http.Headers.RangeHeaderValue(start, end);

            using (var response = await _httpClientT.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, _cts.Token))
            {
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                var canReportProgress = totalBytes != -1;

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

                        if (canReportProgress)
                            progress?.Report(totalRead);
                    }

                    return ms.ToArray();
                }
            }
        }

        public static void CancelFetch()
        {
            _cts?.Cancel();
        }
    }
}
