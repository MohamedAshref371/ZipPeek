using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ZipPeek
{
    public static class Zip64Reader
    {
        public class EOCDInfo
        {
            public bool IsZip64 { get; set; }
            public uint CdSize { get; set; }
            public uint CdOffset { get; set; }
            public ushort CdEntries { get; set; }
            public int Zip64LocatorRelativeOffset { get; set; }
        }

        public class Zip64LocatorInfo
        {
            public long Zip64EOCDOffset { get; set; }
        }

        public class CentralDirectoryInfo
        {
            public long Offset { get; set; }
            public long Size { get; set; }
            public long EntryCount { get; set; }
        }

        public static async Task<List<ZipEntry>> ReadZip64EntriesAsync(string url)
        {
            long fileSize = await GetRemoteFileSizeAsync(url);

            byte[] eocdBuffer = await FindEOCDAsync(url, fileSize);
            EOCDInfo eocd = ParseEOCD(eocdBuffer);

            if (!eocd.IsZip64)
                throw new NotSupportedException("Only ZIP64 is supported in this reader.");

            // Read ZIP64 Locator
            long locatorOffset = fileSize - eocdBuffer.Length + eocd.Zip64LocatorRelativeOffset;
            byte[] locatorBuffer = await DownloadRangeAsync(url, locatorOffset, locatorOffset + 20 - 1);
            long zip64EOCDOffset = ParseZip64Locator(locatorBuffer);

            // Read ZIP64 EOCD
            byte[] zip64EocdBuffer = await DownloadRangeAsync(url, zip64EOCDOffset, zip64EOCDOffset + 56 - 1);
            CentralDirectoryInfo cdInfo = ParseZip64EOCD(zip64EocdBuffer);

            // Read Central Directory
            byte[] cdBuffer = await DownloadRangeAsync(url, cdInfo.Offset, cdInfo.Offset + cdInfo.Size - 1);
            return ParseCentralDirectory(cdBuffer);
        }

        public static async Task<long> GetRemoteFileSizeAsync(string url)
        {
            using (var httpClient = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 0);

                using (var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    if (!response.IsSuccessStatusCode || response.Content.Headers.ContentRange == null)
                        throw new InvalidOperationException("Unable to determine remote file size. Server may not support Range requests.");

                    return response.Content.Headers.ContentRange.Length ?? throw new InvalidOperationException("Content-Range header is missing length.");
                }
            }
        }

        public static async Task<byte[]> DownloadRangeAsync(string url, long from, long to)
        {
            if (from > to)
                throw new ArgumentException("Invalid range: 'from' must be less than or equal to 'to'.");

            using (var httpClient = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(from, to);

                using (var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    if (!response.IsSuccessStatusCode || response.Content == null)
                        throw new InvalidOperationException($"Failed to download range {from}-{to} from {url}.");

                    return await response.Content.ReadAsByteArrayAsync();
                }
            }
        }

        public static async Task<byte[]> FindEOCDAsync(string url, long fileSize)
        {
            const int maxEOCDSearch = 256 * 1024; // 256KB
            int readSize = (int)Math.Min(fileSize, maxEOCDSearch);
            long offset = fileSize - readSize;

            // حمل آخر جزء من الملف
            byte[] buffer = await DownloadRangeAsync(url, offset, fileSize - 1);

            // ابحث عن توقيع EOCD: 0x50 0x4B 0x05 0x06
            for (int i = buffer.Length - 22; i >= 0; i--) // EOCD minimum size = 22 bytes
            {
                if (buffer[i] == 0x50 &&
                    buffer[i + 1] == 0x4B &&
                    buffer[i + 2] == 0x05 &&
                    buffer[i + 3] == 0x06)
                {
                    byte[] eocd = new byte[buffer.Length - i];
                    Array.Copy(buffer, i, eocd, 0, eocd.Length);
                    return eocd;
                }
            }

            throw new InvalidDataException("End of Central Directory (EOCD) not found. ZIP64 may be malformed or unsupported.");
        }

        public static EOCDInfo ParseEOCD(byte[] buffer)
        {
            const uint signature = 0x06054b50;

            if (BitConverter.ToUInt32(buffer, 0) != signature)
                throw new InvalidDataException("Invalid EOCD signature.");

            ushort totalEntries = BitConverter.ToUInt16(buffer, 10);
            uint cdSize = BitConverter.ToUInt32(buffer, 12);
            uint cdOffset = BitConverter.ToUInt32(buffer, 16);
            ushort commentLength = BitConverter.ToUInt16(buffer, 20);

            bool isZip64 = (totalEntries == 0xFFFF || cdSize == 0xFFFFFFFF || cdOffset == 0xFFFFFFFF);

            int zip64LocatorOffset = -1;

            // نحاول إيجاد ZIP64 Locator (حجمه 20 بايت ويأتي مباشرة قبل EOCD)
            if (buffer.Length >= 22 + 20)
            {
                for (int i = buffer.Length - 22 - 20; i >= 0; i--)
                {
                    if (buffer[i] == 0x50 &&
                        buffer[i + 1] == 0x4B &&
                        buffer[i + 2] == 0x06 &&
                        buffer[i + 3] == 0x07)
                    {
                        zip64LocatorOffset = i;
                        break;
                    }
                }
            }

            return new EOCDInfo
            {
                IsZip64 = isZip64,
                CdEntries = totalEntries,
                CdSize = cdSize,
                CdOffset = cdOffset,
                Zip64LocatorRelativeOffset = zip64LocatorOffset
            };
        }

        public static async Task<Zip64LocatorInfo> ReadZip64LocatorAsync(string url, long locatorOffset)
        {
            const int locatorSize = 20;
            byte[] buffer = await DownloadRangeAsync(url, locatorOffset, locatorOffset + locatorSize - 1);

            const uint signature = 0x07064b50;
            if (BitConverter.ToUInt32(buffer, 0) != signature)
                throw new InvalidDataException("Invalid ZIP64 EOCD Locator signature.");

            ulong zip64EocdOffset = BitConverter.ToUInt64(buffer, 8);

            return new Zip64LocatorInfo
            {
                Zip64EOCDOffset = (long)zip64EocdOffset
            };
        }

        public static async Task<CentralDirectoryInfo> ReadZip64EOCDAsync(string url, long eocdOffset)
        {
            const int minZip64EOCDSize = 56; // بدون الامتدادات

            byte[] buffer = await DownloadRangeAsync(url, eocdOffset, eocdOffset + minZip64EOCDSize - 1);

            const uint signature = 0x06064b50;
            if (BitConverter.ToUInt32(buffer, 0) != signature)
                throw new InvalidDataException("Invalid ZIP64 EOCD signature.");

            long cdEntryCount = (long)BitConverter.ToUInt64(buffer, 32);
            long cdSize = (long)BitConverter.ToUInt64(buffer, 40);
            long cdOffset = (long)BitConverter.ToUInt64(buffer, 48);

            return new CentralDirectoryInfo
            {
                EntryCount = cdEntryCount,
                Size = cdSize,
                Offset = cdOffset
            };
        }

        public static async Task<List<ZipEntry>> ReadCentralDirectoryAsync(string url, long offset, long size)
        {
            byte[] cdBuffer = await DownloadRangeAsync(url, offset, offset + size - 1);

            List<ZipEntry> entries = new List<ZipEntry>();
            int ptr = 0;

            while (ptr + 46 <= cdBuffer.Length)
            {
                uint signature = BitConverter.ToUInt32(cdBuffer, ptr);
                if (signature != 0x02014b50)
                    break;

                ushort fileNameLength = BitConverter.ToUInt16(cdBuffer, ptr + 28);
                ushort extraLength = BitConverter.ToUInt16(cdBuffer, ptr + 30);
                ushort commentLength = BitConverter.ToUInt16(cdBuffer, ptr + 32);

                uint compSize32 = BitConverter.ToUInt32(cdBuffer, ptr + 20);
                uint uncompSize32 = BitConverter.ToUInt32(cdBuffer, ptr + 24);
                uint localHeaderOffset32 = BitConverter.ToUInt32(cdBuffer, ptr + 42);

                string fileName = Encoding.UTF8.GetString(cdBuffer, ptr + 46, fileNameLength);

                long compSize = compSize32;
                long uncompSize = uncompSize32;
                long localHeaderOffset = localHeaderOffset32;

                // تخزين السجل
                entries.Add(new ZipEntry
                {
                    FileName = fileName,
                    CompressedSize = compSize,
                    UncompressedSize = uncompSize,
                    LocalHeaderOffset = localHeaderOffset
                });

                ptr += 46 + fileNameLength + extraLength + commentLength;
            }

            return entries;
        }

        public static long ParseZip64Locator(byte[] buffer)
        {
            const uint signature = 0x07064b50;

            if (buffer.Length < 20)
                throw new InvalidDataException("ZIP64 Locator buffer too small.");

            if (BitConverter.ToUInt32(buffer, 0) != signature)
                throw new InvalidDataException("Invalid ZIP64 Locator signature.");

            ulong zip64EocdOffset = BitConverter.ToUInt64(buffer, 8);

            return (long)zip64EocdOffset;
        }

        public static CentralDirectoryInfo ParseZip64EOCD(byte[] buffer)
        {
            const uint signature = 0x06064b50;

            if (buffer.Length < 56)
                throw new InvalidDataException("ZIP64 EOCD buffer too small.");

            if (BitConverter.ToUInt32(buffer, 0) != signature)
                throw new InvalidDataException("Invalid ZIP64 EOCD signature.");

            long cdEntryCount = (long)BitConverter.ToUInt64(buffer, 32);
            long cdSize = (long)BitConverter.ToUInt64(buffer, 40);
            long cdOffset = (long)BitConverter.ToUInt64(buffer, 48);

            return new CentralDirectoryInfo
            {
                EntryCount = cdEntryCount,
                Size = cdSize,
                Offset = cdOffset
            };
        }

        public static List<ZipEntry> ParseCentralDirectory(byte[] cdBuffer)
        {
            List<ZipEntry> entries = new List<ZipEntry>();
            int ptr = 0;

            while (ptr + 46 <= cdBuffer.Length)
            {
                uint signature = BitConverter.ToUInt32(cdBuffer, ptr);
                if (signature != 0x02014b50)
                    break;

                ushort fileNameLength = BitConverter.ToUInt16(cdBuffer, ptr + 28);
                ushort extraLength = BitConverter.ToUInt16(cdBuffer, ptr + 30);
                ushort commentLength = BitConverter.ToUInt16(cdBuffer, ptr + 32);

                uint compSize32 = BitConverter.ToUInt32(cdBuffer, ptr + 20);
                uint uncompSize32 = BitConverter.ToUInt32(cdBuffer, ptr + 24);
                uint localHeaderOffset32 = BitConverter.ToUInt32(cdBuffer, ptr + 42);

                string fileName = Encoding.UTF8.GetString(cdBuffer, ptr + 46, fileNameLength);

                long compSize = compSize32;
                long uncompSize = uncompSize32;
                long localHeaderOffset = localHeaderOffset32;

                entries.Add(new ZipEntry
                {
                    FileName = fileName,
                    CompressedSize = compSize,
                    UncompressedSize = uncompSize,
                    LocalHeaderOffset = localHeaderOffset
                });

                ptr += 46 + fileNameLength + extraLength + commentLength;
            }

            return entries;
        }


    }

}
