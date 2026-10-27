using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace ZipPeek
{
    // Used only on a background worker: the native archive engine requires synchronous Read/Seek.
    internal sealed class HttpRangeStream : Stream
    {
        private const int MaxRequestSize = 1024 * 1024;
        private readonly HttpClient client;
        private readonly string url;
        private readonly FileStream cache;
        private readonly SortedList<long, Segment> segments = new SortedList<long, Segment>();
        private readonly Stopwatch progressTimer = Stopwatch.StartNew();
        private EntityTagHeaderValue entityTag;
        private DateTimeOffset? lastModified;
        private long position;
        private long length;
        private bool disposed;

        internal CancellationToken CancellationToken { get; set; }
        internal IProgress<long> Progress { get; set; }
        internal long DownloadedBytes { get; private set; }

        private HttpRangeStream(HttpClient client, string url, CancellationToken token, IProgress<long> progress)
        {
            this.client = client;
            this.url = url;
            CancellationToken = token;
            Progress = progress;
            string cachePath = Path.Combine(Path.GetTempPath(), "ZipPeek-" + Guid.NewGuid().ToString("N") + ".cache");
            cache = new FileStream(cachePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None,
                8192, FileOptions.DeleteOnClose | FileOptions.RandomAccess);
        }

        internal static async Task<HttpRangeStream> OpenAsync(HttpClient client, string url, CancellationToken token, IProgress<long> progress)
        {
            var result = new HttpRangeStream(client, url, token, progress);
            try
            {
                await result.FetchAsync(0, 31, true).ConfigureAwait(false);
                return result;
            }
            catch { result.Dispose(); throw; }
        }

        private async Task FetchAsync(long start, long end, bool initial = false)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                request.Headers.Range = new RangeHeaderValue(start, end);
                request.Headers.AcceptEncoding.ParseAdd("identity");
                if (entityTag != null && !entityTag.IsWeak)
                    request.Headers.IfRange = new RangeConditionHeaderValue(entityTag);
                else if (lastModified.HasValue)
                    request.Headers.IfRange = new RangeConditionHeaderValue(lastModified.Value);

                using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, CancellationToken).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    if (response.StatusCode != HttpStatusCode.PartialContent)
                        throw new InvalidDataException("The server must support HTTP Range requests and the archive must remain unchanged.");
                    var range = response.Content.Headers.ContentRange;
                    if (range == null || range.Unit != "bytes" || !range.Length.HasValue || range.Length <= 0 ||
                        range.From != start || range.To != Math.Min(end, range.Length.Value - 1))
                        throw new InvalidDataException("The server returned an invalid Content-Range.");
                    if (response.Content.Headers.ContentEncoding.Any(x => !string.Equals(x, "identity", StringComparison.OrdinalIgnoreCase)))
                        throw new InvalidDataException("Encoded HTTP responses cannot be used for remote archive access.");
                    if (!initial && (range.Length != length ||
                        (entityTag != null && !entityTag.Equals(response.Headers.ETag)) ||
                        (lastModified.HasValue && lastModified != response.Content.Headers.LastModified)))
                        throw new InvalidDataException("The remote archive changed. Read the archive again before downloading.");
                    if (initial)
                    {
                        length = range.Length.Value;
                        entityTag = response.Headers.ETag;
                        lastModified = response.Content.Headers.LastModified;
                    }

                    int size = checked((int)(range.To.Value - start + 1));
                    var data = new byte[size];
                    using (var input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    {
                        int read = 0;
                        while (read < size)
                        {
                            int count = await input.ReadAsync(data, read, size - read, CancellationToken).ConfigureAwait(false);
                            if (count == 0) throw new EndOfStreamException("The range download is incomplete.");
                            read += count;
                            ReportProgress(DownloadedBytes + read);
                        }
                    }
                    CancellationToken.ThrowIfCancellationRequested();
                    long cacheOffset = cache.Length;
                    cache.Position = cacheOffset;
                    cache.Write(data, 0, size);
                    segments.Add(start, new Segment { Start = start, End = start + size, CacheOffset = cacheOffset });
                    DownloadedBytes += size;
                    ReportProgress(DownloadedBytes, true);
                }
            }
        }

        private void ReportProgress(long bytes, bool force = false)
        {
            if (Progress != null && (force || progressTimer.ElapsedMilliseconds >= 200))
            {
                Progress.Report(bytes);
                progressTimer.Restart();
            }
        }

        private int FindSegment(long offset)
        {
            int left = 0, right = segments.Count - 1, result = -1;
            while (left <= right)
            {
                int middle = left + (right - left) / 2;
                if (segments.Keys[middle] <= offset) { result = middle; left = middle + 1; }
                else right = middle - 1;
            }
            return result;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || count < 0 || offset > buffer.Length - count) throw new ArgumentOutOfRangeException();
            if (disposed) throw new ObjectDisposedException(nameof(HttpRangeStream));
            CancellationToken.ThrowIfCancellationRequested();
            int total = 0;
            while (count > 0 && position < length)
            {
                CancellationToken.ThrowIfCancellationRequested();
                int index = FindSegment(position);
                var segment = index >= 0 && position < segments.Values[index].End ? segments.Values[index] : null;
                if (segment == null)
                {
                    long end = position + Math.Min(length - position, Math.Min(count, MaxRequestSize));
                    if (index + 1 < segments.Count) end = Math.Min(end, segments.Keys[index + 1]);
                    FetchAsync(position, end - 1).GetAwaiter().GetResult();
                    continue;
                }
                int take = (int)Math.Min(count, segment.End - position);
                cache.Position = segment.CacheOffset + position - segment.Start;
                int read = cache.Read(buffer, offset, take);
                if (read == 0) throw new EndOfStreamException("The archive cache is incomplete.");
                position += read;
                offset += read;
                count -= read;
                total += read;
            }
            return total;
        }

        internal void DownloadEntireArchive()
        {
            long previousPosition = position;
            try
            {
                position = 0;
                var buffer = new byte[MaxRequestSize];
                while (Read(buffer, 0, buffer.Length) != 0) { }
            }
            finally { position = previousPosition; }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long target;
            switch (origin)
            {
                case SeekOrigin.Begin: target = offset; break;
                case SeekOrigin.Current: target = checked(position + offset); break;
                case SeekOrigin.End: target = checked(length + offset); break;
                default: throw new ArgumentOutOfRangeException(nameof(origin));
            }
            if (target < 0) throw new IOException("Cannot seek before the archive.");
            return position = target;
        }

        public override bool CanRead => !disposed;
        public override bool CanSeek => !disposed;
        public override bool CanWrite => false;
        public override long Length => length;
        public override long Position { get => position; set => Seek(value, SeekOrigin.Begin); }
        public override void Flush() { }
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing)
        {
            if (disposing && !disposed)
            {
                disposed = true;
                cache.Dispose();
            }
            base.Dispose(disposing);
        }

        private sealed class Segment
        {
            internal long Start, End, CacheOffset;
        }
    }
}
