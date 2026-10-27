using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using ZipPeek;

internal static class DownloadTests
{
    private const string Url = "https://example.invalid/archive.zip";
    private static int passed;

    private static int Main(string[] args)
    {
        try
        {
            RunAsync().GetAwaiter().GetResult();
            if (args.Contains("--sevenzip")) SevenZipTests.RunAsync().GetAwaiter().GetResult();
            Console.WriteLine("Passed " + passed + " download regression checks.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }

    private static async Task RunAsync()
    {
        var progress = new RecordingProgress();
        using (var client = Client((request, token) => Task.FromResult(Response(new byte[] { 2, 3, 4 }, 2, 4, 10))))
        {
            var downloader = new RangeDownloader(client);
            var result = await downloader.FetchRangeAsync(Url, 2, 4, progress, CancellationToken.None);
            Check(result.SequenceEqual(new byte[] { 2, 3, 4 }) && progress.Last == 3, "memory download and final progress");
            await Throws<ArgumentOutOfRangeException>(() => downloader.FetchRangeAsync(Url, -1, 4, null, CancellationToken.None));
            await Throws<ArgumentOutOfRangeException>(() => downloader.FetchRangeAsync(Url, 4, 2, null, CancellationToken.None));
            await Throws<ArgumentOutOfRangeException>(() => downloader.FetchRangeAsync(Url, 0, long.MaxValue, null, CancellationToken.None));
        }

        using (var client = Client((request, token) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[10]) })))
            await Throws<InvalidOperationException>(() => new RangeDownloader(client).FetchRangeAsync(Url, 2, 4, null, CancellationToken.None));

        using (var client = Client((request, token) => Task.FromResult(Response(new byte[3], 0, 2, 10))))
            await Throws<InvalidDataException>(() => new RangeDownloader(client).FetchRangeAsync(Url, 2, 4, null, CancellationToken.None));

        using (var client = Client((request, token) => Task.FromResult(Response(new byte[2], 2, 4, 10))))
            await Throws<EndOfStreamException>(() => new RangeDownloader(client).FetchRangeAsync(Url, 2, 4, null, CancellationToken.None));

        using (var client = Client((request, token) => Task.FromResult(Response(new byte[2], 8, 9, 10))))
        {
            var result = await new RangeDownloader(client).FetchRangeAsync(Url, 8, 40, null, CancellationToken.None);
            Check(result.Length == 2, "padding beyond EOF");
        }

        string tempPath = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(tempPath, new byte[] { 2 });
            int requests = 0;
            using (var client = Client((request, token) =>
            {
                requests++;
                var range = request.Headers.Range.Ranges.Single();
                Check(range.From == 3 && range.To == 4, "resume requests only missing bytes");
                return Task.FromResult(Response(new byte[] { 3, 4 }, 3, 4, 10));
            }))
            {
                var downloader = new RangeDownloader(client);
                await downloader.DownloadRangeToFileAsync(Url, tempPath, 2, 4, progress, CancellationToken.None);
                Check(File.ReadAllBytes(tempPath).SequenceEqual(new byte[] { 2, 3, 4 }) && progress.Last == 3, "resume preserves bytes and reports total");
                await downloader.DownloadRangeToFileAsync(Url, tempPath, 2, 4, null, CancellationToken.None);
                Check(requests == 1, "complete files do not issue requests");
                await Throws<InvalidOperationException>(() => downloader.DownloadRangeToFileAsync(Url, tempPath, 2, 3, null, CancellationToken.None));
            }
        }
        finally
        {
            File.Delete(tempPath);
        }

        var started = new TaskCompletionSource<bool>();
        using (var source = new CancellationTokenSource())
        using (var client = Client(async (request, token) =>
        {
            started.SetResult(true);
            await Task.Delay(Timeout.Infinite, token);
            throw new InvalidOperationException("Canceled request completed.");
        }))
        {
            var download = new RangeDownloader(client).FetchRangeAsync(Url, 0, 1, null, source.Token);
            await started.Task;
            source.Cancel();
            await Throws<OperationCanceledException>(async () => await download);
        }

        var bothStarted = new TaskCompletionSource<bool>();
        int count = 0;
        using (var client = Client(async (request, token) =>
        {
            if (Interlocked.Increment(ref count) == 2)
                bothStarted.SetResult(true);
            await bothStarted.Task;
            var range = request.Headers.Range.Ranges.Single();
            return Response(new byte[] { (byte)range.From.Value }, range.From.Value, range.To.Value, 10);
        }))
        {
            var downloader = new RangeDownloader(client);
            var results = await Task.WhenAll(
                downloader.FetchRangeAsync(Url, 1, 1, null, CancellationToken.None),
                downloader.FetchRangeAsync(Url, 5, 5, null, CancellationToken.None));
            Check(results[0][0] == 1 && results[1][0] == 5 && client.DefaultRequestHeaders.Range == null, "concurrent requests keep independent ranges");
        }

        var entries = await RemoteZipReader.ReadZipEntriesAsync(Url, 0, 0, null);
        Check(entries.Count == 0, "empty central directory requires no HTTP request");
    }

    private static HttpClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond)
    {
        return new HttpClient(new StubHandler(respond));
    }

    private static HttpResponseMessage Response(byte[] data, long start, long end, long total)
    {
        var response = new HttpResponseMessage(HttpStatusCode.PartialContent) { Content = new ByteArrayContent(data) };
        response.Content.Headers.ContentRange = new ContentRangeHeaderValue(start, end, total);
        return response;
    }

    private static void Check(bool condition, string name)
    {
        if (!condition)
            throw new Exception("Failed: " + name);
        passed++;
        Console.WriteLine("PASS: " + name);
    }

    private static async Task Throws<T>(Func<Task> action) where T : Exception
    {
        try { await action(); }
        catch (T) { Check(true, "rejects " + typeof(T).Name); return; }
        throw new Exception("Expected " + typeof(T).Name);
    }

    private sealed class RecordingProgress : IProgress<long>
    {
        public long Last { get; private set; }
        public void Report(long value) { Last = value; }
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond;
        public StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) { this.respond = respond; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) { return respond(request, token); }
    }
}
