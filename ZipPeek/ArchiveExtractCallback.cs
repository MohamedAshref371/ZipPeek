using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace ZipPeek.SevenZip
{
    internal static class ArchiveOutputPath
    {
        internal static void ValidateName(string name)
        {
            if (string.IsNullOrEmpty(name) || Path.IsPathRooted(name)) throw new InvalidDataException("Invalid archive path: " + name);
            foreach (string part in name.Replace('\\', '/').Split('/'))
            {
                string stem = part.Split('.')[0].ToUpperInvariant();
                if (part.Length == 0 || part == "." || part == ".." || part.EndsWith(".") || part.EndsWith(" ") ||
                    part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                    new[] { "CON", "PRN", "AUX", "NUL" }.Contains(stem) ||
                    (stem.Length == 4 && (stem.StartsWith("COM") || stem.StartsWith("LPT")) && stem[3] >= '1' && stem[3] <= '9'))
                    throw new InvalidDataException("Unsafe archive path: " + name);
            }
        }

        internal static string Resolve(string directory, string name)
        {
            ValidateName(name);
            string root = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string result = Path.GetFullPath(Path.Combine(root, name.Replace('/', Path.DirectorySeparatorChar)));
            if (!result.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("The archive path escapes the output directory.");
            // Do not follow existing junctions/symlinks inside or above the output directory.
            for (string path = result; !string.IsNullOrEmpty(path); path = Path.GetDirectoryName(path))
                if ((File.Exists(path) || Directory.Exists(path)) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("Extraction through a link is not supported: " + path);
            return result;
        }
    }

    [ComVisible(true), ClassInterface(ClassInterfaceType.None)]
    public sealed class ArchiveExtractCallback : IArchiveExtractCallback, ICryptoGetTextPassword, ISequentialOutStream, IDisposable
    {
        private readonly Dictionary<uint, SevenZipArchiveEntry> entries;
        private readonly ISet<uint> filesToWrite;
        private readonly string directory, password;
        private readonly CancellationToken token;
        private readonly IProgress<long> progress;
        private readonly bool skipFileErrors;
        private FileStream output;
        private SevenZipArchiveEntry current;
        private string temporaryPath, targetPath;
        internal Exception Error { get; private set; }
        internal List<string> FailedFiles { get; } = new List<string>();
        internal int CompletedFiles { get; private set; }

        internal ArchiveExtractCallback(IEnumerable<SevenZipArchiveEntry> entries, ISet<uint> filesToWrite,
            string directory, string password, CancellationToken token, IProgress<long> progress, bool skipFileErrors)
        {
            this.entries = entries.ToDictionary(x => x.Index);
            this.filesToWrite = filesToWrite;
            this.directory = directory;
            this.password = password;
            this.token = token;
            this.progress = progress;
            this.skipFileErrors = skipFileErrors;
        }

        private int Run(Action action)
        {
            try { token.ThrowIfCancellationRequested(); action(); return 0; }
            catch (Exception error) { Error = error; return Marshal.GetHRForException(error); }
        }
        public int SetTotal(ulong total) => Run(() => { });
        public int SetCompleted(IntPtr value) => Run(() => { if (value != IntPtr.Zero) progress?.Report(Marshal.ReadInt64(value)); });
        public int PrepareOperation(int askMode) => Run(() => { });
        public int GetStream(uint index, out ISequentialOutStream stream, int askMode)
        {
            stream = null;
            int status = Run(() =>
            {
                Cleanup();
                current = null;
                if (askMode != 0 || !filesToWrite.Contains(index)) return;
                if (!entries.TryGetValue(index, out current)) throw new InvalidDataException("Unexpected 7z entry index.");
                targetPath = ArchiveOutputPath.Resolve(directory, current.FileName);
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                temporaryPath = Path.Combine(Path.GetDirectoryName(targetPath), ".zippeek-" + Guid.NewGuid().ToString("N") + ".tmp");
                output = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            });
            if (status == 0 && output != null) stream = this;
            return status;
        }
        public int Write(IntPtr data, uint size, IntPtr processedSize)
        {
            if (processedSize != IntPtr.Zero) Marshal.WriteInt32(processedSize, 0);
            return Run(() =>
            {
                var buffer = new byte[Math.Min(size, 1024 * 1024)];
                uint written = 0;
                while (written < size)
                {
                    token.ThrowIfCancellationRequested();
                    int count = (int)Math.Min(buffer.Length, size - written);
                    Marshal.Copy(IntPtr.Add(data, checked((int)written)), buffer, 0, count);
                    output.Write(buffer, 0, count);
                    written += (uint)count;
                    if (processedSize != IntPtr.Zero) Marshal.WriteInt32(processedSize, unchecked((int)written));
                }
            });
        }
        public int SetOperationResult(int result)
        {
            return Run(() =>
            {
                if (result != 0 && skipFileErrors && current != null)
                {
                    FailedFiles.Add(current.FileName + " (result " + result + ")");
                    Cleanup();
                    return;
                }
                if (result != 0) throw new InvalidDataException("7z extraction failed (result " + result + "). Check the password and archive integrity.");
                if (output == null) return;
                long size = output.Length;
                output.Dispose(); output = null;
                if (size != current.UncompressedSize) throw new InvalidDataException("The extracted file size is incorrect.");
                // Apply timestamps before replacing any existing destination file.
                File.SetLastWriteTime(temporaryPath, current.LastModified);
                ArchiveOutputPath.Resolve(directory, current.FileName);
                if (File.Exists(targetPath)) File.Replace(temporaryPath, targetPath, null);
                else File.Move(temporaryPath, targetPath);
                temporaryPath = null;
                CompletedFiles++;
            });
        }
        public int CryptoGetTextPassword(out string value)
        {
            value = password;
            return Run(() => { if (password == null) throw new InvalidOperationException("A password is required for this 7z archive."); });
        }
        private void Cleanup()
        {
            output?.Dispose(); output = null;
            if (temporaryPath != null) { File.Delete(temporaryPath); temporaryPath = null; }
        }
        public void Dispose() { Cleanup(); }
    }
}
