using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

// Interface IDs and vtable order follow the official 7-Zip SDK:
// CPP/7zip/{IStream.h,IPassword.h,Archive/IArchive.h,PropID.h}.
namespace ZipPeek.SevenZip
{
    [ComImport, Guid("23170F69-40C1-278A-0000-000600600000"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IInArchive
    {
        [PreserveSig] int Open(IInStream stream, ref ulong maxCheckStartPosition, IArchiveOpenCallback callback);
        [PreserveSig] int Close();
        [PreserveSig] int GetNumberOfItems(out uint count);
        [PreserveSig] int GetProperty(uint index, uint property, IntPtr value);
        [PreserveSig] int Extract([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] uint[] indices, uint count, int testMode, IArchiveExtractCallback callback);
        [PreserveSig] int GetArchiveProperty(uint property, IntPtr value);
        [PreserveSig] int GetNumberOfProperties(out uint count);
        [PreserveSig] int GetPropertyInfo(uint index, out IntPtr name, out uint property, out ushort type);
        [PreserveSig] int GetNumberOfArchiveProperties(out uint count);
        [PreserveSig] int GetArchivePropertyInfo(uint index, out IntPtr name, out uint property, out ushort type);
    }

    [ComVisible(true), Guid("23170F69-40C1-278A-0000-000300030000"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IInStream
    {
        [PreserveSig] int Read(IntPtr data, uint size, IntPtr processedSize);
        [PreserveSig] int Seek(long offset, uint origin, IntPtr newPosition);
    }

    [ComVisible(true), Guid("23170F69-40C1-278A-0000-000300020000"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ISequentialOutStream
    {
        [PreserveSig] int Write(IntPtr data, uint size, IntPtr processedSize);
    }

    [ComVisible(true), Guid("23170F69-40C1-278A-0000-000600100000"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IArchiveOpenCallback
    {
        [PreserveSig] int SetTotal(IntPtr files, IntPtr bytes);
        [PreserveSig] int SetCompleted(IntPtr files, IntPtr bytes);
    }

    [ComVisible(true), Guid("23170F69-40C1-278A-0000-000600200000"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IArchiveExtractCallback
    {
        [PreserveSig] int SetTotal(ulong total);
        [PreserveSig] int SetCompleted(IntPtr completeValue);
        [PreserveSig] int GetStream(uint index, [MarshalAs(UnmanagedType.Interface)] out ISequentialOutStream stream, int askMode);
        [PreserveSig] int PrepareOperation(int askMode);
        [PreserveSig] int SetOperationResult(int result);
    }

    [ComVisible(true), Guid("23170F69-40C1-278A-0000-000500100000"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ICryptoGetTextPassword
    {
        [PreserveSig] int CryptoGetTextPassword([MarshalAs(UnmanagedType.BStr)] out string password);
    }

    internal sealed class SevenZipNative : IDisposable
    {
        // The module remains loaded while any native archive or COM callback can exist.
        private static readonly Lazy<IntPtr> Module = new Lazy<IntPtr>(LoadModule);
        internal IInArchive Archive { get; private set; }

        internal SevenZipNative()
        {
            var address = GetProcAddress(Module.Value, "CreateObject");
            if (address == IntPtr.Zero) throw new InvalidOperationException("7z.dll does not export CreateObject.");
            var create = (CreateObject)Marshal.GetDelegateForFunctionPointer(address, typeof(CreateObject));
            var classId = new Guid("23170F69-40C1-278A-1000-000110070000");
            var interfaceId = typeof(IInArchive).GUID;
            IntPtr result;
            Check(create(ref classId, ref interfaceId, out result));
            try { Archive = (IInArchive)Marshal.GetObjectForIUnknown(result); }
            finally { Marshal.Release(result); }
        }

        private static IntPtr LoadModule()
        {
            var paths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "7zip.org", "7z.dll"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "7z.dll"),
                Path.GetFullPath(Path.Combine("7zip.org", "7z.dll")),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip", "7z.dll"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "7-Zip", "7z.dll")
            };
            foreach (string path in paths.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!File.Exists(path)) continue;
                IntPtr module = LoadLibraryEx(path, IntPtr.Zero, 8);
                if (module != IntPtr.Zero) return module;
            }
            throw new FileNotFoundException("7z support requires 7-Zip. Install 7-Zip matching the application's architecture, or place its 7z.dll in the 7zip.org folder beside ZipPeek.exe.");
        }

        internal object Property(uint index, uint property, bool archiveProperty = false)
        {
            IntPtr value = Marshal.AllocCoTaskMem(24);
            Marshal.Copy(new byte[24], 0, value, 24);
            try
            {
                Check(archiveProperty ? Archive.GetArchiveProperty(property, value) : Archive.GetProperty(index, property, value));
                switch (Marshal.ReadInt16(value))
                {
                    case 0: return null; // VT_EMPTY
                    case 8: return Marshal.PtrToStringBSTR(Marshal.ReadIntPtr(value, 8));
                    case 11: return Marshal.ReadInt16(value, 8) != 0;
                    case 19: return (long)(uint)Marshal.ReadInt32(value, 8);
                    case 20: return Marshal.ReadInt64(value, 8);
                    case 21: return checked((long)(ulong)Marshal.ReadInt64(value, 8));
                    case 64: return DateTime.FromFileTime(Marshal.ReadInt64(value, 8));
                    default: throw new InvalidDataException("Unsupported 7-Zip property type.");
                }
            }
            finally { PropVariantClear(value); Marshal.FreeCoTaskMem(value); }
        }

        internal static void Check(int result)
        {
            if (result != 0) throw new InvalidDataException("7-Zip could not process the archive (code 0x" + result.ToString("X8") + "). Check the archive and password.");
        }

        public void Dispose()
        {
            if (Archive == null) return;
            // SDK interfaces have no apartment-marshalling proxy. Creation, use and
            // release must stay in the worker MTA, even when the UI closes an archive.
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.MTA)
            {
                Task.Run((Action)Dispose).GetAwaiter().GetResult();
                return;
            }
            try { Archive.Close(); }
            finally { Marshal.FinalReleaseComObject(Archive); Archive = null; }
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int CreateObject(ref Guid classId, ref Guid interfaceId, out IntPtr result);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibraryEx(string path, IntPtr reserved, uint flags);
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true)]
        private static extern IntPtr GetProcAddress(IntPtr module, string name);
        [DllImport("ole32.dll")]
        private static extern int PropVariantClear(IntPtr value);
    }

    [ComVisible(true), ClassInterface(ClassInterfaceType.None)]
    public sealed class ArchiveInput : IInStream
    {
        private readonly Stream stream;
        internal Exception Error { get; private set; }
        internal ArchiveInput(Stream stream) { this.stream = stream; }
        public int Read(IntPtr data, uint size, IntPtr processedSize)
        {
            if (processedSize != IntPtr.Zero) Marshal.WriteInt32(processedSize, 0);
            try
            {
                var buffer = new byte[Math.Min(size, 1024 * 1024)];
                int read = stream.Read(buffer, 0, buffer.Length);
                Marshal.Copy(buffer, 0, data, read);
                if (processedSize != IntPtr.Zero) Marshal.WriteInt32(processedSize, read);
                return 0;
            }
            catch (Exception error) { Error = error; return Marshal.GetHRForException(error); }
        }
        public int Seek(long offset, uint origin, IntPtr newPosition)
        {
            try
            {
                long result = stream.Seek(offset, (SeekOrigin)origin);
                if (newPosition != IntPtr.Zero) Marshal.WriteInt64(newPosition, result);
                return 0;
            }
            catch (Exception error) { Error = error; return Marshal.GetHRForException(error); }
        }
        internal void ClearError() { Error = null; }
    }

    [ComVisible(true), ClassInterface(ClassInterfaceType.None)]
    public sealed class ArchiveOpenCallback : IArchiveOpenCallback, ICryptoGetTextPassword
    {
        private readonly string password;
        private readonly System.Threading.CancellationToken token;
        internal ArchiveOpenCallback(string password, System.Threading.CancellationToken token) { this.password = password; this.token = token; }
        public int SetTotal(IntPtr files, IntPtr bytes) => token.IsCancellationRequested ? unchecked((int)0x80004004) : 0;
        public int SetCompleted(IntPtr files, IntPtr bytes) => SetTotal(files, bytes);
        public int CryptoGetTextPassword(out string value)
        {
            value = password;
            return password == null ? unchecked((int)0x80004004) : 0;
        }
    }
}
