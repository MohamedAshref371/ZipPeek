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
using ZipPeek;
using ZipPeek.SevenZip;

internal static class SevenZipTests
{
    private const string Url = "https://example.invalid/archive.7z";
    private static string root;
    private static string executable;
    private static int passed;

    internal static async Task RunAsync()
    {
        executable = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip", "7z.exe");
        if (!File.Exists(executable)) throw new FileNotFoundException("Install 7-Zip to run --sevenzip integration checks.");
        root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ZipPeek-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "input", "nested"));
        try
        {
            var random = new Random(9274);
            foreach (string name in new[] { "a.bin", "nested/b.bin", "nested/c.bin" })
            {
                var bytes = new byte[2 * 1024 * 1024];
                random.NextBytes(bytes);
                File.WriteAllBytes(Path.Combine(root, "input", name), bytes);
            }
            File.WriteAllBytes(Path.Combine(root, "input", "empty.txt"), new byte[0]);
            foreach (string mode in new[] { "off", "2f", "on" })
            {
                string archive = CreateArchive(mode, null, false);
                await CheckArchive(archive, mode, null);
            }
            await CheckArchive(CreateArchive("2f", "secret", true), "2f", "secret");
            await CheckArchive(CreateArchive("off", "secret", false), "off", "secret");
            await CheckWrongDataPassword(CreateArchive("off", "secret", false));
            await CheckSkipFailedFile(CreateArchive("off", null, false));
            string encrypted = CreateArchive("on", "secret", true);
            using (var handler = new RangeHandler(File.ReadAllBytes(encrypted)))
            using (var client = new HttpClient(handler))
                await Throws(() => RemoteSevenZipArchive.OpenAsync(Url, "wrong", CancellationToken.None, null, client), "wrong header password rejected");

            await CheckCancellationAndChanges(CreateArchive("on", null, false));
            await CheckStaDisposal(CreateArchive("off", null, false));
            await CheckInvalidHttpResponses();
            foreach (string name in new[] { "../escape", "/rooted", "C:/escape", "a/../../escape", "file:stream", "CON.txt", "a./file" })
            {
                bool rejected = false;
                try { ArchiveOutputPath.Resolve(Path.Combine(root, "safe"), name); }
                catch (InvalidDataException) { rejected = true; }
                Check(rejected, "unsafe path rejected: " + name);
            }
            Console.WriteLine("Passed " + passed + " 7z integration checks.");
        }
        finally { Directory.Delete(root, true); }
    }

    private static string CreateArchive(string mode, string password, bool encryptedHeaders)
    {
        string path = Path.Combine(root, Guid.NewGuid().ToString("N") + ".7z");
        var info = new ProcessStartInfo(executable, "a -t7z -mx=1 -m0=LZMA2 -ms=" + mode + " -mhc=on " +
            (password == null ? "" : "-p" + password + " -mhe=" + (encryptedHeaders ? "on " : "off ")) +
            "\"" + path + "\" a.bin nested empty.txt")
        {
            WorkingDirectory = Path.Combine(root, "input"),
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true
        };
        using (var process = Process.Start(info))
        {
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0) throw new Exception(output);
        }
        return path;
    }

    private static async Task CheckArchive(string path, string mode, string password)
    {
        byte[] bytes = File.ReadAllBytes(path);
        using (var handler = new RangeHandler(bytes))
        using (var client = new HttpClient(handler))
        using (var archive = await RemoteSevenZipArchive.OpenAsync(Url, password, CancellationToken.None, null, client))
        {
            Check(archive.Entries.Count(x => !x.IsDirectory) == 4, "lists files and empty file: " + mode);
            Check(handler.Transferred < 64 * 1024, "browses without downloading payload: " + mode);
            var blocks = archive.Entries.Where(x => x.BlockId.HasValue).GroupBy(x => x.BlockId.Value).ToArray();
            Check(blocks.Length == (mode == "on" ? 1 : mode == "off" ? 3 : 2), "identifies solid blocks: " + mode);
            var selected = blocks.OrderByDescending(x => x.Count()).First().First();
            var plan = archive.Plan(new[] { selected });
            Check(plan.Entries.Count(x => x.BlockId.HasValue) == (mode == "on" ? 3 : mode == "off" ? 1 : 2), "expands selection to whole block: " + mode);
            Check(plan.DownloadsWholeArchive == (mode == "on"), "whole-archive warning decision: " + mode);
            string output = Path.Combine(root, Guid.NewGuid().ToString("N"));
            var ids = new HashSet<uint>(plan.Entries.Select(x => x.Index));
            await archive.ExtractAsync(plan, ids, output, password, CancellationToken.None, null, null);
            foreach (var entry in plan.Entries)
                Check(File.ReadAllBytes(Path.Combine(output, entry.FileName)).SequenceEqual(File.ReadAllBytes(Path.Combine(root, "input", entry.FileName))), "exact extracted bytes: " + entry.FileName);
            foreach (var entry in archive.Entries.Where(x => !x.IsDirectory && !ids.Contains(x.Index)))
                Check(!File.Exists(Path.Combine(output, entry.FileName)), "unrelated file not extracted: " + entry.FileName);
            Check(mode == "on" ? handler.Transferred == bytes.Length : handler.Transferred < bytes.Length * 0.9, "transfers only required data: " + mode);
            long before = handler.Transferred;
            await archive.ExtractAsync(plan, ids, output, password, CancellationToken.None, null, null);
            Check(handler.Transferred == before, "cached blocks are reused: " + mode);
            foreach (var entry in plan.Entries)
                Check(new FileInfo(Path.Combine(output, entry.FileName)).Length == entry.UncompressedSize, "overwrite retains correct size");
            var empty = archive.Entries.Single(x => x.FileName == "empty.txt");
            var emptyPlan = archive.Plan(new[] { empty });
            Check(!emptyPlan.DownloadsWholeArchive && emptyPlan.Entries.Length == 1, "empty selection does not expand blocks");
            await archive.ExtractAsync(emptyPlan, new HashSet<uint> { empty.Index }, output, password, CancellationToken.None, null, null);
            Check(new FileInfo(Path.Combine(output, "empty.txt")).Length == 0, "empty file is extracted");
        }
    }

    private static async Task CheckCancellationAndChanges(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        using (var handler = new RangeHandler(bytes))
        using (var client = new HttpClient(handler))
        using (var archive = await RemoteSevenZipArchive.OpenAsync(Url, null, CancellationToken.None, null, client))
        {
            var plan = archive.Plan(new[] { archive.Entries.First(x => x.BlockId.HasValue) });
            var ids = new HashSet<uint>(plan.Entries.Select(x => x.Index));
            string output = Path.Combine(root, "cancel-output");
            handler.WaitForCancellation = true;
            using (var cancellation = new CancellationTokenSource())
            {
                var extraction = archive.ExtractAsync(plan, ids, output, null, cancellation.Token, null, null);
                if (await Task.WhenAny(handler.Started.Task, Task.Delay(10000)) != handler.Started.Task) throw new Exception("Cancellation test timed out.");
                cancellation.Cancel();
                bool canceled = false;
                try { await extraction; } catch (OperationCanceledException) { canceled = true; }
                Check(canceled, "cancellation interrupts an active HTTP request");
            }
            handler.WaitForCancellation = false;
            await archive.ExtractAsync(plan, ids, output, null, CancellationToken.None, null, null);
            Check(handler.Transferred == bytes.Length, "canceled download resumes from cached ranges");
        }
        using (var handler = new RangeHandler(bytes))
        using (var client = new HttpClient(handler))
        using (var archive = await RemoteSevenZipArchive.OpenAsync(Url, null, CancellationToken.None, null, client))
        {
            handler.Tag = "\"changed\"";
            var plan = archive.Plan(new[] { archive.Entries.First(x => x.BlockId.HasValue) });
            await Throws(() => archive.ExtractAsync(plan, new HashSet<uint>(plan.Entries.Select(x => x.Index)), Path.Combine(root, "changed"), null, CancellationToken.None, null, null), "changed remote archive rejected");
        }
        bytes[64] ^= 255;
        using (var handler = new RangeHandler(bytes))
        using (var client = new HttpClient(handler))
        using (var archive = await RemoteSevenZipArchive.OpenAsync(Url, null, CancellationToken.None, null, client))
        {
            var plan = archive.Plan(new[] { archive.Entries.First(x => x.BlockId.HasValue) });
            string output = Path.Combine(root, "corrupt");
            await Throws(() => archive.ExtractAsync(plan, new HashSet<uint>(plan.Entries.Select(x => x.Index)), output, null, CancellationToken.None, null, null), "corrupt compressed data rejected");
            Check(!Directory.Exists(output) || !Directory.EnumerateFiles(output, ".zippeek-*.tmp", SearchOption.AllDirectories).Any(), "failed extraction removes partial files");
        }
    }

    private static async Task CheckWrongDataPassword(string path)
    {
        using (var handler = new RangeHandler(File.ReadAllBytes(path)))
        using (var client = new HttpClient(handler))
        using (var archive = await RemoteSevenZipArchive.OpenAsync(Url, null, CancellationToken.None, null, client))
        {
            var entry = archive.Entries.First(x => x.BlockId.HasValue);
            var plan = archive.Plan(new[] { entry });
            var ids = new HashSet<uint> { entry.Index };
            string output = Path.Combine(root, "password-output");
            string destination = Path.Combine(output, entry.FileName);
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            File.WriteAllText(destination, "original destination");
            await Throws(() => archive.ExtractAsync(plan, ids, output, "wrong", CancellationToken.None, null, null), "wrong data password rejected");
            Check(File.ReadAllText(destination) == "original destination", "failed extraction preserves existing destination");
            await archive.ExtractAsync(plan, ids, output, "secret", CancellationToken.None, null, null);
            Check(File.ReadAllBytes(destination).SequenceEqual(File.ReadAllBytes(Path.Combine(root, "input", entry.FileName))), "corrected password succeeds using cached bytes");
        }
    }

    private static async Task CheckSkipFailedFile(string path)
    {
        var bytes = File.ReadAllBytes(path);
        bytes[64] ^= 255;
        using (var handler = new RangeHandler(bytes))
        using (var client = new HttpClient(handler))
        using (var archive = await RemoteSevenZipArchive.OpenAsync(Url, null, CancellationToken.None, null, client))
        {
            var plan = archive.Plan(archive.Entries.Where(x => !x.IsDirectory));
            string output = Path.Combine(root, "skip-failed");
            await Throws(() => archive.ExtractAsync(plan, new HashSet<uint>(plan.Entries.Select(x => x.Index)), output, null,
                CancellationToken.None, null, null, true), "skip-failed reports partial failure");
            foreach (string name in new[] { "nested/b.bin", "nested/c.bin" })
                Check(File.ReadAllBytes(Path.Combine(output, name)).SequenceEqual(File.ReadAllBytes(Path.Combine(root, "input", name))), "skip-failed continues to independent file: " + name);
            Check(!File.Exists(Path.Combine(output, "a.bin")), "failed file is not committed when skipping errors");
        }
    }

    private static async Task CheckInvalidHttpResponses()
    {
        foreach (string failure in new[] { "range ignored", "wrong range", "truncated", "encoded body" })
        {
            using (var handler = new RangeHandler(new byte[128]) { Failure = failure })
            using (var client = new HttpClient(handler))
            {
                bool rejected = false;
                try { using (await HttpRangeStream.OpenAsync(client, Url, CancellationToken.None, null)) { } }
                catch (InvalidDataException) { rejected = true; }
                catch (EndOfStreamException) { rejected = true; }
                Check(rejected, "HTTP source rejects " + failure);
            }
        }
    }

    private static async Task CheckStaDisposal(string path)
    {
        using (var handler = new RangeHandler(File.ReadAllBytes(path)))
        using (var client = new HttpClient(handler))
        {
            var archive = await RemoteSevenZipArchive.OpenAsync(Url, null, CancellationToken.None, null, client);
            Exception failure = null;
            var thread = new Thread(() => { try { archive.Dispose(); } catch (Exception error) { failure = error; } });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            if (!thread.Join(10000)) throw new Exception("STA disposal timed out.");
            if (failure != null) throw new Exception("STA disposal failed.", failure);
            Check(true, "native archive can be closed from the UI apartment");
        }
    }

    private static async Task Throws(Func<Task> action, string message)
    {
        try { await action(); }
        catch (InvalidDataException) { Check(true, message); return; }
        throw new Exception("Expected InvalidDataException: " + message);
    }
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("Failed: " + message);
        passed++;
        Console.WriteLine("PASS: " + message);
    }

    private sealed class RangeHandler : HttpMessageHandler
    {
        private readonly byte[] bytes;
        internal long Transferred;
        internal string Tag = "\"original\"";
        internal bool WaitForCancellation;
        internal string Failure;
        internal TaskCompletionSource<bool> Started = new TaskCompletionSource<bool>();
        internal RangeHandler(byte[] bytes) { this.bytes = bytes; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            if (WaitForCancellation)
            {
                Started.TrySetResult(true);
                await Task.Delay(Timeout.Infinite, token);
            }
            var range = request.Headers.Range.Ranges.Single();
            long start = range.From.Value, end = Math.Min(range.To.Value, bytes.Length - 1);
            var data = new byte[checked((int)(end - start + 1)) - (Failure == "truncated" ? 1 : 0)];
            Buffer.BlockCopy(bytes, (int)start, data, 0, data.Length);
            Transferred += data.Length;
            var response = new HttpResponseMessage(Failure == "range ignored" ? HttpStatusCode.OK : HttpStatusCode.PartialContent) { Content = new ByteArrayContent(data) };
            response.Content.Headers.ContentRange = new ContentRangeHeaderValue(Failure == "wrong range" ? start + 1 : start, end, bytes.Length);
            if (Failure == "encoded body") response.Content.Headers.ContentEncoding.Add("gzip");
            response.Headers.ETag = new EntityTagHeaderValue(Tag);
            return response;
        }
    }
}
