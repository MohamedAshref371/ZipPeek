using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ZipPeek
{
    public static class DownloadManager
    {
        private static readonly HttpClient HttpClient = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        private static readonly RangeDownloader Downloader = new RangeDownloader(HttpClient);
        private static readonly object SyncRoot = new object();
        private static readonly HashSet<CancellationTokenSource> ActiveDownloads = new HashSet<CancellationTokenSource>();

        public static Task<byte[]> FetchRangeAsync(string url, long start, long end, IProgress<long> progress)
        {
            return RunAsync(token => Downloader.FetchRangeAsync(url, start, end, progress, token));
        }

        public static async Task DownloadRangeToFileAsync(string url, string filePath, long start, long end, IProgress<long> progress)
        {
            await RunAsync(async token =>
            {
                await Downloader.DownloadRangeToFileAsync(url, filePath, start, end, progress, token).ConfigureAwait(false);
                return true;
            }).ConfigureAwait(false);
        }

        private static async Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> operation)
        {
            using (var source = new CancellationTokenSource())
            {
                lock (SyncRoot)
                    ActiveDownloads.Add(source);
                try
                {
                    return await operation(source.Token).ConfigureAwait(false);
                }
                finally
                {
                    lock (SyncRoot)
                        ActiveDownloads.Remove(source);
                }
            }
        }

        public static void CancelFetch()
        {
            // Removal and disposal cannot race cancellation.
            lock (SyncRoot)
            {
                var sources = new CancellationTokenSource[ActiveDownloads.Count];
                ActiveDownloads.CopyTo(sources);
                foreach (var source in sources)
                    source.Cancel();
            }
        }
    }
}
