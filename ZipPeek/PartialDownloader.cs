using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace ZipPeek
{
    internal static class PartialDownloader
    {
        private static readonly HttpClient client = new HttpClient();
        public static async Task<long> GetRemoteFileSizeAsync(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Head, url);
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (response.IsSuccessStatusCode && response.Content.Headers.ContentLength.HasValue)
                return response.Content.Headers.ContentLength.Value;
            
            return 0;
        }

        public static async Task<bool> DownloadPartialAsync(string url, string outputFile, long startByte, long endByte)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(startByte, endByte);

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (response.IsSuccessStatusCode && response.Content.Headers.ContentLength > 0)
            {
                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var fs = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
                {
                    await stream.CopyToAsync(fs);
                }

                return true;
            }

            return false;
        }

        public static async Task<byte[]> DownloadRangeAsync(string url, long start, long end)
        {
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(start, end);

                var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                if (response.StatusCode != System.Net.HttpStatusCode.PartialContent)
                    throw new Exception($"Range request not supported or failed. Status: {response.StatusCode}");

                return await response.Content.ReadAsByteArrayAsync();
            }
        }


        public static async Task<bool> DownloadRawEntryAsync(string url, ZipEntry entry, string outputFile)
        {
            const int localHeaderFixedSize = 30;
            long headerStart = entry.LocalHeaderOffset;
            long headerEnd = headerStart + localHeaderFixedSize + 256; // نقرأ زيادة علشان الاسم و extra

            byte[] headerData;
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(headerStart, headerEnd);
                var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                if (response.StatusCode != System.Net.HttpStatusCode.PartialContent)
                    throw new Exception("فشل في قراءة Local File Header");

                headerData = await response.Content.ReadAsByteArrayAsync();
            }

            // تحقق من توقيع Local Header
            if (BitConverter.ToUInt32(headerData, 0) != 0x04034b50)
                throw new Exception("توقيع Local File Header غير صحيح");

            int fileNameLength = BitConverter.ToUInt16(headerData, 26);
            int extraFieldLength = BitConverter.ToUInt16(headerData, 28);
            int headerTotalLength = 30 + fileNameLength + extraFieldLength;
            int compressedSize = BitConverter.ToInt32(headerData, 18);

            long dataStart = headerStart + headerTotalLength;
            long dataEnd = dataStart + compressedSize - 1;

            // تحميل البيانات المضغوطة فقط
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(dataStart, dataEnd);
                var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                if (response.StatusCode != System.Net.HttpStatusCode.PartialContent)
                    throw new Exception("فشل في تحميل بيانات الملف المضغوطة");

                using (var fs = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
                {
                    await response.Content.CopyToAsync(fs);
                }
            }

            return true;
        }
    }
}
