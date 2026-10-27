using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using ZipPeek.SevenZip;

namespace ZipPeek
{
    public sealed class SevenZipArchiveEntry : ZipEntry
    {
        public uint Index { get; internal set; }
        public long? BlockId { get; internal set; }
        public bool IsDirectory { get; internal set; }
        public int BlockFileCount { get; internal set; }
        public long BlockCompressedSize { get; internal set; }
    }

    internal sealed class SevenZipDownloadPlan
    {
        internal SevenZipArchiveEntry[] Entries { get; set; }
        internal int RequestedFileCount { get; set; }
        internal bool DownloadsWholeArchive { get; set; }
        internal long CompressedBytes { get; set; }
    }

    internal sealed class RemoteSevenZipArchive : IDisposable
    {
        private static readonly HttpClient Client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        private HttpRangeStream source;
        private SevenZipNative native;
        private ArchiveInput input;
        private List<SevenZipArchiveEntry> entries;
        private bool disposed;
        internal string Url { get; private set; }
        internal long Length => source.Length;
        internal long DownloadedBytes => source.DownloadedBytes;
        internal IReadOnlyList<SevenZipArchiveEntry> Entries => entries.AsReadOnly();

        internal static Task<RemoteSevenZipArchive> OpenAsync(string url, string password, CancellationToken token,
            IProgress<long> progress, HttpClient client = null)
        {
            return Task.Run(async () =>
            {
                var result = new RemoteSevenZipArchive { Url = url };
                try
                {
                    // Check the dependency before performing any network transfer.
                    result.native = new SevenZipNative();
                    result.source = await HttpRangeStream.OpenAsync(client ?? Client, url, token, progress).ConfigureAwait(false);
                    result.input = new ArchiveInput(result.source);
                    ulong scanLimit = 0; // Open only the explicitly selected 7z format, without scanning the payload.
                    var callback = new ArchiveOpenCallback(password, token);
                    int status = result.native.Archive.Open(result.input, ref scanLimit, callback);
                    token.ThrowIfCancellationRequested();
                    result.ThrowInputError();
                    SevenZipNative.Check(status);
                    result.ReadEntries(token);
                    result.source.Progress = null;
                    return result;
                }
                catch { result.Dispose(); throw; }
            }, token);
        }

        private void ReadEntries(CancellationToken token)
        {
            uint count;
            SevenZipNative.Check(native.Archive.GetNumberOfItems(out count));
            entries = new List<SevenZipArchiveEntry>();
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (uint i = 0; i < count; i++)
            {
                token.ThrowIfCancellationRequested();
                if ((bool?)native.Property(i, 21) == true)
                    throw new NotSupportedException("7z anti-items are not supported.");
                string name = (native.Property(i, 3) as string ?? "").Replace('\\', '/').TrimEnd('/');
                ArchiveOutputPath.ValidateName(name);
                if (!paths.Add(name)) throw new InvalidDataException("The archive contains conflicting paths: " + name);
                bool directory = (bool?)native.Property(i, 6) == true;
                entries.Add(new SevenZipArchiveEntry
                {
                    Index = i,
                    FileName = name + (directory ? "/" : ""),
                    IsDirectory = directory,
                    BlockId = (long?)native.Property(i, 27),
                    CompressedSize = (long?)native.Property(i, 8) ?? 0,
                    UncompressedSize = (long?)native.Property(i, 7) ?? 0,
                    IsEncrypted = (bool?)native.Property(i, 15) == true,
                    LastModified = (DateTime?)native.Property(i, 12) ?? new DateTime(1980, 1, 1)
                });
            }
            var filePaths = new HashSet<string>(entries.Where(x => !x.IsDirectory).Select(x => x.FileName), StringComparer.OrdinalIgnoreCase);
            foreach (var entry in entries)
            {
                string path = entry.FileName.TrimEnd('/');
                int separator = path.LastIndexOf('/');
                while (separator >= 0)
                {
                    if (filePaths.Contains(path.Substring(0, separator)))
                        throw new InvalidDataException("A file conflicts with an archive directory: " + entry.FileName);
                    separator = path.LastIndexOf('/', separator - 1);
                }
            }
            foreach (var group in entries.Where(x => !x.IsDirectory && x.BlockId.HasValue).GroupBy(x => x.BlockId.Value))
            {
                long packed = group.Sum(x => x.CompressedSize);
                foreach (var entry in group)
                {
                    entry.BlockFileCount = group.Count();
                    entry.BlockCompressedSize = packed;
                }
            }
        }

