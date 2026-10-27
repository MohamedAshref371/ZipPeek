using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace ZipPeek
{
    internal sealed class RangeDownloader
    {
        private const int BufferSize = 8192;
        private const int ProgressIntervalMilliseconds = 400;
        private readonly HttpClient httpClient;

        internal RangeDownloader(HttpClient httpClient)
        {
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        internal async Task<byte[]> FetchRangeAsync(string url, long start, long end, IProgress<long> progress, CancellationToken token)
        {
            ValidateRange(start, end);
            using (var output = new MemoryStream())
            {
                await CopyRangeAsync(url, start, end, output, 0, progress, token).ConfigureAwait(false);
                return output.ToArray();
            }
        }

        internal async Task DownloadRangeToFileAsync(string url, string filePath, long start, long end, IProgress<long> progress, CancellationToken token)
        {
            long expectedLength = ValidateRange(start, end);
            long existingLength = File.Exists(filePath) ? new FileInfo(filePath).Length : 0;
            if (existingLength > expectedLength)
                throw new InvalidOperationException("The file located in the _temp folder is larger than the required range.");

            token.ThrowIfCancellationRequested();
            if (existingLength > 0)
                progress?.Report(existingLength);
            if (existingLength == expectedLength)
                return;

            using (var output = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.None, BufferSize, true))
                await CopyRangeAsync(url, start + existingLength, end, output, existingLength, progress, token).ConfigureAwait(false);
        }

        private async Task CopyRangeAsync(string url, long start, long end, Stream output, long existingLength, IProgress<long> progress, CancellationToken token)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                request.Headers.Range = new RangeHeaderValue(start, end);
                using (var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    if (response.StatusCode != HttpStatusCode.PartialContent)
                        throw new InvalidOperationException("Server does not support Range requests.");

                    var range = response.Content.Headers.ContentRange;
                    // Extraction requests may include padding beyond the end of the archive.
                    long expectedEnd = range?.Length.HasValue == true ? Math.Min(end, range.Length.Value - 1) : end;
                    if (range == null || !string.Equals(range.Unit, "bytes", StringComparison.OrdinalIgnoreCase) ||
                        range.From != start || range.To != expectedEnd || expectedEnd < start)
                        throw new InvalidDataException("Server returned an unexpected Content-Range.");

                    long remaining = expectedEnd - start + 1;
                    long totalRead = existingLength;
                    var stopwatch = Stopwatch.StartNew();
                    long nextReportAt = ProgressIntervalMilliseconds;
                    var buffer = new byte[BufferSize];
                    using (var input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    {
                        while (remaining > 0)
                        {
                            int bytesRead = await input.ReadAsync(buffer, 0, (int)Math.Min(buffer.Length, remaining), token).ConfigureAwait(false);
                            if (bytesRead == 0)
                                throw new EndOfStreamException("The range download ended before all requested bytes were received.");

                            await output.WriteAsync(buffer, 0, bytesRead, token).ConfigureAwait(false);
                            remaining -= bytesRead;
                            totalRead += bytesRead;
                            if (stopwatch.ElapsedMilliseconds >= nextReportAt)
                            {
                                progress?.Report(totalRead);
                                nextReportAt = stopwatch.ElapsedMilliseconds + ProgressIntervalMilliseconds;
                            }
                        }
                    }
                    progress?.Report(totalRead);
                }
            }
        }

        private static long ValidateRange(long start, long end)
        {
            if (start < 0)
                throw new ArgumentOutOfRangeException(nameof(start));
            if (end < start || end - start == long.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(end));
            return end - start + 1;
        }
    }
}
