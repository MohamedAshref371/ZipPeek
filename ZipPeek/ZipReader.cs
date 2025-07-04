using System;
using System.Collections.Generic;
using System.IO;
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
    }

    public static class ZipReader
    {
        const uint EOCD_SIGNATURE = 0x06054b50;
        const uint CDFH_SIGNATURE = 0x02014b50;

        public static async Task<List<ZipEntry>> ReadZipEntriesAsync(string url)
        {
            const int maxEOCDSearch = 256 * 1024 /*65536*/ + 22;
            byte[] tailBuffer;

            // 1. معرفة حجم الملف
            long fileSize = await PartialDownloader.GetRemoteFileSizeAsync(url);
            if (fileSize == 0)
                throw new Exception("Unable to get remote file size.");

            // 2. تحميل آخر maxEOCDSearch بايت
            long startByte = Math.Max(0, fileSize - maxEOCDSearch);
            long endByte = fileSize - 1;

            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(startByte, endByte);
                var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                if (response.StatusCode != System.Net.HttpStatusCode.PartialContent)
                    throw new Exception("Server does not support HTTP range requests.");

                tailBuffer = await response.Content.ReadAsByteArrayAsync();
            }

            // 3. البحث عن EOCD
            int eocdOffset = -1;
            for (int i = tailBuffer.Length - 22; i >= 0; i--)
            {
                if (BitConverter.ToUInt32(tailBuffer, i) == EOCD_SIGNATURE)
                {
                    eocdOffset = i;
                    break;
                }
            }

            if (eocdOffset == -1)
                throw new Exception("EOCD not found.");

            int totalEntries = BitConverter.ToUInt16(tailBuffer, eocdOffset + 10);
            int cdSize = BitConverter.ToInt32(tailBuffer, eocdOffset + 12);
            int cdOffset = BitConverter.ToInt32(tailBuffer, eocdOffset + 16);

            // 4. تحميل Central Directory فقط
            byte[] cdBuffer;
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(cdOffset, cdOffset + cdSize - 1);
                var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                if (response.StatusCode != System.Net.HttpStatusCode.PartialContent)
                    throw new Exception("Unable to fetch Central Directory.");

                cdBuffer = await response.Content.ReadAsByteArrayAsync();
            }

            // 5. تحليل Central Directory
            List<ZipEntry> entries = new List<ZipEntry>();
            int ptr = 0;
            for (int i = 0; i < totalEntries; i++)
            {
                if (BitConverter.ToUInt32(cdBuffer, ptr) != CDFH_SIGNATURE)
                    throw new Exception("Invalid Central Directory header.");

                int fileNameLength = BitConverter.ToUInt16(cdBuffer, ptr + 28);
                int extraLength = BitConverter.ToUInt16(cdBuffer, ptr + 30);
                int commentLength = BitConverter.ToUInt16(cdBuffer, ptr + 32);
                int localHeaderOffset = BitConverter.ToInt32(cdBuffer, ptr + 42);

                string fileName = Encoding.UTF8.GetString(cdBuffer, ptr + 46, fileNameLength);

                entries.Add(new ZipEntry
                {
                    FileName = fileName,
                    LocalHeaderOffset = localHeaderOffset,
                    CompressedSize = BitConverter.ToUInt32(cdBuffer, ptr + 20),
                    UncompressedSize = BitConverter.ToUInt32(cdBuffer, ptr + 24),
                    CompressionMethod = BitConverter.ToUInt16(cdBuffer, ptr + 10)
                });

                ptr += 46 + fileNameLength + extraLength + commentLength;
            }

            return entries;
        }
    }
}