        internal SevenZipDownloadPlan Plan(IEnumerable<SevenZipArchiveEntry> selected)
        {
            ThrowIfDisposed();
            var requested = selected.Where(x => !x.IsDirectory).Distinct().ToArray();
            if (requested.Any(x => x.Index >= entries.Count || !ReferenceEquals(entries[(int)x.Index], x)))
                throw new ArgumentException("The selection belongs to another archive.");
            var requestedIds = new HashSet<uint>(requested.Select(x => x.Index));
            var blocks = new HashSet<long>(requested.Where(x => x.BlockId.HasValue).Select(x => x.BlockId.Value));
            var allBlocks = new HashSet<long>(entries.Where(x => !x.IsDirectory && x.BlockId.HasValue).Select(x => x.BlockId.Value));
            bool whole = allBlocks.Count > 0 && allBlocks.SetEquals(blocks);
            var expanded = entries.Where(x => !x.IsDirectory && (whole || requestedIds.Contains(x.Index) ||
                (x.BlockId.HasValue && blocks.Contains(x.BlockId.Value)))).ToArray();
            return new SevenZipDownloadPlan
            {
                Entries = expanded,
                RequestedFileCount = requested.Length,
                DownloadsWholeArchive = whole,
                CompressedBytes = whole ? Length : expanded.Where(x => x.BlockId.HasValue).GroupBy(x => x.BlockId.Value).Sum(x => x.First().BlockCompressedSize)
            };
        }

        internal Task ExtractAsync(SevenZipDownloadPlan plan, ISet<uint> filesToWrite, string outputDirectory,
            string password, CancellationToken token, IProgress<long> downloadProgress, IProgress<long> extractionProgress, bool skipFileErrors = false)
        {
            return Task.Run(() =>
            {
                ThrowIfDisposed();
                token.ThrowIfCancellationRequested();
                source.CancellationToken = token;
                source.Progress = downloadProgress;
                input.ClearError();
                try
                {
                    if (filesToWrite.Count == 0) return;
                    if (plan.DownloadsWholeArchive) source.DownloadEntireArchive();
                    var indices = plan.Entries.Select(x => x.Index).OrderBy(x => x).ToArray();
                    using (var callback = new ArchiveExtractCallback(plan.Entries, filesToWrite, outputDirectory, password, token, extractionProgress, skipFileErrors))
                    {
                        int status = native.Archive.Extract(indices, (uint)indices.Length, 0, callback);
                        token.ThrowIfCancellationRequested();
                        ThrowInputError();
                        if (callback.Error != null) ExceptionDispatchInfo.Capture(callback.Error).Throw();
                        SevenZipNative.Check(status);
                        if (callback.FailedFiles.Count > 0)
                            throw new InvalidDataException($"Extracted {callback.CompletedFiles} files; skipped {callback.FailedFiles.Count} files with errors:\n" +
                                string.Join("\n", callback.FailedFiles.Take(5)));
                    }
                }
                finally { source.Progress = null; }
            }, token);
        }

        private void ThrowInputError()
        {
            if (input.Error != null) ExceptionDispatchInfo.Capture(input.Error).Throw();
        }
        private void ThrowIfDisposed()
        {
            if (disposed) throw new ObjectDisposedException(nameof(RemoteSevenZipArchive));
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            try { native?.Dispose(); }
            finally { source?.Dispose(); }
        }
    }
}
