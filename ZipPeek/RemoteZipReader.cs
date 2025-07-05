using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

namespace ZipPeek
{
    public class ZipEntry
    {
        public string FileName { get; set; }
        public long LocalHeaderOffset { get; set; }
        public long CompressedSize { get; set; }
        public long UncompressedSize { get; set; }
        public ushort CompressionMethod { get; set; }
        public bool IsEncrypted { get; set; }
    }

    public static class RemoteZipReader
    {
        private const int EOCD_SIZE = 22;
        private const int ZIP64_LOCATOR_SIZE = 20;
        private const int ZIP64_EOCD_MIN_SIZE = 56;

        private static readonly HttpClient _httpClient = new HttpClient();

        public static async Task<List<ZipEntry>> ReadAsync(string url)
        {
            long fileSize = await RemoteZipExtractor.GetRemoteFileSizeAsync(url);

            long footerSize = 256 * 1024 + 22;
            long start = Math.Max(0, fileSize - footerSize);
            byte[] footer = await FetchRangeAsync(url, start, fileSize - 1);

            int eocdOffset = FindSignature(footer, 0x06054b50); // EOCD
            if (eocdOffset < 0) throw new Exception("EOCD not found.");
            byte[] eocd = new byte[EOCD_SIZE];
            Array.Copy(footer, eocdOffset, eocd, 0, EOCD_SIZE);

            long cdStart, cdSize;
            bool isZip64 = BitConverter.ToUInt32(eocd, 10) == 0xFFFF ||
                           BitConverter.ToUInt32(eocd, 12) == 0xFFFFFFFF ||
                           BitConverter.ToUInt32(eocd, 16) == 0xFFFFFFFF;

            if (isZip64)
            {
                int locatorOffset = FindSignature(footer, 0x07064b50); // ZIP64 Locator
                if (locatorOffset < 0) throw new Exception("ZIP64 Locator not found.");
                long zip64EOCDOffset = BitConverter.ToInt64(footer, locatorOffset + 8);

                byte[] zip64EOCD = await FetchRangeAsync(url, zip64EOCDOffset, zip64EOCDOffset + ZIP64_EOCD_MIN_SIZE - 1);
                cdSize = BitConverter.ToInt64(zip64EOCD, 40);
                cdStart = BitConverter.ToInt64(zip64EOCD, 48);
            }
            else
            {
                cdSize = BitConverter.ToUInt32(eocd, 12);
                cdStart = BitConverter.ToUInt32(eocd, 16);
            }

            byte[] cdData = await FetchRangeAsync(url, cdStart, cdStart + cdSize - 1);
            return ParseCentralDirectory(cdData);
        }

        private static async Task<byte[]> FetchRangeAsync(string url, long start, long end)
        {
            _httpClient.DefaultRequestHeaders.Range = new System.Net.Http.Headers.RangeHeaderValue(start, end);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }

        private static int FindSignature(byte[] data, uint signature)
        {
            for (int i = 0; i <= data.Length - 4; i++)
            {
                if (BitConverter.ToUInt32(data, i) == signature)
                    return i;
            }
            return -1;
        }

        private static List<ZipEntry> ParseCentralDirectory(byte[] data)
        {
            var list = new List<ZipEntry>();
            int ptr = 0;

            while (ptr + 46 <= data.Length)
            {
                uint sig = BitConverter.ToUInt32(data, ptr);
                if (sig != 0x02014b50) break;

                ushort generalPurpose = BitConverter.ToUInt16(data, ptr + 8);
                ushort compression = BitConverter.ToUInt16(data, ptr + 10);
                uint compressedSize = BitConverter.ToUInt32(data, ptr + 20);
                uint uncompressedSize = BitConverter.ToUInt32(data, ptr + 24);
                ushort fileNameLen = BitConverter.ToUInt16(data, ptr + 28);
                ushort extraLen = BitConverter.ToUInt16(data, ptr + 30);
                ushort commentLen = BitConverter.ToUInt16(data, ptr + 32);
                uint localHeaderOffsetRaw = BitConverter.ToUInt32(data, ptr + 42);

                string fileName = Encoding.UTF8.GetString(data, ptr + 46, fileNameLen);
                byte[] extraField = new byte[extraLen];
                Array.Copy(data, ptr + 46 + fileNameLen, extraField, 0, extraLen);

                bool isEncrypted = (generalPurpose & 0x0001) != 0;

                long compressed = compressedSize;
                long uncompressed = uncompressedSize;
                long localHeaderOffset = localHeaderOffsetRaw;

                // ⬇️ تحقق من ZIP64 extra field إن وجد
                if (compressedSize == 0xFFFFFFFF || uncompressedSize == 0xFFFFFFFF || localHeaderOffsetRaw == 0xFFFFFFFF)
                {
                    int i = 0;
                    while (i + 4 <= extraField.Length)
                    {
                        ushort headerId = BitConverter.ToUInt16(extraField, i);
                        ushort dataSize = BitConverter.ToUInt16(extraField, i + 2);

                        if (headerId == 0x0001)
                        {
                            int offset = 0;
                            if (uncompressedSize == 0xFFFFFFFF)
                            {
                                uncompressed = BitConverter.ToInt64(extraField, i + 4 + offset);
                                offset += 8;
                            }
                            if (compressedSize == 0xFFFFFFFF)
                            {
                                compressed = BitConverter.ToInt64(extraField, i + 4 + offset);
                                offset += 8;
                            }
                            if (localHeaderOffsetRaw == 0xFFFFFFFF)
                            {
                                localHeaderOffset = BitConverter.ToInt64(extraField, i + 4 + offset);
                                offset += 8;
                            }
                            break;
                        }

                        i += 4 + dataSize;
                    }
                }

                list.Add(new ZipEntry
                {
                    FileName = fileName,
                    CompressionMethod = compression,
                    CompressedSize = compressed,
                    UncompressedSize = uncompressed,
                    LocalHeaderOffset = localHeaderOffset,
                    IsEncrypted = isEncrypted,
                });

                ptr += 46 + fileNameLen + extraLen + commentLen;
            }

            return list;
        }
    }
}
